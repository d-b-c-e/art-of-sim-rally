# Crash feel and gear-shift cue — 2026-09-29

## Crash finding

The owner's latest 0.2.7-rc.1 UMM game log contains five accepted crash-kick
requests. The strongest has a 25.4 m/s collision-normal speed and intensity 1;
the native command magnitude is 0.1952. Four smaller contacts request 0.0146,
0.0335, 0.1068 and 0.0119. Their managed expiry follows Play by about
121–130 ms. No native rejection or early managed stop appears in those events.
The installed Settings.xml still contains CrashStrength=19.52381, preserved
from earlier testing. The new-settings default of 50% does not change existing
settings. Normal steering working alongside an accepted command does not show
that the wheel actually delivered a distinct kick.

The next useful comparison is one front impact at the current setting and one
with **Crash strength manually set to 50%**, keeping the wheelbase gain and
other FFB settings fixed. Inspect the fresh support log for accepted magnitude
near 0.5 and stop timing, and ask the driver how it felt. Do not infer physical
torque from API acceptance. No crash waveform, collision threshold, steering or
SimHub telemetry change was made in this candidate.

## Shift signal

Inspection of the locally installed game's `Drivetrain.DoGearShifting` shows
that the delayed path sets neutral near one-third of shift time, engages
`nextGear` near two-thirds, then asks the controller vibrator for a 0.2-second
left-motor cue of amplitude 0.4. The immediate path used by a shifter engages
the target gear but has no equivalent gamepad rumble. A button press could be
rejected or delayed; `shiftTriggered` can remain set. Neither is a reliable
wheel-cue trigger. The game assemblies used for inspection are not committed.

The 0.2.7 candidate observes `Drivetrain.FixedUpdate` before and after. It
emits only on a change to a non-neutral gear in the active player's drivetrain
while the game is driving, focused, not restarting and the settings panel is
closed. This includes reverse and immediate/delayed shifts. It does not alter
game inputs, gearbox behavior, controller rumble, physics or telemetry. The
feature is off by default, with a 5% initial strength and 0–20% range.

It shares the existing finite 25 Hz/120 ms sine handle with landing. A shift
cannot interrupt an impact; either landing or crash can replace a shift. The
constant crash handle remains separate. Focused tests check gear transitions,
player/AI isolation, disabled and paused states, slot sharing, output cap and
priority. These tests establish commands and timing, not physical wheel feel.
The existing saved-drive corpus has no gear field, so it cannot validate real
shift timing. An attended drive should compare automatic/paddle and separate
shifter behavior, neutral/reverse and rapid changes, and confirm no false cue
on pause, restart, menu or finish. Keep it optional until that check.
