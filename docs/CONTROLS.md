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

## Rewired's wheel database - and what is NOT in it

`Rewired.IRacingWheelTemplate` is present, so a *recognised* wheel maps onto a
standard racing-wheel element set - steering, throttle, brake, clutch,
handbrake, shifter - rather than anonymous numbered axes.

Genuine hardware-map entries in this build, each with a real GUID and VID/PID:

> Logitech G25 / G27 / G29 / G920 / G923 / Driving Force, Thrustmaster T150 /
> T300RS / TMX / T-GT, Fanatec Porsche 911 Turbo S

**Moza is not among them.** An earlier version of this document claimed it was;
that was a false positive from grepping the assets for "Moza", which matched
**Mozambique** in the country localisation table. The Rewired build shipped here
predates Moza's wheelbases entirely.

### Verified case: MOZA R12 Base

DirectInput sees the base perfectly well (`tools/dinput-enum`):

```
[2] MOZA R12 Base            VID_346E&PID_0006
    type          : 1STPERSON            <- not DRIVING
    axes/buttons  : 8 axes, 128 buttons, 1 POV
    FORCE FEEDBACK: YES
        axis: X Axis   [FFB actuator]
        axis: Y Axis   [FFB actuator]
```

Rewired also sees it and creates a joystick map for it - but unrecognised:

```
hardwareIdentifier = WindowsRawInputMOZAR12Base0006346E
hardwareGuid       = 00000000-0000-0000-0000-000000000000
```

The all-zero `hardwareGuid` is the signature of an Unknown Controller: no
hardware map matched, so no Racing Wheel Template, no element names, no glyphs.
Note the backend is **Raw Input**, not DirectInput.

### Symptom and first thing to check

Observed 2026-08-31: the controls screen offered only "keyboard" and
"joystick", and rebinding produced no response from the wheel.

Three joystick devices are connected here - DS-8X Shifter, MOZA R12 Base, MOZA
Multi-function Stalk - and `ControlsRemapper` exposes `joystick` (singular),
`activeController` and `joystickCount`, i.e. it targets **one device at a
time**. If it defaults to the shifter or the stalk, the wheel produces nothing.
**Check for a device selector on that screen before concluding anything else.**

### CONFIRMED root cause: Rewired's hidden 10% axis deadzone

Measured in game on 2026-08-31, MOZA R12 Base. The mod logs every axis before
touching it:

```
Rewired calibration for 'MOZA R12 Base' (recognised=False)
  before: [0] dz=0.100 sens=1.00  [1] dz=0.100 ...  (all 32 axes)
  after : [0] dz=0.000 sens=1.00  [1] dz=0.000 ...
```

**Rewired applies a 10% deadzone to every axis of an unrecognised controller.**
On a wheel set to 270 degrees that is +/-13.5 degrees - a 27 degree dead band
at centre.

There are two independent deadzones, and the options screen only reaches one:

1. **The game's.** `AxisCarController.GetInput` calls
   `ProcessDeadzoneForInput(GetAxisRaw(steerAxis), SettingsManager.GetSteeringDeadzone())`.
   That function is a plain cutoff and does nothing at 0. This is what the UI sets,
   and it is innocent.
2. **Rewired's.** `AxisCalibration.deadZone`, applied *inside* `GetAxisRaw`
   before the game sees a number - "raw" means unsmoothed, not uncalibrated.
   Nothing in the game's UI exposes it, and an unrecognised device has no
   hardware profile to override the 0.1 default.

This is why the wheel feels fine in every other game: they do not route it
through Rewired's unrecognised-device defaults.

Fixed by `WheelCalibration` in the mod, which zeroes the deadzone and forces
linear sensitivity. Reported as a large improvement in feel.

Because Rewired's shipped database predates every modern direct-drive base,
this very likely affects Moza, Simagic, Simucube, Fanatec DD and Asetek users
equally - not just this one wheel.

### Options, cheapest first

1. Cycle the remapper to the MOZA R12 Base.
2. Moza Pit House compatibility settings. A `1STPERSON` device type with 128
   buttons is unusual HID presentation; Pit House can change how it enumerates.
3. **A mod that registers a Rewired hardware map for the R12** (`346E:0006`)
   with a Racing Wheel Template mapping. Rewired supports custom controller
   definitions, so this is well-scoped and gives real axis names and sane
   defaults instead of Unknown Controller.

A binding utility is still the wrong answer - see the xoutput section below.
Teaching Rewired the device beats flattening it to a gamepad.

### Raw Input vs DirectInput — tried at runtime, abandoned (2026-09-02/03)

