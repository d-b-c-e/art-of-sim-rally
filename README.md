# dbce-triple-mod-art-of-rally

Research and prototype code for robust, configurable triple-screen rendering in
*art of rally*. The target is geometrically correct per-panel projection like a
modern sim-racing title, with NVIDIA Surround ultrawide as the dependable
fallback. This repository does not redistribute game or mod-loader binaries.

The first prototype contains:

- `Dbce.TripleScreen.Core`, a Unity-independent .NET Standard 2.0 library that
  turns physical panel measurements into three display planes and asymmetric
  projection matrices;
- a read-only Unity Mod Manager probe that inventories the game's cameras,
  display API state, render targets, projection matrices, attached effects, and
  UI canvases without changing them; and
- a versioned JSON contract intended for `triple-screen-optimizer`.

No rendering mod is claimed working yet. The next gate is an attended probe run
against a stage, followed by a center-only projection override and then a
three-render-target compositor. See [research](docs/research.md),
[architecture](docs/architecture.md), and the [experiment plan](docs/experiments/001-runtime-camera-inventory.md).

## Getting Started

Requirements: Windows, .NET 8 SDK, an installed Steam copy of *art of rally*,
and Unity Mod Manager installed into that game. The project defaults to the
owner's current game path; override `GameDir` at build time on another machine.

```powershell
dotnet build Dbce.TripleScreen.sln -c Release
dotnet run --project tests/Dbce.TripleScreen.Core.Tests -c Release
```

Build with a different game location:

```powershell
dotnet build Dbce.TripleScreen.sln -c Release -p:GameDir='E:\SteamLibrary\steamapps\common\artofrally'
```

The probe is deliberately not auto-installed. Follow the experiment checklist
only when the game is closed, and keep the generated UMM log as evidence.

## Project Structure

- `src/Dbce.TripleScreen.Core/` — reusable physical layout and projection math.
- `src/ArtOfRally.TripleScreen.Mod/` — observation-only UMM runtime probe.
- `tests/` — dependency-free executable geometry regression tests.
- `contracts/` — optimizer/mod interchange schema.
- `examples/` — illustrative layouts; not recommendations.
- `docs/` — evidence, architecture, risks, and experiments.

## Current Recommendation

Use NVIDIA Surround for the first playable path. It presents the three displays
as one large display and offers bezel-corrected resolutions, so the unmodified
game can render at the combined width. This is not angle-correct: it remains one
perspective frustum and stretches the side views. True triples need three
per-panel projections and about three times the scene rendering work.

## Safety and Scope

The checked-in probe only logs state. It does not call `Display.Activate`,
`Screen.SetResolution`, set `Camera.projectionMatrix`, clone a camera, patch a
game method, or write game configuration. Do not report a feature as working
until it has passed the attended experiments in `docs/experiments/`.
