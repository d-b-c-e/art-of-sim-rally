# Changelog

Notable changes to art of sim rally.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Direct wheel input** (new "Wheel input (direct)" section). For wheels the
  game's controls screen never responds to — a Fanatec base shows up as two
  identical `FANATEC Wheel` entries the game's input library cannot read.
  Steering, throttle, brake, clutch and handbrake are read straight from the
  device and fed to the car, bypassing that library; assign each by "Assign,
  then move it", and the range calibrates itself the first time the control
  is used fully. Any DirectInput controller can supply any channel, so
  separate pedals work too. Menus still use the keyboard or a pad.
  Switching the game's input library to DirectInput instead was tried twice
  and left the menus dead both times; it remains only as a Settings.xml
  experiment, not in the panel.
- Support bundle now records which input backend was active.

## [0.2.1] - 2026-09-02

### Added

- **Bumper camera.** A second mounted view after the bonnet view in the
  rotation — lower and further forward, just above the front bumper. Its own
  height, forward, side, pitch and field of view; the numpad adjusts whichever
  of the two views is on screen and resets that one alone. Can be switched off
  separately from the bonnet view.

### Changed

- **Force feedback is now lateral force through a pneumatic trail, not the
  game's aligning torque.** `Mz` is a Pacejka curve that reverses sign at
  about 8° of slip, and this game's front tyres sit at 12–29° in ordinary
  corners — so the wheel flipped from centring to pulling toward lock in the
  middle of every corner ("there is no centre"). The new force is the front
  axle's `Fy` scaled by a trail that shrinks toward the limit: it centres in
  proportion to load, lightens as the front starts to slide, never reverses.
  Fades out below 12 km/h, where slip angles mean nothing. Sign confirmed on
  a MOZA R12; `Invert` remains for wheels that read the axis the other way.
  `MzReference` is replaced by `FyReference` (8,000 N).

### Fixed

- **Telemetry parked until the lights went green.** `IsRaceOn` used the strict
  "driving" state, which is false during the countdown, so SimHub treated the
  start line as race-off and a bass shaker ignored the engine while revving.
  It is now true from the start-line hold onward; forces still wait for green.
- **Force feedback inverted on one side only** (MOZA R5, reported with a
  log). The native plugin passed the sign in both the direction vector and the
  magnitude; a negative magnitude reverses the direction again, so on a wheel
  that honours both the force always pointed the same way — right turns
  correct, left inverted, and *Invert* could not help because it negates both.
  The **signed magnitude alone** now carries which way to pull, with the
  direction vector's length matching it: right for a wheel that honours
  direction (R5), for one that ignores it and reads the magnitude sign (R12),
  and for single-axis wheels (Fanatec), which already worked that way.
- **Force feedback stopping mid-session and never returning.** Alt-tabbing
  away from the game left the wheel acquired non-exclusively
  (`DIERR_NOTEXCLUSIVEACQUIRED`), which force feedback cannot use, and nothing
  re-acquired it. Now re-acquired and retried automatically; the exclusive
  mode is also bound to the game's own window rather than whatever was in
  front at startup.

## [0.2.0] - 2026-09-01

The shifter release. Also the point at which the settings panel stopped being
a wall of switches.

### Added

- **Separate shifter support, H-pattern and sequential.** Bind a real shifter
  on its own device. The game only has ShiftUp and ShiftDown actions, so even a
  bound H-pattern lever would have behaved like paddles — selecting 3rd would
  have meant "one gear up from wherever you are". This reads the shifter
  directly and selects the gear you actually chose. Both modes verified on
  hardware.
- **Gear binding inside the settings panel.** Click "set", move the lever, done.
  The rows match your shifter type rather than showing seven gates for a
  sequential.
- **Wheel and shifter dropdowns.** Pick a device by name instead of typing one.
- **"Create support file on Desktop".** Collects settings, controllers, what is
  actually bound, and the logs into one file to attach to a bug report.
- **Install.bat and Uninstall.bat.** Double-click to install. Finds the game on
  non-default Steam libraries, checks Unity Mod Manager is present, and refuses
  to run while the game is open. Uninstall keeps your settings.
- **Button names where the game has no icon.** Unrecognised wheels render some
  bindings as an empty box; they now read `B12` instead of nothing.
- **"Bind whichever device you touch"**, since the controls screen otherwise
  only binds the first joystick it finds.
- **"Skip neutral"** for sequential shifters — reverse to first in one press.
- **Live input status and a rescan button**, showing what the game's input layer
  can actually see.

### Changed

- **The settings panel is drawn by hand**, in collapsible sections with headings
  that do not look like dropdowns, and help text that no longer runs off the
  edge of the panel.
- **Strength is a 0-100 slider.** It was previously a reference-torque figure
  where lower meant stronger, which nobody should have to reason about.
- **Changing the wheel applies immediately.** Trying each of two similarly named
  devices to see which one moves is the natural way to pick one, and that needs
  the change to take effect now rather than next launch.
- **Telemetry host and port apply immediately** for the same reason — working
  out which port is free is exactly when you change it repeatedly.
- **The native plugin loads from the mod folder**, and the loaded path is
  logged. Resolving it loosely had silently loaded a stale copy.

### Fixed

- **Shifter failing to open at load.** Enumeration was skipped whenever force
  feedback had already initialised DirectInput, so the first open always failed.
- **H-pattern holding the car in neutral.** With nothing bound yet, every frame
  read as "no gate held", which is neutral — indistinguishable from the mod
  having broken the game.
- **The end-of-stage camera swing**, partially. The stock rig damps toward its
  target from wherever the camera is, so letting go while mounted inside the car
  sent it out through the bodywork. It now hands back in one step. Some
  movement remains; see Known issues.
- **Help text cut off at the left edge** of the settings panel.
- **Force feedback stopping during cutscenes** rather than fighting the AI.

### Known issues

- The camera can still swing about briefly when the game takes control at the
  end of a stage. Cosmetic, confined to the results cinematic.
- Developed against a MOZA R12 Base. The steering and deadzone fixes should
  apply to any wheel Rewired does not recognise — reasoning, not testing.

## [0.1.2] - 2026-09-01

Tagged at the same commit as 0.1.1 by mistake, so the support-file button its
release notes announced did not actually ship until 0.2.0.

## [0.1.1] - 2026-09-01

### Fixed

- **Force feedback on wheels with a single force-feedback axis**, which covers
  Fanatec bases. The effect was created with a two-axis fallback to one, but
  every update still sent two axes, so each one failed silently and the wheel
  stayed dead. Reported by the first user to try it.
- Failures during force-feedback updates are now logged instead of discarded.

## [0.1.0] - 2026-09-01

First release. Turns art of rally into something you can drive on a wheel.

### Added

- **Force feedback.** The game ships the calling code and the physics for it but
  never joined them, and the DLL it looks for is not in the build at all. This
  supplies that DLL and closes the gap.
- **Direct steering**, removing the gamepad smoothing the game applies to wheels
  it does not recognise.
- **Removal of a hidden 10% deadzone** applied by the game's input library,
  separate from the one in the options screen and shown nowhere.
- **Bonnet camera**, added to the game's own view rotation, with live numpad
  tuning.
- **Forza-compatible UDP telemetry**, for SimHub, dashboards, bass shakers and
  motion rigs.

[0.2.0]: https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.0
[0.1.2]: https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.1.2
[0.1.1]: https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.1.1
[0.1.0]: https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.1.0
