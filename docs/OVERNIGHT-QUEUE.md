# Overnight investigation and implementation queue

Prepared 2026-09-08 UTC after [0.2.3 publication](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.3).
This is a handoff for the next work session, not a scheduled automation or a
claim that the items below have been implemented. Start from current main on a
`codex/` branch. Keep the published tag and archive unchanged.

Read [USER-FEEDBACK.md](USER-FEEDBACK.md), [KNOWN-ISSUES.md](KNOWN-ISSUES.md) and
[RELEASE-READINESS.md](RELEASE-READINESS.md). The full attended matrix is still
pending; RC6 owner smoke passed for cameras, stutter and controls only.

## Work order

| Order | Priority / item | Ready for unattended work | Completion evidence |
|---|---|---|---|
| 1 | P1 — Camera tuner persistence and log volume, KI-14 | Yes: reproduce failed save and view handback in the harness, then fix | Locked-file failure retains retry, recovery saves latest values, view change/disable does not silently lose pending edits; held tuning key no longer logs every frame |
| 2 | P1 — Camera tuning key remapping, FR-1 | Yes, after item 1 | All 11 actions remappable without numpad, cancel/reset work, duplicate choices handled visibly, bind capture cannot move/reset camera; settings roundtrip and legacy defaults pass |
| 3 | P1 — TSS handbrake support and input regression, KI-13 | Docs are done; add focused input tests independently | 0, intermediate values and 1 remain analog through normalization and override; unbound controls unchanged; actual TSS response marked pending |
| 4 | P2 — T300 rotation investigation, KI-12 | Static review/diagnostic design only | Written call-path inventory and a minimal A/B procedure separating degrees, physical lock and logical axis range; upstream change only after reproduced toolkit cause |
| 5 | P2 — Light steering with road/impact feedback, FR-2; RWD snapback, KI-6 | Research and offline signal experiments only | Candidate signal availability, units, limits, resets and independent gains documented; numerical spike/dropout tests; no changed default tune or unattended hardware effects |
| 6 | P2 — GitHub #1 / hardware follow-ups, KI-1/KI-3/KI-5 | Audit and reply drafts complete; external response pending | Reporter verifies 0.2.3 with pad/binding/camera-mod context; Fanatec and original stutter reports get identified builds and scoped outcomes |
| 7 | P1 validation — First real capture and complete 0.2.3 checks | Requires owner drive; prepare commands/checklist only | Final package driven without probe, then separately installed probe captures completed real case; replay passes, case promoted and corpus rerun |

## 1. Camera tuner save failures and logging

New source finding in `CameraTuner.Update`: `_dirty` is cleared before calling
`Main.SaveSettings()`, its boolean failure result is ignored, and "saved" is
logged regardless. Its timer only runs while a mounted view is active. Therefore
the new learned-axis save retry does not cover camera tuning; toggling to a stock
view before the timer expires can leave the camera save pending until another
save or mounted-view update. A held adjustment also formats/writes a log per frame.

Reproduce with an actual locked settings file plus controlled timing and camera
ownership transitions. Keep success logs conditional on successful persistence,
retry idle failures at a bounded rate, and flush/retry through a lifecycle path
that survives leaving the mounted view. Release outputs before shutdown disk IO.
Avoid making every frame a disk-write/log path. Do not claim this explains the
old stage-start stutter: it is a separate path that requires camera adjustments.

Relevant: `CameraTuner.cs`, `Main.SaveSettings`, `DeferredSave.cs`,
`ModWatchdog.cs`, `SettingsPersistence.cs`; regression and lifecycle harnesses.

## 2. Camera keys without a numpad

Use the existing `Settings.Key*` fields rather than inventing a second persisted
mapping format. Expose keyboard rebind, cancellation, clearing where appropriate
and reset-to-defaults; show the current mapping in help. Suppress tuner actions
while listening for a binding or editing the panel. Handle duplicate tuning keys
explicitly and explain possible overlap with game actions: raw Unity input does
not prevent a key from also reaching Rewired. The current source comment claiming
it cannot collide is incorrect.

