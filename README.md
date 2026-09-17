# art of sim rally

Drive [art of rally](https://store.steampowered.com/app/550320/) with a racing
wheel, pedals and a shifter. Adds force feedback from the game's tyre forces,
direct USB controls, bonnet/bumper cameras and telemetry for SimHub.

**[Download 0.2.5](https://github.com/d-b-c-e/art-of-sim-rally/releases/latest)** ·
[Setup guide](docs/SETUP.md) · [Troubleshooting](docs/TROUBLESHOOTING.md) ·
[Changelog](CHANGELOG.md)

## Install

For the **64-bit Windows game**. Tested on the Steam version; other stores are
not confirmed. You need the game, your wheel's Windows driver, and Unity Mod
Manager **0.27.0 or newer** (UMM). SimHub is optional. No SDK or separate toolkit
download is needed.

1. **Close the game.** Download and extract
   [Unity Mod Manager](https://www.nexusmods.com/site/mods/21). Run
   `UnityModManager.exe`, select **Art of Rally**, check its game folder, and click
   **Install**. This is a one-time setup for this game.
2. Download **ArtOfSimRally-0.2.5.zip** from the release's **Assets** section.
   Choose the mod ZIP, not GitHub's **Source code** downloads.
3. Right-click the ZIP → **Extract All**. Open the extracted folder and
   double-click **Install.bat**. Wait for the successful verification message.
4. Launch art of rally through Steam. Press **Ctrl+F10**, find **art of sim rally**
   in UMM, and open its settings. Check that version **0.2.5** is listed.

The installer finds Steam libraries on other drives and preserves existing mod
settings. If it cannot find your game, see [custom folders](docs/SETUP.md#custom-game-folder).
UMM's [Mods-tab installation](docs/SETUP.md#installing-through-umm) is also supported.

**Updating:** close the game, extract the new ZIP into a fresh folder, and run
its `Install.bat`. No uninstall is needed. **Removing:** close the game and run
`Uninstall.bat` from the extracted download. Your settings and game bindings stay.

## First drive

Pause before assigning controls or selecting devices. Keep the game window
focused while force feedback initializes.

- **Wheel:** under **Force feedback**, leave **Enabled** on and select your real
  wheel in **Wheel**. Strength defaults to **50**; lower it if steering feels
  heavy. Smoothing defaults to **0.20**; higher values soften rapid force changes
  but add delay. Force builds between 3 and 12 km/h, so test while moving.
- **Controls:** use the game's bindings if they work. Otherwise enable **Wheel
  input (direct) → Read the wheel directly**, click **Assign** beside a control,
  then move it. Use **Flip** if it reads backwards. Menus still use keyboard/pad.
- **Separate USB handbrake or pedals:** assign them in **Wheel input (direct)**.
  Each row can use a different device; leave working game-controlled rows unbound.
  [TSS handbrake steps](docs/SETUP.md#separate-usb-handbrake).
- **Shifter:** enable **Shifter → Use a separate shifter**, choose the device and
  H-pattern/sequential mode, then bind its gears or shift buttons.
- **Landing vibration:** on by default at **5%**, independently of steering
  Strength. Existing saved opt-outs/strengths are kept. Requires wheel sine-effect
  support. This setting controls the wheel; tune a ButtKicker in SimHub.

The mod also removes hidden wheel deadzones and gamepad steering smoothing.
Leave **Disable steering limiter on car spawn (legacy)** off and use the game's
own assist settings.

## Optional cameras and telemetry

**Cameras:** use the game's change-view button to reach bonnet and bumper views.
Adjust the active view with the numpad, or rebind the tuning keys in **Camera**.
Close the panel to use those keys. [Camera setup](docs/SETUP.md#cameras).
If Nexus Camera Mod is loaded, it keeps control and our mounted views are suspended.

**SimHub:** in **Telemetry**, enable **Send telemetry** while paused. Use host
**127.0.0.1**, port **8000** on the same PC, and select **Forza Horizon 5** in
SimHub with the same UDP port. No extra SimHub helper is required.
[Dashboard and ButtKicker setup](docs/SETUP.md#simhub-and-buttkicker).

## Getting help

For a recurring problem: enable **Log detail for support**, reproduce briefly,
pause, then use **Devices and troubleshooting → Create support file on Desktop**.
Either logging checkbox controls the same option. Turn it off afterward. Ordinary
errors/settings can be collected without detailed logging.

Attach the Desktop `art-of-sim-rally-support-*.txt` to a
[GitHub issue](https://github.com/d-b-c-e/art-of-sim-rally/issues), with your wheel,
driver, stage/car and what happened. See [troubleshooting](docs/TROUBLESHOOTING.md).

Tested on the owner's **MOZA R12**, with positive reports from **MOZA R5** and
**Thrustmaster T300 RS GT** users. Fanatec/TSS-specific behavior and the full
hardware transition matrix still need confirmation. Known limits and pending
checks are in [KNOWN-ISSUES.md](docs/KNOWN-ISSUES.md).

## Development

The 0.2.6 candidate adds optional [crash vibration](docs/CRASH-EFFECTS.md),
and extends landing/crash strength to **40%**. Existing strengths keep their
output; defaults remain 5. The stronger range awaits wheel testing.
The public download remains 0.2.5.

[Build instructions](docs/BUILDING.md) · [Documentation index](docs/README.md) ·
[Roadmap](docs/ROADMAP.md) · [Release procedure](docs/RELEASING.md)

The native driver, managed FFB wrapper/force curve and telemetry encoder come
from **dbce-wheel-mod-toolkit v0.13.0**, vendored in this repository. Developer
recording/replay tools are separate and are not included in the release.

## Licence

MIT. No game or UMM assemblies are redistributed. The native force-feedback
plugin is our own DirectInput implementation.
