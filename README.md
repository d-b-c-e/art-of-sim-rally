# art of rally triple-screen

Angle-correct left, center, and right views for [art of rally](https://store.steampowered.com/app/550320/). Use one wide NVIDIA Surround display or three separate Windows displays. The mod includes its own screen setup and field-of-view control; **Triple Screen Optimizer is optional**.

**[Download 0.3.11](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11)** · [Setup guide](docs/SETUP.md) · [Known issues](docs/KNOWN-ISSUES.md) · [Changelog](CHANGELOG.md)

Version **0.3.11** is a playable prototype tested on the Steam game build **1.5.8b** with Unity Mod Manager (UMM) **0.27.0**. The separate-display drive had aligned seams, stable vegetation, matched lighting, and no visible tearing on the tested rig. A later Surround drive showed much better vegetation and lighting with the viewport renderer, but **tearing remains an open issue in Surround**. See [known issues](docs/KNOWN-ISSUES.md) before installing.

The current source is preparing **0.3.12** with an **Override field of view** toggle. Turn it off to follow the game's live camera FOV on all three screens; turn it on to use the saved slider value. The attended Surround drive confirmed the game-FOV mode and aligned seams. Switching back to the saved slider value still needs an attended check. This candidate is not in the 0.3.11 download.

## Install

For the 64-bit Windows game. You need art of rally and UMM 0.27.0 or newer; no SDK, optimizer, or other mod is required to play.

1. Close the game. Download and extract [Unity Mod Manager](https://www.nexusmods.com/site/mods/21), run `UnityModManager.exe`, select **Art of Rally**, and click **Install** if you have not already.
2. Download **DbceTripleScreenArtOfRally-0.3.11.zip** from the [release Assets](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11). Choose the mod ZIP, not GitHub's source-code ZIP.
3. Extract the whole ZIP and double-click **Install.bat**. It finds Steam libraries, verifies the files, and preserves existing mod settings. You can also drag the ZIP onto UMM's Mods tab.
4. Launch through Steam and press **Ctrl+F10** to open UMM. Find **DBCE triple-screen for art of rally**, open its settings, and choose an output mode.

The installer also supports a custom folder: `Install.bat -GameDir "D:\Games\artofrally"`. Updating is the same as installing. To remove the mod, close the game and run `Uninstall.bat`; saved measurements, other mods, and UMM remain. [Detailed setup](docs/SETUP.md) covers both display modes and a source build.

## Set up your screens

In **Advanced setup and diagnostics**, enter one panel's pixel width/height and visible physical width/height, your eye distance, and the angle of each side screen. Select **Use these measurements**. The example values in the fields are inactive until you accept them. An existing optimizer layout can supply measurements instead; the optimizer itself is not needed.

| Mode | Windows setup | What the mod draws |
|---|---|---|
| **Off** | Any | The game's normal camera. |
| **Single wide display** | One display exactly three panels wide, such as NVIDIA Surround | Three camera viewports across the wide output. |
| **Three separate displays** | Extended desktop, equal native resolutions, center monitor set as Windows primary | A camera on each Unity display. |

The **Field of view** slider changes all three views together. **Reset field of view** returns to the entered measurements. Set or change the Windows display mode while the game is closed. In separate-display mode, Unity keeps the side windows activated until the game exits, so restart after changing modes. The tested display mapping was center `0`, left `1`, right `2`; Unity may number your side displays differently.

The mod also offers an optional center-menu setting for wide output. Gameplay, stage-finish cinematics, and FOV continuity were exercised on the owner's rig. Replay, photo mode, every menu, other monitor arrangements, and long-term performance still need broader testing.

## Known issue: Surround tearing

The attended Surround run showed visible tearing even though Unity requested VSync 1 at 7680×1440 and roughly 59 updates/s. Those values do not prove synchronized scanout across the panels. No visible tearing was reported in the separate-display drive. The comparison with stock Surround was not completed, so the cause is still undetermined. [Known issues and workarounds](docs/KNOWN-ISSUES.md).

## Build from source

The source is independent of the private toolkit. You need .NET 8 SDK, a local game installation, and UMM installed for that game because the build references their assemblies without copying them into this repository. Set `ART_OF_RALLY_DIR` to the folder containing `artofrally.exe`, then run:

```powershell
dotnet build Dbce.TripleScreen.sln -c Release
dotnet run --project tests/Dbce.TripleScreen.Core.Tests -c Release --no-build
```

Alternatively, pass `-p:GameDir="D:\Games\artofrally"` to the build. To produce a checked release ZIP, run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/package/Package.ps1 -GameDir "D:\Games\artofrally"`. [Building and packaging details](docs/BUILDING.md).

## Project and support

- [Setup](docs/SETUP.md) and [troubleshooting](docs/TROUBLESHOOTING.md) for players.
- [Known issues](docs/KNOWN-ISSUES.md) and [release procedure](docs/RELEASING.md).
- [Architecture](docs/architecture.md), [research](docs/research.md), and [attended experiments](docs/experiments/008-attended-separate-display-check.md) for contributors.

The release package contains only this project's DLLs, metadata, license, and installer. It does not redistribute game or UMM assemblies. Tested alongside art of sim rally on the owner's rig; neither mod is required by the other.

## License

MIT. See [LICENSE](LICENSE).
