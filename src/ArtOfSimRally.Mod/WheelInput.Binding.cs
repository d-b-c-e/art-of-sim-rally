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

            public static Binding Parse(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                var p = s.Split('|');
                if (p.Length != 5 && p.Length != 6) return null;
                var b = new Binding { Device = p[0] };
                if (p.Length == 6)
                {
                    if (!p[5].StartsWith("guid:", StringComparison.Ordinal) ||
                        !Guid.TryParse(p[5].Substring(5), out var guid) || guid == Guid.Empty) return null;
                    b.InstanceGuid = guid;
                }
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
                return b;
            }

            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}:{3}|{4}|{5}",
                Device, DeviceIndex, IsButton ? "button" : "axis", Element, Rest, Far) +
                (InstanceGuid.HasValue ? "|guid:" + InstanceGuid.Value.ToString("D") : "");

            public string Describe() => Device + (IsButton ? " button " + (Element + 1) : " axis " + (Element < AxisNames.Length ? AxisNames[Element] : Element.ToString()));
        }

    }
}
