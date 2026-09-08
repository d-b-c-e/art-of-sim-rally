# Known issues

The register of what is broken, what is unverified, and what was deliberately
abandoned. One entry per problem, newest first within each section.

**Reporting something new:** open an [issue](https://github.com/d-b-c-e/art-of-sim-rally/issues) with a support file
(Ctrl+F10 → *Devices and troubleshooting* → **Create support file on Desktop**).
User-facing fixes by symptom live in [TROUBLESHOOTING.md](TROUBLESHOOTING.md);
this file is the engineering view, including things a user cannot act on.

Severity is about the effect on driving, not on how annoying it looks:

| | |
|---|---|
| **blocking** | the feature cannot be used |
| **major** | the feature works but wrongly, or only on some hardware |
| **cosmetic** | visible, no effect on driving or results |
| **unverified** | believed to work; nobody with the hardware has confirmed it |

---

## Open

### KI-14 — Camera tuner loses failed-save retry and logs each adjustment frame

**Persistence/supportability; reproduced in 0.2.3, fixed in next-version source.**
`CameraTuner.Update` clears `_dirty` before `Main.SaveSettings()`, ignores its
failure result and logs success unconditionally. Its timer only runs while a
mounted view is active, so switching away before the timer expires can leave the
edit unsaved until another save or mounted-view update. Held tuning keys also
produce a log line per frame. These are separate from the learned-axis save
policy fixed in KI-8; there is no evidence they caused KI-5's stage-start stutter.

Locked-file reproduction recorded one failed attempt, a false success log and no
retry. The fix uses the persistent watchdog to save while idle and retain failures
with five-second retry backoff. Held adjustments produce no per-frame log.
Production tuner/writer tests cover locked-file recovery, latest edits, debounce,
driving/disable and shutdown; lifecycle tests check output release before writes.
Attended persistence remains pending. No fix is in the published 0.2.3 archive.

### KI-13 — TSS handbrake assignment is not discoverable through stock controls

**Setup; user report, TSS validation pending.** T300 RS GT + TSS user can assign
shifts but not handbrake in the game's controls UI. Mod version was not supplied.
The mod's direct-input panel already supports a Handbrake axis: it normalizes to
0..1 and overrides the game's float handbrake input, retaining unbound channels.
This is not evidence that TSS is unsupported or that the mod handbrake is digital.

[Direct binding steps](TROUBLESHOOTING.md#separate-handbrake-tss-or-other-usb-device)
are documented. Confirm intermediate travel, release and actual braking behavior
on the TSS before claiming hardware verification. See [user feedback](USER-FEEDBACK.md).

### KI-12 — T300 rotation panel changes from 700 to 1080 degrees after launch

**Unverified cause and physical effect; reported 2026-09-08 UTC.** The user sets
700 degrees in the Thrustmaster panel and sees 1080 after starting the modded game,
while steering still feels near 700. Driver/firmware and mod version are unknown.

No physical degrees-setting call was found in the consumer or pinned toolkit
v0.12.0 native source. The toolkit does set logical `DIPROP_RANGE` to 0..65535 and
disable autocenter. A logical axis range is not a physical rotation request;
driver response to initialization remains untested. Compare vanilla/mod/FFB/direct
input with consistent profiles and record actual lock separately from the panel.
Do not change rotation behavior or blame a component before reproducing it.

### KI-1 — Stock cameras 3–8 render reversed

**Major; open pending validation.** Reported on a T300 RS GT with mod 0.2.1 in
[issue #1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1).

**New evidence (2026-09-06 UTC):** the reporter says unplugging a USB PS5
controller resolved the symptom. The attached
[support snapshot](https://github.com/user-attachments/files/31874196/art-of-sim-rally-support-20260905-191759.txt)
shows both mounted views disabled and `ChangeCamera <- Accelerator -`. It reports
assembly version 0.1.0.0, which cannot establish the package version. It is an
after-unplug snapshot, not a before/after reproduction; older log entries in the
same bundle must not be confused with current settings.

There is also a real code defect: the mod writes the rendering child's world
transform, while the stock CarCameras rig only moves its parent. CameraManager
initializes the child at local identity. The original code failed to restore it
when relinquishing a mounted view. The 2026-09-04 fix restores that invariant,
but tying it conclusively to this reporter's issue was premature.

The RC tracks and restores the exact child it owned, restores FOV, hands back on
stock-view selection and mod/feature disable, and selects a usable stock view
instead of leaving a zero-distance placeholder active. Offline ownership tests
pass; owner RC6 feedback on 2026-09-08 UTC says the camera worked great. This
does not reproduce the reporter's pad/binding setup. Check stock views before/after each
mounted view and compare pad attached/absent where available. Do not close the
GitHub issue solely from these code tests.

### KI-2 — The camera moves oddly at the end of a stage

**Cosmetic; fixed in 0.2.3, owner smoke passed; full matrix pending.** Observed on the owner's
rig and independently reported on Reddit for replays and stage end in 0.2.2.

The earlier parent snap did not restore the rendering child's local transform.
The two-line child fix also had a lifecycle gap: CameraManager disables
CarCameras in `EnableCinemachineCamera` and `DisableCameraManagers`, so its
LateUpdate callback need not run at the handback. Verified in the installed
build 17584229 on 2026-09-06.

The RC releases the child before those transitions, with a persistent watchdog
fallback. It does not snap the parent while the cinematic system owns it.
Owner RC6 feedback on 2026-09-08 UTC: "camera worked great". The report did not
enumerate every transition. Finish, replay, intro, pause/resume, stage restart
and unload remain checklist items; test doubles do not establish rendered correctness.

---

### KI-3 — Fanatec fixes are unverified on Fanatec hardware

| | |
|---|---|
| **Since** | 0.2.2, 2026-09-03 |
| **Severity** | unverified |
| **Status** | awaiting a report from the user who supplied the support bundle |

Three fixes shipped in 0.2.2 came from a single Fanatec support bundle and were
verified only by reasoning plus initialisation on a MOZA rig:

- direct wheel input (`WheelInput`) on a base Rewired cannot read;
- the crash when choosing a shifter after force feedback failed to initialise;
- trying every force-feedback candidate when the preferred device has no
  actuator.

The failure they address is specific to a base that presents twice under one
name, which no machine here does. Until a Fanatec owner confirms, treat
[TROUBLESHOOTING.md](TROUBLESHOOTING.md)'s Fanatec section as a best hypothesis
rather than a tested procedure.

0.2.3 adopts toolkit v0.12.0 and stores new explicit wheel selections by strict
DirectInput instance GUID. This distinguishes identically named devices; the
Fanatec setup still needs confirmation. See [ROADMAP.md](ROADMAP.md).

---

### KI-4 — Force scaling is tuned for one wheel

| | |
|---|---|
| **Severity** | major on hardware unlike a MOZA R12 |
| **Status** | open by design; `Strength` is the mitigation |

`FyReference` sets what lateral force counts as full scale, and it has been
retuned twice on the same rig (6,000 → 8,000 → 11,500 N). A gear-driven
Logitech and a 12 Nm direct-drive base want very different numbers, and only
`Strength` is exposed in the panel; `FyReference` itself is Settings.xml only.

This is tuning, not incompatibility — nothing in the force path is
vendor-specific. It stays open because there is no per-wheel default and no way
to acquire one without reports.

Data points so far:

| Rig | Base gain | Strength | Smoothing | Notes |
|---|---|---|---|---|
| MOZA R12 (owner, 2026-09-04) | — | **26** | 0.2 | Historical setting after the `FyReference` retunes; not the current installation |
| Unstated wheel + motion platform (Reddit, 2026-09-04) | 100% | **15** | **0.50** | "works perfect"; raised smoothing specifically to kill notchiness over low-poly inclines |
| MOZA R12 (owner, RC6 drive 2026-09-08 UTC) | — | **50** | 0.2 | Strength from live log and preserved settings; owner reports no control issues, not a separate force-tuning comparison |
| Thrustmaster T300 RS GT (message recorded 2026-09-08 UTC) | 80–90% overall; effect categories 100% | not supplied | not supplied | Wants light steering with strong surface/landing/crash feedback; see FR-2 in USER-FEEDBACK |

The first two reports favored lower gain; the owner subsequently used 50. These
observations do not justify a universal new default. The T300 request also needs
independent effect gains rather than merely a third retune on one rig's opinion.

---

### KI-5 — Stage start stutters and briefly locks up for 10–15 seconds

**Major; plausible contributor fixed, reported symptom unverified.** A Reddit
user first noticed this with 0.2.2. It is not established whether it occurs on
every restart, every new stage or only a cold launch.

`WheelInput` previously saved Settings.xml synchronously when learned ranges
extended, rate-limited to five seconds. Those writes could occur during the
opening seconds of driving. The 2026-09-04 change keeps range updates in memory
and defers disk writes. RC review additionally fixes a failed-save path that
cleared the dirty flag anyway, and moves shutdown persistence after output release.
Failed saves now stay pending and retry at most every five seconds while idle.

Offline policy and shutdown-order checks pass. They do not demonstrate that disk
IO caused the user's stutter. Owner RC6 testing on 2026-09-08 UTC reported
"no stutter"; the original Reddit report is not yet confirmed resolved.
Compare cold stage, same-stage
restart, different stage, and mod-disabled baseline using the optional capture
and [testing checklist](PRE-RELEASE-TESTING.md).

If it persists, investigate device discovery/opening on the first direct-input
update, native driver polling, and game/shader loading. FFB is not strictly
arithmetic-only: it calls native SetForce, and DiagnosticLogging formats/writes
traces. Keep diagnostic settings consistent across comparisons. Explicit camera
tuner and UI saves remain user-triggered and are not the learned-range hot path.

### KI-6 — The wheel snaps back as the car straightens out of a slide

**Major for feel; open.** The Reddit user praised 0.2.2 with wheel FFB at 100%,
mod Strength 15 and Smoothing 0.50, but reported snapback/tank slappers in powerful
RWD cars. That report does not establish a single cause or justify physics changes.

The current output has a lateral-force/trail signal and smoothing, with no explicit
wheel-rate damper. A hardware damper is a candidate experiment; wheelbase settings,
filter delay, changing tyre forces and the car's own behaviour also matter.
Toolkit condition effects are available but unused. Keep this work out of the
maintenance RC; tune against attended captures and compare at the reporter's settings.

### KI-7 — Candidate identity and native-path diagnostics were misleading

**Supportability; fixed in 0.2.3, live support output pending.** Info.json remained
0.2.2 and the assembly remained 0.1.0.0 regardless of the requested zip name.
LoadedPath only recorded a preload request, and version diagnostics could load
the plugin. Documentation incorrectly told users to delete the installer’s
intentional Plugins/x86_64 copy.

RC packages now enforce numeric version consistency and embed the RC/revision/
source state; support reports include the managed file hash and observed resident
native modules, their versions and disk hashes, without loading a module for
inspection. Multiple mapped copies are reported as ambiguous. Compare against
the candidate manifest; a plugin path or a lower native version alone is not stale.

### KI-8 — A failed deferred save discarded the retry; shutdown saved before release

**Major lifecycle defect; fixed in 0.2.3, game persistence check pending.** The
dirty flag was cleared before Main.SaveSettings, which swallowed write errors.
Shutdown also wrote settings before zeroing the wheel. The save API now returns
success, dirty state survives failures and output release happens before disk IO.
Executable regression checks cover failures, retry timing and ordering.

The installed UMM implementation also catches errors internally in ModSettings.Save.
The RC therefore uses the same XML serialization format through an exception-reporting
writer, then atomically replaces Settings.xml. A real locked-file regression confirms
failure leaves the previous XML intact, keeps the retry pending, and recovers after
the lock is released. Actual Settings fields roundtrip through the serializer.

### KI-9 — Toolkit sync can leave a mixed pin on failure

**Tooling; fixed upstream and adopted, 2026-09-07.** Sync preflights all requested
parts/files/local edits, stages a replacement and rolls back a failed commit.
Unknown/missing/empty parts fail before destination changes. Partial syncs retain
other manifest entries and nested paths; forced edits retain backups. Upstream
real-filesystem regression covers copying failure and rollback. Local pins name
the full source revision and dirty state. Hashes prove integrity, not independent
release provenance.

### KI-10: Toolkit adoption lifecycle defects

**Major; fixed upstream/consumer, attended verification pending.** Shared wrapper
could bind the wrong native filename on Mono; selectors could remain stale; a
shared read slot could follow a different FFB wheel; Panic left managed readiness
true; final input-only shutdown could retain DirectInput. Toolkit 0.12 addresses
these with exact-handle bindings, strict GUID selection, slot identity, corrected
readiness and final ShutdownAll. Consumer readers refresh on FFB switches and all
inputs release before persistence. The FFB checkbox immediately zeroes output when
disabled and initializes FFB when enabled after a disabled launch. Nonfinite force
is zeroed before conversion to a device integer. Malformed saved bindings are rejected before indexing axis/button arrays.
No new force tune is introduced.


---

### KI-11 — Telemetry could stay disabled after correcting a failed destination

**Supportability; fixed in 0.2.3, live consumer check pending.**
`_senderFailed` was checked before endpoint changes and was not reset by shutdown.
After one connection/send failure, editing host/port or toggling telemetry could
leave it disabled for the rest of the session. Failed attempts now remember their
endpoint, stay quiet until it changes or is explicitly restarted, and dispose the
failed socket. Production-code loopback tests cover recovery, three parked packets,
destination switching and repeat shutdown. SimHub remains an attended gate.

## Resolved

Kept because each one cost real time to find, and because a regression in any of
them would otherwise look like a new mystery.

| # | Problem | Cause | Fixed in |
|---|---|---|---|
| R-1 | Vendored toolkit was never committed; a fresh clone could not package | `.gitignore` excluded the `lib/` **directory**, so git never descended into it and the `!lib/toolkit/**` re-includes could not match | 0.2.3 |
| R-2 | Force feedback gave up when the preferred device had no actuator | A Fanatec base presents two `FANATEC Wheel` devices, only one with the motor; `CreateEffect` failed `0x80040154` and nothing tried the other | 0.2.2 |
| R-3 | Crash when choosing a shifter after force feedback failed | Listing controllers created a temporary DirectInput instance and released it while the device table stayed populated; opening the chosen device then used the released instance | 0.2.2 |
| R-4 | Direct wheel input steered inverted | Assignment took the moved direction as +1, and the game reads +1 as right, so a left turn during Assign inverted the axis | 0.2.2 |
| R-5 | Force feedback far too strong at Strength 50 | `FyReference` too low for a direct-drive base | 0.2.1, again in 0.2.2 |
| R-6 | The wheel went dead after ~45 s, or after alt-tab | `0x80040205` is `DIERR_NOTEXCLUSIVEACQUIRED` — focus loss returned the device non-exclusively and nothing re-acquired it. Misread twice as `INCOMPLETEEFFECT` and `EFFECTPLAYING` | 0.2.1 |
| R-7 | "No centre" — the wheel pulled toward lock on both sides | Force was `Mz` from `CalcAligningForce`, a Pacejka aligning-torque curve that reverses past ~8° slip; this game's front tyres run 12–29° in ordinary corners. Replaced by lateral force × pneumatic trail | 0.2.1 |
| R-8 | Right worked, left inverted (MOZA R5); then no centre (R12) | The sign was applied in both the direction vector and the magnitude. Only the R5 honours the direction vector. Settled: direction fixed at +X, signed `lMagnitude` carries the sign | 0.1.1, settled 0.2.1 |
| R-9 | 27° dead band at centre on a 270° wheel | Rewired applies a hidden 10% deadzone to every axis of a controller its database does not recognise, inside `GetAxisRaw`, where the game's own deadzone setting cannot reach it | 0.1.0 |
| R-10 | Steering felt slow and vague | The game's direct-steering mode only activates for wheels Rewired recognises; everyone else got the gamepad smoothing filter, ~1.6 s lock-to-lock | 0.1.0 |
| R-11 | Force feedback never ran at all | `UnityForceFeedback.dll` absent from the shipped game, `CarDynamics.forceFeedback` never assigned, `ForceFeedback` never attached, `enableForceFeedback` never set — built from both ends, never joined in the middle | 0.1.0 |

Detail on R-6 through R-11 is in [FORCE-FEEDBACK.md](FORCE-FEEDBACK.md) and
[CONTROLS.md](CONTROLS.md); each has a dated section.

---

## Upstream, recorded here

This is a historical record of the shared-model investigation. The current
candidate consumes the versioned `AxleForceCurve@1` compatibility pipeline from
toolkit v0.12.0; the generic ForceModel/SimLite pipeline remains unused.

### U-1 — `simlite@1` was not art of rally's tuning; `simlite@2` is

dbce-wheel-mod-toolkit's `simlite@1` profile was described as this mod's 0.2.2
tuning. Feeding it `docs/force-curve-vector.csv` (2026-09-04) showed that only
its **model** half was: its **shaper** half was the toolkit's own `ForceShaper`
defaults, which this game does not use. Our curve has no deadzone, no output
deadband, no soft saturation, no slew limit and no ramp — 500 N at 5 km/h
produces 0.005487 undiminished and sends 54 to the wheel, not 0.

Fixed upstream as `simlite@2`. `simlite@1` was annotated rather than edited, so a
published version stays immutable.

**Consequence for us:** if the toolkit's `ForceModel` is ever adopted here
([ROADMAP.md](ROADMAP.md) step 3), take **`ForceProfile.SimLite()`** — from
toolkit 0.7.1, an in-code literal returning the model and its conditioning
together. Not `ForceModelSettings.SimLite()`, which is half a tune; it now
derives from the profile and its doc comment says so, but pairing it with a
default `ForceShaper` reproduces exactly this defect.

Fixed structurally in 0.7.1, which matters more than the value fix: every value
in a preset is now **stated rather than inherited**, so a changed default cannot
quietly retune one, and a drift test pins each preset to the section it names in
the profiles file key by key. `arcade` had the same defect from the other side —
its conditioning wants soft saturation 1.0 and a slew limit of 3.6, neither of
them `ForceShaper` defaults — so the bug was in the pattern, not in one preset.

Read the shaper values before assuming what adoption costs: `simlite@2` sets
`Deadzone` 0, `SoftSaturation` 0, `SlewPerSecond` 0, `OutputDeadband` 0,
`RampSeconds` 0 and `AttackSmoothing` = `DecaySmoothing` = 0.2. It is
**equal on the unsmoothed grid**, not dynamically equivalent. The 2026-09-06
consumer regression against the pinned DLL demonstrates a clamp/EMA difference
even at constant speed: 0.80 then 0.16 here versus 1.00 then 0.32 upstream for
23,000 N then zero at 40 km/h. See ROADMAP.md before adoption.

### U-2 — Clamp order: resolved, the toolkit adopted ours

We fade then clamp; `ForceModel.Compute` used to clamp then fade. Four of 640
vector rows differed, all at 5–7.5 km/h, worst 1,086/10,000 at the wheel. Detail
and the table are in
[FORCE-FEEDBACK.md](FORCE-FEEDBACK.md#clamp-order-fade-first-clamp-last-measured-2026-09-04).

**Aligned to our order in toolkit v0.7.0** (2026-09-04), on the reasoning that a
device limit applied before a model term stops being a boundary constraint and
becomes a silent soft knee. At that time nothing in production moved: no consumer referenced
`Dbce.Wheel.Ffb`, and OutRun — the only user of the shared model, through the C++
port — defaults to its own legacy model and its profiles have no fade.

Two things came out of building it that are worth knowing here:

- **The bigger casualty was soft saturation, not the fade.** `ForceShaper`'s
  `tanh` was receiving an already-clamped value, so everything from full scale
  upward arrived as exactly 1.0 and there was nothing left to compress — a hard
  clip where a profile had asked for a soft knee. A model output of 1.5 reached
  the wheel as 7,615/10,000, identical to what 1.0 produced; it now reaches
  9,051. **This does not reach us**, at step 3 or otherwise: `simlite@2` sets
  `SoftSaturation` to 0, so the stage is inert for our tune. It matters to
  `arcade-outrun@1`, which asks for 1.0 precisely so a crash outweighs a hard
  corner. A note here previously said adopting the shaper would bring us a soft
  knee to judge at the wheel; it would not.
- **The conformance sequence could not see the change.** Applying it produced a
  zero-line golden diff, because the sequence never drove the model past full
  scale. A record that cannot see the class of change it is recording is worse
  than no record. Ours has the same shape of risk: `force-curve-vector.csv`
  covers saturation because the grid deliberately includes 14,000 N, but any
  future addition to `ForceCurve` needs its own straddling rows or the CSV will
  keep passing while proving nothing.

  It happened a third time in toolkit 0.8.0: every shipped profile used strength
  50, where gain is exactly 1.0 and a gain *reordering* is unobservable, so a
  chain reshuffle that fixed two severe defects would have produced another
  zero-line diff. The general rule, better than the way it was first written
  down here: **a conformance set must contain a case where every parameter that
  can change behaviour is off its default.** Strength 50 and smoothing 0 are
  defaults hiding in plain sight — and our own vector uses gain 1.0 throughout,
  for exactly the reason it nearly missed saturation.

### U-3 — The low-speed fade scales the force, it does not cap it

Inherent to fade-then-clamp, not to anyone's implementation, and true of this
mod since 0.2.1. The fade multiplies; a large enough force simply overwhelms it
and reaches full output at walking pace, where a clamp applied earlier would have
limited it by accident.

Front lateral force (both wheels, trail 1.0) needed to reach **full** output:

| km/h | fade | Strength 15 | Strength 26 | Strength 50 | Strength 100 |
|---:|---:|---:|---:|---:|---:|
| 5 | 0.126 | 303,750 N | 175,240 N | 91,125 N | 45,563 N |
| 7.5 | 0.500 | 76,667 N | 44,231 N | 23,000 N | 11,500 N |
| 10 | 0.874 | 43,870 N | 25,309 N | 13,161 N | 6,580 N |

Against a **measured hard-cornering peak of about 8,800 N** total. Inside the
fade band proper (3–8 km/h) the threshold is 5× to 35× anything the game has been
seen to produce, so at the strengths people actually run — 15 and 26 in the two
reports we have — this is unreachable. It is not a present defect.

It becomes live in one specific future: **impact and kerb effects**
([ROADMAP.md](ROADMAP.md)). An impact spike is exactly the kind of transient that
can be several times cornering `Fy`, and it happens at any speed. If those are
added, the fade must not be relied on as the thing that keeps a low-speed
collision from slamming the wheel — that needs its own limit.


## Will not fix

### WNF-1 — Switching Rewired's input backend at runtime

**Four attempts over two days, 2026-09-01 to 2026-09-03. Do not try a fifth.**

`ReInput.configuration.windowsStandalonePrimaryInputSource` has a runtime
setter that calls Rewired's `ResetAll()`. Applied at mod load it killed the
keyboard with no in-game way back. Applied after the title screen it killed the
**menus** while every probe said input was flowing — 67 keypresses seen by
Unity, by Rewired's keyboard controller and by the player's actions
(`UISubmit`, `UICancel`, `UIHorizontal` all firing), keyboard maps enabled and
identical, the UI input module alive with player 0.

The game logged 48,216 "object created by a previous session … no longer valid"
errors from Rewired objects cached by `ControllerButtonDisplay` and `Arcader`;
refreshing all 84 references brought that to zero and the menus stayed dead.
The cause was never found.

Devices Rewired cannot read are handled by `WheelInput`, which bypasses it
entirely and works. The switch survives only as `UseDirectInputBackend` in
Settings.xml, deliberately absent from the panel. Full account in
[CONTROLS.md](CONTROLS.md).

### WNF-2 — Cockpit view

art of rally's cars have no modelled interiors — no dashboard, wheel, pillars
or wipers — and the world is authored for a distant isometric camera, so at eye
level you get LOD pop-in and shadow cascades tuned for tens of metres away. A
cockpit view is not a camera change, it is an art project. Bonnet and bumper
mounts deliver what matters for driving feel. See [CAMERA.md](CAMERA.md).

### WNF-3 — Routing the wheel through xoutput / XInput

It would break force feedback rather than help it: axis resolution drops to
gamepad precision, separate pedal axes collapse into triggers, and **XInput has
no force feedback beyond rumble**. The entire FFB path here is DirectInput, and
a virtual pad hides the real device from exactly the API it needs. A virtual
controller left running can also steal the force-feedback slot from the real
wheel — see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

### WNF-4 — Physics, grip or assist changes

art of rally has online leaderboards. Force feedback, cameras and telemetry are
fair-play neutral; grip, assists and car behaviour are not. Keeping that line
bright is what lets the mod be shared without argument. `DisableSteerAssist`
does change driving aids, which is why it is off by default and labelled.
