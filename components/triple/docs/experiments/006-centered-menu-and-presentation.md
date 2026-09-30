# Experiment 006: Centered UI and presentation follow-up

## Authority and scope — 2026-09-24

The user requested an attempted title/menu centering fix, explanatory hover
help for experimental options, and investigation of screen tearing. The most
recent attended run was stable with experimental three-view rendering **off**;
the user prefers the stock wide field of view. Keep that option off and leave
NVIDIA Surround at the current 7680x1440 topology. Do not change VSync,
graphics-driver settings, ArtOfSimRally files, or the user's camera settings.

## Procedure

1. Preserve current logs, adapter, and settings. Confirm the game is closed.
2. Add passive runtime inventory of the menu RectTransforms and option help;
   build Release and run offline regressions.
3. Install only this mod's owned binaries/metadata, preserving Settings.xml and
   desired-layout.json. Launch with Surround already stable, using the normal
   game launcher. Observe title and menu with stock rendering only; collect
   inventory and exit normally.
4. Based on the actual hierarchy, make the smallest reversible opt-in UI
   centering change. Build, test, deploy, and perform an attended title/menu
   smoke test. Stop if there is a startup hang or visual corruption.
5. Record exact result, limitations, and presentation evidence. A VSync report
   alone is not proof that scanout tearing is absent.

Acceptance for this iteration: title and main interactive menu fit the center
panel while the background remains full-width; pointer/focus still operate;
toggle-off and unload restore the original layout. Pause, dialogs, and motion
tearing require separate observation before claiming comprehensive support.

## Results — 2026-09-24

- Backed up the installed adapter/settings and pre-run diagnostics in the
  ignored `artifacts/experiment006-20260924/` directory. The installed
  `EnableThreeViewPrototype` setting was still `true`, so set it to `false`
  before the smoke test to honor the user's preference. No Surround topology,
  VSync, or camera-mod change was made.
- The read-only menu probe identified `IntroSplashScreen/Text`,
  `Main Menu/Race/VerticalLayoutGroup`, and `Main Menu/Race/GameLogo` under
  `PanelManager`, with a `ScreenSpaceOverlay` canvas at scale factor 1.333.
  The probe was removed from the shipped build.
- Deployed adapter v0.2.2 and launched twice at 7680x1440. The title prompt,
  main-menu logo and main-menu choices visually moved into the center panel;
  the full-width backdrop and decorative right-side art remained in place.
  UMM reported the mod active with experimental three-view rendering disabled.
  The game closed normally and the post-run Player.log records
  `GameEntryPoint: OnApplicationQuit()` without an adapter exception.
- The UMM option panel displays the new centering toggle and explanatory help
  area. Actual hover text, menu pointer hit targets, focus navigation and
  toggle-off restoration were **not conclusively validated** in this smoke
  test, so the menu feature remains partial. An attempted click on the moved
  Options label produced no observed transition; this needs a local attended
  interaction check before broad claims or optimizer integration.
- The run reported ExclusiveFullScreen, VSync requested/effective 1, and
  a 59 Hz virtual display. These are presentation settings, not proof of
  tear-free output. There is no moving-scene capture in this experiment;
  screen tearing remains open. The next controlled comparison should capture
  motion with stock rendering, distinguish a true moving horizontal tear line
  from stutter/bezel seams, and then test a **per-game** VSync driver override
  or borderless presentation one variable at a time, with a known rollback.
