using ArtOfSimRally.Mod;
using ArtOfRally.TripleScreen.Mod;
using UnityModManagerNet;

int assertions=0;
void Check(bool condition,string name){assertions++;if(!condition)throw new Exception(name);}
string root=Path.Combine(Path.GetTempPath(),"art-unified-life-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(root,"Mods","ArtOfSimRally"));
Directory.CreateDirectory(Path.Combine(root,"Mods","DbceTripleScreenArtOfRally"));
UnityModManager.ModEntry Owner()=>new(new(){Id="ArtOfSimRally",Version="0.4.0"},Path.Combine(root,"Mods","ArtOfSimRally")+Path.DirectorySeparatorChar);
void Reset(){Main.Calls.Clear();FeatureModule.Calls.Clear();FeatureModule.Fail=false;UnityModManager.modEntries.Clear();}
var owner=Owner();Reset();UnityModManager.modEntries.Add(owner);
Check(UnifiedEntry.Load(owner),"load");
Check(UnityModManager.modEntries.Count==1,"feature must not register another load owner");
Check(FeatureModule.Entry.Path==Path.Combine(root,"Mods","DbceTripleScreenArtOfRally")+Path.DirectorySeparatorChar,"legacy storage retained");
Check(FeatureModule.Entry.Info.Id=="DbceTripleScreenArtOfRally","legacy settings identity");
owner.OnGUI(owner);Check(Main.Calls.Contains("gui"),"wheel page");
UnityEngine.GUILayout.Page=1;owner.OnGUI(owner);Check(FeatureModule.Calls.Contains("gui"),"triple page");
owner.OnSaveGUI(owner);Check(Main.Calls.Contains("save")&&FeatureModule.Calls.Contains("save"),"save both paths");
owner.OnUpdate(owner,.016f);Check(FeatureModule.Calls.Contains("update"),"dispatch triple update");
Check(owner.OnToggle(owner,false),"disable result");Check(Main.Calls.Contains("toggle:False")&&FeatureModule.Calls.Contains("toggle:False"),"disable both");
Check(owner.OnUnload(owner),"unload result");Check(Main.Calls.Contains("unload")&&FeatureModule.Calls.Contains("unload"),"unload both");
Reset();owner=Owner();FeatureModule.Fail=true;Check(!UnifiedEntry.Load(owner),"failed feature load");Check(Main.Calls.Contains("unload"),"wheel cleanup on feature failure");
Reset();owner=Owner();UnityModManager.modEntries.Add(new(new(){Id="DbceTripleScreenArtOfRally",AssemblyName="legacy.dll"},"unused"));
Check(!UnifiedEntry.Load(owner),"legacy registered conflict");Check(Main.Calls.Count==0,"guard before wheel initialization");
Reset();owner=Owner();File.WriteAllText(Path.Combine(root,"Mods","DbceTripleScreenArtOfRally","ArtOfRally.TripleScreen.Mod.dll"),"legacy");
Check(!UnifiedEntry.Load(owner),"legacy DLL conflict");Check(Main.Calls.Count==0,"DLL guard before initialization");
File.Delete(Path.Combine(root,"Mods","DbceTripleScreenArtOfRally","ArtOfRally.TripleScreen.Mod.dll"));
Reset();owner=Owner();UnityModManager.modEntries.Add(new(new(){Id="DbceTripleScreenArtOfRally",AssemblyName="",EntryMethod=""},"unused"));
Check(UnifiedEntry.Load(owner),"metadata-only bridge allowed");owner.OnUnload(owner);
Reset();owner=Owner();string journal=Path.Combine(root,".dbce-art-unified","pending.json");Directory.CreateDirectory(Path.GetDirectoryName(journal));File.WriteAllText(journal,"{}");
Check(!UnifiedEntry.Load(owner),"pending journal guard");Check(Main.Calls.Count==0,"pending guard before wheel load");File.Delete(journal);
Reset();owner=Owner();string duplicate=Path.Combine(root,"Mods","duplicate");Directory.CreateDirectory(duplicate);File.WriteAllText(Path.Combine(duplicate,"Info.json"),"{\"Id\":\"ArtOfSimRally\"}");
Check(!UnifiedEntry.Load(owner),"duplicate metadata ignored by UMM still blocks");Check(Main.Calls.Count==0,"duplicate disk guard before wheel load");File.Delete(Path.Combine(duplicate,"Info.json"));
Reset();owner=Owner();Check(UnifiedEntry.Load(owner),"load for exceptional unload");FeatureModule.ThrowUnload=true;
try{owner.OnUnload(owner);throw new Exception("Expected unload failure");}catch(Exception ex){Check(ex.Message=="fixture unload failure","unload exception retained");}
Check(Main.Calls.Contains("unload"),"wheel cleanup despite triple unload exception");FeatureModule.ThrowUnload=false;
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{status="PASS",assertions,scope="offline callback lifecycle; no runtime rendering or devices"}));

namespace UnityEngine {
 public static class GUILayout { public static int Page;public static int Toolbar(int value,string[] pages)=>Page; }
 public static class JsonUtility {public static T FromJson<T>(string text)=>System.Text.Json.JsonSerializer.Deserialize<T>(text,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});}
}
namespace UnityModManagerNet {
 public static class UnityModManager {
  public static List<ModEntry> modEntries=new();
  public class ModInfo {public string Id,Version,AssemblyName,EntryMethod;}
  public class ModEntry {
   public ModInfo Info;public string Path;public Logger Logger=new();
   public Action<ModEntry> OnGUI,OnSaveGUI;public Action<ModEntry,float> OnUpdate;public Func<ModEntry,bool,bool> OnToggle;public Func<ModEntry,bool> OnUnload;
   public ModEntry(ModInfo info,string path){Info=info;Path=path;}
  }
  public class Logger {public void Error(string message){} }
 }
}
namespace ArtOfSimRally.Mod {
 public static class Main {
  public static List<string> Calls=new();
  public static bool Load(UnityModManager.ModEntry e){Calls.Add("load");e.OnGUI=_=>Calls.Add("gui");e.OnSaveGUI=_=>Calls.Add("save");e.OnToggle=(_,v)=>{Calls.Add("toggle:"+v);return true;};e.OnUnload=_=>{Calls.Add("unload");return true;};return true;}
 }
}
namespace ArtOfRally.TripleScreen.Mod {
 public static class FeatureModule {
  public static bool Fail,ThrowUnload;public static UnityModManager.ModEntry Entry;public static List<string> Calls=new();
  public static bool Load(UnityModManager.ModEntry e){Entry=e;if(Fail)throw new Exception("fixture initialization failure");Calls.Add("load");e.OnGUI=_=>Calls.Add("gui");e.OnSaveGUI=_=>Calls.Add("save");e.OnUpdate=(_,d)=>Calls.Add("update");e.OnToggle=(_,v)=>{Calls.Add("toggle:"+v);return true;};e.OnUnload=_=>{Calls.Add("unload");if(ThrowUnload)throw new Exception("fixture unload failure");return true;};return true;}
 }
}
