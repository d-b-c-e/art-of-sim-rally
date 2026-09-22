using System;

namespace Dbce.TripleScreen;

/// <summary>Measurements for three identical panels hinged at the center panel edges.</summary>
public sealed class TripleRigDefinition
{
    public TripleRigDefinition(
        double panelWidthMm,
        double panelHeightMm,
        double eyeDistanceMm,
        double sideAngleDegrees)
        : this(
            panelWidthMm,
            panelHeightMm,
            eyeDistanceMm,
            sideAngleDegrees,
            sideAngleDegrees,
            0d)
    {
    }

    public TripleRigDefinition(
        double panelWidthMm,
        double panelHeightMm,
        double eyeDistanceMm,
        double leftAngleDegrees,
        double rightAngleDegrees,
        double eyeHeightAbovePanelCenterMm = 0d)
    {
        if (!(panelWidthMm > 0d)) throw new ArgumentOutOfRangeException(nameof(panelWidthMm));
        if (!(panelHeightMm > 0d)) throw new ArgumentOutOfRangeException(nameof(panelHeightMm));
        if (!(eyeDistanceMm > 0d)) throw new ArgumentOutOfRangeException(nameof(eyeDistanceMm));
        if (leftAngleDegrees < 0d || leftAngleDegrees >= 90d) throw new ArgumentOutOfRangeException(nameof(leftAngleDegrees));
        if (rightAngleDegrees < 0d || rightAngleDegrees >= 90d) throw new ArgumentOutOfRangeException(nameof(rightAngleDegrees));

        PanelWidthMm = panelWidthMm;
        PanelHeightMm = panelHeightMm;
        EyeDistanceMm = eyeDistanceMm;
        LeftAngleDegrees = leftAngleDegrees;
        RightAngleDegrees = rightAngleDegrees;
        EyeHeightAbovePanelCenterMm = eyeHeightAbovePanelCenterMm;
    }

    public double PanelWidthMm { get; }
    public double PanelHeightMm { get; }
    public double EyeDistanceMm { get; }
    public double LeftAngleDegrees { get; }
    public double RightAngleDegrees { get; }
    public double EyeHeightAbovePanelCenterMm { get; }
}
