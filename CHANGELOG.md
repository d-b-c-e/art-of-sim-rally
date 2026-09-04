# Changelog

Notable changes to art of sim rally.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- **Stage start stuttered and briefly locked up for the first 10-15 seconds**
  (new in 0.2.2). `WheelInput` learns each axis's full range the first time the
  control is used - the opening seconds of a stage - and wrote `Settings.xml`
  from the per-frame path each time it extended, rate-limited to once every five
  seconds. A synchronous XML serialise and disk write, landing at roughly t+0,
  t+5 and t+10, in the one moment the player is trying to drive. The settings
  object is still updated on the frame, so the panel shows the live range; only
  the disk write is deferred, to when the player stops driving and to shutdown.
  Reported but **not yet confirmed fixed** - see docs/KNOWN-ISSUES.md KI-5 for
  the alternatives if it survives.

- **The game's own camera angles rendered reversed once a bonnet or bumper view
  had been used** ([#1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1),
  reported on a Thrustmaster T300 RS GT). The stage camera is two objects:
  `CarCameras` drives the GameObject "Stage Camera", but the camera that renders
  is its child "Camera Main", which the game pins at local identity. The mounted
  views are positioned by writing world-space transforms to `Camera.main` - the
  child - which Unity stores as a local offset from the parent, and nothing put
  it back. Cycling on to a stock angle then placed the parent correctly and left
  the child looking roughly 180 degrees the wrong way, for the rest of the stage.
  The handback now clears the child's local transform, restoring the invariant
  `CameraManager` asserts in its own constructor. The same hole is the likely
  cause of the residual swing at the end of a stage, which should now be gone
  too. **Neither is confirmed on screen yet** - see docs/KNOWN-ISSUES.md.

- **The vendored toolkit was never actually committed.** `.gitignore` excluded
  the `lib/` *directory*, and git does not descend into an excluded directory,
  so the `!lib/toolkit/**` re-include below it could never match. A fresh clone
  had no `lib/toolkit`, so `tools/package/package.ps1` failed at its own guard
  and the "pinned by VERSION, diffable bump" convention was untrue in practice
  for the whole of 0.2.2. The rule is now `lib/*`, and the pinned binaries are
  committed.

### Changed

- **Toolkit pin v0.1.0 -> v0.4.0.** The native DLL is a drop-in: 37 undecorated
  exports against the 28 that shipped in 0.2.2, nothing removed, still x64,
  every entry point the mod P/Invokes present. Verified with `dumpbin /exports`
  against the previous release's binary. The telemetry encoder's public surface
  is byte-for-byte identical and the toolkit's 39 encoder tests pass at the
  release commit, including the `Speed`@256 anchor.

  No behaviour change - the mod calls none of the new surface yet. What it
  unlocks: `GetDeviceGuid` / `GetAnyDeviceGuid` (added at this repo's request,
  and the thing that makes `SetPreferredDeviceGuid` usable from a mod with no
  DirectInput layer of its own), `CreateConditionEffect` /
  `UpdateConditionEffect` / `ReleaseConditionEffects` for the damper KI-6 wants,
  and the periodic effects for surface texture. See docs/ROADMAP.md.

  `Dbce.Wheel.Ffb` gained `ForceModelSettings` parameters and a `ForceProfile`
  type in toolkit 0.3.0. This mod does not reference that assembly, so nothing
  moves; the vendored copy is unused and only ships because the sync script
  copies `Dbce.Wheel.*`.

- `tools/Sync-Toolkit.ps1` refreshed from the toolkit (it changed in 0.3.1). It
  now hashes everything it writes into `lib/toolkit/MANIFEST.txt` and refuses to
  overwrite a vendored file that was edited locally, exiting non-zero rather
  than clobbering it.

### Added

- The vector found a defect upstream on its first run: `simlite@1` in
  dbce-wheel-mod-toolkit was described as this mod's tuning, but only its model
  half was - its shaper half was the toolkit's own defaults, which this game does
  not use. Fixed there as `simlite@2`. Recorded here as U-1 because it changes
  what a future adoption of `ForceModel` must take, and it corrects guidance
  written before the vector existed.
- Clamp order was a measured difference from the toolkit: we fade then clamp,
  `ForceModel.Compute` clamped then faded. 4 of 640 vector rows differ, all at
  5-7.5 km/h, worst 1,086/10,000 at the wheel, and only reachable when a force
  past full scale meets a partial fade. The toolkit adopted our order in v0.7.0;
  nothing in production moved. Recorded as U-2 with the table in
  FORCE-FEEDBACK.md, including the two things that came out of building it - the
  bigger casualty was soft saturation rather than the fade, and their conformance
  sequence could not see the change at all until it was extended past full scale.
- Corrected the step 3 guidance again, this time in the direction of less work.
  Toolkit 0.7.1 makes `ForceProfile.SimLite()` return a model and its
  conditioning together as an in-code literal, and `simlite@2`'s shaper states
  every value rather than inheriting: deadzone 0, soft saturation 0, slew 0,
  output deadband 0, ramp 0, and attack smoothing equal to decay at 0.2. So it is
  feel-neutral against what this mod does today, and an earlier note here saying
  adoption would bring a soft knee to judge at the wheel was wrong. It also
  settles the in-code-versus-ini question without argument, since a literal needs
  no deployed file.
- U-3: the low-speed fade scales the force, it does not cap it. Harmless at the
  strengths anyone runs - inside the fade band the threshold is 5x to 35x
  anything the game has been measured producing - but it is a constraint on ever
  adding impact effects, which are transients several times cornering Fy and
  arrive at any speed.

- **A conformance vector for the steering force curve**, `docs/force-curve-vector.csv`,
  generated by `tools/force-vector`. The curve is duplicated - `ForceCurve` here,
  `simlite@1` in dbce-wheel-mod-toolkit - and nothing enforced that the two
  agreed. The failure mode is silent: a subtly wrong force does not throw, it
  just feels like the mod got worse. The generator links the mod's own
  `ForceCurve.cs` rather than copying it, so the vector is produced by the code
  that ships.
- The force curve moved out of `FfbController` into `ForceCurve`, pure
  arithmetic with no Unity dependency, so it can be evaluated outside the game.
  Arithmetic order is unchanged and the output is identical.

- **The support file now reports the native plugin's version, where it was
  loaded from, and its last HRESULT.** The native layer is vendored from
  dbce-wheel-mod-toolkit and pinned per release, so the mod's own version says
  nothing about which one a user is running - and a stale `UnityForceFeedback.dll`
  left in the game's plugin folder by an old manual install can be loaded in
  preference to the current one, then fail on an export it predates. That looks
  exactly like force feedback being broken for no reason, and now takes one line
  of a support file to spot.

### Documentation

- **`docs/KNOWN-ISSUES.md`** - a defect register: open issues with severity and
  ranked hypotheses, resolved ones with their causes, and the things that were
  deliberately abandoned with the reasoning. The end-of-stage camera issue moved
  here from CAMERA.md, and #1's mechanism is written up in full.
- **Corrected diagnostic instructions that no longer worked.** FORCE-FEEDBACK.md
  and ROADMAP.md told you to set `AOSR_FFB_LOG=1` to get a native log. That
  variable ceased to exist when the native layer moved to the toolkit; it is
  `DBCE_FFB_LOG` now, and logging is **on by default**, so no variable is needed.
- **Corrected the plugin's install location.** RELEASING.md said
  `UnityForceFeedback.dll` belongs in `artofrally_Data/Plugins/x86_64/` and that
  a wrong location fails silently. It ships inside the mod folder and is loaded
  from there; a stale copy in the plugin folder is itself a failure mode.
- ROADMAP.md rewritten around what is left. Phases 0-5 were all shipped while
  the file still said phase 0 was next and blocked on plugging in a wheel.
- FORCE-FEEDBACK.md no longer says the native plugin is "not yet
  runtime-verified", which its own phase 0 section had contradicted since
  2026-08-31; FINDINGS.md's four open questions are answered in place; CAMERA.md
  no longer says the camera is unimplemented; CONTROLS.md's resolved symptoms
  and completed checklists are marked as such.
- README: Strength's recommended starting point is 50, matching the two retunes
  and TROUBLESHOOTING.md, not 70. Thrustmaster T300 RS GT added to the tested
  list.
- CLAUDE.md: `dotnet test` runs nothing in this repo and must not be quoted as a
  gate; **the game no longer accepts injected keyboard input**, so anything
  needing a stage driven needs a person; the two-object camera rig recorded as a
  finding; repository structure and status brought up to date.

## [0.2.2] - 2026-09-03

### Changed

- **Native plugin and telemetry encoder now come from dbce-wheel-mod-toolkit
  0.1.0**, vendored under `lib/toolkit` and pinned by `lib/toolkit/VERSION`.
  `UnityForceFeedback.dll` is the toolkit's `WheelFfb.dll` under the name the
  mod P/Invokes (same exports plus the toolkit's lifecycle additions);
  `ArtOfSimRally.Telemetry.dll` is replaced by `Dbce.Wheel.Telemetry.dll`, a
  byte-identical encoder whose test suite moved with it. The local copies of
  both, the telemetry tests, the probe and the synth tool are removed from this
  repo (`tools/forza` in the toolkit replaces the last two). No behaviour change
  intended.
- **Default force feedback is 30% lighter** (`FyReference` 8,000 → 11,500 N).
  At Strength 50 the 0.2.1 default was still too strong on a MOZA R12; the
  slider midpoint now sits where that rig wanted it.

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
- `docs/TROUBLESHOOTING.md`, with a Fanatec section first.
- Device dropdowns show axis and button counts, so two devices with the same
  name (a Fanatec base's two `FANATEC Wheel` entries) can be told apart.

### Fixed

- **Direct wheel input steered inverted** when the wheel had been turned left
  during Assign: the moved direction became +1, and the game reads +1 as
  right. Steering assignment now always takes the increasing side of the
  axis as right (DirectInput's convention on every wheel), and each bound
  channel has a Flip button.
- **Crash when choosing a shifter** whenever force feedback had failed to
  initialise (reported with a Fanatec bundle). Listing controllers created a
  temporary DirectInput instance and released it, leaving the device table
  filled; opening the chosen device then used the released instance. One
  instance now lives for the whole session.
- **Force feedback gave up when the preferred device had no actuator.** A
  Fanatec base presents two `FANATEC Wheel` devices and only one does force
  feedback; picking the other failed with `0x80040154` and left the wheel
  dead. The other candidates are now tried before giving up.

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
