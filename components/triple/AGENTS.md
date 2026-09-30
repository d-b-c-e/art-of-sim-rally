# AGENTS.md

## Repository Purpose

Research and prototype configurable triple-screen support for *art of rally*.
Keep physical-display calculations reusable by `triple-screen-optimizer`; keep
game hooks isolated in the UMM adapter.

## Repository Structure

- `src/Dbce.TripleScreen.Core/`: dependency-free physical geometry/projection.
- `src/Dbce.TripleScreen.Protocol/`: strict canonical contract reader/writer.
- `src/ArtOfRally.TripleScreen.Mod/`: Unity 2019.4 / UMM adapter and probes.
- `tests/`: executable offline geometry/protocol regression checks.
- `tools/package/` and `tools/installer/`: checked release ZIP and a standalone
  player installer; `tools/testing/` tests installation in a fake game folder.
- `contracts/`: versioned JSON interchange contracts.
- `docs/`: sourced findings, decisions, risks, and attended experiments.

## Conventions

- Treat installed game assemblies as read-only, copyrighted inputs. Never commit
  game, Unity, Unity Mod Manager, or decompiler output.
- The core must not reference Unity, UMM, Windows APIs, or the optimizer UI.
- Treat `contracts/` as a pinned copy of `triple-screen-optimizer/contracts`;
  update it from upstream rather than evolving a game-specific dialect.
- Measurements use millimetres and degrees at API/contract boundaries. Core
  vectors are eye-relative, right-handed: +X right, +Y up, -Z forward.
- A display's corners are lower-left, lower-right, upper-left as seen by the
  viewer; that winding makes its normal point toward the viewer.
- Add regression coverage for every geometry or matrix change. Run
  `dotnet build Dbce.TripleScreen.sln -c Release -p:GameDir=<game folder>` and
  the executable test project. `ART_OF_RALLY_DIR` can supply that game folder.
- Runtime observations and attended visual checks are distinct from offline
  math tests. Record game build, settings, logs, screenshots, and pass/fail.
- Do not install into or launch the game without an explicit experiment step.
  Never modify existing `art-of-sim-rally` files from this repo.
- Keep the mod usable without Triple Screen Optimizer. Output mode and FOV belong
  in the mod's main UI; measurements and diagnostics may stay in Advanced setup.
- Record visible tearing as an open NVIDIA Surround presentation issue. Unity
  VSync 1 and update FPS are not proof of tear-free scanout. The tested separate
  display drive had no visible tearing, while the tested Surround drive did.
- Package only this project's DLLs and metadata. Never include game, Unity, UMM,
  optimizer, local Settings.xml, desired-layout.json, or experiment captures.
