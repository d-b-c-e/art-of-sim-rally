using System;
using System.IO;
using System.Linq;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Optional switch of Rewired's Windows input backend from Raw Input to
    /// DirectInput, for devices Raw Input cannot see or cannot read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rewired's Raw Input backend parses HID reports itself. Two failure modes
    /// were observed: devices it never lists (shifters and stalks, which do not
    /// report as joysticks), and devices it lists but cannot read - a Fanatec
    /// direct-drive base appears twice as "FANATEC Wheel" with identical
    /// hardware ids and 32 axes / 144 buttons, Rewired's ceiling for an
    /// unparsed descriptor, and the controls screen never sees an element move.
    /// DirectInput uses the Windows HID parser and reads both correctly.
    /// </para>
    /// <para>
    /// History matters here. A first version of this switch (2026-09-01) applied
    /// it during mod load and killed the keyboard with no in-game way back. The
    /// setter calls Rewired's <c>ResetAll()</c> - a full teardown and rebuild of
    /// controllers, assignments and maps - and doing that in the middle of the
    /// game's own initialisation is the one thing this version never does.
    /// Verified 2026-09-02 on this machine: applied after the title screen is
    /// up, keyboard and mouse survive, saved keyboard maps survive, DirectInput
    /// enumerates three devices where Raw Input saw one, and the reverse switch
    /// works in-process.
    /// </para>
    /// <para>
    /// The verification is not trusted blindly. Every keyboard press is checked
    /// through <c>UnityEngine.Input</c> and through Rewired separately; three
    /// presses Rewired misses put the backend back to Raw Input and turn the
    /// setting off. A marker file covers the case where nothing reaches Unity
    /// either: it is written on switching and cleared by the first keypress
    /// Rewired sees, so a launch that finds it still there turns the setting off
    /// before applying it again.
    /// </para>
    /// <para>
    /// Saved joystick bindings are keyed by a hardware id that begins with the
    /// backend name, so the wheel has to be bound again after switching - once,
    /// because the Raw Input bindings come back if the setting is turned off.
    /// </para>
    /// </remarks>
    internal static class InputBackend
    {
        /// <summary>Seconds after Rewired reports ready before a switch is allowed.</summary>
        private const float SettleSeconds = 8f;
        private const int BlindPressesBeforeRevert = 3;

        /// <summary>One line for the settings panel: backend in use and the last event.</summary>
        public static string Status { get; private set; } = "";

        private static float _readyAt = -1f;
        private static bool _switchedByUs;
        private static bool _keySeenSinceSwitch;
        private static int _blindPresses;

        private static string MarkerPath => Path.Combine(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArtOfSimRally"),
            "directinput-pending");

        /// <summary>
        /// Called once at mod load, before anything is applied: a marker left by the
        /// previous launch means the switch was made and no keypress ever reached
        /// Rewired afterwards, so the setting is turned off rather than repeated.
        /// </summary>
        public static void OnLoad()
        {
            var cfg = Main.Settings;
            if (cfg == null) return;
            if (!File.Exists(MarkerPath)) return;
            DeleteMarker();
            if (!cfg.UseDirectInputBackend) return;
            cfg.UseDirectInputBackend = false;
            Main.SaveSettings();
            Status = "Turned off: last launch switched to DirectInput and no keypress reached the game afterwards.";
            ModLog.Error("DirectInput backend: " + Status);
        }

        /// <summary>Called every frame by the watchdog.</summary>
        public static void Tick()
        {
            var cfg = Main.Settings;
            if (cfg == null) return;
            if (!ReInput.isReady) return;
            if (_readyAt < 0f) _readyAt = Time.realtimeSinceStartup;

            ProbeKeyboard(cfg);

            bool wantDirect = cfg.UseDirectInputBackend;
            bool isDirect = Current() == Rewired.Platforms.WindowsStandalonePrimaryInputSource.DirectInput;
            if (wantDirect == isDirect) return;

            // Never inside the game's own initialisation - see remarks.
            if (wantDirect && Time.realtimeSinceStartup - _readyAt < SettleSeconds) return;

            Apply(wantDirect, wantDirect ? "enabled in settings" : "disabled in settings");
        }

        private static void Apply(bool direct, string reason)
        {
            var target = direct
                ? Rewired.Platforms.WindowsStandalonePrimaryInputSource.DirectInput
                : Rewired.Platforms.WindowsStandalonePrimaryInputSource.RawInput;
            try
            {
                LogState("before switch to " + target);
                if (direct)
                {
                    // Marker first: if the reset takes the process down, the next
                    // launch must know the switch was attempted.
                    Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath));
                    File.WriteAllText(MarkerPath, DateTime.Now.ToString("s"));
                }
                ReInput.configuration.windowsStandalonePrimaryInputSource = target;
                _switchedByUs = direct;
                _keySeenSinceSwitch = false;
                _blindPresses = 0;
                if (!direct) DeleteMarker();
                LogState("after switch to " + target + " (" + reason + ")");
                Status = "Backend now " + Describe() + " - " + reason + ".";
                if (direct) Status += " Bind your wheel in the game's controls screen.";
            }
            catch (Exception ex)
            {
                ModLog.Error("DirectInput backend: switch to " + target + " failed: " + ex.Message);
                Status = "Switch failed: " + ex.Message;
                DeleteMarker();
            }
        }

        // Keyboard only. Unity's anyKeyDown is also true for mouse buttons, which
        // Rewired's keyboard controller naturally never reports - counting those
        // as blind presses reverted the backend on a mouse click during testing.
        private static void ProbeKeyboard(Settings cfg)
        {
            if (!Input.anyKeyDown) return;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return;
            if (!_switchedByUs) return;

            bool rewiredSaw = false;
            try
            {
                var kb = ReInput.controllers.Keyboard;
                rewiredSaw = kb != null && kb.GetAnyButtonDown();
            }
            catch { }

            if (rewiredSaw)
            {
                _blindPresses = 0;
                if (_keySeenSinceSwitch) return;
                _keySeenSinceSwitch = true;
                DeleteMarker();
                ModLog.Info("DirectInput backend: keyboard confirmed reaching the game.");
                return;
            }

            _blindPresses++;
            ModLog.Warning("DirectInput backend: keypress seen by Unity but not by Rewired (" +
                           _blindPresses + "/" + BlindPressesBeforeRevert + ")");
            if (_blindPresses < BlindPressesBeforeRevert) return;

            cfg.UseDirectInputBackend = false;
            Main.SaveSettings();
            Apply(false, "keyboard stopped reaching the game, turned off automatically");
        }

        private static Rewired.Platforms.WindowsStandalonePrimaryInputSource Current()
        {
            try { return ReInput.configuration.windowsStandalonePrimaryInputSource; }
            catch { return Rewired.Platforms.WindowsStandalonePrimaryInputSource.RawInput; }
        }

        /// <summary>"DirectInput (3 controllers)" - for the panel and the support bundle.</summary>
        public static string Describe()
        {
            try
            {
                string name = Current() == Rewired.Platforms.WindowsStandalonePrimaryInputSource.DirectInput
                    ? "DirectInput" : "Raw Input";
                int n = ReInput.isReady ? ReInput.controllers.joystickCount : 0;
                return name + " (" + n + (n == 1 ? " controller)" : " controllers)");
            }
            catch { return "unknown"; }
        }

        private static void DeleteMarker()
        {
            try { if (File.Exists(MarkerPath)) File.Delete(MarkerPath); }
            catch { /* a stale marker costs one automatic turn-off, nothing worse */ }
        }

        private static void LogState(string tag)
        {
            try
            {
                var c = ReInput.configuration;
                var p = ReInput.players.GetPlayer(0);
                var sb = new System.Text.StringBuilder();
                sb.Append("Input backend [").Append(tag).Append("]: source=").Append(c.windowsStandalonePrimaryInputSource)
                  .Append(" keyboard=").Append(ReInput.controllers.Keyboard != null)
                  .Append(" joysticks=").Append(ReInput.controllers.joystickCount)
                  .Append(" assigned=").Append(p.controllers.joystickCount)
                  .Append(" kbMaps=").Append(p.controllers.maps.GetAllMaps(ControllerType.Keyboard).Count())
                  .Append(" joyMaps=").Append(p.controllers.maps.GetAllMaps(ControllerType.Joystick).Count());
                foreach (var j in ReInput.controllers.Joysticks)
                    sb.Append(" | ").Append(j.name).Append(" [").Append(j.hardwareIdentifier).Append("]");
                ModLog.Info(sb.ToString());
            }
            catch (Exception ex) { ModLog.Warning("Input backend state (" + tag + "): " + ex.Message); }
        }
    }
}
