# RC8 first landing test — 2026-09-12 local

Owner tested the installed RC8 on the MOZA R12. **Clarification: the wheel FFB is
fine; the weak/buzzy landing report concerns the ButtKicker.** The initial review
mistakenly applied this feedback to wheel vibration. The owner wants a hard,
noticeable shaker thud even after maximizing their current ButtKicker settings.
They have not checked the amplifier's clipping indicator. The game was paused,
not exited. Logs were
preserved at approximately 00:14 UTC on 2026-09-13; no settings, wheel outputs or
installed files were changed during inspection.

## Evidence

- Build: `0.2.5-rc.8+b9598f5cb6b0a48965b37f4f8d27f350fd59be49.clean`.
- Local toolkit: `dd0ef20ad0cdaccc7a67f10a707dbd2a27a6efe9`, native 0.6.0.
- Process 31520 started 19:03:50 local. Game log identifies Car_M1 and
  Norway_Stage_6_Reverse_Dry_80s, with repeated runs.
- [Preserved logs and settings](../../results/landing-rc8-attended-00f123be246b482d820e3cfbf31507bd/analysis.json).
  Native ffb.log contains older sessions; `ffb-current-session.log` isolates the
  final InitDirectInput at 19:04:06.997 onward. Do not attribute historical force
  failures in the appended log to this launch.
- Eight diagnostic landing cues, all accepted by the driver:

| Cue | Air time (s) | Descent (m/s) | Requested magnitude |
|---|---:|---:|---:|
| 1 | 0.750 | 10.20 | 5.00% |
| 2 | 0.417 | 7.73 | 3.67% |
| 3 | 0.900 | 10.18 | 5.00% |
| 4 | 1.033 | 13.10 | 14.52% |
| 5 | 0.433 | 7.85 | 10.85% |
| 6 | 0.667 | 5.29 | 6.47% |
| 7 | 0.950 | 11.21 | 14.52% |
| 8 | 0.983 | 12.22 | 20.00% |

All requests use 25 Hz/120 ms. No landing rejection or constant-force
SetParameters failure appears in the current-session logs. Initialization and
StartEffect succeed. The autocenter property call returns `0x800700AA`; this
separate observation is retained, not evidence of a rejected sine effect.
The final native constant-force command is zero at 19:13:20.443.

The saved XML still contains LandingStrength=5, while live diagnostic requests
prove intermediate and maximum values were used. A paused, not-yet-exited session
is not evidence of a persistence defect. Existing steering is Strength=50,
Smoothing=0.2. The probe's read-only Status reports Idle, zero frames/force rows;
no new recording exists for these drives.

## Interpretation

The eight cues below are wheel-driver commands, not ButtKicker output. The mod's
Landing strength slider does not affect telemetry or SimHub shaker gain. Its
20% cap and 25 Hz/120 ms waveform therefore do not explain the reported weak
ButtKicker thud. Owner clarification withdraws the earlier recommendation to
reduce steering gain as the next test for this symptom.

Detection and nonzero driver acceptance have live evidence. They do not measure
physical torque, prove all actual jumps were detected, or establish satisfactory
feel. Maximum slider value means a 20% nominal-amplitude sine for three cycles,
not full wheel force or a large directional steering kick.

The first decimated steering trace after the final touchdown is `out=0.99`.
Strong simultaneous steering may mask or clip the additional vibration, but
these logs cannot prove its time-aligned physical waveform. Frequency, duration
and wheelbase filtering may also affect perception. Do not raise amplitude or
retune the established steering curve based only on an accepted return value.

The following toolkit review is retained as independent wheel-path evidence,
not a diagnosis of the ButtKicker report:
The toolkit task's read-only review found no ordinary steering-update path that
cancels the burst: constant-force updates target a separate effect, periodic
effect gain is full-scale, and no toolkit global gain attenuates the request.
Other landings have nearby small steering commands, so saturation cannot explain
every cue. The native watchdog/WM exit guards are not configured by this consumer;
managed reset/cleanup provides those lifecycle actions here.

Two observability gaps remain: reset/stop calls lack reason/timing records, and
the managed end time is set from before the Play call. A slow Play call could
shorten the burst when managed expiry stops it; no duration was measured in this
drive. Driver-adjusted successful parameters also are not read back. Add trigger
latency, immediate steering command and stop reason/elapsed diagnostics before
claiming waveform delivery or choosing a universal higher gain. The frozen
toolkit snapshot remains unchanged. Export support while paused before exit.

Wheel FFB has a positive owner report; this does not pass every landing/lifecycle
case. The ButtKicker thud remains a distinct FR-2 telemetry/haptic-output task.
Startup worked on this launch; repeated acquisition, focus, quit, physical gauge
clearing and the complete RC8 matrix remain pending. No public publication.

## ButtKicker follow-up

The installed SimHub reader/effect pipeline must be distinguished from wheel
effects. Local SimHub effect inspection confirms general Impacts uses velocity
change, Road impacts uses calibrated suspension velocity (with a roll fallback),
and Jump landing consumes SimHub's own front/rear landing values. RC8 sends
motion and suspension but no dedicated haptic landing event. Maximizing a gain
does not establish that a weak or brief source reaches full output.

SimHub's saved profile file still has its earlier global gain near 80%, general
Impacts at 99%, and Road impacts/Jump landing disabled. **The app is running:
these saved values cannot overrule the owner's report of current live changes.**
Do not overwrite that file while SimHub is running or infer remaining headroom
from its old values.

The next comparison should isolate a brief shaker pulse, lower its frequency
from the saved 44–50 Hz toward an experimental 25–35 Hz range, shape attack/decay,
and inspect input/output levels and amplifier clipping. These are tuning starting
points, not a guaranteed hardware response or authorization for unattended output.
If generic effects cannot produce a reliable cue, export a separate derived
landing event for a SimHub custom effect; retain physical telemetry units and
dashboard/motion behavior. Do not fake larger acceleration/suspension values.

Primary reference: [SimHub effect gain, frequency and gain modulation](https://github.com/SHWotever/SimHub/wiki/ShakeIt-V3-Effects-configuration).
Local effect inspection lives in `results/buttkicker-landing-research-20260913/`;
third-party decompiled sources remain local and uncommitted.
