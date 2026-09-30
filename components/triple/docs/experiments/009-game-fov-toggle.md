# Experiment 009: follow the game's FOV

**Date:** 2026-09-27. **Candidate:** 0.3.12 at source revision `0c5a945`.
**Game:** Steam 1.5.8b, Unity Mod Manager 0.27.0. **Output:** one 7680×1440
NVIDIA Surround display. Physical layout: three 2560×1440 panels, 70° side
angles, 660 mm eye distance, 8 mm bezel gap. No game or UMM binary is stored in
this repository.

The new **Override field of view** toggle defaults on to preserve existing
users' saved slider behavior. Turning it off reads the source game's live camera
FOV and derives one shared virtual eye distance for all three off-axis views.
The previously saved slider value remains available when override is enabled
again. The local candidate was built with zero warnings; 66 executable geometry
assertions and 26 isolated installer checks passed.

For this attended run, Windows was already in Surround, while the saved layout
still selected separate displays. With the game closed, the canonical and staged
desired layouts were backed up under ignored `artifacts/` and temporarily set to
`nvidia-surround` at 7680×1440. The 70° angles and 660 mm eye distance were
preserved. The 0.3.12 candidate was installed after backing up the existing
mod folder; the install preserved the player's `Settings.xml` and layouts.

The user entered a stage and turned override **off**. Runtime diagnostics
reported active three-view rendering, source camera FOV **75°**, center
projection FOV **75°**, shared view-width scale **2.74045**, thousands of
successful wide frames, and no render error. Earlier samples followed game FOV
values of 36° and 44° as the camera state changed. The user reported that all
three views rendered, seams aligned, and the FOV felt like the game's default.
An ignored full-resolution screen capture and diagnostics snapshot are in
`artifacts/fov-0312-runtime-game-follow/`.

The user also reported hard stutters and performance problems while substantial
other work was running in the background. One diagnostic sample averaged about
58 updates/s, which does not measure frame-time spikes or identify their cause.
Treat performance in this run as **inconclusive**, not a mod regression or pass.
Surround tearing remains an independently open issue from earlier drives.

The user exited before checking the transition back to **Override field of
view** on and then off again. The saved `Settings.xml` now has override off and
retains the prior slider value (`1.33168638`). That transition and an isolated
performance comparison are pending. The installed candidate and matching
Surround layout remain in place for the next attended check; 0.3.12 has not
been published as a GitHub release.

## Offline follow-through — 2026-09-27

The source now routes both output modes through one testable FOV selection
function. An offline off → on → off sequence verifies that the saved slider
scale is preserved, the live game value is restored, and both choices keep the
left/center/right hinge rays continuous at the current 70° angles. A separate
eligibility check verifies that an armed camera remains active when the game's
driving component is disabled for the Cinemachine finish handoff, while a new
camera, menu camera, or driver for the wrong output mode is rejected. Release
build: zero warnings; 83 executable assertions. These are offline checks; the
user's pending physical-screen toggle test is still pending.

The captured 0.3.12 diagnostic is one roughly 10-second update-rate sample:
59.78 updates/s at a reported 59 Hz with VSync requested/effective count 1.
It shows three recent camera frames and a 2.01 ms spread between their camera
callbacks. It does **not** contain per-frame GPU time, present intervals, or
scanout measurements. Thus it cannot identify the cause of hard stutters or
disprove the previously observed Surround tearing. Three rendered views with
side post-processing and broader vegetation visibility can increase workload;
background contention was also reported. Neither is isolated by this sample.
The screenshot is a single composed frame and cannot establish tear-free
physical scanout.

That snapshot reports source volumetric components enabled and zero side
volumetric copies. This is worth checking in a matching scene but is not yet a
confirmed regression: the source diagnostic only checks component `enabled`,
whereas the side-copy gate also requires `HxVolumetricCamera.volumetricEnabled`.
No captured field records that gate's value.

### Exact next attended steps

1. With the current 0.3.12 candidate and 70°/660 mm Surround layout, use the
   same stage, car, weather, and camera view. Note background workloads. Start
   with **Override field of view off**. Check three live views, both seams, and
   source/center FOV agreement in `render-diagnostics.json`.
2. Turn **Override field of view on** in UMM. The previous slider value should
   remain (about `1.33168638`, roughly 44° for this layout). Drive briefly and
   check that all three views narrow together, seams remain aligned, and no
   side content disappears. Do not move the slider yet.
3. Turn override **off** again. Check that the game's live FOV returns on all
   three views and the seams still align. Reopen UMM once to confirm the slider
   value was retained, then save and exit normally.
4. Cross a finish line with triple rendering active. Confirm both side views
   follow the center through the cinematic camera handoff rather than freezing.
5. For the stutter question, repeat a short, comparable drive with background
   work quiet, recording per-frame timing for stock wide rendering and each
   triple-screen FOV state. Use frame-time spikes or 99th-percentile times, not
   only 10-second update FPS. For tearing, compare physical scanout in stock and
   modded Surround under the same resolution, refresh, and VSync settings; a
   desktop screenshot alone is insufficient.
