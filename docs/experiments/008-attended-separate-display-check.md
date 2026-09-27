# Experiment 008: Attended separate-display check

## Preflight and loader baseline — 2026-09-26

- Windows exposed three independent 2560x1440 screens: primary at `(0,0)`,
  one at `(-2560,0)`, and one at `(2560,0)`. This is desktop geometry, not a
  verified Unity left/right index mapping. The game was closed before deploy.
- Built the 0.3.0 working tree in Release with zero warnings/errors; all 47
  offline geometry/protocol assertions passed. Backed up the installed mod,
  canonical protocol files, and Player logs in ignored
  `artifacts/experiment008-20260926-230120/`.
- Installed only the five owned build outputs. Every deployed hash matched the
  build. Installed `Settings.xml` and staged `desired-layout.json` remained
  byte-identical to the pre-deploy backup. The separate-display opt-in remains
  absent/false, and the staged/canonical layouts still request
  `nvidia-surround` at 7680x1440.
- Launched via Steam app 550320 with experimental rendering off. UMM loaded
  three of three mods and this adapter loaded without a managed exception.
  Status reported `inactive` / `FEATURE_DISABLED`, zero cameras, and the
  existing Surround layout hash. The game process remained responsive.
- Unity initially attempted a 7680x1440 exclusive-fullscreen transition,
  logged `SetFullscreenState failed (887a0022)`, then reverted to a 2560x1440
  fullscreen window. Diagnostics showed three Unity displays at native size,
  with only display 0 active and effective VSync 1. The game process could not
  see the canonical layout path, so the mod used its identical staged copy.
- Fixed the staging script's process guard to recognize the actual
  `artofrally` process name. Its dry run selected the expected 2560x1440
  separate-display candidate and wrote nothing.

The user confirmed the center-screen menu was visible and responsive. The
baseline game exited through a normal window close. After it exited, the
staging script wrote identical canonical and installed `separate-displays`
layouts with SHA-256
`52fa1c6c561a2d8c080831ae0d76556550a1985dd44f2dc71a91b3b1e6804f4d`.
A second launch with the opt-in off accepted that hash and reported
`inactive` / `SEPARATE_DISPLAY_OPT_IN_REQUIRED`, zero cameras, and only Unity
display 0 active. This run also exited normally.

## First direct-display run — 2026-09-26

With the game closed, the saved UMM settings were backed up and only
`EnableSeparateDisplayPrototype` was changed to `true`; the compositor and
center-preview options stayed off. Unity indices 1 and 2 were tentatively
assigned left and right. On entering gameplay, status reported `active`, three
cameras, `asymmetric-frustum`, `three-projections`, and
`independent-displays`, with recent matching-frame callbacks. All three Unity
displays became active at 2560x1440. The user confirmed that physical
left/center/right order was correct and both scene seams appeared aligned.

The user's screenshot in ignored
`artifacts/experiment008-20260926-230120/color-mismatch-active/` shows a
clear visual failure: the center view is dark and color graded, while the side
views are bright and flat. The source camera has a PostProcessLayer with AA
mode `None`; the side camera clones had no PostProcessLayer. This strongly
implicates missing side-view post-processing, although only a retest can
confirm that it fully explains the image difference. The user reported no
visible tearing during the drive, a strange/narrow FOV similar to the earlier
Surround experiment, and incomplete restoration of the side display windows
after Alt+Tab. Those are separate observations; no scanout timing capture was
made. The process remained responsive with roughly 59–60 Unity updates/s,
effective VSync 1, and no adapter exception in Player.log.

Preserve this first-run evidence as a failed color/effects gate, not a complete
direct-display pass. The 0.3.1 build gave each side camera the source camera's
full PostProcessLayer. Its runtime reported two active side layers and recent
three-camera frames, but the user observed severe side blur and misaligned
seams. The screenshot is preserved in ignored `full-postprocess-failed/`.
This is a failed visual gate; the game was closed after capture.

