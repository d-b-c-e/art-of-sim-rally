using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Live camera adjustment by hotkey, persisted back to the mod's settings.
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
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.CameraTuningKeys) return;

            // Per-second rates, scaled by real time so behaviour does not change
            // with frame rate or when the game is paused.
            float dt   = Time.unscaledDeltaTime;
            float move = cfg.TuneMoveSpeed * dt;
            float ang  = cfg.TuneAngleSpeed * dt;

            bool changed = false;

            changed |= Nudge(ref cfg.BonnetHeight,  cfg.KeyUp,        cfg.KeyDown,     move);
            changed |= Nudge(ref cfg.BonnetForward, cfg.KeyForward,   cfg.KeyBack,     move);
            changed |= Nudge(ref cfg.BonnetSide,    cfg.KeyRight,     cfg.KeyLeft,     move);
            changed |= Nudge(ref cfg.BonnetPitch,   cfg.KeyPitchDown, cfg.KeyPitchUp,  ang);
            changed |= Nudge(ref cfg.BonnetFOV,     cfg.KeyFovUp,     cfg.KeyFovDown,  ang);

            if (Input.GetKeyDown(cfg.KeyReset))
            {
                // A fresh instance carries the field initialisers, which are the
                // single source of truth for defaults now that there is no config
                // framework holding them separately.
                var defaults = new Settings();
                cfg.BonnetHeight  = defaults.BonnetHeight;
                cfg.BonnetForward = defaults.BonnetForward;
                cfg.BonnetSide    = defaults.BonnetSide;
                cfg.BonnetPitch   = defaults.BonnetPitch;
                cfg.BonnetFOV     = defaults.BonnetFOV;
                changed = true;
                ModLog.Info("Bonnet camera reset to defaults.");
            }

            if (changed)
            {
                _dirty = true;
                _saveDueAt = Time.unscaledTime + 1f;
                ModLog.Info(
                    $"Camera  height={cfg.BonnetHeight:F2}  forward={cfg.BonnetForward:F2}  " +
                    $"side={cfg.BonnetSide:F2}  pitch={cfg.BonnetPitch:F1}  " +
                    $"fov={cfg.BonnetFOV:F0}");
            }

            if (_dirty && Time.unscaledTime >= _saveDueAt)
            {
                _dirty = false;
                Main.SaveSettings();
                ModLog.Info("Camera settings saved.");
            }
        }

        private static bool Nudge(ref float value, KeyCode increase, KeyCode decrease, float step)
        {
            float delta = 0f;
            if (Input.GetKey(increase)) delta += step;
            if (Input.GetKey(decrease)) delta -= step;
            if (delta == 0f) return false;

            value += delta;
            return true;
        }
    }
}
