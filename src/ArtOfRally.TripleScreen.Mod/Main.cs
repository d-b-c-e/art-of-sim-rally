using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

internal static class Main
{
    private static UnityModManager.ModEntry _modEntry;
    private static Settings _settings;
    private static LayoutSource Layout;
    private static readonly ProjectionController Projection = new();
    private static StatusPublisher _status;
    private static bool _enabled;
    private static int _frame;
    private static ProjectionOutcome _lastOutcome;

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

        _lastOutcome = _settings.EnableThreeViewPrototype || _settings.EnableCenterPanelPreview
            ? ProjectionOutcome.Starting(Layout.Current, "ADAPTER_STARTING", "Adapter loaded; waiting to evaluate the stage camera.")
            : ProjectionOutcome.Inactive(Layout.Current, "FEATURE_DISABLED", "Triple-screen rendering is disabled; stock rendering is unchanged.");
        _status.Publish(_lastOutcome, Application.version, true);
        modEntry.Logger.Log("Loaded v" + AdapterConstants.AdapterVersion + ". Three-view rendering defaults off; no display or resolution APIs are changed.");
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
            var outcome = !_enabled
                ? ProjectionOutcome.Inactive(Layout.Current, "MOD_DISABLED", "The UMM mod is disabled; stock rendering is active.")
                : Projection.Update(Layout.Current, _settings);
            _lastOutcome = outcome;
            _status.Publish(outcome, Application.version);
        }
        catch (Exception exception)
        {
            Projection.Release();
            _lastOutcome = ProjectionOutcome.Error(Layout.Current, "ADAPTER_UPDATE_ERROR", SafeMessage(exception.Message));
            _status.Publish(_lastOutcome, Application.version);
        }
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        _ = modEntry;
        GUILayout.Label("DBCE triple-screen adapter v" + AdapterConstants.AdapterVersion);
        GUILayout.Label("Experimental triple-screen renderer: NVIDIA Surround or an exact-size borderless span is required.");
        _settings.EnableThreeViewPrototype = GUILayout.Toggle(
            _settings.EnableThreeViewPrototype,
            "Enable experimental three-view rendering (unprocessed; no TAA)");
        _settings.EnableCenterPanelPreview = GUILayout.Toggle(
            _settings.EnableCenterPanelPreview,
            "Enable center-panel projection preview when three-view rendering is off");
        _settings.AllowOutputResolutionMismatch = GUILayout.Toggle(
            _settings.AllowOutputResolutionMismatch,
            "Developer override: allow output resolution mismatch");
        if (GUILayout.Button("Reload optimizer layout"))
        {
            LogLayoutRefresh(Layout.Refresh(true));
        }

        var layout = Layout.Current;
        GUILayout.Label(layout.IsSuccess
            ? "Layout: accepted " + layout.Sha256.Substring(0, 12) + "…"
            : "Layout: " + layout.ErrorCode + " — " + layout.ErrorMessage);
        if (_lastOutcome != null)
            GUILayout.Label("Runtime: " + _lastOutcome.State + " — " + _lastOutcome.Diagnostic.Message);
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) => _settings.Save(modEntry);

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        _ = modEntry;
        _enabled = value;
        if (!value)
        {
            Projection.Release();
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
