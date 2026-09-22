using System;
using Dbce.TripleScreen;
using Dbce.TripleScreen.Protocol;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ArtOfRally.TripleScreen.Mod;

internal sealed class ProjectionController
{
    private Camera _camera;
    private CenterProjectionDriver _driver;

    internal ProjectionOutcome Update(LayoutLoadResult layoutResult, Settings settings)
    {
        if (!layoutResult.IsSuccess || layoutResult.Document is null)
        {
            Release();
            return ProjectionOutcome.Rejected(layoutResult.ErrorCode ?? "LAYOUT_INVALID", layoutResult.ErrorMessage ?? "The desired layout is invalid.");
        }

        var layout = layoutResult.Document;
        var output = layout.Output;
        var panel = layout.Panel;
        var geometry = layout.Geometry;
        if (output is null || panel is null || geometry is null)
        {
            Release();
            return ProjectionOutcome.Rejected("LAYOUT_INVALID", "The desired layout is missing a required object.");
        }

        if (!settings.EnableCenterPanelPreview)
        {
            Release();
            return ProjectionOutcome.Inactive(layoutResult, "FEATURE_DISABLED", "Center-panel projection preview is disabled; stock rendering is unchanged.");
        }

        if (output.Mode == "separate-displays")
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "TOPOLOGY_UNSUPPORTED", "Separate-display activation is not implemented; stock rendering remains active.");
        }

        if (!output.CombinedWidthPx.HasValue || !output.CombinedHeightPx.HasValue)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_RESOLUTION_REQUIRED", "The center-panel preview requires the optimizer's combined output resolution.");
        }

        if (output.CombinedHeightPx.Value != panel.NativeHeightPx)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_HEIGHT_UNSUPPORTED", "The preview requires combined output height to equal panel native height.");
        }

        if (output.CombinedWidthPx.Value < panel.NativeWidthPx * panel.Count)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_WIDTH_UNSUPPORTED", "The combined output must provide at least one native-width viewport per panel.");
        }

        if (!settings.AllowOutputResolutionMismatch &&
            (Screen.width != output.CombinedWidthPx.Value || Screen.height != output.CombinedHeightPx.Value))
        {
            Release();
            return ProjectionOutcome.Degraded(
                layoutResult,
                "OUTPUT_RESOLUTION_MISMATCH",
                $"Game output is {Screen.width}x{Screen.height}; optimizer requested {output.CombinedWidthPx.Value}x{output.CombinedHeightPx.Value}.");
        }

        var camera = Camera.main;
        var rig = camera == null || camera.transform.parent == null
            ? null
            : camera.transform.parent.GetComponent<CarCameras>();
        if (camera == null || rig == null || !rig.enabled || camera.name != "Camera Main")
        {
            Release();
            return ProjectionOutcome.Starting(layoutResult, "STAGE_CAMERA_WAIT", "Waiting for the active gameplay Stage Camera; menus and cinematics use stock rendering.");
        }

        var postProcessing = camera.GetComponent<PostProcessLayer>();
        if (postProcessing != null && postProcessing.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "TAA_UNSUPPORTED", "Temporal anti-aliasing can reset custom projection matrices; select another AA mode for this preview.");
        }

        try
        {
            EnsureDriver(camera);
            var definition = new TripleRigDefinition(
                panel.PhysicalWidthMm,
                panel.PhysicalHeightMm,
                geometry.EyeDistanceMm,
                geometry.LeftYawDegrees,
                geometry.RightYawDegrees,
                geometry.EyeHeightAbovePanelCenterMm);
            var center = TripleRigBuilder.Build(definition)[1];

            // Core physical geometry is millimetres. Calculate frustum edges at
            // Unity clip distances expressed in millimetres, then convert those
            // four edges back to Unity units before constructing the matrix.
            var physical = ProjectionCalculator.Calculate(
                center,
                new Vector3d(0d, 0d, 0d),
                camera.nearClipPlane * 1000d,
                camera.farClipPlane * 1000d);
            var matrix = ProjectionCalculator.PerspectiveOffCenter(
                physical.Left / 1000d,
                physical.Right / 1000d,
                physical.Bottom / 1000d,
                physical.Top / 1000d,
                camera.nearClipPlane,
                camera.farClipPlane);

            var viewportWidth = settings.AllowOutputResolutionMismatch ? Screen.width : output.CombinedWidthPx.Value;
            if (viewportWidth <= 0)
            {
                Release();
                return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_RESOLUTION_UNAVAILABLE", "Unity has not reported a usable output width.");
            }

            var panelFraction = (float)panel.NativeWidthPx / viewportWidth;
            var targetRect = new Rect((1f - panelFraction) / 2f, 0f, panelFraction, 1f);
            _driver.Configure(ToUnity(matrix), targetRect);

            if (_driver.SuccessfulFrames == 0)
            {
                return ProjectionOutcome.Starting(layoutResult, "CENTER_PREVIEW_ARMED", "Center-panel projection is armed and waiting for its first rendered frame.");
            }

            return ProjectionOutcome.Active(
                layoutResult,
                _driver.LastSuccessfulFrameUtc,
                "CENTER_PREVIEW_ONLY",
                "Only the center physical viewport is rendered; side projections and final compositor are not implemented.");
        }
        catch (Exception exception)
        {
            Release();
            return ProjectionOutcome.Error(layoutResult, "PROJECTION_ERROR", SafeMessage(exception.Message));
        }
    }

    internal void Release()
    {
        if (_driver != null)
        {
            _driver.Release();
            UnityEngine.Object.Destroy(_driver);
        }

        _driver = null;
        _camera = null;
    }

    private void EnsureDriver(Camera camera)
    {
        if (_camera == camera && _driver != null) return;
        Release();
        _camera = camera;
        _driver = camera.GetComponent<CenterProjectionDriver>();
        if (_driver == null) _driver = camera.gameObject.AddComponent<CenterProjectionDriver>();
    }

    private static Matrix4x4 ToUnity(Matrix4x4d source)
    {
        var matrix = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
            matrix[row, column] = (float)source[row, column];
        return matrix;
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown projection error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
