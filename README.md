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
| `UnityForceFeedback.dll` — the missing native plugin | **Built and verified sound** (loads standalone, 7 exports resolve, no VC-runtime dependency). Tested in game 2026-08-31: **the game never calls it** — see below. |
| Forza telemetry encoder | **Done.** 324-byte FH4/5 Data Out, 24 tests passing, round-trip verified C# → UDP → Python. |
| Synth + probe harness | **Done.** Test the whole chain with no game running. |
| The mod (BepInEx) | **Working in game.** Direct steering + Rewired deadzone fix confirmed to transform wheel feel; FFB device opens successfully. |
| Bonnet camera | Not started. Design in [docs/CAMERA.md](docs/CAMERA.md). |

**Phase 0 is answered (2026-08-31), and the answer is better than a working
shortcut would have been.** Decompilation shows art of rally's force feedback was
built from both ends and never joined in the middle:

- The consumer is complete — `ForceFeedback.Update()` scales a value and calls
  `SetDeviceForcesXY`. It is simply never attached to anything.
- The physics is real but switched off — `Wheel` computes self-aligning torque
  via `CalcAligningForce`, but only `if (cardynamics.enableForceFeedback)`, which
  nothing ever sets. So `Mz` is currently always zero.
- **`CarDynamics.forceFeedback` is never assigned anywhere in the assembly.** The
  link between the two ends was never written.

So the mod's job is small and well-defined: flip `enableForceFeedback` on, compute
a force from the steered wheels' `Mz`, and feed it out through this DLL. Their own
constants even tell us the target range. Full detail in
[docs/FORCE-FEEDBACK.md](docs/FORCE-FEEDBACK.md).

## Getting started

Requires the .NET SDK, and Visual Studio Build Tools with the C++ workload for
the native DLL.

**Build and install the force feedback plugin:**

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
```

**Trace what the game does with it:**

The variable must reach the *game* process. Setting it in a shell is not enough
if you launch from Steam: the game inherits Steam's environment, not your
shell's, so the log comes back empty and you would wrongly conclude the game
never calls the DLL. Set it at user scope and restart Steam:

```powershell
[Environment]::SetEnvironmentVariable('AOSR_FFB_LOG', '1', 'User')
# fully exit Steam, start it again, then launch art of rally and drive
Get-Content "$env:LOCALAPPDATA\ArtOfSimRally\ffb.log"
```

Or start **Steam itself** with the variable set, which avoids any persistent
change - it passes down to the game Steam launches:

```powershell
& "D:\Program Files (x86)\Steam\steam.exe" -shutdown
# wait for Steam to fully exit, then:
$env:AOSR_FFB_LOG = "1"
& "D:\Program Files (x86)\Steam\steam.exe"
```

Launching `artofrally.exe` directly does **not** work: the game ships without a
`steam_appid.txt`, so Steamworks restarts it through Steam and the relaunched
process loses the variable. It looks like the game just failed to start.

Turn it off again with
`[Environment]::SetEnvironmentVariable('AOSR_FFB_LOG', $null, 'User')`.

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
| `docs/` | FINDINGS, FORCE-FEEDBACK, TELEMETRY, CONTROLS, CAMERA, ROADMAP |

## Confirmed: a hidden 10% deadzone on every unrecognised wheel

Measured in game on a MOZA R12: **Rewired applies a 10% deadzone to every axis of
a controller it does not recognise**, and nothing in art of rally's options screen
exposes it — the game's own deadzone setting is a separate, innocent one. On a
270° wheel that is a 27° dead band at centre.

Rewired's shipped hardware database predates every modern direct-drive base, so
this likely affects Moza, Simagic, Simucube, Fanatec DD and Asetek owners alike.
The mod zeroes it. Combined with restoring the game's own direct-steering path,
this is the single biggest improvement to how the game feels on a wheel.

Details in [docs/CONTROLS.md](docs/CONTROLS.md).

## Do I need a utility to bind my wheel?

No. The game has a native press-to-bind UI with split-axis support, and
Rewired already recognises most wheels (G25/G27/G29/G920/G923, Driving Force,
Fanatec, Thrustmaster, Moza). Bindings and calibration persist in the registry.

Do **not** route the wheel through xoutput/XInput — XInput has no force
feedback beyond rumble, and it would hide the device from the DirectInput API
this mod depends on. Details in [docs/CONTROLS.md](docs/CONTROLS.md).

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
