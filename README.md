# dbce-triple-mod-art-of-rally

Research and prototype code for robust, configurable triple-screen rendering in
*art of rally*. The target is geometrically correct per-panel projection like a
modern sim-racing title, with NVIDIA Surround ultrawide as the dependable
fallback. This repository does not redistribute game or mod-loader binaries.

The current `0.2.0` prototype contains:

- `Dbce.TripleScreen.Core`, a Unity-independent .NET Standard 2.0 library that
  turns physical panel measurements into three display planes and asymmetric
  projection matrices;
- a Unity Mod Manager adapter with default-off center-panel and three-view
  projection experiments; the latter renders three unprocessed panel-size
  targets and composites them into one exact-width output;
- strict consumption of the canonical optimizer layout plus atomic canonical
  runtime-status reporting; and
- an adapter manifest that advertises only the runtime-verified
  `asymmetric-frustum` capability; three-view capability requires the
  attended visual and lifecycle gates.

The code builds and its geometry/protocol checks pass. A default-off loader
pass confirmed that the adapter loads alongside the existing mods, but neither
rendering experiment has passed the in-game visual gates, so true-triple
support is not yet claimed. The staged-layout fallback added after that pass
still needs a runtime retry. See [research](docs/research.md),
[architecture](docs/architecture.md), and the
[three-view smoke-test record](docs/experiments/003-three-view-runtime-smoke.md).

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

The build is deliberately not auto-installed. Follow the attended experiment
checklist only when the game is closed, and preserve logs and screenshots as
evidence.

## Project Structure

- `src/Dbce.TripleScreen.Core/` — reusable physical layout and projection math.
- `src/Dbce.TripleScreen.Protocol/` — strict layout/status protocol types.
- `src/ArtOfRally.TripleScreen.Mod/` — feature-gated UMM runtime adapter.
- `tests/` — dependency-free executable geometry regression tests.
- `contracts/` — pinned canonical optimizer/mod interchange schemas.
- `examples/` — illustrative layouts; not recommendations.
- `docs/` — evidence, architecture, risks, and experiments.

## Current Recommendation

Use NVIDIA Surround for the first playable path. It presents the three displays
as one large display and offers bezel-corrected resolutions, so the unmodified
game can render at the combined width. This is not angle-correct: it remains one
perspective frustum and stretches the side views. True triples need three
per-panel projections and about three times the scene rendering work.

## Runtime Contract and Safety

The optimizer writes
`%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\desired-layout.json`; the
adapter reads it without modifying it and atomically publishes `status.json`
beside it. The preview defaults off and refuses separate displays, output-size
mismatches, and TAA. When enabled on the verified gameplay camera shape, it
sets `Camera.rect` and `Camera.projectionMatrix` immediately before rendering;
disable/unload restores the original viewport and Unity projection.

This milestone does not call `Display.Activate` or `Screen.SetResolution`,
patch game methods, manipulate windows, or write game configuration. Only the
explicit three-view experiment creates render cameras and render textures;
they are released on disable or camera transition. Do not report runtime
support as working until the attended experiment passes.
