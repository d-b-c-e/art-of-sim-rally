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

2. Download the latest [release](https://github.com/d-b-c-e/art-of-sim-rally/releases/latest) zip, unzip it, and
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
**Strength** (0–100; 50 is the tuned default, and the right starting point). If two devices share a name, choose one
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

**0.2.4 candidate:** the Camera panel adds keyboard remapping, cancel/clear and
restore-defaults for all tuning actions. Edits save when paused or otherwise idle,
including after leaving a mounted view. See [camera controls](docs/CAMERA.md).
These additions have offline coverage and still need an attended UI check.

**Telemetry** — off by default. Turn it on and point SimHub at a **Forza
Horizon 5** profile on UDP port **8000**. Host and port can be changed while the
game runs, which helps if something else already owns the port.

## Something not working?

In the settings panel, under *Devices and troubleshooting*, press **Create
support file on Desktop**. It collects your settings, your controllers, what is
actually bound, and the logs into one file. Attach that to an
[issue](https://github.com/d-b-c-e/art-of-sim-rally/issues) — it usually contains the answer.


**Fanatec owners:** your base shows up as two identical `FANATEC Wheel` devices
and the game's controls screen cannot read either of them. Use *Wheel input
(direct)* in the mod panel instead — Assign steering and pedals by moving them —
and pick the 8-axis / 108-button entry in the *Wheel* dropdown for force
feedback. Step by step, with the other common problems, in
[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

## Worth knowing

**Steering assist is a separate switch.** *Direct steering* and *Remove hidden
deadzone* only restore what the game already gives wheels it recognises, so they
change nothing about the car. *Disable steering assist* does change how the car
behaves, and is off by default. It is a legacy car-spawn boolean override, not a
temporary replacement of the game's numeric assist setting; unticking does not
restore the current car. Leave it off and use the game's own assist controls.

**Using Nexus Camera Mod:** the 0.2.4 candidate leaves its chase-camera rotation
alone and suspends our mounted views/tuning when that mod is loaded. Disable it
before a fresh game launch to use our bonnet/bumper views. Settings are preserved.

**Reporting a slowdown:** enable *Log detail for support* for a short reproduction,
pause, then create the support file before restarting. Ordinary logs are available
without detailed logging. 0.2.4 adds bounded log reads, loaded-mod/input details and
frame-hitch counts; see [the instructions](docs/TROUBLESHOOTING.md#collecting-an-intermittent-slowdown-or-ffb-report).

**A shifter gate that also accelerates or brakes** means the game auto-bound that
button to one of its own actions when it saw the shifter as a controller. Reset or
clear the binding in the game's controls screen; the mod reads the shifter directly
and does not need the game to know about it.

**Bonnet, not cockpit.** The cars have no modelled interiors, so a cockpit view
isn't possible.

**Tested on a MOZA R12 Base** (the developer's rig), with positive user reports
on a MOZA R5 and a Thrustmaster T300 RS GT. Fanatec-specific fixes still await
hardware confirmation. The force feedback is plain DirectInput constant force with
nothing vendor-specific in it, so it should work on anything that does force
feedback; per-wheel differences that turned up are handled (see
[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)).

**Known issues** are listed in [docs/KNOWN-ISSUES.md](docs/KNOWN-ISSUES.md).
0.2.3 fixes mounted-camera handback during stock-view, replay and cinematic
transitions. Owner RC6 testing reported working cameras and no stage-start
stutter. [Issue #1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1) remains
open: its reporter separately resolved their symptom by unplugging a PS5 pad,
so that report is not conclusively tied to the camera code defect.

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

This mod supplies the missing piece and the missing plugin. The force is the
front axle's lateral force through a pneumatic trail — centring in proportion
to load, lightening as the front starts to slide — faded out below 12 km/h.
Strength controls output level and Smoothing filters changes; *Invert* is there for wheels that read the axis the
other way. Confirmed on a MOZA R12 and, via a user's log, a MOZA R5.

## Shifters

The game only has *ShiftUp* and *ShiftDown* actions internally, so even a bound
H-pattern lever would behave like paddles — selecting 3rd would mean "one gear up
from wherever you are". Its input layer also cannot see most shifters at all, so
usually there is nothing to bind in the first place.

The mod reads the shifter directly instead, and selects the gear you actually
chose.

The same direct path is available for the wheel itself. Some bases — Fanatec's
direct-drive units in particular — appear to the game's input library as two
identical devices it cannot read, so the controls screen never responds to
them. *Wheel input (direct)* in the mod panel reads steering and pedals from
the device and feeds them to the car, bypassing that library; assign each
control by moving it. Menus still use the keyboard or a pad.

Full technical detail, including how to read any of this out of the game
yourself, is in [docs/](docs/) — start with [FINDINGS.md](docs/FINDINGS.md).
Release history is in [CHANGELOG.md](CHANGELOG.md).

## Building

Needs the .NET SDK. The native force-feedback DLL and the telemetry encoder are
vendored from [dbce-wheel-mod-toolkit](https://github.com/d-b-c-e/dbce-wheel-mod-toolkit)
under `lib/toolkit` (`tools\Sync-Toolkit.ps1` refreshes the pin).

```
dotnet build ArtOfSimRally.sln -c Release
tools\testing\Test-Rc.ps1 -Version 0.2.4-rc.5
```

Use an unused RC number. Releases are built, tested and packaged locally; the
validated ZIP and checksum are uploaded directly to GitHub Releases. No GitHub
Actions build is required. See [the release procedure](docs/RELEASING.md).

Release **0.2.3** adopts the shared toolkit force/device code and improves camera
handback, settings persistence, diagnostics and telemetry recovery. Automated
checks pass. Owner RC6 testing reported no stutter, good cameras and no control
issues so far. The full attended checklist and hardware-specific reports remain
open. See the [release notes](docs/releases/0.2.3.md) and
[testing guide](docs/PRE-RELEASE-TESTING.md).

Next work and outstanding user reports are tracked in the
[roadmap](docs/ROADMAP.md) and [investigation queue](docs/OVERNIGHT-QUEUE.md).

Recording is development-only: a separately installed probe captures signals,
and an external runner replays saved cases without the game or wheel. The release
mod has no recorder or playback feature.

Toolkit pin: **v0.12.0** (recorded in `lib/toolkit/VERSION`). The mod
consumes its native driver, managed device wrapper, versioned axle-force pipeline
and telemetry encoder. See [TEST-DRIVE.md](docs/TEST-DRIVE.md) for attended checks.

The vendored toolkit binaries are committed, so a clone builds and packages
without needing the toolkit repo. Release steps are in
[docs/RELEASING.md](docs/RELEASING.md).

Referencing the game's assemblies requires art of rally installed; override
`GameDir` if it isn't in the default Steam location. Unity Mod Manager's
assemblies go in `lib/umm`. Neither is committed.

## Licence

MIT. No game files are redistributed. `UnityForceFeedback.dll` is an original
implementation of a documented DirectInput API.
