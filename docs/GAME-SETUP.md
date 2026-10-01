# art of rally: one setup, independent components

This consolidation candidate keeps wheel and triple source under `components/wheel` and `components/triple`, preserving both histories. Existing release payloads and in-game mod IDs stay unchanged. No consolidated bundle has been published.

The wheel release is [**0.2.7**](https://github.com/d-b-c-e/dbce-mods-art-of-rally/releases/tag/v0.2.7). The released triple prototype is [**0.3.11**](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11); **0.3.12** source remains a separate unaccepted candidate. `game-release.json` identifies the exact released package manifests; setup never substitutes a newer source build for those artifacts.

For a normal single-component install, use the existing component ZIP's Install.bat. Steam discovery and an explicit game path remain available there. Both components require Unity Mod Manager; neither requires the other or Triple Screen Optimizer.

The candidate joint entry point requires an explicit game folder and both extracted package locations, with optional `-Components wheel` or `-Components triple`. It checks every selected package before invoking an installer. `-DryRun` verifies and prints a plan without installation; `-Uninstall` delegates owned-file removal. Close the game normally first. No script launches the game, opens a device, changes profiles or tests force.

```powershell
./tools/game/Install-Game.ps1 -GameDir 'D:/Games/artofrally' -WheelPackage 'D:/Downloads/ArtOfSimRally-0.2.7' -TriplePackage 'D:/Downloads/Triple-0.3.11' -DryRun
```

The existing installers preserve settings, measurements and other mods. They overwrite owned payload files without automatic backup or restoration. A failed or interrupted copy can leave a partially updated component. Joint setup is **not an atomic transaction**: if a later component fails, earlier successful changes remain and the receipt identifies them. Keep known prior packages. After resolving the write failure, manually reinstall the retained prior package or uninstall the affected component; settings remain separate. Joint uninstall requires intact pinned selected packages: a missing or damaged package is refused before any removal. Re-extract the exact release package first, or use the retained component Uninstall.bat according to its instructions. Do not delete the whole mod folder or settings. The pilot validates disposable fixture installs; it does not certify a new game build or current rendered/physical acceptance.

Wheel: [setup](../components/wheel/docs/SETUP.md), [troubleshooting](../components/wheel/docs/TROUBLESHOOTING.md), [exact deployment](../components/wheel/docs/LOCAL-DEPLOYMENT.md). Triple: [setup](../components/triple/docs/SETUP.md), [known issues](../components/triple/docs/KNOWN-ISSUES.md). Art's current wheel final artifact still needs its pending hardware checks; triple 0.3.12 needs slider-return/finish handoff and separate tearing measurement. A wide fallback is not true triples.
