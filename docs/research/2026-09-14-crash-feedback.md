# Crash feedback investigation — 2026-09-14

The owner wants more noticeable crashes in both wheel FFB and the motion
platform, and confirmed the platform uses **SimHub**. This extends FR-2.
The wheel has no dedicated crash effect yet. A useful collision callback exists
in the game; the current telemetry sampler and encoder preserve synthetic
collision acceleration. The remaining motion question is what SimHub receives
and produces during an actual crash.

This investigation adds offline regression coverage and an implementation plan.
No production effect, recorder change, SimHub setting, toolkit repin or installed
payload changed. Stable remains 0.2.5. The accepted landing effect and 30 Hz
ButtKicker profile are the baseline; the earlier amplifier clipping observation
is not a measurement of motion-platform headroom.

## Verified source findings

### Game collision observation

In game build 17584229, `PlayerCollider.OnCollisionEnter(Collision)` handles
collisions while a stage is underway. Its `ApplyVibration` method requests
gamepad motor rumble when relative speed exceeds 8 m/s; it scales from relative
speed against a 50 m/s reference. Our DirectInput wheel path does not consume
these gamepad commands. This explains a missing effect path, not a calibrated
crash-force relationship or proof that the callback fires for every obstacle.

This callback provides richer evidence than `CollisionSoundEffects.OnImpact`:
the Collision argument can describe relative motion and contact geometry. The
damage branch has a seven-second cooldown and additional damage/tag settings;
damage events should not be the crash detector. The vibration call is outside
that branch. Do not call either game method to synthesize effects: they also
perform game actions. Existing off-track torque and damage behavior stay owned
by the game.

A future observer should run before the original collision method, because a
terminal crash can end the stage inside it. It must confirm the active player
and driving state through the existing passive GameState access, never the
lazy `GameEntryPoint.EventManager` factory. Live callback ownership, contact
normal signs, available impulse values and ordering against the physics sample
still need a capture.

Local inspection used `Assembly-CSharp.dll` SHA-256
`7807A1D674C8214FE1FBD15B8EDC6E3E5E47700AB6E6C2943CF5BF234551C5E0`.
Decompiled material remains ignored under `results/crash-study-20260914` and
must not be committed or packaged.

### Wheel output ownership

The existing toolkit v0.13.0 finite periodic API is a suitable starting point
for a short crash vibration with independent strength. The steering force curve
does not need a retune. Use a bounded vibration first; a collision normal alone
does not establish a physically correct signed steering torque.

Before adding another effect, fix the ownership assumption in
[`LandingController.ToolkitOutput.Release`](../../src/ArtOfSimRally.Mod/LandingController.cs):
it calls `ReleasePeriodics()` because landing currently owns the only periodic
slot. Copying this adapter into a crash controller would let either controller
release the other's effect. Coordinate allocation/cleanup centrally, with an
explicit overlap policy so a landing and body collision cannot stack without
bound. Test disabling either feature, device recovery, pause/focus loss, replay,
reset and shutdown. This is a prerequisite for the proposed feature, not a
confirmed defect in the released single-effect implementation.

### Telemetry and SimHub

[`TelemetryPump`](../../src/ArtOfSimRally.Mod/TelemetryPump.cs) samples each
`CarDynamics.FixedUpdate`. [`TelemetryMotion`](../../src/ArtOfSimRally.Mod/TelemetrySampling.cs)
differentiates world velocity, then projects acceleration into the current car
orientation. It has no acceleration gain, low-pass filter or magnitude clamp.
Spawn/reset, invalid time/data and apparent teleports reset or suppress the
derivative. The standard Forza payload carries local acceleration, velocity and
suspension; it has no separate crash-strength field.

