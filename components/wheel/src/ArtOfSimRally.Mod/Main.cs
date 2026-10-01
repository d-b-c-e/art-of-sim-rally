using System;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Rewired;
using UnityModManagerNet;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Unity Mod Manager entry point.
    /// </summary>
    /// <remarks>
    /// The only loader-aware file in the mod. Everything else talks to
    /// <see cref="ModLog"/> and <see cref="Settings"/>, so supporting a second
    /// loader means adding a sibling of this file, not touching the patches.
    /// </remarks>
    public static partial class Main
    {
        internal static Settings Settings { get; private set; }
        internal static bool Enabled { get; private set; }
        internal static bool SettingsVisible => UnityModManager.UI.Instance != null && UnityModManager.UI.Instance.Opened;
        internal static bool OtherCameraModLoaded => UnityModManager.FindMod("CameraMod")?.Loaded == true;

        internal static void WriteLoadedMods(StringBuilder output)
        {
            output.AppendLine("--- UMM mods ---");
            foreach (var entry in UnityModManager.modEntries)
                output.AppendLine(entry.Info.Id + " " + entry.Info.Version + " loaded=" + entry.Loaded + " active=" + entry.Active);
            output.AppendLine();
        }

        private static Harmony _harmony;
        private static UnityModManager.ModEntry _modEntry;
        private static readonly FfbReconnect ForceReconnect = new FfbReconnect();
        private static int _saveAttempts;

        /// <summary>Referenced by <c>EntryMethod</c> in Info.json.</summary>
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _modEntry = modEntry;

            ModLog.Attach(
                m => modEntry.Logger.Log(m),
                m => modEntry.Logger.Warning(m),
                m => modEntry.Logger.Error(m));

            try
            {
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            }
            catch (Exception ex)
            {
                // Corrupt or older settings must not stop the mod loading; fall
                // back to defaults rather than leaving the player with nothing.
                ModLog.Warning($"Could not load settings, using defaults: {ex.Message}");
                Settings = new Settings();
            }

            // Before anything is applied: a marker from a launch where the
            // DirectInput switch left the keyboard dead turns that setting off.
            InputBackend.OnLoad();
            if (SettingsMigration.NeedsHandbrakeSplit(Settings))
            {
                try
                {
                    string path = Path.Combine(modEntry.Path, "Settings.xml");
                    if (File.Exists(path)) File.Copy(path, path + ".pre-ux-" + Guid.NewGuid().ToString("N") + ".bak");
                    SettingsMigration.SplitHandbrake(Settings);
                    MarkSettingsDirty();
                }
                catch (Exception ex) { ModLog.Warning("Handbrake migration deferred; original binding kept: " + ex.Message); }
            }
            WheelInput.LoadBindings();
            FrameHealthPersistence.Initialize();

            modEntry.OnGUI       = OnGUI;
            modEntry.OnSaveGUI   = OnSaveGUI;
            modEntry.OnHideGUI   = entry => { SettingsPanel.CancelPendingEdit(); CameraTuner.SuppressUntilRelease(); FlushUiSettings(true); };
            modEntry.OnToggle    = OnToggle;
            modEntry.OnUnload    = OnUnload;

            FfbNative.Load(modEntry.Path);
            if (Settings.ForceFeedbackEnabled)
            {
                ForceReconnect.Request();
                FfbNative.Waiting();
            }

            try
            {
                _harmony = new Harmony(modEntry.Info.Id);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModLog.Info("Patches applied.");
            }
            catch (Exception ex)
            {
                // Report and keep the game playable rather than taking it down.
                ModLog.Error($"Harmony patching failed: {ex}");
                FfbNative.Shutdown();
                FfbNative.ReleaseInputs();
                _harmony?.UnpatchAll(modEntry.Info.Id);
                return false;
            }

            Enabled = true;

            if (Settings.ShifterEnabled && Settings.ShifterDeviceIndex >= 0)
                Shifter.Open(Settings.ShifterDeviceIndex);

            // Releases the wheel and parks telemetry when the game stops driving
            // or exits, independently of whether any patched object is still
            // ticking. See ModWatchdog.
            ModWatchdog.Install();

            ModLog.Info(
                $"Loaded - directSteering={Settings.DirectSteering}, " +
                $"ffb={Settings.ForceFeedbackEnabled}, telemetry={Settings.TelemetryEnabled}");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            if (value && Settings.ForceFeedbackEnabled && !FfbNative.Ready) ForceReconnect.Request();
            if (!value)
            {
                CameraKeys.Cancel();
                GameBindings.CancelPending();
                // Let go of the wheel and park consumers the moment the player
                // disables the mod, rather than leaving a force applied and a
                // dashboard frozen.
                FfbNative.SetForce(0);
                FfbController.Reset();
                BonnetCamera.Release(true);
                TelemetryPump.Park();
                TelemetryPump.Shutdown();
            }
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            bool changed = GUI.changed; GUI.changed = false;
            int saves = _saveAttempts;
            SettingsPanel.Draw();
            if (GUI.changed && saves == _saveAttempts) MarkSettingsDirty();
            GUI.changed |= changed;
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
            => SaveSettings();

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            Enabled = false;
            CameraKeys.Cancel();
            GameBindings.CancelPending();
            ModWatchdog.Shutdown(unloading: true);
            WheelInput.Close();
            _harmony?.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        /// <summary>
        /// Reopens the force feedback device, e.g. after choosing a different wheel.
        /// </summary>
        public static bool ReopenForceFeedback()
        {
            if (_modEntry == null || Settings == null || !Settings.ForceFeedbackEnabled) return false;
            ForceReconnect.Request();
            FfbNative.Waiting();
            return false; // The idle watchdog performs the actual acquisition.
        }

        internal static void RecoverForceFeedback()
        {
            if (!ForceReconnect.Pending || _modEntry == null || Settings == null) return;
            if (!ForceReconnect.TryBegin(Time.realtimeSinceStartup,
                Enabled && Settings.ForceFeedbackEnabled, GameState.IsDriving,
                WheelInput.Assigning.HasValue, FfbNative.FocusedGameWindow())) return;
            if (!FfbSelection.TryTarget(Settings, out var targetName, out var targetIndex, out var targetGuid, out var reason))
            {
                FfbNative.Shutdown(); FfbNative.SelectionUnavailable(reason);
                ForceReconnect.Cancel(); return;
            }
            // Reader slots may share the old FFB handle. Reopen them after the
            // device switch, and release nonexclusive readers before acquiring FFB.
            WheelInput.Close();
            Shifter.Close();
            try
            {
                ForceReconnect.Complete(FfbNative.Reinitialise(_modEntry.Path,
                    targetName, targetIndex, targetGuid));
                if (!FfbNative.Ready && !ForceReconnect.Pending)
                    ModLog.Warning("FFB recovery exhausted. Pause and select the wheel again to retry.");
            }
            catch (Exception ex) { ModLog.Warning("FFB recovery failed: " + ex.Message); }
            finally
            {
                if (Enabled && Settings.WheelInputEnabled) WheelInput.Open();
                if (Enabled && Settings.ShifterEnabled && Settings.ShifterDeviceIndex >= 0)
                    Shifter.Open(Settings.ShifterDeviceIndex);
            }
        }

        internal static void CancelForceRecovery() => ForceReconnect.Cancel();

        /// <summary>Persists settings changed outside the panel, e.g. by the camera hotkeys.</summary>
        public static bool SaveSettings()
        {
            _saveAttempts++;
            try
            {
                if (Settings == null || _modEntry == null) return false;
                Settings.Save(_modEntry);
                _uiDirty = false; SettingsSaveStatus = "Saved";
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warning($"Could not save settings: {ex.Message}");
                SettingsSaveStatus = "Could not save settings. Check the mod folder is writable; retry while paused.";
                return false;
            }
        }
    }
}
