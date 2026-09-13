# Landing vibration candidate

Implemented for development testing on 2026-09-12 at the owner's request.
This feature is not in stable 0.2.4. It is off by default and requires the new
toolkit finite-periodic API (managed 0.13.0/native 0.6.0). A local development
pin is identified by its exact commit, not represented as a published release.
The toolkit must be published and officially repinned before a public mod release.

Frozen toolkit source: `dd0ef20ad0cdaccc7a67f10a707dbd2a27a6efe9`, local archive
SHA-256 `2F73F5427465CD85E8D969EE7ECAF7D9C2CB08ED923DA80C4AC7C7234AF1B0CC`.
The five vendored artifacts match that package. Upstream local package gates and
zero-output MOZA R12 ABI smoke pass on x86/x64; no nonzero burst was applied.

## What it does

The wheel generates a 25 Hz sine vibration for 120 ms: three cycles. Its finite
lifetime is set on the native device effect, so a frozen Unity callback cannot
hold it indefinitely. This avoids choosing a steering-torque direction from a
vertical landing. Native sine support is required; rejection leaves steering
working and displays a retry instruction. No synthetic constant-force fallback.

The native single-axis setup follows Microsoft's
[DirectInput effect-direction contract](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417536(v=vs.85)).

**Landing strength** sets the maximum vibration magnitude independently of the
existing steering **Strength** and **Smoothing**. Default 5, range 0..20 percent
of nominal device force; the driver's overall gain still applies. Zero disables
it. Steering arithmetic, telemetry packets and game physics are unchanged.
Independent effects are added by the driver; strong cornering can consume its
available output range. This is not a guarantee of unclipped combined torque.

The game-specific detector requires at least 100 ms of established contact,
then no wheel contact for at least 120 ms, followed by a descending touchdown.
One wheel regaining contact triggers one event; later wheels do not trigger more.
Pre-contact world descent above 1.5 m/s scales the cue, reaching its configured
maximum at 10 m/s. It requires motion, rejects overturned samples, and enforces
a 500 ms event cooldown. These are conservative authored cue thresholds, not a
measured impact calibration. Low speed/small hops may intentionally produce none.

Pause, focus loss, restart, invalid samples, missing wheel/body data, car changes,
teleports, clock reversal and gaps over 100 ms reset detection. Wall-clock stalls
over 250 ms also reset flight history,
even if Unity catches up its physics steps afterward. Slot preparation happens
only while idle and focused. Failed play requests are dropped rather than replayed
after recovery; toggle the effect while paused to retry setup.

## Evidence and limits

- Installed RC8 is the exact clean-source package from commit
  `b9598f5cb6b0a48965b37f4f8d27f350fd59be49`. All 16 local gates passed, including
  the preserved drive corpus and 9,044 landing assertions. See the
  [gate and deployment receipt](LOCAL-DEPLOYMENT.md). No attended case is passed
  by this result; the original drive predates the new effect.
- Production detector, delivery policy and game adapter execute against fake
  device/game boundaries: 30/60/120 Hz transitions, jitter, partial wheel lift,
  spawn in flight, rollovers, resets, missing data, focus, effect-disable and
  rejected setup/play/stop. Detector sampling allocates zero managed bytes over
  100,000 updates after warmup.
- The preserved Norway reverse/M1 drive produces exactly one event at force row
  4230: rear-right contact after the recorded jump. Its approximately 10.16 m/s
  pre-contact descent reaches the configured maximum. No capture bytes changed.
- Full consumer force regression/replay still checks the original steering
  curve and exact native constant-force integers. The periodic effect is a
  separate output. Steering CSV replay does **not** verify its physical force
  or delivery; the replay report explicitly says so.
- The shared API's fake native effect tests cover finite expiry without further
  callbacks and low magnitudes, plus focus/watchdog release. They do not prove
  a particular wheel driver's implementation or how this feels.

Live support files include landing event/accepted/rejected counters and the last
requested magnitude. Enable **Log detail for support** before a short test to
record each landing's air time, descent, magnitude and driver acceptance.

## Attended comparison

1. Start with the installed candidate, usual steering tune and **Landing vibration
   off**. Enable **Log detail for support** for this short comparison. Drive a
   familiar stage with a jump and note steering/landing feel.
2. Pause. Enable **Force feedback → Landing vibration (experimental)**, leave
   **Landing strength at 5**, and wait for Ready. Repeat the same stage/jump.
3. Confirm one short vibration at touchdown, with ordinary steering unchanged
   between jumps. Compare with a smaller hop and bumpy ground if available.
4. Check pause, focus loss, disabling the effect, finish/replay and quit. Vibration
   must stop; a restart or return from alt-tab must not replay the landing.
5. While paused, export support and note whether the cue was absent, useful,
   weak, harsh or mistaken for a steering pull. Save any developer recording
   explicitly before quitting. Finish with a run without the developer probe.

First RC8 test on 2026-09-12: eight live wheel cues were accepted, including the
20% maximum. The owner clarified that wheel FFB is fine; their weak/buzzy landing
report concerns the ButtKicker. **This setting does not control the shaker or
change telemetry.** Shaker thud enhancement is a separate FR-2 follow-up.
See [the corrected interpretation](reviews/2026-09-12-landing-wheel-test.md).
Unsupported-driver behavior and exact packaged release sign-off remain pending.
Existing FFB startup/recovery, gauges and quit retests still apply.
