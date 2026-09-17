# RC4 crash feel failure — 2026-09-17 UTC

Owner saved a support file and reported no felt wheel response during further
head-on crashes at a setting of 20. Explicit follow-up: normal steering and
cornering FFB still worked. Crash feature acceptance therefore **failed**;
the successful offline gates and driver API returns do not override that result.

## Evidence

Original Desktop file: `art-of-sim-rally-support-20260916-230204.txt`, generated
2026-09-17 04:02:04 UTC. Build and mapped module hashes match the installed
`0.2.6-rc.4+91cedd8307010c796efe79590661368a52b56d27.clean`, toolkit
`local+c319b0258b11d1f07492a48dbc110d3e50c14dfd.clean`, native 0.7.0.
Saved crash enabled/19.52381 and landing enabled/20 remain intact. Native log
records initialization at 04:00 UTC and shutdown at 04:02:30 UTC. Game is closed.
No developer recording was started. Zero landing commands occurred in this run.

Three unique crash cues occur in the original UMM log. The support bundle also
contains repeated log tails; those are not additional collisions.

| Cue | Normal speed (m/s) | Requested magnitude | Play call (ms) | Managed stop after return (ms) | Steering at start |
|---|---:|---:|---:|---:|---:|
| 1 | 28.54 | .1952 | 2.48 | 127.96 | -.0071 |
| 2 | 33.83 | .1952 | 4.27 | 124.52 | .5688 |
| 3 | 23.72 | .1952 | 4.83 | 131.80 | -.3244 |

All three returned accepted and passed requested-shape parameter readback.
No rejection, overlap suppression or early managed stop is reported; all end
with `duration-elapsed`. These observations exclude premature managed stopping
for these cues, not an unobserved device-side interruption. Initial steering has
nominal headroom in every case, especially the first; saturation is not a
sufficient explanation for all three missing effects. No physical waveform or
motor torque was measured. Pit House gains/filters were not changed; local
saved metadata did not establish the active device configuration.

Raw support, UMM/native/Player logs, settings and build metadata are preserved
with verified SHA-256 in
[the evidence receipt](../../results/rc4-crash-feel-e0d2ce2a88564a6a91394e9c2bb8c3b0/receipt.json).
The exact RC4 attended checklist records `ffb-lifecycle` failed and leaves the
remaining incomplete cases pending. The automated artifact receipt is unchanged.

## Native review and next test

The toolkit task independently reviewed the frozen source and support file:

- Single-axis X with Cartesian direction zero is Microsoft's prescribed setup.
  It cannot be declared a zero-force code bug from inspection alone.
  [Effect direction](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417536(v=vs.85)).
- Ordinary steering updates target a separate constant-force effect. They do
  not call global stop or use SOLO; SDK semantics permit concurrent output.
  [Starting effects](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417955(v=vs.85)).
- Creation requests full effect gain. Per-play zero-initialized `dwGain` does
  not overwrite gain when `DIEP_GAIN` is absent. Parameter echo covers shape,
  but not axes, effect gain or actual playback status/physical torque.

No justified source-level root cause was found. A device-specific periodic
path, axis translation, filtering or mixing issue remains possible. Next is a
developer-only attended comparison outside the game: finite constant pulse,
current shaped sine alone, then the same sine alongside zero-steering updates.
Log axes/gain/status and compare before/after constant updates. Each output must
be explicitly started by the person at the wheel, finite in the driver, and
stopped on focus loss/close. Start low with matched peaks; do not increase gain
or change production routing merely because API calls succeeded.

RC4 remains installed unchanged, public stable remains 0.2.5, and KI-38 remains
open. A new waveform, alternate path or driver setting requires evidence from
that comparison and its own regression/attended checks.
