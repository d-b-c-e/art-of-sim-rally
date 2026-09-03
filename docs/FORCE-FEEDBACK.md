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

## Phase 0 result (2026-08-31): ANSWERED - the feature is half-built

Tested on real hardware (MOZA R12 Base), DLL installed, full stage driven with
a car instantiated. The DLL never loaded, and `Player.log` held no exception -
the game did not try and fail, it did not try. The DLL itself was ruled out:
system dependencies only (no VC runtime), `LoadLibrary` succeeds standalone,
all seven exports resolve.

Decompilation then settled *why*, and it is more interesting than "not
attached". **art of rally's force feedback was built from both ends and never
joined in the middle.**

### The consumer exists and is complete

```csharp
public void Start() {
    cardynamics = GetComponent<CarDynamics>();
    InitialiseForceFeedback();          // no gate, no condition
    SetAutoCenter(autoCentre: false);
}
public void Update() {
    forceFeedback = cardynamics.forceFeedback;
    if (Mathf.Abs(forceFeedback) > clampValue)
        forceFeedback = clampValue * Mathf.Sign(forceFeedback);
    force = (int)(forceFeedback * multiplier) * factor * sign;
    SetDeviceForcesXY(force, 0);
}
```

Note `Start()` has **no recognition check and no enable flag** - an earlier
hypothesis that FFB was gated on Rewired recognising the wheel is disproven. If
this component were attached to a live object it would have called our DLL.

### The physics exists, but is gated off

In `Wheel`, the self-aligning torque that real FFB is built from:

```csharp
if (cardynamics.enableForceFeedback && maxSteeringAngle != 0f)
    Mz = CalcAligningForce(Fz, slipAngle, inclination);
else
    Mz = 0f;
```

`enableForceFeedback` is never set anywhere, so **`Mz` is currently always
zero**. The aligning-torque model is real and present, but switched off.

### The middle link was never written

Searching the whole decompiled assembly:

- `CarDynamics.forceFeedback` is **never assigned**, anywhere. Always 0.
- Nothing ever calls `AddComponent<ForceFeedback>()` or
  `GetComponent<ForceFeedback>()`. The component is never created.

So even had the DLL shipped, and even had the component been attached, the
force would have been a constant zero. The missing DLL was a symptom, not the
cause.

### What this means for the mod

This is a *better* outcome than a working route A, because the remaining work
is small and well-defined. The mod must supply the missing middle:

1. Set `cardynamics.enableForceFeedback = true` - this alone turns on the
   game's own `CalcAligningForce` and gives real per-wheel `Mz`.
2. Each physics step, compute a force from the steered wheels and write
   (*originally from `Mz`; replaced by lateral force × trail — see the last section*)
   it to `cardynamics.forceFeedback` - the link the developers never wrote.
3. Output it, either by attaching the game's `ForceFeedback` component (which
   then drives our DLL unmodified) or by calling `SetDeviceForcesXY` directly.

**Useful calibration, free from their code:** `clampValue = 20`,
`multiplier = 0.5`, `factor = 1000`. So `forceFeedback` was intended to live in
roughly +/-20, mapping to +/-10000 - exactly `DI_FFNOMINALMAX`. That tells us
the units to produce without guessing.

**One flaw to route around:** `(int)(forceFeedback * multiplier) * factor`
casts to int *before* multiplying by 1000, quantising the output to 21 discrete
steps. That would feel notchy. Prefer calling `SetDeviceForcesXY` at full
resolution, or Harmony-patch `Update()`.

## Phase 0: the decisive experiment

The shipped DLL doubles as a probe. Set `AOSR_FFB_LOG=1` and every call is
traced to `%LOCALAPPDATA%\ArtOfSimRally\ffb.log`.

