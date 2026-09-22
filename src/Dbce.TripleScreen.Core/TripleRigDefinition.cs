using System;

namespace Dbce.TripleScreen;

/// <summary>Measurements for three identical panels hinged at the center panel edges.</summary>
public sealed class TripleRigDefinition
{
    public TripleRigDefinition(
        double panelWidthMm,
        double panelHeightMm,
        double eyeDistanceMm,
        double sideAngleDegrees,
        double eyeHeightAbovePanelCenterMm = 0d)
    {
        if (!(panelWidthMm > 0d)) throw new ArgumentOutOfRangeException(nameof(panelWidthMm));
        if (!(panelHeightMm > 0d)) throw new ArgumentOutOfRangeException(nameof(panelHeightMm));
        if (!(eyeDistanceMm > 0d)) throw new ArgumentOutOfRangeException(nameof(eyeDistanceMm));
        if (sideAngleDegrees < 0d || sideAngleDegrees >= 90d) throw new ArgumentOutOfRangeException(nameof(sideAngleDegrees));

        PanelWidthMm = panelWidthMm;
        PanelHeightMm = panelHeightMm;
        EyeDistanceMm = eyeDistanceMm;
        SideAngleDegrees = sideAngleDegrees;
        EyeHeightAbovePanelCenterMm = eyeHeightAbovePanelCenterMm;
    }

    public double PanelWidthMm { get; }
    public double PanelHeightMm { get; }
    public double EyeDistanceMm { get; }
    public double SideAngleDegrees { get; }
    public double EyeHeightAbovePanelCenterMm { get; }
}
