# Telemetry

## Why Forza's format

art of rally has no telemetry output. Rather than invent a format and then
write plugins for every consumer, the mod emits the **Forza Horizon 4/5 "Data
Out"** UDP packet, which SimHub, dashboards, motion rigs, wind simulators and
bass shakers already understand.

That is the entire reuse argument: pick a format the ecosystem already speaks,
and art of rally inherits the ecosystem for free with nobody writing a plugin.

## The packet

324 bytes, little-endian, one datagram per physics frame, UDP to
`127.0.0.1:8000` by default (SimHub's default Forza port).

The layout is the Forza Motorsport 7 "sled" (232 bytes), then a **12-byte block
the Horizon titles insert**, then the "dash" fields. That insert is why FM7's
311-byte dash offsets do not work here and why the packet is 324 rather than
311 bytes. Getting this wrong does not throw — it renders a plausible,
completely incorrect dashboard.

### Field map

| Offset | Type | Field |
|---:|---|---|
| 0 | s32 | `IsRaceOn` |
| 4 | u32 | `TimestampMs` |
| 8 / 12 / 16 | f32 | `EngineMaxRpm`, `EngineIdleRpm`, `CurrentEngineRpm` |
| 20 | f32 ×3 | `Acceleration` X/Y/Z |
| 32 | f32 ×3 | `Velocity` X/Y/Z |
| 44 | f32 ×3 | `AngularVelocity` X/Y/Z |
| 56 / 60 / 64 | f32 | `Yaw`, `Pitch`, `Roll` (radians) |
| 68 | f32 ×4 | `NormalizedSuspensionTravel` |
| 84 | f32 ×4 | `TireSlipRatio` |
| 100 | f32 ×4 | `WheelRotationSpeed` |
| 116 | **s32** ×4 | `WheelOnRumbleStrip` |
| 132 | f32 ×4 | `WheelInPuddleDepth` |
| 148 | f32 ×4 | `SurfaceRumble` |
| 164 | f32 ×4 | `TireSlipAngle` |
| 180 | f32 ×4 | `TireCombinedSlip` |
| 196 | f32 ×4 | `SuspensionTravelMeters` |
| 212–228 | s32 | `CarOrdinal`, `CarClass`, `CarPerformanceIndex`, `DrivetrainType`, `NumCylinders` |
| **232** | — | **12-byte Horizon insert, left zero** |
| 244 | f32 ×3 | `Position` X/Y/Z |
| **256** | f32 | **`Speed` (m/s) — anchor offset** |
| 260 / 264 | f32 | `Power` (W), `Torque` (Nm) |
| 268 | f32 ×4 | `TireTemp` |
| 284 / 288 / 292 | f32 | `Boost`, `Fuel`, `DistanceTraveled` |
| 296–308 | f32 | `BestLap`, `LastLap`, `CurrentLap`, `CurrentRaceTime` |
| 312 | u16 | `LapNumber` |
| 314 | u8 | `RacePosition` |
| 315–318 | u8 | `Accel`, `Brake`, `Clutch`, `HandBrake` |
| **319** | u8 | **`Gear` — anchor offset** |
| 320–322 | s8 | `Steer`, `NormalizedDrivingLine`, `NormalizedAIBrakeDifference` |
| 323 | u8 | unused by Forza — we stamp `'R'` (0x52) as a source sentinel |

Wheel arrays are **always** front-left, front-right, rear-left, rear-right.
`WheelValues` names the corners rather than using a `float[4]` precisely so this
cannot be got wrong silently.

`WheelOnRumbleStrip` is `s32` while every array around it is `f32`. Writing a
float there yields `1.0f == 0x3F800000`, which a consumer reads as
`1065353216`. There is a regression test for this.

### Where the offsets come from

`Speed` at 256 and `Gear` at 319 are the two anchors that pin the whole layout,
and they are **not** guesses. They are taken from the sibling
`cruisn-collection` project's `harness/forza_synth.py` and `forza_probe.py`,
which were validated live against SimHub's Forza Horizon profile. Everything
else follows from field order and sizes, and the arithmetic closes exactly:
sled ends at 232, dash starts at 244, packet ends at 324.

The toolkit's `Dbce.Wheel.Telemetry.Tests` locks all of this down. If a change breaks
`SpeedIsAtOffset256_TheAnchorForTheHorizonLayout`, the change is wrong.

## Mapping art of rally onto it

The mod fills a neutral `TelemetryFrame` (metres, m/s, radians, watts, newton-
metres) and `ForzaPacket` encodes it. Sources, from
[FINDINGS.md](FINDINGS.md):

| Forza field | art of rally source | Notes |
|---|---|---|
| `IsRaceOn` | driving **or held on the start line** | true from `WAITING_TO_BEGIN` so a shaker follows the engine while revving before the lights; must go false in menus and cutscenes, or dashboards never park |
| `CurrentEngineRpm` | `Drivetrain` RPM | |
| `EngineMaxRpm` / `EngineIdleRpm` | `Drivetrain.maxRPM` / `.minRPM` | |
| `Speed` | `CarController.veloKmh / 3.6` | m/s |
| `Velocity` X/Y/Z | `CarController.body.velocity` | Rigidbody, world space |
| `AngularVelocity` | `body.angularVelocity` | |
| `Acceleration` | differentiate velocity per `FixedUpdate` | not stored by the game |
| `Yaw`/`Pitch`/`Roll` | `myTransform.rotation` | convert to radians |
| `TireSlipRatio` | `Wheel.slipRatio_hat` | |
| `TireSlipAngle` | `Wheel.slipAngle_hat` | |
| `TireCombinedSlip` | derive from the two above | |
| `SuspensionTravelMeters` | `Wheel.suspensionTravel` | |
| `NormalizedSuspensionTravel` | travel ÷ max travel | clamp 0..1 |
| `SurfaceRumble` | `Wheel.surfaceType` / `physicMaterial` | the field bass shakers key off |
| `WheelInPuddleDepth` | `Wheel.isOnPuddle` | |
| `TireTemp` | not modelled | leave zero |
| `Power` / `Torque` | `Drivetrain.maxPower`, `.netTorque` | watts, Nm |
| `Gear` | `Drivetrain.transmission` | 0 = reverse/neutral |
| `Accel`/`Brake`/`Clutch`/`HandBrake` | `CarController.*Input` | via `TelemetryFrame.ToPedal` |
| `Steer` | `CarController.steerInput` | via `TelemetryFrame.ToSteer` |
| `DrivetrainType` | `Drivetrain` powered axles | 0 FWD, 1 RWD, 2 AWD |
| `CurrentRaceTime` / `CurrentLap` | stage timer | a rally stage is one "lap" |
| `RacePosition`, `LapNumber` | 1 | no wheel-to-wheel racing |

Use `ToPedal` and `ToSteer` rather than casting. The game's smoothed inputs
overshoot their nominal 0..1 range, and an unchecked cast of `1.01f` wraps to
near zero — a full-throttle reading that momentarily shows as idle.

## Testing without the game

Two independent halves, so a failure can always be localised.

**Prove the consumer** — emit a known-good synthetic drive with no game running:

```powershell
python <toolkit>/tools/forza/forza_synth.py           # port 8000
python <toolkit>/tools/forza/forza_synth.py 5300
```

Speed ramps 0→130→0 mph on a 20 s triangle, gears step 1–5, RPM sawtooths per
gear, steering oscillates. If SimHub mirrors that, the layout and the consumer
are both fine. The synth uses the real `ForzaPacket` encoder, so it cannot drift
from what the mod actually sends.

**Prove the emitter** — listen and print, with SimHub closed:

```bash
python <toolkit>/tools/forza/forza_probe.py    # port 8000
```

The `src` column reads `mod` when byte 323 carries our sentinel, so packets from
this project are distinguishable from anything else on the port. Every packet is
logged to `results/forza_probe_log.csv`.

Verified 2026-08-31: synth → probe round-trips correctly across the C#/Python
boundary, 178 packets, zero failures, all fields decoding to the sent values.

If the probe is right and SimHub is wrong, the problem is the SimHub game
profile, not the emitter.

## Consumer setup

SimHub: add a game profile using **Forza Horizon 5** / Forza Data Out and point
it at UDP 8000. Any consumer that accepts the 324-byte Horizon packet works
unchanged.
