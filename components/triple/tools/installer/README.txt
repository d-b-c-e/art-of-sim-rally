art of rally triple-screen - @VERSION@
====================================

Adds angle-correct three-screen views to the 64-bit Windows version of art of
rally. Tested on Steam. Other stores and display layouts are not yet confirmed.
The mod runs on its own; Triple Screen Optimizer is optional.

INSTALL OR UPDATE
-----------------
1. Close art of rally.
2. Install Unity Mod Manager 0.27.0 or newer for Art of Rally (one-time setup):
   https://www.nexusmods.com/site/mods/21
3. Extract this whole ZIP and double-click Install.bat. It finds Steam libraries
   and checks the package before copying only this mod's files.
4. Launch through Steam. Press Ctrl+F10 for Unity Mod Manager, find this mod,
   and choose Off, Single wide display, or Three separate displays.

For a custom game folder, run from the extracted download:
  .\Install.bat -GameDir "D:\Games\artofrally"

You can also drag the ZIP onto Unity Mod Manager's Mods tab. Keep an extracted
copy if you want to run Uninstall.bat later. Existing Settings.xml and screen
measurements are preserved on update and removal.

FIRST SETUP
-----------
Use Advanced setup to enter one panel's pixel size and visible physical size,
your eye distance, and the angle of each side screen. Choose Use these
measurements. The displayed example values are inactive until accepted.

For Single wide display, first enable a 3-panel-wide resolution such as NVIDIA
Surround, then select that mode in the mod. For Three separate displays, keep
Windows extended desktop mode, set the center monitor as primary, and select
the two secondary Unity display indices in Advanced setup. Restart after
changing Windows display mode. Turn off Override field of view to follow the
game's camera FOV across all three views. Turn it on to use the saved slider;
Reset returns to your entered geometry.

KNOWN LIMITS
------------
Screen tearing was observed in NVIDIA Surround even with Unity VSync set to 1.
It was not observed in the attended separate-display drive. Neither result
proves synchronized scanout on other hardware. Replay, photo mode, every menu,
and every display configuration remain incompletely tested. If the picture is
wrong, choose Off in the mod and restart the game.

REMOVAL
-------
Close the game and double-click Uninstall.bat. It removes only this mod's five
packaged files. Your settings, measurements, other mods, and Unity Mod Manager
remain. See the GitHub README for setup and troubleshooting updates.
