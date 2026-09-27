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
