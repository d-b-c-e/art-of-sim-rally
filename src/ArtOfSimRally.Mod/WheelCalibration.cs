using System.Text;
using Rewired;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Clears Rewired's per-axis calibration deadzone, which the game's own
    /// deadzone setting does not touch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are two independent deadzones in play, and only one of them is
    /// visible in the game's options screen:
    /// </para>
    /// <list type="number">
    /// <item>
    /// The game's, applied in <c>AxisCarController.GetInput</c> as
    /// <c>ProcessDeadzoneForInput(GetAxisRaw(steerAxis), SettingsManager.GetSteeringDeadzone())</c>.
    /// That function is a plain cutoff, so at 0 it does nothing. This is the one
    /// the options screen sets.
    /// </item>
    /// <item>
    /// Rewired's own <see cref="AxisCalibration"/> deadzone, applied *inside*
    /// <c>GetAxisRaw</c> before the game ever sees a number. "Raw" here means
    /// unsmoothed, not uncalibrated. Nothing in the game's UI exposes it.
    /// </item>
    /// </list>
    /// <para>
    /// For a controller Rewired does not recognise - and a MOZA R12 reports
    /// <c>Is Recognized: No</c> - there is no hardware profile to supply sane
    /// values, so the defaults apply. On a wheel set to 270 degrees, even a small
    /// fractional deadzone is a wide dead band around centre, which is exactly
    /// the "there's still a deadzone even though I set none" symptom.
    /// </para>
    /// <para>
    /// This logs what it finds before changing anything, so the hypothesis is
    /// checkable rather than assumed.
    /// </para>
    /// </remarks>
    internal static class WheelCalibration
    {
        private static bool _applied;

        /// <summary>
        /// Reports and optionally clears axis deadzones. Runs once; safe to call repeatedly.
        /// </summary>
        public static void Apply()
        {
            if (_applied) return;
            _applied = true;

            var cfg = Plugin.Settings;
            if (cfg == null) return;

            try
            {
                var joysticks = ReInput.controllers.Joysticks;
                if (joysticks == null || joysticks.Count == 0)
                {
                    Plugin.Log.LogInfo("No joysticks present; skipping calibration.");
                    return;
                }

                foreach (var joystick in joysticks)
                {
                    var map = joystick.calibrationMap;
                    if (map == null) continue;

                    var before = new StringBuilder();
                    var after  = new StringBuilder();

                    for (int i = 0; i < map.axisCount; i++)
                    {
                        var axis = map.GetAxis(i);
                        if (axis == null) continue;

                        before.Append($"[{i}] dz={axis.deadZone:F3} sens={axis.sensitivity:F2}  ");

                        if (cfg.ZeroAxisDeadzone.Value)
                        {
                            axis.deadZone = 0f;
                            // Linear response. A non-1 sensitivity bends the input
                            // curve, which on a wheel reads as vague near centre
                            // and abrupt near lock - the same complaint from a
                            // different cause.
                            axis.sensitivity = 1f;
                        }

                        after.Append($"[{i}] dz={axis.deadZone:F3} sens={axis.sensitivity:F2}  ");
                    }

                    Plugin.Log.LogInfo(
                        $"Rewired calibration for '{joystick.name}' " +
                        $"(recognised={joystick.hardwareTypeGuid != System.Guid.Empty})");
                    Plugin.Log.LogInfo($"  before: {before}");
                    if (cfg.ZeroAxisDeadzone.Value)
                        Plugin.Log.LogInfo($"  after : {after}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Could not adjust Rewired calibration: {ex.Message}");
            }
        }
    }
}
