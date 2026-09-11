# RC5 bug follow-up — 2026-09-11 UTC

Stable remains 0.2.4. RC5 failed the attended FFB-startup and gauge-stop cases;
its manual result is preserved. This work prepares a replacement candidate,
not release acceptance. No game was driven or force sent to hardware here.

## Implemented fixes

- **KI-28, startup FFB:** remove acquisition from UMM load. The idle watchdog
  waits for a valid foreground window owned by the game, stable for 500 ms.
  Five attempts maximum, separated by five seconds; no acquisition while driving
  or assigning an axis. Wheel readers and the auxiliary shifter close before
  acquisition and reopen afterward using their saved identities. Wheel selection
  or re-enabling FFB starts another retry budget. Native/curve binaries unchanged.
- **Menu Quit:** shipping `GameExit` releases outputs before the game's
  `Process.Kill`, including when the optional developer probe is absent. Actual
  Harmony attachment is checked alongside the probe's cancellation/save prefix.
- **KI-30, recorder shutdown:** probe 0.2.5.3 explicitly stops its control server
  after a successful shutdown save. Cancel synchronous native pipe IO on its
  worker thread; the worker disposes the stream. Wake pending command waits.
  Failed capture saves retain the control server for an explicit retry.
- **KI-31, replay:** separate force arithmetic, which retains its 1e-6 tolerance,
  from exact conversion of the recorded float to the native integer. Legacy
  versus toolkit integer output must still agree exactly on the offline runtime.
  New recordings identify their conversion contract; unknown contracts fail.
  A one-unit edit to a recorded command still fails. No force tune changed.

## Runtime evidence

The new `Run-UnityMono.py` supervisor embeds the installed game's Mono DLL in a
separate, time-bounded test process. It does not start Unity or DirectInput.

- The old pipe implementation fails the listening-shutdown assertion under
  actual Mono, then hangs in runtime cleanup until the supervisor's 30-second
  timeout. This reproduces an exit-hang mechanism, but does not prove it was the
  only cause of the owner's Alt+F4 report.
- The corrected implementation exits after listening, connected-without-command,
  and queued-command waits on both CLR and Unity Mono.
- Mono converts float `0.41239998 * 10000` to **4123**; CLR/.NET's rounded
  single-precision product yields **4124**. Mono matches **all 8,974 commands** in
  the preserved jump capture (8,979 assertions including shutdown/conversion).
- Strict replay now passes **136,575 assertions**, explicitly reporting the one
  runtime rounding difference. Its original CSV/manifest bytes were not edited.
- The capture is promoted to
  `results/regression-corpus/index.json`, case
  `norway-stage5-reverse-m1-jump-20260911`. RC runs should supply this corpus.
  This is recorded arithmetic/signal evidence, not hardware playback or RC6 sign-off.

Evidence: `results/bug-investigation-20260911/`, including `baseline-pipe.log`,
`mono-compatibility.json`, `replay-real.json` and `corpus-promotion.log`.

## KI-29: digital speedometer and tachometer

The owner clarified both digital gauges froze on finishing a race. Installed
SimHub receives the mod on UDP 8000. Both connected round displays select
**DSS TFT ROUND Guage**, with **PROCOMP Speed** and **PROCOMP RPM** screens.
Their dial bindings read `[SpeedLocal]` and `[Rpms]` directly; both screens
remain selected in idle mode. There was no explicit stopped-game fallback.

Local inspection of SimHub's installed Forza reader shows `IsRaceOn=0` skips
speed/RPM parsing, marks the game disconnected and clears the raw game sample.
Thus repeated zero race-off packets cannot force that parser to parse zero speed.
This supports fixing the display's idle handling rather than inventing a brief
live-race packet with zero motion. It does not establish the entire rendering
chain or physical result without a display retest.

The selected local template's two needle bindings and two speed text bindings
now return zero when `DataCorePlugin.GameRunning` is false, preserving live
values/local units. The original template and receipt are backed up under
`results/bug-investigation-20260911/gauge-backup/`; no vendor dashboard or art is
committed. Sixteen cases pass using the installed NCalc engine on CLR, including
stopped/null/retained values and live speed/RPM. Reload the dashboard in SimHub
before testing. Physical clearing remains pending. The repair helper takes
explicit screen IDs and refuses unexpected formats or repeated modification.

Production park-packet tests now check every data byte is zero except timestamp
and sentinel, including speed. The packet layout and race-off policy are unchanged.

## Landing feedback

The first capture has exactly one landing matching the owner's note: 1.083 s
airborne, rear-right contact first, then front contact one physics step later.
Steering command resumes at **67.53%** after about **16.7 ms**. It is not a
landing with no steering force. There is still no dedicated landing vibration.

`EffectsLab --capture` validates the recording before studying 12 combinations of
steering gain, independent event gain and both hypothetical torque directions.
408 checks pass, with zero clipping in this trace. Effects disabled reproduce the
scaled steering exactly. No candidate output is sent to a wheel.

The pinned toolkit's 100 ms envelope peaks after 5 ms, with its positive lobe
ending at 20 ms. At this trace's roughly 60 Hz update rate, the sampled peak is
only **0.02101 for a 0.1 event budget**. This is a sampling observation, not a
hardware measurement or a defect in the currently shipped steering path.
An independently controllable effect is feasible, but its envelope must suit the
consumer update rate. Do not simply multiply gain to compensate for a missed peak.
Vertical acceleration cannot determine a left/right steering-torque direction;
periodic vibration or a separately versioned, sample-aware envelope needs an
attended study. Keep any shared primitive change upstream and preserve the
published envelope contract. Data: `landing-study/report.json` and CSV alongside.

## GitHub and next tests

GitHub read-only audit: one open issue, **#1**, with one reporter comment saying
unplugging a USB PS5 controller resolved the camera problem. No PRs or new issue
reports appeared. No maintainer response exists. An acknowledgment/retest draft
is in USER-FEEDBACK.md; nothing was posted or closed.

Next attended checks: repeated focused/alt-tabbed launches with working FFB;
finish-line speed/RPM zeroing; normal Quit with and without the probe; Alt+F4
without a hang; wheel/shifter inputs after recovery; fresh capture/Stop. Existing
camera, TSS/Fanatec and motion/shaker matrix items remain pending. Do not infer
them from the offline gates or clear RC5's failed receipt.
