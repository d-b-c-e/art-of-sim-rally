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

Recording is active as of this handoff. Query STATUS before further commands;
use the current game PID, not a cached one. Once the owner finishes, save while
paused, validate/replay the capture, correlate event times and preserve suitable
evidence in the regression corpus. Record actual user results in RC5's attended
checklist without filling untested cases. Remove the probe with the game closed
for a comparison drive. Signal freshness, landing usefulness, physical force feel
and instrumentation overhead remain unassessed until then.
