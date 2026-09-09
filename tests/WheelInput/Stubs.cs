// Fake device transport and game boundary, with production assignment,
// normalization, binding serialization and input override linked unchanged.
namespace Dbce.Wheel.Ffb
{
    public static class WheelFfbNative
    {
        public sealed class DeviceInfo
        {
            public string Name = "TSS fixture"; public int Index = 0; public Guid? InstanceGuid;
            public int[] StateAxes; public bool Connected = true, CannotOpen;
            public string Label => Name;
        }
        public static DeviceInfo[] Devices = { new DeviceInfo() };
        public static readonly int[] Axes = new int[8];
        public static readonly byte[] Buttons = new byte[128];
        public static bool ReadOk = true, ThrowRead = false, FailOpen = false;
        public static int Reads, Enumerations, Closes;
        public static int LastAuxIndex=-1;
        public static DeviceInfo Aux;
        public static bool OpenAux(int index) { Aux=Devices.SingleOrDefault(d=>d.Index==index); LastAuxIndex=index; return Aux!=null && !Aux.CannotOpen; }
        public static void CloseAux() { Aux=null; }
        public static int ReadAux(byte[] buttons) { if(Aux==null || !Aux.Connected) return 0; Array.Copy(Buttons,buttons,Buttons.Length); return Buttons.Length; }
        private static readonly Dictionary<int, DeviceInfo> slots = new();
        public static DeviceInfo[] ListAllDevices() { Enumerations++; return Devices; }
        public static int OpenRead(int index)
        {
            var device = Devices.Single(d => d.Index == index);
            if (FailOpen || device.CannotOpen) return -1;
            int slot = slots.Count; slots.Add(slot, device); return slot;
        }
        public static bool Read(int slot, int[] axes, byte[] buttons)
        {
            Reads++;
            if (ThrowRead) throw new IOException("fixture read failure");
            if (!ReadOk || !slots.TryGetValue(slot, out var device) || !device.Connected) return false;
            Array.Copy(device.StateAxes ?? Axes, axes, Axes.Length); Array.Copy(Buttons, buttons, Buttons.Length);
            return true;
        }
        public static void CloseRead() { Closes++; slots.Clear(); }
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
public class Drivetrain
{
    public int gear=2; public float[] gearRatios=new float[7]; public int Shifts;
    public void Shift(int selected,bool force) { gear=selected; Shifts++; }
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
    internal static class GameState
    {
        public static bool IsDriving = true;
        public static EventManager ExistingManager => GameEntryPoint.EventManager;
    }
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
