# Session Notes
<!-- Written by /wrapup; overwritten each session, history preserved in git. -->

- **Date:** 2026-09-09 UTC
- **Branch:** main

## What Was Done
- Published stable/latest v0.2.4; release URL https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.4.
- Fast-forwarded overnight work to main. Tag/source dc14fe70002205e59c8b462c8c8636b72132abdd; later docs do not rebuild it.
- Final identity 0.2.4+dc14fe70002205e59c8b462c8c8636b72132abdd.clean; archive SHA-256 08AA70055C4957DA8C96EAB2078BA0F52211E51B7249C38A0B409BBC6AD50942.
- All 16 local automated checks passed: results/rc-0.2.4-2af3c9e99e0a42e0ab44731433055c7c/automated.json. Final manual cases remain pending.
- Downloaded ZIP/checksum and matched report plus GitHub asset digests; exact downloaded ZIP installed with game closed.
- Six mod payloads/native alias verified; Settings.xml unchanged; RC5 backed up. Receipt: results/release-0.2.4-install-3b9ec98a27bf415b807d7974ff3484c3/install-receipt.json.
- Stream Deck Steam 550320 targets final 0.2.4; no game launched, developer probe absent.
- Owner accepted RC5 before release. Preserved log: 197,466 bytes, one manager initialization, zero initialization errors/connection failures/exceptions.
- KI-20 error flood resolved on owner's rig. KI-19 T300 intermittent slowdown remains unknown; no universal stutter claim.
- Updated release notes, guides, issue register, roadmap and unsent T300/TSS reply: docs/replies/2026-09-08-t300-tss.md.
- Post-publication GitHub review: only issue #1 and its existing PS5-unplug comment, no new reports or messages sent.
- Owner replaced scheduled deployment checks with a feature/fix completion checklist; automation verified PAUSED.
- Prepared next overnight queue in docs/OVERNIGHT-QUEUE.md: USB identity/reconnect, last-session diagnostics, lifecycle/performance audit, then development signal captures. Work is queued, not implemented.

## Decisions Made
- Build/test/package locally; GitHub minutes exhausted. Exact assets uploaded directly, zero Actions workflows.
- Owner's overall RC5 acceptance authorized release; full matrix/final-labelled drive still pending.
- Production source/Version.props/toolkit match RC5 source 5701ebb. Toolkit v0.12.0/native 0.5.0/AxleForceCurve@1 unchanged.
- Deploy validated artifacts when finishing each feature/fix, game closed, backup/settings preserved. Owner stopped scheduled polling; automation is paused.

## Open Items
- [ ] Complete detailed camera/input/FFB/telemetry matrix and final-labelled drive.
- [ ] T300/TSS, Fanatec and combined-CameraMod confirmation; motion/shaker comparison.
- [ ] First real capture before FR-2 effects/KI-6 snapback work; KI-12 rotation A/B.

## Next Steps
1. Read docs/reviews/2026-09-09-release-0.2.4.md and docs/LOCAL-DEPLOYMENT.md for exact published/installed evidence.
2. Give owner the draft reply to collect feedback on 0.2.4; do not post externally without authorization.
3. Use docs/TEST-DRIVE.md for remaining checks and docs/OVERNIGHT-QUEUE.md for follow-up.
4. Start the new overnight queue with reproducible input failures and post-exit diagnostic retention; don't repeat completed EffectsLab research or claim hardware validation.

## Context for Next Session
Final 0.2.4 is published and installed with settings preserved. RC5 was driven and accepted;
the exact final-labelled package has not been driven. Deployment is now a feature/fix
completion checklist item; keep-art-of-sim-rally-installed-build-current is paused.
