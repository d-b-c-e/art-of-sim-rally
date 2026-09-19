art of sim rally - @RELEASE@
============================

Wheel force feedback, direct USB controls, bonnet/bumper views and SimHub telemetry
for the 64-bit Windows game. Tested on Steam; other stores are not confirmed.
This candidate includes the new Simple/Advanced settings UI. Runtime UI/device
acceptance is pending. No SDK, separate toolkit download or SimHub helper needed.

INSTALL / UPDATE
----------------
1. Close art of rally.
2. Install Unity Mod Manager 0.27.0 or newer for this game (one-time setup):
   https://www.nexusmods.com/site/mods/21
   Extract UMM, run UnityModManager.exe, select Art of Rally, check its game
   folder, click Install, then close UMM.
3. Use Extract All on this mod ZIP. Keep its files together, then double-click
   Install.bat. Wait for successful verification. Existing settings are retained.
4. Launch through Steam. Press F6 for Wheel settings, or Ctrl+F10 to find the
   same panel in Unity Mod Manager. Check the installed version there.

For a custom folder, find artofrally.exe using Steam > Manage > Browse local files.
Open PowerShell in this extracted download and use the real path:
  .\Install.bat -GameDir "D:\Games\artofrally"
Alternatively, drag the mod ZIP onto UMM's Mods tab after installing its loader.
Prefer Install.bat for updates if you used it before: both native DLL copies stay
synchronized. Vortex installation has not been validated.

Remove with the game closed: double-click Uninstall.bat. Settings, other mods and
UMM stay. A custom path also works: .\Uninstall.bat -GameDir "D:\Games\artofrally"
Before comparing candidates, back up Mods\ArtOfSimRally\Settings.xml separately.

FIRST DRIVE
-----------
Pause before setting up. Opening settings stops mod controls/FFB but does not
promise to pause the game simulation. Keep a mouse/keyboard available for UMM.
Simple is the first view; Advanced adds tuning to the same saved values.

* Controls: keep working game bindings. For a missing axis, centre/release it,
  click Bind, move only that control through its full range, then release.
  Steering needs both full locks and centre. Check the preview, inversion and
  deadzone, then Save calibration. Cancel/timeout/save failure keeps the old
  binding. Explicitly calibrated ranges do not learn themselves during a drive.

* Separate USB handbrake/TSS: select handbrake mode on the device, then bind
  Handbrake (axis) as above. It should read 0-100% with partial travel. If the
  device reports only a button, use Handbrake (button). Axis, button and game
  input coexist by taking the largest value. Each row can use a different USB
  device. TSS physical travel on this new UI still needs an attended check.

* FFB: choose On and Use steering wheel, or explicitly select the physical wheel.
  Follow mode uses the mod's saved Steering binding. If you use only the game's
  steering binding, choose the FFB wheel explicitly. Missing/unverified selections
  stay unavailable; Refresh/retry while paused. Saved Off remains Off.
  Strength defaults to 50. Force builds from 3 to 12 km/h; test while moving.
  Advanced includes direction and smoothing (20% = the old 0.20 setting).
  Higher smoothing softens rapid force changes but adds delay.

* Stop FFB: F8 or the header button stops feedback and saves Off. Choose On in
  FFB explicitly to resume. Controls > Mod buttons also binds optional USB
  Settings and Stop FFB buttons; these work with assigned driving controls Off.

* Driving/menu buttons: Open game bindings uses the game's existing controls
  screen and action maps. If unavailable in this scene, use Options > Controls
  from the main menu. A device unreadable by the game's Raw Input route still
  needs keyboard/pad for those actions; direct axes/mod buttons remain available.
  Shifter is an expandable Controls group: select the device and sequential or
  H-pattern mode, then Bind its buttons. Clear unwanted duplicate game bindings.

* Landing vibration: Advanced > FFB, on by default at 5 (range 0-40).
  Crash kick: experimental, off by default at 50 (range 0-100). It uses a finite
  120ms constant-force push/release; landing stays a 25Hz sine. One impact owns
  the output at a time. Strengths are percentages of nominal wheel force and
  independent of steering Strength. Saved values are retained: select 50 manually
  to compare the crash default. Stronger physical crash response is unaccepted.
  These controls do not scale telemetry or the ButtKicker; tune that in SimHub.

Leave the legacy steering limiter override Off; use the game's assist settings.

CAMERAS
-------
Bonnet/Bumper On includes those views in the game's Change camera cycle.
Adjustment bindings in Simple supports keyboard keys and USB buttons:
8/2 up/down, 9/7 forward/back, 4/6 left/right, 1/3 tilt, +/- FOV, 0 reset.
Defaults use the numpad. Bind/Clear work without a numpad. Close settings and
release captured/held controls before adjusting a view. F8/F10 and the Settings
key are reserved; Reset defaults refuses a conflict rather than overwriting it.
Advanced adds pose/FOV/lean and Reset this view, which affects only that mount.
Nexus CameraMod retains camera control while loaded; our mounts are suspended.

TELEMETRY / SIMHUB / BUTTKICKER
-----------------------------
In SimHub select Forza Horizon 5 and match the mod's destination. The explicit
local preset is 127.0.0.1:8000. Enable Telemetry while paused and verify speed/RPM.
Advanced > Edit connection commits host and port together on Apply connection;
Cancel changes neither. UDP Sending status does not confirm receiver delivery.
Use built-in Impacts/Road impacts for a ButtKicker. No extra helper is required.
The owner's rig improved at 30Hz; tune your own rig and reduce gain if it clips.
The development recorder remains separate and is not included in this ZIP.

HELP
----
Help > Create support file on Desktop collects settings, device identifiers,
paths and logs locally; nothing is uploaded. For a recurring problem, enable
Advanced > Help > Log detail for support, reproduce briefly, pause, export, and
turn detail off. Ordinary errors/settings can be collected with detail off.
Attach the Desktop support TXT to an issue with wheel/driver, car/stage and
what happened. Check Saved/error status: failed binding writes keep old values.

Source, docs and licence: https://github.com/d-b-c-e/art-of-sim-rally
Issues: https://github.com/d-b-c-e/art-of-sim-rally/issues
Stable 0.2.5 docs describe the old panel. This README describes this candidate.
Tested hardware history: MOZA R12; positive R5/T300 reports. This UI, full hardware
transition matrix, Fanatec/TSS travel and stronger crash feel await testing.
