using System;
using Dbce.TripleScreen;

namespace Dbce.TripleScreen.Tests;

internal static class Program
{
    private static int _assertions;

    private static int Main()
    {
        try
        {
            DimensionsFromDiagonal();
            HingesAreContinuous();
            CenterProjectionIsSymmetric();
            SideProjectionsAreMirroredAndOffAxis();
            MatrixMatchesFrustum();
            BadEyeSideIsRejected();
            Console.WriteLine($"PASS: {_assertions} triple-screen geometry assertions");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: " + exception);
            return 1;
        }
    }

    private static void DimensionsFromDiagonal()
    {
        var dimensions = PanelDimensions.FromDiagonal(32d, 16d, 9d);
        Near(dimensions.WidthMm, 708.4d, 0.1d, "32-inch 16:9 width");
        Near(dimensions.HeightMm, 398.5d, 0.1d, "32-inch 16:9 height");
    }

    private static void HingesAreContinuous()
    {
        var surfaces = Rig();
        Near(surfaces[0].LowerRight, surfaces[1].LowerLeft, 1e-9, "left/center lower hinge");
        Near(surfaces[0].UpperLeft + (surfaces[0].Right * surfaces[0].Width), surfaces[1].UpperLeft, 1e-9, "left/center upper hinge");
        Near(surfaces[1].LowerRight, surfaces[2].LowerLeft, 1e-9, "center/right lower hinge");
    }

    private static void CenterProjectionIsSymmetric()
    {
        var center = ProjectionCalculator.Calculate(Rig()[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        Near(center.Left, -center.Right, 1e-12, "center horizontal symmetry");
        Near(center.Bottom, -center.Top, 1e-12, "center vertical symmetry");
        Near(center.CameraForward, new Vector3d(0d, 0d, -1d), 1e-12, "center forward basis");
    }

    private static void SideProjectionsAreMirroredAndOffAxis()
    {
        var surfaces = Rig();
        var left = ProjectionCalculator.Calculate(surfaces[0], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        var right = ProjectionCalculator.Calculate(surfaces[2], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        Near(left.Left, -right.Right, 1e-12, "mirrored outer frustum edge");
        Near(left.Right, -right.Left, 1e-12, "mirrored inner frustum edge");
        Check(Math.Abs(left.Left + left.Right) > 1e-6, "arbitrary side angle must produce an asymmetric side frustum");
    }

    private static void MatrixMatchesFrustum()
    {
        var projection = ProjectionCalculator.Calculate(Rig()[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        var expectedM00 = (2d * projection.Near) / (projection.Right - projection.Left);
        Near(projection.ProjectionMatrix[0, 0], expectedM00, 1e-12, "projection m00");
        Near(projection.ProjectionMatrix[3, 2], -1d, 0d, "projection perspective row");
    }

    private static void BadEyeSideIsRejected()
    {
        var threw = false;
        try
        {
            ProjectionCalculator.Calculate(Rig()[1], new Vector3d(0d, 0d, -1000d), 0.1d, 1500d);
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        Check(threw, "eye behind display must be rejected");
    }

    private static System.Collections.Generic.IReadOnlyList<DisplaySurface> Rig()
    {
        var dimensions = PanelDimensions.FromDiagonal(32d, 16d, 9d);
        return TripleRigBuilder.Build(new TripleRigDefinition(dimensions.WidthMm, dimensions.HeightMm, 700d, 60d));
    }

    private static void Near(double actual, double expected, double tolerance, string name)
    {
        Check(Math.Abs(actual - expected) <= tolerance, $"{name}: expected {expected}, got {actual}");
    }

    private static void Near(Vector3d actual, Vector3d expected, double tolerance, string name)
    {
        Check((actual - expected).Length <= tolerance, $"{name}: expected {expected}, got {actual}");
    }

    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
