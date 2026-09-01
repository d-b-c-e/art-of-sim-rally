# Controls and binding

**Short answer: no binding utility is needed.** art of rally has a proper
native rebinding UI with split-axis support, and Rewired already recognises
most wheels. Verified from the shipped assemblies, 2026-08-31.

## The game's own binder

`ControlsRemapper` in `Assembly-CSharp.dll` is a complete press-to-bind system:

```
Poll  OnPollForElementAssignment  EndPolling
AssignElementToJoystickMap        AssignElementToKeyboardMap
isPollingPositiveSplitAxis        isPollingNegativeSplitAxis
RemoveExistingAxisAssignments     RemoveExistingButtonAssignments
DeleteExistingMapsForAction       RevertCurrentActionMap
RestoreDefaultControls            RefreshDisplayedMaps
JoystickConnected                 JoystickDisconnected
OnControllerAssignmentChangeEvent joystickCount  controlType
```

The two split-axis fields matter most for a wheel: the game can bind **each
half of an axis independently**, which is what combined pedal sets need and
which proves pedals are not modelled as gamepad triggers.

`controlType` with `NextControlType` / `PreviousControlType` means multiple
control schemes, and the `JoystickConnected` / `JoystickDisconnected` handlers
mean hot-plug is handled rather than requiring a restart.

## Rewired knows the wheels

`Rewired.IRacingWheelTemplate` is present, so wheels map onto a standard
racing-wheel element set — steering, throttle, brake, clutch, handbrake,
shifter — rather than anonymous numbered axes.

The Rewired hardware database shipped in this build matches, among others:

> G25, G27, G29, G920, G923, Driving Force, Fanatec, Thrustmaster T150 / T300 /
> TMX, Moza

Recognised devices get real names and on-screen glyphs via `ControllerGlyphs`
and `ControllerButtonDisplay`.

An unrecognised wheel is **still fully bindable** — polling assigns raw
elements regardless — it just shows as "Unknown Controller" with default
calibration and no glyphs. Rewired supports custom controller maps if it ever
comes to that; a mod could inject one.

## Where bindings persist

`Rewired.Data.UserDataStore_PlayerPrefs` — Unity PlayerPrefs, which on Windows
is the registry:

```
HKCU\Software\Funselektor Labs\art of rally
```

It saves controller maps, player data, input behaviours **and joystick
calibration** (`LoadJoystickCalibrationData`), so deadzones, axis ranges and
inversion survive restarts.

On this machine that key does not exist yet, i.e. the game has never been
launched here — the LocalLow folder holds only Steam Cloud saves. The key
appearing is the confirmation that bindings were written.

## Do NOT route the wheel through xoutput / XInput

This was considered as a fallback and is rejected, because it would break force
feedback rather than help it.

Presenting the wheel as a virtual XInput gamepad costs three things:

1. Axis resolution drops to gamepad precision.
2. Separate pedal axes collapse into triggers.
3. **XInput has no force feedback beyond rumble motors.**

The entire FFB route in this project goes through DirectInput — the missing
`UnityForceFeedback.dll` opens a DirectInput device and creates a constant
force effect (see [FORCE-FEEDBACK.md](FORCE-FEEDBACK.md)). Hiding the real
device behind a virtual pad hides it from exactly the API the mod needs.

Native DirectInput is both the better input path and the only one where force
feedback is possible at all.

## Settings that live outside the game

- **Rotation range (900°/1080°)** is configured in the wheel's own driver — G
  HUB, Fanatec control panel, Thrustmaster panel. Note though that
  `Assembly-CSharp.dll` binds `LogiSetPreferredControllerProperties`, so the
  game *can* set operating range itself for Logitech wheels.
- **H-shifter**: `LogiGetShifterMode` is bound, so Logitech shifter mode is
  understood natively.

## To verify with real hardware

1. Plug in the wheel, launch the game, open the controls screen. Note whether
   it is named correctly or shows as "Unknown Controller".
2. Bind throttle and brake as **separate** axes — this is what the split-axis
   polling exists for.
3. Confirm `HKCU\Software\Funselektor Labs\art of rally` appears afterwards.
4. If pedals or an H-shifter enumerate as their own USB devices, confirm they
   bind alongside the wheel. `ControlsRemapper.joystickCount` implies
   multi-device is anticipated, but this has not been observed.

Then move to [ROADMAP.md](ROADMAP.md) phase 0, the force feedback test.

## Related risk

The FFB plugin needs `DISCL_EXCLUSIVE` on the wheel while Rewired already holds
it for input. See the "fighting Rewired for the device" section in
[FORCE-FEEDBACK.md](FORCE-FEEDBACK.md).
