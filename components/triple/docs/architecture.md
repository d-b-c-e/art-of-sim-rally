# Architecture

## Boundaries

1. `Dbce.TripleScreen.Core` turns physical panel measurements into three display planes, camera rotations, and off-axis projection matrices. It has no Unity, game, Windows, UMM, or optimizer dependency.
2. `Dbce.TripleScreen.Protocol` validates an optional schema-versioned layout and serializes runtime status. It reads Json.NET from the installed game at build/runtime rather than redistributing it.
3. `ArtOfRally.TripleScreen.Mod` owns UMM settings, local measurement setup, game-camera lifecycle, Unity display targets, image effects, vegetation visibility, and status diagnostics.

The mod is standalone. Triple Screen Optimizer may write a `desired-layout.json` measurement source, but neither the optimizer app nor its private toolkit is required to build, install, or configure the mod. The contract continues to use millimetres and degrees.

## Runtime views

The game's `Camera Main` is the center view and remains tagged as the main camera. The mod adds untagged left/right cameras as children. Every frame it copies the source camera's transform and camera settings, then applies three physical projections and side rotations. A late pre-cull hook reapplies each side matrix after the game's post-processing callbacks. The side cameras use physical camera properties so the post-processing layer does not reset their matrices.

```text
CarCameras or Cinemachine on Stage Camera
                  |
           original Camera Main
           /       |       \
      left copy  center  right copy
         off-axis projection each
                  |
    one wide output OR three Unity displays
```

- **Single wide display:** center camera renders into the middle third of the 3-panel-wide output; side cameras render into the left and right thirds. This replaced the earlier render-texture compositor, which lost vegetation and camera lighting effects. The viewports use the same projections as separate displays.
- **Three separate displays:** center uses Unity display 0, while side cameras target two selected secondary Unity displays. Display activation lasts until process exit.
- **Off:** the mod releases camera state and leaves stock rendering. An existing Unity secondary window can retain its last image until process exit; restart after changing modes.

Each side camera gets an isolated PostProcessVolume copied from the center's active effects except MotionBlur, plus the center's enabled occlusion and volumetric effects. Copies must keep camera-specific callbacks independent. The game draws vegetation from its source camera; the mod widens that camera's vegetation culling to 360° while three views are active and restores the original mode on release. Per-camera direct vegetation registration caused side vegetation to disappear in the 0.3.7 attended test.

At the finish line, the game disables `CarCameras` and lets `CinemachineBrain` move the same `Camera Main`. An already armed driver stays attached through that handoff. The mod does not attach a three-view driver to an unrelated menu camera before gameplay.

## Configuration and safety

The UMM panel presents Off, Single wide display, Three separate displays, and a field-of-view slider. Advanced setup accepts a local panel size, eye distance, side angles, and separate-display indices. Example measurements remain inactive until the user accepts them. An imported optimizer layout is optional; the effective layout is validated and hashed without mutating the import.

The wide mode requires exactly three native panel widths and one native height. The separate mode requires the center as Windows primary, a native-size game output, matching secondary displays, and distinct Unity indices. The adapter never calls `Screen.SetResolution` or changes NVIDIA/Windows display topology. It writes runtime status and diagnostic JSON atomically under `%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\`.

## Test status and risks

Attended checks on game build 1.5.8b found aligned seams, FOV continuity, stable vegetation, closer lighting, and no visible tearing in separate-display mode. The 0.3.10 finish-camera handoff appeared fixed. The 0.3.11 wide viewport path improved vegetation and lighting. **Surround tearing remains open** despite Unity VSync 1; update FPS and camera callbacks are not scanout timing. A minor vegetation fringe reported in separate mode has not been isolated with a same-scene comparison. See [known issues](KNOWN-ISSUES.md) and [experiment 008](experiments/008-attended-separate-display-check.md).

Three cameras can cost substantially more than one. Screen-space effects can disagree across seams, and global shader state may be overwritten between cameras. Mixed monitor sizes, DPI, all menus, replay, photo mode, and longer performance runs need further attended testing. Offline geometry tests verify projection and viewport math but cannot certify those visual/lifecycle gates.