The 0.3.2 candidate instead gives side cameras an isolated, color-grading-only
PostProcessVolume. It copies the source's current color grade without its motion
blur and other effects. Release build succeeded with zero warnings and 52 offline
assertions passed. The mod UI now offers output mode and FOV directly and can
construct a layout from local screen measurements without the optimizer. The
0.3.2 package was installed with the previous package backed up in ignored
`installed-mod-before-selective-grade/`; hashes matched. It launched with all
three UMM mods loaded. During the stage the user observed broken seams even at
the original FOV, unmatched side lighting/color, and a FOV slider that appeared
to affect only center. The screenshot and runtime snapshot are in ignored
`selective-grade-failed/`. Diagnostics showed 3 recent camera frames, 2 side
post-processing layers, source color grading enabled, and side color grading
disabled. The user closed the game after capture. This is another failed visual
gate, not a direct-display pass.

Decompilation of the installed Unity Postprocessing Runtime shows that
`PostProcessLayer.OnPreCull` calls `Camera.ResetProjectionMatrix()` when physical
camera properties are off. The center driver reapplies its off-axis matrix in
its own late `OnPreCull`; the side cameras had no equivalent hook. This explains
the seam failure independently of FOV and the apparent center-only slider.
The 0.3.3 candidate adds a late side-camera projection hook. It also records
color-copy creation/errors so a disabled side grade cannot pass unnoticed.
The attended 0.3.3 retest still showed broken seams and unmatched color at the
original 1.00x view width. Its screenshot/log are preserved in ignored
`late-projection-failed/`. Runtime logging showed a `NullReferenceException`
during side color copying; side grading was never created. The game was closed.

The 0.3.4 candidate sets `usePhysicalProperties` on side cameras so the
post-processing pre-cull path skips `ResetProjectionMatrix()`. It also adds the
side projection component after the post-processing component as a second
ordering safeguard. Color copy now skips an unnecessary settings hash and
records the failing step plus a stack trace if an exception remains. Color
The attended 0.3.4 drive restored aligned seams; the user also changed FOV and
confirmed all three views continued to align. Side color grading was created
and enabled without an exception. The screenshot in ignored
`physical-camera-partial-pass/` still shows smaller lighting/shadow differences
between the center and sides. This is a partial visual pass. The 0.3.5 candidate
copies all currently active center post-processing effects except MotionBlur
into an isolated side profile, and records the source and copied effect names.
Visual parity and Alt+Tab behavior remain open.

The 0.3.5 drive showed closer post-processing but still mismatched grass/shadow
lighting and objects popping on side monitors. Its screenshot/runtime record is
in ignored `all-safe-effects-partial/`. Source effects were Bloom, ColorGrading,
Grain, MotionBlur, and Vignette; side effects were the same except MotionBlur.
There was no side effect-copy error and update FPS remained near 59. Decompilation
of the installed game assembly shows `GenericRenderer.LateUpdate()` explicitly
calls `Render(Camera.main)`, and `GenericRenderer.Render` calculates frustum
planes from that one camera before rendering instanced objects. This is a
plausible source of side-object popping, but the active instance count still
needs runtime confirmation. The 0.3.6 diagnostic build records that count and
the main camera's components to investigate remaining shadow effects.

The 0.3.6 attended diagnostics showed zero active `GenericRenderer` instances
on this stage. The center gameplay camera has enabled `AmplifyOcclusionEffect`,
`Beautify`, `HxVolumetricCamera`, and `HxVolumetricImageEffect` components in
addition to PostProcessLayer; side cameras had none. The game also contains
Vegetation Studio Pro, whose `VegetationSystemPro` registers only `Camera.main`
by default and exposes `AddCamera`/`RemoveCamera` for additional cameras. The
0.3.7 candidate adds side registrations with direct-to-camera vegetation draws
and broad side vegetation culling, restoring original registrations on release.
It also mirrors Amplify Occlusion and Beautify to the side cameras. These are
game-specific rendering changes and need an attended runtime and performance
check before any support claim.

