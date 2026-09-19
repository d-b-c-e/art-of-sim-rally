using System.Text.Json;
using ArtOfSimRally.Mod;
using Host=ArtOfSimRally.Mod.Main;

static class Program
{
    static int assertions;
    static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
    static int Main()
    {
        try
        {
            var manager=new PanelManager();var panel=new Panel();var remapper=new ControlsRemapper{Owner=manager,Target=panel};
            UnityEngine.Object.All.Add(manager);UnityEngine.Object.All.Add(remapper);
            foreach(string guard in new[]{"driving","edit","empty-stack","intro","already-open","inactive-panel","foreign-owner","missing","ambiguous"})
            {
                GameState.IsDriving=guard=="driving";SettingsPanel.Editing=guard=="edit";manager.Count=guard=="empty-stack"?0:1;
                manager.IsInIntroductionSequence=guard=="intro";manager.ControlsOpen=guard=="already-open";
                panel.gameObject.activeInHierarchy=guard!="inactive-panel";remapper.Owner=guard=="foreign-owner"?new():manager;
                remapper.Target=guard=="missing"?null:panel;Host.SettingsVisible=true;
                if(guard=="ambiguous")UnityEngine.Object.All.Add(new ControlsRemapper{Owner=manager,Target=new Panel()});
                GameBindings.Open();Check(manager.Adds==0&&Host.SettingsVisible,"route changed host under guard: "+guard);
                if(guard=="ambiguous")UnityEngine.Object.All.RemoveAt(UnityEngine.Object.All.Count-1);
            }
            GameBindings.Open();Check(manager.Adds==1&&!Host.SettingsVisible&&manager.Last==panel,"route failed to close UMM before using game stack");
            Check(GameBindings.Status.Contains("opened"),"success status missing");
            Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,liveUi=false}));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
namespace UnityEngine
{
    public class Object
    {
        public static readonly List<Object> All=new();
        public static T FindObjectOfType<T>() where T:Object=>All.OfType<T>().FirstOrDefault();
        public static T[] FindObjectsOfType<T>() where T:Object=>All.OfType<T>().ToArray();
    }
}
public class GameObject { public bool activeInHierarchy=true; }
public class Panel:UnityEngine.Object { public string name="ControlsSettings";public GameObject gameObject=new(); }
public class ControlsRemapper:UnityEngine.Object
{
    public PanelManager Owner;public Panel Target;
    public T GetComponentInParent<T>() where T:class => (typeof(T)==typeof(Panel)?(object)Target:Owner) as T;
}
public class PanelManager:UnityEngine.Object
{
    public int Count=1,Adds;public bool IsInIntroductionSequence,ControlsOpen;public Panel Last;
    public int GetPanelStackCount()=>Count;
    public bool isControlsSettingsPanelInStack()=>ControlsOpen;
    public void AddPanelAddToHistory(Panel panel){if(Host.SettingsVisible)throw new Exception("UMM still owns focus");Adds++;Last=panel;}
}
namespace ArtOfSimRally.Mod
{
    public static class Main { public static bool SettingsVisible=true;public static void CloseSettings()=>SettingsVisible=false; }
    public static class GameState { public static bool IsDriving; }
    public static class SettingsPanel { public static bool Editing; }
    public static class ModLog { public static void Warning(string value){} }
}
