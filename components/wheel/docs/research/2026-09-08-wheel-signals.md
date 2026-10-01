# Wheel rotation, handbrake and effect signals — 2026-09-08

Scope: the installed game build 17584229 and toolkit **v0.12.0**, native **0.5.0**.
This is a static call-path audit plus a synthetic managed-code experiment. No
T300 hardware, new wheel effects, gamepad input injection or game drive was used.
Game assembly decompilations remain local under `results/overnight-2026-09-08`;
only findings and original test code are committed.

## T300 rotation: cause remains unconfirmed

The report says 700 degrees becomes 1080 in the Thrustmaster panel after launch,
but the wheel still feels near 700. The available source does not request that
physical rotation change.

| Path | What it actually does |
|---|---|
| `Main.Load` → `FfbNative.Initialise` → shared `WheelFfbNative.Initialise` | Load exact native alias, configure preferred identity/strict selection, initialize DirectInput and start the constant effect |
| Native device creation | Set joystick data format, exclusive/background cooperative level, turn autocenter off, set input range 0..65535, acquire, create constant force |
| `WheelInput.Open` → shared `OpenRead` | Reuse the FFB wheel handle or create a nonexclusive/background reader; normalize its reported axis range to 0..65535 |
| Native reacquisition | Acquire/retry after focus or device errors; restart/recreate an effect when needed |
| Native emergency stop/autocenter API | Stop/release effects and manage autocenter; no angle-setting API |

All `SetProperty` calls in the pinned `native/wheelffb/WheelFfb.cpp` concern
`DIPROP_RANGE` or `DIPROP_AUTOCENTER`; the scan found no vendor angle call or
DirectInput `Escape` request. The toolkit loads system dinput8 and the mod's own
alias; it does not load a Thrustmaster rotation SDK. This excludes an explicit
700→1080 request in those audited paths, not a driver/profile reaction to them.
Microsoft describes range as the values returned by an axis, which is distinct
from wheel travel in degrees. [DirectInput device properties](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee416595(v=vs.85)).

