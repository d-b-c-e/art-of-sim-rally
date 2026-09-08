# Releasing

## One mod, not four

All four features ship as a single mod with per-feature toggles, and that is
deliberate rather than lazy packaging:

- They hang off the same two Harmony hooks. `CarDynamics.FixedUpdate` serves both
  force feedback and telemetry; `CarController.Start` serves both the steering fix
  and the calibration fix. Split into separate mods, each would install its own
  patch on the same methods, and users would be running two or three copies of the
  same hook.
- The steering and deadzone fixes are prerequisites for the rest feeling right.
  Someone who installs "the force feedback mod" alone and still has a 27 degree
  dead band would reasonably conclude the force feedback is bad.
- Anything unwanted is one config line away from off.

The config is grouped by feature so this reads clearly to a user:

```
[Steering]  DirectSteering, ZeroAxisDeadzone, BindAnyDevice, GlyphTextFallback, DisableSteerAssist
[ForceFeedback]  Enabled, Strength, FyReference, Smoothing, Invert, PreferredDevice(+Index), DiagnosticLogging
[Shifter]  Enabled, IsHPattern, DeviceIndex/Name, SkipNeutral, gear and shift buttons
[WheelInput]  Enabled, Steer/Throttle/Brake/Clutch/Handbrake bindings ("device|index|axis:N|rest|far")
[Camera]  Bonnet* and Bumper*: Enabled, Height, Forward, Side, Pitch, FOV; BonnetLean
[CameraTuning]  numpad hotkeys
[Telemetry]  Enabled, Host, Port
[Experiment]  UseDirectInputBackend (Settings.xml only; see CONTROLS.md)
```

## Loader: Unity Mod Manager (done)

Shipping for **Unity Mod Manager**, which has official art of rally support in its
own game database:

```xml
<GameInfo Name="Art of Rally">
  <EntryPoint>[UnityEngine.UIModule.dll]UnityEngine.Canvas.cctor:Before</EntryPoint>
  <StartingPoint>[Assembly-CSharp.dll]GameEntryPoint.Start:After</StartingPoint>
</GameInfo>
```

Reasons, in order of weight:

1. **It is what the community already runs.** The Nexus camera mod requires it, so
   most people who would install this already have it.
2. **In-game settings.** UMM draws a settings panel at Ctrl+F10. That matters more
   than usual here: force feedback strength and the camera mount can only be judged
   while driving, and the alternative is quitting to edit a text file for every
   adjustment.

3. **It would have collided with BepInEx.** Verified after installing: UMM's
   installer wrote Doorstop to the game root, exactly as BepInEx does.

   ```ini
   ; <game>/doorstop_config.ini, alongside <game>/winhttp.dll
   target_assembly = artofrally_Data\Managed\UnityModManager\UnityModManager.dll
   ```

   Worth recording how this got muddled, because the trap is easy to fall into
   twice. UMM's game database defines an `EntryPoint` for art of rally that looks
   like assembly injection, and reading that alone led to retracting a correct
   claim. UMM supports both mechanisms and its installer chose Doorstop here.
   **A config file describes a capability; only the install shows which one was
   actually used.**

Development originally used BepInEx because it installs unattended; UMM's installer
is a GUI. That is a fine reason to prototype with it and a poor reason to ship it.

### How the port stayed contained

`Main.cs` is the only loader-aware file. Everything else talks to `ModLog` and a
plain-field `Settings` class, so supporting a second loader means adding a sibling
of `Main.cs` rather than touching a single patch. The pieces a loader entry point
provides:

1. `Info.json` — `Id`, `DisplayName`, `Version`, `AssemblyName`, `EntryMethod`.
2. `Main.Load(UnityModManager.ModEntry)` — creates the Harmony instance, patches,
   installs the watchdog.
3. `Settings : UnityModManager.ModSettings, IDrawable` with `[Draw]` attributes,
   giving the Ctrl+F10 panel.
4. References to `UnityModManager.dll` and its Harmony, extracted into `lib/umm`
   and **never committed**.

Two build notes worth keeping:

- The project targets **net48**, not net472, because UMM's own assemblies are
  built against .NET Framework 4.8 and will not resolve from a lower target.
  Unity 2019.4's Mono runs both.
