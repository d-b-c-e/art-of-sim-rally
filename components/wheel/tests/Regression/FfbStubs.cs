namespace ArtOfSimRally.Mod
{
    internal static class FfbController { public static void Reset() { } }
    internal static class ModLog
    {
        public static void Info(string message) { }
        public static void Warning(string message) { }
        public static void Error(string message) { }
    }
}

namespace ArtOfSimRally.Mod
{
    internal static partial class WheelInput
    {
        private static readonly string[] AxisNames = { "X", "Y", "Z", "Rx", "Ry", "Rz", "Slider 1", "Slider 2" };
    }
}
