art of sim rally - @RELEASE@
============================

Wheel force feedback, direct USB controls, bonnet/bumper views and SimHub telemetry
for the 64-bit Windows game. Tested on Steam; other stores are not confirmed.
No SDK or separate toolkit download is needed. SimHub is optional.

INSTALL
-------
1. Close art of rally.
2. Install Unity Mod Manager 0.27.0 or newer for this game (one-time setup):
   https://www.nexusmods.com/site/mods/21
   Extract UMM, run UnityModManager.exe, select Art of Rally, check the game
   folder, click Install, then close UMM.
3. Use Extract All on this mod ZIP, then open the extracted folder. Keep all
   files together and double-click Install.bat. Wait for successful verification.
4. Launch art of rally through Steam. Press Ctrl+F10, find art of sim rally,
   check its version, and open the mod's settings.

The installer detects Steam libraries on other drives. If detection fails,
find the folder containing artofrally.exe (Steam: Manage > Browse local files).
Open PowerShell in this extracted download and run, with your actual folder:

  .\Install.bat -GameDir "D:\Games\artofrally"

Alternatively, after installing UMM for the game, drag the mod ZIP onto UMM's
Mods tab. Prefer Install.bat for updates if you previously used it: it keeps both
native plugin copies synchronized. Vortex installation has not been validated.

UPDATE OR REMOVE
----------------
Update: close the game, extract the new release into a fresh folder, and run
its Install.bat. No uninstall is needed. Your Settings.xml and bindings stay.
Remove: close the game and run Uninstall.bat from this extracted download.
Settings, other mods and UMM stay. For a custom folder use:
  .\Uninstall.bat -GameDir "D:\Games\artofrally"

FIRST DRIVE
-----------
Pause before assigning controls or selecting devices. Keep the game focused
while force feedback sets up.

* Force feedback: leave Enabled on and select your real wheel under Wheel.
  Strength defaults to 50; lower it if heavy. Force builds from 3 to 12 km/h,
  so test while moving. If input is correct but force pulls away from centre,
  enable Invert direction. Smoothing defaults to 0.20; higher values soften
  fast force changes and add delay. It filters FFB, not steering input.

* Controls: keep the game's bindings if they work. Otherwise enable Wheel input
  (direct) > Read the wheel directly. Centre the wheel/release the pedal, click
  Assign on its row, then move only that control through its normal range.
  Use Flip if reversed. Menus still use keyboard/pad.

* Separate USB handbrake (including TSS): in Wheel input (direct), release the
  lever, click Handbrake > Assign, then pull it. Exercise full travel once and
  release. An axis should read gradually from 0 to 1; a button is on/off. Each
  row may use a different USB device. Leave working game-controlled rows unbound.
  Select the TSS handbrake mode first; actual TSS travel still needs confirmation.

* Shifter: enable Use a separate shifter, choose the device and H-pattern or
  sequential mode, then click set for each gear/shift and move the lever.
  Clear game bindings if the same lever also accelerates, brakes or shifts twice.

* Landing vibration: on by default at strength 5 (maximum 20), independently of
  steering. Existing saved choices are kept. Requires wheel sine-effect support.
  This controls the wheel, not the ButtKicker. Small hops may not trigger it.

* Leave Disable steering limiter on car spawn (legacy) off; use the game's own
  assist settings. That checkbox does not temporarily replace its assist slider.

CAMERAS (OPTIONAL)
------------------
Use the game's change-view button to reach bonnet/bumper views. Numpad:
8/2 up/down, 7/9 back/forward, 4/6 left/right, 1/3 tilt, +/- FOV, 0 reset.
No numpad? Camera > Enable camera tuning keys > Rebind. Close the settings
panel to use the keys. Changes save when paused or otherwise idle.
If Nexus CameraMod is loaded, our mounts/tuning are suspended. Disable that mod
before a fresh launch to use our mounts; saved settings are kept.

SIMHUB / BUTTKICKER (OPTIONAL)
------------------------------
In SimHub select Forza Horizon 5 and check its UDP listening port is 8000.
In the mod, pause and enable Telemetry > Send telemetry. On the same PC use
host 127.0.0.1, port 8000. Start a stage and check speed/RPM respond in SimHub.
Forza itself does not need to run or be installed. Avoid another app listening
on the same port; change both ends if necessary. Driving edits wait for pause.

Use SimHub's built-in Impacts/Road impacts for the ButtKicker. No extra helper
is needed. Wheel Landing strength does not control these effects. The owner's
rig improved at 30 Hz; tune your own rig. Reduce gain if the amplifier clips.

HELP
----
For a recurring problem, enable Log detail for support (either checkbox),
reproduce briefly, pause, then press Create support file on Desktop under
Devices and troubleshooting. Turn logging off afterward. Ordinary errors and
settings can be collected with detail off. Attach the Desktop support TXT to
an issue with wheel/driver, car/stage, and what happened.

Tested on MOZA R12, with positive R5/T300 reports. Fanatec/TSS-specific behavior
and the complete hardware transition matrix still need confirmation.

Full setup: https://github.com/d-b-c-e/art-of-sim-rally/blob/main/docs/SETUP.md
Troubleshooting: https://github.com/d-b-c-e/art-of-sim-rally/blob/main/docs/TROUBLESHOOTING.md
Issues: https://github.com/d-b-c-e/art-of-sim-rally/issues
Source and licence: https://github.com/d-b-c-e/art-of-sim-rally
