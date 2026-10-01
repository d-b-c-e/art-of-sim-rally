# Unified game package delivery contract

Status: PROPOSED v1.0, 2026-10-01. No consumer or generator has adopted this specification. Art is the intended first pilot. This specification adds delivery metadata and validation; it authorizes no runtime feature, installation, repository rename, release, toolkit upgrade, or optimizer change.

## Scope and document identity

Recommended filename: `delivery-manifest.json`, exactly once at the extracted package root. Recommended discriminator: `schema: "dbce.game-delivery"`, `schemaVersion: 1`, `kind: "package-delivery"`.

This is a package descriptor, not the installed ownership receipt, build receipt, file-integrity manifest, source readiness document, optimizer discovery manifest, or runtime status. Those existing formats retain their identities and locations. Package roots may retain legacy product descriptors for compatibility; their overlapping identity/version/feature claims must agree with this descriptor. Do not add this format to optimizer scanning or automatically copy it into a legacy discovery path.

The existing format that inventories package bytes MUST hash `delivery-manifest.json`. This descriptor MUST NOT hash itself or the enclosing integrity manifest, avoiding a cycle. The external archive SHA-256 is recorded in the release review, outside the archive. Neither hashes nor a clean-source assertion are a compiler attestation.

All fields below are required unless explicitly optional. Unknown top-level fields are rejected in v1; owner-specific additions belong in optional `extensions`, keyed by a namespace, and must not override standard fields. Duplicate JSON keys, duplicate IDs, wrong JSON types and unsupported schema versions are rejected. JSON booleans cannot be replaced with strings or numbers.

## Field definitions

| Field | Definition |
|---|---|
| `schema`, `schemaVersion`, `kind` | Exact constants above. |
| `packageId` | Stable delivery identity matching `^[a-z0-9]+(?:-[a-z0-9]+)*$`. Never infer from ZIP, loader, repository or display name. |
| `gameId` | Stable game identity with the same syntax; distinct titles/editions remain distinct. |
| `displayName` | Nonempty player-facing product name. |
| `version` | One explicit product release version, SemVer 2.0 syntax without a leading `v`. Existing component/dependency versions remain independent. |
| `channel` | `candidate`, `prerelease`, or `stable`. A plain version may be a candidate; channel is not inferred from prerelease suffixes. |
| `platform` | Object: `os: "windows"`, `architecture: "x86"` or `"x64"`. |
| `repository` | Object: `canonicalUrl` (absolute HTTPS URL), `visibility` (`public`/`private`), `mappingStatus` (`proposed`/`configured`/`verified`), `previousUrls` (array). A local remote is not proof of a server rename. |
| `provenance` | Object defined below. |
| `integrity` | Object: `manifestPath` (existing package-relative integrity manifest), `format` (nonempty legacy format identifier). Every package byte must be covered by that verifier's allowlist, excluding only its own integrity/checksum records according to its documented format. |
| `loadOwners` | Array of `{ownerId, mechanism, role}`; `role` is `game-host`, `mod-loader`, or `offline-companion`. One active owner of each hook/device role; internal assemblies do not imply separately installable products. Metadata-only legacy rows are described under compatibility, not active owners. |
| `features` | Exactly six entries with distinct canonical IDs, defined below. Cameras/BS Deluxe and other optional features may be described in extensions without replacing the six. |
| `setup` | Object defined below. |
| `compatibility` | Object: `legacyDescriptors` (relative paths), `retainedIdentities` (array of `{kind,value}` for loader/config/receipt/adapter identities), `settingsPolicy: "preserve-existing"`, `capturePolicy: "preserve-existing"`, `optimizerDiscovery: "not-adopted"`. Optional `notes` array. |
| `recording` | Object defined below, always present even when unsupported. |
| `extensions` | Optional namespace-keyed object; no implicit runtime effect. |

Package-relative paths use `/`, are nonempty and bounded, and reject absolute/UNC/drive paths, `:`, empty/`.`/`..` segments and reparse/symlink escapes. They may point only inside the extracted package. Installed target roots are enumerated separately under setup, never confused with package paths. HTTPS repository/documentation links are not filesystem paths. No private absolute owner paths, credentials, user configuration, recordings, ROMs, game assemblies, generated proprietary interop, or private build inputs enter public metadata/payloads.

## Version and provenance

