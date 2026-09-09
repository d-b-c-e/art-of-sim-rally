# Session Notes
<!-- Written by /wrapup. Read by /catchup at the start of the next session. -->

- **Date:** 2026-09-08
- **Branch:** codex/overnight-improvements

## What Was Done
- Follow-up user feedback authorized further roadmap implementation while away and a short unsent reply.
- Added CameraMod isolation (KI-18), bounded support logs/frame aggregates/live input + mod inventory (KI-19 diagnostics), and corrected legacy assist UI/help (KI-17; behavior unchanged).
- Corrected telemetry suspension/local-motion sampling and reset acceleration history (KI-16); 1,230 actual sampling/encoded-UDP assertions.
- Observed toolkit Send=false; actual closed-socket consumer test now passes (36 assertions), replacing a manually invoked error handler.
- Support tests: 25, with bounded real files and 100,000 allocation-free counter updates. Lifecycle/CameraMod tests: 36.
- Short reply: docs/replies/2026-09-08-t300-tss.md. New review: docs/reviews/2026-09-08-feedback-review.md.
- RC3 stopped before packaging: direct Unity ECalls in watchdog Update broke the offline Harmony probe hook. A non-inlined runtime helper fixes attachment; the actual 14-assertion hook test and full RC4 pass without skipped/weakened gates.
- Current candidate: dist/ArtOfSimRally-0.2.4-rc.4.zip; identity 0.2.4-rc.4+38c1bff31ff1ca20696c15a8b7de9298ec04dbf3.clean.
- ZIP SHA-256 C5284CDF7F0B2991F8E013246857A414CD15FADCCE583AD54F923251B8F47887 independently rechecked.
- All 16 automated checks passed: results/rc-0.2.4-rc.4-744b3572e72e4404a229e5d10fbec92e/automated.json. All seven manual cases pending; check correctly rejects missing tester/rig. No real corpus, installation, drive or publication of RC4.
- Stable 0.2.3's six payload hashes and Settings.xml still match final-install receipt. Stream Deck remains on that installation. Later documentation commits do not rebuild RC4.
- Original overnight work/RC2 details below remain historical.
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
- Historical artifact: dist/ArtOfSimRally-0.2.4-rc.2.zip; identity 0.2.4-rc.2+43334e2ad627674efc71ee4e6a18d65d207619a3.clean.
- ZIP SHA-256 D2ADF06B614E0925B8C5AA04B54B418713EF7DDD1AE05AB03F4401A40B760CF6.
- Report: results/rc-0.2.4-rc.2-ee7891e97a444aa08e82bffd07746eb7/automated.json; manual.json correctly remains pending.
- Handoff: docs/reviews/2026-09-08-overnight.md. Stable install payloads/settings/native copy rechecked; probe DLL and Info absent (historical cache only).

## Decisions Made
- Owner's GitHub Actions minutes are exhausted: build/test/package locally and upload exact artifacts with gh release. No workflows found through the GitHub API; none needed disabling. Recorded procedure in docs/RELEASING.md and working notes. RC4 was already built locally; no rebuild/publication or runtime sign-off occurred in this documentation update.
- Preserve published v0.2.3 and installed Stream Deck Steam 550320 target.
- New fixes are a separate candidate; no unattended game drive/hardware-force output.
- Reuse pinned toolkit v0.12.0; production AxleForceCurve@1/FFB tune unchanged.
- Effect work stays managed research; no speculative T300 rotation override/upstream release.

## Open Items
- [x] Run full candidate gate from clean committed source; record artifact and report.
- [ ] Attended UMM remapping/camera saves, input Flip/reconnect and final artifact checks.
- [ ] First real capture; TSS/Fanatec/PS5-specific verification.
- [x] KI-16 sampling correction and offline/UDP validation; attended motion/shaker comparison still pending.
- [ ] KI-12 T300 A/B; FR-2 effects/KI-6 snapback await signal capture/tuning.

## Next Steps
1. Read the feedback follow-up review for the newest candidate and exact evidence; RC2 is historical.
2. Follow docs/TEST-DRIVE.md extra 0.2.4 checks with the owner. Install the exact candidate first, with the game closed.
3. Use docs/research/2026-09-08-wheel-signals.md for KI-16 corrections and hardware follow-up.

## Context for Next Session
Published 0.2.3 remains installed, settings preserved, developer probe removed.
Offline probes use doubles/synthetic signals; no new camera/hardware validation claimed.
