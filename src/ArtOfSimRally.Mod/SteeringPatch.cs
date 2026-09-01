using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Gives a wheel the game's own direct-steering path, which it otherwise only
    /// grants to controllers Rewired recognises as racing wheels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CarController.SmoothSteer()</c> already contains the behaviour we want:
    /// </para>
    /// <code>
    /// bool flag = ... GetTemplate&lt;IRacingWheelTemplate&gt;() != null;
    /// float num  = steerCorrectionFactor;
    /// float num2 = steerTime;   // etc
    /// if (flag) { num = 1f; num2 = num3 = num4 = num5 = 0f; }
    /// </code>
    /// <para>
    /// A wheel Rewired does not recognise - anything newer than the shipped
    /// hardware database, e.g. a MOZA R12, which reports
    /// <c>Is Recognized: No</c> - has no racing wheel template, so <c>flag</c>
    /// stays false and the gamepad smoothing filter is applied to a 900-degree
    /// wheel. The steering rate is <c>1 / (steerTime + veloSteerTime * velo)</c>,
    /// which at ~30 m/s works out near 1.6 seconds lock to lock. That reads as a
    /// huge deadzone followed by a sudden snap of oversteer.
    /// </para>
    /// <para>
    /// The fix needs no transpiler. <c>SmoothSteer</c> copies those fields into
    /// locals and only overwrites them when <c>flag</c> is set, so zeroing the
    /// fields themselves produces an identical result through the untouched code
    /// path. We set them once when the car spawns.
    /// </para>
    /// <para>
    /// This is not a physics change and not an advantage: it is exactly what a
    /// player with a recognised G29 already gets. See <c>steerAssistance</c>
    /// below for the setting that genuinely does cross that line.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CarController), "Start")]
    internal static class SteeringPatch
    {
        [HarmonyPostfix]
        private static void ApplyDirectSteering(CarController __instance)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null) return;

            // Rewired is guaranteed initialised by the time a car spawns, which
            // is not true when the mod first loads.
            WheelCalibration.Apply();

            if (!cfg.DirectSteering) return;

            // Mirrors the `if (flag)` branch in SmoothSteer exactly.
            __instance.steerTime             = 0f;
            __instance.steerReleaseTime      = 0f;
            __instance.veloSteerTime         = 0f;
            __instance.veloSteerReleaseTime  = 0f;
            __instance.steerCorrectionFactor = 1f;

            // SteerAssistance() clamps steering authority by lateral slip
            // (maxSteer = 1 - |average lateralSlip|), so the more the car slides
            // the less steering the driver is allowed. Recognised wheels do NOT
            // escape this - it is a genuine driving aid, not a device fix, and
            // turning it off changes how the car behaves. Off by default and
            // clearly labelled, because art of rally has online leaderboards.
            if (cfg.DisableSteerAssist)
                __instance.steerAssistance = false;

            ModLog.Info(
                $"Direct steering applied to {__instance.GetType().Name} " +
                $"(steerAssistance={__instance.steerAssistance})");
        }
    }
}
