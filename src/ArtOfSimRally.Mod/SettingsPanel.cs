using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class SettingsPanel
    {
        private static GUIStyle _wrap, _help;
        private static Vector2 _scroll;
        private static bool _shifter, _cameraKeys, _clutch, _settingsKey;
        private static float _keyDeadline;
        private static string _keyStatus = "";
        private static int _mount;
        private static readonly ConnectionEdit Connection = new ConnectionEdit();
        internal static bool Editing => WheelInput.Assigning.HasValue || CameraKeys.Listening >= 0 ||
            Panel.BindingActive || Connection.Editing || _settingsKey;
        internal static bool CancelPendingEdit()
        {
            bool editing = Editing;
            WheelInput.CancelAssign(); CameraKeys.Cancel(); Panel.CancelBinding(); Connection.Cancel(); _settingsKey = false;
            DeviceDropdown.CloseAll();
            return editing;
        }
        public static void Draw()
        {
            if (Main.Settings == null) return;
            using (new SettingsPresentation()) DrawContent();
        }
        private static void DrawContent()
        {
            var c = Main.Settings;
            if (c == null) return;
            _wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _help = new GUIStyle(_wrap);
            _help.normal.textColor = new Color(.72f, .72f, .72f);
            HandleKey(c);
            GUILayout.Label("Wheel settings", new GUIStyle(_wrap) { fontStyle = FontStyle.Bold });
            GUILayout.BeginHorizontal();
            GUILayout.Label("View:", SettingsPresentation.Width(45));
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !Editing;
            bool advanced = SettingsViewPolicy.Advanced(c);
            int view = GUILayout.Toolbar(advanced ? 1 : 0, new[] { "Simple", "Advanced" }, SettingsPresentation.Width(210));
            if (view != (advanced ? 1 : 0)) Select(c, view == 1, SettingsViewPolicy.Page(c));
            GUI.enabled = wasEnabled;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Stop FFB (F8)")) Main.StopFeedback();
            if (GUILayout.Button("Close")) Main.CloseSettings();
            GUILayout.EndHorizontal();
            if (Editing) Help("Finish or cancel the current edit to change view.");
            GUILayout.Label(Editing ? "Edit in progress — pending calibration/connection is not saved. " + Main.SettingsSaveStatus : Main.SettingsSaveStatus, _wrap);
            if (!c.ForceFeedbackEnabled) Help("FFB off — choose On in FFB when ready.");
            else if (!FfbNative.Ready) Help("FFB unavailable — " + FfbNative.Status);
            GUI.enabled = wasEnabled && !Editing;
            int page = GUILayout.Toolbar(SettingsViewPolicy.Page(c), SettingsViewPolicy.Pages);
            if (page != SettingsViewPolicy.Page(c)) Select(c, advanced, page);
            GUI.enabled = wasEnabled;
            // Header stays outside our page scroll; explicit host sizes win.
            float height = SettingsPresentation.PageHeight;
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(height));
            switch (SettingsViewPolicy.Page(c))
            {
                case 0: Setup(c); break;
                case 1: Controls(c); break;
                case 2: Feedback(c); break;
                case 3: Cameras(c); break;
                case 4: Telemetry(c); break;
                case 5: Support(c); break;
            }
            GUILayout.EndScrollView();
        }
        private static void Select(Settings c, bool advanced, int page)
        {
            if (!SettingsViewPolicy.Select(c, advanced, page, Editing)) return;
            _scroll = Vector2.zero; DeviceDropdown.CloseAll(); GUIUtility.keyboardControl = 0; Main.MarkSettingsDirty();
        }
        private static void HandleKey(Settings c)
        {
            if (_settingsKey && Time.unscaledTime > _keyDeadline)
            { _settingsKey = false; _keyStatus = "Binding timed out. Previous Settings key kept."; }
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Escape && Editing)
            { CancelPendingEdit(); Main.SuppressHostClose = true; e.Use(); return; }
            if (CameraKeys.HandleKey(c, e.keyCode, e.shift || e.control || e.alt || e.command)) { e.Use(); return; }
            if (!_settingsKey) return;
            _keyStatus = "Use a single keyboard key; F8/F10 and camera adjustment keys are reserved.";
            if (!e.shift && !e.control && !e.alt && !e.command && CameraKeys.IsKeyboardKey(e.keyCode) &&
                e.keyCode != KeyCode.F8 && e.keyCode != KeyCode.F10 && !CameraKeys.ModifierHeld())
            {
                bool conflict = false;
                foreach (var key in CameraKeys.Bindings) if (key.Get(c) == e.keyCode) conflict = true;
                if (!conflict && !NativeKeyboardBindings.Available(e.keyCode, out string reason))
                { conflict = true; _keyStatus = reason; }
                if (!conflict)
                {
                    var previous = c.SettingsKey;
                    if (SettingsCommit.TrySave(() => c.SettingsKey = e.keyCode, () => c.SettingsKey = previous))
                    { _settingsKey = false; _keyStatus = "Settings key saved."; }
                    else _keyStatus = "Could not save. Previous Settings key kept; pause and check Settings.xml is writable.";
                }
            }
            e.Use();
        }
        private static void Setup(Settings c)
        {
            GUILayout.Label("Connect → axes → buttons → check FFB → drive", _wrap);
            Help("Pause before binding or calibrating. Keep working game controls; add separate devices here as needed.");
            Bar(WheelInput.Channel.Steer, "Steering"); Bar(WheelInput.Channel.Throttle, "Throttle"); Bar(WheelInput.Channel.Brake, "Brake");
            bool complete = c.WheelInputEnabled && WheelInput.IsBound(WheelInput.Channel.Steer) &&
                WheelInput.IsBound(WheelInput.Channel.Throttle) && WheelInput.IsBound(WheelInput.Channel.Brake);
            GUILayout.Label(complete ? "Axes assigned — verify the bars, desired buttons and FFB before driving." :
                "Next: check steering and pedals. Bind any controls the game cannot read.", _wrap);
            if (GUILayout.Button("Open Controls")) Select(c, SettingsViewPolicy.Advanced(c), 1);
            if (GUILayout.Button("Check FFB")) Select(c, SettingsViewPolicy.Advanced(c), 2);
            Help("Handbrake, shifter, cameras and telemetry are optional. Bars show device input, not measured car behavior.");
        }
        private static void Controls(Settings c)
        {
            bool active = Toggle(c.WheelInputEnabled, "Use assigned controls");
            if (active != c.WheelInputEnabled) { c.WheelInputEnabled = active; if (!active) WheelInput.CancelAssign(); }
            Help("Unbound controls keep the game's bindings. Release/centre before Bind; complete full travel and release before Save calibration.");
            Axis(c, WheelInput.Channel.Steer, "Steering"); Axis(c, WheelInput.Channel.Throttle, "Throttle");
            Axis(c, WheelInput.Channel.Brake, "Brake"); Axis(c, WheelInput.Channel.Handbrake, "Handbrake (axis)");
            Axis(c, WheelInput.Channel.HandbrakeButton, "Handbrake (button)");
            Help("Handbrake axis, button and game controls use the greater value; a held button contributes 100%.");
            _clutch = GUILayout.Toggle(_clutch, "Clutch");
            if (_clutch) Axis(c, WheelInput.Channel.Clutch, "Clutch");
            if (!WheelInput.Assigning.HasValue) Help(WheelInput.Status);
            _shifter = GUILayout.Toggle(_shifter, "Shifter bindings");
            if (_shifter)
            {
                c.ShifterEnabled = Toggle(c.ShifterEnabled, "Separate shifter");
                if (c.ShifterEnabled) Panel.DrawShifterBinding(c);
                if (!c.ShifterIsHPattern) c.SkipNeutral = Toggle(c.SkipNeutral, "Skip neutral");
            }
            GUILayout.Label("Driving and menu buttons", _wrap);
            Help("The game's binding screen owns Shift up/down, Change camera, held Look behind, Reset car, Pause and menu controls. It preserves keyboard/pad action maps. Settings/Stop FFB buttons below read USB devices directly.");
            if (GUILayout.Button("Open game bindings")) GameBindings.Open();
            Help(GameBindings.Status);
            GUILayout.Label("Mod buttons", _wrap);
            if (GUILayout.Button(_settingsKey ? "Press a Settings key (Esc cancels)" : "Settings: " + CameraKeys.Name(c.SettingsKey) + " — Bind"))
            { if (!Editing) { _settingsKey = true; _keyStatus = "Press a key within 10 seconds. Escape cancels."; _keyDeadline = Time.unscaledTime + 10; } }
            Help(_keyStatus);
            Axis(c, WheelInput.Channel.SettingsButton, "Settings (button)");
            Axis(c, WheelInput.Channel.StopFfbButton, "Stop FFB (button)");
            Help("F8 always stops FFB. These optional device buttons work even with assigned driving controls Off. Release held buttons after reconnecting.");
            if (!SettingsViewPolicy.Advanced(c) && SettingsViewPolicy.CustomControls(c) &&
                GUILayout.Button("Custom control tuning active — Review in Advanced")) Select(c, true, 1);
            if (SettingsViewPolicy.Advanced(c))
            {
                GUILayout.Label("Steering compatibility", _wrap);
                c.DirectSteering = Toggle(c.DirectSteering, "Direct steering");
                c.ZeroAxisDeadzone = Toggle(c.ZeroAxisDeadzone, "Remove hidden deadzone");
                c.BindAnyDevice = Toggle(c.BindAnyDevice, "Bind whichever device you touch");
                c.GlyphTextFallback = Toggle(c.GlyphTextFallback, "Show button names without icons");
                c.DisableSteerAssist = Toggle(c.DisableSteerAssist, "Legacy steering limiter override");
                Help("The legacy override affects car behavior on spawn, not the game's numeric assist setting. Keep Off and use the game's assist controls.");
            }
        }
        private static void Axis(Settings c, WheelInput.Channel channel, string label)
        {
            GUILayout.Space(6); GUILayout.Label(label + ": " + WheelInput.Describe(channel), _wrap);
            Bar(channel, "Device input");
            if (!WheelInput.IsButtonChannel(channel) && WheelInput.IsBound(channel))
                Help(WheelInput.CalibrationDescription(channel));
            bool enabled = GUI.enabled; GUI.enabled = enabled && !Editing;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bind")) WheelInput.BeginCalibration(channel);
            GUI.enabled = enabled && !Editing && WheelInput.IsBound(channel);
            if (!WheelInput.IsButtonChannel(channel) && GUILayout.Button("Calibrate")) WheelInput.BeginCalibration(channel);
            if (GUILayout.Button("Clear"))
            { if (WheelInput.Clear(channel) && channel == WheelInput.Channel.Steer && FfbSelection.FollowsSteering(c)) Main.SelectForceDevice(); }
            GUILayout.EndHorizontal(); GUI.enabled = enabled;
            if (WheelInput.Assigning == channel) CalibrationEditor(c);
        }
        private static void CalibrationEditor(Settings c)
        {
                GUILayout.Label(WheelInput.Status, _wrap);
                var pending = WheelInput.PendingCalibration;
                if (pending != null)
                {
                    GUILayout.Label("Device input: " + (WheelInput.CalibrationValue * 100).ToString("F0") + "%", _wrap);
                    if (!pending.IsButton)
                    {
                        pending.Inverted = Toggle(pending.Inverted, "Invert " + (WheelInput.Assigning == WheelInput.Channel.Handbrake ? "handbrake" : "axis"));
                        pending.Deadzone = Slider(pending.Deadzone, 0, .1f, 0, "Deadzone", 100, "%");
                    }
                }
                GUILayout.BeginHorizontal(); bool enabled = GUI.enabled;
                GUI.enabled = enabled && WheelInput.CanSaveCalibration;
                if (GUILayout.Button(pending != null && pending.IsButton ? "Save binding" : "Save calibration"))
                {
                    string previous = c.SteerBinding;
                    if (WheelInput.SaveCalibration() && previous != c.SteerBinding && FfbSelection.FollowsSteering(c)) Main.SelectForceDevice();
                }
                GUI.enabled = enabled;
                if (GUILayout.Button("Cancel")) WheelInput.CancelAssign();
                GUILayout.EndHorizontal();

        }
        private static void Bar(WheelInput.Channel channel, string label)
        {
            float value = WheelInput.Value(channel);
            string text = !WheelInput.IsBound(channel) ? "Game controls / not bound here" : channel == WheelInput.Channel.Steer
                ? Math.Abs(value) < .005f ? "Centre" : (value < 0 ? "Left " : "Right ") + (Math.Abs(value) * 100).ToString("F0") + "%"
                : (value * 100).ToString("F0") + "%";
            GUILayout.Label(label + ": " + text, _wrap);
            if (!WheelInput.IsBound(channel)) return;
            var rect = GUILayoutUtility.GetRect(20, 14, GUILayout.ExpandWidth(true));
            var color = GUI.color;
            GUI.color = new Color(.2f, .2f, .2f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            var fill = rect;
            bool steering = channel == WheelInput.Channel.Steer;
            float travel = Math.Max(0, Math.Min(1, Math.Abs(value)));
            fill.width *= steering ? travel * .5f : travel;
            if (steering) fill.x += rect.width * .5f - (value < 0 ? fill.width : 0);
            GUI.color = new Color(.3f, .75f, .65f); GUI.DrawTexture(fill, Texture2D.whiteTexture);
            if (steering)
            {
                var centre = new Rect(rect.x + rect.width * .5f, rect.y, 1, rect.height);
                GUI.color = Color.white; GUI.DrawTexture(centre, Texture2D.whiteTexture);
            }
            GUI.color = color;
        }
        private static void Feedback(Settings c)
        {
            bool enabled = Toggle(c.ForceFeedbackEnabled, "FFB");
            if (enabled != c.ForceFeedbackEnabled) Main.SetFeedbackEnabled(enabled);
            Panel.DrawWheelPicker();
            c.Strength = (int)Slider(c.Strength, 0, 100, 50, "Strength", 1, "%");
            Help(!c.ForceFeedbackEnabled ? "Off — choose On when ready." : !FfbNative.Ready ? FfbNative.Status :
                "Inactive while settings are open. Feedback resumes through normal driving gates.");
            if (!SettingsViewPolicy.Advanced(c))
            {
                if (SettingsViewPolicy.CustomFfb(c) && GUILayout.Button("Custom FFB tuning active — Review in Advanced")) Select(c, true, 2);
                return;
            }
            c.Smoothing = Slider(c.Smoothing, 0, .95f, .2f, "Smoothing", 100, "%");
            Help("Higher smoothing softens rapid force changes and delays their response.");
            c.Invert = Toggle(c.Invert, "Invert force direction");
            Help("Force reference: " + c.FyReference.ToString("0.##") + " N (legacy tuning; default 11500 N).");
            if (c.FyReference != 11500f && GUILayout.Button("Restore default force reference")) { c.FyReference = 11500f; Main.MarkSettingsDirty(); }
            c.LandingEffectsEnabled = Toggle(c.LandingEffectsEnabled, "Landing vibration");
            c.LandingStrength = Slider(c.LandingStrength, 0, 40, 5, "Landing strength", 1, "%");
            Help(LandingController.Status);
            c.CrashEffectsEnabled = Toggle(c.CrashEffectsEnabled, "Crash kick (experimental)");
            c.CrashStrength = Slider(c.CrashStrength, 0, 100, 50, "Crash strength", 1, "%");
            Help("Short constant push/release, separate from steering and telemetry. Full nominal force can saturate alongside steering. " + CrashController.Status);
            if (GUILayout.Button("Reset FFB tuning")) { c.ResetFfbTuning(); Main.MarkSettingsDirty(); }
            Help("Restores the displayed force tuning. Keeps FFB Off/On, the device and all other settings.");
        }
        private static void Cameras(Settings c)
        {
            if (Main.OtherCameraModLoaded) { Help(BonnetCamera.ExternalCameraHelp); return; }
            c.BonnetCameraEnabled = Toggle(c.BonnetCameraEnabled, "Bonnet");
            c.BumperCameraEnabled = Toggle(c.BumperCameraEnabled, "Bumper");
            Help("Included in the game's Change camera cycle. Change camera and held Look behind use the game's bindings. Close settings to adjust the active mount.");
            if (GUILayout.Button("Bind Change camera / Look behind")) GameBindings.Open();
            Help(GameBindings.Status);
            _cameraKeys = GUILayout.Toggle(_cameraKeys, "Adjustment bindings");
            if (_cameraKeys)
            {
                c.CameraTuningKeys = Toggle(c.CameraTuningKeys, "Live adjustment shortcuts");
                Help("Use keyboard keys or separate USB buttons; no numpad required. F8 and the Settings key are reserved. Bind only buttons not already used by the game.");
                for (int i = 0; i < CameraKeys.Bindings.Length; i++)
                {
                    var binding = CameraKeys.Bindings[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(binding.Label + ": " + CameraKeys.Name(binding.Get(c)), _wrap);
                    bool old = GUI.enabled; GUI.enabled = old && (!Editing || CameraKeys.Listening == i);
                    if (GUILayout.Button(CameraKeys.Listening == i ? "Cancel" : "Bind", SettingsPresentation.Width(80)))
                    { if (CameraKeys.Listening == i) CameraKeys.Cancel(); else CameraKeys.Begin(i); }
                    if (GUILayout.Button("Clear", SettingsPresentation.Width(70))) CameraKeys.Clear(c, i);
                    GUI.enabled = old; GUILayout.EndHorizontal();
                    Axis(c, WheelInput.CameraChannel(i), binding.Label + " (button)");
                }
                bool enabled = GUI.enabled; GUI.enabled = enabled && !Editing;
                if (GUILayout.Button("Restore numpad defaults")) CameraKeys.Reset(c);
                GUI.enabled = enabled; Help(CameraKeys.Status);
            }
            if (!SettingsViewPolicy.Advanced(c))
            { if (SettingsViewPolicy.CustomCamera(c) && GUILayout.Button("Custom camera tuning active — Review in Advanced")) Select(c, true, 3); return; }
            _mount = GUILayout.Toolbar(_mount, new[] { "Bonnet pose", "Bumper pose" });
            if (GUILayout.Button("Reset this view (" + (_mount == 0 ? "Bonnet" : "Bumper") + ")")) { c.ResetCameraMount(_mount != 0); Main.MarkSettingsDirty(); }
            if (_mount == 0)
            {
                c.BonnetHeight = Slider(c.BonnetHeight, -.5f, 3, .95f, "Height", 1, " m");
                c.BonnetForward = Slider(c.BonnetForward, -3, 5, 1, "Forward", 1, " m");
                c.BonnetSide = Slider(c.BonnetSide, -2, 2, 0, "Side", 1, " m");
                c.BonnetPitch = Slider(c.BonnetPitch, -45, 45, 3, "Tilt", 1, "°");
                c.BonnetFOV = Slider(c.BonnetFOV, 40, 120, 75, "Field of view", 1, "°");
            }
            else
            {
                c.BumperHeight = Slider(c.BumperHeight, -.5f, 3, .45f, "Height", 1, " m");
                c.BumperForward = Slider(c.BumperForward, -3, 5, 1.9f, "Forward", 1, " m");
                c.BumperSide = Slider(c.BumperSide, -2, 2, 0, "Side", 1, " m");
                c.BumperPitch = Slider(c.BumperPitch, -45, 45, 2, "Tilt", 1, "°");
                c.BumperFOV = Slider(c.BumperFOV, 40, 120, 80, "Field of view", 1, "°");
            }
            c.BonnetLean = Slider(c.BonnetLean, 0, 1, .1f, "Corner lean (both views)", 100, "%");
            c.TuneMoveSpeed = Slider(c.TuneMoveSpeed, .05f, 2, .4f, "Shortcut movement speed", 1, " m/s");
            c.TuneAngleSpeed = Slider(c.TuneAngleSpeed, 1, 60, 20, "Shortcut angle speed", 1, "°/s");
        }
        private static void Telemetry(Settings c)
        {
            c.TelemetryEnabled = Toggle(c.TelemetryEnabled, "Telemetry");
            Help("Forza Horizon 5-compatible UDP. In SimHub choose that receiver and match this destination.");
            GUILayout.Label("Saved destination: " + c.TelemetryHost + ":" + c.TelemetryPort, _wrap);
            Help(!c.TelemetryEnabled ? "Off" : TelemetryPump.ActiveEndpoint == null ? "Unavailable — pause to connect; inspect Help if it fails." :
                "Sending to " + TelemetryPump.ActiveEndpoint + ". UDP does not confirm receiver delivery.");
            if (!SettingsViewPolicy.Advanced(c))
            {
                if (GUILayout.Button("Use local SimHub preset (127.0.0.1:8000)")) { c.TelemetryHost = "127.0.0.1"; c.TelemetryPort = 8000; Main.MarkSettingsDirty(); }
                if (GUILayout.Button("Connection settings (Advanced)")) Select(c, true, 4);
                return;
            }
            if (!Connection.Editing && GUILayout.Button("Edit connection")) Connection.Begin(c);
            if (Connection.Editing)
            {
                GUILayout.Label("Host"); Connection.Host = GUILayout.TextField(Connection.Host ?? "");
                GUILayout.Label("Port"); Connection.Port = GUILayout.TextField(Connection.Port ?? "");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply connection") && Connection.Apply(c)) Main.MarkSettingsDirty();
                if (GUILayout.Button("Cancel")) Connection.Cancel();
                GUILayout.EndHorizontal(); Help(Connection.Error);
            }
            Help("Connection applies atomically while paused. Recording remains a separate development probe and is not shipped in this panel.");
        }
        private static void Support(Settings c)
        {
            GUILayout.Label("Art of Sim Rally " + Main.ModVersion, _wrap);
            Help("No FFB: pause, open FFB, check On and the selected wheel, then Refresh. Missing controls: bind them in Controls.");
            GUILayout.Label("Settings text size", _wrap);
            int textMode = GUILayout.Toolbar(c.SettingsFollowHostScale ? 1 : 0, new[] { "Auto (screen size)", "Use UMM scale" });
            c.SettingsFollowHostScale = textMode == 1;
            Help("Auto enlarges this mod's content on high-resolution screens at UMM's default scale. A custom UMM scale takes priority. The surrounding UMM window keeps its own preferences; use UMM Settings to resize it.");
            if (GUILayout.Button("Create support file on Desktop")) SupportBundle.Create();
            Help(string.IsNullOrEmpty(SupportBundle.LastResult) ? "Creates a local file with settings, device identifiers, paths and logs; nothing is uploaded." : SupportBundle.LastResult);
            if (!SettingsViewPolicy.Advanced(c))
            { if (GUILayout.Button("Details (Advanced)")) Select(c, true, 5); return; }
            c.DiagnosticLogging = Toggle(c.DiagnosticLogging, "Log detail for support");
            Help("Enable, reproduce briefly, pause, create the support file, then turn detail logging off.");
            Panel.DrawInputStatus();
        }
        private static bool Toggle(bool value, string label)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label + ": " + (value ? "On" : "Off"), _wrap);
            int result = GUILayout.Toolbar(value ? 1 : 0, new[] { "Off", "On" }, SettingsPresentation.Width(110));
            GUILayout.EndHorizontal(); return result == 1;
        }
        private static float Slider(float value, float min, float max, float normal, string label, float scale, string unit)
        {
            GUILayout.Label(label + ": " + (value * scale).ToString("0.##") + unit, _wrap);
            GUILayout.BeginHorizontal();
            bool changed = GUI.changed; GUI.changed = false;
            float result = GUILayout.HorizontalSlider(value, min, max);
            bool moved = GUI.changed; GUI.changed |= changed;
            if (GUILayout.Button("Default", SettingsPresentation.Width(80))) { result = normal; moved = true; GUI.changed = true; }
            GUILayout.EndHorizontal(); return moved ? result : value;
        }
        private static void Help(string text) { if (!string.IsNullOrEmpty(text)) GUILayout.Label(text, _help); }
    }
}
