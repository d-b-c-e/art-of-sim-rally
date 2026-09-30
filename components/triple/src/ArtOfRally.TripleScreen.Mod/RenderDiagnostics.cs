using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>Observes the actual Unity presentation settings without changing them.</summary>
internal sealed class RenderDiagnostics
{
    private readonly UnityModManager.ModEntry.ModLogger _logger;
    private float _lastSampleTime = -1f;
    private int _lastSampleFrame;
    private string _lastPresentation;

    internal string PresentationSummary { get; private set; } = "Waiting for presentation sample.";

    internal RenderDiagnostics(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;

    internal void Sample(ProjectionOutcome outcome, ProjectionController projection, bool force = false)
    {
        try
        {
            SampleCore(outcome, projection, force);
        }
        catch (Exception exception)
        {
            // Observational tooling must not disable the renderer if sampling fails.
            _logger.Warning("Could not sample presentation diagnostics: " + exception.GetType().Name);
        }
    }

    private void SampleCore(ProjectionOutcome outcome, ProjectionController projection, bool force)
    {
        var now = Time.realtimeSinceStartup;
        if (!force && _lastSampleTime >= 0 && now - _lastSampleTime < 10f) return;

        var elapsed = now - _lastSampleTime;
        double? updateFps = _lastSampleTime >= 0 && elapsed > 0
            ? (Time.frameCount - _lastSampleFrame) / (double)elapsed
            : null;
        _lastSampleTime = now;
        _lastSampleFrame = Time.frameCount;

        var requestedVsync = PlayerPrefs.GetInt("SETTINGS_VSYNC", 1);
        var effectiveVsync = QualitySettings.vSyncCount;
        PresentationSummary = $"{Screen.width}x{Screen.height}, {Screen.fullScreenMode}, " +
                              $"VSync requested {requestedVsync} / effective {effectiveVsync}, " +
                              $"display {Screen.currentResolution.refreshRate} Hz";
        var camera = Camera.main;
        var postProcessing = camera == null ? null : camera.GetComponent<PostProcessLayer>();
        var snapshot = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            adapterVersion = AdapterConstants.AdapterVersion,
            gameBuild = Application.version,
            unityVersion = Application.unityVersion,
            state = outcome.State,
            diagnosticCode = outcome.Diagnostic.Code,
            layoutSha256 = outcome.Layout.IsSuccess ? outcome.Layout.Sha256 : null,
            activeCameraCount = outcome.ActiveCameraCount,
            outputWidth = Screen.width,
            outputHeight = Screen.height,
            fullscreenMode = Screen.fullScreenMode.ToString(),
            displayRefreshHz = Screen.currentResolution.refreshRate,
            requestedVsyncCount = requestedVsync,
            effectiveVsyncCount = effectiveVsync,
            vsyncRequestMismatch = requestedVsync != effectiveVsync,
            targetFrameRate = Application.targetFrameRate,
            targetFrameRateIgnoredByVsync = effectiveVsync > 0,
            observedUpdateFps = updateFps,
            focused = Application.isFocused,
            graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            sourceCamera = camera == null ? null : camera.name,
            sourceCameraComponents = camera == null ? null : camera.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name +
                    (component is Behaviour behaviour && !behaviour.enabled ? " (disabled)" : ""))
                .ToArray(),
            sourceDepthTextureMode = camera == null ? null : camera.depthTextureMode.ToString(),
            sourceRenderingPath = camera == null ? null : camera.actualRenderingPath.ToString(),
            genericRendererCount = UnityEngine.Object.FindObjectsOfType<GenericRenderer>().Length,
            vegetationSystemCount = UnityEngine.Object.FindObjectsOfType<AwesomeTechnologies.VegetationSystem.VegetationSystemPro>().Length,
            expandedVegetationSystems = projection.ExpandedVegetationSystems,
            sideOcclusionCount = projection.SideOcclusionCount,
            sideBeautifyCount = projection.SideBeautifyCount,
            sourceVolumetricEnabled = camera != null && camera.GetComponent<HxVolumetricCamera>()?.enabled == true &&
                                      camera.GetComponent<HxVolumetricImageEffect>()?.enabled == true,
            sideVolumetricCount = projection.SideVolumetricCount,
            lastRaisedSideWindowCount = projection.LastRaisedSideWindowCount,
            frustumShadowComponentCount = UnityEngine.Object.FindObjectsOfType<NGSS_FrustumShadows>().Length,
            shadowDistance = QualitySettings.shadowDistance,
            sourceVerticalFovDegrees = camera == null ? (float?)null : camera.fieldOfView,
            sourcePitchDegrees = camera == null ? (float?)null : Mathf.DeltaAngle(0, camera.transform.eulerAngles.x),
            antialiasing = postProcessing == null ? "none" : postProcessing.antialiasingMode.ToString(),
            sourcePostProcessLayerEnabled = postProcessing != null && postProcessing.enabled,
            sourcePostProcessVolumeLayer = postProcessing == null ? (int?)null : postProcessing.volumeLayer.value,
            centerProjectionVerticalFovDegrees = projection.CenterVerticalFovDegrees,
            activeViewWidthScale = projection.ActiveViewWidthScale,
            successfulWideFrames = projection.SuccessfulWideFrames,
            lastSuccessfulWideFrameUtc = projection.LastSuccessfulWideFrameUtc?.ToString("O"),
            separateDisplaysRenderedRecently = projection.SeparateDisplaysRendered,
            separateCameraCallbackSpanMs = projection.SeparateCameraCallbackSpanMs,
            activeSidePostProcessLayers = projection.ActiveSidePostProcessLayers,
            sideColorGradingEnabled = projection.SideColorGradingEnabled,
            sideColorGradeCreated = projection.SideColorGradeCreated,
            sideColorGradeError = projection.SideColorGradeError,
            sideCopiedEffects = projection.SideEffectSummary,
            sourceActiveEffects = postProcessing == null || !postProcessing.enabled ? null :
                string.Join(", ", PostProcessManager.instance.settingsTypes.Keys
                    .Where(type => postProcessing.GetBundle(type)?.settings?.enabled.value == true)
                    .Select(type => type.Name).ToArray()),
            sourceColorGradingEnabled = postProcessing != null && postProcessing.enabled &&
                                        postProcessing.GetSettings<ColorGrading>()?.enabled.value == true,
            unityDisplays = Display.displays.Select((display, index) => new
            {
                index,
                active = display.active,
                systemWidth = display.systemWidth,
                systemHeight = display.systemHeight,
                renderingWidth = display.renderingWidth,
                renderingHeight = display.renderingHeight
            }).ToArray(),
            note = "VSync is the Unity request, not proof of synchronized display scanout. " +
                   "Update FPS, render CPU time, and camera callback span are not GPU/present timing. " +
                   "Active physical projection replaces camera FOV but preserves camera pitch and position."
        };

        try
        {
            var destination = Path.Combine(AdapterConstants.GameProtocolDirectory, "render-diagnostics.json");
            Directory.CreateDirectory(AdapterConstants.GameProtocolDirectory);
            var temporary = destination + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(snapshot, Formatting.Indented), new UTF8Encoding(false));
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }

            if (PresentationSummary != _lastPresentation)
            {
                _logger.Log("Presentation: " + PresentationSummary + ". Details: " + destination);
                _lastPresentation = PresentationSummary;
            }
        }
        catch (Exception exception)
        {
            // Diagnostics must never release or disable a functioning renderer.
            _logger.Warning("Could not write presentation diagnostics: " + exception.GetType().Name);
        }
    }
}
