using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Adds mounted views - bonnet, then bumper - to the game's existing view rotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// They behave like any other view: press the change-view button and they
    /// appear in the cycle after the eight stock angles, and the choice persists.
    /// That works because <c>CarCameras.SetCameraFromSave</c> wraps on
    /// <c>CameraAnglesList.Count</c> rather than on a hard-coded 8, so appending
    /// entries in a <c>Start</c> postfix is enough to join the rotation - no
    /// patching of the cycling logic at all.
    /// </para>
    /// <para>
    /// The stock rig cannot produce these views by parameters alone. It always
    /// places the camera at a distance and calls <c>LookAt</c> on the car, so a zero
    /// distance would have the camera looking at itself. Instead, when one of our
    /// entries is the active one, a <c>LateUpdate</c> postfix takes the camera over
    /// completely and mounts it to the car.
    /// </para>
    /// <para>
    /// Deliberately rigid: no position or rotation damping. The stock damping exists
    /// to smooth a distant chase view; on a mounted camera it reads as the car
    /// sliding around underneath a floating viewpoint. Inheriting body roll and
    /// pitch directly is the entire point - it is what makes suspension and camber
    /// legible from inside the car.
    /// </para>
    /// <para>
    /// Bonnet and bumper, not cockpit. art of rally's cars have no modelled
    /// interiors, so there is nothing to sit inside of. See docs/CAMERA.md.
    /// </para>
    /// </remarks>
    internal static class BonnetCamera
    {
        /// <summary>Which mounted view, if any, the player has cycled to.</summary>
        internal enum View { None, Bonnet, Bumper }

        // Identified by reference rather than by CameraAngle.CameraAngles, whose
        // enum only defines CAMERA1..CAMERA8. Inventing new values would mean
        // casting out-of-range ints and hoping nothing switches on them.
        private static CameraAngle _bonnetAngle;
        private static CameraAngle _bumperAngle;

        private static float _lateralOffset;

        // Tracks the moment we stop controlling, so the handback runs once.
        private static bool _wasDriving;

        // CameraAnglesList and cardynamics are private on CarCameras. AccessTools
        // resolves them once at type-init rather than reflecting per frame.
        private static readonly AccessTools.FieldRef<CarCameras, List<CameraAngle>> AnglesList =
            AccessTools.FieldRefAccess<CarCameras, List<CameraAngle>>("CameraAnglesList");

        private static readonly AccessTools.FieldRef<CarCameras, CarDynamics> Dynamics =
            AccessTools.FieldRefAccess<CarCameras, CarDynamics>("cardynamics");

        internal static View ActiveView(CarCameras cameras)
        {
            var current = cameras.CurrentCameraAngle;
            if (_bonnetAngle != null && ReferenceEquals(current, _bonnetAngle)) return View.Bonnet;
            if (_bumperAngle != null && ReferenceEquals(current, _bumperAngle)) return View.Bumper;
            return View.None;
        }

        [HarmonyPatch(typeof(CarCameras), "Start")]
        internal static class AddToRotation
        {
            [HarmonyPostfix]
            private static void Append(CarCameras __instance)
            {
                var cfg = Main.Settings;
                if (cfg == null) return;
                var list = AnglesList(__instance);
                if (list == null) return;

                // Start runs per car; only ever contribute one entry per view.
                // distance 0 keeps the stock rig from doing anything useful or
                // harmful before our LateUpdate takes over. The CameraAngles tag
                // is cosmetic here; CAMERA1 is reused because the value is never
                // compared against ours.
                if (cfg.BonnetCameraEnabled && (_bonnetAngle == null || !list.Contains(_bonnetAngle)))
                {
                    _bonnetAngle = new CameraAngle(0f, 0f, 0f, CameraAngle.CameraAngles.CAMERA1);
                    list.Add(_bonnetAngle);
                    ModLog.Info($"Bonnet camera added as view {list.Count} in the rotation.");
                }
                if (cfg.BumperCameraEnabled && (_bumperAngle == null || !list.Contains(_bumperAngle)))
                {
                    _bumperAngle = new CameraAngle(0f, 0f, 0f, CameraAngle.CameraAngles.CAMERA1);
                    list.Add(_bumperAngle);
                    ModLog.Info($"Bumper camera added as view {list.Count} in the rotation.");
                }
            }
        }

        [HarmonyPatch(typeof(CarCameras), "LateUpdate")]
        internal static class DriveCamera
        {
            // Runs after the stock rig has positioned itself, so whatever it did
            // this frame is simply overwritten before rendering.
            [HarmonyPostfix]
            private static void Mount(CarCameras __instance)
            {
                var cfg = Main.Settings;
                if (cfg == null) return;
                var view = ActiveView(__instance);
                bool shouldDrive = view != View.None && GameState.IsPlayerView;

                // Hand the camera back cleanly for the end-of-stage cinematic,
                // replays and the intro.
                //
                // Simply stopping is not enough. The stock rig damps toward its
                // target from wherever the camera currently is, and we leave it
                // mounted inside the car - so it swings out through the bodywork to
                // the chase position, which is the "goes berserk" people see after
                // the finish line. The game has its own method for putting the
                // camera where it belongs in one step; call that as we let go.
                if (!shouldDrive)
                {
                    _lateralOffset = 0f;
                    if (_wasDriving)
                    {
                        _wasDriving = false;
                        try { __instance.SetToWantedPositionImmediate(); }
                        catch { /* handing back is best-effort */ }
                    }
                    return;
                }
                _wasDriving = true;

                var target = __instance.target;
                if (target == null) return;

                var cam = UIManager.Instance?.PanelManager?.mainCamera;
                if (cam == null) return;

                // Mount in the car's own frame, so body roll and pitch come along.
                var rot = target.rotation;

                // A small lateral lean under cornering load is what sells a mounted
                // camera, but it is also the first thing that makes people queasy,
                // so it is configurable down to zero. Smoothed because raw lateral
                // acceleration is noisy on gravel.
                float lean = 0f;
                if (cfg.BonnetLean > 0f)
                {
                    float lateral = Mathf.Clamp(GetLateralLoad(__instance), -1f, 1f);
                    _lateralOffset = Mathf.Lerp(_lateralOffset, lateral, 0.1f);
                    lean = _lateralOffset * cfg.BonnetLean;
                }

                bool bumper = view == View.Bumper;
                var offset = new Vector3(
                    (bumper ? cfg.BumperSide : cfg.BonnetSide) + lean,
                    bumper ? cfg.BumperHeight : cfg.BonnetHeight,
                    bumper ? cfg.BumperForward : cfg.BonnetForward);

                cam.transform.position = target.position + rot * offset;
                cam.transform.rotation = rot * Quaternion.Euler(bumper ? cfg.BumperPitch : cfg.BonnetPitch, 0f, 0f);

                // The stock UpdateFOVAndPitch rewrites fieldOfView every frame for
                // the chase camera, so set ours after it rather than once.
                cam.fieldOfView = bumper ? cfg.BumperFOV : cfg.BonnetFOV;

                // Polled here so the hotkeys are live only while looking through
                // one of our views, and never while driving a stock view or in a menu.
                CameraTuner.Update(view);
            }

            // Average lateral slip across the wheels, which the game already
            // computes for its steering assist, as a cheap cornering-load proxy.
            private static float GetLateralLoad(CarCameras cameras)
            {
                var cd = Dynamics(cameras);
                var axles = cd?.axles;
                var front = axles?.frontAxle;
                if (front?.leftWheel == null || front.rightWheel == null) return 0f;
                return (front.leftWheel.slipAngle + front.rightWheel.slipAngle) / 90f;
            }
        }
    }
}
