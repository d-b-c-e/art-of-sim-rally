# Experiment 007: Three independent Unity displays

## Scope and safety — 2026-09-25

The user requested an experiment that renders one physically projected camera
to each of three separate monitors, without NVIDIA Surround. This is a new
render path, **not** a replacement for the current working Surround profile.
No topology change, game launch, or switch to the new mode is part of the
offline implementation step.

The prototype must require all of the following before it calls Unity's
`Display.Activate()` or changes the camera:

1. A valid optimizer layout with `output.mode = separate-displays`.
2. An independently saved, default-off UMM opt-in.
3. The center monitor as the Windows primary / Unity display 0, with game
   output exactly one panel's native resolution.
4. At least three Unity displays, with two distinct secondary indices and
   matching native panel dimensions.
5. An active gameplay stage camera. Menus and cinematics remain stock.

Unity 2019.4 states that activated secondary displays cannot be deactivated
until the process exits. Consequently toggling the experiment off releases
mod cameras but a restart is required to return to one-display presentation.
Exit the game before changing monitor topology. Do not modify ArtOfSimRally.

## Offline gates

- Build Release and run core/protocol regressions.
- Review the guarded activation path and verify that the installed
  `Settings.xml` leaves the new option disabled.
- If deploying the binary, preserve the installed settings and desired layout.
  Do not automatically rewrite the optimizer's current Surround contract.

## Later attended runtime gate

After the user switches to three separate 2560x1440 monitors and closes the
game, explicitly stage a `separate-displays` contract. Start with the center
monitor primary and each monitor at its normal higher refresh rate. Launch
once with the experiment enabled; verify left/center/right mapping, menu
behavior, usable controls, post-processing, FPS/present cadence and a moving
camera tear test. Record the exact Windows display arrangement, driver and
game versions, logs, and a phone video. A static screenshot or Unity VSync
request is not evidence of tear-free scanout. Stop on corruption or a hang;
return to the backed-up Surround contract only after the game exits.

## Offline result — 2026-09-25

- Adapter v0.3.0 now has a separate-display render driver: one physical
  off-axis projection per Unity display, with no 7680-pixel render target or
  compositor. It waits for all three camera `OnPostRender` probes before
  reporting three-view activity. The stage camera remains the center view;
  two cloned cameras render the side views directly.
- The path is doubly gated by `output.mode = separate-displays` and the
  default-off `EnableSeparateDisplayPrototype` setting. The current installed
  layout remains `nvidia-surround`; the new setting is absent/false in the
  preserved installed settings. Therefore the newly deployed binary cannot
  activate secondary displays in the owner's current Surround profile.
- `tools/experiments/Stage-SeparateDisplayLayout.ps1` performs a dry run by
  default and can back up/stage identical canonical and installed layouts for
  the attended test. Its dry run identified the current 2560x1440-per-panel
  source correctly; `-Apply` was **not** run.
- Release solution build succeeded with zero warnings; 47 geometry/protocol
  assertions passed. The installed DLL hash matches the build; settings and
  staged layout hashes match their pre-deployment backups. No game launch,
  display activation, refresh-rate change, or topology switch occurred.
- **Open:** Actual display-index mapping, secondary-window presentation,
  camera post-processing, FPS, tearing, return-to-menu behavior, and any
  failure recovery remain unverified until an attended separate-monitor run.

## Offline follow-up — 2026-09-26

- The direct-display driver now resets its frame evidence when the accepted
  layout hash or left/right Unity display mapping changes. Runtime status only
  reports `active` after center, left, and right camera callbacks complete in
  the same recent Unity frame. An old side-camera callback cannot certify a
  new layout or a stopped side view.
- Side camera objects are prepared before secondary-display activation. The
  diagnostic snapshot now records Unity's display indices, active flags,
  system/rendering dimensions, and the span between the three render callbacks.
  These are render-side observations, **not** presentation or scanout timing.
- The local projection core's eight shared C# source files are byte-identical
  to private toolkit revision `1d67d066e1f0c165172597c0397d1056f2502c53`.
  The toolkit additionally has eye-ray primitives. The mod still builds its
  local core; no private repository dependency was introduced.
- Release build completed with zero warnings/errors and all 47 offline
  geometry/protocol assertions passed. No game launch, installation, layout
  staging, display activation, or topology change occurred in this follow-up.

## Attended acceptance record to fill

Record the exact game/Unity/UMM and GPU driver versions, Windows primary and
left/right arrangement, per-monitor mode and refresh, layout hash, mod settings,
and installed DLL hashes before launch. Preserve pre-run `Player.log`,
`status.json`, and `render-diagnostics.json` with the previous settings.

1. With the game closed, stage the separate-display layout and independently
   confirm its hash. Set the center monitor as Windows primary and leave the
   three displays at their normal supported native modes. Do not infer Unity's
   left/right index order from Windows monitor numbers.
2. Launch with the separate-display opt-in enabled and the Surround three-view
   and center-preview toggles off. Confirm the title/menu appears on center,
   pointer and keyboard/controller focus work, then enter a stage. Record which
   physical monitor receives Unity indices 1 and 2; swap only the mod indices
   if needed. Confirm a new frame succeeds after any swap.
3. Check a straight world edge and both shared scene hinges while stopped and
   while moving. Test the user's saved bonnet camera pitch (9.218609 degrees)
   without changing ArtOfSimRally settings. Capture a phone video that shows
   all three panels and their seams, plus screenshots/status for exact frames.
4. Drive a steady horizontal pan at each panel's native refresh. Compare a
   moving horizontal tear line within a panel with stutter or a mismatch at a
   bezel. Record VSync request/effective values, observed FPS, game fullscreen
   mode, and callback span. These diagnostics alone cannot prove tear-free
   presentation; compare any borderless or per-game driver change separately.
5. Test pause, settings/dialogs, camera changes, return to menu, replay and
   photo mode. Verify center UI, pointer hit targets, post effects, HUD, and
   stock-camera restoration. Check that disabling the opt-in stops side-camera
   rendering; exit the game before expecting activated displays to release.
6. Stop at a hang or corrupted output. Preserve logs/video, close the game,
   and restore the backed-up layout/settings after exit. Mark each test pass,
   fail, or untested; do not promote the capability in the manifest from a
   single `active` status or a static screenshot.

If independent output fails, the existing exact-width NVIDIA Surround
compositor remains the known wide-output fallback. A borderless span across
three separate Windows displays is a separate experiment: use the public
`srwe-cli` fork's `apply --pid ... --borderless --client-area` only after an
attended windowed launch, then `inspect --pid ... --json` to verify the actual
client rectangle. Derive `x`, `y`, width, and height from the live Windows
desktop layout, and confirm Unity rebuilds its 7680x1440 backbuffer rather
than stretching a 2560x1440 image. SRWE changes a window rectangle; it does
not produce three projections or synchronize scanout.
