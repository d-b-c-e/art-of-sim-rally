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

### Leading hypothesis: Raw Input vs DirectInput

Rewired supports several Windows backends (`Rewired.InputSource` in
`Rewired_Core.dll`):

```
None = 0   DirectInput = 1   XInput = 2   Fallback = 4   RawInput = 5
WindowsGamingInput = 30   SDL2 = 19   Steam = 18   ...
```

art of rally is running **Raw Input** - the saved binding keys are prefixed
`WindowsRawInput...`. That matters because the two backends handle an
unrecognised device very differently:

- **Raw Input** reads the raw HID report and needs a hardware definition to
  know which bytes are which axis. With no matching map (our all-zero
  `hardwareGuid`) it has little to go on.
- **DirectInput** exposes the device through its own axis/button abstraction,
  which works for unknown devices. `tools/dinput-enum` proves this reads the
  R12 correctly - 8 axes, 128 buttons, FFB actuators identified.

So the likely fix is to make Rewired use DirectInput for this device, or to
give Raw Input the hardware map it is missing. Both are things a mod can do;
the backend is selected in the Rewired configuration, so a patch would have to
land **before** `ReInput` initialises. This is a hypothesis with a clear test,
not yet a confirmed diagnosis.

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
