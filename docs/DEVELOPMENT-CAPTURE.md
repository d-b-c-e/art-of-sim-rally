# Development signal capture

The removable developer probe now writes schema 3. Install and control it using
[TEST-DRIVE.md](TEST-DRIVE.md). Nothing starts automatically; no recorder is
installed by the release package. The replay executable runs without Unity, the
game or a wheel, and never emits hardware force.

## Recorded contract

Keep `manifest.xml`, `frames.csv`, `forces.csv` and `signals.csv` together.
The receipt hashes every CSV and identifies the game, mod, recorder and toolkit.
Schema 3 keeps schema 2's stateful force rows, adding exactly one signal row per
force row with the same zero-based index, realtime timestamp and reset epoch.
Sampling uses fixed-capacity value buffers: 250,000 frame observations and 90,000
force/motion observations by default. Overflow marks the saved capture incomplete.
Start allocates memory; Stop writes files only after pausing (or after output
release on normal exit). Capture exceptions stop sampling and preserve incomplete
buffers for an explicit save/retry. A crash can lose unsaved buffers.

| Fields | Meaning |
|---|---|
| `time_s` | Unity realtime clock, copied from the corresponding force observation |
| `physics_time_s` | Unity fixed-time clock for motion differentiation; seconds |
| `valid` | 1 means all four wheels and the Rigidbody were available; 0 means unavailable, with zero placeholders, not measured zero motion |
| `contact_mask` | Downward wheel contact bits: FL=1, FR=2, RL=4, RR=8 |
| `p*_m`, `v*_mps` | Rigidbody world position in meters and velocity in meters/second |
| `qx,qy,qz,qw` | Rigidbody rotation from vehicle-local to world coordinates |
| `local_v*_mps` | Fresh world velocity projected through inverse rotation; local X right, Y up, Z forward |
| `compression_*_m`, `travel_*_m` | Raw wheel compression and available suspension travel in meters, ordered FL/FR/RL/RR; normalized compression is their ratio when capacity is positive |

The probe reads at the boundary where the mod's `CarDynamics.FixedUpdate`
postfix consumes steering force. It reads every value anew; it does not reuse
previous measurements when a component is missing. Unity's ordering of other
components' wheel/Rigidbody updates has **not been measured in a real capture**.
Contact is a raycast boolean, not a collision impulse. Compression can exceed
nominal travel; the research file preserves it instead of clipping the evidence.
Frame input channels are the mod's direct-input values, not physical wheel
position/velocity telemetry or all of Rewired's game inputs.

## Standalone analysis

```powershell
dotnet run --project tools/testing/Replay -c Release -- --replay 'C:/path/to/capture'
```

The JSON result retains original-versus-toolkit force comparison, integer device
magnitude checks, reset-history validation and frame timing. Its `signals` section
adds the following descriptive measurements:

- **Landing candidates:** at least 80 ms of observed all-wheel airborne state,
  followed by at least 40 ms of nonzero contact. This rejects brief contact
  flicker. Each candidate references the first contact's force row, prior/current
  steering output, slip, speed, normalized compression and local acceleration.
- **Slide-recovery candidates:** while in contact above 12 km/h, mean front slip
  decreases from at least twice the ideal angle to at most the ideal angle.
  This does not establish oversteer, a tank-slapper, or a driver's wheel motion.
- **Road context:** RMS change in normalized compression across consecutive
  all-four-grounded samples, plus the number of contributing wheel pairs. This
  depends on sample rate and is not road roughness in physical units.
- Maximum absolute local vertical acceleration, usable/missing row counts,
  discontinuities and omitted event count. At most 128 detailed events are
  returned; totals still cover the capture. Missing acceleration reports null.

Acceleration differentiates **world** velocity before projecting into the
current orientation. No derivative or event history crosses a force reset epoch,
missing sample, duplicate/backward physics time, gap above 100 ms or apparent
teleport (displacement above 5 m plus the larger observed speed times delta).
These are conservative research thresholds, not production effect defaults.
Acceleration includes game motion/gravity; it is not a calibrated landing impulse.
Neither landing nor slide candidates classify a crash or prescribe gain/damping.

Schema 1 and 2 remain readable and explicitly report unavailable signal context.
Only continuous game-origin schema 2/3 captures can enter a regression corpus;
schema 3 promotion preserves and rechecks the signal file too. Hashes, row counts,
alignment, finite values, contact flags, quaternion and local projection are
validated before a successful report. Signal files are limited to 64 MiB; world
positions above 10 million meters or velocities above 10,000 m/s are rejected as
outside this capture contract. Synthetic test fixtures never qualify as driving
evidence merely because they exercise the game-origin protocol.

## Next attended capture

Keep the existing force tune. Capture ordinary road, a jump/landing and a
controlled slide, noting car/stage and approximate event times. Compare those
notes/video with candidate rows and signal freshness before choosing an effect
design. Remove the probe and repeat a short run to assess its own overhead.
New impact/damper effects and FR-2 tuning still require this evidence and an
attended hardware comparison. See [the prior signal research](research/2026-09-08-wheel-signals.md).
