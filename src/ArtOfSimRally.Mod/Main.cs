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

            ApplyInputBackend();

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

        /// <summary>
        /// Optionally switches Rewired from Raw Input to DirectInput.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Rewired's Raw Input backend enumerates by HID usage and skips devices
        /// that do not present as a joystick or gamepad. A DS-8X sequential
        /// shifter and a MOZA Multi-function Stalk both report as supplemental
        /// devices: DirectInput lists them, Raw Input does not, and the game shows
        /// "found 1 joysticks attached" while joy.cpl shows three. A device the
        /// input layer never sees cannot be bound by any means.
        /// </para>
        /// <para>
        /// Off by default because it is not free: Rewired keys saved bindings by a
        /// hardware identifier that begins with the backend name, so switching
        /// invalidates every existing binding and the player has to redo them.
        /// That is a bad surprise to inflict on someone whose wheel already works.
        /// </para>
        /// </remarks>
        private static void ApplyInputBackend()
        {
            if (Settings == null || !Settings.UseDirectInput) return;

            try
            {
                if (ReInput.isReady)
                {
                    // Already enumerated; the setting cannot take effect until the
                    // next launch. Say so rather than appearing to do nothing.
                    ModLog.Warning("Rewired already started - DirectInput takes effect next launch.");
                }

                ReInput.configuration.windowsStandalonePrimaryInputSource =
                    Rewired.Platforms.WindowsStandalonePrimaryInputSource.DirectInput;
                ModLog.Info("Controller backend set to DirectInput. Rebinding will be required.");
            }
            catch (Exception ex)
            {
                ModLog.Error("Could not switch to DirectInput: " + ex.Message);
            }
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