The attended 0.3.7 run reported one active Vegetation Studio system, one
registered side-camera expansion, two enabled side Amplify Occlusion components,
and two side Beautify components. FPS recovered to roughly 57–59 after load.
The user reported a clear regression: grass/vegetation disappeared entirely on
the left and right screens. The screenshot is in ignored
`vegetation-direct-draw-failed/`. The game was closed; 0.3.7 failed the visual
gate. The 0.3.8 candidate leaves the game's original un-targeted vegetation
draw path intact and changes only its source camera culling mode to
`Complete360`, restoring the original mode on release. This should include
side-visible vegetation in the shared draw without camera-targeted draws.

The attended 0.3.8 drive confirmed that side vegetation stayed visible without
the earlier popping, the three views remained aligned, and overall lighting was
much closer. The remaining visible mismatch was sunlight/godrays appearing only
on the primary monitor. The runtime snapshot and desktop capture are preserved
in ignored `shared-vegetation-pass/`; diagnostics recorded one expanded
vegetation system, three recent camera frames, and roughly 59 updates/s. The
game was closed before the next deploy.

The 0.3.9 candidate mirrors the source camera's enabled `HxVolumetricCamera`
and `HxVolumetricImageEffect` onto each side camera. Each copy has its own
camera callbacks so it cannot bind to the center image effect. It also includes
the side-window foreground restorer for the reported Alt+Tab issue; the
foreground action runs only when application focus returns. Release build
succeeded with zero warnings and 52 geometry assertions passed. Volumetric
lighting and Alt+Tab recovery require separate attended checks.

The attended 0.3.9 run loaded with two enabled side volumetric effects, three
recent display-camera frames, no new render errors, and roughly 59 updates/s
after stage load. The user reported that the lighting was looking good. At the
finish line, however, the center cinematic camera moved while both side screens
froze on the hood. The installed game's `CameraManager` disables `CarCameras`
and enables `CinemachineBrain` on the same Stage Camera for finishing, results,
and replay. Our separate-display eligibility check had been releasing the
driver whenever `CarCameras.enabled` became false. The 0.3.10 candidate keeps
an already armed driver on that same `Camera Main` through the handoff. It does
not attach to an unrelated menu camera before gameplay. Release build again
succeeded with zero warnings and 52 geometry assertions passed; the finish
transition needs an attended retest.

The user reported that the 0.3.10 finish-camera handoff appears fixed. Runtime
checks during the drive still showed three recent display-camera frames and
roughly 59 updates/s. A minor colored halo around vegetation was visible on
all three screens; this has not yet been attributed to the mod. The user is
comparing the same artifact in Surround mode before any renderer change.

The 0.3.10 Surround comparison reached `THREE_VIEW_EXPERIMENTAL` at 7680×1440
exclusive fullscreen with VSync requested and effective count 1. The user
observed tearing, vegetation disappearing, and flatter lighting across the
wide output. No comparable halo was obvious, but the stage was different.
The legacy Surround path manually rendered three cameras to textures during
the source camera's pre-cull and copied those textures over the source image;
it did not carry the source camera's per-camera image effects or the working
vegetation visibility path. The 0.3.11 candidate instead assigns left, center,
and right cameras contiguous thirds of the wide display and uses the same
effect/vegetation path as the separate-display mode. Six new viewport geometry
assertions pass (58 total); the Release build has zero warnings.

Before enabling that candidate, the installed 0.3.11 package was launched
with rendering Off to test stock Surround presentation. The baseline is
7680×1440 exclusive fullscreen, VSync 1, status `inactive`/`FEATURE_DISABLED`.
The saved setting is backed up in ignored
`Settings-before-surround-baseline.xml`. The user changed from Off to Single
wide display during that run, so the stock Surround visual baseline was
inconclusive. With 0.3.11's direct viewport path, the user reported vegetation
and lighting were much better and judged the picture good enough for the
current release candidate. Visible screen tearing remained in Surround. Its
cause is still unknown; VSync 1 and roughly 59 updates/s do not establish
tear-free scanout. The game was closed afterward.
