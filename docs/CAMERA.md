# Camera

## The accepted concession

**This is a bonnet camera, not a cockpit camera.** That is a decision, not a
gap to be closed later.

art of rally's cars have no modelled interiors — no dashboard, no wheel, no
pillars, no wipers. The world is authored to be seen from a distant isometric
camera, so at eye level you get LOD pop-in, low-detail near geometry, and
shadow cascades tuned for a viewpoint tens of metres away.

A cockpit view is therefore not a camera change, it is an art project: modelling
interiors for every car in the game. Out of scope.

What a bonnet or bumper mount *does* deliver is the thing that actually matters
for driving feel — the horizon moving with the suspension, the car rotating
about you, and a stable forward reference that lets you place the car by feel
instead of by reading a top-down sprite.

## What the game gives us

From [FINDINGS.md](FINDINGS.md), `CarCameras` is an ordinary follow camera:

```
target  distance  height  yawAngle  initialPitchAngle  currentPitchAngle
MinFOV  MaxFOV  CurrentFOV  smoothTimeFOV  smoothTimeTilt
rotationDamping  heightDamping  yawResetSpeed  dampFixedCamera
CurrentCameraAngle  CameraAnglesList  cardynamics  myTransform  mtarget
```

A bonnet view is `distance` ≈ 0, `height` at bonnet level, pitch level, damping
near zero so the camera is rigid to the car rather than lagging it, and a
narrower FOV than the isometric default.

`CameraAngles` is `CAMERA1`..`CAMERA8`, and `Cinemachine.dll` ships with a
`cinemachineBrain` on `CameraManager`, so there is room to add a mode rather
than hijack an existing one.

There is **no** latent first-person mode to switch on — see the `FirstPerson`
dead end recorded in FINDINGS.md.

## Prior art

The Nexus "Camera Mod" already adds extra perspectives and a small camera
editor via Unity Mod Manager. The approach is proven; the question is only
whether to build on it or implement independently. Building independently keeps
this repo's dependency surface to UMM + Harmony alone, and lets the camera share
the mod's settings UI with force feedback and telemetry.

## Design notes

- **Rigid, not damped.** The default camera's `rotationDamping` and
  `heightDamping` exist to smooth an isometric chase view. On a bonnet mount
  they read as the car sliding under a floating camera. Damping should go to
  near zero, with any smoothing reintroduced deliberately.
- **Suspension coupling is the whole point.** The camera must inherit body roll
  and pitch. `CarDynamics` and `Wheel.suspensionTravel` are right there.
- **Head movement under load** — a small lateral offset from lateral G — is what
  sells a bonnet cam. Keep it subtle and make it configurable to zero; it is
  also the first thing to make people motion-sick.
- **FOV with speed.** `MinFOV`/`MaxFOV`/`smoothTimeFOV` already exist and are
  already speed-driven for the chase camera; reuse rather than reimplement.
- **Expect to hide the car's own geometry** if the mount clips through it, and
  expect near-plane tuning.
- **Co-driver calls become essential.** With no top-down view of the road ahead,
  pace notes stop being flavour and start being the interface. Worth checking
  early whether the game's existing calls have enough lead time for a driver who
  can no longer see round the corner.

## Status

Not yet implemented. Force feedback and telemetry come first: they are the
features with no existing community solution, whereas a working camera mod
already exists on Nexus for anyone who needs one today.

## Open issue: the camera still moves oddly at the end of a stage

**Status:** open as of 2026-09-01, reproduced on every stage finish.
**Severity:** cosmetic. It happens during the results cinematic, never while
driving, and nothing else is affected.

### What happens

The bonnet camera is in use, the stage ends, the game takes the camera over for
the results sequence — and it swings around for roughly a second before
settling.

### What is already fixed, and why it was not enough

The first cause is understood and dealt with. `CarCameras` damps toward its
wanted position from wherever the camera currently sits. Releasing the camera
while it was mounted inside the car meant the stock rig interpolated out through
the bodywork to the chase position — a long, obviously wrong sweep.

`BonnetCamera.Mount` now calls `SetToWantedPositionImmediate()` on the frame it
stops driving, which places the camera in one step instead of easing. That
removed the long sweep. What remains is shorter and different in character, so
it is a second cause, not the first one incompletely fixed.

### Ranked hypotheses to test

Nothing below is confirmed — none has been instrumented yet.

1. **The handback fires more than once, or at the wrong moment.** `shouldDrive`
   is `IsActive() && GameState.IsPlayerView`. If `IsPlayerView` flickers as the
   cinematic starts, the mod would repeatedly hand back and re-mount. *Test:*
   log every `_wasDriving` transition with the frame number. This is first
   because it is the cheapest to confirm and would explain the residue exactly.

2. **The cinematic uses a different camera or rig** than the one being patched,
   so `SetToWantedPositionImmediate()` tidies an object that is no longer the
   one on screen. *Test:* log the instance id of the active camera across the
   transition.

3. **Write ordering.** The mod mounts from a Harmony postfix; if the game's own
   camera update runs later in the frame during the cinematic, the last mounted
   transform could still be read as a starting point. *Test:* compare camera
   position at the end of our postfix against the start of the next frame.

4. **Rotation is not covered by the same call.** `SetToWantedPositionImmediate`
   may settle position while rotation continues to damp from the car's
   orientation. *Test:* log position and rotation separately across the
   transition.

### Suggested approach

Add a temporary verbose camera trace behind the existing diagnostic-logging
toggle, capturing per frame across the finish line: `IsActive`, `IsPlayerView`,
`_wasDriving`, active camera instance id, camera position and rotation. One
stage finish with that trace should separate the four hypotheses in a single
run, rather than guessing at fixes.

Note that the failure is only reachable by driving a stage to completion, so
this cannot be checked from the menu.

## Bumper view

Added 2026-09-02 as a second entry in the same rotation, after the bonnet
view. It is the same mechanism — a `CameraAngle` appended in `CarCameras.Start`,
taken over in the `LateUpdate` postfix — with its own offsets (`BumperHeight`,
`BumperForward`, `BumperSide`, `BumperPitch`, `BumperFOV`), defaulting to just
above the front bumper looking down the road. `BonnetLean` is shared. The
numpad tuner adjusts whichever mounted view is on screen and the reset key
resets that view alone. Either view can be disabled independently; changes to
the rotation take effect on the next stage, when `CarCameras.Start` runs again.
