# Experiment 001: Runtime Camera and Display Inventory

## Question

What exact camera components, canvases, projection changes, and display state
exist in each Art of Rally mode on the current owner machine?

## Safety

The 0.0.1 probe only logs. It does not patch game code or mutate rendering,
resolution, displays, config, or saves. Install/remove only while the game is
closed. Preserve any existing `ArtOfSimRally` installation.

## Build

```powershell
dotnet build Dbce.TripleScreen.sln -c Release
dotnet run --project tests/Dbce.TripleScreen.Core.Tests -c Release
```

Copy these three files into a new UMM mod folder named
`DbceTripleScreenArtOfRally`:

- `ArtOfRally.TripleScreen.Mod.dll`
- `Dbce.TripleScreen.Core.dll`
- `Info.json`

Do not automate installation until the package/install workflow is reviewed.

## Attended matrix

Capture the UMM log after each state:

1. Main menu.
2. Pre-stage cinematic.
3. Waiting at the start line.
4. Driving with each stock camera preset.
5. Pause menu.
6. Finish cinematic.
7. Replay.
8. Photo mode.
9. Repeat driving with FXAA, SMAA, TAA, and no AA if all are offered.
10. Repeat one drive with `art-of-sim-rally` bonnet view active.

Record game build, resolution/fullscreen mode, GPU driver, Surround status, and
the relevant `UnityModManager/Log.txt` excerpt. A screenshot of each state is
useful but does not replace the log.

## Pass criteria

- The probe loads without changing visible behavior.
- Every lifecycle state reports the authoritative camera, its components,
  target, viewport, projection summary, and canvases.
- We can identify which component resets projection/TAA and which UI canvases
  must stay center-only.
- Removing the probe leaves the game and existing mods unchanged.

## Decision after the experiment

If camera/effect ownership is stable, implement Experiment 002: a reversible
center-only projection override with an on-screen grid and a one-key bypass. If
it is not stable, add targeted diagnostics first; do not jump to three cameras.
