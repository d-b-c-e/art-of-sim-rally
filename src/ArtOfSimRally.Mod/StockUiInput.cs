using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Rewired;
using Rewired.Integration.UnityUI;
using Rewired.UI;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class StockUiInput
    {
        private static readonly MenuInputBarrier Barrier = new MenuInputBarrier();
        private static readonly KeyCode[] Keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        // PanelManager's menu shortcuts and PauseScreen's pause/resume actions,
        // verified in build 17584229. The module's configured axes are read below.
        private static readonly int[] MenuActions = { 8, 17, 19, 28, 48, 71, 73 };
        private static readonly int[] ModuleActions = { -1, -1, -1, -1 };
        private static readonly Dictionary<int, int> PolledAxes = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> PolledButtons = new Dictionary<int, int>();
        private static readonly FieldInfo PointerData = AccessTools.Field(typeof(RewiredPointerInputModule), "m_PlayerPointerData");
        internal static void Capture() => Barrier.Capture();
        internal static void Reset() { Barrier.Reset(); PolledAxes.Clear(); PolledButtons.Clear(); }
        internal static void Observe(RewiredStandaloneInputModule module)
        {
            ModuleActions[0] = module.HorizontalActionId;
            ModuleActions[1] = module.VerticalActionId;
            ModuleActions[2] = module.SubmitActionId;
            ModuleActions[3] = module.CancelActionId;
        }
        internal static void DiscardPointerPresses(RewiredStandaloneInputModule module)
        {
            // Drop a native press/drag that began before opening. Do not call
            // ClearSelection: that also erases the game's keyboard selection.
            var players = (Dictionary<int, Dictionary<int, PlayerPointerEventData>[]>)PointerData.GetValue(module);
            foreach (var pointers in players.Values)
                foreach (var pointer in pointers)
                {
                    if (pointer == null) continue; // sparse mouse-source indices
                    foreach (var data in pointer.Values)
                    {
                        data.eligibleForClick = false; data.pointerPress = null;
                        data.rawPointerPress = null; data.pointerDrag = null;
                        data.dragging = false; data.clickCount = 0;
                    }
                }
        }
        internal static bool Blocked
        {
            get
            {
                if (!Main.Enabled) { Reset(); return false; }
                bool visible = Main.SettingsVisible;
                // Catch the opening keyboard edge regardless of Unity Update order.
                if (Main.Settings != null && !CameraKeys.ModifierHeld() &&
                    Input.GetKeyDown(Main.Settings.SettingsKey)) Capture();
                if (visible) return Barrier.Blocks(true, true, true, Time.frameCount);
                if (!Barrier.Captured) return false;
                return Barrier.Blocks(false, Application.isFocused, Held(), Time.frameCount);
            }
        }
        private static bool Held()
        {
            // Exclude joystick buttons here: a parked H-pattern gear must not
            // lock the menu. Only mapped UI actions and our Settings button matter.
            if (HandoffHeld()) return true;
            try
            {
                if (!ReInput.isReady) return true;
                foreach (var player in ReInput.players.AllPlayers)
                {
                    foreach (int action in MenuActions) if (player.GetButton(action)) return true;
                    if (AxisHeld(player, 14)) return true;
                    for (int i = 0; i < ModuleActions.Length; i++)
                        if (ModuleActions[i] >= 0 && (i < 2 ? AxisHeld(player, ModuleActions[i]) : player.GetButton(ModuleActions[i]))) return true;
                    // Replay scrub24/25 and context-specific buttons only hold
                    // handback while that native input path is actually polling.
                    // Allow either Update order; the neutral-frame barrier covers
                    // context teardown without latching unrelated resting pedals.
                    foreach (var axis in PolledAxes)
                        if (Time.frameCount - axis.Value <= 1 && Math.Abs(player.GetAxis(axis.Key)) > .00001f) return true;
                    foreach (var button in PolledButtons)
                        if (Time.frameCount - button.Value <= 1 && player.GetButton(button.Key)) return true;
                }
            }
            catch { return true; } // Teardown is not evidence of control release.
            return false;
        }
        // A programmatic UMM -> game-panel handoff waits only on controls that
        // can actually activate UMM. Unity joystick buttons and Rewired axes may
        // be parked/offset continuously and must not trap the native screen.
        internal static bool HandoffHeld()
        {
            foreach (var key in Keys)
                if (key > KeyCode.None && key < KeyCode.JoystickButton0 && Input.GetKey(key)) return true;
            return WheelInput.Value(WheelInput.Channel.SettingsButton) > .5f;
        }
        private static bool AxisHeld(Player player, int action) => action >= 0 &&
            (player.GetButton(action) || player.GetNegativeButton(action) || Math.Abs(player.GetAxis(action)) > .1f);
        internal static bool ButtonDown(Player player, int action)
        { PolledButtons[action] = Time.frameCount; return !Blocked && player.GetButtonDown(action); }
        internal static bool Button(Player player, int action)
        { PolledButtons[action] = Time.frameCount; return !Blocked && player.GetButton(action); }
        internal static float Axis(Player player, int action)
        { PolledAxes[action] = Time.frameCount; return Blocked ? 0 : player.GetAxis(action); }
    }

    // UMM's Canvas only intercepts raycasts. This stops actual pointer,
    // navigation, submit and cancel dispatch without changing maps or focus.
    [HarmonyPatch(typeof(RewiredStandaloneInputModule), "Process")]
    internal static class StockUiDispatchGuard
    {
        [HarmonyPrefix]
        private static bool BeforeProcess(RewiredStandaloneInputModule __instance)
        {
            StockUiInput.Observe(__instance);
            if (!StockUiInput.Blocked) return true;
            StockUiInput.DiscardPointerPresses(__instance);
            return false;
        }
    }
    [HarmonyPatch(typeof(PanelManager), "Update")]
    internal static class StockPanelInputGuard
    {
        [HarmonyPrefix] private static bool BeforeUpdate() => !StockUiInput.Blocked;
    }
    [HarmonyPatch(typeof(ModsPanel), "Update")]
    internal static class StockModsInputGuard
    {
        [HarmonyPrefix] private static bool BeforeUpdate() => !StockUiInput.Blocked;
    }
    // These updates also maintain replay/tire UI state. Guard their input reads,
    // not the whole update, so presentation continues while settings are open.
    [HarmonyPatch]
    internal static class StockScreenInputGuard
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(PauseScreen), "UpdateMe");
            yield return AccessTools.Method(typeof(ReplayManager), "Update");
        }
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Guard(IEnumerable<CodeInstruction> instructions)
        {
            var down = AccessTools.Method(typeof(Player), "GetButtonDown", new[] { typeof(int) });
            var button = AccessTools.Method(typeof(Player), "GetButton", new[] { typeof(int) });
            var axis = AccessTools.Method(typeof(Player), "GetAxis", new[] { typeof(int) });
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                string wrapper = instruction.Calls(down) ? "ButtonDown" : instruction.Calls(button) ? "Button" : instruction.Calls(axis) ? "Axis" : null;
                if (wrapper != null)
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(StockUiInput), wrapper);
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced == 0) throw new InvalidOperationException("Stock menu input seam changed; no guarded reads found.");
        }
    }
}
