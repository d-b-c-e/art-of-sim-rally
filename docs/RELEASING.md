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

## Package contents

A release zip needs:

```
ArtOfSimRally.Mod.dll          the mod
Dbce.Wheel.Telemetry.dll       the Forza encoder (from dbce-wheel-mod-toolkit)
UnityForceFeedback.dll         the native plugin the GAME is missing
README / install instructions
```

`UnityForceFeedback.dll` is ours — a clean-room implementation against a
documented DirectInput API and a P/Invoke signature read from the game's own
metadata. It ships with the mod. **No game files are redistributed**, and none
should ever be added to this repo.

**It ships inside the mod folder**, beside `ArtOfSimRally.Mod.dll`, and is
loaded from there by absolute path — `FfbNative.ResolveDllPath` looks in the mod
folder first, then its parent, and only then falls back to
`artofrally_Data/Plugins/x86_64/`. An earlier revision of this document said the
plugin folder was the required location; it is not, and a stale copy left there
by an old manual install is a real failure mode, because it can be loaded in
preference to the current one and then fail on an export it predates.

The binary itself is vendored, not built here: it is `WheelFfb.dll` from
dbce-wheel-mod-toolkit (`lib/toolkit/native`, pinned by `lib/toolkit/VERSION`),
copied to `UnityForceFeedback.dll` by `tools/package/package.ps1`.

## Cutting a release

Done once per version, in this order. The repo went public at 0.1.0 and the
pre-publication checklist that used to live here is complete: MIT licence, a
user-facing README, no game assemblies ever committed
(`git log --stat --all | grep -iE "Assembly-CSharp|\.CT$"` stays empty), and
tagged releases with the zip attached.

1. **Refresh the toolkit pin if it moved.** `tools\Sync-Toolkit.ps1 -Version
   vX.Y.Z`, then confirm the native exports are still a superset of what the mod
   P/Invokes — `dumpbin /exports` on the new `lib\toolkit\native\WheelFfb.dll`
   against the previous release's `UnityForceFeedback.dll`. The vendored
   binaries are committed, so the bump shows up as a diff.
2. **Close the game.** The DLLs are locked while it runs, and a deploy that
   "succeeds" over a running game leaves you testing the previous build.
3. `dotnet build ArtOfSimRally.sln -c Release`
4. `tools\package\package.ps1 -Version X.Y.Z`
5. Install the packaged zip, not the working tree, and drive a stage.
6. Update `CHANGELOG.md`, tag, and attach the zip to a GitHub release.

There are no unit tests in this repo — the telemetry encoder's suite moved to
dbce-wheel-mod-toolkit with the encoder. `dotnet test` here succeeds while
running nothing, so it proves nothing; step 5 is the real gate.

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
