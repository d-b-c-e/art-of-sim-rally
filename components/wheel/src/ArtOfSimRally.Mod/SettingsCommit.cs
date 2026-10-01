using System;

namespace ArtOfSimRally.Mod
{
    // Explicit binding edits run only while idle. The serializer sees the draft,
    // but effective readers are swapped by the caller only after a successful
    // atomic file replacement. No callback/frame can observe a failed draft.
    internal static class SettingsCommit
    {
        public static bool TrySave(Action apply, Action restore)
        {
            if (GameState.IsDriving) return false;
            try { apply(); if (Main.SaveSettings()) return true; }
            catch { /* A host that throws gets the same rollback as false. */ }
            restore();
            return false;
        }
    }
}
