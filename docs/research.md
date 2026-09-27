# Technical Research

This is the 2026-09-21 feasibility analysis. Its initial preference for a
render-texture compositor was superseded by the attended 0.3.11 direct-viewport
renderer. See [architecture](architecture.md) and [known issues](KNOWN-ISSUES.md)
for current behavior.

Research date: 2026-09-21. Conclusions below distinguish inspected facts from
proposals. No game binary or decompiled source is stored in this repository.

## What was verified

### Engine and runtime

The owner's installed Steam build uses `UnityPlayer.dll` version
`2019.4.38.16628595`, has managed `Assembly-CSharp.dll`, and does not have an
IL2CPP `GameAssembly.dll`: this is Unity 2019.4.38f1 on the Mono scripting
backend. SteamDB also detects Unity and Mono and lists Windows app ID 550320;
the public depot is build 17584229 dated 2025-03-04.

The installed managed directory includes `Unity.Postprocessing.Runtime.dll` and
no URP/HDRP runtime assembly. Decompiled game types directly use
`UnityEngine.Rendering.PostProcessing.PostProcessLayer`, while the official
PostProcessing v2 source describes one layer per camera. This is the legacy
built-in rendering path plus PostProcessing v2, not a scriptable render pipeline.

Sources: [SteamDB configuration](https://steamdb.info/app/550320/config/),
[SteamDB depots](https://steamdb.info/app/550320/depots/), and
[Unity PostProcessing v2 source](https://github.com/Unity-Technologies/PostProcessing/blob/v2/PostProcessing/Runtime/PostProcessLayer.cs).

### Modding surface

Unity Mod Manager is already a proven loader for this title. The open-source
[Camera Mod](https://github.com/thoxx/aor-camera-mod) finds `CarCameras`, appends
camera angles, and edits its parameters. The owner's
[art-of-sim-rally](https://github.com/d-b-c-e/art-of-sim-rally) uses UMM and
Harmony against the current Steam build and already implements mounted cameras.
The separate [VR hack](https://github.com/SuiMachine/Art-Of-Rally-VR-Hack)
demonstrates BepInEx 6 can also load, but mixing loaders adds no benefit for the
first triple-screen prototype. UMM is the lowest-risk starting point and should
coexist with the owner's existing mod workflow.

### Camera behavior

Inspection of the current `Assembly-CSharp.dll` found:

- `CarCameras` is attached to `Stage Camera` and controls the chase transform,
  dynamic vertical FOV (up to 1.1 times the configured value), pitch, clipping,
  damping, and camera cycling.
- The rendering camera is its child, `Stage Camera/Camera Main`. `CameraManager`
  explicitly resets that child's local position and rotation and switches the
  parent between `CarCameras` and a `CinemachineBrain` for gameplay, intros,
  finish cinematics, replays, and photo mode.
- Gameplay code and effects repeatedly use `Camera.main` or
  `UIManager.Instance.PanelManager.mainCamera`. Cloned cameras must not retain
  the `MainCamera` tag, and the original needs to remain the authoritative
  anchor for game systems.
- The camera carries PostProcessing v2 and additional quality/effect logic,
  including temporal anti-aliasing options. The PostProcessing v2 source resets
  custom projection state around TAA and warns that built-in-pipeline partial
  camera viewports may produce visual artifacts on some platforms.

Therefore, three naive clones with `Camera.rect` thirds may work as a quick
experiment, but are not a robust final design. Full-size per-panel render
textures followed by a simple compositor give each post-process camera a normal
full target and isolate seams/effects.

Unity 2019.4 explicitly supports a custom `Camera.projectionMatrix` and provides
the standard off-center matrix formula. It also provides `Camera.rect`,
`Camera.targetDisplay`, `Display.displays`, and `Display.Activate`. These APIs
make both a single-wide compositor and experimental separate-display output
technically possible.

Sources: [Unity Camera.projectionMatrix 2019.4](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Camera-projectionMatrix.html),
[Camera.rect](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Camera-rect.html),
[Camera.targetDisplay](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Camera-targetDisplay.html),
[Display API](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Display.html), and
[Unity multi-display manual](https://docs.unity3d.com/2019.4/Documentation/Manual/MultiDisplay.html).

### Resolution, windows, and displays

The current game code has a single-display settings path. `SettingsManager`
reads `Screen.resolutions`, uses only `Display.main.systemWidth/systemHeight`,
and calls `Screen.SetResolution`. A full decompile search found no game call to
`Display.Activate` and no camera assignment to another `targetDisplay`.

Consequences:

1. NVIDIA Surround is the least invasive path because it makes three monitors
   appear as one large display. NVIDIA documents combined and bezel-corrected
   resolutions that are then visible to games.
2. A borderless window spanning ordinary extended desktops may avoid Surround,
   but reliable cross-monitor placement requires Windows window management that
   Unity's game settings do not expose. Treat it as a later experiment.
3. Unity separate-display mode requires the mod to activate displays, route
   cameras and UI, and validate window placement. Unity says activation cannot
   be undone during that run. Keep this opt-in and experimental.

Sources: [NVIDIA Surround configuration](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/NVIDIA_Surround_Configuration_.htm),
[NVIDIA display setup and bezel correction](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-gb/mergedProjects/nv3dENG/To_configure_my_displays_for_Surround.htm), and
[NVIDIA Surround viewing modes](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Surround_Display_Viewing.htm).

## Projection model

The reusable core treats each monitor as a physical rectangle and the viewer's
eyes as a common center of projection. For each panel it derives a camera basis,
projects the panel corners onto the near plane, and emits an asymmetric frustum.
This avoids the side stretch of one very wide frustum and remains correct when
panel angle or eye distance is not the one special symmetric arrangement.

Panel angle and eye distance are required. Resolution alone cannot determine
the correct image. Physical width/height are also required; diagonal plus aspect
ratio is only an estimate. Bezel width affects continuity or driver bezel
correction but not the visible image plane itself.

A 1500R panel is curved. Version 1 records the radius but approximates each
whole panel by one chord plane. Exact correction needs a mesh or shader that
maps the camera image to a cylindrical surface; three flat off-axis frusta alone
cannot remove within-panel curve error. This is a documented limitation, not a
reason to ignore the angle and distance inputs.

## Feasibility assessment

- **Surround ultrawide:** high confidence for output and resolution; low
  geometric fidelity at the side panels.
- **Three projections into one wide output:** feasible API and mod-loader path;
  medium implementation risk because of post-processing, TAA, global shader
  state, Cinemachine transitions, and UI.
- **Three independent Unity displays:** API exists; higher operational risk
  because the shipped game has no multi-display lifecycle or UI path.
- **Curved-panel-perfect projection:** possible only with a later distortion
  warp; not in the initial flat-panel core.

Nothing above establishes runtime visual correctness. The checked-in probe and
attended experiments exist to close that evidence gap.

## Surround refresh-rate investigation — 2026-09-25

The owner's NVIDIA setup screenshot shows the three GS32QCA panels grouped at
7680x1440 with **only 60 Hz offered**. A read-only driver check reported an RTX
5080 on 610.88. This proves the current configured group is limited to 60 Hz;
it does not establish a universal Surround limit or identify the exact cause.

NVIDIA's [Surround support guide](https://nvidia.custhelp.com/app/answers/detail/a_id/5335/kw/global%20settings)
explains that a Surround group uses the highest **common resolution and
refresh timing**, including pixel clock, across all selected displays. It
recommends identical displays on the same connection protocol and comparable
cables; adapters can alter reported timings. It also documents the custom
resolution icon next to the resolution dropdown, available after Surround is
enabled. The three selected monitors are the same model, but their individual
active timings and connector protocols have not yet been compared.

The [GS32QCA manufacturer manual](https://download.gigabyte.com/FileList/Manual/GIGABYTE_GS27QCA_GS32QCA_UM_English_20240823.pdf)
lists 2560x1440 at 120 Hz on all input columns, while the model's published
maximum is 180 Hz. Thus **120 Hz is a sensible first common-mode target**, not
a guaranteed Surround mode. NVIDIA's UI may still reject it if the three
panels report different detailed timings, port modes, or other constraints.

Safe diagnostic sequence after exiting games: disable Surround, inspect each
GS32QCA individually at 2560x1440 for available 120 Hz and higher modes,
connector type, color depth/HDR, and whether all three cables are the same
protocol. If all three expose 120 Hz, set them to the same supported mode and
re-open Surround's dropdown. Only then consider its documented custom-mode
dialog, with a known way back to 60 Hz. Do **not** blindly edit EDIDs, force a
high-rate custom timing, update the GPU driver, or change cable topology during
gameplay. Independent-display rendering may access each panel's higher native
rate, but loses NVIDIA Surround's documented inter-display synchronization;
it is not inherently a screen-tear cure.
