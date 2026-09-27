# Setup guide

Start with the [install steps](../README.md#install). This mod works without Triple Screen Optimizer or art of sim rally. It does require Unity Mod Manager (UMM) installed for art of rally.

## Check the installation

Launch the game through Steam, press **Ctrl+F10**, and find **DBCE triple-screen for art of rally 0.3.11** in UMM. The game folder should contain `Mods/DbceTripleScreenArtOfRally/Info.json` and three DLLs, with no extra `Mods/Mods` level. The mod starts **Off** on a fresh install.

If automatic game detection fails, Steam's **Manage → Browse local files** opens the folder containing `artofrally.exe`. Run this from the extracted ZIP, replacing the example path:

```powershell
.\Install.bat -GameDir "D:\Games\artofrally"
```

Drag-and-drop installation through UMM's Mods tab is also supported. Updating by either method preserves `Settings.xml` and any local `desired-layout.json`. Keep the extracted download if you want to run `Uninstall.bat` later. The uninstaller removes only this mod's DLLs and metadata, leaving settings, other mods, and UMM in place.

## Measure your rig

Open **Advanced setup and diagnostics** in the mod's UMM panel. Enter:

1. The native pixel width and height of **one** monitor, not the total Surround width.
2. The visible physical width and height of one panel in millimetres. Measure the lit image area inside the bezel.
3. Your eye distance from the center of the middle screen in millimetres.
4. Each side screen's angle relative to the center screen plane, in degrees. A straight three-screen row is 0°; inward-angled side screens use positive angles. Enter left and right independently if they differ.

Choose **Use these measurements**. The values shown initially are examples and do nothing until accepted. You can instead use an existing optimizer `desired-layout.json` as a measurement source and leave **Use measurements entered here** off. The mod copies neither the optimizer app nor its UI. The imported layout is optional and is read from `%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\desired-layout.json` or a staged copy beside the mod.

The mod uses these measurements for the three off-axis projections. Use the **Field of view** slider for a visual preference after setup; it moves all three projections together so seams stay aligned. **Reset field of view** restores the measured view. It does not change the saved physical measurements.

## Three separate displays

With the game closed, set Windows to **Extend these displays**. Make the center screen Windows primary and use the same native resolution on all three screens. Launch the game at that native center resolution. Choose **Three separate displays** in the mod and start a stage. The title and menus remain on the primary screen.

The tested Unity mapping was left `1`, center `0`, right `2`. If the side views are swapped, change **Left display index** and **Right display index** in Advanced setup. These are Unity indices and need not match the numbers shown by Windows. The side display windows remain activated until the game exits, so restart the game after switching output modes or correcting a display arrangement.

On the tested three-monitor rig this mode had aligned seams, stable vegetation, matched lighting, and no visible tearing during the attended drive. Other monitor mixes and replay/photo flows remain unverified.

## Single wide display

With the game closed, enable a single 3-panel-wide output such as NVIDIA Surround. Use exactly three native panel widths and one native panel height; for three 2560×1440 screens, that is **7680×1440**. Bezel-corrected widths are not yet supported by this renderer. Launch the game at the same wide resolution and choose **Single wide display** in the mod. It places three camera viewports side by side, each with its own off-axis projection.

Surround tearing remains unresolved on the tested rig even though Unity reported VSync 1. If it is distracting, try **Three separate displays** after closing the game and returning Windows to extended desktop mode. See [known issues](KNOWN-ISSUES.md).

## Updating, removing, and compatibility

Close the game before installation, updating, removal, or changing Windows display topology. To update, extract a new release to a fresh folder and run its `Install.bat`. To remove, run `Uninstall.bat` with the game closed. Your UMM settings and screen measurements remain for a later reinstall.

This mod was exercised alongside art of sim rally on one rig. It does not install or depend on that mod and does not modify its files. Other camera mods may change the source camera; report the other mod name and version with any conflict. For a bad image, choose **Off** in this mod and restart the game; [troubleshooting](TROUBLESHOOTING.md) has more checks.
