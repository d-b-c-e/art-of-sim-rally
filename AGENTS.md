# Art game coordination

Wheel implementation and its instructions live in `components/wheel/AGENTS.md`; triple implementation and instructions live in `components/triple/AGENTS.md`. Read the relevant component instructions before edits. Run component build/package commands from that component's directory.

Root `docs`, `game-release.json` and `tools/game` retain historical independent setup/release records. The local unified product candidate is documented in `docs/UNIFIED-MIGRATION.md` and packaged/tested through `tools/unified`. It uses one code load owner with internal feature assemblies and a metadata-only legacy optimizer bridge. Preserve public historical tags/assets, component mod IDs and installed owner settings. Packages do not imply physical or rendering acceptance. Keep game/Unity/UMM references, personal settings, recordings and build outputs out of Git. Do not deploy or launch games as part of source-layout or offline checks.

The explicitly requested source-only Actions gate is documented in `docs/SOURCE-CI.md`
and runs `tools/ci/Test-Source.ps1`. It uses committed/redistributable inputs and
synthetic installer fixtures only. It does not change the local game-dependent
release-build policy, publish/deploy anything, or certify hardware/rendering.
