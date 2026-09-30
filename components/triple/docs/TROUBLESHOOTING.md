# Troubleshooting

## The mod does not appear in Unity Mod Manager

Confirm that UMM is installed for **Art of Rally**, not only for another game. Press **Ctrl+F10** in the game and check for `DBCE triple-screen for art of rally`. The folder should be `Mods/DbceTripleScreenArtOfRally` under the game, not `Mods/Mods/DbceTripleScreenArtOfRally`. Close the game and rerun the extracted `Install.bat` if needed. If the installer cannot find Steam, use `Install.bat -GameDir "D:\Games\artofrally"` with the folder containing `artofrally.exe`.

## The mod says setup or layout is missing

Open **Advanced setup and diagnostics**, enter your measurements, select **Use measurements entered here**, and choose **Use these measurements**. The sample values are inactive until accepted. An optimizer layout is optional. See [setup](SETUP.md#measure-your-rig).

## The side screens are blank, swapped, or frozen

For separate displays, check that Windows is in extended desktop mode, the center screen is primary, and all three monitors run at the same native resolution. In Advanced setup, swap Unity side indices `1` and `2` if left and right are reversed. Restart the game after changing display mode. If a side window did not return after Alt+Tab, bring the game back to the foreground and wait a frame; the mod tries to raise both Unity secondary windows on focus return. Report a repeatable failure with the stage and screenshot.

For Single wide display, check that Windows and the game both report exactly three native panel widths and one panel height. Current bezel-corrected wide resolutions are not supported.

## Seams, field of view, or lighting look wrong

Check the physical width/height and left/right angles in Advanced setup. The field-of-view slider affects all three views; use **Reset field of view** to compare with measured geometry. Other camera or post-processing mods may change the source camera. For a stage-specific color fringe or vegetation artifact, compare the same location, time of day, and mode, and note whether the center image has it too.

## Tearing in Surround

Tearing is [still open](KNOWN-ISSUES.md) even with Unity VSync 1 on the tested wide output. VSync settings and update FPS alone cannot verify display scanout. The tested separate-display drive had no visible tearing. Close the game, return Windows to extended desktop mode, and use **Three separate displays** if you prefer that result. We have not isolated whether stock Surround tears on this rig.

## Collecting a useful report

Include the game build, mod version, UMM version, monitor resolutions/refresh rates, Windows display mode, output mode selected in the mod, and the stage/time of day. Attach a full three-screen screenshot if the defect is visual. The game's `Player.log` is under `%USERPROFILE%\AppData\LocalLow\Funselektor Labs\Art of Rally\`; the mod writes `status.json` and `render-diagnostics.json` under `%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\`. Review logs before sharing them because they can contain local paths or device details.
