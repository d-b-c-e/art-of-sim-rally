# User feedback and support follow-up

Recorded 2026-09-08 UTC after publishing 0.2.3. Reports describe their own builds
and setups; they are not all tests of 0.2.3. Implementation work is ordered in
[OVERNIGHT-QUEUE.md](OVERNIGHT-QUEUE.md); defects remain in
[KNOWN-ISSUES.md](KNOWN-ISSUES.md).

## Owner RC6 drive

Exact feedback: "no stutter, camera worked great, and no issues with the controls
so far." Rig: owner's MOZA R12 setup. The installed RC6's local UMM log confirms
toolkit initialization and live driving force evaluation. The log records Strength
50; the earlier Strength 26 observation remains historical. Smoothing is 0.2 in
the preserved installed settings.

This passes a scoped driving smoke check. It does not enumerate every camera
transition, focus recovery, restart persistence or live telemetry consumer case.
The final-labelled ZIP has the same production source/toolkit, but has not yet
been driven. Full checklist and real capture corpus remain pending.

## T300 RS GT / TSS feedback supplied by the owner

Source: pasted user message in this task; mod version and driver/firmware versions
not supplied. Wheel and pedals: Thrustmaster T300 RS GT. Separate TSS is in
handbrake mode; wheel paddles provide shifts. The user drives in the stock high
chase view with another camera mod bringing it closer. Preserve that camera-mod
combination when investigating; they are not using our mounted views.

The user reports markedly better steering than stock, several enjoyable hours of
tinkering, and a strong preference for this wheel experience. Their earlier
x360ce setup supplied controller-like rumble and handbrake mapping; our constant
steering force has different behavior and does not reproduce that rumble track.

| Feedback | Current evidence / next action |
|---|---|
| Set rotation to 700 degrees; Thrustmaster panel shows 1080 after launch, though steering still feels near 700 | KI-12. Cause and actual physical lock unknown. No degrees-setting API found in the consumer or pinned native source. Toolkit sets logical input range 0..65535 and disables autocenter; neither is evidence of a 1080-degree request. Compare physical travel, axis output and panel readout across controlled launch cases. |
| Wants a very light wheel with strong bumps/landing/crash feedback | KI-4 and feature request FR-2. Base Constant/Periodic/Spring/Damper at 100%, overall 80–90%; mod strength not given. Current output is one lateral-force/trail constant force with smoothing. Independent steering/effect gains require a new effects design, not a universal wheelbase preset. |
| Some jumps and crashes give little/no vibration, unlike PS5 rumble | FR-2. No dedicated landing/crash/road vibration channel is currently shipped. Investigate available event signals and contact transitions before promising effects. |
| TSS can change gears but cannot be assigned as a handbrake through stock controls | KI-13. Mod already supports a separately bound Handbrake axis and supplies a 0..1 float to the game's handbrake input. Document the direct-input path, then verify actual TSS intermediate travel and game response. |
| Asks whether analog handbrake is possible | Analog input is implemented; device support and downstream braking response still need attended confirmation. Do not promise a new physics model. |

The pasted message references Discord videos without a direct URL. GitHub issue
#1 independently supplies a [Discord message link](https://discord.com/channels/408108350052630540/1496625082682839120/1496625139847135274)
and two video links. Their media was not reviewed in this audit; no video findings
are inferred.

## Camera controls without a numpad — FR-1

Separate feature request supplied by the owner: allow camera control buttons to
be remapped for keyboards without a numpad. `Settings.cs` already stores 11
`KeyCode` fields and `CameraTuner` reads them, but the panel only exposes the
numpad toggle and fixed help text. The work is a binding UI, conflict handling,
clear help and persistence, preserving existing defaults/settings. It concerns
mounted-view tuning; the game's ChangeCamera binding remains separate. Keyboard
remapping addresses the reported need; wheel/controller button support would
need additional input binding design.

An interim XML example and its limits are in [CAMERA.md](CAMERA.md).

## GitHub audit — 2026-09-08 UTC

Read all issues (including closed), issue comments, PR review comments and commit
comments through the GitHub API after publication. Repository Discussions are
disabled. Result: **one open issue, one issue comment, no PRs, no PR review or
commit comments**. No newer unanswered GitHub report was found.

- [#1: Reverses Camera position for default cameras 3-8](https://github.com/d-b-c-e/art-of-sim-rally/issues/1),
  opened 2026-09-04 for 0.2.1, T300 RS GT. Reporter wants stock/chase views.
- [Reporter's comment](https://github.com/d-b-c-e/art-of-sim-rally/issues/1#issuecomment-5556652229),
  2026-09-06 03:33:55 UTC: unplugging the USB PS5 controller fixed the symptom.
  The attached support file is explicitly after the workaround. Existing analysis
  records mounted views disabled and `ChangeCamera <- Accelerator -` in that
  snapshot. It cannot prove the original before/after state or package version.
- 0.2.3 fixes a separate confirmed camera ownership defect. Owner RC6 success
  does not establish the cause of #1. Keep it open pending a scoped follow-up.

Local API snapshots are under `results/release-0.2.3-preparation`. No GitHub
comments were posted or issues closed during this audit.

## Draft follow-ups — not sent

### GitHub #1

Thanks for reporting the PS5-controller workaround. Version 0.2.3 is now available
and also fixes camera restoration when switching out of our mounted views and
when the game takes over for replays or stage end. Your workaround may describe
a separate input-binding problem. When convenient, please check the stock camera
views on 0.2.3, including with the pad connected if you can reproduce it. Please
check what the game's ChangeCamera action is bound to and include a new support
file if the problem returns. Let us know whether another camera mod is active.

### T300/TSS setup and feel

Thanks for the detailed setup report. For the TSS, open Ctrl+F10, expand Wheel
input (direct), enable Read the wheel directly, and select Assign on Handbrake
while the lever is released. Pull it, then use its full travel once to calibrate.
The mod supports an analog handbrake axis; you can leave working steering and
pedal channels unbound in this section. Check that the displayed handbrake value
increases gradually and returns to zero; use Flip only if the direction is wrong.

The current FFB is a steering-force signal. It has no dedicated landing/crash
rumble channel, so lowering Strength also lowers any detail already in that
signal. Independent steering weight and effect gains are now on the roadmap.

We have not confirmed what changes the rotation readout from 700 to 1080 degrees.
Please include your mod version, Thrustmaster driver/firmware versions and whether
the physical wheel travel changes, ideally comparing the same launch with and
without the mod. A support file will help identify the exact loaded build and
devices. There is no confirmed rotation fix or TSS hardware result yet.
