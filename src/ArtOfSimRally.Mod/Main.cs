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

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);

            GUILayout.Space(12);
            DrawInputStatus();

            GUILayout.Space(12);
            DrawDevicePicker();

            GUILayout.Space(12);
            GUILayout.Label("<b>Having trouble?</b>");

            // Long explanations live here rather than in [Draw] tooltips. UMM
            // renders a tooltip to the left of its "?" marker with no option to
            // change side, so anything more than a few words runs off the panel
            // and is unreadable. Visible wrapped text has no such limit and does
            // not need hovering to find.
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label(
                "Wheel too light or too strong? Move the strength slider. Everything else " +
                "is under 'Show advanced options'.", wrap);
            GUILayout.Label(
                "Shifter or handbrake missing from the controls screen? The game's input " +
                "layer skips devices that do not report as a joystick, which is common for " +
                "shifters. Turn on 'Use DirectInput for controllers' under advanced, restart, " +
                "and rebind - it sees more devices, but it does mean rebinding.", wrap);

            GUILayout.Space(6);
            if (GUILayout.Button("Create support file on Desktop", GUILayout.Width(260)))
                SupportBundle.Create();

            if (!string.IsNullOrEmpty(SupportBundle.LastResult))
                GUILayout.Label(SupportBundle.LastResult, wrap);
            else
                GUILayout.Label(
                    "Collects your settings, the force feedback log and the game's log into one " +
                    "file to attach to a bug report.", wrap);
        }

        /// <summary>
        /// Shows what the game's input layer can actually see.
        /// </summary>
        /// <remarks>
        /// Added because "I ticked the DirectInput box and the shifter still is
        /// not there" had two possible causes that looked identical from the
        /// panel: the setting only applies at startup, and the backend may not
        /// enumerate the device at all. Showing the live backend and joystick
        /// count separates them without reading a log.
        /// </remarks>
        private static void DrawInputStatus()
        {
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("<b>Game controller input</b>");

            try
            {
                if (!ReInput.isReady) { GUILayout.Label("Input system not started yet.", wrap); return; }

                var joysticks = ReInput.controllers.Joysticks;
                var backend = ReInput.configuration.windowsStandalonePrimaryInputSource;

                GUILayout.Label("Backend: " + backend + "    Devices seen: " +
                                (joysticks == null ? 0 : joysticks.Count), wrap);

                if (joysticks != null)
                    foreach (var j in joysticks)
                        GUILayout.Label("    " + j.name +
                                        (j.hardwareTypeGuid == Guid.Empty ? "   (not recognised)" : ""),
                                        wrap);

                // The setting is applied during Load, so ticking it now changes
                // nothing until the game is restarted. Say so, rather than letting
                // it look broken.
                if (joysticks != null && joysticks.Count < 2)
                    GUILayout.Label("Only one device here. A shifter or handbrake missing from this " +
                                    "list cannot be bound - the game's input layer skips devices " +
                                    "that do not report as a joystick, which is common for them.",
                                    wrap);
            }
            catch (Exception ex)
            {
                GUILayout.Label("Could not read input state: " + ex.Message, wrap);
            }
        }

        // Device names are not unique - a Fanatec rig reports two identical
        // "FANATEC Wheel" entries - so the picker stores the index as well and
        // shows the position, letting a user tell two same-named devices apart by
        // trying each. Cached because enumerating on every OnGUI frame would hit
        // DirectInput sixty times a second.
        private static string[] _devices;
        private static bool _devicesListed;

        private static void DrawDevicePicker()
        {
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("<b>Force feedback device</b>");

            if (!_devicesListed)
            {
                _devices = FfbNative.ListDevices();
                _devicesListed = true;
            }

            if (_devices == null || _devices.Length == 0)
            {
                GUILayout.Label("No force-feedback device found. Check the wheel is on and " +
                                "not in use by another program.", wrap);
                if (GUILayout.Button("Look again", GUILayout.Width(140))) _devicesListed = false;
                return;
            }

            if (_devices.Length == 1)
            {
                GUILayout.Label("Using: " + _devices[0], wrap);
                Settings.PreferredDevice = _devices[0];
                Settings.PreferredDeviceIndex = -1;
                return;
            }

            GUILayout.Label("You have more than one. Pick your wheel:", wrap);
            for (int i = 0; i < _devices.Length; i++)
            {
                bool chosen = Settings.PreferredDeviceIndex == i;
                bool now = GUILayout.Toggle(chosen, "  " + _devices[i] + "   (device " + i + ")");
                if (now && !chosen)
                {
                    Settings.PreferredDeviceIndex = i;
                    Settings.PreferredDevice = _devices[i];
                    SaveSettings();
                    ModLog.Info("Force feedback device set to [" + i + "] " + _devices[i]);
                }
            }
            GUILayout.Label("Takes effect next time you start the game.", wrap);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
            => Settings.Save(modEntry);

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            ModWatchdog.Shutdown();
            _harmony?.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        /// <summary>Persists settings changed outside the panel, e.g. by the camera hotkeys.</summary>
        internal static void SaveSettings()
        {
            try { Settings.Save(_modEntry); }
            catch (Exception ex) { ModLog.Warning($"Could not save settings: {ex.Message}"); }
        }
    }
}
