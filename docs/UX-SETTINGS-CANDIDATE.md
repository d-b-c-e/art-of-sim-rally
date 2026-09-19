# Settings candidate: short test guide

This guide applies to the **installed 0.2.6-rc.12 Simple/Advanced candidate**,
not the public 0.2.5 download. It replaced RC10 after all 16 local gates on
2026-09-19; see [Local deployment](LOCAL-DEPLOYMENT.md) for the exact identity,
backup and receipt.
The [engineering audit](UX-OVERNIGHT-2026-09-16.md) records coverage and remaining
gaps. The usual installer still preserves Settings.xml; start any attended
comparison with an extra backup of that file.

**RC9 smoke found a blocker:** navigation/Escape can reach the game menu behind
the panel, and default4K text is too small. RC10 contains the reviewed fixes
and passed all16 local gates. Live keyboard isolation/recovery passed, but
default4K layout still clipped controls. RC12 corrects host-bound widths and
large-scale row stacking, passed all16 gates and is installed; its rendered
retest is pending. See
[RC10 results](reviews/2026-09-19-rc10-ui-smoke.md) and
[the recorded failure and retest](reviews/2026-09-19-rc9-ui-smoke.md).

## Open and stop

Press **F6** to open this mod's settings. **Ctrl+F10** still opens Unity Mod
Manager, where the same panel is available. F6 is rebindable under
**Controls → Mod buttons**, with optional separate USB Settings/Stop buttons.
**F8** or **Stop FFB** turns feedback off and saves
that choice; choose **FFB → On** explicitly when ready to resume it.

Opening settings suppresses steering, pedal/handbrake, shifter and wheel-force
output from the mod. **Pause the game first**: opening UMM is not a promise
that the game simulation is paused. A held shifter button must be released
before shifting again. Close/Escape cancels an unfinished edit first; close a
second time to leave. Camera adjustment keys wait for release after closing.

**Simple** is the first view. **Advanced** adds tuning on the same six pages;
switching views preserves the actual values. Your view/page choice is saved.
Custom hidden settings have a **Review in Advanced** link. The save status is
visible above the pages; a pending calibration or connection edit is not saved
until you use its Save/Apply button. Keep a mouse/keyboard available for UMM;
wheel-only menu navigation is not provided by this panel.

## Controls and separate USB devices

Keep working game bindings. Under **Controls**, each assigned row can read a
different USB device. To bind or recalibrate:

1. Pause; centre the wheel or release the pedal/lever. Click **Bind** on its row.
2. Move only that control. For steering, turn fully both ways and return to
   centre. For a pedal or handbrake, use full travel and release.
3. Check the candidate input, adjust inversion/deadzone if necessary, then
   **Save calibration**. Saving a driving control enables **Use assigned
   controls**; saving a mod/camera button does not. Cancel, timeout, a failed
   write or a disconnected candidate keeps the previous binding. A failed save
   leaves the draft available for retry or Cancel.
4. Check the saved bar: steering reads **Centre**, then Left/Right percentages;
   pedals and an analog handbrake read 0–100%. It shows device input, not a
   measurement of game physics. Explicitly calibrated ranges do not extend
   themselves during a drive.

**Handbrake (axis)** and **Handbrake (button)** are separate. Their values and
the game's handbrake input combine by taking the largest value; a held button
contributes 100%. A TSS in handbrake mode belongs on the axis row if it reports
an axis. If it only reports a button, use the button row. The new UI has not yet
been physically checked on a TSS; an axis travel fixture is not that check.

Clutch and shifter bindings expand on demand. **Open game bindings** routes to
the existing game controls panel for Change camera, Look behind, Reset car and
Pause. If unavailable in the current scene, use **Options → Controls** from the
main menu. The mod does not duplicate that binder's maps. Devices the game's
Raw Input cannot read still need keyboard/pad for game button actions; direct
USB axes, shifter and mod/camera buttons use the mod's reader independently.

## FFB

Choose **Use steering wheel** to follow the identity saved in the mod's Steering
row, or select a specific wheel. If steering is bound only in the game's own
controls, select the FFB wheel explicitly here. Missing/unverified devices stay
unavailable; the mod does not substitute a different controller. Refresh/retry
while paused. A device selection does not change a saved FFB Off choice.

**Upgrading an older selection:** if FFB says the saved wheel has no verified
identity, select it again once on this page. On this rig, choose **Use steering
wheel** to use the already saved MOZA steering identity, or select the MOZA
explicitly. This step is necessary even though the old wheel name is displayed;
the old name/index alone does not identify an output device reliably.

Simple shows On/Off, device and Strength. Advanced also shows smoothing,
direction, landing vibration and experimental crash kick. Steering Strength
50 retains the existing reference tuning. Landing defaults to 5 (range 0–40);
crash defaults to 50 (range 0–100) and remains off for new settings. Existing
values are preserved. These effect strengths are percentages of nominal wheel
force; they do not scale telemetry or SimHub. The crash candidate still needs
the owner's physical acceptance independently of the UI tests.

## Cameras, telemetry and help

**Cameras:** enable bonnet/bumper and expand **Adjustment bindings** to rebind
keyboard shortcuts or USB buttons. F8, F10 and the Settings key are reserved;
Restore numpad defaults checks the whole batch before changing any key. These
keyboard edits also check the game's loaded keyboard maps and name conflicts;
this includes inactive contexts and modified game chords. Unavailable maps keep
the old assignment. Changing bindings later in the game's own menu can introduce
a conflict again, so choose separate keys there too. Advanced
adds pose, field of view, lean, shortcut rates and Reset this view. Nexus
CameraMod retains priority. Choose buttons not used by game actions: there is
no guessed association between DirectInput button numbers and Rewired maps.

**Telemetry:** Simple shows On/Off and the saved destination. Use the explicit
local preset only if SimHub listens on 127.0.0.1:8000 with Forza Horizon 5
selected. **Advanced → Edit connection** stages host and port together;
**Apply connection** commits both, and Cancel changes neither. Connection
work still waits for pause. “Sending” does not confirm receiver delivery.

**Help:** create a support file without changing logging. For a recurring
problem, use **Advanced → Help → Log detail for support**, reproduce briefly,
pause, create the support file, then turn detail off. No file is uploaded.
The recorder remains a separate development probe and is not in the release.

**Settings text size:** Auto enlarges this mod's content with screen resolution
when UMM uses its default1x scale. A custom UMM scale takes priority. Choose
**Use UMM scale** to follow the host exactly, including1x. The surrounding UMM
window retains its saved dimensions and own text; resize it in UMM Settings.

## Attended acceptance still required

Check all pages at 720p and the rig's normal DPI/font scale, F6/UMM navigation,
Escape/cancel, keyboard focus, reconnect/reorder, and a real calibration. Test
FFB Off across restart and F8 while driving, with ordinary steering, landing
and crash behavior after leaving settings. Offline checks do not establish
layout quality, physical torque, menu focus behavior or full UX-1 compliance.
