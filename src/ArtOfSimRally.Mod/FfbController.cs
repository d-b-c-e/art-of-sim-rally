using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Supplies the link art of rally never wrote: physics to steering force.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game's force feedback was built from both ends and never joined.
    /// <c>Wheel</c> computes real self-aligning torque, but only
    /// <c>if (cardynamics.enableForceFeedback)</c>, which nothing ever sets - so
    /// <c>Mz</c> is always zero. <c>ForceFeedback.Update()</c> is a complete
    /// consumer, but nothing ever attaches it, and the value it reads
    /// (<c>CarDynamics.forceFeedback</c>) is never assigned anywhere in the
    /// assembly. See docs/FORCE-FEEDBACK.md.
    /// </para>
    /// <para>
    /// So this class does three things: turns the aligning-torque model on,
    /// computes a force from the steered wheels, and drives the device.
    /// </para>
    /// <para>
    /// It deliberately does NOT reuse the game's <c>ForceFeedback.Update()</c>
    /// even though supplying <c>forceFeedback</c> would make it work. That method
    /// computes <c>(int)(forceFeedback * multiplier) * factor</c>, casting to int
    /// *before* scaling by 1000, which quantises the output to 21 discrete steps
    /// and feels notchy. We write <c>forceFeedback</c> anyway, so anything else
    /// reading it sees a sane value, but drive the device at full resolution.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CarDynamics), "FixedUpdate")]
    internal static class FfbController
    {
        // Their own constants imply the intended range: clampValue 20,
        // multiplier 0.5, factor 1000 -> +/-10000 == DI_FFNOMINALMAX.
        private const float GameForceFeedbackRange = 20f;

        private static float _smoothed;
        private static float _peakMz;
        private static float _peakSpeedKmh;
        private static float _nextDiagnostic;

        /// <summary>
        /// Runs before the physics step so the flag is set when the tyre model
        /// evaluates. Setting it in a postfix would compute Mz one frame late.
        /// </summary>
        [HarmonyPrefix]
        private static void EnableAligningTorque(CarDynamics __instance)
        {
            // Cheap enough to assert every frame, and robust against the game
            // resetting it on car change, stage restart or setup reload.
            if (!__instance.enableForceFeedback)
                __instance.enableForceFeedback = true;
        }

        [HarmonyPostfix]
        private static void DriveWheel(CarDynamics __instance)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.ForceFeedbackEnabled) return;
            if (!FfbNative.Ready) return;

            // FixedUpdate keeps running through the end-of-stage cutscene while
            // the game steers the car itself. Without this the wheel is dragged
            // to full lock and held there after crossing the line.
            if (!GameState.IsDriving)
            {
                if (_smoothed != 0f)
                {
                    _smoothed = 0f;
                    FfbNative.SetForce(0);
                }
                return;
            }

            var axles = __instance.axles;
            if (axles == null) return;

            // Steering force comes from the steered axle only. Rear-wheel Mz is
            // real but does not reach the steering column.
            var front = axles.frontAxle;
            if (front?.leftWheel == null || front.rightWheel == null) return;

            float mz = front.leftWheel.Mz + front.rightWheel.Mz;

            if (float.IsNaN(mz) || float.IsInfinity(mz)) return;

            // Normalise against a reference torque, then apply user gain. The
            // reference is configurable because the absolute magnitude of
            // CalcAligningForce is not documented anywhere and varies by car;
            // DiagnosticLogging prints the observed peak so it can be tuned
            // against real driving rather than guessed.
            float normalised = mz / Mathf.Max(1f, cfg.MzReference);
            normalised *= cfg.GainFromStrength;
            if (cfg.Invert) normalised = -normalised;
            normalised = Mathf.Clamp(normalised, -1f, 1f);

            // First-order smoothing. Raw per-step Mz is noisy over kerbs and
            // rocks, and an unfiltered signal reads as rattle rather than detail.
            float a = Mathf.Clamp01(cfg.Smoothing);
            _smoothed = Mathf.Lerp(normalised, _smoothed, a);

            // Publish in the game's own units so anything reading this field -
            // including the game's orphaned ForceFeedback component, if a future
            // version attaches it - sees a coherent value.
            __instance.forceFeedback = _smoothed * GameForceFeedbackRange;

            FfbNative.SetForce((int)(_smoothed * FfbNative.ForceMax));

            if (cfg.DiagnosticLogging) Diagnose(mz, __instance.velo * 3.6f);
        }

        // Reports the peak aligning torque seen in each window, which is the
        // number MzReference should be set near.
        private static void Diagnose(float mz, float speedKmh)
        {
            float abs = Mathf.Abs(mz);
            if (abs > _peakMz) _peakMz = abs;
            if (speedKmh > _peakSpeedKmh) _peakSpeedKmh = speedKmh;

            if (Time.unscaledTime < _nextDiagnostic) return;
            _nextDiagnostic = Time.unscaledTime + 5f;

            // Speed matters as much as torque here. A near-zero peak while parked
            // is expected; the same figure at 90 km/h means something is wrong,
            // and without speed the two readings are indistinguishable.
            ModLog.Info(
                $"FFB peak |Mz| {_peakMz:F1} over 5s at up to {_peakSpeedKmh:F0} km/h " +
                $"(reference={Main.Settings.MzReference:F0}, strength={Main.Settings.Strength}, " +
                $"output={_smoothed:F2})");
            _peakMz = 0f;
            _peakSpeedKmh = 0f;
        }

        /// <summary>Clears filter state between stages so a stale force is not carried over.</summary>
        public static void Reset()
        {
            _smoothed = 0f;
            _peakMz = 0f;
        }
    }
}