`provenance.sources` is an array of `{role, repositoryUrl, commit, tree, dirty}`. Roles include `runtime`, `installer`, `packaging`; one source entry may cover several roles through distinct entries with identical pins. Commit/tree are full 40-hex Git IDs. `dirty` is a boolean asserted at build time. Source-only evidence without a binary may use a staging review outside this package contract; do not fabricate runtime provenance.

`provenance.artifacts` is an array of `{path, sha256, origin, sourceRoles}` for every shipped executable/native/managed binary. SHA-256 uses lowercase 64-hex. `origin` is `fresh-build`, `retained-binary`, or `upstream-binary`; `sourceRoles` references source roles or an explicit dependency ID. Repacking must retain original runtime provenance and record the new installer/packaging source separately. If an existing artifact's source identity is genuinely unknown, adoption is blocked pending owner review; do not relabel it as a fresh build.

`provenance.dependencies` is an array of `{dependencyId, version, status, sourceCommit, files, overrideOf}`. `status` is `released` or `unpublished-override`; `sourceCommit` and `overrideOf` may be null only with an explicit `reason`. `files` is an array of `{path,sha256}` for included dependency bytes. Non-shipped reference dependencies have empty `files` and explicit `distribution: "external-reference"`; shipped ones have `distribution: "included"`. Preserve existing toolkit/native version distinctions and reviewed input-only overrides. An unpublished recording DLL must not be represented as belonging to a released toolkit merely because it was built on that base.

One exact immutable artifact is selected per release milestone. Changing installer, docs, or payload hashes requires a new candidate identity/version or an explicitly versioned repack reviewed by the coordinator; never replace historical archives under the same release asset identity. Historical tags/releases/redirects remain untouched. Legacy ZIP filenames and native fork repository names need not change.

## Six canonical features

Canonical IDs: `wheel`, `ffb`, `telemetry`, `triple`, `recording`, `playback`.

Each entry is `{featureId, implemented, packaged, acceptance, defaultState, capabilities, limitations}`. `implemented` and `packaged` are separate booleans. Packaged=true requires implemented=true and an actual inventoried implementation in this artifact. `capabilities` and `limitations` are arrays of nonempty strings. Unimplemented features have empty capabilities and default `unavailable`; research source excluded from the package does not count as packaged implementation.

`acceptance` is `{status, scope, evidence}`. Status is `not-assessed`, `pending`, `partial`, `accepted`, or `not-applicable`. Scope is a nonempty concise statement of exact candidate/device/game-build/topology boundaries. Evidence is an array of `{reference, identity, scope}`: references are safe package paths, absolute HTTPS links, or opaque review IDs; identity pins the relevant binary/source/settings/session hashes. Empty evidence cannot accompany partial/accepted. Historical acceptance may be cited with an explicitly historical scope but cannot promote this candidate to accepted. Unimplemented or unshipped features use not-applicable for package acceptance.

`defaultState` is `unavailable`, `off`, `on`, or `preserve-existing`. Unpackaged features must be unavailable. Packaged features cannot be unavailable; experimental FFB remains off or preserve-existing with explicit opt-in/disable policy. Adoption MUST NOT enable force, reset controls, modify saved feature choices or promote acceptance. A fresh-install default and an upgrade's retained setting may differ; record the fresh default in optional `freshInstallDefault` (`off`/`on`) when defaultState is preserve-existing.

Do not reintroduce a generic `available` boolean: it obscures implementation, distribution and acceptance. If a legacy descriptor requires it, its meaning must remain documented and tested by that game's mapping fixture, rather than inferred globally.

`playback` means an explicitly typed software playback capability, not necessarily driving-input replay. Capability examples: `offline-force-analysis`, `game-input-replay`, `telemetry-inspection`. Inspection without replay processing does not establish playback. Feature acceptance never establishes physical force output or full game replay from a diagnostic session.

## Setup and recovery declaration

`setup` is `{deliveryMode, entrypoints, target, operations, recovery, preservation}`.

`deliveryMode`: `transactional-install`, `retained-installer`, or `fresh-folder`. `target`: `{kind, legacyRootIdentity}` where kind is `game-directory`, `per-user-app`, or `new-folder`; identity describes the stable target/config root without embedding an owner's private path. F-Zero may retain a native fresh-folder launcher with no automated existing-install modification.

