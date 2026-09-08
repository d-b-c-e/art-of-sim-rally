# Findings — art of rally, verified on disk

The original survey below was read from shipped game files on 2026-08-31.
Later verified additions are explicitly dated; the install table is historical.
Nothing in this document is inferred from forum posts. Where something is
*suspected* rather than proven, it says so explicitly.

Re-deriving these costs an hour of assembly spelunking, so treat this file as
the source of truth and don't repeat the work.

## Camera lifecycle addendum — 2026-09-06

Verified in build 17584229: CameraManager.EnableCinemachineCamera disables
stageCamera (CarCameras) and enables CinemachineBrain. DisableCameraManagers
disables both. Finish/replay take the former path; intro can take the latter.
A CarCameras.LateUpdate postfix cannot be relied on to release a mounted child
after its component is disabled. Current RC patches those transitions and retains
a watchdog fallback. This is code evidence, not a rendered-game test.

## Install under test

See [the 2026-09-08 signal audit](research/2026-09-08-wheel-signals.md) for newly
verified analog-handbrake torque propagation, PlayerVibrator landing/gamepad-rumble
paths, suspension units and the outstanding consumer telemetry sampling defects.
That addendum records method-body evidence without redistributing game code.

| Property | Value |
|---|---|
| Steam app id | 550320 |
| Build id | 17584229 |
| Install root | `D:\Program Files (x86)\Steam\steamapps\common\artofrally` |
| Size on disk | 7.4 GB |
| Publisher / title | Funselektor Labs / art of rally (`artofrally_Data/app.info`) |
| Mod loader present | none — clean vanilla install |

## Engine

**Unity 2019.4.38f1, Mono scripting backend.**

Read from `globalgamemanagers`, `level0` and `UnityPlayer.dll`. The Mono backend
is confirmed by the presence of both `MonoBleedingEdge/` and
`artofrally_Data/Managed/` with real IL assemblies — an IL2CPP build would have
neither.

This is the best possible case for modding:

- `Assembly-CSharp.dll` is ordinary IL, readable and patchable.
- Unity 2019.4 is squarely inside BepInEx 5's and Unity Mod Manager's tested range.
- Harmony patching works without any IL2CPP interop shim.

## Input stack

Rewired, with all four backends shipped:

```
artofrally_Data/Managed/Rewired_Core.dll
artofrally_Data/Managed/Rewired_Windows.dll
artofrally_Data/Plugins/x86_64/Rewired_DirectInput.dll
artofrally_Data/Plugins/x86_64/Rewired_WindowsGamingInput.dll
```

Rewired enumerates DirectInput wheels natively, which matches the community
report that **wheel input already works and only force feedback is missing**.
This is why `xoutput-redux` is very likely unnecessary — see
[FORCE-FEEDBACK.md](FORCE-FEEDBACK.md) for what is actually missing.

## The missing force feedback plugin

The single most important finding. Full detail in
[FORCE-FEEDBACK.md](FORCE-FEEDBACK.md); the short version:

`Assembly-CSharp.dll` contains a complete `ForceFeedback` MonoBehaviour that
P/Invokes seven entry points from a native module named `UnityForceFeedback`.
**`UnityForceFeedback.dll` does not exist anywhere in the 7.4 GB install.**

All five Logitech wrappers shipped. The one generic force feedback wrapper did
not. The managed side is fully written and wired to `CarDynamics`; it is dead
only because its native counterpart is absent.

### Native plugins actually shipped

```
LogitechGArxControlEnginesWrapper.dll     Rewired_DirectInput.dll
LogitechGkeyEnginesWrapper.dll            Rewired_WindowsGamingInput.dll
LogitechLcdEnginesWrapper.dll             UnityFbxSdkNative.dll
LogitechLedEnginesWrapper.dll             discord_game_sdk.dll
LogitechSteeringWheelEnginesWrapper.dll   lib_burst_generated.dll
                                          steam_api64.dll
                                          xaudio2_9redist.dll
```

