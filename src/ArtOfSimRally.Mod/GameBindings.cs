using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Use the game's own panel stack and remapper. UIManager.Instance is a lazy
    // prefab factory: never call it merely to discover whether controls exist.
    internal static class GameBindings
    {
        private static readonly MenuInputBarrier Handoff = new MenuInputBarrier();
        private static PanelManager _pendingManager;
        private static global::Panel _pendingTarget;
        internal static string Status { get; private set; } = "";
        internal static void Open()
        {
            if (_pendingTarget != null)
            { Status = "Game controls are opening; release the mouse and keyboard."; return; }
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
                _pendingManager = manager;
                _pendingTarget = target;
                Handoff.Capture();
                Status = "Opening game controls; release the mouse and keyboard.";
            }
            catch (Exception ex)
            { Status = "Could not open game controls; use Options → Controls. " + ex.Message; ModLog.Warning(Status); }
        }
        // The UMM button's pointer/key event is still live in the frame that
        // closes its window. Defer the native panel until that initiating input
        // has been released for two frames. Do not wait on Rewired UI axes here:
        // wheel/pedal maps can rest at a non-zero value and latch the general
        // stock-menu barrier forever, leaving the animated panel with no input.
        internal static void Tick()
        {
            if (_pendingTarget == null) return;
            if (Main.SettingsVisible)
            { CancelPending(); Status = "Opening game controls cancelled."; return; }
            bool held = StockUiInput.HandoffHeld();
            if (Handoff.Blocks(false, Application.isFocused, held, Time.frameCount)) return;
            var manager = _pendingManager;
            var target = _pendingTarget;
            CancelPending();
            if (manager == null || target == null || !target.gameObject.activeInHierarchy ||
                manager.GetPanelStackCount() == 0 || manager.IsInIntroductionSequence)
            { Status = "Game controls changed before opening. Use Options → Controls."; return; }
            try
            {
                // The initiating pointer press has been discarded by the normal
                // UMM close barrier. Its persistent Rewired axis state must not
                // continue owning native input after this explicit handoff.
                StockUiInput.Reset();
                manager.AddPanelAddToHistory(target);
                Status = "Game controls opened. Press Settings to return to Wheel settings.";
            }
            catch (Exception ex)
            { Status = "Could not open game controls; use Options → Controls. " + ex.Message; ModLog.Warning(Status); }
        }
        internal static void CancelPending()
        {
            _pendingManager = null;
            _pendingTarget = null;
            Handoff.Reset();
        }
    }
}
