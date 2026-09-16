# Setup guide

Start with the [four install steps](../README.md#install). This guide covers
custom folders, controls and optional rig setup for **0.2.5**.

## Check the installation

Launch through Steam, press **Ctrl+F10**, and find **art of sim rally 0.2.5** in
Unity Mod Manager. Open the mod's settings. If the entry is absent or red, follow
[installation troubleshooting](TROUBLESHOOTING.md#installation-or-settings-panel-missing).

The game folder contains `artofrally.exe`. Our mod goes in
`Mods/ArtOfSimRally`; there should not be a nested `Mods/Mods` folder.
Keep the extracted download if you want to use its uninstaller later.

## Custom game folder

Steam: right-click art of rally in your Library → **Manage → Browse local files**
to find the folder containing `artofrally.exe`.

If automatic detection fails, open PowerShell in the **extracted download** and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -GameDir "D:\Games\artofrally"
```

Replace the example with your actual folder. Add `-Uninstall` to remove the mod.
This also supplies a path for non-Steam installs, but those game builds have not
been confirmed compatible. UMM must be installed for the selected game first.

## Installing through UMM

With the game closed and UMM installed for **Art of Rally**, drag the **mod ZIP**
onto UMM's **Mods** tab. The archive includes the `ArtOfSimRally` folder at its
root. This is an alternative to `Install.bat`; choose one installation route.

The ZIP includes the native FFB DLL beside the mod. `Install.bat` also installs
a copy in `artofrally_Data/Plugins/x86_64`. Both locations are intentional.
If switching from the batch installer to UMM, prefer the batch installer for
updates so both copies stay in sync. Vortex installation has not been validated.

## Updates, removal and settings

- **Update:** close the game, extract the new release into a fresh folder, and
  run `Install.bat`. It replaces mod files and verifies their hashes. Existing
  `Settings.xml`, other user files and the game's bindings are preserved.
- **Remove:** close the game and run `Uninstall.bat` from the extracted download.
  It removes our payload and native plugin copy, leaving settings and other mods.
  UMM is left installed. The revised installer can also remove our mod after UMM
  has already been removed; the original 0.2.5 installer requires UMM present.
- **Back up settings:** with the game closed, copy
  `Mods/ArtOfSimRally/Settings.xml` somewhere outside the game folder. Restore it
  with the game closed. Game-menu bindings are stored separately by the game.

## Wheel and pedals

1. Connect the wheel and pedals and verify they respond in the manufacturer's
   Windows software. Use the real USB wheel rather than routing it through a
   virtual Xbox controller.
2. In the game, pause and open **Ctrl+F10 → Force feedback**. Enable FFB and
   select your wheel in **Wheel**. Keep the game focused and allow a few seconds
   for setup. If it remains unavailable, toggle **Enabled** off/on while paused.
3. Use the game's control bindings if they already work. If they do not, open
   **Wheel input (direct)** and enable **Read the wheel directly**.
4. Centre the wheel/release the pedal. Click **Assign** beside **Steer**,
   **Throttle**, **Brake** or **Clutch**, then move only that control. Exercise
   its normal full range once. Steering should centre near 0 and move from -1
   to 1; pedals should return to 0 and reach 1. Use **Flip** if reversed.
5. Drive slowly first. If steering input is reversed, use **Flip** on **Steer**.
   If input is correct but the force pulls away from centre, use **Force feedback
   → Invert direction** instead.

Only assigned direct-input rows replace the game's inputs. Different rows can
read different USB devices. Menus continue to use the keyboard or a pad.
If a saved device becomes unavailable, pause, reconnect it, and reassign if needed.
See [Fanatec guidance](TROUBLESHOOTING.md#fanatec-wheels-csl-dd-dd-pro-clubsport-dd-gt-dd-pc-and-compatibility-modes)
for duplicate device names and the limits of current hardware confirmation.

## Separate USB handbrake

TSS means **Thrustmaster TSS Handbrake**; it can also operate as a sequential
shifter. Select its handbrake mode in the device setup before assigning it here.

1. Pause and open **Wheel input (direct)**. Enable **Read the wheel directly**.
2. With the lever released, click **Assign** beside **Handbrake**, then pull it.
3. Move through full travel once, then release. Check the live value at rest,
   partial pull and full pull: an axis should vary gradually from 0 to 1.
   Use **Flip** if backwards.
4. Leave working wheel/pedal rows unbound. Clear conflicting game or separate
   shifter bindings if pulling the handbrake also shifts gear.

An axis binding is analog; a button is on/off. Seeing the TSS as **button 2**
in the Shifter panel only confirms a button, not analog travel. **No profile**
describes the game's recognition and does not mean the mod cannot read it.
Actual TSS travel/braking behavior still needs user confirmation.
[More handbrake troubleshooting](TROUBLESHOOTING.md#separate-handbrake-tss-or-other-usb-device).

## Steering force and landing vibration

| Setting | What to expect |
|---|---|
| **Strength**, default 50 | Overall steering force. Lower it for a lighter wheel. |
| **Smoothing**, default 0.20 | Higher values soften rapid force changes/rattle and add delay. It filters FFB, not steering input. |
| **Landing vibration**, default on | A short wheel vibration at a qualifying touchdown. Saved opt-outs remain off. |
| **Landing strength**, default 5, maximum 20 | Independent wheel vibration strength; smaller landings use less. Zero disables it. |

Steering force fades in between 3 and 12 km/h. Landing vibration requires wheel
sine-effect support; if unsupported, steering can still work. Small hops may not
trigger it. [Details and limitations](LANDING-EFFECTS.md).

**0.2.6 candidate only:** [Crash vibration (experimental)](CRASH-EFFECTS.md)
is off by default, with independent strength 5 and maximum 20. Enable while
paused for testing. Landing and crash vibrations share one effect and do not
stack. The public 0.2.5 download does not include this setting.

Leave **Disable steering limiter on car spawn (legacy)** off. It is not a live
override of the game's assist slider; use the game's assist settings instead.

## Shifter

While paused, open **Shifter → Use a separate shifter**, choose the device and
H-pattern or sequential mode, then click **set** for each gear/shift action and
move the lever. A shifter can be a separate USB device. If a gate also brakes
or accelerates, clear that conflicting binding in the game's controls.

## Cameras

Press the game's change-view button to cycle to bonnet and bumper views.
These are external mounts; the cars do not have modelled cockpit interiors.

| Default numpad keys | Action |
|---|---|
| 8 / 2 | Up / down |
| 7 / 9 | Back / forward |
| 4 / 6 | Left / right |
| 1 / 3 | Tilt |
| + / - | Field of view |
| 0 | Reset active mount |

No numpad? Use **Camera → Enable camera tuning keys → Rebind**. Choose keys that
do not overlap driving controls. Close the panel and release held keys before
tuning. Adjustments save when paused or otherwise idle.

If Nexus **CameraMod** is loaded, it keeps its chase views and our mounts/tuning
are suspended. Disable it before a fresh launch to use our mounts. This does
not resolve every camera compatibility case; see [known issues](KNOWN-ISSUES.md).

## SimHub and ButtKicker

Skip this section if you only use wheel FFB. Telemetry is off by default and
does not change the wheel force settings.

1. Open SimHub and select **Forza Horizon 5** as the receiving game. Use its
   game/UDP settings to check the listening port is **8000**.
2. In art of rally, pause and open **Telemetry**. Enable **Send telemetry**.
   On the same PC, set host **127.0.0.1** and port **8000**. For another PC,
   use that PC's local network address and matching port.
3. Apply setup while paused, then start a stage. Confirm the mod's packet count
   increases and SimHub's speed/RPM respond. A sending counter alone does not
   prove receipt. Art of rally supplies the packets; Forza itself does not need
   to be installed or running.
4. For a ButtKicker, use SimHub's **ShakeIt Bass Shakers** effects, including
   **Impacts** and **Road impacts**. No companion plugin is needed. Tune their
   frequency and gain separately from wheel Landing strength. The owner's rig
   produced a more distinct thud at **30 Hz**; other rigs may differ. If the
   amplifier's CLIP indicator lights, reduce gain rather than raising it.

Choose a free port and change both ends if another application already listens
on 8000. Changes made while driving wait for pause. General receiving/port help
is in [SimHub's official guide](https://github.com/SHWotever/SimHub/wiki/SimHub-Basics----Games-config-and-troubleshooting).
Our [telemetry reference](TELEMETRY.md) describes the exported data.

For errors, missing effects or frozen gauges, [collect a support file](TROUBLESHOOTING.md#collecting-an-intermittent-slowdown-or-ffb-report).
