# Proposed Architecture

## Boundary

Keep two layers:

1. `Dbce.TripleScreen.Core` owns measurements, display planes, camera bases,
   asymmetric frusta, and projection matrices. It has no Unity, game, Windows,
   or UI dependency.
2. `ArtOfRally.TripleScreen.Mod` owns discovery of the game's authoritative
   camera, lifecycle, Unity matrix conversion, render targets, compositor,
   UI routing, and UMM settings.

`triple-screen-optimizer` can consume the .NET Standard library directly if its
stack is .NET, or exchange schema-versioned JSON and reproduce/host the same
math in a small service/CLI. The contract uses millimetres and degrees and must
remain backwards compatible within a schema version.

## Runtime pipeline

Preferred true-triple path:

```text
CarCameras / Cinemachine / art-of-sim-rally
                    |
            authoritative Camera Main
                    |
       late transform + clip/effect snapshot
              /           |           \
       left camera    center camera    right camera
        off-axis         off-axis         off-axis
          RT L             RT C             RT R
              \            |            /
                  wide compositor
                         |
       Surround or borderless 3-panel output
```

Key rules:

- The original `Camera Main` remains tagged main and is the lifecycle anchor.
- Render cameras have no audio listener and no MainCamera tag.
- Projection is applied as late as necessary and re-applied every frame because
  the game, Cinemachine, and TAA can update camera state.
- Start with post-processing and TAA disabled on clones. Add components/effects
  one at a time using evidence from the inventory probe.
- Prefer one full render texture per view over three partial `Camera.rect`
  viewports; this isolates post effects and enables a later curved-panel warp.
- UI renders once. In a wide output it should default to the center-panel safe
  area; optional HUD spreading is a separate feature.
- Restore or destroy all created state on disable, scene transition, and unload.

## Output modes

### NVIDIA Surround

Recommended first. The driver exposes one combined resolution, optionally with
bezel correction. The true-triple compositor fills that surface. If the mod is
disabled, the game still has a usable single-camera ultrawide fallback.

### Borderless desktop span

Same wide compositor, but a later Windows adapter sizes and positions the game
window across ordinary extended monitors. Keep window control outside the core
and require exact monitor topology validation before changing a window.

### Separate Unity displays

Experimental. Activate display 1/2 once, send one camera to each target, and
route a center UI canvas explicitly. The user must restart the game to undo
display activation. Do not make this the default until window ordering, mixed
DPI, refresh rate, focus/input, pause, and teardown have passed.

## Configuration flow

1. Optimizer reads EDID/Windows topology for count, pixel modes, coordinates,
   GPU, and whether Surround appears active.
2. User supplies/validates physical width or diagonal/aspect, curve radius,
   bezels, side angles, eye distance, and vertical eye offset.
3. Optimizer validates the contract and recommends an output mode.
4. Art of Rally adapter reads the same JSON, builds three `DisplaySurface`
   instances, and converts the core matrices/bases to Unity types.
5. The adapter writes only its own UMM configuration. It should not mutate
   registry PlayerPrefs or NVIDIA settings in the first release.

## Phases and gates

1. **Inventory:** capture cameras, canvases, effects, projections, displays in
   menu, stage, pause, replay, and photo mode.
2. **Single-camera override:** apply the center physical FOV/off-axis matrix and
   prove handoff/restoration across all camera states.
3. **Three unprocessed views:** render three RTs without post effects; validate
   seams with a grid overlay and representative stages.
4. **Effects and UI:** clone/route only the components proven safe; test every
   AA mode, weather/fog, shadows, menus, HUD, photo mode, and replay.
5. **Output alternatives:** Surround first, then borderless span, then separate
   displays if it adds enough value.
6. **Curvature:** optional cylindrical compositor warp using curve radius and
   measured active-area geometry.

## Principal risks

- Three views can approach three times the scene render cost before shared work.
- Global shader state and screen-space effects may be overwritten by the last
  camera and differ at seams.
- TAA can reset/jitter custom projection matrices and maintain per-camera
  history that does not agree at panel edges.
- Camera lifecycle changes between gameplay, Cinemachine sequences, replay, and
  photo mode; stale clones could render the wrong transform or survive teardown.
- Multiple UMM camera mods can fight over update order. If `ArtOfSimRally` is
  loaded, the triple renderer should sample after its mounted-camera update and
  never alter its saved camera rotation list.
- 1500R/other curved panels need warp for exact geometry; a planar approximation
  must be labeled as such in the UI.