Rewired supports several Windows backends (`Rewired.InputSource` in
`Rewired_Core.dll`): `DirectInput = 1`, `XInput = 2`, `RawInput = 5`, and
others. art of rally runs **Raw Input** — saved binding keys are prefixed
`WindowsRawInput...`. Raw Input parses HID reports itself and has two failure
modes seen in the wild:

- **Devices it never lists.** Shifters and stalks report as supplemental HID
  devices, not joysticks; the game logs "found 1 joysticks attached" while
  joy.cpl shows three.
- **Devices it lists but cannot read.** A Fanatec direct-drive base (support
  bundle, 2026-09-02) appears twice as `FANATEC Wheel` with *identical*
  hardware ids and 32 axes / 144 buttons — Rewired's ceiling for a descriptor
  it failed to parse. Both entries are assigned to the player, nothing is
  bound, and the controls screen never sees an element move, so no binding
  fix can help. DirectInput on the same machine reads them as 8 axes / 108
  buttons and 12 / 63, distinct.

`ReInput.configuration.windowsStandalonePrimaryInputSource` has a runtime
setter, and it calls Rewired's `ResetAll()` — a full teardown and rebuild of
controllers, assignments and maps. A first attempt (2026-09-01) applied it
during mod load and killed the keyboard with no in-game way back; it was
removed twice without the cause being established. The instrumented re-test
on 2026-09-02 (every keypress probed through `UnityEngine.Input` and through
Rewired independently) established what matters:

| Applied after the title screen is up | Result |
|---|---|
| Keyboard, 15 presses under DirectInput | seen by Unity and by Rewired, every one |
| Saved keyboard maps | intact (3 before, 3 after) |
| Joysticks enumerated | 1 on Raw Input (base) → 3 on DirectInput (base, stalk, DS-8X) |
| Switching back in-process | works; Raw Input bindings return |

That looked like the whole story, and was not. Applied later in the same
session from the settings panel — and then again at the title screen with the
switch pre-armed — the switch left the **menus** dead while every probe said
input was flowing: 30 and then 67 keypresses seen by Unity, by Rewired's
keyboard controller *and* by the player's actions (`UISubmit`, `UICancel`,
`UIHorizontal` all firing), keyboard maps enabled and identical, Rewired's UI
input module alive with player 0. The game's own log showed 48,216 "object
created by a previous session ... no longer valid" errors — stale Rewired
objects cached by game code (`ControllerButtonDisplay`, `Arcader`) — and
refreshing all 84 of them brought the count to zero without bringing the
menus back. Whatever else `ResetAll()` breaks in this game's menu code was
not found, and after four attempts across two days it was abandoned.
**The switch stays in the code as an experiment reachable only through
`UseDirectInputBackend` in Settings.xml; it is not in the panel.**

### The fix that shipped: read the wheel directly (2026-09-03)

Instead of making Rewired see the device, the mod stops needing Rewired for
the wheel. The native plugin already enumerates every DirectInput controller
for the shifter; `OpenReadDevice` / `ReadDeviceState` open each one
non-exclusively for reading (the force-feedback wheel is read through the
exclusive handle already held) with axes requested in 0..65535.
`WheelInput` binds Steer, Throttle, Brake, Clutch and Handbrake to an axis or
button by "Assign, then move it": the value at rest and the value it moved
to are recorded, the far end keeps extending as the control is used, steering
maps the recorded direction to +1 and the other lock to −1, pedals map rest
to 0 and the moved direction to 1 (so pedals that idle at the top of their
range work too). A postfix on `AxisCarController.GetInput` writes bound
channels over the game's values after its own deadzone processing and keeps
the steering-alignment effect, so direct steering, steer assist and telemetry
all see what they would from a wheel Rewired understood. Unbound channels
are untouched; menus still use keyboard or pad.

Verified 2026-09-03 on the MOZA rig: all three controllers open, the base
reads through the FFB handle (steering centred at 32669), pedals on the base
at rest. Driving with bound channels: see the release notes.

### Why the device presents unusually

DirectInput's hard ceiling is 8 axes, 128 buttons and 4 POV hats. The R12
reports **exactly 8 axes and 128 buttons** - it saturates the envelope, because
Moza aggregates base, rim, pedals and accessories into a single HID device.
That is also why DirectInput classifies it `1STPERSON` rather than `DRIVING`.
There are widespread reports of Moza buttons above index 32 going undetected in
various games, which is the same root cause seen from a different angle.

### This does not block force feedback

Force feedback goes through DirectInput, which reports the R12 with FFB
actuators on X and Y - a direct match for `SetDeviceForcesXY`. Binding trouble
and FFB are independent problems; phase 0 can proceed regardless.

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
