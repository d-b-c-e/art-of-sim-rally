# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

- **Date:** 2026-09-09 UTC
- **Branch:** codex/overnight-0.2.5

## What Was Done
- Completed the four post-0.2.4 overnight items and an additional telemetry follow-up; see docs/reviews/2026-09-09-overnight-025.md.
- KI-21: strict device GUIDs for direct axes/buttons, unique legacy migration, neutral missing identities and bounded idle reader recovery. Assign discovers late USB devices.
- KI-19: opt-in last-session frame-health retention (8 KiB XML), idle/normal-exit atomic saves, identity/timestamps, corrupt/stale rejection and separate previous/current support labels.
- KI-22: missing wheel data/native readiness clears stale force, filter history and game force value. Same shared force tune.
- KI-23: separate shifter opens and displays by identity; cached picker survives native re-enumeration; reopen clears gear latch.
- KI-24: assignment cancels on resume; direct binding edits defer driving writes and retry locked settings files.
- KI-25: connection setup removed from physics; idle watchdog prepares after force release, destination edits wait for pause, disabling telemetry parks immediately.
- Separate probe schema 3 captures contact/suspension/Rigidbody motion with force rows; standalone analysis reports bounded landing/slide candidates and road context. Schemas 1/2 still replay. Probe absent from shipped/installed mod.
- Final 0.2.5-rc.5 passes all 16 local gates; results/rc-0.2.5-rc.5-7449894089e14b6a93ad29900db85299/automated.json. Manual cases all pending; no real corpus supplied.
- Exact source 43e0b4a2978121e712d20c8f170567b587554bdf, clean. Identity 0.2.5-rc.5+43e0b4a2978121e712d20c8f170567b587554bdf.clean.
- ZIP SHA-256 0EA35724F69D29DDF084629D38335695094EBF8051196B1483C780703C0E27F9; dist/ArtOfSimRally-0.2.5-rc.5.zip.
- Exact RC5 installed with game closed; six payloads/native alias verified, Settings.xml unchanged, RC3 backed up. Receipt results/overnight-025-install-156deb45275a414bba20b5ee1f2135a5/install-receipt.json.
- UMM displays 0.2.5; support/build.json include RC5. Stream Deck Steam 550320 still targets this install. No game launch or physical force testing.
- RC1/2/3 passed and were installed; RC4 passed but was superseded before install by the shifter display correction. Evidence immutable.
- Updated working notes, guides, roadmap/queue, known issues, changelog and capture contract; local Markdown targets checked.
- GitHub audit: only issue #1 and its PS5-unplug comment, no new review/commit comments; no replies or issue state changes sent.

## Decisions Made
- Published stable remains v0.2.4. No new release/tag or attended sign-off authorized by this work.
- Build/gates locally; zero Actions workflows. Toolkit v0.12.0/native 0.5.0/AxleForceCurve@1 unchanged.
- No force retune, new effects, physics or assist changes. No automatic probe installation.
- Deploy validated builds at feature completion only, preserving backups/settings; never close a running game. Deployment automation remains paused.
- Synchronous UDP sends remain a possible bounded-worker investigation, not a measured cause of stutter. No upstream change justified.

## Open Items
- [ ] Drive installed RC5 and fill its exact manual.json; USB identity/reconnect, shifter display, saved controls, FFB lifecycle and telemetry off/pending destination.
- [ ] Check previous-session diagnostics after normal quit/relaunch; camera matrix and motion/shaker comparison.
- [ ] TSS/Fanatec and combined-CameraMod confirmation; T300 rotation A/B.
- [ ] First real schema-3 capture before FR-2/KI-6 tuning. Contact freshness and probe overhead are unmeasured in Unity.

## Next Steps
1. Read docs/LOCAL-DEPLOYMENT.md and docs/TEST-DRIVE.md; no rebuild needed for the installed RC5 drive.
2. For capture only, install the probe with game closed, follow docs/DEVELOPMENT-CAPTURE.md, then remove it for shipping validation.
3. Preserve owner/reporter results; decide release/merge after candidate feedback. Do not post existing support drafts without authorization.

## Context for Next Session
The overnight implementation is finished and the next useful evidence requires a person.
Work is preserved on codex/overnight-0.2.5; main/public stable remain at the prior release line.
Handoff-only documentation after 43e0b4a does not require rebuilding RC5.
