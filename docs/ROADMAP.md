# Roadmap

Ordered by what settles the most uncertainty per unit of effort, not by what is
most exciting.

> **The original phases 0–5 are all shipped.** Force feedback was proved,
> re-derived from lateral force, and released; the UMM mod, telemetry, the
> mounted cameras and public distribution all followed. That history is kept
> at the bottom under [Shipped](#shipped) because the reasoning still explains
> why things are the way they are. What follows is what is actually left.

Open defects are tracked in [KNOWN-ISSUES.md](KNOWN-ISSUES.md); this file is
about work, not symptoms.

## Now — drive one stage, then release 0.2.3  ← next

[Issue #1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1) (stock cameras
3–8 rendering reversed) is **root-caused and fixed in the working tree**, and the
same fix should close the end-of-stage wobble that has been open since
2026-09-01. Neither is confirmed on screen, because injected input no longer
drives this game's UI — that takes a person at the rig.

One stage settles both:

1. Cycle bonnet → bumper → a stock angle. The stock angle should look forward
   down the road, not back at the car.
2. Drive to the finish and watch the results cinematic for the residual swing
   ([KI-2](KNOWN-ISSUES.md#ki-2--the-camera-moves-oddly-at-the-end-of-a-stage)).

Then release, and reply to the reporter — they are on 0.2.1, they hit this on a
Thrustmaster T300 RS GT, and they came to the mod from an x360ce workaround, so
they are worth keeping. Detail and the mechanism are in
[KI-1](KNOWN-ISSUES.md#ki-1--stock-cameras-38-render-reversed).

## Next — adopt toolkit 0.2.0

The pin moved to `v0.4.0` on 2026-09-04 and the DLL is a drop-in (37 exports,
strict superset of the 28 that shipped in 0.2.2). Nothing in the mod uses the
new surface yet. Two pieces are worth taking, in this order — and **both are now
unblocked**: the missing exports this file previously recorded as the critical
path were added in toolkit 0.4.0 at this repo's request.

### `SetPreferredDeviceGuid` — pick the wheel by instance GUID

The wheel is chosen today by index, then by name, then by trying every
force-feedback candidate in turn, and all three are guesses when a Fanatec base
presents two devices called `FANATEC Wheel`. An instance GUID is unambiguous and
survives a replug, which the index does not.

This was blocked until 0.4.0, because `SetPreferredDeviceGuid` took a GUID that
nothing handed out — fine for a native proxy that enumerates DirectInput itself,
useless to a mod that sees devices only through the toolkit.
**`GetDeviceGuid(index, out16)` and `GetAnyDeviceGuid(index, out16)` close the
round trip**, returning the 16 raw bytes for an entry in the
`EnumerateDevices` / `EnumerateAllDevices` list — the same list the panel's
dropdowns are built from.

The consumer side is then straightforward: P/Invoke the two getters, add a GUID
field to `Settings` written when a device is picked in the panel, pass it before
`InitDirectInput`, and keep the name/index path as the fallback for settings
written by an older version. A GUID that no longer matches anything attached
must fall back rather than fail — the native side already logs and falls back in
that case.

### Hardware periodic effects — road texture and tyre slip

`CreatePeriodicEffect` / `UpdatePeriodicEffect` / `ReleasePeriodicEffects` let
the wheelbase render an effect itself instead of the mod synthesising it through
the constant force. That matters more than it sounds: a texture ridden on the
constant force loses roughly 26% to zero-order-hold roll-off at 60 Hz, and
another 30–60% to soft saturation when it sits on top of steering load.

This is the surviving substance of the old phase 3 — surface texture per
`surfaceType` / `physicMaterial`, kerb and impact effects, `ABSTriggered` /
`TCSTriggered` as discrete effects. The game already computes every input
needed; see the `Wheel` field list in [FINDINGS.md](FINDINGS.md).

Before any of it, note [U-3](KNOWN-ISSUES.md#u-3--the-low-speed-fade-scales-the-force-it-does-not-cap-it):
the low-speed fade multiplies, it does not cap. Harmless today, but an impact
effect is a transient several times cornering `Fy` and can arrive at any speed,
so impacts need their own limit rather than leaning on the fade.

**Take a damper first, before any of the texture work.** The output today is a
pure centring force with nothing opposing the wheel's rate of movement, so it
overshoots when a slide gathers up and oscillates on a direct-drive base
([KI-6](KNOWN-ISSUES.md#ki-6--the-wheel-snaps-back-as-the-car-straightens-out-of-a-slide)).
`Smoothing` is the only thing resisting that now and it is the wrong tool — a
low-pass on the force delays every cue, not just the unwanted one. A damper is
smaller than the texture work, fixes a thing a user has actually complained
about, and proves the effect plumbing before anything subtle rides on it.

Toolkit 0.4.0 adds the condition effects it needs:
`CreateConditionEffect(type)` — 0 spring, 1 damper, 2 inertia, 3 friction —
returning a slot, then `UpdateConditionEffect(slot, coefficient, saturation,
deadband, offset)` with everything in −10000..10000, and
`ReleaseConditionEffects()`. Two things to design around, both from the
toolkit's own guidance:

- **`-1` from `CreateConditionEffect` is a legitimate answer, not an error.**
  Plenty of drivers expose no condition effects at all. Treat it as "damp in the
  force model instead", not as a failure path — the same discipline as the FFB
  candidate fallback.
- **A hardware damper is not a drop-in for a model damper term.** The base
  computes it from axis velocity continuously, between our updates, which is
  exactly why it feels smoother than anything we can synthesise at 60 Hz. But it
  knows nothing about road speed, grip or the car, so it cannot be scaled the
  way a term inside our own force model could. The likely answer is **both**: a
  small constant hardware damper for stability, plus a speed-scaled term in the
  force model for feel. Note this mod computes its own force and does not use
  the toolkit's `ForceModelSettings`, so the model-side term is ours to write.

### Neither is blocked any more

This section previously recorded both items as blocked on missing toolkit
exports. Toolkit 0.4.0 added them — verified here rather than taken on trust:
37 exports, a strict superset of the 28 the mod ships today, nothing removed,
`GetDeviceGuid`, `GetAnyDeviceGuid`, `CreateConditionEffect`,
`UpdateConditionEffect` and `ReleaseConditionEffects` all present, x64. The
remaining constraint is a rig to tune against, not an API.

## De-duplicating against the toolkit

The force curve exists twice: `ForceCurve` here, `simlite@2` in
dbce-wheel-mod-toolkit. The P/Invokes exist twice too — `FfbNative` and
`WheelInput` hand-roll what `WheelFfbNative` already wraps. Neither duplicate is
urgent, and the force model is the one thing users have actually praised, so
this is deliberate and staged rather than a single swap.

Order matters, and step 1 gates everything after it.

1. **The conformance vector** — `docs/force-curve-vector.csv`, generated by
   `tools/force-vector`. **Done**, and adopted upstream as a test in toolkit
   0.6.0. It immediately earned its keep: see
   [U-1](KNOWN-ISSUES.md#u-1--simlite1-was-not-art-of-rallys-tuning-simlite2-is).
2. **Adopt `WheelFfbNative` for plumbing only.** Drops ~15 hand-rolled
   `DllImport`s and hands over `DeviceGuid` / `PreferGuid` / `CreateCondition`
   for free. Behaviour-neutral if two things survive: `ResolveDllPath`, which
   exists because a UMM mod does not inherit Unity's native-plugin search path,
   and `SetLogPath` keeping the historical `ArtOfSimRallyfb.log` location.
   Verifiable by "does the wheel still work", cheap to revert.
3. **Adopt `ForceModel`**, only once the vector says the numbers match.
   - Take **`ForceProfile.SimLite()`** (toolkit 0.7.1+), which returns the model
     and its conditioning together as an in-code literal — no file IO. Not
     `ForceModelSettings.SimLite()`, which is half a tune; it now derives from
     the profile and says so in its own doc comment, but pairing it with a
     default `ForceShaper` is exactly the defect the vector exposed.
   - That also settles the **in code, not the ini** question in our favour
     without argument: the preset is a literal, so `force-profiles.ini` need not
     be deployed. The toolkit's own advice is to prefer `LoadFrom`, because a
     file can be A/B tested and player-overridden without a rebuild. Worth
     revisiting only if someone asks for user-editable tunes; today it would add
     a deployed data file, a `package.ps1` payload and an install surface for
     nothing.
   - The clamp order is settled: the toolkit adopted ours in v0.7.0
     ([U-2](KNOWN-ISSUES.md#u-2--clamp-order-resolved-the-toolkit-adopted-ours)).
   - **`simlite@2` is feel-neutral by construction**, which is the whole point of
     it. Its shaper states every value rather than inheriting: `Deadzone` 0,
     `SoftSaturation` 0, `SlewPerSecond` 0, `OutputDeadband` 0, `RampSeconds` 0,
     `PeakLimit` 1, fade 3→12 km/h, and `AttackSmoothing` = `DecaySmoothing` =
     0.2, matching our single symmetric coefficient. So adopting it does **not**
     bring a soft knee, a deadzone or an asymmetric filter. An earlier note here
     said it would; that was wrong, and it was wrong in the direction of
     inventing work.
   - Know which half of that each test carries, because they are not equal proof.
     **Ten of the eleven shaper terms are verified against our own vector on all
     640 rows** (the toolkit's `FullCurveMatches`, asserting the float to 1e-4
     and the truncated integer the device receives). The eleventh is the
     smoothing, which that test zeroes, and it is carried separately by the
     step-response block — which is why the profile insists attack equals decay.
   - **Do not apply the gain twice.** `ForceShaper.GainFromStrength` is
     `Math.Max(0, Strength) / 50f`; ours is `Strength / 50f`. They are the same
     scale, so `Strength` passes straight through with no rescaling — and that
     is exactly the trap. If we adopt `ForceShaper` and keep handing
     `cfg.GainFromStrength` to `ForceCurve.Normalised` *while* also setting
     `ForceShaper.Strength`, the gain is squared: a user at 26 gets 0.27 instead
     of 0.52, roughly halving the force for the two people who actually report.
     The vector cannot catch it, because it tests the curve rather than the
     integration.

     Toolkit 0.8.0 moved gain ahead of the soft-saturation knee, so in general
     a squared gain now lands somewhere else on the `tanh` rather than simply
     halving. **For us the 0.27 still holds exactly**, because `simlite@2` sets
     `SoftSaturation` to 0 and the knee is skipped entirely.
   - The chain reordered in toolkit 0.8.0 and is now deadzone → EMA →
     gain/invert → soft saturation → slew → fade → ramp → clamp → output
     deadband. Gain lands *before* the fade, as ours does, so the ordering
     caveat recorded here previously is resolved rather than merely tolerated.

     **What remains is the EMA.** Theirs is stage 2, before gain and before the
     fade; ours is last, applied to the already-faded and already-clamped value.
     The coefficient semantics are identical (0.2 means keep 20% of the previous
     output on both sides), and a constant gain commutes with an EMA — but the
     **fade does not**, because it varies with speed. So the two orders agree at
     steady speed and diverge while crossing the 3–12 km/h band, which is every
     launch from the start line.

     Simulated at 50 Hz with a steady pre-fade force of 0.5 and the shipped
     smoothing of 0.2, the peak difference is **25/10,000 at the wheel for a
     gentle launch, 104/10,000 for a violent one**, always near the middle of the
     fade band. That is a quarter of one percent to one percent of full scale,
     for about a second. Negligible — but it is *not covered by either test*:
     `FullCurveMatches` zeroes attack and decay so the EMA is inert, and the
     step-response block holds no speed at all. The feel-neutrality proof is a
     **steady-state** proof. Worth knowing before someone measures a launch and
     thinks something broke.

Staying here regardless, because it is game-specific and belongs nowhere else:
`WheelInput`, the cameras, `TelemetryPump`, the panel, `GameState`, and the
Harmony hooks.

Not needed for any of this yet: the pin stays at **v0.4.0**. Nothing under
`native/wheelffb` or `dotnet` changed through v0.6.0 — only the profiles file and
the tests — so v0.4.0 is still the verified one. Take v0.6.0 when step 3 wants
`simlite@2`.

## After that

**Per-wheel force defaults.** `FyReference` has been retuned twice on one rig
and is Settings.xml only ([KI-4](KNOWN-ISSUES.md#ki-4--force-scaling-is-tuned-for-one-wheel)).
Either expose it, or ship a small table of starting points by wheel class once
enough reports exist to build one. Reports are the blocker, not the code.

**Close the loop with Fanatec and Thrustmaster owners.** Three fixes shipped in
0.2.2 on the strength of one support bundle and have never run on the hardware
they target. The T300 RS GT in issue #1 is the first Thrustmaster report of any
kind; whatever comes out of KI-1, ask what else that rig does or does not do.

**The Logitech SDK path.** `LogitechSteeringWheelEnginesWrapper.dll` ships with
the game and `Assembly-CSharp.dll` already binds `LogiPlayDirtRoadEffect`,
`LogiPlayBumpyRoadEffect`, `LogiPlaySlipperyRoadEffect` and
`LogiPlaySurfaceEffect`. For a Logitech wheel those are purpose-built rally
effects available for nothing. Logitech only, so it can only ever be a bonus
path alongside DirectInput — and hardware periodic effects may make it
redundant. Evaluate after that lands, not before.

**Nexus Mods.** GitHub releases are the source of truth and the audience is
already there; Nexus is where art of rally modders actually look. See the
channels table in [RELEASING.md](RELEASING.md).

## Explicitly out of scope

Cockpit view, physics or assist changes, redistributing game assemblies, and
switching Rewired's input backend at runtime. Each is recorded with its
reasoning under [Will not fix](KNOWN-ISSUES.md#will-not-fix).

---

## Shipped

Kept for the reasoning, not the status. Dates are when the work landed.

| Phase | What | Landed |
|---|---|---|
| 0 | **Prove the force feedback path.** Answered 2026-08-31: the DLL never loaded because the `ForceFeedback` behaviour is never attached — the feature was built from both ends and never joined in the middle. Route A (supply the missing DLL and let the game drive it) was dead on arrival; route B (compute the force ourselves) is what shipped. | 0.1.0 |
| 1 | **The Unity Mod Manager mod.** `Info.json`, entry point, Ctrl+F10 settings, Harmony hooks on the car's physics update. | 0.1.0 |
| 2 | **Telemetry against the real game.** Forza Horizon 324-byte packet, anchored on `Speed`@256 and `Gear`@319, verified with SimHub and a ButtKicker. | 0.1.0 |
| 3 | **Force feedback worth having.** `Mz` first, then replaced by front-axle lateral force × pneumatic trail after `Mz` proved to reverse sign mid-corner. Low-speed fade, sign settled across three wheels, re-acquire after focus loss. | 0.2.1 |
| 4 | **Bonnet camera**, and a bumper view beside it, appended to the game's own rotation with a numpad tuner. | 0.2.0 |
| 5 | **Make it shareable.** Public repo, MIT, tagged releases with a double-click installer, user-facing README and troubleshooting. | 0.1.0 → 0.2.2 |

Phase 3's remaining ambitions — surface texture, kerb and impact effects,
discrete ABS/TCS effects — were parked for want of a way to render them
properly. Toolkit 0.2.0's periodic effects are that way, which is why they
appear under [Next](#next--adopt-toolkit-020) rather than here.
