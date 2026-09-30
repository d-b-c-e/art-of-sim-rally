using System;

namespace Dbce.TripleScreen;

/// <summary>Builds a perspective projection whose image plane is a physical monitor.</summary>
public static class ProjectionCalculator
{
    public static OffAxisProjection Calculate(DisplaySurface surface, Vector3d eye, double near, double far)
    {
        if (surface is null)
        {
            throw new ArgumentNullException(nameof(surface));
        }

        if (!(near > 0d) || !(far > near))
        {
            throw new ArgumentOutOfRangeException(nameof(near), "Require 0 < near < far.");
        }

        var rightBasis = surface.Right;
        var upBasis = surface.Up;
        var normalTowardViewer = surface.NormalTowardViewer;
        var toLowerLeft = surface.LowerLeft - eye;
        var toLowerRight = surface.LowerRight - eye;
        var toUpperLeft = surface.UpperLeft - eye;
        var eyeDistance = -Vector3d.Dot(toLowerLeft, normalTowardViewer);

        if (eyeDistance <= 1e-9)
        {
            throw new ArgumentException(
                "The eye must be in front of the display. Check corner winding and physical positions.",
                nameof(surface));
        }

        var left = Vector3d.Dot(rightBasis, toLowerLeft) * near / eyeDistance;
        var right = Vector3d.Dot(rightBasis, toLowerRight) * near / eyeDistance;
        var bottom = Vector3d.Dot(upBasis, toLowerLeft) * near / eyeDistance;
        var top = Vector3d.Dot(upBasis, toUpperLeft) * near / eyeDistance;

        if (right - left <= 1e-12 || top - bottom <= 1e-12)
        {
            throw new ArgumentException("The display produces a degenerate viewing frustum.", nameof(surface));
        }

        return new OffAxisProjection(
            surface,
            eye,
            rightBasis,
            upBasis,
            -normalTowardViewer,
            left,
            right,
            bottom,
            top,
            near,
            far,
            PerspectiveOffCenter(left, right, bottom, top, near, far));
    }

    public static Matrix4x4d PerspectiveOffCenter(
        double left,
        double right,
        double bottom,
        double top,
        double near,
        double far)
    {
        var x = (2d * near) / (right - left);
        var y = (2d * near) / (top - bottom);
        var a = (right + left) / (right - left);
        var b = (top + bottom) / (top - bottom);
        var c = -(far + near) / (far - near);
        var d = -(2d * far * near) / (far - near);

        return new Matrix4x4d(
            x, 0d, a, 0d,
            0d, y, b, 0d,
            0d, 0d, c, d,
            0d, 0d, -1d, 0d);
    }
}
