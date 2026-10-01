# Delivery contract acceptance fixtures proposal

Status: PROPOSED; no new test implementation or runtime execution. Companion: SPEC.md, dbce.game-delivery schemaVersion 1. Fixtures use inert dummy payloads and disposable game/app trees. No devices, games, ROMs, profiles, network credentials or internal task databases.

## Shared descriptor fixtures

| ID | Fixture / expected result |
|---|---|
| D01 | Complete Art-shaped candidate with six features, original loader/adapter identities and optimizerDiscovery=not-adopted: accept. |
| D02 | Minimal fresh-folder native F-Zero descriptor, explicit manual existing-install recovery and executable launcher side effects: accept without requiring an installer/ownership receipt. |
| D03 | x86 OutRun retained-binary repack with distinct runtime and installer/packaging full commits, explicit version and manual ownership limitation: accept metadata; automatic-update safety gate remains blocked. |
| D04 | Missing/wrong schema/kind/version, duplicate JSON keys, duplicate feature/source-role IDs, unknown standard fields, boolean strings/numbers: reject before package or target writes. |
| D05 | Missing one canonical feature, extra canonical entry, wheel-input/forceFeedback/tripleScreen used as standard IDs: reject. Optional camera/Deluxe information in namespaced extension: accept. |
| D06 | Packaged true / implemented false; unshipped default on; implemented false with advertised capability; partial/accepted without evidence: reject. |
| D07 | Fresh candidate cites historical rig acceptance as exact-candidate accepted: reject mapping/semantic gate. Pending with explicit historical notes: accept. |
| D08 | Changed descriptor bytes without integrity-manifest update; conflicting legacy identity/version/features; descriptor omitted from integrity allowlist: reject. |
| D09 | Unsupported schema present: reject this descriptor; no fallback to a stale descriptor. Descriptor absent in intact recognized legacy package: existing legacy verifier remains usable. |
| D10 | Path traversal, drive/UNC/absolute/colon/empty/dot segment, duplicate normalized/case-insensitive file paths, junction or symbolic-link escape: reject. |
| D11 | Repack labels retained runtime fresh-build or changes original runtime pin; toolkit override labeled released base; malformed hash/short source IDs: reject. |
| D12 | Private source/recording/settings/ROM/game binary appears in public package, or a package instruction points into excluded source tree: reject release gate. No source/game execution to inspect it. |
| D13 | A declared entrypoint or docs file is missing/unhashed; check claims no writes but delegates install: reject capability gate. |
| D14 | Native fork repo name differs from package ID with explicit reviewed mapping: accept. Proposed/configured rename represented as verified without evidence: reject promotion. |

## Installer behavior fixtures (existing owners implement only applicable cases)

| ID | Disposable scenario / expected result |
|---|---|
| S01 | Existing settings, bindings, captures, unknown user files, other mod and shared loader sentinels: hashes remain unchanged after install/update/uninstall/recovery, except explicitly requested documented exception. |
| S02 | Unknown file or changed prior receipt-owned payload at a replacement/removal target: fail before mutation; retain every sentinel. Record nonconformance for retained installers until fixed. |
| S03 | Damaged backup/receipt, wrong root, wrong backup ID or changed current runtime: refuse destructive rollback before first write. |
| S04 | Inject copy failure after each transaction boundary: verified restoration or truthful recovery-required result; preserve receipt and newer settings. |
| S05 | Simulated interruption after backup, journal prepare, partial copy, receipt commit: declared recovery path works, subsequent unsafe update refuses; do not kill a live worker/game. |
| S06 | Changed payload introduced between preflight and write/rollback: preserve external bytes and report recovery-required; no destructive race overwrite. |
| S07 | Running-game predicate mocked true, duplicate hook owner or legacy renderer conflict present: refuse before device initialization/write; no forced kill/removal. Art metadata-only bridge remains permitted. |
| S08 | Native fresh-folder package: verify extraction and package hashes in new folder; original installation/saves/ROM/BS data remain untouched. No expectation of an automatic upgrade transaction. |
| S09 | Stock PowerShell 5.1 BAT/PS entrypoints with spaces/brackets in roots; documented flags reach same retained installer. Check mode is tested for no target writes. |
| S10 | Explicit settings purge remains separately requested and documented; standard uninstall preserves settings/captures/backups. Do not invoke purge during normalization fixtures against real data. |

## Feature/recording mappings by game

| Game | Required mapping assertions; acceptance stays scoped |
|---|---|
| Art | Six entries; wheel/FFB/telemetry/triple implementations packaged, each with exact-candidate acceptance limits. Recording tooling excluded from shipping payload remains unpackaged; classify replay tooling separately. Keep bridge/discovery identity unchanged. |
| iRacing | Wheel/FFB/telemetry and bounded recording actually packaged. Offline force analysis is playback capability only if its tools are shipped; otherwise implemented/unpackaged with an external reference. Triple remains unshipped; no invented request/status paths. |
| Woden | Wheel/FFB/telemetry and recording classified independently; prepared-before-launch/Stop behavior accurately described. Determine whether offline analysis tool ships before marking playback packaged. Triple probe excluded; input-only override pins preserved. |
| DRIVE | Telemetry/recording packaged; inspection does not imply game-input playback. Wheel/FFB/camera/triple remain unavailable in the player package. Preserve unpublished recording extension provenance. |
| Sonic | Packaged offline wheel prototype may be implemented=true/packaged=true while acceptance remains pending. Other feature prototypes require actual implementation evidence; no accepted player feature invented. Recorder/playback remain unimplemented. No UE4SS runtime included. |
| OutRun | Wheel/FFB/telemetry implemented/packaged with pending acceptance and FFB Off. Triple/recording/game-input playback not implemented. Redux unrelated payload excluded; runtime/installer source pins distinct. |
| F-Zero | Native wheel/FFB/telemetry/experimental triples and input/snapshot replay classified separately. Replay capability is game-input-replay plus optional external normalized observations, never physical playback. BS Deluxe excluded from stock-only package and current replay adapter unsupported. |

## Recording evidence fixtures

| ID | Fixture / expected result |
|---|---|
| R01 | Completed bounded JSONL diagnostic with exact recording DLL/source/settings provenance: accept stated diagnostic scope, not deterministic game replay. |
| R02 | Incomplete/truncated/cap-limited session: accurately mark incomplete; reject gates that require complete driving coverage. |
| R03 | F-Zero input/state pair uses exact matching snapshot, cartridge/settings identity and complete frame hashes: accept game-input replay scope only. Wrong sibling snapshot/source/settings or Deluxe without pins: refuse. Use synthetic metadata, not ROM execution. |
| R04 | Optional external normalization tool absent: basic native capture/replay remains separately declared; no false claim that private toolkit access is required. |
| R05 | Offline playback path attempts native FFB/device load: reject via mocked adapter/device boundary. All normalization fixtures remain force-free. |
| R06 | Unavailable/stale/unknown-unit signal relabeled measured zero, or OCR relabeled memory: reject shared signal-provenance gate. |

## Milestone exit evidence

For each adopted game, retain: exact clean generator/source IDs, generated descriptor, external ZIP SHA-256, existing integrity verifier result, all applicable D/S/R fixtures with explicit skip reasons, existing installer regression results, and owner-reviewed mapping. Source-only stages may pass descriptor review but cannot be labeled frozen player release artifacts.

Shared validator/parser and data fixtures should be reviewed once after the Art pilot. Adapter-specific installer/recording fixtures stay with existing owners. A contract pass proves delivery consistency within declared limits; it does not establish physical force, rendered triples, desktop scanout, camera completeness, comfort, runtime replay determinism or repository publication.
