# Troubleshooting

Start with the support file: Ctrl+F10 → *Devices and troubleshooting* →
**Create support file on Desktop**. It shows what the game's input library sees,
what DirectInput sees, and what the mod did — and the two views disagreeing is
usually the answer. Attach it when you report a problem.

## Fanatec wheels (CSL DD, DD Pro, ClubSport DD, GT DD; PC and compatibility modes)

Fanatec bases present to Windows as **two devices with the same name**,
`FANATEC Wheel`. That causes three distinct problems, each with its own fix.

**1. The game's controls screen never responds to the wheel.**
The game's input library (Rewired, Raw Input backend) cannot read either
device — both show as 32 axes / 144 buttons and never report a movement, in PC
or compatibility mode. No binding trick fixes this; the mod reads the wheel
itself instead.

- Ctrl+F10 → **Wheel input (direct)** → tick *Read the wheel directly*.
- **Steer → Assign**, then turn the wheel (either direction). **Throttle →
  Assign**, press the throttle. **Brake → Assign**, press the brake. Clutch and
  handbrake if you use them (a button works for handbrake).
- Drive. The first full press of each pedal and the first full turn calibrate
  the range. If a control runs the wrong way, press **Flip** on that row.
- Menus still use the keyboard or a gamepad; that is expected.

**2. Force feedback is dead, or was dead until 0.2.2.**
Only one of the two `FANATEC Wheel` devices has the motor. Picking the other
one in the *Wheel* dropdown used to fail silently. From 0.2.2 the mod tries
every force-feedback device before giving up, and the dropdown shows axis and
button counts: the **8 axes / 108 buttons** entry is the one with force
feedback, the 12 axes / 63 buttons entry is the rim and its buttons. If the
direction is wrong, tick *Invert direction* under Force feedback.

**3. Choosing a shifter crashed the game (before 0.2.2).**
Fixed. It happened only when force feedback had failed to initialise, which on
a Fanatec was the wrong-twin case above.

**Pedals on a separate USB cable** (ClubSport V3 etc.) appear as their own
device and are read the same way: Assign, press the pedal.

## The wheel steers the wrong way with direct input

Press **Flip** on the Steer row (Ctrl+F10 → Wheel input (direct)). From 0.2.2
assignment no longer depends on which way you turned during Assign, but a wheel
whose axis runs backwards would still need it.

## Force feedback too strong or too weak

Use *Force feedback → Strength* to adjust force level; 50 is the default.
Force fades in between 3 and 12 km/h. Smoothing controls how quickly force
changes reach the wheel; keep it consistent when comparing builds.

## Force feedback stops after alt-tab (before 0.2.1)

Fixed: the wheel came back non-exclusively acquired and nothing re-acquired
it. If you still see it, the support file's force-feedback section will show
`0x80040205` counts; report it with the file.

## No force feedback at all

- In 0.2.3 candidate support files, compare **build**, **mod sha256**, **mapped
  file** and **file sha256** with the candidate manifest. `preload requested` is
  only the path requested, not proof of which module was bound. Inspection does
  not load the plugin; `(not loaded)` may simply mean FFB/input is disabled.
- The installer intentionally puts UnityForceFeedback.dll in both the mod folder
  and Plugins/x86_64. **Do not delete a copy just because it is in Plugins.**
  If hashes differ, close the game and reinstall the complete package, then
  capture a fresh support file. Multiple resident modules are reported as ambiguous.
- Toolkit pin **v0.12.0** and native component **0.5.0** are different version
  sequences; that combination is expected. Older support files call the latter
  `native abi`, and their `loaded from` field only recorded the preload request.
- The support file's force-feedback section says whether the mod computed
  forces and whether the device refused them (`SetParameters FAILED`).
- Another program holding the wheel exclusively (a second game instance, a
  crashed one, some wheel utilities) blocks force feedback. Check Task Manager
  for a leftover `artofrally.exe`.
- The wheel's own software must be in PC mode with force feedback enabled.

## A stranded force after a crash

If the game dies with force applied, the base may keep pushing. Power-cycle the
base, or use the wheel software's stop-FFB control. Closing the game normally
zeroes the wheel first.

## Shifter gate maps to the throttle

The game auto-bound a shifter gate to an axis it took for the throttle. Reset
the game's own controls once (its options screen); the mod reads the shifter
directly and needs no game bindings.

## Gamepads, vJoy and virtual controllers

A virtual controller (vJoy, XOutput, ViGEm) can look exactly like a wheel to
the game and steal the force-feedback slot. Disable it while playing, or pick
the real wheel explicitly in the *Wheel* dropdown.

## The camera moves about at the end of a stage

The 0.2.3 candidate restores the stock camera when the game enters a replay or
results cinematic. The code fix still needs visual confirmation. Note which
mounted/stock view you used beforehand and attach a support file and short video.
For reversed stock views, also inspect the ChangeCamera binding and record whether
a PS5 controller is attached: that resolved issue #1's original reporter's symptom.
See [KNOWN-ISSUES.md](KNOWN-ISSUES.md) for the distinction between the report and
the camera handback defect.

## Telemetry stays off after a connection error

Check the host and UDP port in the mod panel. In the 0.2.3 candidate, correcting
the destination retries the connection; turning telemetry off and back on also
clears the failed state. An unchanged failing destination stays quiet instead of
repeating connection work every physics step. If it still fails, retain the error
from the support file and check the receiving application's host/port settings.
