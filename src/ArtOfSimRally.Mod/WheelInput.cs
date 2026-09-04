using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Reads steering and pedals straight from the device and feeds them to the
    /// car, bypassing the game's input library entirely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game reads the wheel through Rewired's Raw Input backend, which parses
    /// HID reports itself and cannot read some devices at all: a Fanatec
    /// direct-drive base shows up twice as "FANATEC Wheel" with 32 axes / 144
    /// buttons and never reports an element moving, so the controls screen can
    /// never bind it. Switching Rewired to DirectInput at runtime was tried
    /// (2026-09-01 and again 2026-09-02) and left the menus dead both times.
    /// </para>
    /// <para>
    /// This goes around the problem instead. The native plugin already reads
    /// every DirectInput controller for the shifter; the same path reads axes.
    /// Bound channels are written over the game's own values in a postfix on
    /// <c>AxisCarController.GetInput</c>, after the game's deadzone processing
    /// and with its steering-alignment effect preserved, so everything downstream
    /// - direct steering, steer assist, telemetry - sees exactly what it would
    /// from a wheel Rewired understood. Menus still use keyboard or pad.
    /// </para>
    /// <para>
    /// Binding is "press Assign, then move the control". The value at rest and
    /// the value it moved to are recorded, and the far end keeps extending as
    /// the control is used, so a half turn at assignment does not cap the range.
    /// Axes are requested in the range 0-65535 on every device. Steering maps
    /// rest to 0 and the recorded direction to +1, the other lock to -1; pedals
    /// map rest to 0 and the moved direction to 1, which also handles pedals that
    /// idle at the top of their range. A button can be bound to any channel and
    /// reads 0 or 1 - useful for a handbrake.
    /// </para>
    /// </remarks>
    internal static class WheelInput
    {
        private const string Dll = "UnityForceFeedback";

        [DllImport(Dll)] private static extern int EnumerateAllDevices();
        [DllImport(Dll, CharSet = CharSet.Ansi)]
        private static extern int GetAnyDeviceName(int index, StringBuilder buffer, int size);
        [DllImport(Dll)] private static extern int OpenReadDevice(int index);
        [DllImport(Dll)] private static extern int ReadDeviceState(int slot, int[] axes, byte[] buttons, int buttonCount);
        [DllImport(Dll)] private static extern void CloseReadDevices();

        public enum Channel { Steer, Throttle, Brake, Clutch, Handbrake }
        public static readonly Channel[] Channels = { Channel.Steer, Channel.Throttle, Channel.Brake, Channel.Clutch, Channel.Handbrake };

        private const int AxisCount = 8;
        private const int ButtonCount = 128;
        private const int AssignThreshold = 12000;   // of 65535, so a nudge does not bind
        private static readonly string[] AxisNames = { "X", "Y", "Z", "Rx", "Ry", "Rz", "Slider 1", "Slider 2" };

        private sealed class Device
        {
            public int Slot, Index;
            public string Name;
            public int[] Axes = new int[AxisCount];
            public byte[] Buttons = new byte[ButtonCount];
            public int[] BaseAxes = new int[AxisCount];
            public byte[] BaseButtons = new byte[ButtonCount];
            public bool Ok;
        }

        /// <summary>One bound control. Serialised as "device|index|axis:N or button:N|rest|far".</summary>
        public sealed class Binding
        {
            public string Device = "";
            public int DeviceIndex = -1;
            public bool IsButton;
            public int Element = -1;
            public int Rest, Far;

            public static Binding Parse(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                var p = s.Split('|');
                if (p.Length < 5) return null;
                var b = new Binding { Device = p[0] };
                int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.DeviceIndex);
                var el = p[2].Split(':');
                b.IsButton = el.Length == 2 && el[0] == "button";
                int.TryParse(el.Length == 2 ? el[1] : "-1", NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Element);
                int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Rest);
                int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Far);
                return b.Element >= 0 ? b : null;
            }

            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}:{3}|{4}|{5}",
                Device, DeviceIndex, IsButton ? "button" : "axis", Element, Rest, Far);

            public string Describe() => Device + (IsButton ? " button " + (Element + 1) : " axis " + (Element < AxisNames.Length ? AxisNames[Element] : Element.ToString()));
        }

        private static readonly List<Device> _devices = new List<Device>();
        private static readonly Dictionary<Channel, Binding> _bindings = new Dictionary<Channel, Binding>();
        private static readonly Dictionary<Channel, float> _values = new Dictionary<Channel, float>();
        private static bool _open;
        private static Channel? _assigning;
        private static float _assignDeadline;
        private static float _nextSave = -1f;
        private static float _nextOpenRetry;
        private static bool _firstReadLogged;

        public static string Status { get; private set; } = "";
        public static Channel? Assigning => _assigning;
        public static string DeviceSummary
        {
            get
            {
                if (!_open || _devices.Count == 0) return "no controllers open";
                var sb = new StringBuilder();
                foreach (var d in _devices) { if (sb.Length > 0) sb.Append(", "); sb.Append(d.Name); if (!d.Ok) sb.Append(" (not responding)"); }
                return sb.ToString();
            }
        }

        public static bool Enabled => Main.Enabled && Main.Settings != null && Main.Settings.WheelInputEnabled;
        public static bool IsBound(Channel c) => _bindings.ContainsKey(c);
        public static float Value(Channel c) => _values.TryGetValue(c, out float v) ? v : 0f;
        public static string Describe(Channel c) => _bindings.TryGetValue(c, out var b) ? b.Describe() : "not assigned";

        /// <summary>Loads bindings from settings. Call at load and after settings change.</summary>
        public static void LoadBindings()
        {
            var cfg = Main.Settings;
            _bindings.Clear();
            if (cfg == null) return;
            foreach (var c in Channels)
            {
                var b = Binding.Parse(Setting(cfg, c));
                if (b != null) _bindings[c] = b;
            }
        }

        private static string Setting(Settings cfg, Channel c)
        {
            switch (c)
            {
                case Channel.Steer: return cfg.SteerBinding;
                case Channel.Throttle: return cfg.ThrottleBinding;
                case Channel.Brake: return cfg.BrakeBinding;
                case Channel.Clutch: return cfg.ClutchBinding;
                default: return cfg.HandbrakeBinding;
            }
        }

        private static void Store(Settings cfg, Channel c, string value)
        {
            switch (c)
            {
                case Channel.Steer: cfg.SteerBinding = value; break;
                case Channel.Throttle: cfg.ThrottleBinding = value; break;
                case Channel.Brake: cfg.BrakeBinding = value; break;
                case Channel.Clutch: cfg.ClutchBinding = value; break;
                default: cfg.HandbrakeBinding = value; break;
            }
        }

        /// <summary>Opens every DirectInput controller for reading. Safe to call repeatedly.</summary>
        public static void Open()
        {
            if (_open) return;
            try
            {
                Close();
                int count = EnumerateAllDevices();
                var buf = new StringBuilder(260);
                for (int i = 0; i < count; i++)
                {
                    buf.Length = 0;
                    string name = GetAnyDeviceName(i, buf, buf.Capacity) != 0 ? buf.ToString() : "(device " + i + ")";
                    int slot = OpenReadDevice(i);
                    if (slot < 0) { ModLog.Warning("Wheel input: could not open " + name); continue; }
                    _devices.Add(new Device { Slot = slot, Index = i, Name = name });
                }
                _open = _devices.Count > 0;
                _firstReadLogged = false;
                var names = new StringBuilder();
                foreach (var d in _devices) { if (names.Length > 0) names.Append(", "); names.Append(d.Name); }
                ModLog.Info("Wheel input: opened " + _devices.Count + " controller(s): " + names);
                if (!_open) Status = "No controllers found to read.";
            }
            catch (Exception ex)
            {
                ModLog.Error("Wheel input: open failed: " + ex.Message);
                Status = "Could not open controllers: " + ex.Message;
                _open = false;
            }
        }

        public static void Close()
        {
            try { if (_devices.Count > 0 || _open) CloseReadDevices(); } catch { }
            _devices.Clear();
            _open = false;
            _assigning = null;
        }

        /// <summary>Called every frame by the watchdog.</summary>
        public static void Update()
        {
            var cfg = Main.Settings;
            if (cfg == null) return;
            if (!cfg.WheelInputEnabled || !Main.Enabled)
            {
                if (_open) Close();
                return;
            }
            if (!_open)
            {
                if (Time.realtimeSinceStartup < _nextOpenRetry) return;
                _nextOpenRetry = Time.realtimeSinceStartup + 5f;
                Open();
                if (!_open) return;
            }

            foreach (var d in _devices)
            {
                try { d.Ok = ReadDeviceState(d.Slot, d.Axes, d.Buttons, ButtonCount) != 0; }
                catch { d.Ok = false; }
            }
            if (!_firstReadLogged)
            {
                // Once, with raw values: proves the reads work on every handle,
                // and shows the resting position of each axis for support.
                _firstReadLogged = true;
                var sb = new StringBuilder("Wheel input: first read -");
                foreach (var d in _devices)
                {
                    sb.Append(" | ").Append(d.Name).Append(d.Ok ? " axes " : " NOT RESPONDING");
                    if (d.Ok) for (int i = 0; i < AxisCount; i++) sb.Append(d.Axes[i]).Append(i < AxisCount - 1 ? "," : "");
                }
                ModLog.Info(sb.ToString());
            }

            if (_assigning.HasValue) StepAssign(cfg);

            bool extended = false;
            foreach (var c in Channels)
            {
                if (!_bindings.TryGetValue(c, out var b)) { _values.Remove(c); continue; }
                var d = Resolve(b);
                if (d == null || !d.Ok) { _values[c] = 0f; continue; }
                if (b.IsButton)
                {
                    _values[c] = b.Element < ButtonCount && d.Buttons[b.Element] != 0 ? 1f : 0f;
                    continue;
                }
                int raw = d.Axes[b.Element];
                int span = b.Far - b.Rest;
                if (span == 0) { _values[c] = 0f; continue; }
                // The far end keeps extending in the recorded direction, so the
                // first full press or full lock calibrates the range.
                if (Math.Sign(raw - b.Rest) == Math.Sign(span) && Math.Abs(raw - b.Rest) > Math.Abs(span))
                {
                    b.Far = raw; span = b.Far - b.Rest; extended = true;
                }
                float v = (raw - b.Rest) / (float)span;
                _values[c] = c == Channel.Steer ? Mathf.Clamp(v, -1f, 1f) : Mathf.Clamp01(v);
            }

            if (extended && Time.realtimeSinceStartup >= _nextSave)
            {
                // Persist the learned range, but not more than once every few seconds.
                _nextSave = Time.realtimeSinceStartup + 5f;
                foreach (var kv in _bindings) Store(cfg, kv.Key, kv.Value.ToString());
                Main.SaveSettings();
            }
        }

        private static Device Resolve(Binding b)
        {
            Device byName = null;
            foreach (var d in _devices)
            {
                if (d.Name != b.Device) continue;
                if (d.Index == b.DeviceIndex) return d;
                if (byName == null) byName = d;
            }
            return byName;
        }

        // --- assignment ---------------------------------------------------------

        public static void BeginAssign(Channel c)
        {
            if (!_open) Open();
            if (!_open) return;
            foreach (var d in _devices)
            {
                try { ReadDeviceState(d.Slot, d.Axes, d.Buttons, ButtonCount); } catch { }
                Array.Copy(d.Axes, d.BaseAxes, AxisCount);
                Array.Copy(d.Buttons, d.BaseButtons, ButtonCount);
            }
            _assigning = c;
            _assignDeadline = Time.realtimeSinceStartup + 10f;
            Status = "Move the control you want for " + c + " (or press a button) - 10 seconds.";
        }

        public static void CancelAssign()
        {
            _assigning = null;
            Status = "";
        }

        /// <summary>Mirrors a bound axis around its rest value, for a wheel whose axis runs the other way.</summary>
        public static void Flip(Channel c)
        {
            if (!_bindings.TryGetValue(c, out var b) || b.IsButton) return;
            b.Far = b.Rest - (b.Far - b.Rest);
            var cfg = Main.Settings;
            if (cfg != null) { Store(cfg, c, b.ToString()); Main.SaveSettings(); }
            Status = c + " flipped.";
            ModLog.Info("Wheel input: " + c + " flipped to " + b);
        }

        public static void Clear(Channel c)
        {
            _bindings.Remove(c);
            _values.Remove(c);
            var cfg = Main.Settings;
            if (cfg != null) { Store(cfg, c, ""); Main.SaveSettings(); }
            Status = c + " cleared.";
        }

        private static void StepAssign(Settings cfg)
        {
            var c = _assigning.Value;
            if (Time.realtimeSinceStartup > _assignDeadline)
            {
                _assigning = null;
                Status = "Nothing moved - " + c + " left as it was.";
                return;
            }
            foreach (var d in _devices)
            {
                if (!d.Ok) continue;
                for (int i = 0; i < AxisCount; i++)
                {
                    int delta = d.Axes[i] - d.BaseAxes[i];
                    if (Math.Abs(delta) < AssignThreshold) continue;
                    // Steering: +1 must mean right whichever way the wheel was turned
                    // during Assign. DirectInput's steering axis increases to the right
                    // on every wheel, so the far end is always the increasing side; a
                    // left turn during Assign used to make left positive, and the car
                    // steered inverted (owner's rig, 2026-09-03). Pedals keep the moved
                    // direction: rest -> pressed is unambiguous.
                    int far = c == Channel.Steer ? d.BaseAxes[i] + Math.Abs(delta) : d.Axes[i];
                    Bind(cfg, c, new Binding { Device = d.Name, DeviceIndex = d.Index, IsButton = false, Element = i, Rest = d.BaseAxes[i], Far = far });
                    return;
                }
                for (int i = 0; i < ButtonCount; i++)
                {
                    if (d.Buttons[i] == 0 || d.BaseButtons[i] != 0) continue;
                    Bind(cfg, c, new Binding { Device = d.Name, DeviceIndex = d.Index, IsButton = true, Element = i, Rest = 0, Far = 1 });
                    return;
                }
            }
        }

        private static void Bind(Settings cfg, Channel c, Binding b)
        {
            _bindings[c] = b;
            _assigning = null;
            Store(cfg, c, b.ToString());
            Main.SaveSettings();
            Status = c + " = " + b.Describe() + ". Use it fully once to calibrate the range.";
            ModLog.Info("Wheel input: " + c + " bound to " + b);
        }
    }

    /// <summary>
    /// Writes the directly read channels over the game's values. Runs after the
    /// game's own read, so unbound channels keep whatever Rewired produced.
    /// </summary>
    [HarmonyPatch(typeof(AxisCarController), "GetInput")]
    internal static class WheelInputPatch
    {
        [HarmonyPostfix]
        private static void Override(AxisCarController __instance,
                                     ref float throttleInput, ref float brakeInput, ref float steerInput,
                                     ref float handbrakeInput, ref float clutchInput, ref bool startEngineInput)
        {
            if (!WheelInput.Enabled) return;
            try
            {
                // The game hands the car to its own driver here and zeroes input;
                // leave that alone.
                if (GameEntryPoint.EventManager.status == EventStatusEnums.EventStatus.FINISHING_STAGE_ANIMATION) return;
            }
            catch { return; }

            if (WheelInput.IsBound(WheelInput.Channel.Steer))
                steerInput = AxisCarController.ProcessDeadzoneForInput(WheelInput.Value(WheelInput.Channel.Steer), SettingsManager.GetSteeringDeadzone())
                             + __instance.SteeringOutOfAlignmentEffect;
            if (WheelInput.IsBound(WheelInput.Channel.Throttle))
            {
                throttleInput = AxisCarController.ProcessDeadzoneForInput(WheelInput.Value(WheelInput.Channel.Throttle), SettingsManager.GetThrottleDeadzone());
                startEngineInput = throttleInput > 0f;
            }
            if (WheelInput.IsBound(WheelInput.Channel.Brake))
                brakeInput = AxisCarController.ProcessDeadzoneForInput(WheelInput.Value(WheelInput.Channel.Brake), SettingsManager.GetBrakingDeadzone());
            if (WheelInput.IsBound(WheelInput.Channel.Clutch))
                clutchInput = WheelInput.Value(WheelInput.Channel.Clutch);
            if (WheelInput.IsBound(WheelInput.Channel.Handbrake))
                handbrakeInput = WheelInput.Value(WheelInput.Channel.Handbrake);
        }
    }
}
