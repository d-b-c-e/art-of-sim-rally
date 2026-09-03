# art of sim rally

A mod that makes [art of rally](https://store.steampowered.com/app/550320/) work
properly on a racing wheel.

- **Force feedback.** The game has a complete implementation that was never
  wired up. This finishes it, driven by the game's own tyre model.
- **Fixes vague steering.** Two separate problems make wheels feel dead near
  centre. Both are fixed.
- **Shifter support**, H-pattern and sequential, on a separate device.
- **Bonnet and bumper cameras**, added to the game's normal view rotation.
- **Telemetry** for SimHub, dashboards, bass shakers and motion rigs.

## Install

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) and point
   it at art of rally. The game is already in its supported list.

2. Download the latest [release](../../releases/latest) zip, unzip it, and
   double-click **Install.bat**.

That's it. The installer finds your game even on a non-default Steam library,
checks Unity Mod Manager is present, and puts every file where it belongs.

Launch the game and press **Ctrl+F10** for settings. To remove the mod later,
double-click **Uninstall.bat** — your settings are kept.

<details>
<summary>Installing by hand, or with Vortex</summary>

Drag the zip onto Unity Mod Manager's **Mods** tab. The mod folder sits at the
zip root, so it installs correctly.

The game must be closed. `UnityForceFeedback.dll` ships inside the mod folder and
is loaded from there, so no extra copying is needed.
</details>

## Settings

Everything is in the Ctrl+F10 panel, in collapsible sections, and adjustable
while driving.

**Force feedback** — pick your wheel from the **Wheel** dropdown, then set
**Strength** (0–100; start around 70). If two devices share a name, choose one
and turn the wheel — if nothing happens, choose the other. Switching wheels takes
effect immediately. If the wheel pulls the wrong way, tick *Invert direction*.

**Shifter** — tick *Use a separate shifter*, choose the device, and bind each
gear: click **set**, then move the lever into that gate. H-pattern and sequential
are both supported, and the bindings shown match the mode you picked.

**Camera** — press your change-view button to cycle onto the bonnet view, and
once more for the bumper view. Adjust whichever is on screen with the numpad:

| Key | |
|---|---|
| `8` / `2` | up / down |
| `7` / `9` | back / forward |
| `4` / `6` | left / right |
| `1` / `3` | tilt |
| `+` / `-` | field of view |
| `0` | reset |

Changes save automatically.

**Telemetry** — off by default. Turn it on and point SimHub at a **Forza
Horizon 5** profile on UDP port **8000**. Host and port can be changed while the
game runs, which helps if something else already owns the port.

## Something not working?

In the settings panel, under *Devices and troubleshooting*, press **Create
support file on Desktop**. It collects your settings, your controllers, what is
actually bound, and the logs into one file. Attach that to an
[issue](../../issues) — it usually contains the answer.

## Worth knowing

**Steering assist is a separate switch.** *Direct steering* and *Remove hidden
deadzone* only restore what the game already gives wheels it recognises, so they
change nothing about the car. *Disable steering assist* does change how the car
behaves, and is off by default. The game has online leaderboards.

**A shifter gate that also accelerates or brakes** means the game auto-bound that
button to one of its own actions when it saw the shifter as a controller. Reset or
clear the binding in the game's controls screen; the mod reads the shifter directly
and does not need the game to know about it.

**Bonnet, not cockpit.** The cars have no modelled interiors, so a cockpit view
isn't possible.

**Tested on one wheel**, a MOZA R12 Base. The force feedback is plain DirectInput
constant force with nothing vendor-specific in it, so it should work on anything
that does force feedback — but that is reasoning rather than testing.

**Known issue:** the camera can move about for a second when the game takes over
at the end of a stage. Cosmetic, and confined to the results cinematic.

## Why the steering felt wrong

Two things, both invisible from the options screen:

1. **Rewired applies a 10% deadzone to any controller it doesn't recognise**, and
   its hardware database predates every modern direct-drive wheel. The deadzone in
   the game's options is a *different* one — you can set it to zero and still have
   a dead band. On a 270° wheel this was 27° of nothing at centre.

2. **The game has a direct-steering mode that only activates for wheels it
   recognises.** Everyone else gets the gamepad smoothing filter, which works out
   around 1.6 seconds lock-to-lock at speed.

## Force feedback

art of rally has a complete force feedback implementation that never ran. The
physics are there, the output code is there, and nothing connects them — the value
linking the two is never assigned, and the native plugin it calls into isn't in
the shipped build.

This mod supplies the missing piece and the missing plugin.

## Shifters

The game only has *ShiftUp* and *ShiftDown* actions internally, so even a bound
H-pattern lever would behave like paddles — selecting 3rd would mean "one gear up
from wherever you are". Its input layer also cannot see most shifters at all, so
usually there is nothing to bind in the first place.

The mod reads the shifter directly instead, and selects the gear you actually
chose.

Full technical detail, including how to read any of this out of the game
yourself, is in [docs/](docs/) — start with [FINDINGS.md](docs/FINDINGS.md).
Release history is in [CHANGELOG.md](CHANGELOG.md).

## Building

Needs the .NET SDK and Visual Studio Build Tools with the C++ workload.

```
dotnet build ArtOfSimRally.sln -c Release
tools\package\package.ps1 -Version 0.2.0
```

Referencing the game's assemblies requires art of rally installed; override
`GameDir` if it isn't in the default Steam location. Unity Mod Manager's
assemblies go in `lib/umm`. Neither is committed.

## Licence

MIT. No game files are redistributed. `UnityForceFeedback.dll` is an original
implementation of a documented DirectInput API.
