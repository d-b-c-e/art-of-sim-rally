# Session Notes
<!-- Written by /wrapup; overwritten each session, history preserved in git. -->

- **Date:** 2026-09-09 UTC
- **Branch:** codex/overnight-improvements, preparing fast-forward to main

## What Was Done
- Owner accepted installed 0.2.4-rc.5 and explicitly requested publication of 0.2.4.
- Preserved RC5 logs/build/settings in results/release-0.2.4-preparation/rc5-drive-20260909-025859.
- Unity log: 197,466 bytes, one normal manager initialization, zero constructor errors/connection failures/exceptions; RC4 flood absent.
- Updated release notes, changelog, guides, known issues and roadmap for 0.2.4. KI-20 resolved on owner's rig; KI-19 remains unknown.
- Production source, toolkit and Version.props still match RC5 source 5701ebb69adcd17fe4a4122806b77b88594155a8.
- Draft support reply: docs/replies/2026-09-08-t300-tss.md; not sent.
- GitHub issue #1 unchanged; existing PS5-unplug comment only.

## Decisions Made
- Build/test/package locally; GitHub minutes exhausted. Upload exact ZIP/checksum directly, no Actions workflows.
- Owner's overall RC5 acceptance authorizes release; preserve incomplete detailed matrix/final-labelled drive as pending.
- Keep installed/deployed copy current automatically, game closed, with backups/settings preserved. Never close game to update.
- Toolkit v0.12.0/native 0.5.0/AxleForceCurve@1 unchanged; no speculative force/physics/assist changes.

## Open Items
- [ ] Finish final 0.2.4 gate, tag/publication, downloaded-asset verification and installation receipt.
- [ ] Complete detailed camera/input/FFB/telemetry matrix and final-labelled drive.
- [ ] T300/TSS, Fanatec and combined-CameraMod confirmation; motion/shaker comparison.
- [ ] First real capture before FR-2 effects/KI-6 snapback work; KI-12 rotation A/B.

## Next Steps
1. Complete release with docs/reviews/2026-09-09-release-0.2.4.md as the canonical handoff.
2. Record exact installed identity/receipt in docs/LOCAL-DEPLOYMENT.md.
3. Collect user feedback on the released build; use docs/TEST-DRIVE.md for remaining checks.

## Context for Next Session
RC5 installed and accepted. Final release preparation is in progress; later documentation
does not rebuild immutable artifacts. Hourly heartbeat keep-art-of-sim-rally-installed-build-current
is active and quiet when unchanged or waiting for the game to close.