Thrustmaster documents a T300 range of 40–1080 degrees and notes automatic game
angle control specifically for some PlayStation games. That does not establish
what this PC game or this user's driver did. Record installed driver/firmware
versions before changing anything. [T300 help center](https://support.thrustmaster.com/en/kb/1802-en/).

### Attended A/B procedure

Keep the same car, profile, USB ports, controller/virtual-device state and camera
mods. Start each case from a closed game and record the chosen 700-degree setting.
Do not change firmware or multiple settings midway through the comparison.

| Case | Mod | FFB | Direct input | Isolates |
|---|---|---|---|---|
| A | disabled before launch | off | off | Game/driver baseline |
| B | enabled | off before launch | off | Managed steering/camera patches and module load |
| C | enabled | on before launch | off | Exclusive FFB initialization and effect creation |
| D | enabled | off before launch | on | Read-only DirectInput acquisition/range request |
| E | enabled | on | on | Shared FFB/read handle interaction |

For each: record the panel angle before launch, in menu, after stage load, after
focus loss/return and after quit; separately note physical lock/travel and the
steering-axis reading at the same measured physical angles. A panel value alone
does not establish the effective game steering ratio. Use the mod's support file
for exact build identity/device/native log. Do not open another diagnostic reader
mid-case: its acquisition can itself change the experiment.

If only C/E differ, next isolate the FFB initialization calls upstream on the
reporter's rig. If D also differs, include nonexclusive acquisition and logical
range. If A differs, investigate the game/driver profile first. No toolkit change
or repin is justified by the current evidence (KI-12).

## Analog handbrake: the game has a proportional path

Verified locally by following the existing fields and method bodies:

1. `AxisCarController.GetInput` supplies float `handbrakeInput`. The mod's postfix
   can replace it with the normalized direct axis.
2. `CarController.Update` passes `handbrakeInput` unchanged to `Wheel.handbrake`
   during normal driving. It deliberately forces full handbrake at the start line
   and during stage restart; those holds are not analog-input failures.
3. `Wheel.FixedUpdate` multiplies `handbrakeFrictionTorque` by `handbrake` in its
   friction-torque calculation. There are additional low-speed braking branches;
   this does not promise a linear relationship between lever travel and stopping
   distance under all conditions.

No physics patch is needed to supply intermediate handbrake values. The new
90-assertion input suite exercises the actual consumer assignment, normalization,
serialization and override against fake transport/game boundaries. It also
reproduced and fixes KI-15: stale cached values after failed reopen, pedal/steering
Flip defects and missing assignment baselines. Actual TSS results remain pending.

## Landing and road signals already exist separately from steering force

| Candidate source | Meaning and units | Limits / next check |
|---|---|---|
| `PlayerVibrator.OnLanding` | Existing game landing event; invokes a left-motor vibration request | `UpdateAirborne` uses all-wheels-grounded transitions; not a calibrated landing impulse. `OnCollisionStay` also clears airborne state. Log correlation before choosing the event as sole detector. |
| `PlayerVibrator.UpdateVibration` / `UpdateVibrationPunches` | Slip/off-track/speed-dependent gamepad rumble; some strengths/timing randomized | These are authored gamepad effects, not axle torque or road geometry. Do not convert an unsigned rumble pulse into an arbitrary one-sided steering kick. |
| `Wheel.onGroundDown` | Per-wheel downward contact raycast boolean | Contact jitter and partly airborne cars require stable transitions/debounce |
| `Wheel.compression` / `suspensionTravel` | Actual compressed travel / maximum available travel, in game distance units consistent with meters | Normalize compression by available travel; guard missing/zero limits and nonfinite values |
| `Wheel.normalForce`, `suspensionForce`, `normalVelocity` | Load/force and suspension-velocity candidates | Timing/sign need a real capture; `normalVelocity` participates in several spring/tyre updates |
| Rigidbody velocity / derived acceleration | World motion, m/s and m/s² | Transform to vehicle local axes; reset differentiation on spawn, reset and discontinuity |
| `CollisionSoundEffects.OnImpact` | Tagged impact callback with scalar velocity magnitude | Not a measured collision impulse or side; calling it would run sound/game effects. Observe only if useful. |

`VibrationSingleton.Update` routes motor commands only to a connected,
vibration-capable last-active Rewired joystick. Our DirectInput constant-force
path does not consume it. This explains an architectural difference from the
user's earlier controller-rumble experience; it does not prove every missing
landing report has the same cause.

### Reuse toolkit primitives, keep game sampling here

The pinned toolkit supplies `CreatePeriodic`/`UpdatePeriodic` using hardware sine
effects, condition effects including a damper, and managed `ImpactMixer`.
DirectInput damper conditions depend on wheel velocity; spring conditions depend
on position. They are not interchangeable ways to reduce constant steering load.
[Microsoft DICONDITION](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee416601(v=vs.85)).

Periodic magnitude is a nominal force value and its period is in microseconds at
the DirectInput boundary. Hardware periodic/condition outputs are independent of
the constant-force mixer: its bound does not bound their combined physical torque.
[Microsoft DIPERIODIC](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee416634(v=vs.85)).

`SignalSample.Usable` accepts held values and several units. A one-shot event must
also validate its expected unit, freshness and event identity so a held sample
cannot re-trigger every frame. The current unit enum lacks meters, acceleration
and impulse; add appropriate upstream units only when a real signal contract is
ready. Do not label suspension or RPM values as normalized force merely to pass
validation.

### Reproducible numerical study

Run `dotnet run --project tools/testing/EffectsLab/EffectsLab.csproj -c Release`.
It writes a new immutable local report and CSV. Results against the pinned DLLs:
**10,242 assertions, 4,848 synthetic rows, maximum normalized output 1.0**.

| Experiment | Result / implication |
|---|---|
| Structural input 0.8, `Headroom=0.25`, no event | Output **0.6**. Simply inserting ImpactMixer would retune steering even with no collision. Keep it out of the current production pipeline. |
| Light steering 0.2 vs heavy 0.8, same event at 5 ms | Outputs **0.4 / 0.85**; event contribution is **0.25** in both. Separate steering gain can preserve event gain; global mixer strength scales both. |
| Eight simultaneous extreme event amplitudes | Bounded reserve and final output; ninth event rejected, expired events removed |
| Stale/future/NaN/infinite/held/wrong-unit signals | Proposed consumer event policy rejects them; generic usable telemetry alone is insufficient |
| Reset, bad sample, clock rollback | No retained/reappearing impact envelope in tested sequences |

The headroom values are numerical test cases, not recommended wheel settings.
FFB library SHA-256 `549C41CA701E14E0EA03D493C7E69D2DBB454AAAFB059EEFE9D1A6F1C4114F32`.
CSV SHA-256 `5442377E68BB91EE2C887386F59C1EF0583E8C8CA0EDF1712E629CD888327867`.
No native loader/output calls, Unity dependencies or new production effects are
present in the lab. This remains research, not a playthrough or hardware test.

## Newly identified telemetry sampling work — KI-16

The signal audit also found that current `TelemetryPump.FillWheels` sends maximum
`suspensionTravel` as actual travel and clamps meter-valued `compression` directly
as normalized travel. With 0.20 m capacity and 0.10 m compression, it sends
0.20 m / 0.10 normalized instead of the intended 0.10 m / 0.50 normalized.
`BuildFrame` also sends Rigidbody world velocity/acceleration/angular velocity
where Forza describes vehicle-local axes. Forza's published semantics distinguish
actual meters from normalized compression and specify local motion coordinates.
[Official Forza data-out specification](https://forums.forza.net/t/forza-motorsport-7-data-out-feature-details/74013).

These are consumer sampling issues, not encoder offsets or native FFB faults.
They should be corrected in a focused follow-up with projection/travel tests and
a recorded SimHub comparison. Correcting them changes motion/shaker amplitudes
and axis response, so this research step does not quietly change them alongside
keyboard/input fixes. Published 0.2.3 and the current wheel tune remain unchanged.
Do not use the present derived telemetry channels as calibrated impact inputs.
