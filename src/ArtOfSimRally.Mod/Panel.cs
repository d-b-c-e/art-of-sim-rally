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
        private static GUIStyle _wrap;
        private static GUIStyle Wrap => _wrap ?? (_wrap = new GUIStyle(GUI.skin.label) { wordWrap = true });

        private static string[] _ffbDevices;
        private static bool _ffbListed;
        private static string[] _allDevices;
        private static bool _allListed;
        private static int _bindingGear = int.MinValue;

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
            if (!_ffbListed) { _ffbDevices = FfbNative.ListDevices(); _ffbListed = true; }

            int chosen = DeviceDropdown.Draw(
                "wheel", "Wheel", _ffbDevices, cfg.PreferredDeviceIndex,
                "No force-feedback device found. Check the wheel is powered on and not held " +
                "by another program.");

            if (chosen >= 0)
            {
                cfg.PreferredDeviceIndex = chosen;
                cfg.PreferredDevice = _ffbDevices[chosen];
                Main.SaveSettings();

                // Switch immediately rather than at next launch. Trying each of two
                // similarly named devices to see which one moves is the natural way
                // to pick, and that needs the change to take effect now.
                if (Main.ReopenForceFeedback())
                    ModLog.Info("Now using " + _ffbDevices[chosen]);
                else
                    ModLog.Warning("Could not switch to " + _ffbDevices[chosen] +
                                   "; a restart may be needed.");
            }

            if (_ffbDevices != null && _ffbDevices.Length > 1)
                GUILayout.Label("      Two devices with the same name? Pick one and turn the " +
                                "wheel - if nothing happens, choose the other.", Wrap);
        }

        public static void DrawShifterBinding(Settings cfg)
        {
            if (!_allListed) { _allDevices = Shifter.ListDevices(); _allListed = true; }

            cfg.ShifterIsHPattern = GUILayout.Toggle(cfg.ShifterIsHPattern,
                "  H-pattern (off = sequential)");

            int picked = DeviceDropdown.Draw(
                "shifter", "Shifter", _allDevices, cfg.ShifterDeviceIndex, "No controllers found.");
            if (picked >= 0)
            {
                cfg.ShifterDeviceIndex = picked;
                cfg.ShifterDeviceName = _allDevices[picked];
                Shifter.Open(picked);
                Main.SaveSettings();
            }

            if (cfg.ShifterDeviceIndex < 0)
            {
                GUILayout.Label("      Choose the device your shifter is, then bind each gear.", Wrap);
                return;
            }

            if (!Shifter.IsOpen)
            {
                if (GUILayout.Button("Connect", GUILayout.Width(140)))
                    Shifter.Open(cfg.ShifterDeviceIndex);
                GUILayout.Label("      Not connected.", Wrap);
                return;
            }

            // Polled here as well as in the physics loop so the reading is live
            // while binding, before any stage has been started.
            Shifter.Poll();

            GUILayout.Label(cfg.ShifterIsHPattern
                ? "      Click Set, then move the lever into that gate."
                : "      Click Set, then press the button for that gear.", Wrap);

            if (_bindingGear != int.MinValue)
            {
                int pressed = Shifter.PressedButton;
                if (pressed >= 0)
                {
                    cfg.SetGearButton(_bindingGear, pressed);
                    ModLog.Info("Bound " + GearLabel(_bindingGear) + " to button " + pressed);
                    _bindingGear = int.MinValue;
                    Main.SaveSettings();
                }
            }

            GearRow(cfg, -1);
            for (int g = 1; g <= 6; g++) GearRow(cfg, g);

            GUILayout.Label("      Pressed now: " +
                (Shifter.PressedButton >= 0 ? "button " + Shifter.PressedButton : "nothing"), Wrap);
        }

        private static void GearRow(Settings cfg, int gear)
        {
            int button = gear == -1 ? cfg.GearReverseButton : cfg.GearButton(gear);
            bool waiting = _bindingGear == gear;

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(GearLabel(gear), GUILayout.Width(70));
            GUILayout.Label(waiting ? "press it..." : (button >= 0 ? "button " + button : "-"),
                            GUILayout.Width(90));
            if (GUILayout.Button(waiting ? "cancel" : "set", GUILayout.Width(70)))
                _bindingGear = waiting ? int.MinValue : gear;
            if (button >= 0 && GUILayout.Button("clear", GUILayout.Width(60)))
            {
                cfg.SetGearButton(gear, -1);
                Main.SaveSettings();
            }
            GUILayout.EndHorizontal();
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
