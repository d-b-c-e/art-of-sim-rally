# Force feedback

## The situation

art of rally ships a complete, fully written force feedback implementation that
never runs, because the native DLL it calls into was left out of the build.

```
Wheel.Mz, suspension, surface
        │
        ▼
CarDynamics.forceFeedback (float)         ← game computes this
        │
        ▼
ForceFeedback.Update()                    ← game MonoBehaviour
        │
        ▼
SetDeviceForcesXY(int x, int y)           ← P/Invoke into "UnityForceFeedback"
        │
        ▼
        ✗  UnityForceFeedback.dll — NOT SHIPPED
```

Every one of the five Logitech native wrappers made it into the build. The one
generic force feedback wrapper did not. This looks like a packaging oversight,
not a design decision.

## The contract

Read out of `Assembly-CSharp.dll` metadata — these are the exact `DllImport`
declarations on the game's `ForceFeedback` class, module `UnityForceFeedback`,
`CallingConvention.Winapi`:

```c
int  InitDirectInput(int hwnd);
void Aquire(void);                    // sic — the game's spelling, not a typo here
int  SetDeviceForcesXY(int x, int y);
BOOL StartEffect(void);
BOOL StopEffect(void);
BOOL SetAutoCenter(BOOL enable);
void FreeDirectInput(void);
```

The class also imports `user32!GetForegroundWindow`, which is what it passes to
`InitDirectInput`.

Supporting fields on the same class, which are the tuning surface the game
already exposes:

```
force (int)              forceFeedback (float)     forceFeedbackEnabled (bool)
multiplier (float)       smoothingFactor (float)   clampValue (int)
invertForceFeedback (bool)  sign (int)  m_force (float)  cardynamics (CarDynamics)
```

### ABI gotchas that will silently break this

- **`BOOL` is four bytes.** The default P/Invoke marshalling for a C# `bool`
  return is the Win32 `BOOL`, not a one-byte C++ `bool`. Returning `bool` from
  the C++ side leaves the upper three bytes undefined and the managed caller
  reads garbage. Our implementation returns `BOOL`.
- **x64 only.** art of rally is a 64-bit Unity player. A 32-bit DLL fails to
  load with no diagnostic beyond force feedback quietly not working.
- **Exports must be undecorated** and named exactly as above. `build.ps1`
  verifies all seven with `dumpbin /exports` and fails the build otherwise,
  because the failure mode is otherwise an `EntryPointNotFoundException` thrown
  deep inside a MonoBehaviour that the game swallows.
- `InitDirectInput` takes an `int`, so the game truncates its `HWND`. Our
  implementation sign-extends it back, validates with `IsWindow`, and falls
  back to `GetForegroundWindow()` if that fails.

## Two routes

### Route A — supply the missing DLL

Build `UnityForceFeedback.dll` and drop it in
`artofrally_Data\Plugins\x86_64\`. The game's own force feedback comes alive
with **zero** patching of game code, no mod loader, and no Harmony.

This is implemented in [`src/UnityForceFeedback/`](../src/UnityForceFeedback/)
and builds today:

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
```

Status: **built and export-verified, not yet runtime-verified** — nobody has
confirmed the game actually calls it. That is phase 0 of the roadmap.

Limits of route A: we get whatever force curve the developers wrote and never
shipped. It may be excellent, it may be unusable. We control only `multiplier`,
`smoothingFactor`, `clampValue` and `invertForceFeedback`, and only by patching
those fields — and it is a single constant force with no separate road texture,
kerb or surface effects.

### Route B — compute force feedback ourselves

A Unity Mod Manager + Harmony mod that reads `Wheel.Mz` (self-aligning torque),
suspension velocity, `surfaceType` and the `ABSTriggered` / `TCSTriggered`
flags, and drives the wheel directly. Far more work, far higher ceiling: proper
self-aligning torque with load sensitivity, surface texture per material, kerb
and impact effects, and a real tuning UI.

Output options for route B:

- **Our own DirectInput** via the same native shim, extended past a single
  constant force. Works on every wheel.
- **The Logitech SDK that already ships with the game.** `Assembly-CSharp.dll`
  already binds `LogiPlayConstantForce`, `LogiPlayDamperForce`,
  `LogiPlaySpringForce`, `LogiPlayDirtRoadEffect`, `LogiPlayBumpyRoadEffect`,
  `LogiPlaySlipperyRoadEffect`, `LogiPlaySurfaceEffect`, `LogiPlayCarAirborne`
  and the collision effects, and
  `LogitechSteeringWheelEnginesWrapper.dll` **is** present. For a Logitech
  wheel this is a zero-native-code path to rich, purpose-built rally effects.
  Logitech only, so it can only ever be a bonus path alongside DirectInput.

### Recommendation

Do A first — it is a day's work and it settles every open question about
whether the game's FFB path is live. Then do B, reusing everything A taught us.
They compose: A's logging mode is the instrumentation B needs.

## Phase 0: the decisive experiment

The shipped DLL doubles as a probe. Set `AOSR_FFB_LOG=1` before launching and
every call is traced to `%LOCALAPPDATA%\ArtOfSimRally\ffb.log`.

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
$env:AOSR_FFB_LOG = "1"
# launch art of rally, drive a stage, quit
Get-Content "$env:LOCALAPPDATA\ArtOfSimRally\ffb.log"
```

Read the log:

| What you see | What it means |
|---|---|
| `InitDirectInput` then a stream of `SetDeviceForcesXY` | Everything works. The premise is proven and route A is done. |
| `InitDirectInput` but no `SetDeviceForcesXY` | The behaviour is attached but gated. Look at `CarDynamics.enableForceFeedback` and `ForceFeedback.forceFeedbackEnabled`; force them with Harmony. |
| Nothing at all | The `ForceFeedback` MonoBehaviour is not attached to a live object. Route A is dead; go to route B. |
| `no force-feedback device found` | No FFB-capable DirectInput device is attached, or another process holds it. |
| `SetCooperativeLevel ... failed` | Exclusive acquisition was refused — most likely Rewired already owns the wheel. See below. |

The `SetDeviceForcesXY` values are also a free gift: they are the game's own
computed force curve, which tells us immediately whether route A's output is
worth keeping and gives route B a reference to beat.

Note the log only records `SetDeviceForcesXY` when the value *changes*, to keep
a 60 Hz stream readable.

## Known risk: fighting Rewired for the device

Force feedback requires `DISCL_EXCLUSIVE`. Rewired already holds the wheel for
input through `Rewired_DirectInput.dll`. DirectInput permits an exclusive
acquire alongside another object's non-exclusive one, but this is exactly the
kind of thing that behaves differently per driver stack.

If `SetCooperativeLevel` fails in the log, the fallbacks in order are:

1. Retry the acquire later, once Rewired has settled after startup.
2. Use the shipped Logitech SDK path instead, which does not go through
   DirectInput at all (Logitech wheels only).
3. Reuse Rewired's own device handle from inside the process via Harmony,
   rather than opening a second one.
