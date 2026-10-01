# DBCE mods for art of rally

The local [unified product candidate](docs/UNIFIED-MIGRATION.md) supplies wheel,
FFB, telemetry and triple-screen features in one installable package with one
code load owner and release version. It is unreleased; existing downloads below
remain available and unchanged. Build the candidate with `tools/unified/package.ps1`
and validate its migration using `tools/unified/Test-Installer.ps1`.

Independent [wheel](components/wheel/README.md) and [triple-screen](components/triple/README.md) components, with shared [setup](docs/GAME-SETUP.md) and [release policy](docs/RELEASE-STRUCTURE.md).

- [Wheel 0.2.7](https://github.com/d-b-c-e/dbce-mods-art-of-rally/releases/tag/v0.2.7): wheel/pedals, force feedback, cameras and telemetry.
- [Triple 0.3.11](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11): retained prototype release. Triple 0.3.12 source is unreleased and has pending attended checks.

Build and package each component from its own folder. Component versions, mod IDs and released ZIP layouts remain independent. This source reorganization does not change installed packages or establish hardware/rendering acceptance. The old triple repository is archived with its downloads retained.
