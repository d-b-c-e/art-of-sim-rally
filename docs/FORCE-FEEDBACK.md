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
- **Exports must be undecorated** and named exactly as above. The toolkit's
  `build.ps1` verifies every export with `dumpbin /exports` and fails the build
  otherwise, because the failure mode is otherwise an
  `EntryPointNotFoundException` thrown deep inside a MonoBehaviour that the
  game swallows.
- `InitDirectInput` takes an `int`, so the game truncates its `HWND`. Our
  implementation sign-extends it back, validates with `IsWindow`, and falls
  back to `GetForegroundWindow()` if that fails.

## Two routes (historical — route B is what shipped)

This section is the analysis done before anything ran, kept because it explains
why the mod is shaped the way it is. **Route A turned out to be impossible** —
see "Phase 0 result" below — and route B is what ships today.

### Route A — supply the missing DLL

Build `UnityForceFeedback.dll` and drop it in
`artofrally_Data\Plugins\x86_64\`. The game's own force feedback comes alive
with **zero** patching of game code, no mod loader, and no Harmony.

**Outcome: dead.** The DLL was built and installed, and the game never called
it — not because the DLL was wrong, but because the `ForceFeedback` behaviour it
would serve is never attached to anything. Settled 2026-08-31; the evidence is
under "Phase 0 result" below.

The DLL itself lives on, driven by the mod instead of by the game. Its source is
no longer in this repo: it is `WheelFfb.dll` from
[dbce-wheel-mod-toolkit](https://github.com/d-b-c-e/dbce-wheel-mod-toolkit),
vendored under `lib/toolkit/native` and shipped as `UnityForceFeedback.dll` —
the name the mod P/Invokes. Refresh the pin with `tools\Sync-Toolkit.ps1`.

Limits route A would have had: we get whatever force curve the developers wrote and never
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

### How it actually went

A was done first, on the reasoning that it was a day's work and would settle
every open question about whether the game's FFB path was live. It did settle
them — by answering no. The work was not wasted: A's DLL is B's output stage,
and A's logging mode is the instrumentation B was debugged with.

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

## The native log

The DLL traces itself to `%LOCALAPPDATA%\ArtOfSimRally\ffb.log`. It is the first
thing to read for any force-feedback report, and the support file embeds it.

**Logging is on by default** — since the native layer moved to
dbce-wheel-mod-toolkit, no environment variable is needed to switch it on. The
mod calls `SetLogPath` at init to keep the historical `ArtOfSimRally\ffb.log`
location rather than the toolkit's default `%LOCALAPPDATA%\DbceWheel\ffb.log`.

| Variable | Effect |
|---|---|
| `DBCE_FFB_LOG=0` | turns logging **off** |
| `DBCE_FFB_LOG_PATH=<file>` | redirects it, overriding the default |

> Earlier revisions of this document told you to set `AOSR_FFB_LOG=1`. **That
> variable no longer exists** and setting it does nothing — harmless now that
> logging defaults to on, but it was worth an hour of confusion when the
> renaming landed. If a set of instructions anywhere still mentions it, they
> predate toolkit 0.1.0.

An environment variable does still have to reach the game *process*, not just
your shell, if you ever need one. A game launched from Steam inherits Steam's
environment, so setting a variable in a terminal and then pressing Play in Steam
has no effect on it. Set it at user scope and fully restart Steam, or start
Steam itself with the variable set:

```powershell
& "D:\Program Files (x86)\Steam\steam.exe" -shutdown
# wait for Steam to fully exit, then:
$env:DBCE_FFB_LOG = "0"
& "D:\Program Files (x86)\Steam\steam.exe"
```

**Do not try to launch `artofrally.exe` directly.** The game ships without a
`steam_appid.txt`, so Steamworks restarts it through Steam — and the relaunched
process is spawned by Steam, so it does not inherit a variable set in your
shell. It looks like the game simply failed to start.

### Reading it

| What you see | What it means |
|---|---|
| `InitDirectInput` then a stream of `SetDeviceForcesXY` | Working normally. |
| `no force-feedback device found` | No FFB-capable DirectInput device attached, or another process holds it exclusively. |
| `SetCooperativeLevel ... failed` | Exclusive acquisition refused. See "fighting Rewired for the device" below. |
| `CreateEffect ... 0x80040154` | The chosen device has no actuator — the wrong twin of a Fanatec base. The mod now tries the others. |
| `access lost ... re-acquire` | `0x80040205`, focus was lost and the device came back non-exclusive. Recovering as designed; a flood of them is a problem. |
| `SetParameters FAILED` | The device refused the force outright. Report it with the support file. |

The log only records `SetDeviceForcesXY` when the value *changes*, to keep a
60 Hz stream readable.

### What it said in 2026-08-31's decisive experiment

The same log settled phase 0. With the DLL installed and a full stage driven,
it was **empty** and `Player.log` held no exception — the game did not try and
fail, it did not try. That is what killed route A, and the table used to read:

| What you saw | What it meant then |
|---|---|
| `InitDirectInput` then `SetDeviceForcesXY` | The premise is proven, route A is done. |
| `InitDirectInput` but no `SetDeviceForcesXY` | Attached but gated; force `enableForceFeedback` with Harmony. |
| Nothing at all | The `ForceFeedback` MonoBehaviour is not attached. Route A is dead. |

It was the third row.

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
| **Multiple FFB devices** | Every FFB device is logged with its axis and button counts; the preferred one (by index, then name) is opened first and, if it cannot create a constant-force effect, the others are tried before giving up. A Fanatec direct-drive base presents **two** `FANATEC Wheel` devices with the same name and only one has the actuator (the other fails with `0x80040154`); the dropdowns show axes/buttons so the twins can be told apart. One DirectInput instance lives for the whole session: releasing it after a "temporary" enumeration while the device table stayed populated crashed the game when a shifter was chosen (2026-09-03). |
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

### Clamp order: fade first, clamp last (measured 2026-09-04)

`ForceCurve.Normalised` scales, fades, and **then** clamps to ±1. The clamp is a
*device* constraint — DirectInput takes ±10,000 — so it belongs at the boundary,
after the model has had its say. dbce-wheel-mod-toolkit's `ForceModel.Compute`
does the opposite: it clamps before returning, and `ForceShaper` applies the fade
to an already-clamped value.

The difference only appears when a force past full scale meets a partial fade at
the same instant — which is a low-speed slide, and nothing else. Across the 640
grid rows of `force-curve-vector.csv`, 16 saturate and **4 differ**, all at 5 and
7.5 km/h:

| Fy total | slip | km/h | pre-fade | fade | ours | clamp-first | Δ at the wheel |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 14,000 | 0° | 7.5 | 1.217 | 0.500 | 0.609 | 0.500 | 1,086 / 10,000 |
| 14,000 | 4.25° | 7.5 | 1.096 | 0.500 | 0.548 | 0.500 | 478 |
| 14,000 | 0° | 5 | 1.217 | 0.126 | 0.154 | 0.126 | 274 |
| 14,000 | 4.25° | 5 | 1.096 | 0.126 | 0.138 | 0.126 | 120 |

Note what those rows require: over 11,500 N of front lateral force at 5–7.5 km/h.
That is barely reachable in the real game — a car doing walking pace cannot
generate cornering force like that — so the divergence is most likely confined to
an impact or a low-speed slide where `Fy` spikes. It is real, it is measured, and
it is probably never felt.

Ours is the better order on layering grounds: the fade is a speed-based
authority scale on the *model*, and applying a *device* limit before it throws
away headroom the fade was meant to shape. The toolkit asserts the difference
rather than hiding it (`ClampOrderDiffersAndIsMeasured`); aligning it is a
decision for the toolkit, not a fix, because it moves every profile's output and
a cross-language golden file.

`FyReference` defaults to 11,500 N — a hard corner at ~100 km/h measured
6,000–7,000 N; 6,000 and then 8,000 were both too strong at Strength 50 on a
MOZA R12 (the owner settled at 20 with 8,000), so the default is 30% lighter
again (2026-09-03).
