# art-of-sim-rally

Turn [art of rally](https://store.steampowered.com/app/550320/) into a sim rig
game: real force feedback, Forza-compatible telemetry, and a bonnet camera.

Under the isometric camera, art of rally runs a genuine load-sensitive tire
model — per-wheel slip ratio, slip angle, and self-aligning torque (`Mz`). The
simulation is already there. What is missing is everything that connects it to a
wheel, a dashboard and a viewpoint.

## The discovery this project is built on

**art of rally ships a complete force feedback implementation that has never
run, because the native DLL it calls into was left out of the build.**

The managed `ForceFeedback` class P/Invokes seven entry points from a module
named `UnityForceFeedback`. That DLL does not exist anywhere in the 7.4 GB
install. All five Logitech native wrappers shipped; the one generic force
feedback wrapper did not.

```
Wheel.Mz, suspension, surface
   → CarDynamics.forceFeedback → ForceFeedback.Update() → SetDeviceForcesXY()
       → ✗ UnityForceFeedback.dll — NOT SHIPPED
```

So the first thing in this repo is a clean-room implementation of that missing
DLL. Drop it into the game's plugin folder and the game's own force feedback
wakes up with no game code patched at all.

Full evidence in [docs/FINDINGS.md](docs/FINDINGS.md) — all of it read directly
out of the shipped assemblies, none of it from forum posts.

## Status

| Component | State |
|---|---|
| `UnityForceFeedback.dll` — the missing native plugin | **Built**, x64, all 7 exports verified. Not yet runtime-verified in game. |
| Forza telemetry encoder | **Done.** 324-byte FH4/5 Data Out, 24 tests passing, round-trip verified C# → UDP → Python. |
| Synth + probe harness | **Done.** Test the whole chain with no game running. |
| Unity Mod Manager mod | Not started — needs UMM installed. |
| Bonnet camera | Not started. Design in [docs/CAMERA.md](docs/CAMERA.md). |

The next step is [phase 0](docs/ROADMAP.md): install the DLL with logging on,
drive a stage, and find out whether the game calls it. That single experiment
decides the shape of everything after it.

## Getting started

Requires the .NET SDK, and Visual Studio Build Tools with the C++ workload for
the native DLL.

**Build and install the force feedback plugin:**

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
```

**Trace what the game does with it:**

```powershell
$env:AOSR_FFB_LOG = "1"
# launch art of rally, drive, quit
Get-Content "$env:LOCALAPPDATA\ArtOfSimRally\ffb.log"
```

How to read that log is in
[docs/FORCE-FEEDBACK.md](docs/FORCE-FEEDBACK.md#phase-0-the-decisive-experiment).

**Run the telemetry tests:**

```powershell
dotnet test ArtOfSimRally.sln
```

**Test a dashboard with no game running** — emits a scripted drive, 0→130→0 mph,
five gears, RPM sawtooth:

```powershell
dotnet run --project tools/ArtOfSimRally.Synth
```

**See what is actually being sent** (close SimHub first, it holds the port):

```bash
python harness/forza_probe.py
```

## Project structure

| Path | Contents |
|---|---|
| `src/UnityForceFeedback/` | C++ x64 DirectInput plugin — the DLL the game is missing |
| `src/ArtOfSimRally.Telemetry/` | Forza Data Out encoder + UDP sender. netstandard2.0, no Unity or UMM dependency |
| `tests/` | xUnit tests pinning the wire format |
| `tools/ArtOfSimRally.Synth/` | Synthetic telemetry emitter for testing consumers |
| `harness/` | `forza_probe.py` — listen and print what is on the wire |
| `docs/` | FINDINGS, FORCE-FEEDBACK, TELEMETRY, CAMERA, ROADMAP |

## Two things worth knowing up front

**This is a bonnet camera, not a cockpit camera.** The cars have no modelled
interiors and the world is authored for a distant view. A cockpit view is an art
project, not a mod. See [docs/CAMERA.md](docs/CAMERA.md).

**No physics or assist changes.** art of rally has online leaderboards. Force
feedback, camera and telemetry are fair-play neutral; grip and assists are not.

## Credit

The Forza packet offsets were validated against SimHub by the sibling
`cruisn-collection` project, whose synth/probe harness pattern this repo reuses.
The Unity Mod Manager route follows
[`MMike17/ArtOfRally_ModBase`](https://github.com/MMike17/ArtOfRally_ModBase).
