# Crash kick candidate — 2026-09-17 UTC

**Subsequent owner test failed:** RC4 produced three accepted shaped cues with
zero early managed stops, but no felt crash response; normal steering worked.
See [the support analysis and next diagnostic](2026-09-17-rc4-crash-feel.md).
The implementation/gate record below is retained; it is not physical acceptance.

## RC3 attended result

Owner drove the exact installed RC3 on 2026-09-17 03:29–03:33 UTC and reported
**no distinct crash effect**, then questioned the vibration waveform. UMM records
13 accepted crash commands, six at full event intensity and magnitude 0.1952;
saved CrashStrength is 19.52381, crash enabled, landing enabled/20. Zero landing
commands occur in this drive. This is failed crash-feel acceptance, not a passed
hardware gate. Pit House game FFB gain scales all game effects; the mod's steering
slider is separate and does not explain the report.

Raw logs/settings/build metadata and their SHA-256 receipts are preserved at
`E:/Source/art-of-sim-rally/results/rc3-crash-feel-1e4f242242aa4e4c972525d99c63ae89`.
Current-session native startup is version 601; no current force-trace flood or
delivery rejection was found. Trace acceptance does not measure actuator waveform
or elapsed playback. Nearby steering is low for several accepted collisions,
so constant steering saturation does not explain every event.

## Implementation

Crashes formerly reused landing's 25 Hz/120 ms sine (three rapid cycles). The new
crash request uses 6.25 Hz, phase 90 degrees, and an amplitude envelope fading
from the same peak to zero over the entire 120 ms. Nominal shape is an immediate
positive kick, one smaller negative rebound, then zero at three-quarters cycle.
It is a generic impact cue, not a model of steering-column collision torque.

Native shaping lives upstream in dbce-wheel-mod-toolkit, with an additive API.
Candidate pin is `local+c319b0258b11d1f07492a48dbc110d3e50c14dfd.clean`, package
0.14.0/native 0.7.0, 42 exports. The local archive SHA-256 is
`B1F8624EDBD06A3DC4F73397D5904229A2DFA8F83A7774E1753E500A2CFD6AD5`.
Upstream full local gates passed; smoke binaries were compiled, not launched.
No physical waveform was tested. Its ordinary force-logging policy remains opt-in.
The driver must echo the requested parameters before the new effect starts.
Rejected shaping latches crashes unavailable until explicitly toggled off/on
while paused; it preserves the shared slot for subsequent legacy landings.
No unshaped fallback is advertised as a successful kick. Shared ownership still
uses strongest wins/crash wins ties, without amplitude summing or queued cues.

Landing waveform, detector timing/thresholds, steering arithmetic, settings
values, physics, telemetry and SimHub profiles remain unchanged. The owner has
explicitly deferred motion amplification. Existing 0–40% amplitude limits and
defaults (5; crashes off for new settings) remain. Saved crash opt-in/19.52381
must survive deployment, rather than restoring the earlier RC3 off/5 state.

Production measures call latency with Stopwatch, independently of Unity's cached
frame time. Managed expiry starts after native playback returns; the driver
still enforces finite duration. Mixer overlap uses the same lifetime as output.
Support retains per-effect last delivery and early-stop counts; detail logs
record start/reject, stop reason and elapsed time since return with steering
output. These measure software calls, not motor torque or physical duration.

## Validation and remaining work

Consumer tests cover matched peak amplitude, separate waveform selection,
80/250 ms fake driver latency, post-call lifetime and overlap, early-stop reasons,
shape rejection/landing recovery, explicit paused retry, invalid clocks, failure
cleanup and allocation-free steady ticking. Existing detector/corpus, lifecycle,
Mono loading, package and installer gates are required for the new exact RC.
Upstream fake-COM tests cover native parameters and legacy clearing in the same
slot. No unattended hardware output or automatic game drive is performed.

Current build/deployment identity and completed gates belong in
[LOCAL-DEPLOYMENT](../LOCAL-DEPLOYMENT.md). The candidate has **no attended feel
acceptance**. Test with crash off/on, a front hit and a glancing hit, a landing,
pause/focus loss and quit. Compare at fixed wheelbase/mod gains, starting low
because onset differs. Save support while paused. The prior recording need not
be repeated solely to validate this waveform comparison.

RC4 completed all 16 local gates from clean source
`91cedd8307010c796efe79590661368a52b56d27`, including two real corpus cases and
9,342 landing/crash assertions. Exact ZIP SHA-256:
`39643F929A680027937B195F2B18623DC69C289229A65C1AE27AE7AD0078F2BA`.
Installed 2026-09-17 03:57 UTC with prior RC3 backed up; payload and preserved-file
hashes verified. Crash on/19.52381 and landing on/20 survive. No game launch or
recording, no wheel output, no SimHub change. Consumer source and toolkit shaping
remain separate repositories, with the exact toolkit artifacts vendored here.

Public stable remains 0.2.5. Public toolkit repin and attended checks remain
prerequisites to publication; no release is authorized by this implementation.
