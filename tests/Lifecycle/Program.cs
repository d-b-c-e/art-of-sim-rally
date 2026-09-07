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
        Calls.Log.Clear(); ModWatchdog.Shutdown();
        int disk=Calls.Log.IndexOf("save");
        Check(Calls.Log[0]=="force:0", "shutdown did not zero force first");
        foreach(string call in new[] {"filter-reset","native-close","telemetry-park","telemetry-close","shifter-close","input-close","native-input-close"})
            Check(Calls.Log.IndexOf(call)>=0 && Calls.Log.IndexOf(call)<disk, call+" happened after save");
        var watchdog=new ModWatchdog(); Calls.Log.Clear(); ArtOfSimRally.Mod.Main.Enabled=true; GameState.IsDriving=true;
        typeof(ModWatchdog).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(watchdog,null);
        Check(!Calls.Log.Contains("save"),"watchdog saved while driving");
        GameState.IsDriving=false;
        typeof(ModWatchdog).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(watchdog,null);
        Check(Calls.Log.IndexOf("force:0")<Calls.Log.IndexOf("save"),"idle save preceded release");
        var rig=Mount(); var camera=UIManager.Instance.PanelManager.mainCamera;
        ArtOfSimRally.Mod.Main.Enabled=false; ModWatchdog.Shutdown(unloading:true); Released(camera);
        Check(BonnetCamera.ActiveView(rig)==BonnetCamera.View.None,"unload left mounted placeholder active");
    }
    static string Capture(string root)
    {
        GameState.IsDriving=true; DriveCapture.Start(root); Check(!DriveCapture.Active,"capture started during driving");
        GameState.IsDriving=false; DriveCapture.Start(root); Check(DriveCapture.Active,"capture did not start");
        ArtOfSimRally.Mod.Main.Settings=new Settings();
        for(int i=0;i<3;i++)
        {
            Time.frameCount=i+1; Time.realtimeSinceStartup=1+i*.02f;
            DriveCapture.RecordFrame(.02f,true);
            DriveCapture.RecordForce(5750,0,8.5f,12,i == 0 ? 0 : .5f,.5f);
        }
        DriveCapture.RecordForceReset();
        Time.realtimeSinceStartup += .02f;
        DriveCapture.RecordForce(-5750,0,8.5f,12,0,-.5f);
        GameState.IsDriving=true; DriveCapture.Stop(); Check(DriveCapture.Active,"capture wrote while driving");
        Check(!Directory.Exists(root),"record callbacks wrote to disk");
        GameState.IsDriving=false; DriveCapture.Stop(); Check(!DriveCapture.Pending,"capture not finalized");
        string path=Directory.GetDirectories(root).Single();
        var manifest=XDocument.Load(Path.Combine(path,"manifest.xml")).Root;
        Check((string)manifest.Attribute("complete")=="true" && (int)manifest.Element("frames").Attribute("count")==3,"bad capture receipt");
        return path;
    }
    static int Main(string[] args)
    {
        try
        {
            Cameras(); Shutdown(); string capture=Capture(Path.GetFullPath(args[0]));
            Console.WriteLine(JsonSerializer.Serialize(new {status="passed",assertions,syntheticCapture=capture})); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
