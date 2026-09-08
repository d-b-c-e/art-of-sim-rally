// Fake device transport and game boundary, with production assignment,
// normalization, binding serialization and input override linked unchanged.
namespace Dbce.Wheel.Ffb
{
    public static class WheelFfbNative
    {
        public sealed class DeviceInfo { public string Name = "TSS fixture"; public int Index = 0; }
        public static DeviceInfo[] Devices = { new DeviceInfo() };
        public static readonly int[] Axes = new int[8];
        public static readonly byte[] Buttons = new byte[128];
        public static bool ReadOk = true, ThrowRead = false, FailOpen = false;
        public static int Reads;
        public static DeviceInfo[] ListAllDevices() => Devices;
        public static int OpenRead(int index) => FailOpen ? -1 : index;
        public static bool Read(int slot, int[] axes, byte[] buttons)
        {
            Reads++;
            if (ThrowRead) throw new IOException("fixture read failure");
            if (!ReadOk) return false;
            Array.Copy(Axes, axes, Axes.Length); Array.Copy(Buttons, buttons, Buttons.Length);
            return true;
        }
        public static void CloseRead() { }
    }
}
namespace HarmonyLib
{
    public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
    public sealed class HarmonyPostfix : Attribute { }
}
public static class EventStatusEnums { public enum EventStatus { UNDERWAY, FINISHING_STAGE_ANIMATION } }
public sealed class EventManager { public EventStatusEnums.EventStatus status = EventStatusEnums.EventStatus.UNDERWAY; }
public static class GameEntryPoint { public static EventManager EventManager = new(); }
public class AxisCarController
{
    public float SteeringOutOfAlignmentEffect = 0;
    public static float ProcessDeadzoneForInput(float value, float deadzone) => value;
}
public static class SettingsManager
{
    public static float GetSteeringDeadzone() => 0;
    public static float GetThrottleDeadzone() => 0;
    public static float GetBrakingDeadzone() => 0;
}
namespace ArtOfSimRally.Mod
{
    internal static class Time { public static float realtimeSinceStartup = 100; }
    internal static class GameState { public static bool IsDriving = true; }
    internal static class Main
    {
        public static Settings Settings = new();
        public static bool Enabled = true;
        public static string Path;
        public static int Saves;
        public static bool SaveSettings() { Saves++; SettingsPersistence.Write(Settings, Path); return true; }
    }
    internal static class ModLog
    {
        public static void Info(string message) { }
        public static void Warning(string message) { }
        public static void Error(string message) { }
    }
}