Note `LogitechSteeringWheelEnginesWrapper.dll` **is** present, and
`Assembly-CSharp.dll` binds 40 entry points from it including the whole force
feedback surface: `LogiPlayConstantForce`, `LogiPlaySpringForce`,
`LogiPlayDamperForce`, `LogiPlayDirtRoadEffect`, `LogiPlayBumpyRoadEffect`,
`LogiPlaySlipperyRoadEffect`, `LogiPlaySurfaceEffect`, `LogiPlayCarAirborne`,
`LogiPlaySideCollisionForce`, `LogiPlayFrontalCollisionForce`,
`LogiPlaySoftstopForce`. That is a complete, already-shipped fallback path for
Logitech wheels specifically.

## The physics model is a real sim

This is the finding that makes the whole project worth doing. Under the
isometric camera, art of rally runs a genuine load-sensitive tire model.

### `Wheel`

Per-wheel fields, all present:

```
slipRatio_hat        slipAngle_hat         tanSlipAngle
differentialSlipRatio lateralSlipVelo      longitunalSlipVelo   [sic]
Fx  maxFx            Fy  maxFy             Mz  maxMz
latForce  longForce  totalForce            force
surfaceType          physicMaterial        isOnPuddle
pressure  optimalPressure  pressureFactor  tirePressureEnabled
tirePuncture  tireOffRim  rimScraping
tireDeflection  lateralTireDeflection
lateralTireStiffness  longitudinalTireStiffness  verticalTireStiffness
suspensionTravel  suspensionRate  bumpRate  reboundRate
brakeFrictionTorque  handbrakeFrictionTorque  rollingResistanceTorque
radius  rimRadius  sidewallHeight  width  mass
```

**`Mz` / `maxMz` is the self-aligning torque** — the aligning moment the tire
generates about the steering axis. That is precisely the quantity real force
feedback is built from, available per wheel, every physics frame. Its presence
is what makes a *good* FFB implementation possible rather than a fake one
synthesised from lateral G.

### `Drivetrain`

```
minRPM  maxRPM  torque  netTorque  netTorqueImpulse
maxPower  maxPowerRPM  maxTorque  maxTorqueRPM
gearRatios  finalDriveRatio  transmission  neutral  first  firstReverse
clutch  clutchPosition  clutchMaxTorque  autoClutch  engageRPM  disengageRPM
shifter  automatic  shiftUpRPM  shiftDownRPM  shiftTime  shiftTriggered
revLimiter  revLimiterTriggered  canStall  startEngine
differentialLockCoefficient  engineInertia  drivetrainInertia
```

### `CarController`

```
steerInput  brakeInput  throttleInput  handbrakeInput  clutchInput  startEngineInput
velo  veloKmh  body (Rigidbody)  drivetrain  cardynamics  axles  allWheels
ABS  ABSTriggered  ABSThreshold     TCS  TCSTriggered  TCSThreshold
ESP  ESPTriggered  ESPStrength      steerAssistance  steerCorrectionFactor
```

Methods `GetInput`, `Update`, `FixedUpdate`, `DoABS`, `DoTCS`, `DoESP` are all
viable Harmony targets. The `*Triggered` flags are a free, high-quality signal
for both telemetry and FFB effects.

### `CarDynamics`

```
enableForceFeedback (bool)    forceFeedback (float)
centerOfMass  originalCenterOfMass  deltaCenterOfMass
frontRearWeightRepartition  frontRearBrakeBalance  frontRearHandBrakeBalance
antiRollBarForce  normalForceF  normalForceR  inertiaFactor
physicMaterials (List<MyPhysicMaterial>)  tridimensionalTire  airDensity
```

`CarDynamics.forceFeedback` is the float the game itself computes and hands to
the `ForceFeedback` behaviour. The full path is:

```
physics (incl. Wheel.Mz) -> CarDynamics.forceFeedback -> ForceFeedback.Update()
    -> SetDeviceForcesXY()  ->  [missing UnityForceFeedback.dll]
```

## Camera system

