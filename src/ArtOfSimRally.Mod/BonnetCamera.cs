using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Adds a bonnet camera to the game's existing view rotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It behaves like any other view: press the change-view button and it appears
    /// in the cycle after the eight stock angles, and the choice persists. That
    /// works because <c>CarCameras.SetCameraFromSave</c> wraps on
    /// <c>CameraAnglesList.Count</c> rather than on a hard-coded 8, so appending an
    /// entry in a <c>Start</c> postfix is enough to join the rotation - no patching
    /// of the cycling logic at all.
    /// </para>
    /// <para>
    /// The stock rig cannot produce this view by parameters alone. It always places
    /// the camera at a distance and calls <c>LookAt</c> on the car, so a zero
    /// distance would have the camera looking at itself. Instead, when our entry is
    /// the active one, a <c>LateUpdate</c> postfix takes the camera over completely
    /// and mounts it to the car.
    /// </para>
    /// <para>
    /// Deliberately rigid: no position or rotation damping. The stock damping exists
    /// to smooth a distant chase view; on a mounted camera it reads as the car
    /// sliding around underneath a floating viewpoint. Inheriting body roll and
    /// pitch directly is the entire point - it is what makes suspension and camber
    /// legible from inside the car.
    /// </para>
    /// <para>
    /// This is a bonnet camera, not a cockpit camera. art of rally's cars have no
    /// modelled interiors, so there is nothing to sit inside of. See docs/CAMERA.md.
    /// </para>
    /// </remarks>
    internal static class BonnetCamera
    {
        // Identified by reference rather than by CameraAngle.CameraAngles, whose
        // enum only defines CAMERA1..CAMERA8. Inventing a ninth value would mean
        // casting an out-of-range int and hoping nothing switches on it.
        private static CameraAngle _bonnetAngle;

        private static float _lateralOffset;

        // CameraAnglesList and cardynamics are private on CarCameras. AccessTools
        // resolves them once at type-init rather than reflecting per frame.
        private static readonly AccessTools.FieldRef<CarCameras, List<CameraAngle>> AnglesList =
            AccessTools.FieldRefAccess<CarCameras, List<CameraAngle>>("CameraAnglesList");

        private static readonly AccessTools.FieldRef<CarCameras, CarDynamics> Dynamics =
            AccessTools.FieldRefAccess<CarCameras, CarDynamics>("cardynamics");

        /// <summary>True when the player has cycled to the bonnet view.</summary>
        private static bool IsActive(CarCameras cameras)
            => _bonnetAngle != null && ReferenceEquals(cameras.CurrentCameraAngle, _bonnetAngle);

        [HarmonyPatch(typeof(CarCameras), "Start")]
        internal static class AddToRotation
        {
            [HarmonyPostfix]
            private static void Append(CarCameras __instance)
            {
                var cfg = Plugin.Settings;
                if (cfg == null || !cfg.BonnetCameraEnabled.Value) return;
                var list = AnglesList(__instance);
                if (list == null) return;

                // Start runs per car; only ever contribute one entry.
                if (_bonnetAngle != null && list.Contains(_bonnetAngle))
                    return;

                // distance 0 keeps the stock rig from doing anything useful or
                // harmful before our LateUpdate takes over. The CameraAngles tag
                // is cosmetic here; CAMERA1 is reused simply because the value is
                // never compared against ours.
                _bonnetAngle = new CameraAngle(0f, 0f, 0f, CameraAngle.CameraAngles.CAMERA1);
                list.Add(_bonnetAngle);

                Plugin.Log.LogInfo(
                    $"Bonnet camera added as view {list.Count} of {list.Count} in the rotation.");
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
                var cfg = Plugin.Settings;
                if (cfg == null || !cfg.BonnetCameraEnabled.Value) return;
                if (!IsActive(__instance)) { _lateralOffset = 0f; return; }

                // Hand the camera back for the end-of-stage cinematic, replays and
                // the intro. The game directs its own shots there, and continuing
                // to mount the camera to the car puts the view underground.
                if (!GameState.IsPlayerView) { _lateralOffset = 0f; return; }

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
                if (cfg.BonnetLean.Value > 0f)
                {
                    float lateral = Mathf.Clamp(GetLateralLoad(__instance), -1f, 1f);
                    _lateralOffset = Mathf.Lerp(_lateralOffset, lateral, 0.1f);
                    lean = _lateralOffset * cfg.BonnetLean.Value;
                }

                var offset = new Vector3(
                    cfg.BonnetSide.Value + lean,
                    cfg.BonnetHeight.Value,
                    cfg.BonnetForward.Value);

                cam.transform.position = target.position + rot * offset;
                cam.transform.rotation = rot * Quaternion.Euler(cfg.BonnetPitch.Value, 0f, 0f);

                // The stock UpdateFOVAndPitch rewrites fieldOfView every frame for
                // the chase camera, so set ours after it rather than once.
                cam.fieldOfView = cfg.BonnetFOV.Value;

                // Polled here so the hotkeys are live only while looking through
                // this camera, and never while driving a stock view or in a menu.
                CameraTuner.Update();
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
