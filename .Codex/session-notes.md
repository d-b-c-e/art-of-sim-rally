# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

## Attended session stopped — 2026-09-10 local / 2026-09-11 UTC

- Latest owner follow-up: retry had no FFB, Alt+F4 hung, and first-run gauges
  stayed at finish-line speed through stage end/quit. RC5 manual ffb-lifecycle
  and telemetry now **failed**, all other cases pending. See KI-28/29/30.
- FFB startup failed before recorder load (0x80040205, re-acquire 0x80070578).
  No intentional disable/tune change; no automatic init retry exists. SimHub
  recognised race end at 21:12:50.865 but consumer gauge clearing remains unproven.
  Alt+F4 save/reader closure appears in logs; hang cause still unknown.
- Prioritized investigation queue updated. No production fixes for these three
  reports implemented yet. Inspect source/lifecycle and exact installed SimHub
  consumer, then reproduce before changing force/packet behavior.
- **Nothing recording; game closed.** Owner abandoned the retry and requested
  stop. Its automatic Unity shutdown save has 2169 frames / zero force rows;
  hashes verified, marked abandoned, excluded from corpus and release acceptance.
- First drive included good jumps but was lost on normal menu Quit. Retained logs:
  12206 measured driving intervals, max 33.95 ms, no 100ms+ hitches; 11731 native
  force calls. No retained motion/contact capture to analyse landings.
- Fixed KI-26: Unity Mono's unsupported WindowsIdentity.User and asynchronous
  pipe handling prevented IPC, hidden by background retries. Win32 SID lookup
  preserves current-user ACL; blocking pipe and synchronous startup now work in Unity.
- KI-27: ExitGame.Exit kills its own process, bypassing Unity quit callbacks.
  Probe 0.2.5.2 prefixes it, releases/saves first, cancels quit on pending/failed
  save. Explicit menu Start/Stop and ordinary Unity shutdown saving verified;
  the distinct menu-kill interception still needs an attended test.
- RC5/settings preserved; old probe backed up. Installed probe 0.2.5.2 DLL hash
  CD47CCF20A5007FA9C73658A2BEF9CA977F771A15FA48BCD4A4B82A492138754.
- Probe build passed; actual net48 tests 17 assertions and recorder tests 58 pass.
- Details/evidence: docs/reviews/2026-09-10-attended-rc5.md. No attended cases marked
  passed yet. Next drive: pause/Start, drive, pause/Stop, verify files, then quit.
  Probe remains installed; do not relaunch automatically after the owner's stop.
  The older overnight handoff below predates these explicit capture requests.

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
