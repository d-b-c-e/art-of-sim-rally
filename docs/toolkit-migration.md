# Shared toolkit migration gate

**Status: planned, 2026-09-26.** The public Art of Rally mod must remain
buildable without private GitHub access. Keep the Unity camera, display,
post-processing, menu, and status hooks in this repository.

The candidate private toolkit revision is
`1d67d066e1f0c165172597c0397d1056f2502c53`. All eight C# files in this
repo's `src/Dbce.TripleScreen.Core` match the same-named toolkit files byte for
byte at that revision. The toolkit also contains `EyeRay` and
`EyeRayCalculator`; the Art of Rally adapter does not need them yet. V1 layout,
manifest, and runtime-status schemas remain pinned here and must be synced
through an explicit contract check.

Before replacing the local core with a pinned toolkit artifact or vendored
permitted source:

1. Run matching numerical fixtures against both revisions: measured panel
   corners, asymmetric frustum edges, forward/up basis, matrices, and shared
   hinge rays for symmetric and asymmetric rigs. Compare with explicit
   tolerances and retain the fixture outputs with the revision hash.
2. Complete the attended in-game seam and lifecycle checks for the current
   local core in both feasible paths: three views in an exact-width Surround
   output and direct rendering to three independent displays. Record what
   remains untested; do not substitute offline math tests for visual evidence.
3. Pin an immutable revision and use a redistributable source snapshot or
   package for public builds. Check the toolkit's license and provenance before
   vendoring. Never add a local `E:\Source` project reference or a mandatory
   private submodule to the public solution.
4. Build and rerun the same fixtures and attended seams after substitution.
   Keep the game-bundled Newtonsoft protocol reader in this adapter until a
   portable parser and dependency strategy has been verified.

No reusable toolkit API gap was found for Art of Rally's current matrix path.
Secondary-display activation, camera callbacks, and presentation diagnostics
are Unity/game adapter concerns. The shared native bridge/status writer remain
tracked in the toolkit for other consumers.
