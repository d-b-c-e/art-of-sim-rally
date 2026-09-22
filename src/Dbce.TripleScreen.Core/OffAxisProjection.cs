namespace Dbce.TripleScreen;

/// <summary>Physical view basis and asymmetric near-plane extents for one panel.</summary>
public sealed class OffAxisProjection
{
    internal OffAxisProjection(
        DisplaySurface surface,
        Vector3d cameraPosition,
        Vector3d cameraRight,
        Vector3d cameraUp,
        Vector3d cameraForward,
        double left,
        double right,
        double bottom,
        double top,
        double near,
        double far,
        Matrix4x4d projectionMatrix)
    {
        Surface = surface;
        CameraPosition = cameraPosition;
        CameraRight = cameraRight;
        CameraUp = cameraUp;
        CameraForward = cameraForward;
        Left = left;
        Right = right;
        Bottom = bottom;
        Top = top;
        Near = near;
        Far = far;
        ProjectionMatrix = projectionMatrix;
    }

    public DisplaySurface Surface { get; }
    public Vector3d CameraPosition { get; }
    public Vector3d CameraRight { get; }
    public Vector3d CameraUp { get; }
    public Vector3d CameraForward { get; }
    public double Left { get; }
    public double Right { get; }
    public double Bottom { get; }
    public double Top { get; }
    public double Near { get; }
    public double Far { get; }
    public Matrix4x4d ProjectionMatrix { get; }
}
