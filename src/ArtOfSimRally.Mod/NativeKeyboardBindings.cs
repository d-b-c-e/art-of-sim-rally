using System;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class NativeKeyboardBindings
    {
        // Called only during an explicit edit, never from driving/input polling.
        // Include temporarily disabled maps: the game's binder disables a map
        // while editing it, and a different context can enable it after closing.
        // A modified game chord also conflicts with our bare-key camera polling.
        internal static bool Available(KeyCode key, out string reason)
        {
            reason = "Game keyboard bindings are unavailable. Open game controls, then retry; previous keys kept.";
            try
            {
                if (!ReInput.isReady) return false;
                var player = ReInput.players.GetPlayer(0);
                if (player == null) return false;
                bool sawMap = false;
                foreach (var map in player.controllers.maps.GetAllMaps(ControllerType.Keyboard))
                {
                    if (map == null) continue;
                    sawMap = true;
                    foreach (var entry in map.AllMaps)
                    {
                        if (entry == null || entry.keyCode != key) continue;
                        var action = ReInput.mapping.GetAction(entry.actionId);
                        string name = action == null ? "action #" + entry.actionId : action.name;
                        reason = CameraKeys.Name(key) + " is assigned to game action " + name +
                            ". Choose another key or change it in game controls; previous keys kept.";
                        return false;
                    }
                }
                if (!sawMap) return false;
                reason = "";
                return true;
            }
            catch { return false; } // Never accept an unchecked assignment after teardown/read failure.
        }
    }
}
