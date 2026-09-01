# Roadmap

Ordered by what settles the most uncertainty per unit of effort, not by what is
most exciting.

## Phase 0 — prove the force feedback path  ← next

The one experiment that decides the shape of the whole project.

`UnityForceFeedback.dll` is built and export-verified but has never run inside
the game. Install it with logging on, drive a stage, read the log.

```powershell
.\src\UnityForceFeedback\build.ps1 -Install
$env:AOSR_FFB_LOG = "1"
# launch art of rally, drive, quit
Get-Content "$env:LOCALAPPDATA\ArtOfSimRally\ffb.log"
```

Interpretation table is in [FORCE-FEEDBACK.md](FORCE-FEEDBACK.md#phase-0-the-decisive-experiment).

Outcomes:

- **Calls arrive** — route A works. Force feedback exists in art of rally for
  the first time, with no game code patched. Move to phase 1.
- **Init but no forces** — the behaviour is gated. Small Harmony patch to force
  `enableForceFeedback`; still route A.
- **Silence** — the behaviour is not attached. Route A is dead, go to phase 3
  and build force feedback ourselves.

Also record the `SetDeviceForcesXY` values. That is the game's own force curve,
and it tells us whether route A's output is worth keeping.

**Blocked on:** a wheel being plugged into this machine. Everything up to this
point was doable without one; nothing past it is.

First bind the wheel in game — no utility needed, see
[CONTROLS.md](CONTROLS.md) — and confirm throttle and brake are on separate
axes before trusting anything the FFB log says.

## Phase 1 — the Unity Mod Manager mod

Stand up the UMM + Harmony mod proper, following
[`MMike17/ArtOfRally_ModBase`](https://github.com/MMike17/ArtOfRally_ModBase).

- `Info.json`, entry point, Ctrl+F10 settings UI.
- Harmony hook on `CarDynamics.FixedUpdate` (or `CarController.FixedUpdate`) to
  read the physics state.
- Wire `ArtOfSimRally.Telemetry` in and emit for real.
- Settings: telemetry on/off, host, port; FFB multiplier, smoothing, invert.

**Blocked on:** Unity Mod Manager being installed, to reference
`UnityModManager.dll` and `0Harmony.dll`. The telemetry assembly is deliberately
free of both so it could be built and tested first — which it has been.

## Phase 2 — telemetry against the real game

The encoder is done and round-trip tested; this is about the mapping.

- Fill `TelemetryFrame` from the real `Wheel` / `Drivetrain` / `CarController`
  fields per the table in [TELEMETRY.md](TELEMETRY.md#mapping-art-of-rally-onto-it).
- Verify with `harness/forza_probe.py`, then with SimHub.
- Check units: `veloKmh` ÷ 3.6, degrees → radians, suspension travel normalised.
- Confirm `IsRaceOn` actually goes false in menus.
- Derive acceleration by differentiating velocity — the game does not store it.

## Phase 3 — force feedback worth having

Only meaningful once phase 0 says which route we are on.

Compute force from `Wheel.Mz` (self-aligning torque, per wheel) rather than
faking it from lateral G, and layer on:

- load sensitivity from suspension force
- surface texture per `surfaceType` / `physicMaterial`
- kerb and impact effects
- `ABSTriggered` / `TCSTriggered` as discrete effects
- a tuning UI, because no two wheels agree on anything

Consider the Logitech SDK path in parallel: the game already binds
`LogiPlayDirtRoadEffect`, `LogiPlayBumpyRoadEffect`, `LogiPlaySlipperyRoadEffect`
and `LogiPlaySurfaceEffect`, and that native DLL **is** shipped. For a Logitech
wheel those are purpose-built rally effects available for free.

## Phase 4 — bonnet camera

Design and constraints in [CAMERA.md](CAMERA.md). Deliberately last: a working
camera mod already exists on Nexus, so this is the feature with the least unmet
need, and it is the one most improved by having telemetry to tune against.

## Phase 5 — make it shareable

The stated goal is a robust, reusable, shareable mod.

- Flip the repo public.
- Release workflow producing a UMM-installable zip, versioned by tag.
- README aimed at users rather than at us.
- Publish to Nexus.
- Decide what to do about `UnityForceFeedback.dll`: it is our own clean-room
  implementation of a documented DirectInput API against a P/Invoke signature,
  so it ships with the mod. No game files are redistributed.

## Explicitly out of scope

- **Cockpit view.** No modelled interiors. See [CAMERA.md](CAMERA.md).
- **Physics or assist changes.** art of rally has online leaderboards. Force
  feedback, camera and telemetry are all fair-play neutral; changing grip,
  assists or car behaviour is not. Keeping that line bright is what lets this
  be shared without argument.
- **Redistributing game assemblies.** Reference them from the local install.
