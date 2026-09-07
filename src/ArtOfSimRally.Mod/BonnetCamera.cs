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
        private static Camera _ownedCamera;
        private static CarCameras _ownedRig;
        private static float _savedFov;

        // Release the actual child we changed, even if the game's current camera
        // has since changed or CarCameras has stopped receiving LateUpdate.
        internal static void Release(bool snapParent)
        {
            var camera = _ownedCamera;
            var rig = _ownedRig;
            _ownedCamera = null;
            _ownedRig = null;
            _lateralOffset = 0f;
            if (camera == null) return;
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            camera.fieldOfView = _savedFov;
            if (snapParent && rig != null && rig.enabled && GameState.IsPlayerView)
            {
                try
                {
                    // Disabling the mod/view while mounted must not leave the
                    // stock rig using our zero-distance placeholder angle.
                    SelectStockView(rig);
                    rig.SetToWantedPositionImmediate();
                }
                catch { /* The child invariant is restored even during scene teardown. */ }
            }
        }

        private static void SelectStockView(CarCameras rig)
        {
            if (ActiveView(rig) == View.None) return;
            var list = AnglesList(rig);
            if (list == null || !list.Exists(a => !ReferenceEquals(a, _bonnetAngle) && !ReferenceEquals(a, _bumperAngle))) return;
            var original = list.ToArray();
            try
            {
                list.RemoveAll(a => ReferenceEquals(a, _bonnetAngle) || ReferenceEquals(a, _bumperAngle));
                // The game's method clamps the saved preset to the temporary
                // stock list and updates its private distance/height/pitch too.
                rig.RefreshCameraType();
            }
            finally { list.Clear(); list.AddRange(original); }
        }

        internal static void ReleaseIfInactive()
        {
            if (_ownedCamera == null) return;
            var cfg = Main.Settings;
            var view = _ownedRig == null ? View.None : ActiveView(_ownedRig);
            if (!Main.Enabled || cfg == null || !GameState.IsPlayerView ||
                _ownedRig == null || !_ownedRig.enabled || view == View.None ||
                (view == View.Bonnet && !cfg.BonnetCameraEnabled) ||
                (view == View.Bumper && !cfg.BumperCameraEnabled)) Release(true);
        }

        // CameraManager disables CarCameras before cinematic/replay rendering.
        // Its LateUpdate postfix alone cannot perform that handback.
        [HarmonyPatch(typeof(CameraManager), "EnableCinemachineCamera")]
        internal static class CinematicHandback
        {
            [HarmonyPrefix] private static void Before() => Release(false);
        }

        [HarmonyPatch(typeof(CameraManager), "DisableCameraManagers")]
        internal static class IntroHandback
        {
            [HarmonyPrefix] private static void Before() => Release(false);
        }

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
                bool shouldDrive = Main.Enabled && GameState.IsPlayerView &&
                    ((view == View.Bonnet && cfg.BonnetCameraEnabled) ||
                     (view == View.Bumper && cfg.BumperCameraEnabled));

                // Hand the camera back cleanly for the end-of-stage cinematic,
                // replays, the intro, and - the one that bit - the player simply
                // cycling on to a stock camera angle.
                //
                // Two things have to be undone, because the rig is two objects.
                // CarCameras lives on "Stage Camera" and drives only that; the
                // camera that renders is "Camera Main", ITS CHILD, which the game
                // pins at local identity (CameraManager's constructor zeroes the
                // child's localPosition and localRotation - it is the invariant
                // stated out loud). We mount by writing world-space position and
                // rotation to Camera.main, i.e. to the child, which Unity stores as
                // a local offset from the parent.
                //
                //  - The parent damps toward its target from wherever it currently
                //    is, so releasing it mid-corner swept it out through the
                //    bodywork to the chase position - the "goes berserk" after the
                //    finish line. SetToWantedPositionImmediate places it in one step.
                //  - The child keeps our offset forever unless we clear it. Relative
                //    to a parent 30-46 m behind and 15-45 m above the car looking
                //    back down at it, a bonnet mount is close to a 180 degree yaw -
                //    so every stock angle rendered backwards for as long as the
                //    stage lasted. This is a code defect; issue #1's reporter
                //    separately resolved their symptom by unplugging a PS5 pad.
                //
                // Clear the child first so the parent's placement is the last word.
                if (!shouldDrive)
                {
                    Release(true);
                    if (GameState.IsPlayerView && view != View.None) SelectStockView(__instance);
                    return;
                }

                var target = __instance.target;
                if (target == null) return;

                var cam = UIManager.Instance?.PanelManager?.mainCamera;
                if (cam == null) return;
                if (_ownedCamera != cam || _ownedRig != __instance)
                {
                    Release(false);
                    _ownedCamera = cam;
                    _ownedRig = __instance;
                    _savedFov = cam.fieldOfView;
                }

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
