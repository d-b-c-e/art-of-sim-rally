# Experiment 002: Center-Panel Projection Preview

## Question

Can the adapter safely own the gameplay camera's viewport and off-axis
projection for a complete stage, then restore stock rendering across camera and
mod lifecycle changes?

This is an attended experiment. The repository implementation has not been
installed or run in the game.

## Preconditions

- Build and tests pass from a clean checkout.
- NVIDIA Surround (or a borderless combined surface) is already configured at
  exactly the resolution in `desired-layout.json`.
- TAA is disabled. Use no AA, FXAA, or SMAA for this first experiment.
- Back up UMM configuration and preserve any existing mod installation.

The adapter never enables Surround, changes resolution, or activates displays.
Its preview toggle defaults off.

## Package

Copy only the five files listed in `adapter-manifest.json` from the Release
output into a new `Mods/DbceTripleScreenArtOfRally` directory while the game is
closed. Do not copy Newtonsoft.Json: the adapter intentionally uses the version
already loaded by the game.

## Procedure

1. Start with the preview disabled. Confirm stock menu and stage rendering and
   inspect `status.json` for `inactive` / `FEATURE_DISABLED`.
2. At the start line, enable **experimental center-panel projection preview**.
3. Confirm the rendered view occupies the center native-width viewport. Side
   output is intentionally unspecified in this milestone.
4. Confirm `status.json` becomes `active`, its layout hash matches the desired
   file, `activeCameraCount` is 1, and `activeCapabilities` contains only
   `asymmetric-frustum` after a successful frame.
5. Drive, pause/resume, change stock camera, finish, replay, and enter/leave
   photo mode. Record status transitions, UMM log, screenshots, and exceptions.
6. Disable the preview and then disable/unload the mod. Confirm the stock
   viewport and projection return immediately in each case.
7. Restart with TAA selected and confirm the adapter reports
   `degraded` / `TAA_UNSUPPORTED` without changing the camera.

## Pass Criteria

- No mutation occurs while disabled or rejected.
- The projection becomes active only on `Camera Main` under an enabled
  `CarCameras` gameplay rig and only after `OnPreCull` applies a frame.
- Disable, unload, camera replacement, and rejected configuration restore stock
  viewport/projection with no persistent game or display changes.
- Status never claims `multi-camera-render` or `render-target-compositor`.

## Next Decision

If lifecycle and restoration pass, implement three clone cameras rendering to
three temporary render textures, with post-processing and UI initially absent,
then composite them into the same wide output. If ownership is unstable, add
targeted camera-transition logging before creating any additional cameras.
