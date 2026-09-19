using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfSimRally.Mod
{
    public static partial class Main
    {
        private static bool _uiDirty;
        private static float _uiSaveAt;
        private static bool _settingsWereVisible;
        internal static bool SuppressHostClose;
        public static string SettingsSaveStatus { get; private set; } = "Saved";
        internal static string ModVersion => _modEntry?.Info.Version ?? "Development build";
        // Keep Unity ECalls out of methods patched by the separate CLR probe
        // fixture. The game evaluates this boundary on its own Mono runtime.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static bool HasFocus() => Application.isFocused;
        public static void MarkSettingsDirty()
        { _uiDirty = true; _uiSaveAt = Time.realtimeSinceStartup + .4f; SettingsSaveStatus = "Changes pending"; }
        internal static void FlushUiSettings(bool force = false)
        {
            if (!_uiDirty || (!force && (GameState.IsDriving || Time.realtimeSinceStartup < _uiSaveAt))) return;
            _uiSaveAt = Time.realtimeSinceStartup + 5;
            SaveSettings();
        }
        internal static void SetFeedbackEnabled(bool enabled)
        {
            if (Settings == null) return;
            Settings.ForceFeedbackEnabled = enabled;
            FfbNative.StopOutputs();
            if (!enabled) CancelForceRecovery();
            else if (!FfbNative.Ready) ReopenForceFeedback();
            MarkSettingsDirty();
        }
        internal static void StopFeedback()
        {
            SetFeedbackEnabled(false);
            // Explicit stop is rare and user initiated; stop hardware before I/O.
            FlushUiSettings(true);
        }
        internal static void SelectForceDevice()
        {
            FfbNative.Shutdown();
            if (Settings.ForceFeedbackEnabled) ReopenForceFeedback();
        }
        private static readonly PropertyInfo UmmSelection = typeof(UnityModManager.UI).GetProperty("ShowModSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo UmmFilter = typeof(UnityModManager.UI).GetField("mModFilter", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static void ToggleSettings()
        {
            var ui = UnityModManager.UI.Instance;
            if (ui == null || _modEntry == null) return;
            if (SettingsVisible) { CloseSettings(); return; }
            ui.tabId = 0;
            UmmFilter?.SetValue(ui, _modEntry.Info.DisplayName);
            ui.ToggleWindow(true);
            UmmSelection?.SetValue(ui, UnityModManager.modEntries.IndexOf(_modEntry), null);
            FfbNative.StopOutputs();
            CameraTuner.SuppressUntilRelease();
        }
        internal static void CloseSettings()
        {
            if (SettingsPanel.CancelPendingEdit()) return;
            UnityModManager.UI.Instance?.ToggleWindow(false);
            FlushUiSettings(true);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void TickSettingsUi()
        {
            if (Settings == null) return;
            bool stopButton = WheelInput.ShortcutPressed(WheelInput.Channel.StopFfbButton);
            bool settingsButton = WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton);
            if (Application.isFocused && (Input.GetKeyDown(KeyCode.F8) || (!SettingsPanel.Editing && stopButton))) StopFeedback();
            if (Enabled && Application.isFocused && (settingsButton || (!CameraKeys.ModifierHeld() && Input.GetKeyDown(Settings.SettingsKey))))
            {
                if (!SettingsPanel.Editing) ToggleSettings();
            }
            if (!Application.isFocused) SettingsPanel.CancelPendingEdit();
            if (SettingsVisible || !Application.isFocused) Shifter.SuppressUntilRelease();
            if (SettingsVisible && !_settingsWereVisible) FfbNative.StopOutputs();
            if (!SettingsVisible && _settingsWereVisible)
            { SettingsPanel.CancelPendingEdit(); CameraTuner.SuppressUntilRelease(); FlushUiSettings(true); }
            _settingsWereVisible = SettingsVisible;
            CameraKeys.Tick();
            CameraTuner.ReadResetButton();
            FlushUiSettings();
        }
    }

    // UMM handles Escape on key-up outside OnGUI. Preserve cancel-first semantics
    // for our active edit without replacing the host's input/cursor management.
    [HarmonyPatch(typeof(UnityModManager.UI), "ToggleWindow", new[] { typeof(bool) })]
    internal static class SettingsCloseGuard
    {
        [HarmonyPrefix]
        private static bool BeforeClose(bool open)
        {
            if (open || !Main.Enabled) return true;
            if (Main.SuppressHostClose) { Main.SuppressHostClose = false; return false; }
            return !SettingsPanel.CancelPendingEdit();
        }
    }
}
