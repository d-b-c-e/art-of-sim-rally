# Integration with triple-screen-optimizer

The optimizer should own hardware discovery and user-facing measurement. This
repo should own Art of Rally adaptation. Their stable seam is
`contracts/triple-screen-layout.schema.json` plus the projection semantics in
`Dbce.TripleScreen.Core`.

Minimum optimizer inputs:

- detected monitor count, pixel resolution, desktop coordinates, refresh rate,
  GPU/vendor, and whether Windows currently exposes one Surround display;
- physical active width/height (manufacturer value preferred; diagonal/aspect
  estimate otherwise), curve radius, and bezel width;
- left/right side-panel yaw, eye distance perpendicular to center screen, and
  vertical eye offset.

Recommendation logic should prefer:

1. true-triple compositor over a working single-wide output when the mod is
   installed and the profile has passed compatibility gates;
2. NVIDIA Surround ultrawide when Surround is already available or robust
   single-window output is required; and
3. separate Unity displays only while explicitly marked experimental.

The optimizer may write this mod's own profile file after validating against the
schema. It should back up an existing profile and perform an atomic replace. It
should not edit Art of Rally PlayerPrefs or enable/disable NVIDIA Surround in
the first implementation.
