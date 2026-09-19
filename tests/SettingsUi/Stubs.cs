using UnityEngine;
namespace UnityModManagerNet
{
    public static class UnityModManager
    {
        public class ModSettings { public virtual void Save(ModEntry e){} public static string GetPath(ModEntry e)=>"unused"; }
        public class ModEntry { public ModInfo Info=new(); }
        public class ModInfo { public string Version="fixture",DisplayName="fixture"; }
        public static List<ModEntry> modEntries=new();
        public class UI
        {
            public static UI Instance=new(); public int tabId; public bool Opened; public int Changes;
            private int ShowModSettings { get; set; }
            private string mModFilter="";
            public string Filter=>mModFilter;
            public int Selected=>ShowModSettings;
            public void ToggleWindow(bool open){Opened=open; Changes++;}
        }
    }
}
namespace ArtOfSimRally.Mod
{
    internal static partial class WheelInput
    {
        static readonly string[] AxisNames={"X","Y","Z","Rx","Ry","Rz","Slider 1","Slider 2"};
        public enum Channel { SettingsButton, StopFfbButton }
        public static readonly HashSet<Channel> Pressed=new();
        public static bool ShortcutPressed(Channel c)=>Pressed.Remove(c);
        public static float SettingsHeld;
        public static float Value(Channel c)=>c==Channel.SettingsButton?SettingsHeld:0;
    }
    internal static class Input
    {
        public static readonly HashSet<KeyCode> Down=new();
        public static readonly HashSet<KeyCode> Held=new();
        public static bool GetKey(KeyCode key)=>Held.Contains(key);
        public static bool GetKeyDown(KeyCode key)=>Down.Contains(key);
    }
    internal static class Time { public static float realtimeSinceStartup=100; public static int frameCount; }
    internal static class Application { public static bool isFocused=true; }
    internal static class GameState { public static bool IsDriving; }
    internal static class CameraKeys { public static bool Modified=false; public static bool ModifierHeld()=>Modified; public static void Tick(){} }
    internal static class CameraTuner { public static int Suppressed; public static void SuppressUntilRelease()=>Suppressed++; public static void ReadResetButton(){} }
    internal static class Shifter { public static void SuppressUntilRelease(){} }
    internal static class SettingsPanel
    {
        public static bool Editing;
        public static bool CancelPendingEdit(){bool result=Editing; Editing=false; return result;}
    }
    internal static class FfbNative
    {
        public static bool Ready=true;
        public static int Stops,Shutdowns;
        public static void StopOutputs()=>Stops++;
        public static void Shutdown(){Stops++;Shutdowns++;Ready=false;}
    }
    public static partial class Main
    {
        internal static Settings Settings=new(); internal static bool Enabled=true;
        internal static bool SettingsVisible=>UnityModManagerNet.UnityModManager.UI.Instance.Opened;
        private static UnityModManagerNet.UnityModManager.ModEntry _modEntry=new();
        public static int Saves,Requests,Cancels; public static bool WriteSucceeds=true;
        public static bool SaveSettings(){Saves++;if(WriteSucceeds){_uiDirty=false;SettingsSaveStatus="Saved";}else SettingsSaveStatus="Write failed";return WriteSucceeds;}
        public static void ReopenForceFeedback(){if(Settings.ForceFeedbackEnabled)Requests++;}
        public static void CancelForceRecovery()=>Cancels++;
        internal static void ResetFixture()
        {
            _uiDirty=false;_uiSaveAt=0;_settingsWereVisible=false;SuppressHostClose=false;
            Settings=new();Enabled=true;Saves=Requests=Cancels=0;WriteSucceeds=true;
            UnityModManagerNet.UnityModManager.modEntries.Clear();UnityModManagerNet.UnityModManager.modEntries.Add(_modEntry);
        }
    }
}
