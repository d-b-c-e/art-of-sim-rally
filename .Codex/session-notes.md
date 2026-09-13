# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

## Owner correction — ButtKicker thud, not weak wheel FFB

- Owner clarified wheel FFB is fine. The light/buzzy landing complaint applies
  to the ButtKicker, where they want a hard thud even after maximizing settings.
  They have not checked the amplifier CLIP light. This supersedes the wheel
  tuning interpretation below. Do not reduce steering to investigate this symptom.
- The wheel landing slider does not change telemetry or shaker gain. Current
  eight accepted wheel cues are not evidence of shaker output. Corrected review,
  known issues and roadmap preserve that distinction; full RC gate still pending.
- Local SimHub effect inspection: general Impacts = velocity change; Road
  impacts = calibrated suspension velocity (roll fallback); Jump landing =
  SimHub's derived front/rear landing values. No dedicated mod haptic event is
  exported. See results/buttkicker-landing-research-20260913/ and the corrected
  docs/reviews/2026-09-12-landing-wheel-test.md.
- Saved SimHub JSON still shows old values; app is running, so do not infer live
  settings or overwrite the owner's current changes. No profile/output changes
  made. Game still running; no deployment or hardware tests performed by agent.
- Direction: isolate a short shaker pulse, experiment with 25–35 Hz and fast
  attack/decay, observe input/output/clipping. If needed add a separate haptic
  landing signal/custom effect, preserving honest acceleration/suspension data.
  No universal extra-gain promise; amplifier/seat response remains unmeasured.

## RC8 paused log inspection — 2026-09-12 local / 2026-09-13 UTC

- Owner is at the wheel; game remains paused, PID31520. No deployment/relaunch
  or output test. They report landing vibration subtle even at maximum.
- Eight live landing cues, all driver-accepted, culminating in 0.20 magnitude,
  25 Hz/120 ms after .983 s airborne / 12.22 m/s descent. The following decimated
  steering trace is .99; masking/clipping is plausible but not physically proven.
- Preserve evidence: results/landing-rc8-attended-00f123be246b482d820e3cfbf31507bd/.
  Native current-session subset avoids many historical failures in appended log.
  Startup/start succeeded this launch; no current SetParameters failures.
  Autocenter0x800700AA recorded separately. Last constant-force request is zero.
- Probe Status: Idle, zero frames/forces. No recording exists for this test.
  Settings.xml still says landing5, but live requests confirm changed strength;
  do not diagnose persistence while user is paused before exit.
- Read docs/reviews/2026-09-12-landing-wheel-test.md. Eight accepted requests do
  not pass feel, all-jump detection or complete RC matrix. Toolkit read-only
  review found no routine steering update cancelling the burst. Stop timing,
  Play latency and driver-adjusted parameters are not observed; add diagnostics
  before claiming waveform delivery. Keep the frozen snapshot and installed RC8.
- Useful next comparison: unchanged landing strength, temporarily lighter
  steering, wheel evaluated separately from shaker; then bounded frequency/
  duration variant only if needed. Support export while paused is useful.

## Landing candidate installed — 2026-09-12 23:54 UTC

- Owner requested landing-effect implementation/testing and asked about remaining
  roadmap. They are available to test today. No attended result has arrived yet.
- RC8 is installed from clean source `b9598f5cb6b0a48965b37f4f8d27f350fd59be49`.
  All 16 local gates pass, including strict steering replay of the original drive
  and 9,044 landing assertions. Exactly one landing at row 4230 matches the owner.
  ZIP SHA-256: `90F3F0BD942FD6F446702BAF185842196090B54882D821AE737AE1046CDAFADB`.
- Gate: results/rc-0.2.5-rc.8-669ae7f5aae140ca9e793fd9ad0b1781/automated.json.
  Receipt/RC7 backup: results/landing-rc8-install-a1e5dbdfefd9432d98ebeea43f31b65d/receipt.json.
  All payloads match; Settings.xml and separate probe 0.2.5.3 are unchanged.
  Stream Deck Steam550320 still launches this installation. Game closed;
  nothing recording, no nonzero wheel force applied by this work.
