#nullable disable
using System;
using System.Globalization;

namespace ArtOfSimRally.Mod
{
    internal static partial class WheelInput
    {
        /// <summary>Legacy five-field control, optionally followed by a strict guid: identity.</summary>
        public sealed class Binding
        {
            public string Device = "";
            public int DeviceIndex = -1;
            public bool IsButton;
            public int Element = -1;
            public int Rest, Far;
            public Guid? InstanceGuid;
            public bool Calibrated, Inverted;
            public int Left;
            public float Deadzone;

            public static Binding Parse(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                var p = s.Split('|');
                if (p.Length < 5 || p.Length > 7) return null;
                var b = new Binding { Device = p[0] };
                if (p.Length >= 6 && p[5].StartsWith("guid:", StringComparison.Ordinal))
                {
                    if (!p[5].StartsWith("guid:", StringComparison.Ordinal) ||
                        !Guid.TryParse(p[5].Substring(5), out var guid) || guid == Guid.Empty) return null;
                    b.InstanceGuid = guid;
                }
                int calibration = b.InstanceGuid.HasValue ? 6 : 5;
                if (p.Length > calibration)
                {
                    var c = p[calibration].Split(':');
                    if (c.Length != 4 || c[0] != "cal" || !int.TryParse(c[1], out b.Left) || b.Left < 0 || b.Left > 65535 ||
                        !float.TryParse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b.Deadzone) ||
                        float.IsNaN(b.Deadzone) || b.Deadzone < 0 || b.Deadzone > .25f || (c[3] != "0" && c[3] != "1")) return null;
                    b.Calibrated = true; b.Inverted = c[3] == "1";
                }
                if (p.Length > calibration + 1) return null;
                if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.DeviceIndex) || b.DeviceIndex < 0) return null;
                var el = p[2].Split(':');
                if (el.Length != 2 || (el[0] != "button" && el[0] != "axis")) return null;
                b.IsButton = el[0] == "button";
                if (!int.TryParse(el[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Element) ||
                    b.Element < 0 || b.Element >= (b.IsButton ? 128 : 8)) return null;
                if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Rest) ||
                    !int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out b.Far) ||
                    b.Rest < 0 || b.Rest > 65535 || (long)b.Far - b.Rest < -65535 || (long)b.Far - b.Rest > 65535) return null;
                // Steering Flip reflects a calibrated endpoint around center;
                // 32767..65535 becomes 32767..-1. The endpoint is a calibration
                // value, not a raw sample. Bound the span without rejecting it.
                if (b.Calibrated && (b.IsButton || b.Far < 0 || b.Far > 65535 || b.Far == b.Rest)) return null;
                return b;
            }

            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}:{3}|{4}|{5}",
                Device, DeviceIndex, IsButton ? "button" : "axis", Element, Rest, Far) +
                (InstanceGuid.HasValue ? "|guid:" + InstanceGuid.Value.ToString("D") : "") +
                (Calibrated ? "|cal:" + Left.ToString(CultureInfo.InvariantCulture) + ":" + Deadzone.ToString("R", CultureInfo.InvariantCulture) + ":" + (Inverted ? "1" : "0") : "");

            public float Normalize(int raw, bool steering)
            {
                float span = Calibrated && steering && raw < Rest ? Rest - Left : Far - Rest;
                if (span == 0) return 0;
                float value = (raw - Rest) / span;
                if (Calibrated && Inverted) value = steering ? -value : 1 - value;
                value = Math.Max(steering ? -1 : 0, Math.Min(1, value));
                if (Calibrated)
                {
                    float magnitude = Math.Abs(value);
                    value = magnitude <= Deadzone ? 0 : Math.Sign(value) * (magnitude - Deadzone) / (1 - Deadzone);
                }
                return value;
            }

            public string Describe() => Device + (IsButton ? " button " + (Element + 1) : " axis " + (Element < AxisNames.Length ? AxisNames[Element] : Element.ToString()));
        }

    }
}
