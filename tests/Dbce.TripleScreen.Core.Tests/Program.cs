using System;
using System.Text;
using Dbce.TripleScreen;
using Dbce.TripleScreen.Protocol;

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
            SharedEdgesLandOnAdjacentViewportBorders();
            IndependentSideAnglesArePreserved();
            MatrixMatchesFrustum();
            BadEyeSideIsRejected();
            CanonicalLayoutParsesStrictly();
            InvalidLayoutsAreRejected();
            RuntimeStatusMatchesCanonicalShape();
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

    private static void SharedEdgesLandOnAdjacentViewportBorders()
    {
        var surfaces = Rig();
        var projections = new[]
        {
            ProjectionCalculator.Calculate(surfaces[0], new Vector3d(0d, 0d, 0d), 0.1d, 1500d),
            ProjectionCalculator.Calculate(surfaces[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d),
            ProjectionCalculator.Calculate(surfaces[2], new Vector3d(0d, 0d, 0d), 0.1d, 1500d)
        };

        var leftHinge = surfaces[1].LowerLeft + (surfaces[1].Up * (surfaces[1].Height / 2d));
        var rightHinge = surfaces[1].LowerRight + (surfaces[1].Up * (surfaces[1].Height / 2d));
        Near(ProjectHorizontal(projections[0], leftHinge), 1d, 1e-9, "left view inner edge");
        Near(ProjectHorizontal(projections[1], leftHinge), -1d, 1e-9, "center view left edge");
        Near(ProjectHorizontal(projections[1], rightHinge), 1d, 1e-9, "center view right edge");
        Near(ProjectHorizontal(projections[2], rightHinge), -1d, 1e-9, "right view inner edge");
    }

    private static double ProjectHorizontal(OffAxisProjection view, Vector3d point)
    {
        var ray = point - view.CameraPosition;
        var depth = Vector3d.Dot(ray, view.CameraForward);
        var nearX = Vector3d.Dot(ray, view.CameraRight) * view.Near / depth;
        return (2d * nearX - view.Left - view.Right) / (view.Right - view.Left);
    }

    private static void IndependentSideAnglesArePreserved()
    {
        var dimensions = PanelDimensions.FromDiagonal(32d, 16d, 9d);
        var surfaces = TripleRigBuilder.Build(new TripleRigDefinition(
            dimensions.WidthMm,
            dimensions.HeightMm,
            700d,
            45d,
            65d));
        Near(surfaces[0].CameraYawDegrees(), -45d, 1e-10, "left independent yaw");
        Near(surfaces[2].CameraYawDegrees(), 65d, 1e-10, "right independent yaw");
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

    private static void CanonicalLayoutParsesStrictly()
    {
        var result = LayoutContractParser.Parse(Utf8(ValidLayout));
        Check(result.IsSuccess, result.ErrorMessage ?? "valid layout rejected");
        Check(result.Document?.Geometry?.LeftYawDegrees == 55d, "left yaw lost during parse");
        Check(result.Document?.Geometry?.RightYawDegrees == 60d, "right yaw lost during parse");
        Check(result.Sha256?.Length == 64, "layout SHA-256 missing");
    }

    private static void InvalidLayoutsAreRejected()
    {
        var unknown = LayoutContractParser.Parse(Utf8(ValidLayout.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"invented\":true")));
        Check(!unknown.IsSuccess && unknown.ErrorCode == "LAYOUT_INVALID_JSON", "unknown property accepted");

        var duplicate = LayoutContractParser.Parse(Utf8(ValidLayout.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1")));
        Check(!duplicate.IsSuccess && duplicate.ErrorCode == "LAYOUT_INVALID_JSON", "duplicate property accepted");

        var future = LayoutContractParser.Parse(Utf8(ValidLayout.Replace("\"schemaVersion\":1", "\"schemaVersion\":2")));
        Check(!future.IsSuccess && future.ErrorCode == "LAYOUT_VERSION_UNSUPPORTED", "future contract guessed at");

        var badAngle = LayoutContractParser.Parse(Utf8(ValidLayout.Replace("\"leftYawDegrees\":55", "\"leftYawDegrees\":90")));
        Check(!badAngle.IsSuccess && badAngle.ErrorCode == "LAYOUT_GEOMETRY_INVALID", "invalid side angle accepted");

        var comment = LayoutContractParser.Parse(Utf8(ValidLayout + "/* trailing comment */"));
        Check(!comment.IsSuccess && comment.ErrorCode == "LAYOUT_COMMENTS_UNSUPPORTED", "JSON comment accepted");

        var trailing = LayoutContractParser.Parse(Utf8(ValidLayout + "{}"));
        Check(!trailing.IsSuccess, "trailing JSON value accepted");

        var oversized = LayoutContractParser.Parse(new byte[LayoutContractParser.MaximumDocumentBytes + 1]);
        Check(!oversized.IsSuccess && oversized.ErrorCode == "LAYOUT_TOO_LARGE", "oversized layout accepted");
    }

    private static void RuntimeStatusMatchesCanonicalShape()
    {
        var status = new RuntimeStatusDocument
        {
            AdapterId = "dbce-triple-mod-art-of-rally",
            AdapterVersion = "0.1.0",
            GameId = "art-of-rally",
            State = "active",
            AcceptedLayoutSha256 = new string('a', 64),
            LayoutContractVersion = 1,
            Topology = "nvidia-surround",
            ActiveCameraCount = 1,
            LastSuccessfulFrameUtc = "2026-09-21T12:00:00.0000000Z"
        };
        status.ActiveCapabilities.Add("asymmetric-frustum");
        status.Diagnostics.Add(new RuntimeDiagnostic("CENTER_PREVIEW_ACTIVE", "info", "Center projection preview is active."));
        var json = RuntimeStatusJson.Serialize(status);
        Check(json.Contains("\"schemaVersion\": 1"), "status schema version missing");
        Check(json.Contains("\"state\": \"active\""), "status state missing");
        Check(json.Contains("\"activeCapabilities\": ["), "status capabilities missing");
    }

    private static System.Collections.Generic.IReadOnlyList<DisplaySurface> Rig()
    {
        var dimensions = PanelDimensions.FromDiagonal(32d, 16d, 9d);
        return TripleRigBuilder.Build(new TripleRigDefinition(dimensions.WidthMm, dimensions.HeightMm, 700d, 60d));
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    private const string ValidLayout = "{" +
        "\"schemaVersion\":1," +
        "\"panel\":{\"count\":3,\"nativeWidthPx\":2560,\"nativeHeightPx\":1440,\"physicalWidthMm\":708.4,\"physicalHeightMm\":398.5,\"curveRadiusMm\":1500,\"bezelWidthMm\":8}," +
        "\"geometry\":{\"eyeDistanceMm\":700,\"eyeHeightAbovePanelCenterMm\":10,\"leftYawDegrees\":55,\"rightYawDegrees\":60}," +
        "\"output\":{\"mode\":\"nvidia-surround\",\"combinedWidthPx\":7680,\"combinedHeightPx\":1440}" +
        "}";

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

internal static class SurfaceTestExtensions
{
    internal static double CameraYawDegrees(this DisplaySurface surface) =>
        Math.Atan2(surface.CameraForward().X, -surface.CameraForward().Z) * 180d / Math.PI;

    private static Vector3d CameraForward(this DisplaySurface surface) => -surface.NormalTowardViewer;
}
