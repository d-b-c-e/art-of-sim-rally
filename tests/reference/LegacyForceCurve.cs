// Frozen 0.2.2 formula for portable replay. Independent of current toolkit code.
// Consumer regression also compares this with the actual game Mathf helpers.
namespace ArtOfSimRally.Testing
{
    internal static class LegacyForceCurve
    {
        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Evaluate(float fy, float slip, float ideal, float speed, float reference, float gain, bool invert, float smoothing, float previous)
        {
            float trail = Lerp(1f, .6f, Clamp01(slip / (2f * System.Math.Max(1f, ideal))));
            float n = fy * trail / System.Math.Max(1f, reference);
            n *= gain;
            float t = Clamp01((speed - 3f) / 9f);
            n *= -2f * t * t * t + 3f * t * t;
            if (invert) n = -n;
            n = n < -1f ? -1f : n > 1f ? 1f : n;
            return Lerp(n, previous, Clamp01(smoothing));
        }
    }
}
