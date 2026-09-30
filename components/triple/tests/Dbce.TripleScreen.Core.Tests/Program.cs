using System;
using System.IO;
using System.Text;
using ArtOfRally.TripleScreen.Mod;
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
            SpanViewportsCoverTheOutput();
            ViewWidthOverridePreservesHinges();
            GameFovPreservesAllThreeSeams();
            CameraHandoffKeepsArmedView();
            IndependentSideAnglesArePreserved();
            MatrixMatchesFrustum();
            BadEyeSideIsRejected();
            CanonicalLayoutParsesStrictly();
            SeparateDisplayLayoutParsesWithoutWideOutput();
            InvalidLayoutsAreRejected();
            StagedLayoutFallbackAndReload();
            RuntimeStatusMatchesCanonicalShape();
            GameFovSideOnlyPointsSurviveIndependentClipVolumes();
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

    private static void SpanViewportsCoverTheOutput()
    {
        var left = TripleViewportLayout.ForPanelIndex(0);
        var center = TripleViewportLayout.ForPanelIndex(1);
        var right = TripleViewportLayout.ForPanelIndex(2);
        Near(left.X, 0d, 0d, "span begins at left edge");
        Near(left.X + left.Width, center.X, 1e-12, "span left/center border");
        Near(center.X + center.Width, right.X, 1e-12, "span center/right border");
        Near(right.X + right.Width, 1d, 1e-12, "span ends at right edge");
        Near(left.Height, 1d, 0d, "span fills output height");
        Check(left.Width == center.Width && center.Width == right.Width,
            "span panels must have equal viewport widths");
    }

    private static void ViewWidthOverridePreservesHinges()
    {
        var dimensions = PanelDimensions.FromDiagonal(32d, 16d, 9d);
        var measured = TripleRigBuilder.Build(new TripleRigDefinition(dimensions.WidthMm, dimensions.HeightMm, 700d, 60d));
        var widened = TripleRigBuilder.Build(new TripleRigDefinition(dimensions.WidthMm, dimensions.HeightMm, 700d / 1.5d, 60d));
        var measuredCenter = ProjectionCalculator.Calculate(measured[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        var widerCenter = ProjectionCalculator.Calculate(widened[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        Check(widerCenter.Right - widerCenter.Left > measuredCenter.Right - measuredCenter.Left,
            "view-width preference should widen the center frustum");

        var left = ProjectionCalculator.Calculate(widened[0], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        var right = ProjectionCalculator.Calculate(widened[2], new Vector3d(0d, 0d, 0d), 0.1d, 1500d);
        var leftHinge = widened[1].LowerLeft + (widened[1].Up * (widened[1].Height / 2d));
        var rightHinge = widened[1].LowerRight + (widened[1].Up * (widened[1].Height / 2d));
        Near(ProjectHorizontal(left, leftHinge), 1d, 1e-9, "wider left inner edge");
        Near(ProjectHorizontal(widerCenter, leftHinge), -1d, 1e-9, "wider center left edge");
        Near(ProjectHorizontal(widerCenter, rightHinge), 1d, 1e-9, "wider center right edge");
        Near(ProjectHorizontal(right, rightHinge), -1d, 1e-9, "wider right inner edge");
    }

    private static void GameFovPreservesAllThreeSeams()
    {
        // The observed game camera uses 75° while the measured center view is
        // much narrower. Game-follow must permit values beyond the slider's
        // old 2x scale and apply one virtual eye to every physical view.
        const double width = 708.4d;
        const double height = 398.5d;
        const double distance = 660d;
        const double savedSliderScale = 1.33168638d;
        var wideScale = FovPreference.ResolveScale(height, distance, 0d,
            false, savedSliderScale, 75d);
        Check(wideScale > FovPreference.MaximumSliderScale,
            "game FOV was incorrectly limited to the manual slider range");
        Near(FovPreference.VerticalDegrees(height, distance, 0d, wideScale), 75d,
            1e-9, "game camera FOV match");
        var tighterScale = FovPreference.ResolveScale(height, distance, 0d,
            false, savedSliderScale, 65d);
        Check(tighterScale < wideScale, "live game FOV changes should update the shared scale");
        var sliderScale = FovPreference.ResolveScale(height, distance, 0d,
            true, savedSliderScale, 75d);
        Near(sliderScale, savedSliderScale, 1e-12,
            "turning override on should restore the saved slider scale");
        Near(FovPreference.ResolveScale(height, distance, 0d,
            false, savedSliderScale, 75d), wideScale, 1e-12,
            "turning override off again should restore game FOV");
        var savedSliderDegrees = FovPreference.VerticalDegrees(height, distance, 0d, sliderScale);
        Near(FovPreference.ScaleForSliderDegrees(height, distance, 0d, savedSliderDegrees),
            savedSliderScale, 1e-9, "saved slider FOV is preserved when override is enabled");
        Near(FovPreference.ClampSliderScale(double.NaN), 1d, 0d,
            "invalid saved slider scale uses the safe measured view");

        CheckSeamsForScale(width, height, distance, wideScale, "game FOV before override");
        CheckSeamsForScale(width, height, distance, sliderScale, "saved slider override");
        CheckSeamsForScale(width, height, distance,
            FovPreference.ResolveScale(height, distance, 0d, false, savedSliderScale, 75d),
            "game FOV after override");
    }

    private static void CheckSeamsForScale(double width, double height, double distance,
        double scale, string phase)
    {
        var surfaces = TripleRigBuilder.Build(new TripleRigDefinition(
            width, height, distance / scale, 70d, 70d));
        var views = new[]
        {
            ProjectionCalculator.Calculate(surfaces[0], new Vector3d(0d, 0d, 0d), 0.1d, 1500d),
            ProjectionCalculator.Calculate(surfaces[1], new Vector3d(0d, 0d, 0d), 0.1d, 1500d),
            ProjectionCalculator.Calculate(surfaces[2], new Vector3d(0d, 0d, 0d), 0.1d, 1500d)
        };
        var leftHinge = surfaces[1].LowerLeft + (surfaces[1].Up * (height / 2d));
        var rightHinge = surfaces[1].LowerRight + (surfaces[1].Up * (height / 2d));
        Near(ProjectHorizontal(views[0], leftHinge), 1d, 1e-9, phase + " left seam");
        Near(ProjectHorizontal(views[1], leftHinge), -1d, 1e-9, phase + " center left seam");
        Near(ProjectHorizontal(views[1], rightHinge), 1d, 1e-9, phase + " center right seam");
        Near(ProjectHorizontal(views[2], rightHinge), -1d, 1e-9, phase + " right seam");
    }

    private static void CameraHandoffKeepsArmedView()
    {
        Check(StageCameraEligibility.CanRender(true, true, true, false, false),
            "the initial driving camera should be eligible before driver arming");
        Check(StageCameraEligibility.CanRender(true, true, false, true, true),
            "the armed stage camera should survive the Cinemachine handoff");
        Check(!StageCameraEligibility.CanRender(true, true, false, true, false),
            "a disabled driving rig must not arm an unrelated output mode");
        Check(!StageCameraEligibility.CanRender(true, true, false, false, true),
            "a new camera must not inherit an old driver's handoff state");
        Check(!StageCameraEligibility.CanRender(false, true, true, false, false),
            "a menu camera must not receive the stage projection");
        Check(!StageCameraEligibility.CanRender(true, false, true, false, false),
            "a camera without the driving rig must not arm the stage projection");
    }

    private static void GameFovSideOnlyPointsSurviveIndependentClipVolumes()
    {
        // Exercise this consumer's parser -> FOV preference -> physical views ->
        // actual homogeneous projection matrices. Existing hinge assertions use
        // frustum extents directly and cannot detect a broken matrix offset.
        // These synthetic points are not evidence of Unity/vegetation visibility.
        const double near = 100d; // millimetres: 0.1 Unity metres
        const double far = 1500000d; // millimetres: 1500 Unity metres
        const double sampleDepth = 10000d;
        const double savedScale = 1.33168638d;
        var eye = new Vector3d(0d, 0d, 0d);
        foreach (var topology in new[] { "nvidia-surround", "separate-displays" })
        foreach (var leftYaw in new[] { 70d, 65d })
        {
            var json = ValidLayout.Replace("\"eyeDistanceMm\":700", "\"eyeDistanceMm\":660")
                .Replace("\"eyeHeightAbovePanelCenterMm\":10", "\"eyeHeightAbovePanelCenterMm\":25")
                .Replace("\"leftYawDegrees\":55", "\"leftYawDegrees\":" + leftYaw)
                .Replace("\"rightYawDegrees\":60", "\"rightYawDegrees\":70");
            if (topology == "separate-displays")
                json = json.Replace("\"output\":{\"mode\":\"nvidia-surround\",\"combinedWidthPx\":7680,\"combinedHeightPx\":1440}",
                    "\"output\":{\"mode\":\"separate-displays\"}");
            var parsed = LayoutContractParser.Parse(Utf8(json));
            Check(parsed.IsSuccess, "side-only fixture layout must parse: " + parsed.ErrorMessage);
            var panel = parsed.Document!.Panel!;
            var geometry = parsed.Document.Geometry!;
            double? firstScale = null;
            for (var phase = 0; phase < 3; phase++)
            {
                var scale = FovPreference.ResolveScale(panel.PhysicalHeightMm,
                    geometry.EyeDistanceMm, geometry.EyeHeightAbovePanelCenterMm,
                    phase == 1, savedScale, 75d);
                if (phase == 0) firstScale = scale;
                if (phase == 1) Near(scale, savedScale, 1e-12, "side-only saved slider selection");
                if (phase == 2) Near(scale, firstScale!.Value, 1e-12, "side-only game FOV restoration");
                var surfaces = TripleRigBuilder.Build(new TripleRigDefinition(
                    panel.PhysicalWidthMm, panel.PhysicalHeightMm, geometry.EyeDistanceMm / scale,
                    geometry.LeftYawDegrees, geometry.RightYawDegrees, geometry.EyeHeightAbovePanelCenterMm));
                var center = ProjectionCalculator.Calculate(surfaces[1], eye, near, far);
                foreach (var index in new[] { 0, 2 })
                {
                    var view = ProjectionCalculator.Calculate(surfaces[index], eye, near, far);
                    var u = index == 0 ? 0.05d : 0.95d;
                    var v = index == 0 ? 0.25d : 0.75d;
                    var panelPoint = view.Surface.LowerLeft + view.Surface.Right * (u * view.Surface.Width)
                        + view.Surface.Up * (v * view.Surface.Height);
                    var ray = panelPoint - eye;
                    var viewDepth = Vector3d.Dot(ray, view.CameraForward);
                    var point = eye + ray * (sampleDepth / viewDepth);
                    var clip = ProjectClip(view, point);
                    var label = $"{topology}, left {leftYaw}, phase {phase}, panel {index}";
                    Check(point.Z > 0d, label + ": outer side-only sample must extend beyond the common-eye forward hemisphere");
                    Check(clip.W > 0d && InsideClip(clip), label + ": owning side view must retain its outer sample");
                    Check(!InsideClip(ProjectClip(center, point)), label + ": center-only pre-culling would incorrectly discard side scenery");
                    Near(clip.X / clip.W, 2d * u - 1d, 1e-9, label + " actual matrix horizontal coordinate");
                    Near(clip.Y / clip.W, 2d * v - 1d, 1e-9, label + " actual matrix vertical coordinate");
                    var atNear = ProjectClip(view, eye + ray * (near / viewDepth));
                    var atFar = ProjectClip(view, eye + ray * (far / viewDepth));
                    Near(atNear.Z / atNear.W, -1d, 1e-9, label + " near clip depth");
                    Near(atFar.Z / atFar.W, 1d, 1e-9, label + " far clip depth");
                    Check(!InsideClip(ProjectClip(view, eye + ray * ((near / 2d) / viewDepth))), label + ": reject before near plane");
                    Check(!InsideClip(ProjectClip(view, eye + ray * ((far * 2d) / viewDepth))), label + ": reject beyond far plane");
                    Check(!InsideClip(ProjectClip(view, eye - ray * (sampleDepth / viewDepth))), label + ": reject behind owning camera");
                }
            }
        }
    }

    private static (double X, double Y, double Z, double W) ProjectClip(OffAxisProjection view, Vector3d point)
    {
        // Right-handed view space: forward is negative Z; row-major matrix times
        // a column vector. Expected UVs above come from physical panel fractions.
        var relative = point - view.CameraPosition;
        var camera = new[] { Vector3d.Dot(relative, view.CameraRight),
            Vector3d.Dot(relative, view.CameraUp), -Vector3d.Dot(relative, view.CameraForward), 1d };
        var clip = new double[4];
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
            clip[row] += view.ProjectionMatrix[row, column] * camera[column];
        return (clip[0], clip[1], clip[2], clip[3]);
    }

    private static bool InsideClip((double X, double Y, double Z, double W) clip) =>
        clip.W > 0d && Math.Abs(clip.X) <= clip.W && Math.Abs(clip.Y) <= clip.W && Math.Abs(clip.Z) <= clip.W;

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

    private static void SeparateDisplayLayoutParsesWithoutWideOutput()
    {
        var independent = ValidLayout.Replace(
            "\"output\":{\"mode\":\"nvidia-surround\",\"combinedWidthPx\":7680,\"combinedHeightPx\":1440}",
            "\"output\":{\"mode\":\"separate-displays\"}");
        var result = LayoutContractParser.Parse(Utf8(independent));
        Check(result.IsSuccess, result.ErrorMessage ?? "separate-display layout rejected");
        Check(result.Document?.Output?.Mode == "separate-displays", "independent display mode lost");
        Check(result.Document?.Output?.CombinedWidthPx is null, "separate layout unexpectedly requires a wide canvas");
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

    private static void StagedLayoutFallbackAndReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dbce-triple-layout-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var canonicalPath = Path.Combine(directory, "canonical.json");
            var stagedPath = Path.Combine(directory, "staged.json");
            var source = new LayoutSource(stagedPath, canonicalPath);
            Check(!source.Refresh(true) && source.Current.ErrorCode == "LAYOUT_NOT_FOUND", "missing layout should remain rejected");

            File.WriteAllText(stagedPath, ValidLayout);
            Check(source.Refresh(false) && source.Current.IsSuccess, "staged layout fallback was not accepted");
            Check(source.CurrentPath == stagedPath, "staged path was not selected");
            Check(!source.Refresh(false), "unchanged staged layout should not reload");

            File.WriteAllText(canonicalPath, ValidLayout.Replace("\"leftYawDegrees\":55", "\"leftYawDegrees\":45"));
            Check(source.Refresh(false) && source.Current.IsSuccess, "canonical layout was not accepted");
            Check(source.CurrentPath == canonicalPath, "canonical path should take precedence");
            Check(source.Current.Document?.Geometry?.LeftYawDegrees == 45d, "canonical layout changes were not loaded");

            File.Delete(canonicalPath);
            Check(source.Refresh(false) && source.CurrentPath == stagedPath, "staged fallback was not restored");
            Check(source.Current.Document?.Geometry?.LeftYawDegrees == 55d, "staged fallback loaded the wrong layout");

            File.WriteAllBytes(stagedPath, new byte[LayoutContractParser.MaximumDocumentBytes + 1]);
            Check(source.Refresh(false) && source.Current.ErrorCode == "LAYOUT_TOO_LARGE", "oversized staged layout was not rejected");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
