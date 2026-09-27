# Experiment 004: Presentation and camera diagnostics

## Authority and scope — 2026-09-24

The user reported a successful first three-view test, bonnet-pitch tuning,
and visible tearing with VSync enabled. They authorized investigation and
testing while away, then explicitly switched their monitors back to three
separate displays. Keep that topology. This experiment may install the built
diagnostic adapter while the game is closed and launch through Steam to test
loading, actual VSync values, and the output-resolution guard. It must not
switch Surround back on, alter ArtOfSimRally files, or claim a new full-width
visual pass on separate displays.

## Prior-run evidence

The user's 0.2.0 status records `active`, three cameras and capabilities
`asymmetric-frustum`, `three-projections`, `surround-compositor`, with successful
frame timestamp `2026-09-24T05:18:13.8265557Z` and layout SHA-256
`cd4291834afb65ec45c7cef3e38b661e2377bb23e5de178c2ef6876c8818865d`.
This is stronger than the previous smoke record, which had no active frames.
It is still not a complete replay/photo/UI/performance qualification.

Read-only inspection of saved camera settings found bonnet pitch 9.218609
degrees and source-camera FOV 75 degrees. ArtOfSimRally applies pitch and
position in LateUpdate; the triple renderer then takes that pose and replaces
the projection using physical panel measurements (center roughly 33.6 degrees
vertical). Its source FOV control is consequently bypassed in true triples.
Preserve those user-tuned values. A separate duplicate pitch controller is not
needed to diagnose this interaction.

The three views render sequentially in the same Unity frame and are composited
only when all three have completed for that frame. The adapter does not change
`QualitySettings.vSyncCount` or `Application.targetFrameRate`. Inspection of the
installed game's settings methods shows its VSync preference is applied to
Unity's vSyncCount (default 1); unlimited frame cap maps to targetFrameRate480.
That cap is not evidence of unsynchronized output when VSync is active.

Primary Unity documentation:
- https://docs.unity3d.com/2019.4/Documentation/ScriptReference/QualitySettings-vSyncCount.html
- https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Application-targetFrameRate.html
- https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Camera.Render.html

## 0.2.1 changes

- Add a ten-second presentation snapshot at the existing per-user protocol
  directory as `render-diagnostics.json`, separate from canonical status schema.
- Record requested and actual Unity VSync, fullscreen mode, output resolution,
  refresh rate, target frame rate, observed update FPS, focus, camera pitch/FOV,
  projected center vertical FOV, composite count and CPU submission time.
- Show actual presentation values and the FOV/pitch distinction in UMM.
- Leave projection, camera settings, VSync, frame cap, and display topology as
  configured. These are diagnostics, not a claimed tearing fix.

## Procedure

1. Preserve prior-run status/log, installed adapter and settings before replacement.
2. Build Release and run all executable regressions.
3. Deploy only owned package binaries/metadata while game is closed; preserve
   Settings.xml, layout, and all other mods.
4. Confirm separate displays, launch via Steam, capture presentation diagnostics
   and verify graceful output mismatch at single-monitor resolution.
5. Exit normally and record pass/fail with paths and limitations. A Surround
   motion comparison still needs a later test with the user.

## Results — 2026-09-24, separate-display smoke test

- Release build: passed, zero warnings/errors; executable regressions: 44 passed.
- Installed 0.2.1 owned binaries and metadata only; deployed hashes matched build
  output. Preserved mod Settings.xml and staged desired-layout.json.
- Steam game 1.5.8b / Unity 2019.4.38f1 / UMM 0.33.0 loaded the adapter normally
  alongside the existing mods. Main menu and UMM options displayed; no adapter
  exceptions appeared in Player.log. This was not a driving test.
- Windows remained three independent 2560x1440 displays. Actual game output
  was 2560x1440, so `OUTPUT_RESOLUTION_MISMATCH`, degraded state, and zero active
  projection cameras were the expected safe fallback. No Surround switch occurred.
- Focused menu sample: ExclusiveFullScreen, Unity-reported refresh 59 Hz,
  requested/effective VSync both 1, approximately 59.95 update FPS, Direct3D11.
  `targetFrameRate=480` is ignored while VSync is nonzero; it is not evidence
  that the game was presenting at 480 FPS. Focus changes temporarily reported
  Windowed. No driver settings or game graphics preferences were changed.
- The main-menu camera reported FOV 40 and pitch 30.42 degrees. These are not
  the user's gameplay bonnet values. Projected FOV/composite metrics correctly
  remained empty/zero because the output guard prevented triple rendering.
- Closed the game normally after checking the menu. ArtOfSimRally Settings.xml
  SHA-256 before/after matched:
  `30AC77558E527F2920706E0418746BBE6AAD35FF7D9226CBD31F3417715A802C`.
  The three-view checkbox remains enabled, center preview and mismatch bypass
  remain disabled. A checkbox toggle lifecycle test was NOT verified this run.

Local evidence (ignored artifacts, not distributable game files):
`artifacts/experiment004-20260924/` contains the installed-before backup,
before/after Player logs and status, saved camera settings, and
`render-diagnostics-after.json`. The deployment is diagnostic instrumentation,
not a verified tearing fix. Next attended test: reproduce motion at 7680x1440,
inspect the new diagnostics, distinguish horizontal tearing inside a panel
from mismatch at panel joins, then compare presentation settings one at a time.