`entrypoints` is an array `{path, purpose, argumentStyle, startsGame, opensDevices}`. Purpose is `primary`, `legacy-compatible`, or `recovery`; argumentStyle documents the actual existing flags rather than forcing common command syntax. Files must exist and be integrity-covered. StartsGame/opensDevices are truthful booleans: validation never invokes a launcher merely to inspect metadata. F-Zero's Setup.cmd opens its existing launcher; declare that side effect and preserve its native design.

`operations` contains exactly `check`, `install`, `update`, `uninstall`, `rollback`. Each is `{support, entrypoint, arguments, notes}`, with support `automated`, `manual`, or `unsupported`; entrypoint is null for manual/unsupported. Check means declared no-target-write preflight, not ordinary install with a guessed flag. Package-only hash verification is separately documented in recovery/notes if it does not validate the target. Do not advertise CLI operations the retained script does not implement.

`recovery` is `{ownershipModel, changedOrUnownedPayload, caughtFailure, interruption, backupVerification}`. OwnershipModel: `receipt-hashes`, `recognized-legacy-hashes`, `runtime-backup`, or `none-fresh-folder`. ChangedOrUnownedPayload: `refuse`, `manual-review`, or `not-applicable`. CaughtFailure/interruption: `automatic-verified-rollback`, `explicit-verified-rollback`, `manual-backup-recovery`, or `not-applicable`. BackupVerification: `hashes`, `manual`, or `not-applicable`. These declarations are truthful capability statements, not a claim that every existing implementation meets the next acceptance gate.

`preservation` declares `settings: "byte-preserved-existing"`, `captures: "retained"`, `otherMods: "retained"`, `sharedLoader: "retained-by-default"`, and an array `explicitExceptions`. Seeding missing settings is allowed only when declared. Existing loader prerequisite normalization and explicit user-data purge switches must be documented as scoped exceptions; no metadata adoption may add or silently invoke them.

Safety gates for automatic updates: reject changed/unowned replacement/removal before mutation; verify receipt ownership and backup bytes; reject path escapes/links; retain newer settings; refuse a running game without killing it; preserve conflicting hooks; leave diagnostic recovery evidence after failure/interruption. A legacy implementation that lacks a gate must declare manual-review limitations and cannot claim safe automatic update conformance until its owner fixes and tests it. Unrelated mods/shared loader are never recursively deleted. Archive history remains intact.

## Recording semantics

`recording` is `{captureKinds, playbackKinds, formatIds, bounded, completionPolicy, identityPolicy, privacy, physicalOutput}`. Kinds are arrays: captureKinds from `input`, `telemetry`, `configuration`, `state`, `force-requests`; playbackKinds from `inspection`, `offline-force-analysis`, `game-input-replay`. No formats are required to be identical across engines. Unsupported recording/playback uses empty arrays, bounded=false and explicit unsupported notes.

`formatIds`: array of `{id,version,adapterId}`; retain JSONL/F-Zero snapshot/input formats and original adapter ownership. `bounded`: boolean; optional `limits` states exact duration/size/sample caps or the existing diagnostic recorder's limitations. `completionPolicy` and `identityPolicy` are nonempty strings describing incomplete-file handling and exact source/game/settings/artifact checks, including explicit source-mismatch analysis modes. `privacy: "local-private-by-default"`; `physicalOutput: "forbidden-during-offline-playback"`. No contract adoption introduces automatic corpus upload, a recorder, input injection, or physical actuation.

F-Zero input/WRAM replay with a ROM/snapshot identity is distinct from iRacing/Woden/DRIVE telemetry sessions. Art's excluded diagnostic recorder is not falsely promoted to shipped recording. Externally supplied normalized toolkit replay tools remain external references. Toolkit `SignalSample` unit/source/quality/age distinctions are retained; unavailable values cannot become measured zero, and OCR cannot be relabeled game-memory telemetry.

## Adoption entrypoints and touchpoints

Paths below are relative to the named candidate root. Snapshot audit heads: Art d706d0a; iRacing d028520; Woden 14740fd; DRIVE ab46ae6; Sonic 394906c; OutRun 7969aca; F-Zero c3ba5fd. Optimization/probe branches are separate from F-Zero's package baseline.

