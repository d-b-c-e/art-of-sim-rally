using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static partial class WheelInput
    {
        // Provisional state is separate from both the effective binding and XML.
        private sealed class Calibration
        {
            public Binding Candidate;
            public int Minimum, Maximum, Raw;
            public bool Released;
        }
        private static Calibration _calibration;
        public static Binding PendingCalibration => _calibration?.Candidate;
        public static float CalibrationValue => PendingCalibration == null ? 0 :
            PendingCalibration.Normalize(_calibration.Raw, _assigning == Channel.Steer);
        public static bool CanSaveCalibration => PendingCalibration != null && _calibration.Released &&
            (PendingCalibration.IsButton || (_assigning == Channel.Steer
                ? PendingCalibration.Rest - _calibration.Minimum >= AssignThreshold && _calibration.Maximum - PendingCalibration.Rest >= AssignThreshold
                : Math.Abs(PendingCalibration.Far - PendingCalibration.Rest) >= AssignThreshold));
        public static bool Inverted(Channel channel) => _bindings.TryGetValue(channel, out var b) && b.Inverted;
        public static float Deadzone(Channel channel) => _bindings.TryGetValue(channel, out var b) && b.Calibrated ? b.Deadzone : 0;
        public static string CalibrationDescription(Channel channel)
        {
            if (!_bindings.TryGetValue(channel, out var b)) return "Not calibrated.";
            if (!b.Calibrated) return "Saved legacy travel/direction preserved. Use Calibrate to set full travel, inversion and deadzone.";
            return "Invert: " + (b.Inverted ? "On" : "Off") + "; deadzone: " + (b.Deadzone * 100).ToString("0.#") + "%. Adjust with Calibrate.";
        }

        public static void BeginCalibration(Channel channel)
        {
            BeginAssign(channel);
            if (_assigning != channel) return;
            _calibration = new Calibration();
            _assignDeadline = Time.realtimeSinceStartup + 45;
            Status = channel == Channel.Steer ? "Centre the wheel before Bind, then turn fully left, fully right, and centre it."
                : IsButtonChannel(channel) ? "Press and release a button."
                : "Release the control before Bind, then press/pull fully and release. Save calibration when ready.";
        }
        private static void StepCalibration(Settings cfg)
        {
            if (Time.realtimeSinceStartup > _assignDeadline)
            { CancelAssign(); Status = "Calibration timed out. Previous binding kept."; return; }
            var candidate = _calibration.Candidate;
            if (candidate == null)
            {
                Binding found = null;
                int matches = 0;
                foreach (var device in _devices)
                {
                    if (!device.Ok || !device.InstanceGuid.HasValue) continue;
                    if (!device.HasAssignBaseline)
                    {
                        Array.Copy(device.Axes, device.BaseAxes, AxisCount);
                        Array.Copy(device.Buttons, device.BaseButtons, ButtonCount);
                        device.HasAssignBaseline = true; continue;
                    }
                    int count = IsButtonChannel(_assigning.Value) ? ButtonCount : AxisCount;
                    for (int i = 0; i < count; i++)
                    {
                        bool button = IsButtonChannel(_assigning.Value);
                        if (button ? device.Buttons[i] == 0 || device.BaseButtons[i] != 0
                            : Math.Abs(device.Axes[i] - device.BaseAxes[i]) < AssignThreshold) continue;
                        matches++;
                        found = new Binding { Device = device.Name, DeviceIndex = device.Index,
                            InstanceGuid = device.InstanceGuid, Element = i, IsButton = button,
                            Rest = button ? 0 : device.BaseAxes[i], Far = button ? 1 : device.Axes[i],
                            Left = button ? 0 : device.BaseAxes[i], Calibrated = !button };
                    }
                }
                if (matches > 1) { Status = "More than one control moved. Release them and move only the requested control."; return; }
                if (found == null) return;
                if (found.IsButton)
                {
                    foreach (var existing in _bindings)
                        if (existing.Key != _assigning && (IsShortcut(existing.Key) || IsShortcut(_assigning.Value)) &&
                            existing.Value.IsButton && existing.Value.InstanceGuid == found.InstanceGuid && existing.Value.Element == found.Element)
                        { Status = "That button is already used by " + existing.Key + ". Release it and choose another, or cancel and clear the old binding."; return; }
                }
                _calibration.Candidate = found;
                _calibration.Minimum = Math.Min(found.Rest, found.Far);
                _calibration.Maximum = Math.Max(found.Rest, found.Far);
                Status = found.Describe() + ": complete the full travel and release, then Save calibration.";
                candidate = found;
            }
            var d = Resolve(candidate);
            if (d == null || !d.Ok) { CancelAssign(); Status = "Device disconnected. Previous binding kept."; return; }
            if (candidate.IsButton)
            { _calibration.Raw = d.Buttons[candidate.Element] == 0 ? 0 : 1; _calibration.Released = _calibration.Raw == 0; return; }
            int raw = d.Axes[candidate.Element];
            _calibration.Raw = raw;
            _calibration.Minimum = Math.Min(_calibration.Minimum, raw);
            _calibration.Maximum = Math.Max(_calibration.Maximum, raw);
            if (_assigning == Channel.Steer)
            { candidate.Left = _calibration.Minimum; candidate.Far = _calibration.Maximum; }
            else if (Math.Abs(raw - candidate.Rest) > Math.Abs(candidate.Far - candidate.Rest)) candidate.Far = raw;
            _calibration.Released = Math.Abs(raw - candidate.Rest) < 2000;
        }
        public static bool SaveCalibration()
        {
            if (!CanSaveCalibration || Main.Settings == null) return false;
            var channel = _assigning.Value;
            var binding = PendingCalibration;
            if (!Bind(Main.Settings, channel, binding, !IsShortcut(channel))) return false;
            _calibration = null;
            Status = "Calibration saved. Existing game buttons remain available.";
            return true;
        }
    }
}