```
CarCameras:   target  distance  height  yawAngle  initialPitchAngle
              currentPitchAngle  MinFOV  MaxFOV  CurrentFOV
              rotationDamping  heightDamping  smoothTimeFOV  smoothTimeTilt
              CurrentCameraAngle  CameraAnglesList  cardynamics  myTransform
CameraManager: stageCamera  CameraMainTransform  cinemachineBrain
              EnableStageCamera  EnableCinemachineCamera  DisableCameraManagers
CameraAngles (enum):        CAMERA1 .. CAMERA8
CameraTypeBehaviour (enum): GAMEPLAY, HELI, STATIC_FIXED, STATIC_PAN, DOLLY, NONE
```

`Cinemachine.dll` ships, and `CameraManager` holds a `cinemachineBrain`.

`CarCameras` is an ordinary follow camera driven by `distance` / `height` /
`yawAngle` / FOV. Repositioning it to a bonnet mount is a matter of setting
those fields, which is exactly what the existing Nexus "Camera Mod" does.

### One dead end, recorded so nobody chases it twice

Grepping the assembly turns up a `FirstPerson` symbol. **It is not a camera.**
It is a member of the `EnvironmentControllerType` enum. `CameraAngles` contains
only `CAMERA1`..`CAMERA8`. There is no latent first-person camera mode to
switch on.

## Ecosystem

- The established loader for this game is **Unity Mod Manager**, not BepInEx.
  The Nexus "Camera Mod" states *"Adds more camera perspectives to Art of Rally.
  Also offers a small camera editor. Requires 'Unity Mod Manager'."*
- [`MMike17/ArtOfRally_ModBase`](https://github.com/MMike17/ArtOfRally_ModBase)
  is a UMM + Harmony template targeting art of rally v1.5.5, with settings
  handling and an `Info.json`; mods surface under Ctrl+F10 in game.
- PCGamingWiki and the Steam forums both record wheel support present, force
  feedback absent.

## How to reproduce this analysis

No decompiler is needed and nothing was downloaded. The type, field and method
names, and the P/Invoke table, are all readable from assembly metadata with
`System.Reflection.Metadata`, which ships in the .NET SDK:

```powershell
# see tools/ in this repo, or roll it inline:
$pe = [System.Reflection.PortableExecutable.PEReader]::new(
        [System.IO.File]::OpenRead("$game\artofrally_Data\Managed\Assembly-CSharp.dll"))
$md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
# enumerate $md.TypeDefinitions, then GetFields()/GetMethods() per type;
# MethodDefinition.GetImport() yields the DllImport module and entry point.
```

## Open questions — all four answered

Posed 2026-08-31, settled by the end of 0.2.1. Kept with their answers because
the questions are the ones anyone re-deriving this would ask.

1. **Is the `ForceFeedback` MonoBehaviour attached to a live GameObject and
   enabled at runtime?** **No.** Nothing ever calls `AddComponent<ForceFeedback>()`
   or `GetComponent<ForceFeedback>()` anywhere in the assembly. The class name
   in `sharedassets0.assets` was suggestive and misleading. This is why the
   installed DLL was never called, and why route A was dead.
2. **Is `CarDynamics.enableForceFeedback` true by default?** **No — it is never
   set at all**, anywhere. Since `Wheel` guards its aligning-torque calculation
   on it, `Mz` is permanently zero in the shipped game. The mod sets the flag.
3. **Is the game's own force curve any good?** **Unanswerable, and moot.**
   `CarDynamics.forceFeedback` is never assigned either, so there is no curve to
   judge — the feature was built from both ends and never joined in the middle.
   The mod computes its own force, and `Mz` turned out to be the wrong basis
   for it anyway (it reverses sign past ~8° slip; see FORCE-FEEDBACK.md).
4. **Does exclusive DirectInput acquisition fight Rewired?** **No.** The
   exclusive acquire coexists with Rewired's non-exclusive one on every stack
   reported so far. Losing the window focus *does* return the device
   non-exclusively — `0x80040205` — which is a separate problem with its own
   fix.

Full evidence for 1–3 is in [FORCE-FEEDBACK.md](FORCE-FEEDBACK.md) under
"Phase 0 result"; current open defects are in
[KNOWN-ISSUES.md](KNOWN-ISSUES.md).
