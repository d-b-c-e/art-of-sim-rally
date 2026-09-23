# Experiment 003: Three-View Runtime Smoke Test

## Purpose

Determine whether the 0.2.0 experimental adapter can render three distinct
off-axis views into native-size render textures and composite them into one
wide Art of Rally output. A successful build or an `active` status is not a
visual pass by itself.

## Safe loader preflight on separate displays

1. Confirm the game is closed. Record the installed game executable and
   managed-assembly hashes, Unity version, UMM version, and active displays.
2. Build the solution and run the executable tests. Install only the five
   package files listed in `adapter-manifest.json` into the new
   `Mods/DbceTripleScreenArtOfRally` folder. Back up any prior copy first.
3. Export `desired-layout.json` through the optimizer from the saved rig
   profile. It stages identical bytes beside the installed adapter if needed.
   Do not enable the three-view or center-preview toggles.
4. Launch the game in the current display topology. Confirm the mod loads,
   menus remain stock, and `status.json` reports `inactive` / `FEATURE_DISABLED`
   with zero cameras. Quit the game. This does not test three-view rendering.

## Attended three-view pass on Surround

1. Confirm the game is closed. Switch to a non-bezel-corrected NVIDIA Surround
   profile at exactly 3 × native panel width and native panel height. Confirm
   Windows exposes one wide display before launching.
2. Export the same optimizer layout with `nvidia-surround` output and confirm
   the game itself is using that exact resolution. Select no AA, FXAA, or SMAA;
   TAA is explicitly unsupported by this prototype.
3. Open UMM settings and enable **experimental three-view rendering**. On a
   gameplay stage, confirm that left, center, and right panels show distinct
   directions, seams align at both joins, and `status.json` reports `active`,
   `activeCameraCount: 3`, a matching layout hash, and a recent successful
   frame. Capture a screenshot at the start line and while driving.
4. Pause/resume, cycle cameras, finish, replay, and open photo mode. Record
   whether the game falls back to stock rendering or safely restores it when
   the gameplay camera is no longer authoritative. Confirm HUD/menus remain
   usable, noting any stretching, missing elements, or overlays.
5. Disable the three-view toggle, disable the mod, and then quit/relaunch.
   Confirm stock rendering is restored after each transition. Test TAA only
   after turning the prototype back off; enabling it should report
   `degraded` / `TAA_UNSUPPORTED` rather than applying a projection.

## Evidence and gates

Record game build, installed mod file hashes, GPU/driver, topology, game
resolution, AA mode, relevant UMM and Player.log excerpts, runtime status,
screenshots, and approximate FPS/VRAM. A visual pass requires three distinct
correctly oriented views, no obvious seam discontinuity, usable HUD/menus,
safe restoration, and no repeated runtime errors. Replays, photo mode,
effects, and performance must be checked or explicitly marked degraded.

The compositor currently leaves the original game camera rendering in place
and replaces its image at `OnRenderImage`; that is deliberately reversible but
costs a fourth scene render until runtime evidence supports suppressing it.
The clones have no PostProcessLayer. No separate-display activation, window
resizing, bezel-corrected output, or curved-panel warp is attempted.

## 2026-09-23 preflight record

- Installed game reports Art of Rally `1.5.8b`, Unity `2019.4.38f1`, and
  Unity Mod Manager `0.33.0`; GPU is an RTX 5080 with driver `32.0.16.1088`.
- Windows exposed three separate `2560×1440` displays. The physical profile
  exported by the optimizer was three 32-inch 1500R panels at 60° side yaw,
  660 mm eye distance, and an 8 mm bezel gap.
- Release build passed with zero warnings; 34 offline geometry/protocol
  assertions passed. The first default-off loader pass loaded all three UMM
  mods successfully and left stock rendering active.
- The game process could write `status.json` but could not see the optimizer's
  newly exported per-user `desired-layout.json` (a direct `File.OpenRead`
  returned `FileNotFoundException`). The identical staged layout fallback in
  the owned mod folder was then implemented, tested offline, and deployed.
  That fallback has **not yet been verified in the game**.
- Direct executable launches without a fresh Steam handoff eventually failed
  with `Steamworks is not initialized`, unrelated to projection. A proper
  Steam launch then displayed a conflict warning that another computer was
  playing on the same account. The launch was canceled; no other session was
  disconnected. Reattempt only when Steam is free.

The next runtime action is the safe default-off loader pass with the staged
layout present. It must show `inactive` / `FEATURE_DISABLED` and an accepted
layout hash matching the optimizer export before moving to Surround or
enabling three-view rendering.