Make settings accessible with either bonnet or bumper enabled; today the tuning
toggle is nested only beneath the bonnet option. Preserve numpad defaults and
existing XML mappings. Tests should cover cancellation, duplicate handling,
modifier/key validation, persistence, and bumper-only configuration. Actual UMM
key capture, stock-view isolation and another camera mod need screen tests.

## 3. TSS handbrake

The support steps are in TROUBLESHOOTING.md and the unsent draft in USER-FEEDBACK.md.
Current `WheelInput.Update` normalizes non-steering axes to 0..1;
`WheelInputPatch.Override` writes that float to `handbrakeInput` only when bound.
Unbound steering/pedals retain game input. Inspect the remaining game input-to-
brake path only as needed; no grip, braking or assist modifications.

Add integration coverage for partial pulls, reversed axes, zero span, disconnected
device and preservation of unbound channels. A TSS owner must confirm raw and
normalized rest/25%/50%/100% and release, plus gameplay behavior. Do not treat a
different handbrake or synthetic input as a TSS hardware pass.

## 4. Rotation readout

The pinned toolkit's `WheelFfb.cpp` writes `DIPROP_RANGE` 0..65535 for input
normalization and changes autocenter. The initial audit found no physical
degrees-setting call. This does not exclude a driver/profile response to acquiring
FFB. Do not convert the logical range into degrees or blame the toolkit from the
control-panel screenshot alone.

Next: trace FFB initialization/read-only opens and available driver diagnostics,
then prepare an attended baseline with the same driver profile: vanilla, mod with
FFB disabled, mod FFB enabled, direct input separately toggled. Keep third-party
camera mods/pad/virtual-controller state explicit. Record control-panel setting,
measured physical travel and normalized game steering independently. If a toolkit
call is causal, produce a minimal repro upstream, release there and repin here.

## 5. Effect separation and snapback

Current production uses `AxleForceCurve@1` through constant `SetForce`; it has no
dedicated road, crash or landing rumble path. Identify contact transitions, vertical
acceleration and collision signals without changing physics. Compare available
toolkit condition/periodic/event primitives before duplicating them locally.

Separate steering gain from event gain, bound impulses independently of the
low-speed fade (KNOWN-ISSUES U-3), and reset every effect on pause/focus/disable/
shutdown. RWD snapback needs a capture with speed, slip, force history and wheel
motion before choosing damping or more smoothing. More smoothing can add delay;
do not assume it fixes snapback. Keep shipping defaults identical until an attended
tuning comparison supports a change. Offline experiments must never send force.

## 6–7. Human and external follow-up

GitHub audit found only #1 and its existing PS5-unplug comment. Drafts are ready
in USER-FEEDBACK.md; no messages were sent or issue state changed. A future session
can refresh the read-only audit, but should not post drafts without authorization.

The final release is installed behind the existing Stream Deck Steam 550320 key.
The developer probe was removed for the shipping setup. Install it separately
only for the capture portion of [TEST-DRIVE.md](TEST-DRIVE.md), pause before
Start/Stop, then remove it again for final runtime checks. Preserve each completed
capture and use `-Corpus` on subsequent RC runs; generated fixtures remain synthetic.
Do not attempt automated game keyboard injection or present arithmetic replay as
Unity playthrough replay.

## End-of-session handoff

Commit each coherent change with its tests; update this queue, KNOWN-ISSUES,
CHANGELOG Unreleased and `.Codex/session-notes.md`. Run a warnings-as-errors build
and meaningful affected suites. Any new candidate uses a new version/RC number;
do not overwrite 0.2.3 or RC6. Record what passed offline, what is only source
evidence and what requires an attended check. Publishing another release and
changing hardware force behavior are not implied by preparing this queue.