SimHub's motion effects apply their own input scaling, smoothing and limits.
Surge responds to forward/backward acceleration; Sway responds to lateral
acceleration. Pitch/Roll pose alone need not communicate a crash well.
[Official motion tuning guide](https://manual.simhubdash.com/motion-addon/motion-profile-tuning).
Local inspection of the installed Surge implementation also shows its
`AccelerationSurge` input passing through washout and dynamic range handling.
Motion DLL SHA-256:
`C5409E30E705CC240C8FD93F0C1F9DFFDE929B8AC41A44A904101BABC1335807`.
Packaged effect defaults are not evidence of the owner's active settings.

Keep motion separate from ShakeIt: local inspection of built-in **Impacts**
shows a velocity-change input (`FeedbackData.VelocityDistance`); **Road impacts**
uses suspension changes. Increasing acceleration fields would not necessarily
increase shaker Impacts and would misrepresent motion data. Keep the existing
Forza signals and built-in effects; no additional SimHub helper is proposed.

No cause for weak motion crashes is confirmed. A one-step peak might be missed
between sampling/receiver updates or softened later. Compare raw inputs with
effect output before changing transport or tuning. SimHub provides a Game data
view, an effect activity view and computed platform/actuator views under its
profile tools. These distinguish game data from commanded movement.
[Official integrated utilities](https://manual.simhubdash.com/motion-addon/integrated-utilities).

## Regression evidence

Run `dotnet run --project tests/Signals/Signals.csproj -c Release`.
Result: **2,910 assertions passed, including 48 synthetic crash scenarios**.
The suite is already invoked by `Test-Rc.ps1`; its new cases run automatically.
Receipt: `results/crash-study-20260914/signals-test.txt`.

The cases combine 30/50/60/120 Hz, headings 0/90/225 degrees, front deceleration,
front rebound and left/right lateral stops. They use the actual sampler,
projection and vendored encoder/sender, receiving packets on an isolated
ephemeral loopback port. No test sends to SimHub or physical hardware.

- Signed acceleration and velocity survive the encoded UDP path at all headings.
- For a prescribed 10 m/s forward velocity loss, the one-step acceleration is
  -300/-500/-600/-1,200 m/s² at the four rates. Integrated acceleration remains
  -10 m/s. These artificial peaks are test inputs, not suggested motion gains.
- An impact does not remain active after velocity settles.
- Teleports and explicit history resets suppress false impulses and do not
  replay the preceding impact on the next sample.

These checks do not simulate Unity collision resolution, receiver scheduling,
packet loss, SimHub filtering, or physical motion. The preserved Norway drive
has one owner-labelled landing and no labelled crash; it cannot validate a
crash classifier. Existing schema-3 contact flags describe wheels touching the
ground, not body-collision events.

## Implementation and attended test sequence

1. Extend the **separate developer probe** with bounded collision observations:
   active player/reset epoch, physics/realtime stamp, relative velocity, contact
   normal/point, counterpart identity/tag and available impulse/mass. Record
   plain values without retaining Collision objects or doing disk IO in the
   callback. Keep schema compatibility and ordinary no-probe gameplay intact.
2. Record ordinary braking, one front impact and one glancing side impact, plus
   a landing and restart as negative controls. Note stage/car and approximate
   event times. Capture SimHub input/effect output alongside it using built-in
   diagnostics; retain the profile used. Pause, STOP and verify saved files
   before quitting, as in [TEST-DRIVE](../TEST-DRIVE.md).
3. Reconcile body collisions with velocity/acceleration and contact-normal
   projection. A high total speed in a shallow scrape must not become a full
   crash cue. Reject resets, road-contact jitter and repeated contact; establish
   the landing/crash overlap policy from evidence. Add the labelled cases to
   offline regression before selecting strength thresholds.
4. Implement independent wheel **Crash vibration** using shared periodic-effect
   ownership and bounded finite bursts. Run lifecycle/overlap regressions and
   all local RC gates before installing a candidate with the game closed.
5. Compare crash vibration off/on without changing steering. For SimHub motion,
   inspect the recorded Surge/Sway input and output first. If input is present
   but softened, tune a copy of the existing profile, one effect at a time,
   within existing platform limits. If input is absent or missed, fix the
   measured sampling/transport issue. Do not disable crash protection or invent
   physical acceleration to force a stronger response.

Physical crash feel, useful thresholds and the final motion improvement remain
unverified. No attended sign-off or release candidate is implied by this study.
