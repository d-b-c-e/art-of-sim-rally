using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using ArtOfSimRally.Mod;
using UnityEngine;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Patch(string nested, string method, params object[] args) => typeof(BonnetCamera)
        .GetNestedType(nested, BindingFlags.NonPublic).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    static CarCameras Mount()
    {
        ArtOfSimRally.Mod.Main.Enabled=true; ArtOfSimRally.Mod.Main.Settings=new Settings(); GameState.IsPlayerView=true;
        var rig = new CarCameras(); UIManager.Instance.PanelManager.mainCamera=new Camera();
        rig.CameraAnglesList.Add(new CameraAngle(1,1,1,CameraAngle.CameraAngles.CAMERA1));
        Patch("AddToRotation", "Append", rig);
        rig.CurrentCameraAngle = rig.CameraAnglesList[1];
        Patch("DriveCamera", "Mount", rig);
        // Give the test double the local offset Unity derives from a world write.
        UIManager.Instance.PanelManager.mainCamera.transform.localPosition=new Vector3(3,4,5);
        UIManager.Instance.PanelManager.mainCamera.transform.localRotation=new Quaternion {w=.2f};
        return rig;
    }
    static void Released(Camera cam) => Check(cam.transform.localPosition.x==0 && cam.transform.localPosition.y==0 &&
        cam.transform.localPosition.z==0 && cam.transform.localRotation.w==1 && cam.fieldOfView==60, "camera child/FOV not restored");
    static void Cameras()
    {
        var rig=Mount(); var camera=UIManager.Instance.PanelManager.mainCamera;
        rig.CurrentCameraAngle=new CameraAngle(1,1,1,CameraAngle.CameraAngles.CAMERA1);
        Patch("DriveCamera", "Mount", rig); Released(camera); Check(rig.Snaps==1,"stock handback did not snap");
        BonnetCamera.Release(true); Check(rig.Snaps==1,"double release moved rig");
        rig=Mount(); camera=UIManager.Instance.PanelManager.mainCamera;
        rig.enabled=false; GameState.IsPlayerView=false;
        Patch("CinematicHandback", "Before"); Released(camera); Check(rig.Snaps==0,"cinematic handback fought parent");
        rig=Mount(); camera=UIManager.Instance.PanelManager.mainCamera;
        Patch("IntroHandback", "Before"); Released(camera); Check(rig.Snaps==0,"intro parent changed");
        rig=Mount(); camera=UIManager.Instance.PanelManager.mainCamera;
        var other=new Camera(); other.transform.localPosition=new Vector3(9,9,9);
        UIManager.Instance.PanelManager.mainCamera=other;
        ArtOfSimRally.Mod.Main.Enabled=false; BonnetCamera.ReleaseIfInactive(); Released(camera);
        Check(other.transform.localPosition.x==9,"handback reset an unrelated camera");
        rig=Mount(); camera=UIManager.Instance.PanelManager.mainCamera;
        ArtOfSimRally.Mod.Main.Settings.BonnetCameraEnabled=false; BonnetCamera.ReleaseIfInactive(); Released(camera);
        Check(BonnetCamera.ActiveView(rig)==BonnetCamera.View.None && rig.CameraAnglesList.Count==3,"disabled mounted placeholder stayed active/rotation changed");
        Patch("DriveCamera", "Mount", rig); Check(camera.fieldOfView==60,"disabled view retook camera");
        rig=Mount(); camera=UIManager.Instance.PanelManager.mainCamera;
        rig.enabled=false; BonnetCamera.ReleaseIfInactive(); Released(camera);
    }
    static void Shutdown()
    {
        Recovery();
        Calls.Log.Clear(); ModWatchdog.Shutdown();
        int disk=Calls.Log.IndexOf("save");
        Check(Calls.Log.IndexOf("native-input-close")<Calls.Log.IndexOf("diagnostic-save"),"diagnostics saved before outputs released");
        Check(Calls.Log.IndexOf("native-input-close")<Calls.Log.IndexOf("shifter-save"),"shifter selection saved before outputs released");
        Check(Calls.Log[0]=="force:0", "shutdown did not zero force first");
        foreach(string call in new[] {"filter-reset","landing-stop","native-close","telemetry-park","telemetry-close","shifter-close","input-close","native-input-close"})
            Check(Calls.Log.IndexOf(call)>=0 && Calls.Log.IndexOf(call)<disk &&
                Calls.Log.IndexOf(call)<Calls.Log.IndexOf("camera-save"), call+" happened after save");
        var watchdog=new ModWatchdog(); Calls.Log.Clear(); ArtOfSimRally.Mod.Main.Enabled=true; GameState.IsDriving=true;
        typeof(ModWatchdog).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(watchdog,null);
        Check(!Calls.Log.Contains("telemetry-prepare") && Calls.Log.Contains("telemetry-stop-disabled"), "driving watchdog connected/did not check telemetry disable");
        Check(!Calls.Log.Contains("force-recover"), "driving watchdog attempted FFB acquisition");
        Check(Calls.Log.Contains("landing-tick"), "landing lifecycle not observed during driving");
        Check(!Calls.Log.Contains("save") && !Calls.Log.Contains("camera-save") && !Calls.Log.Contains("diagnostic-save") && !Calls.Log.Contains("shifter-save"),"watchdog saved while driving");
        GameState.IsDriving=false;
        Calls.Log.Clear();
        typeof(ModWatchdog).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(watchdog,null);
        Check(Calls.Log.IndexOf("force:0")<Calls.Log.IndexOf("save"),"idle save preceded release");
        Check(Calls.Log.IndexOf("telemetry-prepare")>Calls.Log.IndexOf("force:0"),"connection setup preceded force release");
        Check(Calls.Log.IndexOf("force-recover")>Calls.Log.IndexOf("force:0"), "FFB acquisition preceded force release");
        Check(Calls.Log.IndexOf("landing-tick")>Calls.Log.IndexOf("force:0"), "landing preparation preceded force release");
        Check(Calls.Log.IndexOf("force:0")<Calls.Log.IndexOf("camera-save"),"camera save preceded release");
        Check(Calls.Log.IndexOf("force:0")<Calls.Log.IndexOf("diagnostic-save"),"diagnostics idle save preceded release");
        Calls.Log.Clear(); ArtOfSimRally.Mod.Main.Enabled=false;
        typeof(ModWatchdog).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(watchdog,null);
        Check(Calls.Log.Contains("camera-save"),"disabled mod lost camera save retry");
        var rig=Mount(); var camera=UIManager.Instance.PanelManager.mainCamera;
        ArtOfSimRally.Mod.Main.Enabled=false; ModWatchdog.Shutdown(unloading:true); Released(camera);
        Check(BonnetCamera.ActiveView(rig)==BonnetCamera.View.None,"unload left mounted placeholder active");
    }
    static void Recovery()
    {
        var retry = new FfbReconnect(); var window = new IntPtr(123);
        retry.Request();
        Check(!retry.TryBegin(0,true,false,false,IntPtr.Zero), "acquired without game window");
        Check(!retry.TryBegin(1,true,false,false,window), "unstable startup window acquired");
        Check(!retry.TryBegin(2,true,true,false,window), "acquired during driving");
        Check(!retry.TryBegin(3,true,false,false,window), "driving interruption did not reset stability");
        Check(retry.TryBegin(3.5,true,false,false,window), "idle startup attempt missing");
        retry.Complete(false);
        Check(!retry.TryBegin(8.49,true,false,false,window), "failed acquisition retried too soon");
        Check(retry.TryBegin(8.5,true,false,false,window), "transient failure did not retry");
        retry.Complete(true);
        Check(!retry.TryBegin(100,true,false,false,window), "successful acquisition reopened wheel");
        retry.Request();
        Check(!retry.TryBegin(101,true,false,true,window), "axis assignment interrupted");
        Check(!retry.TryBegin(102,false,false,false,window), "disabled FFB acquired");
        Check(!retry.TryBegin(103,true,false,false,window), "request skipped stability interval");
        for (int i=0; i<5; i++) Check(retry.TryBegin(104+i*5,true,false,false,window), "retry budget lost");
        Check(!retry.Pending && !retry.TryBegin(500,true,false,false,window), "retry budget unbounded");
        retry.Request(); retry.Cancel();
        Check(!retry.Pending, "shutdown retained a pending acquisition");
    }
    static void CameraCompatibility()
    {
        ArtOfSimRally.Mod.Main.OtherCameraModLoaded=true;
        foreach(bool externalFirst in new[]{false,true})
        {
            var rig=new CarCameras();
            for(int i=0;i<8;i++) rig.CameraAnglesList.Add(new CameraAngle(1,1,1,CameraAngle.CameraAngles.CAMERA1));
            var externalA=new CameraAngle(7,2,-1,CameraAngle.CameraAngles.CAMERA1);
            var externalB=new CameraAngle(10,3,-1.5f,CameraAngle.CameraAngles.CAMERA1);
            if(externalFirst) { rig.CameraAnglesList.Add(externalA); rig.CameraAnglesList.Add(externalB); }
            Patch("AddToRotation","Append",rig);
            if(!externalFirst) { rig.CameraAnglesList.Add(externalA); rig.CameraAnglesList.Add(externalB); }
            Check(rig.CameraAnglesList.Count==10,"camera mods competed for rotation slots");
            Check(ReferenceEquals(rig.CameraAnglesList[8],externalA) && ReferenceEquals(rig.CameraAnglesList[9],externalB),"external camera slots 8/9 displaced");
            rig.CurrentCameraAngle=externalA;
            Check(BonnetCamera.ActiveView(rig)==BonnetCamera.View.None,"external camera taken as mounted view");
            var camera=new Camera(); UIManager.Instance.PanelManager.mainCamera=camera;
            Patch("DriveCamera","Mount",rig);
            Check(camera.fieldOfView==60,"mounted FOV applied to external chase camera");
        }
        ArtOfSimRally.Mod.Main.OtherCameraModLoaded=false;
        var own=Mount(); Check(BonnetCamera.ActiveView(own)==BonnetCamera.View.Bonnet,"mounted views did not work without external mod");
        BonnetCamera.Release(true);
    }
    static int Main(string[] args)
    {
        try
        {
            Cameras(); Shutdown(); CameraCompatibility();
            Console.WriteLine(JsonSerializer.Serialize(new {status="passed",assertions})); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
