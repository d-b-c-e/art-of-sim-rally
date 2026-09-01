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

## The loader problem — read this before shipping

Development so far uses **BepInEx**, chosen because it installs unattended and
Unity 2019.4 Mono is its native target. **That is probably the wrong thing to
ship**, for a concrete reason rather than a stylistic one.

BepInEx and Unity Mod Manager both install Doorstop, and both drop it at the game
root as `winhttp.dll`. The established art of rally mod ecosystem is UMM — the
Nexus camera mod requires it — so a user who already has UMM installed and then
installs our BepInEx build has two loaders contending for the same file. UMM does
offer an assembly-injection mode that may sidestep it, but "may" is not a good
install experience.

Shipping for UMM means our mod coexists with what people already run.

### Porting to UMM

The work is contained. `Plugin.cs` is the only loader-aware file; everything else
talks to `Plugin.Log` and `Plugin.Settings`. A UMM build needs:

1. `Info.json` with `Id`, `DisplayName`, `Version`, `AssemblyName`, `EntryMethod`.
2. A `Load(UnityModManager.ModEntry)` entry point that creates the Harmony
   instance and patches, mirroring `Plugin.Awake`.
3. Settings via `UnityModManager.ModSettings` plus an `OnGUI` panel, replacing the
   BepInEx `ConfigEntry` bindings. This is the bulk of it — UMM shows settings
   in-game under Ctrl+F10, which is nicer than editing a file.
4. Reference `UnityModManager.dll` from the UMM install (never commit it).

Shipping both loaders is possible — two thin entry assemblies over a shared core —
but pick one as the documented default so support questions stay simple.

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
