# Unified package and release policy

## Current release policy

The authoritative product direction is the [unified product candidate](UNIFIED-MIGRATION.md):
one installable package, one setup, and one package release version per game.
Internal wheel/triple source directories and provenance versions do not define
separate new product release streams. The stable package ID is
`dbce-mods-art-of-rally`; its source repository is
[the public Art repository](https://github.com/d-b-c-e/dbce-mods-art-of-rally).

`tools/unified/package.ps1` stages the local `0.4.0-rc.4` candidate as
`dbce-mods-art-of-rally-0.4.0-rc.4.zip`. This candidate is **unreleased** and has
no unified binary release tag. Source publication and CI do not publish binaries
or establish runtime acceptance. Future product releases use the single unified
version; no new wheel/triple/bundle release streams are recommended.

Freeze and verify an exact candidate archive before any separately authorized
binary publication. Preserve settings, ownership receipts and migration backups.
Keep the metadata-only optimizer bridge, its legacy adapter ID/path and canonical
layout contract; it is compatibility metadata, not a second product. Its second
visible UMM row and supported loader-version qualification remain explicit limits.
Unknown installed triple 0.3.12 payloads are not automatically adopted.

Record implementation, packaged contents and accepted scope separately. The
maintainer's rig acceptance of his installed wheel 0.2.7/triple 0.3.12 does not
accept the unified candidate or prove byte identity. Offline checks do not certify
physical FFB, displayed three-view rendering, seams, tearing or performance.
Existing diagnostic recording/replay is separate from the unified release payload.
No proprietary game/Unity/UMM assemblies, owner settings, recordings or credentials
belong in published source or packages. Source CI is not a blanket private-asset scan.

## Legacy component routes (historical support only)

Retain all existing tags, ZIP names, release metadata, download links and component
mod IDs. Public wheel 0.2.7 and triple 0.3.11 downloads remain unchanged. The old
triple repository remains archived with its releases available. Preserve the old
repository-name redirect; do not reuse `art-of-sim-rally`.

`game-release.json`, root `tools/game`, and component build/package commands retain
the historical independent-package pilot and exact artifact pins. That inventory
is not the authoritative unified release manifest, nor a current recommendation
for separate installs or new component streams. The historical pilot's unaccepted
0.3.12 wording does not override acceptance of the maintainer's installed binaries;
their exact triple source identity remains unknown. See [current setup](GAME-SETUP.md)
and the migration guide for the unified route and qualification limits.
