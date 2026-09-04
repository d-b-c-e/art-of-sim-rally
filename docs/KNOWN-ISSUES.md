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
| **Reported** | 2026-09-01, reproduced on every stage finish |
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
