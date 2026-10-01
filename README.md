# DBCE mods for art of rally

The [unified product candidate](docs/UNIFIED-MIGRATION.md) supplies wheel,
FFB, telemetry and triple-screen features in one installable package with one
code load owner and release version. It is **unreleased**; previous rig acceptance
applies to the maintainer's installed binaries, not this unified artifact.

Use the [current unified setup](docs/GAME-SETUP.md) and
[current unified release policy](docs/RELEASE-STRUCTURE.md). Build a local candidate
with `tools/unified/package.ps1` and validate its migration with
`tools/unified/Test-Installer.ps1`. Supported UMM versions and physical/rendered
acceptance remain pending. Legacy optimizer metadata and existing settings are
preserved. Diagnostic recording/replay remains separate from the unified payload.

[Source-only CI](docs/SOURCE-CI.md) checks pure managed suites, geometry/protocol,
disposable installer policy and documentation consistency. It does not build or
publish releases, certify runtime acceptance, or scan all future private assets.

The [package delivery pilot](docs/DELIVERY-PILOT.md) adds validated package-only
metadata; it does not change runtime discovery or promote UAT.

## Legacy component routes (historical support only)

- [Wheel 0.2.7](https://github.com/d-b-c-e/dbce-mods-art-of-rally/releases/tag/v0.2.7): retained wheel/pedals, force feedback, cameras and telemetry download.
- [Triple 0.3.11](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11): retained prototype download; the old repository is archived.

These downloads, tags and release metadata remain unchanged. The
[wheel](components/wheel/README.md) and [triple](components/triple/README.md)
folders retain internal source, provenance and historical component guidance;
they are not separate new product release recommendations. Installed triple
0.3.12 source provenance is unknown, and unrecognized payload migration is refused.
