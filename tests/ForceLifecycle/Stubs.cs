namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type,string name) { } }
    public class HarmonyPrefix : Attribute { }
    public class HarmonyPostfix : Attribute { }
}
namespace UnityEngine
{
    public static class Mathf { public static float Abs(float f) => Math.Abs(f); }
    public static class Time { public static float unscaledTime=0; }
}
public class CarDynamics { public bool enableForceFeedback; public float forceFeedback, velo=20; public Axles axles=new(); }
public class Axles { public Axle frontAxle=new(); }
public class Axle { public Wheel leftWheel=new(), rightWheel=new(); }
public class Wheel { public float Fy=5000, slipAngle=5, idealSlipAngle=10, steering, Mz; }
namespace ArtOfSimRally.Mod
{
    internal class Settings
    {
        public bool ForceFeedbackEnabled=true, Invert=false, DiagnosticLogging=false;
        public float FyReference=11500, GainFromStrength=.5f, Smoothing=.2f;
        public int Strength=50;
    }
    internal static class Main { public static Settings Settings=new(); public static bool Enabled=true; }
    internal static class GameState { public static bool IsDriving=true; }
    internal static class ModLog { public static void Info(string text) { } }
    internal static class FfbNative
    {
        public static bool Ready=true;
        public const int ForceMax=10000;
        public static int Last, Sends;
        public static void SetForce(int force) { Last=force; Sends++; }
    }
}
