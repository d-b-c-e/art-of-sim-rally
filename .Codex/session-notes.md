# Session Notes
<!-- Written by /wrapup. Read by /catchup at the start of the next session. -->

- **Date:** 2026-09-08
- **Branch:** codex/overnight-improvements

## What Was Done
- Executed the unattended scope of docs/OVERNIGHT-QUEUE.md after 0.2.3 publication.
- Reproduced KI-14 with a locked file; camera save retries now survive failure/view changes; removed per-frame tuning logs.
- Added all 11 keyboard camera bindings with cancel/clear/defaults, duplicate/modifier checks and panel/capture isolation.
- CameraTuning suite: 182 assertions. Lifecycle suite: 27 assertions.
- Added 90 actual consumer input assertions; fixed stale values after failed reopen, pedal/steering Flip and failed assignment baselines (KI-15).
- Added live normalized values to the direct-input panel.
- Traced analog handbrake into the game's torque path; no physics change needed.
- Audited T300 initialization paths; no explicit degrees request found. Wrote an attended A/B procedure.
- Added managed-only EffectsLab: 10,242 checks / 4,848 synthetic rows; headroom changes steering even without an event.
- Found telemetry sampling defects (KI-16): meters vs normalized travel and world vs local motion. Queued separate correction/rig comparison.
- Refreshed GitHub audit: still only #1 and existing PS5-unplug comment; no messages sent.
- Bumped candidate source/Info to 0.2.4 and extended RC/manual checks.

## Decisions Made
- Preserve published v0.2.3 and installed Stream Deck Steam 550320 target.
- New fixes are a separate candidate; no unattended game drive/hardware-force output.
- Reuse pinned toolkit v0.12.0; production AxleForceCurve@1/FFB tune unchanged.
- Effect work stays managed research; no speculative T300 rotation override/upstream release.

## Open Items
- [ ] Run full 0.2.4-rc.1 gate from clean committed source; record artifact and report.
- [ ] Attended UMM remapping/camera saves, input Flip/reconnect and final artifact checks.
- [ ] First real capture; TSS/Fanatec/PS5-specific verification.
- [ ] KI-16 telemetry sampling correction with explicit motion/shaker validation.
- [ ] KI-12 T300 A/B; FR-2 effects/KI-6 snapback await signal capture/tuning.

## Next Steps
1. Finish the RC gate and record identity; push the work branch.
2. Follow docs/TEST-DRIVE.md extra 0.2.4 checks with the owner.
3. Use docs/research/2026-09-08-wheel-signals.md for signal corrections and hardware follow-up.

## Context for Next Session
Published 0.2.3 remains installed, settings preserved, developer probe removed.
Offline probes use doubles/synthetic signals; no new camera/hardware validation claimed.