| Game / candidate root | Exact authoritative generator | Existing verifier/installer and migration touchpoints |
|---|---|---|
| Art / task-8/art-ci-candidate | `tools/unified/package.ps1` | `tools/unified/verify.ps1`, `install.ps1`, `Test-Installer.ps1`; add descriptor to PackageExtras/integrity allowlist, not Installed OwnedPaths; preserve features.json, bridge Info/manifest, adapter ID and receipt/journal. Fix root README, GAME-SETUP and RELEASE-STRUCTURE authority first. Old tools/package route remains historical. |
| iRacing / task-6/iracing-ci-candidate | `components/wheel/tools/package/package.ps1` | Generates root game-product.json/Setup.ps1 then New-ReleaseManifest; update ReleaseFiles.ps1 verifier and installer fixtures; retain receipt product IRacingArcadeWheel/config/native filenames. Add recording/playback classifications. Resolve replacement ownership preflight before automatic-update conformance. |
| Woden / task-5/woden-unified | `tools/game/Stage-Package.ps1` | `tools/game/Verify-Package.ps1`; `components/wheel/tools/Manage-Install.ps1` and Test-ManageInstall.ps1. Add root descriptor to stage allowlist only, not legacy plugin payloadNames; preserve WodenWheel-install receipt. Resolve super-woden package ID versus woden repository mapping; create package-local guide and freeze/archive stage in a separately reviewed release step. |
| DRIVE / task-10/drive-unified-candidate | `tools/Package-Player.ps1` (calls Package-Dev.ps1) | `installer/Common.ps1` ReadPackage allowlist, Install.ps1, Uninstall.ps1, tools/Test-PlayerPackage.ps1; retain installed game-mod.json and legacy receipt product/config/recording roots. Descriptor remains package-root metadata. |
| Sonic / task-9/sonic-unified-candidate | `tools/game/Build-Package.ps1` (calls scripts/Build-Package.ps1) | `tools/game/Setup.ps1` Read-Package, Test-Installer.ps1; keep package.json, root game-product.json, app-only install-manifest receipt and update journal. Do not install descriptor into app ownership unless separately justified; retain unavailable acceptance versus offline wheel implementation. |
| OutRun / task-13/outrun2006-candidate | `tools/Package-WheelSettings.ps1` | `tools/Install-WheelSettings.ps1` and tools/tests/Test-ProductPackage.ps1, Test-WheelInstall*.ps1; root product.json/package-manifest retained. Add explicit product version and preserve runtimeSourceCommit versus installer/packaging commits. Proxy identity alone is not receipt ownership; declare/manual-review gap before safe automatic-update conformance. No Redux payload import. |
| F-Zero / task-7/fzero-unified-candidate | `tools/stage_unified.py` then `tools/package_unified.py` | package_unified.py SOURCE_FILES/allowlist/verify, tests/test_unified_package.py; preserve dbce.fzero-build receipt, dbce.fzero-package integrity manifest, VERSION and stock-only/ROM-free boundary. Fresh-folder/manual recovery remains supported exception; retain fork identity, companion and launcher. Historical make_release.py is not unified generator. |

Repository normalization must be explicit: retain native FZeroSNESRecomp/OutRun fork identity; do not require dbce-mods naming. Art/iRacing configured URLs are not proof of remote completion. Woden's current package ID may remain stable once its differing repository mapping is approved. Sonic/DRIVE proposed renames require independent owner/coordinator sequencing, not manifest publication as proof.

## One milestone, sequential adoption

1. Coordinator freezes this contract plus fixture expectations with existing owners; no seven independent schema designs. Resolve canonical game/package/repository mapping and feature semantics before code changes.
2. Art owner pilots an additive root descriptor, independent verifier and legacy mapping fixtures. No optimizer adoption or runtime changes. Run existing exact-package installer tests unchanged plus new fixtures.
3. Review Art's generated bytes/schema and safety declarations; freeze shared definitions and one validator/fixture dataset in the separately owned shared toolkit or agreed contract home. Do not copy device/runtime implementations into games.
4. Each existing game owner adopts that exact version in its listed generator/verifier, one at a time; coordinator verifies no runtime/settings/toolkit changes. Existing receipts/descriptors/history remain supported.
5. Require per-game fixture results and one frozen artifact identity. Record unmet safety gates as blockers rather than manufactured capabilities. Publish/deploy only through existing authorization and handoff sequence.

No adoption is currently implemented by this coordination task. This document is the stable proposal for review, not a validated schema library or approved release.
