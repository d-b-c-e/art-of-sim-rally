# Early landing and startup stutter investigation — 2026-09-16

The T300/TSS reporter describes slightly early landing vibration at strength 20
and a slight hitch in the first five seconds on Finland Haapajarvi, using public
0.2.5. Their support file and video have not been received. This investigation
uses existing owner captures and game/toolkit source evidence; no new drive or
physical timing measurement was performed. Public stable remains 0.2.5.

## Landing evidence

The game has three distinct observations:

- `Wheel.FixedUpdate` sets `onGroundDown` from a downward raycast extending by
  suspension travel plus loaded wheel radius.
- `Wheel.Update`/`CalcWheelMovement` separately updates visible wheel position
  from compression minus suspension travel.
- `PlayerVibrator.UpdateAirborne`/`OnLanding` waits for `AllWheelsOnGround()`
  for the stock landing response. It is not the mod's first-contact trigger.

These were read from build 17584229's previously decompiled local types under
`results/overnight-2026-09-08` in the main checkout. No game source is committed.

In unchanged corpus case `norway-stage5-reverse-m1-jump-20260911`, row 4230 has
contact mask 8 (rear right), zero recorded compression and vertical speed
-9.1866 m/s. Row 4231, 16.670227 ms later, has all four contact flags and rear-right
compression 0.08208859 m on 0.255000025 m travel. All wheels have positive
compression at row 4232. First contact is a plausible explanation for an early
impression; sampling order and rendering also matter. This is neither a
Haapajarvi reproduction nor a calibrated 17 ms correction.

The detector, 25 Hz/120 ms waveform, strength default/cap and force arbitration
are unchanged. With support logging enabled, new `LandingTiming` observes only
the last detected event for up to 200 ms: first contact mask, maximum contact-wheel
compression/travel, time to >=2% compression and time to all-wheel contact.
It cancels on discontinuity/reset. The 2% proxy is diagnostic, not a new trigger,
wheel load measurement or visual/driver-latency measurement. No timeline is
retained. It is independent of whether arbitration accepts the requested cue.

## Startup evidence and correction

Settings persistence, device discovery/open and telemetry connection setup remain
deferred to idle. Existing-manager polling avoids the lazy factory fixed in KI-20.
UDP sends remain synchronous; there is no measured need here for a transport rewrite.

Official toolkit v0.13.0 does perform synchronous native log open/write/close on
every changed `SetDeviceForcesXY` value, by default and independently of the mod's
support checkbox. This is confirmed avoidable driving I/O (KI-37), but its
relationship to this reporter's hitch is unknown.

Upstream isolated source `82c789117f115034a074bcb93133fefc8b955e35` changes that
routine trace to opt-in `DBCE_FFB_TRACE_FORCE=1`, cached once per process. Existing
startup/lifecycle/error logging remains; `DBCE_FFB_LOG=0` overrides both. It changes
no force math, delivery, recovery, waveform or exports. The consumer vendors the
complete clean local candidate (managed 0.13.1/native 0.6.1), not a native fork.
This toolkit version is **unpublished**; final packaging must await official repin.

Production-adapter fake-effect tests deliver 6,000 changed updates with identical
force parameters: x64 0.109 ms/zero force log lines by default versus 652.404 ms/
6,000 lines with tracing; x86 0.120 ms versus 649.692 ms. These totals depend on
filesystem/cache state, exclude a real device and do not predict game-frame gains.
Identical-value delivery, invalid opt-in and master-off cases also pass.
Upstream full local package checks passed. Both hardware smoke attempts loaded
all 41 exports/version 601, but found no FFB device attached; device stages did
not run and no torque was applied.

Upstream candidate archive SHA-256:
`CF7599CF460EEE247B1D869F33BA3B70D7F73F0E7F5AEAF57254EB8BC7926852`.
Vendored x64 native SHA-256:
`FE85A1ECEC10E84134EA14F8362683293894BE8D485076F8CB38CA22AE9FF2F0`.
All five upstream handoff artifact hashes were independently verified before sync.
Managed sources are unchanged; version/source metadata rebuild changes their bytes.

## Better timing evidence

Live support counters now separate first 5s, next 10s and later driving, with
nested >=33.33/50/100 ms counts and maxima. Sampling allocates/writes no files.
Schema-2 retention writes only at idle/exit; schema-1 histories remain readable
and explicitly lack the new detail. Segments restart after pause/focus return.
Frame cap matters: counts alone do not establish stutter or its cause.

Standalone replay applies the same interval policy to capture timestamps;
existing `delta_s` summaries are preserved. It cannot reconstruct focus/stage IDs.

| Unchanged owner capture | First 5s maximum | Next 10s maximum | Later maximum | >=33/50/100 ms counts, later |
|---|---:|---:|---:|---:|
| Norway reverse jump | 20.86 ms | 22.67 ms | 33.18 ms | 0 / 0 / 0 |
| Crash baseline | 24.30 ms | 25.71 ms | 86.23 ms | 1 / 1 / 0 |

No startup hitch is reproduced by these captures. The later 86 ms interval also
matches the owner's retained 0.2.5 session maximum; the previous 100 ms threshold
could miss such a smaller hitch. Neither drive is the reporter's run.

Targeted checks passed: 61 support assertions, 9,173 landing/crash assertions
including the same single landing at row 4230 and zero allocation in the warmed
timing observer, 19 replay protocol tests and warnings-as-errors solution build.
Full candidate validation and exact installation identity are recorded in
[LOCAL-DEPLOYMENT](../LOCAL-DEPLOYMENT.md), after the source is frozen.

## Next attended comparison

1. Use the same car, Haapajarvi direction, frame cap, mods and wheel settings.
   Start with crash effects off. Confirm ordinary steering and landing still work.
2. Cold-launch run: enable **Log detail for support** before starting, drive the
   first 20 seconds and the reported jump, then pause and export a support file.
   Record which jump felt early; a short video with landing sound helps relate
   contact diagnostics to the view. No gain increase or fixed delay is assumed.
3. Restart that stage, then try another stage; export separately while paused.
   Repeat the same route with support logging off to check diagnostic overhead.
4. If the hitch repeats, compare public 0.2.5 and candidate under identical
   conditions, then a mod-disabled baseline. Preserve build identity and state
   which other mods were active. Native per-force tracing stays off normally.

Get the reporter's original 0.2.5 support file/car/direction first. These checks
can narrow the cause; an absence of hitches on the owner's different wheel is
not confirmation that the T300 report is resolved. Landing visible timing, new
native hardware behavior and cold-stage performance remain unverified.
