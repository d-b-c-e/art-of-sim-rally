using System;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// The parts of the settings panel that read live hardware: device pickers,
    /// shifter binding, and what the game's input layer can see.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="SettingsPanel"/>, which is only layout over
    /// stored values. These blocks talk to DirectInput and Rewired, and each is
    /// drawn inside the section of the feature it belongs to rather than in a
    /// separate list of devices - picking a wheel belongs with force feedback, and
    /// binding gears belongs with the shifter.
    /// </remarks>
    internal static class Panel
    {
        private static GUIStyle Wrap => new GUIStyle(GUI.skin.label) { wordWrap = true };

        private static string[] _ffbDevices;

        private static string[] _ffbLabels = new string[0];
        private static bool _ffbListed;
        private static string[] _allDevices;
        private static string[] _allLabels = new string[0];
        private static bool _allListed;
        private static int _bindingGear = int.MinValue;
        private static float _bindingUntil;
        private static string _bindingStatus = "", _ffbSelectionStatus = "";
        internal static bool BindingActive => _bindingGear != int.MinValue;
        internal static void CancelBinding() => _bindingGear = int.MinValue;

        /// <summary>Forces both device lists to be re-read.</summary>
        public static void Rescan()
        {
            _ffbListed = false;
            _allListed = false;
            DeviceDropdown.CloseAll();
        }

        public static void DrawWheelPicker()
        {
            var cfg = Main.Settings;
            if (GameState.IsDriving) { GUILayout.Label("Pause before changing the FFB device.", Wrap); return; }
            if (!_ffbListed) { _ffbDevices = FfbNative.ListDevices(); _ffbLabels = FfbNative.ListDeviceLabels(_ffbDevices); _ffbListed = true; }
            bool follow = FfbSelection.FollowsSteering(cfg);
            var steering = WheelInput.Binding.Parse(cfg.SteerBinding);
            int position = follow ? 0 : FfbNative.SelectedPosition(cfg) + 1;
            bool missing = !follow && position == 0;
            var labels = new string[_ffbDevices.Length + 1 + (missing ? 1 : 0)];
            labels[0] = "Use steering wheel — " + (steering?.Device ?? "Steering not bound");
            for (int i = 0; i < _ffbDevices.Length; i++)
            {
                string guid = FfbNative.DeviceGuid(i);
                labels[i + 1] = _ffbDevices[i] + (guid.Length >= 6 ? " · " + guid.Substring(guid.Length - 6) : " (identity unavailable)");
            }
            if (missing) { position = labels.Length - 1; labels[position] = "Saved device disconnected/unverified — " + cfg.PreferredDevice; }
            int chosen = DeviceDropdown.Draw("wheel", "FFB device", labels, position, "No devices found.");
            if (chosen >= 0 && chosen <= _ffbDevices.Length)
            {
                string oldMode = cfg.FfbDeviceMode, oldName = cfg.PreferredDevice, oldGuid = cfg.PreferredDeviceGuid;
                int oldIndex = cfg.PreferredDeviceIndex;
                bool saved = SettingsCommit.TrySave(() => {
                    cfg.FfbDeviceMode = chosen == 0 ? "steering" : "explicit";
                    if (chosen > 0) { cfg.PreferredDeviceIndex = chosen - 1; cfg.PreferredDevice = _ffbDevices[chosen - 1]; cfg.PreferredDeviceGuid = FfbNative.DeviceGuid(chosen - 1); }
                }, () => { cfg.FfbDeviceMode = oldMode; cfg.PreferredDevice = oldName; cfg.PreferredDeviceGuid = oldGuid; cfg.PreferredDeviceIndex = oldIndex; });
                _ffbSelectionStatus = saved ? "Selection saved." : "Could not save. Previous FFB device kept; check Settings.xml is writable.";
                if (saved) Main.SelectForceDevice();
            }
            GUILayout.Label(_ffbSelectionStatus, Wrap);
            if (!FfbSelection.TryTarget(cfg, out _, out _, out _, out var reason)) GUILayout.Label(reason, Wrap);
            if (GUILayout.Button("Refresh / retry connection")) { Rescan(); Main.SelectForceDevice(); }
        }

        public static void DrawShifterBinding(Settings cfg)
        {
            if (BindingActive && Time.realtimeSinceStartup >= _bindingUntil) CancelBinding();
            if (GameState.IsDriving)
            {
                GUILayout.Label("      Pause to change shifter devices or bindings.", Wrap);
                return;
            }
            if (!_allListed) { _allDevices = Shifter.ListDevices(); _allLabels = Shifter.ListDeviceLabels(_allDevices); _allListed = true; }

            cfg.ShifterIsHPattern = GUILayout.Toggle(cfg.ShifterIsHPattern,
                "  H-pattern (off = sequential)");

            int picked = DeviceDropdown.Draw(
                "shifter", "Shifter", _allLabels, Shifter.SelectedPosition(cfg), "No controllers found.");
            if (picked >= 0)
            {
                int oldIndex = cfg.ShifterDeviceIndex; string oldName = cfg.ShifterDeviceName, oldGuid = cfg.ShifterDeviceGuid;
                bool saved = SettingsCommit.TrySave(() => { cfg.ShifterDeviceIndex = picked; cfg.ShifterDeviceName = _allDevices[picked]; cfg.ShifterDeviceGuid = Shifter.DeviceGuid(picked); },
                    () => { cfg.ShifterDeviceIndex = oldIndex; cfg.ShifterDeviceName = oldName; cfg.ShifterDeviceGuid = oldGuid; });
                _bindingStatus = saved ? "Selection saved." : "Could not save. Previous shifter kept; check Settings.xml is writable.";
                if (saved) Shifter.Open(picked);
            }
            GUILayout.Label(_bindingStatus, Wrap);

            if (cfg.ShifterDeviceIndex < 0)
            {
                GUILayout.Label("      Choose the device your shifter is, then bind each gear.", Wrap);
                return;
            }

            if (!Shifter.IsOpen)
            {
                if (GUILayout.Button("Connect", GUILayout.Width(140)))
                    Shifter.Open(cfg.ShifterDeviceIndex);
                GUILayout.Label("      " + (string.IsNullOrEmpty(Shifter.Status) ? "Not connected." : Shifter.Status), Wrap);
                return;
            }

            // Polled here as well as in the physics loop so the reading is live
            // while binding, before any stage has been started.
            Shifter.PollForBinding();

            GUILayout.Label(cfg.ShifterIsHPattern
                ? "      Click Bind, then move the lever into that gate."
                : "      Click Bind, then push the lever that way.", Wrap);

            if (_bindingGear != int.MinValue)
            {
                int pressed = Shifter.PressedButton;
                if (pressed >= 0)
                {
                    if (!SaveShifterButton(cfg, _bindingGear, pressed)) return;

                    ModLog.Info("Bound " +
                        (_bindingGear == BindUp ? "shift up"
                         : _bindingGear == BindDown ? "shift down"
                         : GearLabel(_bindingGear)) + " to button " + pressed);

                    _bindingGear = int.MinValue;
                }
            }

            if (cfg.ShifterIsHPattern)
            {
                GearRow(cfg, -1);
                for (int g = 1; g <= 6; g++) GearRow(cfg, g);
            }
            else
            {
                // A sequential lever is two momentary switches. Offering seven
                // gates for one would be asking the wrong question.
                SequentialRow(cfg, true);
                SequentialRow(cfg, false);
            }

            GUILayout.Label("      Pressed now: " +
                (Shifter.PressedButton >= 0 ? "button " + (Shifter.PressedButton + 1) : "nothing"), Wrap);
        }

        private static void GearRow(Settings cfg, int gear)
        {
            int button = gear == -1 ? cfg.GearReverseButton : cfg.GearButton(gear);
            bool waiting = _bindingGear == gear;

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(GearLabel(gear), GUILayout.Width(70));
            GUILayout.Label(waiting ? "Press a button" : (button >= 0 ? "Button " + (button + 1) : "Not bound"),
                            GUILayout.Width(90));
            if (GUILayout.Button(waiting ? "Cancel" : "Bind", GUILayout.Width(70)))
            { _bindingGear = waiting ? int.MinValue : gear; _bindingUntil = Time.realtimeSinceStartup + 10; }
            if (button >= 0 && GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                SaveShifterButton(cfg, gear, -1);
            }
            GUILayout.EndHorizontal();
        }

        // Sequential rows are bound the same way but stored separately, and use
        // int.MinValue/MaxValue as their binding ids so they cannot collide with a
        // gear number.
        private const int BindUp = int.MaxValue;
        private const int BindDown = int.MaxValue - 1;

        private static void SequentialRow(Settings cfg, bool isUp)
        {
            int id = isUp ? BindUp : BindDown;
            int button = isUp ? cfg.ShiftUpButton : cfg.ShiftDownButton;
            bool waiting = _bindingGear == id;

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(isUp ? "Shift up" : "Shift down", GUILayout.Width(90));
            GUILayout.Label(waiting ? "Press a button" : (button >= 0 ? "Button " + (button + 1) : "Not bound"),
                            GUILayout.Width(90));
            if (GUILayout.Button(waiting ? "Cancel" : "Bind", GUILayout.Width(70)))
            { _bindingGear = waiting ? int.MinValue : id; _bindingUntil = Time.realtimeSinceStartup + 10; }
            if (button >= 0 && GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                SaveShifterButton(cfg, id, -1);
            }
            GUILayout.EndHorizontal();
        }

        private static bool SaveShifterButton(Settings cfg, int gear, int button)
        {
            int previous = gear == BindUp ? cfg.ShiftUpButton : gear == BindDown ? cfg.ShiftDownButton : gear == -1 ? cfg.GearReverseButton : cfg.GearButton(gear);
            Action<int> set = value => { if (gear == BindUp) cfg.ShiftUpButton = value; else if (gear == BindDown) cfg.ShiftDownButton = value; else cfg.SetGearButton(gear, value); };
            bool saved = SettingsCommit.TrySave(() => set(button), () => set(previous));
            _bindingStatus = saved ? "Binding saved." : "Could not save. Previous binding kept; check Settings.xml is writable, then retry or Cancel.";
            return saved;
        }
        private static string GearLabel(int gear) => gear == -1 ? "Reverse" : "Gear " + gear;

        public static void DrawInputStatus()
        {
            try
            {
                if (!ReInput.isReady) { GUILayout.Label("Input system not started yet.", Wrap); return; }

                var joysticks = ReInput.controllers.Joysticks;
                GUILayout.Label("What the game can see: " +
                                (joysticks == null ? 0 : joysticks.Count) + " controller(s)", Wrap);

                if (joysticks != null)
                    foreach (var j in joysticks)
                        GUILayout.Label("      " + j.name +
                                        (j.hardwareTypeGuid == Guid.Empty ? "   (no profile)" : ""),
                                        Wrap);

                // "Not recognised" reads like a fault, and it is not. Say what it
                // actually means, since it is the reason several fixes exist.
                GUILayout.Label(
                    "\"No profile\" only means the game's input library has no built-in entry " +
                    "for that model - its database predates most direct-drive wheels. Nothing " +
                    "is broken; it is why the steering and deadzone fixes above exist.", Wrap);

                if (joysticks != null && joysticks.Count < 2)
                    GUILayout.Label(
                        "Shifters and handbrakes often do not appear here at all, because the " +
                        "game's input layer skips devices that do not report as a joystick. " +
                        "Use the Shifter section, which reads the device directly.", Wrap);

                if (GUILayout.Button("Rescan devices", GUILayout.Width(160))) Rescan();
            }
            catch (Exception ex)
            {
                GUILayout.Label("Could not read input state: " + ex.Message, Wrap);
            }
        }
    }
}
