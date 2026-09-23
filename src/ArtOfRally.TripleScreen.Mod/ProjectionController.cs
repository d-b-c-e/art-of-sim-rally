using System;
using Dbce.TripleScreen;
using Dbce.TripleScreen.Protocol;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ArtOfRally.TripleScreen.Mod;

internal sealed class ProjectionController
{
    private Camera _camera;
    private CenterProjectionDriver _centerDriver;
    private ThreeViewRenderDriver _threeViewDriver;

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

        if (!settings.EnableCenterPanelPreview && !settings.EnableThreeViewPrototype)
        {
            Release();
            return ProjectionOutcome.Inactive(layoutResult, "FEATURE_DISABLED", "Triple-screen rendering is disabled; stock rendering is unchanged.");
        }

        if (output.Mode == "separate-displays")
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "TOPOLOGY_UNSUPPORTED", "Separate-display activation is not implemented; stock rendering remains active.");
        }

        if (!output.CombinedWidthPx.HasValue || !output.CombinedHeightPx.HasValue)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_RESOLUTION_REQUIRED", "The selected rendering mode requires the optimizer's combined output resolution.");
        }

        if (output.CombinedHeightPx.Value != panel.NativeHeightPx)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_HEIGHT_UNSUPPORTED", "The combined output height must equal panel native height.");
        }

        if ((long)output.CombinedWidthPx.Value < (long)panel.NativeWidthPx * panel.Count)
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
            return ProjectionOutcome.Degraded(layoutResult, "TAA_UNSUPPORTED", "Temporal anti-aliasing can reset custom projection matrices; select another AA mode for this experiment.");
        }

        try
        {
            var definition = new TripleRigDefinition(
                panel.PhysicalWidthMm,
                panel.PhysicalHeightMm,
                geometry.EyeDistanceMm,
                geometry.LeftYawDegrees,
                geometry.RightYawDegrees,
                geometry.EyeHeightAbovePanelCenterMm);
            var surfaces = TripleRigBuilder.Build(definition);

            if (settings.EnableThreeViewPrototype)
            {
                if ((long)output.CombinedWidthPx.Value != (long)panel.NativeWidthPx * 3)
                {
                    Release();
                    return ProjectionOutcome.Degraded(
                        layoutResult,
                        "BEZEL_CORRECTED_OUTPUT_UNSUPPORTED",
                        "The first three-view prototype requires exactly three native panel widths; disable driver bezel correction for this experiment.");
                }

                if (panel.NativeWidthPx > SystemInfo.maxTextureSize ||
                    panel.NativeHeightPx > SystemInfo.maxTextureSize ||
                    (long)panel.NativeWidthPx * panel.NativeHeightPx * 3 > 30_000_000L)
                {
                    Release();
                    return ProjectionOutcome.Degraded(
                        layoutResult,
                        "RENDER_TARGET_SIZE_UNSUPPORTED",
                        "The requested three-view render targets exceed the safe prototype limit for this GPU.");
                }

                EnsureThreeViewDriver(camera);
                var projections = new Matrix4x4[3];
                var rotations = new Quaternion[3];
                for (var index = 0; index < 3; index++)
                {
                    var view = CalculateView(surfaces[index], camera);
                    projections[index] = ToUnityProjection(view, camera);
                    var forward = view.CameraForward;
                    rotations[index] = Quaternion.LookRotation(
                        new Vector3((float)forward.X, (float)forward.Y, (float)-forward.Z),
                        Vector3.up);
                }

                _threeViewDriver.Configure(
                    layoutResult.Sha256,
                    panel.NativeWidthPx,
                    panel.NativeHeightPx,
                    projections,
                    rotations);
                if (_threeViewDriver.RenderError != null)
                    return ProjectionOutcome.Error(layoutResult, "THREE_VIEW_RENDER_ERROR", _threeViewDriver.RenderError);
                if (_threeViewDriver.SuccessfulFrames == 0)
                    return ProjectionOutcome.Starting(layoutResult, "THREE_VIEW_ARMED", "Three off-axis views are armed; waiting for the first composited frame.");

                return ProjectionOutcome.ThreeViewActive(
                    layoutResult,
                    _threeViewDriver.LastSuccessfulFrameUtc,
                    "THREE_VIEW_EXPERIMENTAL",
                    "Three panel-sized views are composited. Post-processing, HUD routing, replay, and photo mode still need visual verification.");
            }

            EnsureCenterDriver(camera);
            var center = surfaces[1];

            // Core physical geometry is millimetres. Calculate frustum edges at
            // Unity clip distances expressed in millimetres, then convert those
            // four edges back to Unity units before constructing the matrix.
            var physical = CalculateView(center, camera);

            var viewportWidth = settings.AllowOutputResolutionMismatch ? Screen.width : output.CombinedWidthPx.Value;
            if (viewportWidth <= 0)
            {
                Release();
                return ProjectionOutcome.Degraded(layoutResult, "OUTPUT_RESOLUTION_UNAVAILABLE", "Unity has not reported a usable output width.");
            }

            var panelFraction = (float)panel.NativeWidthPx / viewportWidth;
            var targetRect = new Rect((1f - panelFraction) / 2f, 0f, panelFraction, 1f);
            _centerDriver.Configure(layoutResult.Sha256, ToUnityProjection(physical, camera), targetRect);

            if (_centerDriver.SuccessfulFrames == 0)
            {
                return ProjectionOutcome.Starting(layoutResult, "CENTER_PREVIEW_ARMED", "Center-panel projection is armed and waiting for its first rendered frame.");
            }

            return ProjectionOutcome.CenterPreviewActive(
                layoutResult,
                _centerDriver.LastSuccessfulFrameUtc,
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
        if (_centerDriver != null)
        {
            _centerDriver.Release();
            UnityEngine.Object.Destroy(_centerDriver);
        }

        if (_threeViewDriver != null)
        {
            _threeViewDriver.Release();
            UnityEngine.Object.Destroy(_threeViewDriver);
        }

        _centerDriver = null;
        _threeViewDriver = null;
        _camera = null;
    }

    private void EnsureCenterDriver(Camera camera)
    {
        if (_camera == camera && _centerDriver != null) return;
        Release();
        _camera = camera;
        _centerDriver = camera.GetComponent<CenterProjectionDriver>();
        if (_centerDriver == null) _centerDriver = camera.gameObject.AddComponent<CenterProjectionDriver>();
    }

    private void EnsureThreeViewDriver(Camera camera)
    {
        if (_camera == camera && _threeViewDriver != null) return;
        Release();
        _camera = camera;
        _threeViewDriver = camera.GetComponent<ThreeViewRenderDriver>();
        if (_threeViewDriver == null) _threeViewDriver = camera.gameObject.AddComponent<ThreeViewRenderDriver>();
    }

    private static OffAxisProjection CalculateView(DisplaySurface surface, Camera camera)
    {
        return ProjectionCalculator.Calculate(
            surface,
            new Vector3d(0d, 0d, 0d),
            camera.nearClipPlane * 1000d,
            camera.farClipPlane * 1000d);
    }

    private static Matrix4x4 ToUnityProjection(OffAxisProjection physical, Camera camera)
    {
        var matrix = ProjectionCalculator.PerspectiveOffCenter(
            physical.Left / 1000d,
            physical.Right / 1000d,
            physical.Bottom / 1000d,
            physical.Top / 1000d,
            camera.nearClipPlane,
            camera.farClipPlane);
        return ToUnity(matrix);
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
