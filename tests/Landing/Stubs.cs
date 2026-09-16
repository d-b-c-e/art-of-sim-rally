using System.Numerics;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new(0,1,0);
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
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
    public class Collider { public bool Road; public bool CompareTag(string tag) => Road&&tag=="Road"; }
    public struct ContactPoint { public Vector3 normal; }
    public class Collision
    {
        public Collider collider=new(); public Vector3 relativeVelocity;
        public ContactPoint[] points=Array.Empty<ContactPoint>(); public int Reads;
        public int contactCount => points.Length;
        public ContactPoint GetContact(int i) { Reads++;return points[i]; }
    }
    public static class Time { public static float fixedTime,realtimeSinceStartup; }
    public static class Application { public static bool isFocused=true; }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type,string name) { } }
    public class HarmonyPrefix : Attribute { }
}
public class PlayerCollider
{
    public UnityEngine.Rigidbody body;
    public T GetComponent<T>() where T:class => body as T;
}
public class EventManager { public PlayerManager playerManager=new(); }
public class PlayerManager { public UnityEngine.Rigidbody playerRigidBody; }
public class Wheel { public bool onGroundDown=true; public float compression,suspensionTravel=.25f; }
public class Axle { public Wheel leftWheel=new(),rightWheel=new(); }
public class Axles { public Axle frontAxle=new(),rearAxle=new(); }
public class CarDynamics
{
    public Axles axles=new(); public UnityEngine.Rigidbody body=new();
    public T GetComponent<T>() where T:class => body as T;
}
namespace ArtOfSimRally.Mod
{
    internal class Settings { public bool ForceFeedbackEnabled=true,LandingEffectsEnabled=true,DiagnosticLogging=true,CrashEffectsEnabled; public float LandingStrength=5,CrashStrength=5; }
    internal static class Main { public static Settings Settings=new(); public static bool Enabled=true; }
    internal static class GameState { public static bool IsDriving,IsRestarting; public static EventManager ExistingManager=new(); }
    internal static class FfbNative { public static bool Ready=true; }
    internal static class ModLog { public static void Info(string s) { } public static void Warning(string s) { } }
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
