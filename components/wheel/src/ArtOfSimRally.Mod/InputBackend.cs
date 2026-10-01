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
    /// OUTCOME (2026-09-03): abandoned. Applied from the panel, and again at the
    /// title screen with the switch pre-armed, the switch left the menus dead
    /// while every probe said input was flowing - keyboard controller, player
    /// actions (UISubmit/UICancel/UIHorizontal firing), keyboard maps enabled,
    /// Rewired's UI input module alive. The game's log showed 48,216 stale-object
    /// errors from game code caching Rewired objects (ControllerButtonDisplay,
    /// Arcader); refreshing all of them brought the count to zero and did not
    /// bring the menus back. Whatever else ResetAll() breaks in this game's menu
    /// code was not found. This class stays as an experiment reachable only via
    /// UseDirectInputBackend in Settings.xml; it is not in the panel. The
    /// shipped answer is WheelInput, which bypasses Rewired for the wheel.
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
        private static float _stateAgainAt = -1f;

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
            if (_stateAgainAt > 0f && Time.realtimeSinceStartup >= _stateAgainAt)
            {
                _stateAgainAt = -1f;
                LogState("1 s later");
            }

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
                _stateAgainAt = Time.realtimeSinceStartup + 1f;
                ModLog.Info("UI state after switch:" + UiProbe());
                ReinitUiModule();
                RefreshStaleReferences();
                HookStaleObjectErrors();
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

            bool controllerSaw = false, playerSaw = false;
            try
            {
                var kb = ReInput.controllers.Keyboard;
                controllerSaw = kb != null && kb.GetAnyButtonDown();
                playerSaw = ReInput.players.GetPlayer(0).GetAnyButtonDown();
            }
            catch { }
            ModLog.Info("Input backend probe: key seen by Unity; keyboard controller=" + controllerSaw +
                        " player actions=" + playerSaw + " (" + Describe() + ")" + UiProbe());
            // The game reads actions through the player, so that is the level
            // that decides whether input "works".
            bool rewiredSaw = playerSaw;

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

        // What the menus actually consume: the Unity EventSystem driven by Rewired's
        // input module (Submit/Cancel/Move actions resolved by name), and the
        // game's PanelManager polling action ids 17/28 (back) and 14 (tabs).
        private static string UiProbe()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var p = ReInput.players.GetPlayer(0);
                sb.Append(" | last active=").Append(ReInput.controllers.GetLastActiveControllerType());
                sb.Append(" back17=").Append(p.GetButtonDown(17)).Append(" back28=").Append(p.GetButtonDown(28))
                  .Append(" tab14=").Append(p.GetButtonDown(14) || p.GetNegativeButtonDown(14));
                var es = UnityEngine.EventSystems.EventSystem.current;
                sb.Append(" | eventSystem=").Append(es != null ? (es.enabled ? "on" : "off") : "none");
                if (es != null)
                    sb.Append(" module=").Append(es.currentInputModule != null ? es.currentInputModule.GetType().Name : "none")
                      .Append(" focused=").Append(es.isFocused)
                      .Append(" selected=").Append(es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "none");
                var mod = UnityEngine.Object.FindObjectOfType<Rewired.Integration.UnityUI.RewiredStandaloneInputModule>();
                sb.Append(" | rewiredModule=").Append(mod != null ? (mod.enabled ? "on" : "off") : "none");
                if (mod != null)
                {
                    var t = mod.GetType();
                    var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var ids = t.GetField("playerIds", bf)?.GetValue(mod) as int[];
                    sb.Append(" playerIds=").Append(ids == null ? "null" : string.Join(",", ids.Select(i => i.ToString()).ToArray()));
                    foreach (var f in new[] { "m_SubmitButton", "m_CancelButton", "m_HorizontalAxis", "m_VerticalAxis" })
                    {
                        var name = t.GetField(f, bf)?.GetValue(mod) as string;
                        int id = string.IsNullOrEmpty(name) ? -1 : ReInput.mapping.GetActionId(name);
                        bool down = id >= 0 && (p.GetButtonDown(id) || p.GetNegativeButtonDown(id));
                        sb.Append(' ').Append(f.Substring(2)).Append('=').Append(name ?? "?").Append('#').Append(id).Append(down ? "*" : "");
                    }
                }
            }
            catch (Exception ex) { sb.Append(" | ui probe failed: ").Append(ex.Message); }
            return sb.ToString();
        }

        // Rewired's UI input module clears its player list on ShutDownEvent and
        // refills it on InitializedEvent; ResetAll() raises both. Belt and braces:
        // run its own initialisation again after a switch, and say what it did.
        private static void ReinitUiModule()
        {
            try
            {
                var mod = UnityEngine.Object.FindObjectOfType<Rewired.Integration.UnityUI.RewiredStandaloneInputModule>();
                if (mod == null) { ModLog.Info("UI module: none found"); return; }
                var m = mod.GetType().GetMethod("InitializeRewired",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (m == null) { ModLog.Info("UI module: no InitializeRewired"); return; }
                m.Invoke(mod, null);
                ModLog.Info("UI module re-initialised:" + UiProbe());
            }
            catch (Exception ex) { ModLog.Warning("UI module re-init failed: " + ex.Message); }
        }

        // The game caches Rewired objects in a few places (ControllerButtonDisplay
        // keeps a Player and re-fetches it only when null; Arcader keeps one from
        // Awake). After ResetAll every such object is dead and Rewired logs
        // "created by a previous session ... no longer valid" on each access.
        private static void RefreshStaleReferences()
        {
            int nulled = 0;
            try
            {
                var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                foreach (var d in Resources.FindObjectsOfTypeAll<ControllerButtonDisplay>())
                {
                    var t = typeof(ControllerButtonDisplay);
                    foreach (var name in new[] { "player", "activeController", "lastController", "currentControllerGlyphType" })
                    {
                        var f = t.GetField(name, bf);
                        if (f != null && f.GetValue(d) != null) { f.SetValue(d, null); nulled++; }
                    }
                }
                foreach (var a in Resources.FindObjectsOfTypeAll<Arcader>())
                {
                    var f = typeof(Arcader).GetField("player", bf);
                    if (f != null) { f.SetValue(a, ReInput.players.GetPlayer(0)); nulled++; }
                }
                ModLog.Info("Stale Rewired references refreshed: " + nulled);
            }
            catch (Exception ex) { ModLog.Warning("Refreshing stale references failed: " + ex.Message); }
        }

        // Rewired's stale-object error carries no stack trace in the player log.
        // Capture the managed stack for the first few so the holder can be named.
        private static int _staleTraces;
        private static bool _hooked;
        private static void HookStaleObjectErrors()
        {
            if (_hooked) return;
            _hooked = true;
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (_staleTraces >= 4 || msg == null || !msg.StartsWith("Rewired: [ERROR] You are attemping")) return;
                _staleTraces++;
                string trace = new System.Diagnostics.StackTrace(1, false).ToString();
                ModLog.Warning("Stale Rewired object access #" + _staleTraces + ": " + trace);
            };
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
                  .Append(" joyMaps=").Append(p.controllers.maps.GetAllMaps(ControllerType.Joystick).Count())
                  .Append(" hasKeyboard=").Append(p.controllers.hasKeyboard)
                  .Append(" mapEnabler=").Append(p.controllers.maps.mapEnabler != null && p.controllers.maps.mapEnabler.enabled);
                foreach (var m in p.controllers.maps.GetAllMaps(ControllerType.Keyboard))
                    sb.Append(" | kb map cat=").Append(m.categoryId).Append(" layout=").Append(m.layoutId)
                      .Append(" enabled=").Append(m.enabled).Append(" buttons=").Append(m.buttonMapCount);
                foreach (var j in ReInput.controllers.Joysticks)
                    sb.Append(" | ").Append(j.name).Append(" [").Append(j.hardwareIdentifier).Append("]");
                ModLog.Info(sb.ToString());
            }
            catch (Exception ex) { ModLog.Warning("Input backend state (" + tag + "): " + ex.Message); }
        }
    }
}
