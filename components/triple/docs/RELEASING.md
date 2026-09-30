# Release procedure

The repository can have a private GitHub release before its visibility changes. Publishing the repo and creating a GitHub release are separate actions. The current **0.3.11** build is a tested prototype with [open Surround tearing](KNOWN-ISSUES.md); do not describe it as fully qualified on other rigs.

## Source and license

1. Review `git status` and the full tracked-file list. Keep `artifacts/`, captures, decompiler output, installed game binaries, UMM binaries, `Settings.xml`, and personal layouts out of Git history and release assets. Check newly added documentation for machine paths, accounts, and private data.
2. Confirm the root `LICENSE` and version in `Info.json`, `adapter-manifest.json`, README, and installer instructions agree.
3. Confirm source builds from a clean clone when supplied with a local game path, with no sibling private toolkit checkout.

## Build and package

1. With the game closed, run the Release build and executable geometry tests from [BUILDING.md](BUILDING.md).
2. Run `tools/package/Package.ps1` from a clean source tree, using an unused output directory. Keep the generated ZIP and SHA-256 beside the test report.
3. Run `tools/testing/Test-Installer.ps1` against the extracted stage. It checks missing UMM, fresh install, upgrade, preserved settings/layout/other mods, damaged package rejection, and uninstall in a fake game folder.
4. Inspect the ZIP's exact contents. It must contain only our five mod files, installer, README, license, and package manifest. It must not contain a local desired layout, game DLLs, UMM, screenshots, or test artifacts.

## Attended game checks

On the intended game build, validate Off, Single wide display, and Three separate displays separately. Confirm the menu loads; a stage renders left/center/right in order; seams, color, vegetation, FOV, finish camera, Alt+Tab, and normal exit behave as expected. Note resolution, refresh, fullscreen mode, frame rate, visual tearing, and any screenshot/log evidence. Do not infer a visual pass from offline tests or Unity VSync values alone.

The current owner's attended checks support the separate-display mode and show much improved Surround visual parity. They do **not** close Surround tearing, other rigs, mixed-resolution monitors, all menus, replay, or photo mode. State these limits in release notes rather than hiding them.

## Publish

After the source and package are reviewed, commit and push the release source. Create the release with the mod ZIP and SHA-256 file, link this README and setup guide, and list the known limitations. Change GitHub visibility only when the owner explicitly chooses to publish the repository.
