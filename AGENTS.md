# AGENTS.md

## Repository Purpose

Research and prototype configurable triple-screen support for *art of rally*.
Keep physical-display calculations reusable by `triple-screen-optimizer`; keep
game hooks isolated in the UMM adapter.

## Repository Structure

- `src/Dbce.TripleScreen.Core/`: dependency-free physical geometry/projection.
- `src/ArtOfRally.TripleScreen.Mod/`: Unity 2019.4 / UMM adapter and probes.
- `tests/`: executable offline regression checks.
- `contracts/`: versioned JSON interchange contracts.
- `docs/`: sourced findings, decisions, risks, and attended experiments.

## Conventions

- Treat installed game assemblies as read-only, copyrighted inputs. Never commit
  game, Unity, Unity Mod Manager, or decompiler output.
- The core must not reference Unity, UMM, Windows APIs, or the optimizer UI.
- Measurements use millimetres and degrees at API/contract boundaries. Core
  vectors are eye-relative, right-handed: +X right, +Y up, -Z forward.
- A display's corners are lower-left, lower-right, upper-left as seen by the
  viewer; that winding makes its normal point toward the viewer.
- Add regression coverage for every geometry or matrix change. Run
  `dotnet build Dbce.TripleScreen.sln -c Release` and the executable test project.
- Runtime observations and attended visual checks are distinct from offline
  math tests. Record game build, settings, logs, screenshots, and pass/fail.
- Do not install into or launch the game without an explicit experiment step.
  Never modify existing `art-of-sim-rally` files from this repo.
- Prefer NVIDIA Surround as the supported fallback until true-triple rendering
  passes post-processing, UI, replay, photo-mode, and performance gates.
