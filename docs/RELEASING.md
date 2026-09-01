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
[Steering]  DirectSteering, ZeroAxisDeadzone, DisableSteerAssist
[ForceFeedback]  Enabled, Gain, MzReference, Smoothing, Invert
[Camera]  BonnetCamera, Height, Forward, Side, Pitch, FieldOfView, Lean
[CameraTuning]  hotkeys
[Telemetry]  Enabled, Host, Port
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

An earlier version of this document claimed BepInEx and UMM would collide over
`winhttp.dll` because both use Doorstop. That was wrong for this game — the entry
above is assembly injection, not Doorstop, so they would not necessarily have
fought. The port stands on the two reasons above, not on that one.

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
ArtOfSimRally.Telemetry.dll    the Forza encoder
UnityForceFeedback.dll         the native plugin the GAME is missing
README / install instructions
```

`UnityForceFeedback.dll` is ours — a clean-room implementation against a
documented DirectInput API and a P/Invoke signature read from the game's own
metadata. It ships with the mod. **No game files are redistributed**, and none
should ever be added to this repo.

Note its install path differs from the rest: it belongs in
`artofrally_Data/Plugins/x86_64/`, beside the game's other native plugins, not in
the plugin folder. Any installer or instructions must be explicit about that,
because a wrong location fails silently with no force feedback and no error.

## Before the repo goes public

- [ ] Flip the repo to public.
- [ ] LICENSE is MIT; check the copyright name is the one you want.
- [ ] Rewrite README's opening for users rather than for us — right now it leads
      with the engineering discovery, which is the right hook for other modders
      but not for someone who just wants their wheel to work.
- [ ] Confirm no game assemblies or `.CT` files were ever committed:
      `git log --stat --all | grep -iE "Assembly-CSharp|\.CT$"`
- [ ] Tag `v0.1.0` and attach the zip to a GitHub release.

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
   reasoning rather than testing. `MzReference` will need tuning per wheel.
2. **Leaderboards.** `DirectSteering` and `ZeroAxisDeadzone` restore what a
   recognised wheel already gets and are fair-play neutral. `DisableSteerAssist`
   genuinely changes driving aids and is off by default. Say so, so nobody enables
   it by accident and posts a time.
