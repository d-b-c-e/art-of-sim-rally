using System;

namespace Dbce.TripleScreen;

/// <summary>
/// A rectangular planar display in eye-relative physical space. Corners must be
/// supplied as lower-left, lower-right, and upper-left as viewed by the user.
/// </summary>
public sealed class DisplaySurface
{
    public DisplaySurface(string id, Vector3d lowerLeft, Vector3d lowerRight, Vector3d upperLeft)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A surface id is required.", nameof(id));
        }

        Id = id;
        LowerLeft = lowerLeft;
        LowerRight = lowerRight;
        UpperLeft = upperLeft;

        var horizontal = lowerRight - lowerLeft;
        var vertical = upperLeft - lowerLeft;
        if (horizontal.Length <= 1e-9 || vertical.Length <= 1e-9)
        {
            throw new ArgumentException("Display edges must have non-zero length.");
        }

        var perpendicular = Math.Abs(Vector3d.Dot(horizontal.Normalized(), vertical.Normalized()));
        if (perpendicular > 1e-6)
        {
            throw new ArgumentException("Display horizontal and vertical edges must be perpendicular.");
        }
    }

    public string Id { get; }
    public Vector3d LowerLeft { get; }
    public Vector3d LowerRight { get; }
    public Vector3d UpperLeft { get; }
    public Vector3d Right => (LowerRight - LowerLeft).Normalized();
    public Vector3d Up => (UpperLeft - LowerLeft).Normalized();
    public Vector3d NormalTowardViewer => Vector3d.Cross(Right, Up).Normalized();
    public double Width => (LowerRight - LowerLeft).Length;
    public double Height => (UpperLeft - LowerLeft).Length;
    public Vector3d Center => LowerLeft + ((LowerRight - LowerLeft) / 2d) + ((UpperLeft - LowerLeft) / 2d);
}
