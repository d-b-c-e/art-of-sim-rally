using Dbce.Wheel.Ffb;

namespace ArtOfSimRally.Mod
{
    // Game-facing name retained for callers and vector tooling. The implementation
    // lives in the pinned toolkit; CompatibilityVersion 1 preserves the released tune.
    internal static class ForceCurve
    {
        public const int CompatibilityVersion = AxleForceCurve.CompatibilityVersion;
        public const float FadeStartKmh = AxleForceCurve.FadeStartKmh;
        public const float FadeFullKmh = AxleForceCurve.FadeFullKmh;
        public const float LimitTrail = AxleForceCurve.LimitTrail;
        public static float Clamp01(float value) => AxleForceCurve.Clamp01(value);
        public static float Lerp(float a, float b, float t) => AxleForceCurve.Lerp(a, b, t);
        public static float SmoothStep01(float value) => AxleForceCurve.SmoothStep01(value);
        public static float Trail(float slip, float ideal) => AxleForceCurve.Trail(slip, ideal);
        public static float Normalised(float fy, float slip, float ideal, float speed, float reference, float gain, bool invert)
            => AxleForceCurve.Normalised(fy, slip, ideal, speed, reference, gain, invert);
        public static float Smooth(float previous, float target, float smoothing)
            => AxleForceCurve.Smooth(previous, target, smoothing);
    }
}
