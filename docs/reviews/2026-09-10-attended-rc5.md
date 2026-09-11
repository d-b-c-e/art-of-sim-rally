# Attended RC5 capture — 2026-09-10 local time

The owner requested a short wheel test and a recorded session. Shipping candidate
remains `0.2.5-rc.5+43e0b4a2978121e712d20c8f170567b587554bdf.clean`, with
the existing tune and toolkit. No release or attended pass is implied by setup.

## Setup and recorder correction

The separate probe 0.2.5 was installed with the game closed. Production mod files
and settings were hashed before/after and unchanged. The game loaded both mods,
but no control pipe existed and STATUS timed out; no recording started in that run.
Inspection of the installed Mono assemblies confirmed the unsupported identity API
and asynchronous pipe behavior described in [KI-26](../KNOWN-ISSUES.md).

The owner quit normally. Developer probe 0.2.5.1 was installed at
2026-09-11 02:08:31 UTC, preserving a backup of the old probe and a fresh hash
baseline for all production mod files and settings. Those files were unchanged
by replacement. Steam app 550320 relaunched the game; Unity loaded the probe,
STATUS returned Idle, START returned Recording, and subsequent STATUS reported
2,515 frames / 607 forces with `incomplete=False`. This establishes functioning
runtime control and observation, not saved-capture validity or hardware acceptance.

- Installed probe DLL SHA-256:
  `D0D16AAF93C202A63289E30977148805556BAF753B427D420DBFAAFAD57E55B3`.
- Probe Release build with warnings as errors: passed.
- Actual net48/Harmony/control tests: 16 assertions passed, including current-user
  SID equivalence and synchronous startup-error propagation.
- Recorder .NET 8 buffer/IPC/metadata tests: 47 assertions passed.
- [Initial setup receipt](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/setup.json),
  [fixed probe receipt](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/probe-fix-install.json),
  [runtime recording confirmation](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/recording-started.json).

Local evidence also contains production-file baselines, startup Player.log copies,
the old probe backup and synthetic regression output. Decompiled game files are
local ignored diagnostic evidence, never committed or packaged.

## Drive and remaining work

The short plan keeps the tune unchanged: jumps, road bumps, ordinary corners and
a controlled slide; pedals/handbrake/shifting; stock/bonnet/bumper cameras,
finish/replay; pause and alt-tab force release/recovery. The owner was asked to
pause and report completion before STOP writes the capture.

The owner reported a couple of good jumps and used the normal Quit option.
No drive capture was saved. The game's `ExitGame.Exit` path forcibly kills its
process, bypassing the shutdown observer (KI-27); the first run has no Unity quit
callbacks in its log. Preserve this as a failed capture, not successful replay.

Retained evidence from that run:

- Stage `Norway_Stage_4_Reverse_Dry_80s`, car `Car_M1` from Player.log.
- 12,206 foreground-driving intervals in the retained diagnostic summary;
  maximum 33.95 ms, zero 100 ms+ intervals. Loading/pause/focus transitions excluded.
- 11,731 native force commands and 1,017 sampled FFB trace lines. These contain
  steering forces, not the lost contact/suspension/motion stream. They cannot
  identify or measure the jump landings reliably.
- [First-drive summary](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/drive-summary.json).

## Quit-save correction and abandoned retry

Probe **0.2.5.2** intercepts `ExitGame.Exit` when a capture is pending, invokes
normal output shutdown and permits the process kill only after the save succeeds.
A release/save failure retains the buffers and cancels that quit for a Stop retry.
Build passed, with 58 recorder assertions and 17 actual net48 hook assertions.
This is a developer-tool change; shipping RC5 and all settings were preserved.

- [Install receipt](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/quit-fix-install.json),
  probe SHA-256 `CD47CCF20A5007FA9C73658A2BEF9CA977F771A15FA48BCD4A4B82A492138754`.
- Explicit Start/Stop saved a menu-only preflight, all CSV hashes verified:
  [save preflight](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/save-preflight.json).
- The owner abandoned the retry and requested recorder stop. The game exited
  during the status request; this time its log shows Unity quit callbacks and a
  successful probe save. The capture has 2,169 frames and **zero force rows**;
  all CSV hashes match. It is excluded from regression and acceptance evidence:
  [abandoned-run receipt](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/second-run-abandoned.json).

**Nothing is recording; the game is closed.** The probe remains installed.
Explicit save and ordinary Unity shutdown saving have now run, but this does not
verify the distinct process-kill menu hook. No usable jump capture or corpus case
was retained. Next attended attempt: Start while paused, drive, pause and leave
the game open, Stop, verify files, then quit. Signal freshness, landing analysis,
physical feel and instrumentation overhead remain pending; no release cases were
marked passed based on these attempts.

## Owner follow-up: RC5 has attended failures

The owner clarified that the retry was abandoned because **FFB was absent**, and
Alt+F4 then hung. The original run also left gauges stuck at finish-line speed at
stage end and after quitting. These are KI-28, KI-30 and KI-29 respectively.

The retry's Player.log reports FFB acquisition failure before the probe loads;
native logging confirms failed exclusive acquisition and an invalid window on
reacquire. All four direct-input readers opened, while FFB stayed unavailable.
Settings still enable FFB at strength 50/smoothing 0.2. No deliberate force change
was made. This explains the zero force rows; it is not just an unsuitable drive.

SimHub's retained log registers the original race ending at 21:12:50.865 local,
while the owner reports gauges did not clear. No packet/display correlation was
captured, so the exact zeroing defect remains open. The Alt+F4 log has a successful
save and native reader closure, but those do not disprove the reported hang.

RC5's exact `manual.json` now marks **ffb-lifecycle** and **telemetry** failed,
with hashed evidence. Other cases remain pending, including the full stutter
matrix. The abandoned capture stays out of the corpus, retained only as defect
evidence. [Owner follow-up](../../results/attended-rc5-bcbd05cd936c4291be58c6e22a3721e0/owner-followup.json).
No production fix for KI-28/29 or established diagnosis for KI-30 has shipped.