- Landing vibration defaults OFF, independent strength 5 (cap 20), 25 Hz sine for
  120 ms. Finite native duration, idle-only setup, focus/pause/quit release,
  jitter/teleport/restart/stall rejection and counters are implemented/tested.
  Old captures check steering only; periodic delivery is explicitly unobserved.
- Toolkit task prepared isolated clean commit
  `dd0ef20ad0cdaccc7a67f10a707dbd2a27a6efe9` in
  E:/Source/dbce-wheel-mod-toolkit-bursts, branch codex/finite-periodic-bursts.
  Vendored pin is local+that commit.clean, managed 0.13.0/native 0.6.0. Upstream
  local gates and physical R12 zero-output ABI smoke pass on x86/x64. No toolkit
  publication/push occurred. Final mod packaging rejects local pins (verified);
  official toolkit release/repin is needed before public mod publication.
- Next: owner A/B same jump off/on at strength 5, diagnostic logging on, support
  export while paused. Assess a single brief cue, no false triggers or steering
  changes; check FFB startup/recovery, gauges at finish/quit, font scaling and
  exit with/without probe. Full RC8 manual.json remains pending.
- Remaining roadmap: RWD snapback/slip study; road/crash effects; TSS/Fanatec and
  T300 setup; PS5/Nexus camera compatibility; measured hitch investigation;
  Nexus distribution and richer developer captures. See docs/ROADMAP.md and
  docs/LANDING-EFFECTS.md. Stable remains 0.2.4; no public support reply sent.
- This entry supersedes historical installed versions/defect status below.
  KI-31 is resolved by the verified Mono conversion contract, not widened replay
  tolerance. The known drive is now in results/regression-corpus/index.json.

## Retry saved and analysed — 2026-09-10 local / 2026-09-11 UTC

- **Recording stopped; game closed.** Explicit STOP saved 11,706 frame rows and
  8,974 force/motion rows; all CSV hashes verified and independent copy preserved.
  Mod RC5/probe 0.2.5.2 unchanged. FFB initialized this launch; no force rejections.
- Original capture: C:/Users/antho/AppData/Local/ArtOfSimRally/dev-captures/
  20260911-024022-59d5395fc08e4f2cb2012b4f4c729800.
- Evidence: results/attended-rc5-retry-2b067b8ccde040cd87f6274d4b0ea19b/;
  copy in capture-diagnostic-only, preservation.json, diagnostic-audit.json,
  landing-detail.json, landing-signals.png/SVG and analysis scripts.
- Norway_Stage_5_Reverse_Dry_80s / Car_M1. Owner said one jump; one detected:
  70.5s into drive, 1.083s airborne, rear-right first contact, FFB zero then
  front contact/67.53% command at next 16.7ms sample. Strong vertical/compression
  response; not a calibrated impulse. 8973 driving frames, p95 18.72ms, max27.12ms,
  no100ms+ intervals. Full hardware matrix/overhead still pending.
- KI-31 blocks strict replay/corpus: one integer mismatch at row5892, expected4124
  observed4123; max recorded/replayed float delta1.1920929e-7; frozen legacy and
  toolkit agree exactly offline. Double-product truncation of recorded float
  matches every observed integer, single-product rounds up once. Preserve evidence,
  confirm Mono conversion contract before changing replay; no tolerance widened.
- Read docs/reviews/2026-09-10-first-jump-capture.md. A different stage is optional;
  told owner this trace suffices for initial analysis. No new effect/tune shipped.
- Prior RC5 FFB/telemetry failed attended cases and Alt+F4 hang remain open.
  Do not mark them cleared by a good capture. Earlier notes describe prior attempts.

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
