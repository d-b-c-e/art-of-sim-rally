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
- Full RC1 and RC2 runs passed all 16 automated checks. RC2 corrects the packaged camera guide and scopes prior owner evidence to 0.2.3-rc.6.
- Current artifact: dist/ArtOfSimRally-0.2.4-rc.2.zip; identity 0.2.4-rc.2+43334e2ad627674efc71ee4e6a18d65d207619a3.clean.
- ZIP SHA-256 D2ADF06B614E0925B8C5AA04B54B418713EF7DDD1AE05AB03F4401A40B760CF6.
- Report: results/rc-0.2.4-rc.2-ee7891e97a444aa08e82bffd07746eb7/automated.json; manual.json correctly remains pending.
- Handoff: docs/reviews/2026-09-08-overnight.md. Stable install payloads/settings/native copy rechecked; probe DLL and Info absent (historical cache only).

## Decisions Made
- Preserve published v0.2.3 and installed Stream Deck Steam 550320 target.
- New fixes are a separate candidate; no unattended game drive/hardware-force output.
- Reuse pinned toolkit v0.12.0; production AxleForceCurve@1/FFB tune unchanged.
- Effect work stays managed research; no speculative T300 rotation override/upstream release.

## Open Items
- [x] Run full candidate gate from clean committed source; record artifact and report.
- [ ] Attended UMM remapping/camera saves, input Flip/reconnect and final artifact checks.
- [ ] First real capture; TSS/Fanatec/PS5-specific verification.
- [ ] KI-16 telemetry sampling correction with explicit motion/shaker validation.
- [ ] KI-12 T300 A/B; FR-2 effects/KI-6 snapback await signal capture/tuning.

## Next Steps
1. Review the committed overnight handoff and exact RC2 artifact; any rebuild needs rc.3 or later.
2. Follow docs/TEST-DRIVE.md extra 0.2.4 checks with the owner. Install the exact candidate first, with the game closed.
3. Use docs/research/2026-09-08-wheel-signals.md for KI-16 corrections and hardware follow-up.

## Context for Next Session
Published 0.2.3 remains installed, settings preserved, developer probe removed.
Offline probes use doubles/synthetic signals; no new camera/hardware validation claimed.
