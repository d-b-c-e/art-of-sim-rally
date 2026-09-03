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
        private const float FadeStartKmh = 3f;
        private const float FadeFullKmh = 12f;
        // Trail at twice the ideal slip angle, as a fraction of the straight-ahead trail.
        private const float LimitTrail = 0.6f;

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

            // Steering torque is the lateral force on the steered axle acting
            // through a pneumatic trail - NOT the game's Mz. Mz is a 1989
            // Pacejka aligning torque, and that curve reverses sign at about
            // 7-8 degrees of slip. This game's front tyres run at 12-29 degrees
            // in ordinary corners (measured 2026-09-02), so raw Mz flips from
            // centring to pushing outward mid-corner, on every corner. Fy is
            // large, follows the steering 98% of the time and saturates without
            // ever reversing; scaling it by a trail that shrinks toward the limit
            // keeps the "lightening" cue without the reversal.
            var lw = front.leftWheel; var rw = front.rightWheel;
            float fy = lw.Fy + rw.Fy;
            if (float.IsNaN(fy) || float.IsInfinity(fy)) return;

            float absSlip = 0.5f * (Mathf.Abs(lw.slipAngle) + Mathf.Abs(rw.slipAngle));
            float ideal = Mathf.Max(1f, lw.idealSlipAngle);
            float trail = Mathf.Lerp(1f, LimitTrail, Mathf.Clamp01(absSlip / (2f * ideal)));

            // Sign set at the wheel, not derived: on a MOZA R12 (DirectInput
            // constant force on X) +Fy is the one that centres - a left turn
            // pushes the wheel right. The mirrored default pulled toward lock
            // on every corner (2026-09-02). Invert covers devices that read the
            // axis the other way.
            float normalised = fy * trail / Mathf.Max(1f, cfg.FyReference);
            normalised *= cfg.GainFromStrength;

            // Low-speed fade. The aligning-torque model is a 1989 Pacejka curve,
            // which peaks at a few degrees of slip and then falls through zero
            // and reverses. At walking pace the slip angle is
            // -atan(lateral / forward velocity) with a tiny denominator, so any
            // steering angle lands past the peak and the wheel is pushed *into*
            // the turn, on both sides of centre - "there is no centre". Real
            // cars have no aligning torque at parking speed either; every sim
            // fades it out below roughly 10 km/h.
            float speedKmh = __instance.velo * 3.6f;
            normalised *= Mathf.SmoothStep(0f, 1f, (speedKmh - FadeStartKmh) / (FadeFullKmh - FadeStartKmh));

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

            if (cfg.DiagnosticLogging)
            {
                Diagnose(fy * trail, speedKmh);
                Trace(front.leftWheel, front.rightWheel, speedKmh);
            }
        }

        // Five lines a second of the quantities the force is built from, so the
        // sign relation between Mz and Fy - which depends on each car's tyre
        // coefficients - can be read off a real drive instead of assumed.
        private static float _nextTrace;
        private static void Trace(Wheel l, Wheel r, float speedKmh)
        {
            if (Time.unscaledTime < _nextTrace) return;
            _nextTrace = Time.unscaledTime + 0.2f;
            ModLog.Info(
                $"FFB trace v={speedKmh:F1} steer={l.steering:F2} " +
                $"slipL={l.slipAngle:F1} slipR={r.slipAngle:F1} ideal={l.idealSlipAngle:F1} " +
                $"FyL={l.Fy:F0} FyR={r.Fy:F0} MzL={l.Mz:F1} MzR={r.Mz:F1} out={_smoothed:F2}");
        }

        // Reports the peak steering force seen in each window, which is the
        // number FyReference should be set near.
        private static void Diagnose(float force, float speedKmh)
        {
            float abs = Mathf.Abs(force);
            if (abs > _peakMz) _peakMz = abs;
            if (speedKmh > _peakSpeedKmh) _peakSpeedKmh = speedKmh;

            if (Time.unscaledTime < _nextDiagnostic) return;
            _nextDiagnostic = Time.unscaledTime + 5f;

            // Speed matters as much as torque here. A near-zero peak while parked
            // is expected; the same figure at 90 km/h means something is wrong,
            // and without speed the two readings are indistinguishable.
            ModLog.Info(
                $"FFB peak |Fy x trail| {_peakMz:F0} over 5s at up to {_peakSpeedKmh:F0} km/h " +
                $"(reference={Main.Settings.FyReference:F0}, strength={Main.Settings.Strength}, " +
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
