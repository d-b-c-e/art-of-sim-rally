using System;
using System.Collections.Generic;

namespace Dbce.TripleScreen;

public static class TripleRigBuilder
{
    public static IReadOnlyList<DisplaySurface> Build(TripleRigDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));

        var width = definition.PanelWidthMm;
        var height = definition.PanelHeightMm;
        var centerY = -definition.EyeHeightAbovePanelCenterMm;
        var centerZ = -definition.EyeDistanceMm;
        var up = new Vector3d(0d, 1d, 0d);
        var centerRight = new Vector3d(1d, 0d, 0d);
        var center = CreateSurface("center", new Vector3d(0d, centerY, centerZ), centerRight, up, width, height);

        var radians = definition.SideAngleDegrees * Math.PI / 180d;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);

        var leftRight = new Vector3d(cosine, 0d, -sine);
        var leftHinge = new Vector3d(-width / 2d, centerY, centerZ);
        var leftCenter = leftHinge - (leftRight * (width / 2d));
        var left = CreateSurface("left", leftCenter, leftRight, up, width, height);

        var rightRight = new Vector3d(cosine, 0d, sine);
        var rightHinge = new Vector3d(width / 2d, centerY, centerZ);
        var rightCenter = rightHinge + (rightRight * (width / 2d));
        var right = CreateSurface("right", rightCenter, rightRight, up, width, height);

        return new[] { left, center, right };
    }

    private static DisplaySurface CreateSurface(
        string id,
        Vector3d center,
        Vector3d right,
        Vector3d up,
        double width,
        double height)
    {
        var halfRight = right * (width / 2d);
        var halfUp = up * (height / 2d);
        return new DisplaySurface(id, center - halfRight - halfUp, center + halfRight - halfUp, center - halfRight + halfUp);
    }
}
