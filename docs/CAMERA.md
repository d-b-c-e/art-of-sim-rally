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

**Shipped.** The bonnet view landed in 0.2.0 and the bumper view beside it the
same day; both are in the game's own view rotation with a numpad tuner, and both
are verified on the owner's rig.

Implementation is [`BonnetCamera.cs`](../src/ArtOfSimRally.Mod/BonnetCamera.cs)
and [`CameraTuner.cs`](../src/ArtOfSimRally.Mod/CameraTuner.cs). The mechanism is
smaller than the design notes above imply: `CarCameras.SetCameraFromSave` wraps
on `CameraAnglesList.Count` rather than a hard-coded 8, so appending entries in a
`Start` postfix is enough to join the rotation — no patching of the cycling logic
at all. A `LateUpdate` postfix then takes the camera over completely while one of
ours is active, because the stock rig always places the camera at a distance and
calls `LookAt` on the car, and a zero distance would have it looking at itself.

Of the design notes, two were followed exactly and one was not:

- **Rigid, not damped** — followed. No position or rotation damping at all.
- **Head movement under load** — followed, as `BonnetLean`, smoothed and
  configurable to zero.
- **FOV with speed** — *not* done. `UpdateFOVAndPitch` rewrites `fieldOfView`
  every frame for the chase camera, so the mounted views set a fixed FOV after
  it rather than reusing the speed curve. Nobody has asked for the curve.

### Open issues and RC handback

KI-1 and KI-2 are tracked in [KNOWN-ISSUES.md](KNOWN-ISSUES.md). The issue #1
reporter says removing a PS5 controller resolved their symptom; the stale child
transform is a separately established code defect, not a confirmed diagnosis of
that report.

The RC restores the owned camera child's local identity and prior FOV. Stock-view
handback may snap the parent; cinematic handback does not. CameraManager disables
CarCameras when enabling Cinemachine, so explicit transition prefixes and a
persistent watchdog cover paths where the LateUpdate postfix never runs.
Disabling an active mounted view returns to a usable stock angle. New rotation
entries still require a stage start if they were disabled when the stage loaded.

Offline ownership/callback tests pass. The rendered stock views, replay, finish,
restart and mod-disable transitions remain in the attended RC checklist.

## Bumper view

Added 2026-09-02 as a second entry in the same rotation, after the bonnet
view. It is the same mechanism — a `CameraAngle` appended in `CarCameras.Start`,
taken over in the `LateUpdate` postfix — with its own offsets (`BumperHeight`,
`BumperForward`, `BumperSide`, `BumperPitch`, `BumperFOV`), defaulting to just
above the front bumper looking down the road. `BonnetLean` is shared. The
numpad tuner adjusts whichever mounted view is on screen and the reset key
resets that view alone. Either view can be disabled independently; changes to
the rotation take effect on the next stage, when `CarCameras.Start` runs again.
