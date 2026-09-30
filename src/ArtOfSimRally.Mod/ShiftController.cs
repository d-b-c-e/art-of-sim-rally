using System;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Observe the game's completed gear change. Button presses can be rejected,
    // delayed by the clutch or made by a separate shifter; the engaged gear is
    // the common event for all of them. Never alter the drivetrain or rumble.
    [HarmonyPatch(typeof(Drivetrain), "FixedUpdate")]
    internal static class ShiftController
    {
        internal static bool Engaged(int before, int after, int neutral)
            => before != after && after >= 0 && after != neutral;

        [HarmonyPrefix]
        private static void Before(Drivetrain __instance, ref int __state)
            => __state = __instance.gear;

        [HarmonyPostfix]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void After(Drivetrain __instance, int __state)
        {
            if (!Engaged(__state, __instance.gear, __instance.neutral) ||
                !ImpactController.Available(ImpactKind.Shift) || !FfbNative.Ready ||
                !Application.isFocused || !GameState.IsDriving || GameState.IsRestarting) return;
            try
            {
                var player = GameState.ExistingManager?.playerManager?.carcontroller;
                if (player == null || !ReferenceEquals(player.GetComponent<Drivetrain>(), __instance)) return;
                float now = Time.realtimeSinceStartup;
                if (float.IsNaN(now) || float.IsInfinity(now) || now < 0) return;
                var result = ImpactController.Trigger(ImpactKind.Shift, 1f, Main.Settings.ShiftStrength, now);
                if (Main.Settings.DiagnosticLogging)
                    ModLog.Info($"Shift FFB gear={__state}->{__instance.gear} magnitude={ImpactController.Magnitude(ImpactKind.Shift):F4} result={result}");
            }
            catch (Exception ex) { ModLog.Warning("Shift vibration observation failed: " + ex.Message); }
        }
    }
}
