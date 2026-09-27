using System;
using System.IO;
using Dbce.TripleScreen.Protocol;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

internal static class Main
{
    private static UnityModManager.ModEntry _modEntry;
    private static Settings _settings;
    private static LayoutSource Layout;
    private static readonly ResolvedLayoutSource EffectiveLayout = new();
    private static readonly ProjectionController Projection = new();
    private static StatusPublisher _status;
    private static RenderDiagnostics _renderDiagnostics;
    private static readonly MenuCenteringController MenuCentering = new();
    private static bool _enabled;
    private static int _frame;
    private static ProjectionOutcome _lastOutcome;
    private static bool _showAdvanced;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        _modEntry = modEntry;
        try
        {
            _settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        }
        catch (Exception exception)
        {
            modEntry.Logger.Warning("Could not load settings; safe defaults are active: " + exception.Message);
            _settings = new Settings();
        }

        _status = new StatusPublisher(modEntry.Logger);
        _renderDiagnostics = new RenderDiagnostics(modEntry.Logger);
        Layout = new LayoutSource(Path.Combine(modEntry.Path, "desired-layout.json"));
        var layoutPath = AdapterConstants.DesiredLayoutPath;
        modEntry.Logger.Log("Layout candidates: canonical=" + layoutPath +
                            " (visible=" + File.Exists(layoutPath) + "), staged=" +
                            Path.Combine(modEntry.Path, "desired-layout.json") +
                            " (visible=" + File.Exists(Path.Combine(modEntry.Path, "desired-layout.json")) + ").");
        Layout.Refresh(true);
        modEntry.OnUpdate = OnUpdate;
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        modEntry.OnToggle = OnToggle;
        modEntry.OnUnload = OnUnload;
        _enabled = true;