- `IDrawable` lives in the `UnityModManagerNet` namespace directly, not nested
  inside `UnityModManager`.

Shipping both loaders remains possible — two thin entry assemblies over the shared
core — but keep one as the documented default so support questions stay simple.

## Package contents and native placement

The zip contains `ArtOfSimRally/` at its root for UMM, plus Install.bat,
Uninstall.bat, install.ps1, verify.ps1, README.txt, LICENSE and manifest.json.
The mod folder contains only Info.json, build.json, ArtOfSimRally.Mod.dll,
Dbce.Wheel.Ffb.dll, Dbce.Wheel.Telemetry.dll and UnityForceFeedback.dll. No game
or UMM assemblies or user Settings.xml are packaged. The development recorder
is a separate optional installation; packaging rejects its types/dependencies
in the production assembly as well as unexpected payload files.

The native artifact is our toolkit's x64 WheelFfb.dll, shipped under the name
UnityForceFeedback.dll. The batch installer deliberately copies it **both** beside
the mod and into `artofrally_Data/Plugins/x86_64`. UMM's archive route installs the
mod-folder copy. Neither path proves staleness: compare observed module hashes
with the tested package. If multiple copies differ, close the game and reinstall
the complete candidate; do not delete a correct plugin copy merely for its path.

## Cutting a release

1. Vendor released toolkit v0.12.0 using the transactional Sync-Toolkit.ps1.
   Confirm VERSION and hashes; never label a local build as a release.
2. Keep `Version.props` and source Info.json on the same numeric UMM version.
   Run `tools/testing/Test-Rc.ps1 -Version 0.2.3-rc.N` with an unused RC number.
   It builds with warnings as errors, runs consumer checks, validates vendor
   hashes/exports and packages the identified artifact. Existing RCs are immutable.
3. Close the game, install that zip and complete the generated attended checklist.
   See [PRE-RELEASE-TESTING.md](PRE-RELEASE-TESTING.md) for captures and evidence.
4. Run `python tools/testing/rc_gate.py check <manual.json>`. Pending cases,
   missing evidence and changed archives return nonzero. A build alone is not a gate.
5. Review the concise Unreleased notes; keep unconfirmed hardware reports open.
   Commit the source, prepare the final-labelled artifact from a clean tree, and
   validate that exact artifact before tagging or publishing. Rebuilding changes
   its identity and invalidates the earlier artifact's sign-off.

`tools/package/package.ps1 -Version X.Y.Z` remains the packaging entry point for
a final clean-tree build; it does not itself authorize publication or claim game
tests passed. Numeric UMM/assembly versions and the full informational build
identity are validated before archiving. Archive and payload hashes are retained.

Use `tools/testing/Test-Rc.ps1 -Version X.Y.Z -Final` to run the complete offline
suite against a final-labelled artifact, including packaging and installation
tests. This creates a new attended checklist; `-Final` does not mark it passed.

`dotnet test ArtOfSimRally.sln` runs no tests; the explicit executable runners in
the RC script are the automated evidence. Native/telemetry source suites live in
the toolkit. No automated path in this repo recreates a Unity playthrough.

## Channels

| Where | Notes |
|---|---|
| **Nexus Mods** | The main one. art of rally has an active page and the existing camera mod lives there. Expects UMM. |
| **GitHub releases** | Source of truth, links from everywhere else. |
| **OverTake.gg** | Sim racing audience specifically — the people who care most about the FFB and telemetry. |
| **Official Discord** | Community camera mods are already shared there. |
| **In-game CurseForge browser** | `ModManager` with `GameID 78103`. Content pipeline — cars, liveries, stages. Almost certainly will not accept a code mod; unverified. |

## Honesty in the release notes

Two things to state plainly, because both will otherwise generate complaints:

1. **Tested on one wheel.** Everything was developed against a MOZA R12 Base. The
   deadzone and steering findings should apply to any wheel Rewired does not
   recognise, which is likely every modern direct-drive base, but that is
   reasoning rather than testing. `FyReference` will need tuning per wheel.
2. **Leaderboards.** `DirectSteering` and `ZeroAxisDeadzone` restore what a
   recognised wheel already gets and are fair-play neutral. `DisableSteerAssist`
   genuinely changes driving aids and is off by default. Say so, so nobody enables
   it by accident and posts a time.
