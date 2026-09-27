using System;

namespace ArtOfRally.TripleScreen.Mod;

// Resolve a desired center vertical FOV into the same shared virtual eye
// distance used by all three off-axis projections. Never tune only one view.
internal static class FovPreference
{
    internal const double MinimumSliderScale = 0.75d;
    internal const double MaximumSliderScale = 2d;
    private const double MinimumGameScale = 0.1d;
    private const double MaximumGameScale = 10d;

    internal static double VerticalDegrees(double panelHeightMm, double eyeDistanceMm,
        double eyeHeightMm, double scale)
    {
        if (!Finite(panelHeightMm) || panelHeightMm <= 0d ||
            !Finite(eyeDistanceMm) || eyeDistanceMm <= 0d ||
            !Finite(eyeHeightMm) || !Finite(scale) || scale <= 0d)
            throw new ArgumentOutOfRangeException(nameof(scale), "FOV geometry must be finite and positive.");

        var distance = eyeDistanceMm / scale;
        var halfHeight = panelHeightMm / 2d;
        return (Math.Atan((halfHeight - eyeHeightMm) / distance) -
            Math.Atan((-halfHeight - eyeHeightMm) / distance)) * 180d / Math.PI;
    }

    internal static double ScaleForSliderDegrees(double panelHeightMm, double eyeDistanceMm,
        double eyeHeightMm, double targetDegrees) =>
        ScaleForDegrees(panelHeightMm, eyeDistanceMm, eyeHeightMm, targetDegrees,
            MinimumSliderScale, MaximumSliderScale);

    internal static double ScaleForGameDegrees(double panelHeightMm, double eyeDistanceMm,
        double eyeHeightMm, double targetDegrees) =>
        ScaleForDegrees(panelHeightMm, eyeDistanceMm, eyeHeightMm, targetDegrees,
            MinimumGameScale, MaximumGameScale);

    private static double ScaleForDegrees(double panelHeightMm, double eyeDistanceMm,
        double eyeHeightMm, double targetDegrees, double minimumScale, double maximumScale)
    {
        if (!Finite(targetDegrees) || targetDegrees <= 0d || targetDegrees >= 180d)
            throw new ArgumentOutOfRangeException(nameof(targetDegrees), "FOV must be between 0° and 180°.");
        if (Math.Abs(eyeHeightMm) >= panelHeightMm / 2d)
            throw new ArgumentOutOfRangeException(nameof(eyeHeightMm),
                "FOV matching requires the eye to be within the panel's vertical extent.");

        var minimum = VerticalDegrees(panelHeightMm, eyeDistanceMm, eyeHeightMm, minimumScale);
        var maximum = VerticalDegrees(panelHeightMm, eyeDistanceMm, eyeHeightMm, maximumScale);
        if (targetDegrees <= minimum) return minimumScale;
        if (targetDegrees >= maximum) return maximumScale;

        var low = minimumScale;
        var high = maximumScale;
        for (var index = 0; index < 48; index++)
        {
            var mid = (low + high) / 2d;
            if (VerticalDegrees(panelHeightMm, eyeDistanceMm, eyeHeightMm, mid) < targetDegrees)
                low = mid;
            else
                high = mid;
        }
        return (low + high) / 2d;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
