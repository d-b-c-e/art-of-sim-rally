using HarmonyLib;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Applies the separate shifter's selected gear to the car each physics step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs on <c>CarDynamics.FixedUpdate</c> alongside force feedback and
    /// telemetry, so gear selection is sampled at the physics rate rather than the
    /// frame rate.
    /// </para>
    /// <para>
    /// Only while the player is driving: the end-of-stage cutscene drives the car
    /// itself, and forcing gears into an AI-driven car would fight it - the same
    /// mistake that had the wheel pulling to full lock after the finish line.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CarDynamics), "FixedUpdate")]
    internal static class ShifterPatch
    {
        private static CarDynamics _cachedFor;
        private static Drivetrain _drivetrain;

        [HarmonyPostfix]
        private static void ApplyGear(CarDynamics __instance)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.ShifterEnabled) return;
            if (!Shifter.IsOpen) return;

            if (!GameState.IsDriving) { Shifter.Reset(); return; }

            if (!ReferenceEquals(_cachedFor, __instance))
            {
                _cachedFor = __instance;
                _drivetrain = __instance.GetComponent<Drivetrain>();
                Shifter.Reset();
            }

            Shifter.Update(_drivetrain);
        }
    }
}
