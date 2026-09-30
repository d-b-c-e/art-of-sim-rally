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
    private SeparateDisplayRenderDriver _separateDriver;

    internal double? CenterVerticalFovDegrees { get; private set; }
    internal long SuccessfulWideFrames => _separateDriver?.SpanMode == true
        ? _separateDriver.SuccessfulCenterFrames : 0;
    internal DateTime? LastSuccessfulWideFrameUtc => SuccessfulWideFrames == 0
        ? (DateTime?)null : _separateDriver.LastCenterFrameUtc;
    internal bool SeparateDisplaysRendered => _separateDriver != null && _separateDriver.AllDisplaysRendered;
    internal double? SeparateCameraCallbackSpanMs => _separateDriver?.LastCameraCallbackSpanMs;
    internal int ActiveSidePostProcessLayers => _separateDriver?.ActiveSidePostProcessLayers ?? 0;
    internal bool SideColorGradingEnabled => _separateDriver != null && _separateDriver.SideColorGradingEnabled;
    internal bool SideColorGradeCreated => _separateDriver != null && _separateDriver.SideColorGradeCreated;
    internal string SideColorGradeError => _separateDriver?.SideColorGradeError;
    internal string SideEffectSummary => _separateDriver?.SideEffectSummary;
    internal int ExpandedVegetationSystems => _separateDriver?.ExpandedVegetationSystems ?? 0;
    internal int SideOcclusionCount => _separateDriver?.SideOcclusionCount ?? 0;
    internal int SideBeautifyCount => _separateDriver?.SideBeautifyCount ?? 0;
    internal int SideVolumetricCount => _separateDriver?.SideVolumetricCount ?? 0;
    internal int LastRaisedSideWindowCount => _separateDriver?.LastRaisedSideWindowCount ?? 0;
    internal double ActiveViewWidthScale { get; private set; } = 1d;

    internal ProjectionOutcome Update(LayoutLoadResult layoutResult, Settings settings)
    {
        if (!settings.EnableCenterPanelPreview && !settings.EnableThreeViewPrototype &&
            !settings.EnableSeparateDisplayPrototype)
        {
            Release();
            return ProjectionOutcome.Inactive(layoutResult, "FEATURE_DISABLED", "Triple-screen rendering is off; stock rendering is active.");
        }

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

        if (output.Mode == "separate-displays")
            return UpdateSeparateDisplays(layoutResult, settings);

        if (!settings.EnableCenterPanelPreview && !settings.EnableThreeViewPrototype)
        {
            Release();
            return ProjectionOutcome.Inactive(layoutResult, "FEATURE_DISABLED", "Triple-screen rendering is disabled; stock rendering is unchanged.");
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
        if (!StageCameraEligibility.CanRender(camera != null && camera.name == "Camera Main",
                rig != null, rig?.enabled == true, camera != null && camera == _camera,
                _separateDriver?.SpanMode == true))
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
            var viewWidthScale = ResolveViewWidthScale(panel, geometry, camera, settings);
            var definition = new TripleRigDefinition(
                panel.PhysicalWidthMm,
                panel.PhysicalHeightMm,
                geometry.EyeDistanceMm / viewWidthScale,
                geometry.LeftYawDegrees,
                geometry.RightYawDegrees,
                geometry.EyeHeightAbovePanelCenterMm);
            var surfaces = TripleRigBuilder.Build(definition);
            ActiveViewWidthScale = viewWidthScale;

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

                var projections = new Matrix4x4[3];
                var rotations = new Quaternion[3];
                for (var index = 0; index < 3; index++)
                {
                    var view = CalculateView(surfaces[index], camera);
                    projections[index] = ToUnityProjection(view, camera);
                    if (index == 1)
                        CenterVerticalFovDegrees = (Math.Atan(view.Top / view.Near) - Math.Atan(view.Bottom / view.Near)) * 180d / Math.PI;
                    var forward = view.CameraForward;
                    rotations[index] = Quaternion.LookRotation(
                        new Vector3((float)forward.X, (float)forward.Y, (float)-forward.Z),
                        Vector3.up);
                }

                EnsureSeparateDriver(camera);
                _separateDriver.ConfigureSpan(layoutResult.Sha256, projections, rotations);
                if (_separateDriver.RenderError != null)
                    return ProjectionOutcome.Error(layoutResult, "THREE_VIEW_RENDER_ERROR", _separateDriver.RenderError);
                if (!_separateDriver.AllDisplaysRendered)
                    return ProjectionOutcome.Starting(layoutResult, "THREE_VIEW_ARMED", "Three off-axis viewports are armed; waiting for all cameras to render.");

                return ProjectionOutcome.ThreeViewActive(
                    layoutResult,
                    _separateDriver.LastThreeViewFrameUtc,
                    "THREE_VIEW_EXPERIMENTAL",
                    $"Three camera viewports cover the wide display with {FovDescription(settings, camera)}. HUD routing, replay, and photo mode still need visual verification.");
            }

            EnsureCenterDriver(camera);
            var center = surfaces[1];

            // Core physical geometry is millimetres. Calculate frustum edges at
            // Unity clip distances expressed in millimetres, then convert those
            // four edges back to Unity units before constructing the matrix.
            var physical = CalculateView(center, camera);
            CenterVerticalFovDegrees = (Math.Atan(physical.Top / physical.Near) - Math.Atan(physical.Bottom / physical.Near)) * 180d / Math.PI;

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

    private ProjectionOutcome UpdateSeparateDisplays(LayoutLoadResult layoutResult, Settings settings)
    {
        var panel = layoutResult.Document.Panel;
        var geometry = layoutResult.Document.Geometry;
        if (!settings.EnableSeparateDisplayPrototype)
        {
            Release();
            return ProjectionOutcome.Inactive(layoutResult, "SEPARATE_DISPLAY_OPT_IN_REQUIRED",
                "The independent-display experiment is off; stock rendering remains active.");
        }

        if (settings.EnableThreeViewPrototype || settings.EnableCenterPanelPreview)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "RENDER_MODE_CONFLICT",
                "Turn off the Surround three-view and center-preview options before using independent displays.");
        }

        if (Screen.width != panel.NativeWidthPx || Screen.height != panel.NativeHeightPx)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "PRIMARY_DISPLAY_MISMATCH",
                $"Primary game output is {Screen.width}x{Screen.height}; independent displays require the center monitor at {panel.NativeWidthPx}x{panel.NativeHeightPx}.");
        }

        var displays = Display.displays;
        if (settings.LeftDisplayIndex < 1 || settings.RightDisplayIndex < 1 ||
            settings.LeftDisplayIndex == settings.RightDisplayIndex ||
            settings.LeftDisplayIndex >= displays.Length || settings.RightDisplayIndex >= displays.Length)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "SECONDARY_DISPLAY_MAPPING_INVALID",
                $"Unity reports {displays.Length} displays; choose distinct secondary indices for left and right (center is 0).");
        }

        if (displays[settings.LeftDisplayIndex].systemWidth != panel.NativeWidthPx ||
            displays[settings.LeftDisplayIndex].systemHeight != panel.NativeHeightPx ||
            displays[settings.RightDisplayIndex].systemWidth != panel.NativeWidthPx ||
            displays[settings.RightDisplayIndex].systemHeight != panel.NativeHeightPx)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "SECONDARY_DISPLAY_RESOLUTION_MISMATCH",
                "Both selected secondary Unity displays must report the panel's native resolution before activation.");
        }

        var camera = Camera.main;
        var rig = camera == null || camera.transform.parent == null
            ? null : camera.transform.parent.GetComponent<CarCameras>();
        // The game's CameraManager disables CarCameras at the finish line and
        // enables CinemachineBrain on the same Stage Camera. Keep an already
        // armed three-display driver on that Camera Main through the handoff.
        if (!StageCameraEligibility.CanRender(camera != null && camera.name == "Camera Main",
                rig != null, rig?.enabled == true, camera != null && camera == _camera,
                _separateDriver != null && !_separateDriver.SpanMode))
        {
            Release();
            return ProjectionOutcome.Starting(layoutResult, "STAGE_CAMERA_WAIT",
                "Waiting for the active gameplay camera. Menus remain on the primary center display.");
        }

        var postProcessing = camera.GetComponent<PostProcessLayer>();
        if (postProcessing != null && postProcessing.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing)
        {
            Release();
            return ProjectionOutcome.Degraded(layoutResult, "TAA_UNSUPPORTED",
                "Temporal anti-aliasing can reset custom projection matrices; select another AA mode for this experiment.");
        }

        try
        {
            var viewWidthScale = ResolveViewWidthScale(panel, geometry, camera, settings);
            var definition = new TripleRigDefinition(
                panel.PhysicalWidthMm, panel.PhysicalHeightMm, geometry.EyeDistanceMm / viewWidthScale,
                geometry.LeftYawDegrees, geometry.RightYawDegrees, geometry.EyeHeightAbovePanelCenterMm);
            var surfaces = TripleRigBuilder.Build(definition);
            ActiveViewWidthScale = viewWidthScale;
            var projections = new Matrix4x4[3];
            var rotations = new Quaternion[3];
            for (var index = 0; index < 3; index++)
            {
                var view = CalculateView(surfaces[index], camera);
                projections[index] = ToUnityProjection(view, camera);
                if (index == 1)
                    CenterVerticalFovDegrees = (Math.Atan(view.Top / view.Near) - Math.Atan(view.Bottom / view.Near)) * 180d / Math.PI;
                var forward = view.CameraForward;
                rotations[index] = Quaternion.LookRotation(
                    new Vector3((float)forward.X, (float)forward.Y, (float)-forward.Z), Vector3.up);
            }

            EnsureSeparateDriver(camera);
            _separateDriver.Configure(layoutResult.Sha256, projections, rotations, settings.LeftDisplayIndex, settings.RightDisplayIndex);
            if (_separateDriver.RenderError != null)
                return ProjectionOutcome.Error(layoutResult, "SEPARATE_DISPLAY_RENDER_ERROR", _separateDriver.RenderError);
            if (!_separateDriver.AllDisplaysRendered)
                return ProjectionOutcome.Starting(layoutResult, "SEPARATE_DISPLAYS_ARMED",
                    "Three physical views are armed; waiting for all three cameras to finish a frame.");
            return ProjectionOutcome.SeparateViewActive(layoutResult, _separateDriver.LastThreeViewFrameUtc,
                "SEPARATE_DISPLAYS_EXPERIMENTAL",
                $"Three cameras target independent Unity displays with {FovDescription(settings, camera)}. Side post-processing, UI routing, FPS and scanout remain under test.");
        }
        catch (Exception exception)
        {
            Release();
            return ProjectionOutcome.Error(layoutResult, "SEPARATE_DISPLAY_ERROR", SafeMessage(exception.Message));
        }
    }

    internal void Release()
    {
        if (_centerDriver != null)
        {
            _centerDriver.Release();
            UnityEngine.Object.Destroy(_centerDriver);
        }

        if (_separateDriver != null)
        {
            _separateDriver.Release();
            UnityEngine.Object.Destroy(_separateDriver);
        }

        _centerDriver = null;
        _separateDriver = null;
        _camera = null;
        CenterVerticalFovDegrees = null;
        ActiveViewWidthScale = 1d;
    }

    private void EnsureCenterDriver(Camera camera)
    {
        if (_camera == camera && _centerDriver != null) return;
        Release();
        _camera = camera;
        _centerDriver = camera.GetComponent<CenterProjectionDriver>();
        if (_centerDriver == null) _centerDriver = camera.gameObject.AddComponent<CenterProjectionDriver>();
    }

    private void EnsureSeparateDriver(Camera camera)
    {
        if (_camera == camera && _separateDriver != null) return;
        Release();
        _camera = camera;
        _separateDriver = camera.GetComponent<SeparateDisplayRenderDriver>();
        if (_separateDriver == null) _separateDriver = camera.gameObject.AddComponent<SeparateDisplayRenderDriver>();
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

    private static double ResolveViewWidthScale(PanelDocument panel, GeometryDocument geometry,
        Camera camera, Settings settings)
    {
        return FovPreference.ResolveScale(panel.PhysicalHeightMm, geometry.EyeDistanceMm,
            geometry.EyeHeightAbovePanelCenterMm, settings.OverrideFieldOfView,
            settings.ViewWidthScale, camera.fieldOfView);
    }

    private string FovDescription(Settings settings, Camera camera) => settings.OverrideFieldOfView
        ? $"custom {CenterVerticalFovDegrees:0}° center FOV"
        : $"the game's {camera.fieldOfView:0}° camera FOV across all views";

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown projection error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
