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

The pin moved to `v0.2.0` on 2026-09-04 and the DLL is a drop-in (32 exports,
strict superset of the 28 that shipped in 0.2.2). Nothing in the mod uses the
new surface yet. Two pieces are worth taking, in this order:

### `SetPreferredDeviceGuid` — pick the wheel by instance GUID

**Blocked on a toolkit change. Do not start here.**

The idea is sound: the wheel is chosen today by index, then by name, then by
trying every force-feedback candidate in turn, and all three are guesses when a
Fanatec base presents two devices called `FANATEC Wheel`. An instance GUID is
unambiguous and survives a replug, which the index does not.

The problem is that **nothing hands the GUID out.** `SetPreferredDeviceGuid`
takes one — the toolkit's own example passes "a GUID from my input layer",
which suits a native proxy that enumerates DirectInput itself. This mod has no
such layer: it sees devices only through `EnumerateDevices`, `GetDeviceName`
and `GetDeviceInfo`, none of which return a GUID, and the full 32-export list
has no `GetDeviceGuid`.

So the first move is in **dbce-wheel-mod-toolkit**: add a
`GetDeviceGuid(int index, void* out16)` (and probably `GetAnyDeviceGuid` for the
read-only device list), release, re-pin here. Only then is the consumer side —
a settings field, the panel writing it at selection time, the old name/index
path kept as a fallback for settings written by an older version — worth
writing.

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

**Take a damper first, before any of the texture work** — and note it is
*also* blocked on the toolkit. The output today is a pure centring force with
nothing opposing the wheel's rate of movement, so it overshoots when a slide
gathers up and oscillates on a direct-drive base
([KI-6](KNOWN-ISSUES.md#ki-6--the-wheel-snaps-back-as-the-car-straightens-out-of-a-slide)).
`Smoothing` is the only thing resisting that now and it is the wrong tool — a
low-pass on the force delays every cue, not just the unwanted one. A damper is
smaller than the texture work, fixes a thing a user has actually complained
about, and proves the effect plumbing before anything subtle rides on it. But a
damper is a DirectInput **condition** effect (`GUID_Damper`), and the toolkit
exports only a periodic one, so it needs an export too.

### So the toolkit is the critical path

Both items above need the same thing: one small toolkit release adding
`GetDeviceGuid` and a condition (damper) effect, then a re-pin here. That is the
work to do while waiting for a test window, and it is where the next hour is
best spent — not on the consumer side, which cannot be written until the API
exists.

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