**The variable must reach the game process, not just your shell.** A game
launched from Steam inherits Steam's environment, so setting it in a terminal
and then pressing Play in Steam produces an empty log - which reads exactly
like "the game never calls our DLL" and would send you down route B for no
reason. Set it at user scope and restart Steam:

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
[Environment]::SetEnvironmentVariable('AOSR_FFB_LOG', '1', 'User')
# fully exit Steam, start it again, then launch art of rally and drive a stage
Get-Content "$env:LOCALAPPDATA\ArtOfSimRally\ffb.log"
```

Or bypass the persistent setting by starting **Steam itself** with the variable
- it passes down to the game it launches:

```powershell
& "D:\Program Files (x86)\Steam\steam.exe" -shutdown
# wait for Steam to fully exit, then:
$env:AOSR_FFB_LOG = "1"
& "D:\Program Files (x86)\Steam\steam.exe"
```

**Do not try to launch `artofrally.exe` directly.** The game ships without a
`steam_appid.txt`, so Steamworks restarts it through Steam - and the relaunched
process is spawned by Steam, so it does not inherit a variable set in your
shell. It looks like the game simply failed to start.

An empty log is only meaningful once you have confirmed the variable actually
reached the game.

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

## Wheel compatibility

**Nothing in the plugin is vendor-specific.** It is plain DirectInput 8: enumerate
`DI8DEVCLASS_GAMECTRL`, take a device advertising `DIDC_FORCEFEEDBACK`, create a
`GUID_ConstantForce` effect, and update its magnitude. No vendor SDK, no VID/PID
matching, no Moza-specific anything.

Constant force is the most universally implemented DirectInput effect there is, so
in principle this works with any PC wheel that does force feedback at all -
Logitech, Thrustmaster, Fanatec, Simucube, Simagic, Asetek, Cammus, Moza.

That is an argument from the API, not from testing. Only a MOZA R12 Base has
actually run it. The things most likely to differ elsewhere:

| Risk | Detail |
|---|---|
| **Multiple FFB devices** | Addressed: the plugin now logs every FFB device it finds and takes the first unless a preferred name is set in the mod settings. Previously it silently grabbed whichever DirectInput listed first. |
| **Exclusive acquisition** | FFB needs `DISCL_EXCLUSIVE` while Rewired already holds the wheel. Fine on this stack; other driver stacks may refuse. The log says so explicitly if it happens. |
| **Force scaling** | `FyReference` is per-wheel. A strong direct-drive base and a gear-driven Logitech want very different numbers. This is tuning, not compatibility. |
| **Axis assignment** | Force is applied on X, which is steering on every wheel that follows the convention. A device that reports steering elsewhere would need the effect axis changed. |
| **Driver compatibility modes** | Some bases can present in a mode that hides or limits DirectInput FFB. If the log shows the wheel with no FFB capability, that is where to look. |

`tools/dinput-enum` answers the first question for any machine without launching the
game: it lists every DirectInput controller and whether it reports force feedback.

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


## Sign handling (settled 2026-09-02, after two contradictory reports)

`SetDeviceForcesXY(x, 0)` must put the sign in exactly one place, and that
place has to be one every wheel reads. Three wheels, three behaviours:

| Wheel | Honours direction vector | Honours magnitude sign |
|---|---|---|
| MOZA R5 | yes | yes |
| MOZA R12 | **no** | yes |
| Fanatec (single-axis effect) | n/a — DirectInput ignores it | yes |

The original code signed both (correct on the R12, double-negated on the R5 —
"right works, left inverted, Invert does nothing"). Putting the sign only in
the direction fixed the R5 and broke the R12 ("no centre, pushes away both
ways"). The encoding that satisfies all three: **direction fixed at +X; signed `lMagnitude` carries the sign.** Do not put the sign back in the direction vector.


## The wheel going dead: `0x80040205` is NOTEXCLUSIVEACQUIRED (2026-09-02)

After the sign fix, an R12 session would run for ~45 s (or fail from the first
update if the first update came late) and then reject every `SetParameters`
with `0x80040205` — 4,401 refusals in one session, no force at all. That code
was misread twice (as `INCOMPLETEEFFECT` 0x…206 and `EFFECTPLAYING` 0x…208);
the SDK header says it is **`DIERR_NOTEXCLUSIVEACQUIRED`**: the device is
acquired, but not exclusively, and force feedback needs exclusive access. It
happens when the game loses the foreground — alt-tabbing to a chat window —
and the device comes back non-exclusive; nothing re-acquired it, so it stayed
dead.

Fix, in `SetDeviceForcesXY`: on `NOTEXCLUSIVEACQUIRED` / `INPUTLOST` /
`NOTACQUIRED`, unacquire, acquire again (exclusive once the game is in front)
and retry the update, rate-limited and logged as *"access lost … re-acquire"*.
The exclusive cooperative level is also bound to this process's own main
window rather than whatever `GetForegroundWindow()` returned at init. A
standalone probe confirmed every parameter encoding is accepted by the R12,
so nothing about the encoding was involved.

## The force model: lateral force × trail, not `Mz` (2026-09-02)

The first shipped model was `Mz` from the steered axle, on the reasoning that
the game already computes a real self-aligning torque. On the wheel it felt
like there was no centre: the wheel pulled *toward* lock, on both sides, and
snapped across the middle. A 5 Hz trace of the quantities the force is built
from (`FFB trace` lines, with `DiagnosticLogging` on) showed why:

| | |
|---|---|
| Ideal (peak-grip) slip angle | 8.5° |
| Front slip angles in ordinary corners | 12° at 86 km/h, 17° at 47 km/h, 29° at 39 km/h |
| `Mz` vs `Fy` sign, small slip | opposite (centring) |
| `Mz` vs `Fy` sign, above ~8° | same (pushes outward) |
| Share of samples above 60 km/h where `Mz` centred | 42% |

`CalcAligningForce` is a 1989 Pacejka aligning-torque curve. Like the real
thing it peaks at a few degrees of slip, then falls through zero and reverses,
and this game's tyres spend most of every corner past that point. So the
force flipped from centring to shoving outward mid-corner, every corner. It
was also tiny (|Mz| ≤ 15 against a reference of 150).

The force is now what most sims use: the **front axle's lateral force through
a pneumatic trail** — `(FyL + FyR) × trail / FyReference`, where the trail
falls linearly from 1.0 at zero slip to 0.6 at twice the ideal slip angle.
`Fy` is large (up to 4,400 N per wheel), follows the steering 98–100% of the
time above 30 km/h, and saturates without reversing; the shrinking trail
keeps the "lightening" cue as the front starts to slide. A **low-speed fade**
(0 at 3 km/h, full at 12 km/h) removes the parking-lot chaos where slip angles
are meaningless. The sign was set at the wheel: on a MOZA R12 `+Fy` centres;
`Invert` remains for devices that read the axis the other way.

`FyReference` defaults to 8,000 N — a hard corner at ~100 km/h measured
6,000–7,000 N, and 6,000 felt a little strong at Strength 50.
