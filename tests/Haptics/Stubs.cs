namespace HarmonyLib
{
    public class HarmonyPatch : System.Attribute { public HarmonyPatch(System.Type t, string m) {} }
    public class HarmonyPostfix : System.Attribute { }
}
namespace UnityEngine
{
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c) { x=a;y=b;z=c; } public static Vector3 up => new Vector3(0,1,0); }
    public struct Quaternion { public static Vector3 operator *(Quaternion q,Vector3 v) => v; }
    public class Rigidbody { public Vector3 position,velocity; public Quaternion rotation=new Quaternion(); }
    public static class Time { public static float fixedTime,realtimeSinceStartup; }
    public static class Application { public static bool isFocused=true; }
}
public class Wheel { public bool onGroundDown=true; }
public class Axle { public Wheel leftWheel=new Wheel(),rightWheel=new Wheel(); }
public class Axles { public Axle frontAxle=new Axle(),rearAxle=new Axle(); }
public class CarDynamics
{
    public Axles axles=new Axles(); public UnityEngine.Rigidbody Body=new UnityEngine.Rigidbody();
    public T GetComponent<T>() where T:class => Body as T;
}
namespace ArtOfSimRally.Mod
{
    public class Settings
    { public bool TelemetryEnabled=true,ShakerLandingEnabled=true,DiagnosticLogging=true,ForceFeedbackEnabled=false; public int ShakerLandingStrength=50; }
    public static class Main { public static bool Enabled=true; public static Settings Settings=new Settings(); }
    public static class GameState { public static bool IsDriving,IsRestarting; }
    public static class ModLog { public static void Info(string v) {} public static void Error(string v) { throw new System.Exception(v); } }
}
