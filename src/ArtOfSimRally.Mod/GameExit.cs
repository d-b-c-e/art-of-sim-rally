using HarmonyLib;

namespace ArtOfSimRally.Mod
{
    // The game's menu Quit kills its process, so Unity never gets to run our
    // watchdog's OnApplicationQuit. Release outputs before entering that method.
    [HarmonyPatch(typeof(ExitGame), "Exit")]
    internal static class GameExit
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Before() { ModWatchdog.Shutdown(); return true; }
    }
}
