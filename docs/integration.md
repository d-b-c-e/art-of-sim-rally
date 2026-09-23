# Integration with triple-screen-optimizer

The optimizer should own hardware discovery and user-facing measurement. This
repo should own Art of Rally adaptation. Their stable seam is the three pinned
schemas in `contracts/`, the file protocol below, and the projection semantics
in `Dbce.TripleScreen.Core`.

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

The canonical file protocol is:

- desired state: `%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\desired-layout.json`;
- observed state: `%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\status.json`.

If the game process cannot see a freshly exported per-user file, the optimizer
also stages identical bytes at `Mods/DbceTripleScreenArtOfRally/desired-layout.json`
inside the installed adapter. The mod prefers the canonical per-user file when
visible and reads the staged copy only when that file is absent from its view.
The active layout SHA-256 in `status.json` still lets the optimizer verify that
the game accepted exactly the profile it exported. Both writes are backed up
before replacement.

The optimizer writes desired state atomically after schema validation. The mod
never edits that file; it validates it strictly, applies only supported gated
behavior, and atomically publishes observed status. Consumers must require an
`active` state, matching layout SHA-256, and the expected active capabilities
before presenting a green result. A matching hash alone is not proof that the
rendering path is active.

The first implementation does not edit Art of Rally PlayerPrefs or toggle
NVIDIA Surround. It advertises only `nvidia-surround` and `borderless-span`,
not separate displays.
