using System;
using System.IO;
using System.Reflection;
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
    public static class Main
    {
        internal static Settings Settings { get; private set; }
        internal static bool Enabled { get; private set; }

        private static Harmony _harmony;
        private static UnityModManager.ModEntry _modEntry;

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

            modEntry.OnGUI       = OnGUI;
            modEntry.OnSaveGUI   = OnSaveGUI;
            modEntry.OnToggle    = OnToggle;
            modEntry.OnUnload    = OnUnload;

            if (Settings.ForceFeedbackEnabled)
                // modEntry.Path IS the mod folder; do not take its parent.
                FfbNative.Initialise(modEntry.Path,
                                     Settings.PreferredDevice, Settings.PreferredDeviceIndex);

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
            if (!value)
            {
                // Let go of the wheel and park consumers the moment the player
                // disables the mod, rather than leaving a force applied and a
                // dashboard frozen.
                FfbNative.SetForce(0);
                TelemetryPump.Park();
                TelemetryPump.Shutdown();
            }
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry) => SettingsPanel.Draw();

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
            => Settings.Save(modEntry);

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            ModWatchdog.Shutdown();
            _harmony?.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        /// <summary>
        /// Reopens the force feedback device, e.g. after choosing a different wheel.
        /// </summary>
        public static bool ReopenForceFeedback()
        {
            if (_modEntry == null || Settings == null || !Settings.ForceFeedbackEnabled) return false;
            return FfbNative.Reinitialise(_modEntry.Path,
                                          Settings.PreferredDevice, Settings.PreferredDeviceIndex);
        }

        /// <summary>Persists settings changed outside the panel, e.g. by the camera hotkeys.</summary>
        public static void SaveSettings()
        {
            try { Settings.Save(_modEntry); }
            catch (Exception ex) { ModLog.Warning($"Could not save settings: {ex.Message}"); }
        }
    }
}
