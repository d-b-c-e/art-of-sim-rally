// Minimal test doubles for ownership and callback ordering. These intentionally
// make no claim to simulate Unity transforms, rendering, destroyed objects or Harmony.
using ArtOfSimRally.Mod;

namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } }
    public class HarmonyPrefix : Attribute { }
    public class HarmonyPostfix : Attribute { }
    public static class AccessTools
    {
        public delegate ref F FieldRef<T, F>(T value);
        public static FieldRef<T,F> FieldRefAccess<T,F>(string name)
        {
            if (name == "CameraAnglesList") return (FieldRef<T,F>)(object)(FieldRef<CarCameras,List<CameraAngle>>)(x => ref x.CameraAnglesList);
            return (FieldRef<T,F>)(object)(FieldRef<CarCameras,CarDynamics>)(x => ref x.cardynamics);
        }
    }
}
namespace UnityEngine
{
    public class MonoBehaviour { public static void DontDestroyOnLoad(object value) { } }
    public enum HideFlags { HideAndDontSave }
    public class GameObject
    {
        public HideFlags hideFlags;
        public GameObject(string name) { }
        public T AddComponent<T>() where T : new() => new T();
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct Quaternion
    {
        public float w;
        public static Quaternion identity => new Quaternion { w=1 };
        public static Quaternion Euler(float x,float y,float z) => identity;
        public static Quaternion operator *(Quaternion a,Quaternion b) => a;
        public static Vector3 operator *(Quaternion a,Vector3 b) => b;
    }
    public class Transform { public Vector3 position, localPosition; public Quaternion rotation, localRotation; }
    public class Camera { public Transform transform = new Transform(); public float fieldOfView=60; }
    public static class Mathf
    {
        public static float Clamp(float v,float min,float max) => Math.Clamp(v,min,max);
        public static float Lerp(float a,float b,float t) => a+(b-a)*Math.Clamp(t,0,1);
    }
    public static class Time { public static float realtimeSinceStartup, unscaledDeltaTime=.016f; public static int frameCount; }
    public static class Application { public static string version="fixture", unityVersion="fixture"; }
}
public class CameraAngle
{
    public enum CameraAngles { CAMERA1 }
    public CameraAngle(float a,float b,float c,CameraAngles angle) { }
}
public class CarCameras
{
    public List<CameraAngle> CameraAnglesList = new List<CameraAngle>();
    public CarDynamics cardynamics;
    public CameraAngle CurrentCameraAngle;
    public UnityEngine.Transform target = new UnityEngine.Transform();
    public bool enabled = true;
    public int Snaps;
    public void RefreshCameraType() { CurrentCameraAngle=CameraAnglesList[0]; }
    public void SetToWantedPositionImmediate() { Snaps++; }
}
public class CameraManager { }
public class CarDynamics { public Axles axles; }
public class Axles { public Axle frontAxle; }
public class Axle { public Wheel leftWheel, rightWheel; }
public class Wheel { public float slipAngle; }
public class UIManager
{
    public static UIManager Instance = new UIManager();
    public PanelManager PanelManager = new PanelManager();
}
public class PanelManager { public UnityEngine.Camera mainCamera = new UnityEngine.Camera(); }
namespace ArtOfSimRally.Mod
{
    internal class Settings
    {
        public bool BonnetCameraEnabled=true, BumperCameraEnabled=true;
        public float BonnetLean=0, BonnetSide=0, BonnetHeight=1, BonnetForward=1, BonnetPitch=0, BonnetFOV=75;
        public float BumperSide=0, BumperHeight=1, BumperForward=1, BumperPitch=0, BumperFOV=75;
        public float FyReference=11500, GainFromStrength=1, Smoothing=0;
        public bool Invert=false;
    }
    internal static class Calls { public static List<string> Log = new List<string>(); }
    internal static class Main { public static Settings Settings = new Settings(); public static bool Enabled=true; }
    internal static class GameState { public static bool IsDriving=false, IsPlayerView=true; }
    internal static class ModLog { public static void Info(string value) { } }
    internal static class CameraTuner
    {
        public static void Update(BonnetCamera.View view) { }
        public static void Flush(bool shutdown=false) => Calls.Log.Add("camera-save");
    }
    internal static class InputBackend { public static void Tick() { } }
    internal static class WheelInput
    {
        public enum Channel { Steer, Throttle, Brake, Clutch, Handbrake }
        public static bool Enabled => true;
        public static float Value(Channel c) => 0;
        public static void Update() { }
        public static void Close() => Calls.Log.Add("input-close");
        public static void FlushLearnedRanges(bool shutdown=false) => Calls.Log.Add("save");
    }
    internal static class FfbNative
    {
        public static void SetForce(int value) => Calls.Log.Add("force:"+value);
        public static void Shutdown() => Calls.Log.Add("native-close");
        public static void ReleaseInputs() => Calls.Log.Add("native-input-close");
    }
    internal static class FfbController { public static void Reset() => Calls.Log.Add("filter-reset"); }
    internal static class TelemetryPump
    {
        public static void Park() => Calls.Log.Add("telemetry-park");
        public static void Shutdown() => Calls.Log.Add("telemetry-close");
    }
    internal static class Shifter { public static void Close() => Calls.Log.Add("shifter-close"); }
}
