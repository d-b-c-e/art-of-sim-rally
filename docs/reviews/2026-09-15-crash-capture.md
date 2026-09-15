# Crash baseline and recorder follow-through — 2026-09-15 UTC

The owner drove several head-on collisions and at least one light side collision
on installed stable 0.2.5, then paused while the recording was saved. Game closed
afterward. This is a useful force/motion baseline; the old schema-3 probe did
not record body-collision callbacks or SimHub's received/processed output.

## Preserved evidence

- Capture: `20260915-042546-e34ceee1170144408d7472f705e939d8`, probe 0.2.5.3.
- Mod identity: `0.2.5+c6242a0a163315f7a390d3860c6204b4ca215619.clean`.
- Started 04:25:46 UTC; explicitly saved at 04:27:39 while paused.
- 12,465 frame rows and 2,947 aligned, available force/motion rows. Complete
  receipt; all CSV hashes and counts verified; 2,947 native calls, zero rejected.
- Strict replay: 79,341 assertions, zero float/device mismatches. Two runtime
  rounding differences follow the already-verified Mono conversion contract.
- Driving frame p95 16.81 ms, maximum 74.67 ms; no recorded 100 ms hitch.
- Owner labels: a few head-on collisions and at least one light side collision.
  Exact event times, car/stage and obstacle identities were not supplied.
- Immutable corpus case: `owner-crash-baseline-20260915` in
  `results/regression-corpus/index.json`, alongside the original Norway landing.
  Original capture bytes are unchanged.
- Local study, owner notes, launch/settings snapshots and logs:
  `results/crash-drive-20260915-042518`.

| CSV | SHA-256 |
|---|---|
| frames | `EA2CAFC098B8144CA7A689B233AA7E1B1382B92151C5634B022D7227852B92F5` |
| forces | `6F99F10A5828885C761919A514321236B4FB64D4EE1A85B973716F3CA2CF910D` |
| signals | `F8C134EB9562D94BE0E1575C12D90A2FED5269740E230467C16B9684FAF2EEFB` |

## What the drive shows

Differentiate world Rigidbody velocity, reject discontinuities, then project to
the current car orientation. These are derived motion observations, not values
captured at SimHub's receiver. Time below is physics time since the first force
sample, not wall-clock time from pressing Record.

| Time | Force row | Speed before → after | Local forward acceleration | Steering output after |
|---:|---:|---:|---:|---:|
| 4.98 s | 299 | 144.8 → 4.4 km/h | -2,485 m/s² | approximately zero |
| 14.77 s | 886 | 38.2 → 0.1 km/h | -639 m/s² | +1.8% nominal |
| 28.28 s | 1697 | 63.6 → 33.5 km/h | -501 m/s² | -39.5% nominal |
| 40.60 s | 2436 | 58.7 → 48.8 km/h | -165 m/s² | -54.7% nominal |
| 45.50 s | 2730 | 58.3 → 5.7 km/h | -876 m/s² | -6.5% nominal |

Row 2436 also has approximately +164 m/s² local lateral acceleration, consistent
with a glancing event. It is a candidate for the owner's side-hit note, not an
exact event identification. Other peaks during the 28–30 s sequence may be
multiple contacts, rebound or further body motion; do not count each as a new
crash without contact evidence.

The largest head-on event is nearly invisible to wheel steering output: front
lateral tyre force is close to zero and the car immediately enters the low-speed
fade. A separate finite vibration can fill this gap without retuning steering.
Motion has large short spikes already; multiplying all acceleration is not
justified. Receiver timing and SimHub's input/output/filter response remain
unobserved in this capture. Keep the accepted built-in profile and gains.

## Implemented developer improvements

**Probe 0.2.5.4 writes schema 4**, preserving the original frame/force/signal
contracts and adding hashed `collisions.csv`. It passively observes the active
player's `PlayerCollider.OnCollisionEnter` before game handling, including when
FFB is unavailable. It excludes inactive/restarting states, never invokes game
actions and stores no Unity references in its fixed-capacity buffer.

Each entry includes relative velocity, total impulse, body mass/pose/velocity,
counterpart IDs/layer/known road/crowd flags, reset epoch, time and preceding force
row. Up to eight contacts are examined; the contact with greatest absolute
normal-projected relative velocity is retained, with counts/selection recorded.
Partial contact sampling is explicit. This is observation, not a collision
classifier, measured steering torque or added hardware effect.

The callback uses `GetContact`, not the allocating `contacts` array. Capacity is
4,096 entries, allocated on Start; CSVs are written only on Stop. Overflow marks
the capture incomplete. Save retry, receipt validation, integer ID precision,
legacy playback and corpus copying include the new file.

The CLR cannot prepare the actual game's collision method because of its Unity
ECalls. The gate therefore checks this patch separately using the installed
**Unity Mono**, successfully attaching/unpatching the real method without
executing a collision or initializing hardware. Existing CLR probe checks still
cover force/reset/IPC behavior. Live collision capture remains unverified.

Two developer defects found and fixed:

- **KI-34:** standalone analysis reported a landing after a reset placed the car
  airborne. Ground contact must now be observed after the last discontinuity
  before an airborne transition can become a candidate. This capture now reports
  zero landings, while the original known jump remains the comparison case.
- **KI-35:** appending a second corpus case failed at `File.Replace` because
  PowerShell converted `$null` backup-path input into an empty string. Use
  `[NullString]::Value`; regression now appends a second case and verifies the
  first stays intact. The interrupted real promotion was recovered only after
  revalidating both cases and preserving the original index.

## Validation and next step

Targeted recorder tests (74 assertions), collision protocol tests, signal tests,
CLR hook checks and actual-Mono collision attachment pass. Full local RC gate
and the exact probe deployment receipt are recorded below when complete.

The next short drive can establish live collision ordering and correlate these
values with the motion spikes. Then implement an independent wheel crash cue
with shared landing/crash periodic ownership and bounded overlap. SimHub needs
a separate recorded input/output comparison before choosing a motion change.
The shipping 0.2.5 mod, toolkit, settings and SimHub profile remain unchanged.
