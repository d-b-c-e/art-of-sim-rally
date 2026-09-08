using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // These test-only names shadow Unity's native input/clock ECalls when linking
    // the production tuner. No Unity scene, GUI or physical input is simulated.
    internal static class Input
    {
        public static readonly HashSet<KeyCode> Held = new();
        public static readonly HashSet<KeyCode> Pressed = new();
        public static bool anyKey => Held.Count != 0;
        public static bool GetKey(KeyCode key) => key != KeyCode.None && Held.Contains(key);
        public static bool GetKeyDown(KeyCode key) => key != KeyCode.None && Pressed.Contains(key);
        public static void Release() { Held.Clear(); Pressed.Clear(); }
    }
    internal static class Time { public static float unscaledTime, unscaledDeltaTime = .02f; }
    internal static class GameState { public static bool IsDriving = false; }
    internal static class BonnetCamera { public enum View { None, Bonnet, Bumper } }
    internal static class ModLog
    {
        public static readonly List<string> Messages = new();
        public static void Info(string message) => Messages.Add(message);
    }
    internal static class Main
    {
        public static Settings Settings = new();
        public static bool Enabled = true, SettingsVisible = false;
        public static string Path;
        public static int Saves;
        public static bool SaveSettings()
        {
            Saves++;
            try { SettingsPersistence.Write(Settings, Path); return true; }
            catch (IOException) { return false; }
        }
    }
}
