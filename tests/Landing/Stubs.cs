using System.Numerics;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new(0,1,0);
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public static Vector3 operator *(Quaternion q,Vector3 p)
        {
            var v=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(p.x,p.y,p.z),new System.Numerics.Quaternion(q.x,q.y,q.z,q.w));
            return new Vector3(v.X,v.Y,v.Z);
        }
    }
    public class Rigidbody { public Vector3 position,velocity; public Quaternion rotation=new(){w=1}; }
    public static class Time { public static float fixedTime,realtimeSinceStartup; }
    public static class Application { public static bool isFocused=true; }
}
public class Wheel { public bool onGroundDown=true; }
public class Axle { public Wheel leftWheel=new(),rightWheel=new(); }
public class Axles { public Axle frontAxle=new(),rearAxle=new(); }
public class CarDynamics
{
    public Axles axles=new(); public UnityEngine.Rigidbody body=new();
    public T GetComponent<T>() where T:class => body as T;
}
namespace ArtOfSimRally.Mod
{
    internal class Settings { public bool ForceFeedbackEnabled=true,LandingEffectsEnabled=true,DiagnosticLogging=true; public float LandingStrength=5; }
    internal static class Main { public static Settings Settings=new(); public static bool Enabled=true; }
    internal static class GameState { public static bool IsDriving,IsRestarting; }
    internal static class FfbNative { public static bool Ready=true; }
    internal static class ModLog { public static void Info(string s) { } }
}
namespace Dbce.Wheel.Ffb
{
    internal static class WheelFfbNative
    {
        public static int Creates,Plays,Stops,Releases;
        public static float LastMagnitude;
        public static int CreatePeriodicBurst(int hz,int duration) { Creates++;return 0; }
        public static bool PlayPeriodicBurst(int slot,float magnitude,float hz) { Plays++;LastMagnitude=magnitude;return true; }
        public static bool StopPeriodicBurst(int slot) { Stops++;return true; }
        public static void ReleasePeriodics() { Releases++; }
    }
}
