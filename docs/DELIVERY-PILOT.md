# Art package delivery pilot, contract v1.0.0

The extracted unified package has exactly one root `delivery-manifest.json`,
`schema=dbce.game-delivery`, `schemaVersion=1`, `kind=package-delivery`. This is
package metadata, not runtime discovery. `tools/unified/New-DeliveryManifest.ps1`
generates it from staged binary hashes and the actual build's commit/tree/dirty
state; no static source-only file invents runtime artifact provenance.

The common proposal and fixture expectations are pinned in `tools/delivery/v1`.
Its reusable `delivery-validator.ps1` and strict JSON parser work on Windows
PowerShell 5.1 and PowerShell 7 without SDK, Python, game libraries or device code.
The generic schema checks are separate from `tools/unified/delivery-art.ps1`, which
checks Art's current acceptance, defaults, legacy mapping and actual operations.
Only the existing Art integrity adapter is implemented. Another game may reuse
the schema parser, then add its reviewed integrity and semantic mapping adapter;
`-MetadataOnly` explicitly omits integrity verification and never qualifies a package.

The descriptor is included in `PackageExtras` and hashed by the existing
`package-manifest.json`. It hashes neither itself nor the integrity manifest.
The external ZIP hash stays outside the archive. Delivery validator/parser/mapping
scripts also stay at the package root. None enter `OwnedPaths`, installed receipts,
`features.json`, optimizer scanning, canonical layout or device initialization.
The existing allowlist remains exact and rejects added private/unowned package bytes.
This gate is not a general detector for arbitrary secrets embedded in allowed text.

This metadata change uses a new local candidate identity `0.4.0-rc.3`; prior
`0.4.0-rc.1` and `0.4.0-rc.2` evidence/archives and all public releases remain immutable. No new
binary release is authorized. Fresh package builds still require local external
game/Unity/UMM references and never redistribute them. Vendored toolkit 0.15.0
hashes and native byte identity are retained; the upstream commit is not recorded
in the existing vendor pin and is explicitly null with a reason, not invented.
Toolkit release and native DLL versions remain distinct.

All six canonical feature IDs are present. Wheel, FFB, telemetry and triple are
implemented and packaged with this exact candidate's acceptance **pending**.
Anthony's accepted installed wheel0.2.7/triple0.3.12 rig is historical evidence only;
unknown installed triple source is not promoted to a known candidate identity.
Development Recorder/Replay source is implemented separately and unpackaged;
playback means offline force analysis, not game-input replay or physical output.
The shipping recording declaration has empty kinds/formats and explicit unsupported
notes. Source CI fixtures use inert DLL-named text, explicitly designated synthetic,
and cannot attest that a runtime binary was compiled.

All shipped feature defaults preserve existing settings. Source mapping confirms
fresh wheel input off, telemetry off, triple prototypes off, and legacy FFB on.
The inherited fresh FFB flag is disclosed, with disable guidance; adopting metadata
never enables it, resets settings or opens a device. Gameplay source is unchanged.

`tools/delivery/Test-Delivery.ps1` tests malformed/duplicate JSON, feature/default
and acceptance lies, stale/missing integrity coverage, unsafe paths, omitted
entrypoints, invented check flags, private added files, artifact provenance and
legacy descriptor conflicts. The source gate runs it with inert fixtures; local
release review additionally runs it and the unchanged 17-case installer suite
against the exact real stage and ZIP-extracted bytes. Installer operations stay
native; no common engine launcher, force output or input replay is introduced.

D02 native F-Zero fresh-folder, D03 retained OutRun repack and each game's native
repository mapping need those owners' fixtures. Existing Art regression covers
settings/other-mod retention, changed payload refusal, corrupt rollback backup,
copy failure, interruption, linked targets, duplicate owners and stock BAT paths.
The pilot does not newly certify exhaustive S04/S05 transaction-boundary failure
injection, S06 concurrent races, or a mocked running-game predicate. R01-R06
recording/session/signal gates remain external owners' work because those tools
are not shipped. These are explicit limits, not successful skipped fixtures.

The source tree is captured from `git rev-parse <commit>^{tree}` before compilation
and recorded independently in the existing `build.json` and package integrity
manifest. All delivery source roles must match both anchors' commit, tree and dirty
state. Synthetic fixtures record their generator snapshot in those same records
and remain explicitly inert. This is cross-record provenance consistency, not
compiler attestation, a signature, or proof against coordinated falsification of
all records. The external reviewed ZIP hash pins the selected package bytes.

Art's six capability sets and all three engine operation declarations are checked
exactly. Unsupported game-input replay/device playback and pending renderer
qualification cannot be promoted by changing and rehashing delivery metadata.
The three independent-review mutations plus extra-capability, missing-operation,
source-tree anchor mismatch and missing-anchor cases are explicit regressions.
