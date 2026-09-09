# Overnight investigation and implementation queue

**Latest status (2026-09-09 UTC): all four post-0.2.4 queue items implemented
with offline coverage, plus a telemetry lifecycle follow-up.** Published stable
remains 0.2.4; a local 0.2.5 candidate is installed. See
[the overnight handoff](reviews/2026-09-09-overnight-025.md) and
[deployment receipt](LOCAL-DEPLOYMENT.md). Next: candidate drive, TSS/Fanatec,
camera/motion checks and first real signal capture. No new hardware sign-off.
Earlier release/RC entries below are historical.

## Next overnight queue — after 0.2.4

Prepared and executed at the owner's request. Four original work items are
complete offline; none implies a successful game playthrough or new force tune.

| Order | Work | Result / remaining acceptance |
|---|---|---|
| 1 | USB identity/reconnect | KI-21 strict GUID channel bindings, unique legacy migration, neutral missing devices and idle recovery. KI-23 extends strict selection to shifters. Combined input/shifter suite: 150 assertions. TSS/Fanatec/USB hardware checks pending. |
| 2 | Last-session stutter diagnostics | Bounded opt-in snapshots at idle/normal exit, build/timestamps and distinct session labels; 51 support assertions including locked files, stale/corrupt data and allocation checks. Real game retention pending. |
| 3 | Lifecycle/performance audit | Reproduced and corrected stale force on missing wheel data (KI-22), stale shifter indices (KI-23), and assignment/edit saves while driving (KI-24). 19 force-lifecycle assertions and 41 watchdog/camera lifecycle assertions. No reported-hitch causation claimed. |
| 4 | Development signal capture | Schema-3 contact/suspension/motion observations, standalone landing/slide/force context; schema-1/2 compatibility, corruption rejection, bounded buffers/events. 42 recorder assertions, 14 actual hook assertions, 31 Python tests. No production recorder/effects or real capture. [Contract](DEVELOPMENT-CAPTURE.md). |
| Follow-up | Telemetry connection/disable lifecycle | KI-25 reproduced socket creation during driving. Setup now runs idle after force release; driving edits wait for pause, disabling parks immediately. 74 actual game-field/loopback assertions. Live consumers pending. |

**Further roadmap audit:** no new GitHub reports. Remaining force effects,
RWD snapback and T300 rotation work needs the first real capture or reporter
hardware A/B. Generic calibration abstractions and full Unity input playback need
separate designs. A bounded telemetry-send worker is a future investigation:
the current sender is synchronous, but no send stall was measured here. Preserve
latest-frame/drop/park/shutdown semantics before adopting one.

**Requires a person or reporter:** final-labelled 0.2.4 drive; full camera/input/FFB
matrix; Nexus CameraMod screen checks; TSS/Fanatec behavior; T300 rotation A/B;
motion/shaker comparison; first real capture. These can be prepared overnight but
must not be marked passed from offline work. Do not change physics, assists or
physical wheel rotation based on an unconfirmed report.

**GitHub refresh:** still one open issue (#1), one existing issue comment, zero PR
review comments and zero commit comments. No new actionable report found; no
messages sent. Use the existing 0.2.4 support draft when the owner wants to reply.

Finish each coherent feature/fix with its relevant tests and documentation, then
run the full local gate for a new immutable candidate (next line: `0.2.5-rc.N`).
Deploy the validated package to the local install as a completion checklist item,
preserving settings and backups. If the game is open, record deployment pending
for the next active session. Scheduled deployment checks are disabled; public
publication remains a separate authorization. Documentation/tooling-only work
that leaves the shipping payload unchanged needs no replacement game install.

## Previous overnight work — historical

Prepared 2026-09-08 UTC after [0.2.3 publication](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.3).
Executed on `codex/overnight-improvements` at the owner's request. The unattended
implementation and research items below are complete; **0.2.4-rc.2 passes all 16
automated checks** and awaits an attended drive. [Exact artifact and handoff](reviews/2026-09-08-overnight.md).
Published 0.2.3 is unchanged. The later owner-requested RC4 install replaces it
locally behind the same Stream Deck target; see the follow-up review below.
The owner requested local deployment when finishing each feature/fix; scheduled
checks are now disabled. See [LOCAL-DEPLOYMENT.md](LOCAL-DEPLOYMENT.md).

Read [USER-FEEDBACK.md](USER-FEEDBACK.md), [KNOWN-ISSUES.md](KNOWN-ISSUES.md) and
[RELEASE-READINESS.md](RELEASE-READINESS.md). The full attended matrix is still
pending; 0.2.3-rc.6 owner smoke passed for cameras, stutter and controls only.

Later T300/TSS feedback authorized another implementation pass. See
[the follow-up review](reviews/2026-09-08-feedback-review.md): CameraMod isolation,
support-log/frame-health improvements, clearer assist help, KI-16 telemetry
correction and actual failed-send handling are now implemented beyond RC2.
**0.2.4-rc.4 passes all 16 automated checks** with those changes; its exact
archive, source identity and pending attended checklist are in that review.
The original work-order table below records the first pass. New wheel effects
and T300 rotation changes still require measured signals/hardware evidence.

## Work order

| Order | Priority / item | Status | Completion evidence / remaining work |
|---|---|---|---|
| 1 | P1 — Camera tuner persistence and log volume, KI-14 | Implemented; offline pass | Actual locked-file reproduction now recovers through watchdog; idle retry, view handback, disable/shutdown and log-volume checks pass |
| 2 | P1 — Camera tuning key remapping, FR-1 | Implemented; UI drive pending | All 11 actions, clear/cancel/defaults, duplicate/modifier validation, XML and panel/capture isolation covered in 182 camera assertions |
| 3 | P1 — TSS handbrake support and input regression, KI-13 | Implemented; TSS pending | 90 input assertions; also reproduced/fixed KI-15 cache, Flip and assignment-read defects. Added live values and confirmed proportional game torque path |
| 4 | P2 — T300 rotation investigation, KI-12 | Static audit and A/B procedure complete | No explicit degrees request in pinned paths; hardware/driver response still unknown; no upstream change justified yet |
| 5 | P2 — Light steering with road/impact feedback, FR-2; RWD snapback, KI-6 | Research and numerical study complete | 10,242 assertions / 4,848 synthetic rows against shared mixer. Headroom retunes steering even without events. New telemetry sampling defects KI-16 need separate follow-up |
| 6 | P2 — GitHub #1 / hardware follow-ups, KI-1/KI-3/KI-5 | Audit refreshed; drafts ready | Still one issue and its existing PS5-unplug comment. No messages sent; reporter/hardware results pending |
| 7 | P1 validation — Real capture and attended checks | Instructions/checklist updated; requires owner | TEST-DRIVE and generated gate include new key/Flip/reconnect cases. No stage driven or real capture obtained overnight |

See [the signal audit](research/2026-09-08-wheel-signals.md) for items 4–5 and
[TEST-DRIVE.md](TEST-DRIVE.md) for the next attended session. The sections below
retain the task rationale and acceptance criteria; statuses above describe the
completed implementation, not a completed hardware sign-off.

## 1. Camera tuner save failures and logging

The 0.2.3 baseline finding in `CameraTuner.Update`: `_dirty` was cleared before calling
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

RC4 is now installed behind the existing Stream Deck Steam 550320 key at the
owner's request; the previous stable install was backed up.
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
