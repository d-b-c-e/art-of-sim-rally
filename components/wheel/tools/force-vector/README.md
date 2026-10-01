Current adoption: linked ForceCurve forwards to pinned `AxleForceCurve@1`.
The committed 703-row vector remains the original reference. Consumer regression
separately calls the frozen original formula with Unity's real managed Mathf;
vector regeneration alone is not independent evidence. Historical rationale follows.

# force-vector

Emits `docs/force-curve-vector.csv`, the conformance vector for art of rally's
steering force curve.

```bash
dotnet run --project tools/force-vector -- docs/force-curve-vector.csv
```

## Why this exists

The curve is duplicated. It is `ForceCurve` in this repo and `simlite@1` in
[dbce-wheel-mod-toolkit](https://github.com/d-b-c-e/dbce-wheel-mod-toolkit).
Nothing enforces that the two agree, and the failure mode is silent: a force
that is subtly wrong does not throw, it just feels like the mod got worse,
months after whatever changed.

This turns *"does it feel the same?"* into *"is the number the same?"* — the
same move that keeps the telemetry packet honest, where a wrong offset renders a
plausible and completely incorrect dashboard rather than failing.

It is one prerequisite for adopting the toolkit's `ForceModel` in place of the
local one. That swap is otherwise unverifiable without a wheel and a driver, and
judged only by feel; with a vector it is arithmetic.

## What it does not prove

The grid and separate smoothing block do not prove the combined dynamic
pipeline. The pinned toolkit filters before clipping/fade, while this mod filters
after them; a constant-speed clipping counterexample is now in
tests/Regression. See docs/ROADMAP.md. There is no full force-model equivalence.

Nothing about how the force *feels*, and nothing about the parts of the signal
path outside `ForceCurve` — the sign convention at the device, the device's own
scaling, or `Strength` as a user experiences it.

## Design notes

- It **links** `src/ArtOfSimRally.Mod/ForceCurve.cs` rather than copying it, so
  the vector is produced by the code the mod ships. A transcription would only
  prove that two transcriptions agree.
- It is **not** in `ArtOfSimRally.sln`. That solution builds against the game
  assemblies in the local Steam install; this has to build anywhere, including
  on a machine that has never had art of rally.
- The grid is swept over ranges the game actually produces, measured on a real
  drive on 2026-09-02 and recorded in [FORCE-FEEDBACK.md](../../docs/FORCE-FEEDBACK.md):
  lateral force to about 4,400 N per wheel, front slip angles of 12–29° against
  an ideal of 8.5°, and the 3–12 km/h fade band. Values are chosen to straddle
  every corner in the curve — zero, both fade edges and the midpoint, the ideal
  slip angle and its double where the trail reaches its floor, and past
  saturation where the output clamps.
- Smoothing is stateful, so a grid cannot capture it. A step response is
  appended instead, at 0, the shipped 0.2, and the 0.5 a user settled on.
  **The filter is symmetric** — it does not distinguish a rising force from a
  falling one, which is the most likely point of divergence from an
  attack/decay pair.

Regenerate and commit the CSV whenever the curve changes. A diff on that file is
the review: if a change was not meant to alter the force, the CSV should not
move.
