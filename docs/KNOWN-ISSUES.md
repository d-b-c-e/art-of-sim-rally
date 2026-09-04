# Known issues

The register of what is broken, what is unverified, and what was deliberately
abandoned. One entry per problem, newest first within each section.

**Reporting something new:** open an [issue](../../issues) with a support file
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

### KI-1 — Stock cameras 3–8 render reversed

| | |
|---|---|
| **Reported** | 2026-09-04, [issue #1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1) |
| **Reporter** | Thrustmaster T300 RS GT, mod 0.2.1 |
| **Severity** | major — it makes the game's own views unusable, and they are the views most players prefer |
| **Status** | root cause confirmed and **fixed** 2026-09-04, unreleased; awaiting on-screen confirmation |

The mod's own bonnet and bumper views are correct. The game's stock camera
angles, cycled past them, come out reversed. The reporter supplied a
[video](https://www.youtube.com/watch?v=uBk5wtIIAj0).

#### Cause: the mod moves the camera, the stock rig moves the camera's parent

`CameraManager`'s constructor names the whole hierarchy:

```csharp
stageCamera        = GameObject.Find("Stage Camera").GetComponent<CarCameras>();
CameraMainTransform = GameObject.Find("Stage Camera/Camera Main").transform;
CameraMainTransform.localPosition = Vector3.zero;
CameraMainTransform.localRotation = Quaternion.identity;
```

So `CarCameras` sits on **"Stage Camera"**, and the camera that actually renders
is **"Camera Main", a child of it**. `CarCameras` drives only the parent —
`myTransform = base.transform`, positioned and `LookAt`-ed every frame — and the
game keeps the child pinned at local identity. That reset in the constructor is
the game declaring the invariant out loud.

`BonnetCamera.DriveCamera.Mount` writes **world-space** position and rotation to
`UIManager.Instance.PanelManager.mainCamera.transform`, which is `Camera.main`,
which is the **child**. Unity stores a world-space write on a child as a local
offset from its parent. So while a mounted view is active the child accumulates
a large local transform — roughly "at the car, facing forward" expressed
relative to a parent sitting 30–46 m behind and 15–45 m above the car and aimed
back down at it, which is close to a 180° yaw.

Nothing ever puts it back. `grep` for `localPosition` / `localRotation` across
`src/` returns nothing: the mod has never touched the child's local transform.
When the player cycles to a stock angle, `SetToWantedPositionImmediate()`
correctly places and aims the **parent** — and the child renders from its stale
offset, looking the wrong way. Every stock camera is affected, permanently,
until the next `CameraManager` construction resets the child.

**Replays are the same bug.** A second user on 0.2.2 reports "the camera is
still broken in replays and at stage end". `GameState.IsPlayerView` returns
false for `REPLAY` exactly as it does for the end-of-stage cinematic, so both go
through the same handback and inherit the same stale child transform. The fix
covers all three paths; confirm replays in the same test run.

#### Why nobody here saw it

The `CameraAnglesList` append and the `CAMERA1` tag reuse — the first two
suspects — are innocent. The bug needs you to cycle *out of* a mounted view back
into a stock one, and the developer's rig has been driving the bonnet view since
the day it shipped. The reporter cycles through all of them, so they see it.

It also explains **[KI-2](#ki-2--the-camera-moves-oddly-at-the-end-of-a-stage)**:
the end-of-stage handback has exactly the same hole, which is why
`SetToWantedPositionImmediate()` fixed the long sweep (parent) but left a
shorter, differently-shaped residue (child).

#### The fix (applied, unreleased)

Restore the invariant the game itself asserts, on the frame the mod stops
driving — `BonnetCamera.DriveCamera.Mount`, in the `if (!shouldDrive)` branch,
before the existing `SetToWantedPositionImmediate()` so the parent's placement is
the last word:

```csharp
released.transform.localPosition = Vector3.zero;
released.transform.localRotation = Quaternion.identity;
```

The handback used to return *before* resolving the camera, so the branch now
looks it up itself. The driving path needs no equivalent: it writes world-space
position and rotation every frame, which fully determines the child's local
transform regardless of what it held before.

#### Still to do

**Confirm on screen.** It was not reproduced in this session: the game no longer
accepts injected keyboard input (neither virtual-key nor scan-code `SendInput`,
nor `PostMessage` to the window), so the title screen could not be driven
unattended — see the machine note in CLAUDE.md. The confirmation is one stage:
cycle bonnet → bumper → a stock angle, and check the stock angle looks forward.
Check a stage finish in the same run for
[KI-2](#ki-2--the-camera-moves-oddly-at-the-end-of-a-stage).

Until that is done this is a fix by reasoning, not a verified one — do not
release it as confirmed, and do not close the issue on it.

**Workaround for anyone on 0.2.2:** untick both mounted cameras in Ctrl+F10 →
Camera and restart the stage; the rotation is then unmodified. Restarting the
stage also clears the stale offset on its own.

---

### KI-2 — The camera moves oddly at the end of a stage

| | |
|---|---|
| **Reported** | 2026-09-01, reproduced on every stage finish; independently reported by a second user on 0.2.2, 2026-09-04 |
| **Severity** | cosmetic — results cinematic only, never while driving |
| **Status** | open, partially fixed |

A mounted view is in use, the stage ends, the game takes the camera over for the
results sequence, and it swings for roughly a second before settling.

**What is already fixed, and why it was not enough.** `CarCameras` damps toward
its wanted position from wherever the camera currently sits. Releasing the
camera while it was mounted inside the car made the stock rig interpolate out
through the bodywork to the chase position — a long, obviously wrong sweep.
`BonnetCamera.Mount` now calls `SetToWantedPositionImmediate()` on the frame it
stops driving, placing the camera in one step. That removed the long sweep.
What remains is shorter and different in character, so it is a second cause.

**Most likely the same cause as [KI-1](#ki-1--stock-cameras-38-render-reversed):**
the handback fixed the parent ("Stage Camera") and left the child
("Camera Main") holding the local offset the mounted view put on it. That fits
the evidence exactly — the long sweep was the parent damping out and went away
when `SetToWantedPositionImmediate()` was added; what remains is shorter and
different in character because it is the child, which nothing corrected.

**KI-1's fix should close this too**, since it clears the child on exactly the
same code path. Re-test a stage finish before instrumenting anything; only if
the residue survives is the list below worth working through.

**Older hypotheses, kept in case the fix does not settle it:**

1. **The handback fires more than once, or at the wrong moment.** `shouldDrive`
   is `ActiveView() != None && GameState.IsPlayerView`. If `IsPlayerView`
   flickers as the cinematic starts, the mod would repeatedly hand back and
   re-mount. Cheapest to confirm and would explain the residue exactly.
   *Test:* log every `_wasDriving` transition with the frame number.
2. **The cinematic uses a different camera or rig**, so
   `SetToWantedPositionImmediate()` tidies an object that is no longer on
   screen. *Test:* log the active camera's instance id across the transition.
3. **Write ordering.** The mod mounts from a Harmony postfix; if the game's own
   camera update runs later in the frame during the cinematic, the last mounted
   transform could still be read as a starting point. *Test:* compare camera
   position at the end of the postfix against the start of the next frame.
4. **Rotation is not covered by the same call.** `SetToWantedPositionImmediate`
   may settle position while rotation keeps damping from the car's orientation.
   *Test:* log position and rotation separately across the transition.

One stage finish with a verbose camera trace behind `DiagnosticLogging` —
`ActiveView`, `IsPlayerView`, `_wasDriving`, active camera instance id, position
and rotation, per frame across the finish line — separates all four in a single
run. The failure is only reachable by driving a stage to completion, so it
cannot be checked from the menu.

Given KI-1 is in the same `LateUpdate` postfix, investigate them together.

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

Toolkit 0.2.0 adds `SetPreferredDeviceGuid`, which would let the right twin be
selected by DirectInput instance GUID instead of by trying candidates in turn.
See [ROADMAP.md](ROADMAP.md).

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
| MOZA R12 (owner) | — | **26** | 0.2 | `FyReference` retuned 6,000 → 8,000 → 11,500 N to move the usable setting toward the slider midpoint; it was 20 at 8,000 N, and the live setting is still 26, not the 50 default |
| Unstated wheel + motion platform (Reddit, 2026-09-04) | 100% | **15** | **0.50** | "works perfect"; raised smoothing specifically to kill notchiness over low-poly inclines |

Both users ended up well below Strength 50 — the second at 15 with the base at
full gain. That is two of two, and it suggests the default is still hot for
anyone who does not turn their base down. Worth watching before retuning a
third time on one rig's opinion.

---

### KI-5 — Stage start stutters and briefly locks up for 10–15 seconds

| | |
|---|---|
| **Reported** | 2026-09-04, Reddit, second user (motion platform, wheel model not stated) |
| **Severity** | major — it happens exactly when you are trying to drive |
| **Status** | open, **regression in 0.2.2**; the leading cause is fixed in the working tree, unreleased and unconfirmed |

> "when starting a new stage for the first time there's massive stutter and
> brief lockups when attempting to drive for the first 10-15 seconds almost
> like shader caching or something. This only just started with this patch."

Not yet known whether it recurs on every stage change or only once per session;
the reporter had not tested that. Establish that first — it separates
"initialisation happens once" from "something is retrying".

**Leading cause, now fixed: `Settings.xml` was written from the per-frame input
path.** `WheelInput.Update` self-calibrates each axis by extending its `Far` end
whenever a raw value exceeds the recorded range, and on extension it called
`Main.SaveSettings()` — a synchronous XML serialise and disk write, rate-limited
to once every five seconds. The first hard corner and the first full pedal
presses are exactly when the range keeps extending, so saves fired at roughly
t+0, t+5 and t+10 and then stopped once the range was learned. That shape
matches the report closely, it is new in 0.2.2 (`WheelInput` is), and it would
be worse on a slow disk or with a virus scanner watching the `Mods` folder.

The settings *object* is still updated on the frame the range extends — that is
a few string assignments, and it keeps the panel showing the live range. Only
the disk write is deferred, to `WheelInput.FlushLearnedRanges()`, which the
watchdog calls when the player stops driving and again on shutdown. The cost is
that a crash mid-stage loses a range learned moments earlier, and the next stage
re-learns it in the same few seconds it would have taken anyway.

Writing a file from a hot path was wrong regardless of whether it turns out to
be the whole story, which is why it was fixed before being confirmed. **If the
stutter survives, this entry is not closed** — go to the alternative below.

**Alternative: the device-open retry loop.** If `Open()` fails, `WheelInput`
retries every five seconds, and each attempt is a full DirectInput enumeration
plus an open per device on the main thread. That would stutter on the same
cadence — but it would not stop after 15 seconds, so it fits less well.

**The two are trivial to tell apart** in the UMM log
(`artofrally_Data\Managed\UnityModManager\Log.txt`): `Open()` logs
`Wheel input: opened N controller(s)` every attempt. Repeated lines during the
stutter means the retry loop; a single line at load with the stutter happening
anyway pointed at the save path.

A third possibility neither of those covers: the stutter is the game's own and
was always there, and 0.2.2 only changed the timing enough to expose it. The
reporter's own "almost like shader caching" reading. Test with the mod disabled
from the UMM panel before spending anything on it.

Ruled out already: `InputBackend.Tick` runs every frame but returns immediately
unless the abandoned backend experiment is active; the force-feedback
`FixedUpdate` prefix and postfix are arithmetic only; the `GetInput` postfix
reads cached values.

**Left alone deliberately:** `CameraTuner` also writes settings from
`LateUpdate`, one second after the last numpad adjustment. It is the same class
of thing, but it fires only when the player is deliberately holding a tuning key
rather than on every stage start, and deferring it to the end of the stage would
lose the adjustment to a crash for no real gain. Revisit only if someone reports
a hitch while nudging the camera.

---

### KI-6 — The wheel snaps back as the car straightens out of a slide

| | |
|---|---|
| **Reported** | 2026-09-04, Reddit, second user |
| **Severity** | major for feel on powerful RWD cars; not a defect so much as a missing effect |
| **Status** | open |

> "the steering tends to snap back suddenly as the car is straightening up and
> get stuck in a tank slapper … you have to be so incredibly gentle on the
> throttle if you're sliding"

Part of this is the car — art of rally's high-power RWD cars genuinely dislike
big slip angles, and that is physics the mod must not touch (see
[WNF-4](#wnf-4--physics-grip-or-assist-changes)). But the snap itself is ours:
the output is a **pure spring-like force with no damping term**. Force is
`(FyL + FyR) × trail / FyReference`, low-passed by `Smoothing`, and nothing
opposes the *rate* at which the wheel moves. When a slide gathers up, `Fy`
collapses quickly, the centring force drops with it, and there is nothing to
absorb the wheel's own inertia — so it overshoots, and on a direct-drive base
with no friction to speak of it oscillates.

`Smoothing` is the only thing resisting it today, and it is the wrong tool: it
is a low-pass on the force signal, so raising it delays every cue including the
ones you want. The reporter raised it to 0.50 (default 0.2) and reports it
helping with a different symptom — notchiness over low-poly inclines — which is
consistent with it being a blunt instrument.

The right fix is a **damper effect**, which DirectInput supports natively and
the wheelbase renders itself. Toolkit 0.4.0 exports one
(`CreateConditionEffect(1)` for a damper, then `UpdateConditionEffect`), so the
API exists as of the current pin.

It will not be the whole answer on its own. A hardware damper is computed by the
base from axis velocity, continuously and between our updates — which is why it
is smoother than anything synthesised at 60 Hz — but it knows nothing about road
speed or grip, so it cannot be scaled with the car's state. Expect to want a
small constant hardware damper for stability *plus* a speed-scaled term in our
own force model for feel. And expect `CreateConditionEffect` to return −1 on
devices whose drivers expose no condition effects, which is a normal answer to
fall back from, not a failure. Plan in [ROADMAP.md](ROADMAP.md).

---

## Resolved

Kept because each one cost real time to find, and because a regression in any of
them would otherwise look like a new mystery.

| # | Problem | Cause | Fixed in |
|---|---|---|---|
| R-1 | Vendored toolkit was never committed; a fresh clone could not package | `.gitignore` excluded the `lib/` **directory**, so git never descended into it and the `!lib/toolkit/**` re-includes could not match | unreleased (2026-09-04) |
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
([ROADMAP.md](ROADMAP.md) step 3), take **`simlite@2`'s shaper values**, not
`ForceModelSettings.SimLite()` — that helper still returns the old `ForceShaper`
defaults, so adopting it in code would silently import a deadzone and a soft
knee this game has never had. This corrects the guidance written here before the
vector existed.

### U-2 — Clamp order: resolved, the toolkit adopted ours

We fade then clamp; `ForceModel.Compute` used to clamp then fade. Four of 640
vector rows differed, all at 5–7.5 km/h, worst 1,086/10,000 at the wheel. Detail
and the table are in
[FORCE-FEEDBACK.md](FORCE-FEEDBACK.md#clamp-order-fade-first-clamp-last-measured-2026-09-04).

**Aligned to our order in toolkit v0.7.0** (2026-09-04), on the reasoning that a
device limit applied before a model term stops being a boundary constraint and
becomes a silent soft knee. Nothing in production moved: no consumer references
`Dbce.Wheel.Ffb`, and OutRun — the only user of the shared model, through the C++
port — defaults to its own legacy model and its profiles have no fade.

Two things came out of building it that are worth knowing here:

- **The bigger casualty was soft saturation, not the fade.** `ForceShaper`'s
  `tanh` was receiving an already-clamped value, so everything from full scale
  upward arrived as exactly 1.0 and there was nothing left to compress — a hard
  clip where a profile had asked for a soft knee. A model output of 1.5 reached
  the wheel as 7,615/10,000, identical to what 1.0 produced; it now reaches
  9,051. That matters at [step 3](ROADMAP.md#de-duplicating-against-the-toolkit)
  only if we adopt `ForceShaper` — this mod has no soft saturation at all today.
- **The conformance sequence could not see the change.** Applying it produced a
  zero-line golden diff, because the sequence never drove the model past full
  scale. A record that cannot see the class of change it is recording is worse
  than no record. Ours has the same shape of risk: `force-curve-vector.csv`
  covers saturation because the grid deliberately includes 14,000 N, but any
  future addition to `ForceCurve` needs its own straddling rows or the CSV will
  keep passing while proving nothing.

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
