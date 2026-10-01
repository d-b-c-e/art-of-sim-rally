# Unified Art product candidate

Local, unreleased candidate based on `09c9b20d750cdfe9c5c57d03bcc820462ba189b7`.
One package ID (`dbce-mods-art-of-rally`), version (`0.4.0-rc.3`), and installer
provide wheel input, FFB, telemetry, and triple-screen features. This proposed
version is a migration candidate, not a published successor or runtime acceptance.
Gameplay is in maintenance pending feedback; this change does not retune forces,
bindings, telemetry, projection, or game physics.

## Load and settings ownership

Keep the existing `ArtOfSimRally` UMM ID and directory. Its packaged Info.json
selects `ArtOfSimRally.Mod.UnifiedEntry.Load`. This owner invokes the existing wheel
entry and an internal triple feature wrapper, then dispatches their update, save,
toggle, unload, and settings UI callbacks. The triple feature receives an
unregistered ModEntry with its existing ID and storage path; it cannot become a
second UMM load owner. Wheel settings remain `Mods/ArtOfSimRally/Settings.xml`;
triple settings remain `Mods/DbceTripleScreenArtOfRally/Settings.xml`.

The legacy triple directory retains Info.json with an empty AssemblyName and
EntryMethod, and its existing adapter manifest. UMM supports assembly-free
metadata entries: this compatibility row has no code, hooks, or settings page.
It remains a second visible UMM row. Its assembly-free behavior was inspected in
the locally installed loader; qualification across supported UMM versions is pending.
The triple implementation DLL and its geometry/protocol dependencies ship beside
the unified owner, not beside the compatibility metadata. The two original
component implementations remain separate internal assemblies and retain their
component provenance. There is one shipping package and code load entry point.

Both installer and runtime load guard refuse duplicate owner metadata. Runtime
also refuses a legacy triple implementation DLL or a pending migration journal
before wheel initialization, so no FFB/device initialization precedes that guard.
The metadata bridge is not a separately installable feature product.

## Optimizer compatibility

Preserve adapter ID `dbce-triple-mod-art-of-rally`, adapter version `0.3.12`, legacy
discovery paths, layout contract v1, and the canonical per-user root
`%LOCALAPPDATA%/DBCE/TripleScreen/games/art-of-rally`. Existing request and status
filenames are `desired-layout.json` and `status.json`. The optimizer can continue
staging identical layout bytes beside the legacy discovery metadata. Neither setup
nor optimizer migration edits wheel settings or canonical layout files.

Installed `features.json` follows the shared proposal's package/feature identity
separation, but explicitly marks itself a local candidate. The optimizer has not
adopted this discovery schema; this task makes no optimizer changes. The legacy
reader's version/hash/fresh-frame checks remain as implemented, including its
Surround-compositor requirement. Package/session identity and topology-aware
unified discovery require coordinated future consumer review. No capability or
successful export here proves displayed three-view rendering.

## Installer transaction

Use `tools/unified/package.ps1` to produce one checked ZIP, then use its single
Install.bat with an explicit game folder. Direct UMM ZIP import is unsupported for
this transactional candidate. DryRun validates payload and ownership without writes.

Old installers did not create ownership receipts. Migration therefore recognizes
only exact payload hashes inventoried from intact retained legacy packages at
packaging time. Known public wheel 0.2.7 and triple 0.3.11 are included in the
initial candidate. Missing legacy files can recover from a recognized partial
install; changed or unknown files refuse automatic migration. Catalog generation
never executes scripts from the old packages.

Before mutation, setup saves prior owned payloads, existing local settings/layout
bytes, and any previous unified receipt under a unique backup ID. A persistent
pending journal records every allowed before/after hash. After copying/removing
only the fixed ownership inventory, setup verifies every installed hash and commits
the receipt. Caught copy errors restore prior owned bytes and prior receipt.
Interrupted setup refuses subsequent installation until explicit rollback. Rollback
preflights target and backup hashes; user-modified payloads block destructive
recovery. Settings/layout backups are retained for manual recovery but are never
automatically restored over settings the user may have changed since installation.
Uninstall removes only receipt-owned payloads. Backups and user settings remain.

All target paths, including receipt/backup paths, are bounded to the selected game
root and reject reparse points. No recursive game/mod-directory removal occurs.
The installer refuses a running game and never launches it or opens devices.

## Evidence and acceptance limits

Run `tests/UnifiedLifecycle` for load-guard and callback cleanup cases, existing
wheel regression suites for retained behavior, triple executable geometry/protocol
checks, and `tools/unified/Test-Installer.ps1` against the exact staged package.
Installer fixtures use real Windows PowerShell in disposable game directories,
including spaces/brackets, migration from each component and both, repeat install,
uninstall, rollback, corrupt/changed files, copy failure, interrupted recovery,
and linked targets. Preserve resulting logs outside Git.

The maintainer reported overall acceptance of his installed wheel 0.2.7/triple 0.3.12
combination. That applies only to his rig and those installed binaries. The exact
installed triple 0.3.12 source revision is unknown; this candidate must not claim
byte equivalence or automatically adopt an unrecognized installed DLL. The older
specific hardware, slider-return/finish-handoff, seam, tearing, and performance
cases remain separately tracked. Offline gates do not replace them.

Existing recorder/replay and private corpus intake are reused, not duplicated or
included in the release payload. Historical releases, tags, downloads, settings,
and source history remain unchanged. Source publication follows independent review
and explicit authorization. No new binary release, installed changes, game launch,
device operation, or display change belongs to this milestone.
