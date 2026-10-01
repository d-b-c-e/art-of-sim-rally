# art of rally: unified setup candidate

## Current setup

The authoritative setup route is the [unified product candidate](UNIFIED-MIGRATION.md):
one package, one setup, one code load owner, and one package release version.
The candidate is **unreleased**. Source publication and offline tests do not qualify
its runtime behavior or make it an accepted replacement for installed binaries.

Build a local candidate with `tools/unified/package.ps1` using the retained exact
legacy packages to generate the migration hash catalog. This requires local game
build references; source CI does not produce a shipping package. Extract the
checked candidate ZIP, install Unity Mod Manager for Art first, close the game,
and use the ZIP's single `Install.bat` with an explicit game directory. Start with
its dry run:

```powershell
./Install.bat -GameDir 'D:/Games/artofrally' -DryRun
```

Direct UMM ZIP import is unsupported for this transactional candidate. The unified
installer backs up and verifies owned payloads, journals pending changes, and
commits an ownership receipt only after verification. Use its `Rollback.bat` for
interrupted migration and `Uninstall.bat` for receipt-owned removal. Changed or
unknown files block destructive operations. Settings and backups remain; never
delete entire mod directories. See the migration guide for the full transaction
and recovery contract.

Wheel settings remain `Mods/ArtOfSimRally/Settings.xml`; triple settings remain
`Mods/DbceTripleScreenArtOfRally/Settings.xml`. The metadata-only triple optimizer
bridge preserves its legacy ID, discovery path and canonical layout files. It
has no code load entry and is not a second installable product. It remains a second
visible UMM row; supported loader-version qualification is pending.

Only exact recognized legacy payload hashes can migrate automatically. Public
wheel 0.2.7 and triple 0.3.11 are cataloged. The maintainer's installed triple 0.3.12
has unknown exact source provenance and is refused if its payload is unrecognized.
His acceptance applies only to his installed wheel 0.2.7/triple 0.3.12 and rig,
not to the unified candidate. Physical FFB, rendering, seams, tearing, performance
and specific handoff checks remain separately qualified. A wide fallback is not
true triples. Existing diagnostic recording/replay stays separate from the unified
payload; no duplicate recorder is added.

## Legacy component routes (historical support only)

[Wheel 0.2.7](https://github.com/d-b-c-e/dbce-mods-art-of-rally/releases/tag/v0.2.7)
and [triple 0.3.11](https://github.com/d-b-c-e/dbce-triple-mod-art-of-rally/releases/tag/v0.3.11)
downloads, release metadata and installed settings remain unchanged. Their own
installers and the old `tools/game` joint installer are retained for historical
support; they are not the current unified setup recommendation. The old joint
installer was non-atomic and could leave earlier component changes applied after
a later failure. Keep exact prior packages for legacy recovery.

Component documentation remains reference material for those historical installs:
wheel [setup](../components/wheel/docs/SETUP.md),
[troubleshooting](../components/wheel/docs/TROUBLESHOOTING.md),
[deployment history](../components/wheel/docs/LOCAL-DEPLOYMENT.md);
triple [setup](../components/triple/docs/SETUP.md) and
[known issues](../components/triple/docs/KNOWN-ISSUES.md).
