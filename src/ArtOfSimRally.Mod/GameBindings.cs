using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Use the game's own panel stack and remapper. UIManager.Instance is a lazy
    // prefab factory: never call it merely to discover whether controls exist.
    internal static class GameBindings
    {
        internal static string Status { get; private set; } = "";
        internal static void Open()
        {
            if (GameState.IsDriving || SettingsPanel.Editing)
            { Status = "Pause and finish or cancel the current edit first."; return; }
            var manager = UnityEngine.Object.FindObjectOfType<PanelManager>();
            if (manager == null || manager.GetPanelStackCount() == 0 || manager.IsInIntroductionSequence)
            { Status = "Open the game's pause or Options menu first, then retry. No game menu was created."; return; }
            if (manager.isControlsSettingsPanelInStack())
            { Status = "Game controls are already open; close Wheel settings to use them."; return; }
            global::Panel target = null;
            foreach (var remapper in UnityEngine.Object.FindObjectsOfType<ControlsRemapper>())
            {
                var panel = remapper.GetComponentInParent<global::Panel>();
                if (panel == null || panel.name != "ControlsSettings" || !panel.gameObject.activeInHierarchy ||
                    remapper.GetComponentInParent<PanelManager>() != manager) continue;
                if (target != null && target != panel)
                { Status = "More than one controls panel is available. Use the game's Options → Controls."; return; }
                target = panel;
            }
            if (target == null)
            { Status = "Game controls are unavailable in this scene. Use Options → Controls from the main menu."; return; }
            try
            {
                Main.CloseSettings();
                if (Main.SettingsVisible) return;
                manager.AddPanelAddToHistory(target);
                Status = "Game controls opened. Press Settings to return to Wheel settings.";
            }
            catch (Exception ex)
            { Status = "Could not open game controls; use Options → Controls. " + ex.Message; ModLog.Warning(Status); }
        }
    }
}
