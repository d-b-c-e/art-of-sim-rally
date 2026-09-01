using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Live camera adjustment by hotkey, persisted back to the config file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The right bonnet mount differs per car - a Group B monster and a 60s Mini do
    /// not want the same offsets - and the only way to judge it is to look through
    /// it while moving. Editing a config file and restarting for every 2 cm makes
    /// that unusable, so the offsets are nudgeable in place.
    /// </para>
    /// <para>
    /// Saving is debounced rather than immediate: writing the config on every frame
    /// a key is held would hammer the disk. Changes are written about a second after
    /// the last adjustment, so it persists without a save key to remember.
    /// </para>
    /// <para>
    /// Input is read through <c>UnityEngine.Input</c> rather than Rewired, so these
    /// keys sit outside the game's binding system and cannot collide with a bound
    /// action. Numpad by default for the same reason.
    /// </para>
    /// </remarks>
    internal static class CameraTuner
    {
        private static float _saveDueAt;
        private static bool  _dirty;

        /// <summary>
        /// Polls adjustment keys. Called from the bonnet camera's LateUpdate patch,
        /// so it only runs while that view is actually active.
        /// </summary>
        public static void Update()
        {
            var cfg = Plugin.Settings;
            if (cfg == null || !cfg.CameraTuningKeys.Value) return;

            // Per-second rates, scaled by real time so behaviour does not change
            // with frame rate or when the game is paused.
            float dt   = Time.unscaledDeltaTime;
            float move = cfg.TuneMoveSpeed.Value * dt;
            float ang  = cfg.TuneAngleSpeed.Value * dt;

            bool changed = false;

            changed |= Nudge(cfg.BonnetHeight,  cfg.KeyUp.Value,      cfg.KeyDown.Value,     move);
            changed |= Nudge(cfg.BonnetForward, cfg.KeyForward.Value, cfg.KeyBack.Value,     move);
            changed |= Nudge(cfg.BonnetSide,    cfg.KeyRight.Value,   cfg.KeyLeft.Value,     move);
            changed |= Nudge(cfg.BonnetPitch,   cfg.KeyPitchDown.Value, cfg.KeyPitchUp.Value, ang);
            changed |= Nudge(cfg.BonnetFOV,     cfg.KeyFovUp.Value,   cfg.KeyFovDown.Value,  ang);

            if (Input.GetKeyDown(cfg.KeyReset.Value))
            {
                cfg.BonnetHeight.Value  = (float)cfg.BonnetHeight.DefaultValue;
                cfg.BonnetForward.Value = (float)cfg.BonnetForward.DefaultValue;
                cfg.BonnetSide.Value    = (float)cfg.BonnetSide.DefaultValue;
                cfg.BonnetPitch.Value   = (float)cfg.BonnetPitch.DefaultValue;
                cfg.BonnetFOV.Value     = (float)cfg.BonnetFOV.DefaultValue;
                changed = true;
                Plugin.Log.LogInfo("Bonnet camera reset to defaults.");
            }

            if (changed)
            {
                _dirty = true;
                _saveDueAt = Time.unscaledTime + 1f;
                Plugin.Log.LogInfo(
                    $"Camera  height={cfg.BonnetHeight.Value:F2}  forward={cfg.BonnetForward.Value:F2}  " +
                    $"side={cfg.BonnetSide.Value:F2}  pitch={cfg.BonnetPitch.Value:F1}  " +
                    $"fov={cfg.BonnetFOV.Value:F0}");
            }

            if (_dirty && Time.unscaledTime >= _saveDueAt)
            {
                _dirty = false;
                try
                {
                    cfg.File.Save();
                    Plugin.Log.LogInfo("Camera settings saved.");
                }
                catch (System.Exception ex)
                {
                    // A failed save costs the tweak, not the session.
                    Plugin.Log.LogWarning($"Could not save camera settings: {ex.Message}");
                }
            }
        }

        private static bool Nudge(
            BepInEx.Configuration.ConfigEntry<float> entry,
            KeyCode increase, KeyCode decrease, float step)
        {
            float delta = 0f;
            if (Input.GetKey(increase)) delta += step;
            if (Input.GetKey(decrease)) delta -= step;
            if (delta == 0f) return false;

            entry.Value += delta;
            return true;
        }
    }
}