        var layout = EffectiveLayout.Resolve(Layout.Current, _settings);
        _lastOutcome = _settings.EnableThreeViewPrototype || _settings.EnableCenterPanelPreview || _settings.EnableSeparateDisplayPrototype
            ? ProjectionOutcome.Starting(layout, "ADAPTER_STARTING", "Adapter loaded; waiting to evaluate the stage camera.")
            : ProjectionOutcome.Inactive(layout, "FEATURE_DISABLED", "Triple-screen rendering is disabled; stock rendering is unchanged.");
        _status.Publish(_lastOutcome, Application.version, true);
        modEntry.Logger.Log("Loaded v" + AdapterConstants.AdapterVersion + ". Experimental rendering defaults off; no display APIs are changed without a separate-layout opt-in.");
        return true;
    }

    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        _ = modEntry;
        _ = deltaTime;
        _frame++;

        try
        {
            if (_frame % 120 == 0) LogLayoutRefresh(Layout.Refresh(false));
            var layout = EffectiveLayout.Resolve(Layout.Current, _settings);
            var outcome = !_enabled
                ? ProjectionOutcome.Inactive(layout, "MOD_DISABLED", "The UMM mod is disabled; stock rendering is active.")
                : Projection.Update(layout, _settings);
            _lastOutcome = outcome;
            _status.Publish(outcome, Application.version);
            _renderDiagnostics.Sample(outcome, Projection);
            if (_frame % 30 == 0) MenuCentering.Update(_enabled && _settings.CenterMenusOnMiddleScreen, layout);
        }
        catch (Exception exception)
        {
            Projection.Release();
            _lastOutcome = ProjectionOutcome.Error(EffectiveLayout.Resolve(Layout.Current, _settings), "ADAPTER_UPDATE_ERROR", SafeMessage(exception.Message));
            _status.Publish(_lastOutcome, Application.version);
        }
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        _ = modEntry;
        if (float.IsNaN(_settings.ViewWidthScale) || float.IsInfinity(_settings.ViewWidthScale))
            _settings.ViewWidthScale = 1f;
        GUILayout.Label("Triple-screen display");
        var mode = _settings.EnableSeparateDisplayPrototype ? 2 : _settings.EnableThreeViewPrototype ? 1 : 0;
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(mode == 0, "Off", GUI.skin.button)) mode = 0;
        if (GUILayout.Toggle(mode == 1, "Single wide display", GUI.skin.button)) mode = 1;
        if (GUILayout.Toggle(mode == 2, "Three separate displays", GUI.skin.button)) mode = 2;
        GUILayout.EndHorizontal();
        _settings.EnableThreeViewPrototype = mode == 1;
        _settings.EnableSeparateDisplayPrototype = mode == 2;
        _settings.EnableCenterPanelPreview = false;

        var layout = EffectiveLayout.Resolve(Layout.Current, _settings);
        var panel = layout.Document?.Panel;
        var geometry = layout.Document?.Geometry;
        if (mode != 0 && panel != null && geometry != null)
        {
            var minimum = FovAtScale(layout, 0.75d);
            var maximum = FovAtScale(layout, 2d);
            var current = FovAtScale(layout, Mathf.Clamp(_settings.ViewWidthScale, 0.75f, 2f));
            GUILayout.Label($"Field of view: {current:0}°");
            var chosen = GUILayout.HorizontalSlider((float)current, (float)minimum, (float)maximum);
            _settings.ViewWidthScale = ScaleForFov(layout, chosen);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Narrower", GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Wider", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Reset field of view")) _settings.ViewWidthScale = 1f;
        }
        else if (mode != 0)
            GUILayout.Label("Set up your screens in Advanced setup to enable field of view.");

        if (_lastOutcome != null && mode != 0)
            GUILayout.Label("Status: " + _lastOutcome.Diagnostic.Message);
        _showAdvanced = GUILayout.Toggle(_showAdvanced, "Advanced setup and diagnostics");
        if (!_showAdvanced) return;

        GUILayout.Label("Screen measurements");
        var imported = Layout.Current.IsSuccess;
        if (imported)
        {
            var local = GUILayout.Toggle(_settings.UseLocalLayout, "Use measurements entered here");
            _settings.UseLocalLayout = local;
            if (!_settings.UseLocalLayout) GUILayout.Label("Using imported measurements. The optimizer is optional.");
        }
        else
        {
            _settings.UseLocalLayout = true;
            GUILayout.Label("No imported measurements found. Enter yours here; no optimizer is required.");
        }
        if (_settings.UseLocalLayout)
        {
            GUILayout.Label("Enter the visible size of one panel and your distance from its center. The example values below are inactive until accepted.");
            _settings.PanelWidthPx = IntField("Panel width (pixels)", _settings.PanelWidthPx);
            _settings.PanelHeightPx = IntField("Panel height (pixels)", _settings.PanelHeightPx);
            _settings.PanelWidthMm = FloatField("Visible panel width (mm)", _settings.PanelWidthMm);
            _settings.PanelHeightMm = FloatField("Visible panel height (mm)", _settings.PanelHeightMm);
            _settings.EyeDistanceMm = FloatField("Eye distance (mm)", _settings.EyeDistanceMm);
            _settings.LeftYawDegrees = FloatField("Left screen angle (degrees)", _settings.LeftYawDegrees);
            _settings.RightYawDegrees = FloatField("Right screen angle (degrees)", _settings.RightYawDegrees);
            if (GUILayout.Button("Use these measurements")) _settings.ManualSetupConfirmed = true;
            GUILayout.Label(_settings.ManualSetupConfirmed ? "Local setup active" : "Local setup not yet active");
        }
        if (mode == 2)
        {
            GUILayout.Label("Center must be Windows primary. Exit the game after changing display mode; activated displays release on exit.");
            _settings.LeftDisplayIndex = IntField("Left display index", _settings.LeftDisplayIndex);
            _settings.RightDisplayIndex = IntField("Right display index", _settings.RightDisplayIndex);
        }
        _settings.CenterMenusOnMiddleScreen = GUILayout.Toggle(_settings.CenterMenusOnMiddleScreen,
            "Center title and main menu on middle screen");
        _settings.AllowOutputResolutionMismatch = GUILayout.Toggle(_settings.AllowOutputResolutionMismatch,
            "Developer: allow output resolution mismatch");
        if (GUILayout.Button("Reload imported layout")) LogLayoutRefresh(Layout.Refresh(true));
        GUILayout.Label(layout.IsSuccess ? "Effective layout: " + layout.Sha256.Substring(0, 12) + "…"
            : "Setup: " + layout.ErrorMessage);
        GUILayout.Label(_renderDiagnostics.PresentationSummary);
    }

    private static double FovAtScale(LayoutLoadResult layout, double scale)
    {
        var panel = layout.Document.Panel;
        var geometry = layout.Document.Geometry;
        var distance = geometry.EyeDistanceMm / scale;
        var halfHeight = panel.PhysicalHeightMm / 2d;
        return (Math.Atan((halfHeight - geometry.EyeHeightAbovePanelCenterMm) / distance) -
            Math.Atan((-halfHeight - geometry.EyeHeightAbovePanelCenterMm) / distance)) * 180d / Math.PI;
    }

    private static float ScaleForFov(LayoutLoadResult layout, float degrees)
    {
        var low = 0.75d;
        var high = 2d;
        for (var index = 0; index < 18; index++)
        {
            var mid = (low + high) / 2d;
            if (FovAtScale(layout, mid) < degrees) low = mid;
            else high = mid;
        }
        return (float)((low + high) / 2d);
    }

    private static int IntField(string label, int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(210));
        var changed = GUILayout.TextField(value.ToString(), GUILayout.Width(80));
        GUILayout.EndHorizontal();
        return int.TryParse(changed, out var parsed) ? parsed : value;
    }

    private static float FloatField(string label, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(210));
        var changed = GUILayout.TextField(value.ToString("0.##"), GUILayout.Width(80));
        GUILayout.EndHorizontal();
        return float.TryParse(changed, out var parsed) ? parsed : value;
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) => _settings.Save(modEntry);

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        _ = modEntry;
        _enabled = value;
        if (!value)
        {
            Projection.Release();
            MenuCentering.Release();
            _lastOutcome = ProjectionOutcome.Inactive(Layout.Current, "MOD_DISABLED", "The UMM mod is disabled; stock rendering is active.");
        }
        else
        {
            _lastOutcome = ProjectionOutcome.Starting(Layout.Current, "ADAPTER_STARTING", "Adapter enabled; waiting to evaluate the stage camera.");
        }

        _status?.Publish(_lastOutcome, Application.version, true);
        return true;
    }

    private static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        _ = modEntry;
        _enabled = false;
        Projection.Release();
        MenuCentering.Release();
        var outcome = ProjectionOutcome.Inactive(Layout.Current, "ADAPTER_UNLOADED", "The adapter unloaded and restored stock camera state.");
        _status?.Publish(outcome, Application.version, true);
        return true;
    }

    private static void LogLayoutRefresh(bool changed)
    {
        if (!changed || _modEntry == null) return;
        var layout = Layout.Current;
        if (layout.IsSuccess) _modEntry.Logger.Log("Accepted optimizer layout " + layout.Sha256.Substring(0, 12) +
                                                   "… from " + Layout.CurrentPath);
        else _modEntry.Logger.Warning((layout.ErrorCode ?? "LAYOUT_INVALID") + ": " + layout.ErrorMessage);
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown adapter error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
