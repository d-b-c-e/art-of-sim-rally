using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Live camera adjustment by hotkey, persisted back to the mod's settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The right mount differs per car - a Group B monster and a 60s Mini do not
    /// want the same offsets - and the only way to judge it is to look through it
    /// while moving. Editing a config file and restarting for every 2 cm makes
    /// that unusable, so the offsets are nudgeable in place. The keys adjust
    /// whichever mounted view is on screen, bonnet or bumper, each with its own
    /// stored offsets.
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
        /// Polls adjustment keys for the given view. Called from the camera's
        /// LateUpdate patch, so it only runs while that view is actually active.
        /// </summary>
        public static void Update(BonnetCamera.View view)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.CameraTuningKeys) return;
            if (view == BonnetCamera.View.None) return;

            // Per-second rates, scaled by real time so behaviour does not change
            // with frame rate or when the game is paused.
            float dt   = Time.unscaledDeltaTime;
            float move = cfg.TuneMoveSpeed * dt;
            float ang  = cfg.TuneAngleSpeed * dt;

            bool changed = false;
            bool bumper = view == BonnetCamera.View.Bumper;

            if (bumper)
            {
                changed |= Nudge(ref cfg.BumperHeight,  cfg.KeyUp,        cfg.KeyDown,     move);
                changed |= Nudge(ref cfg.BumperForward, cfg.KeyForward,   cfg.KeyBack,     move);
                changed |= Nudge(ref cfg.BumperSide,    cfg.KeyRight,     cfg.KeyLeft,     move);
                changed |= Nudge(ref cfg.BumperPitch,   cfg.KeyPitchDown, cfg.KeyPitchUp,  ang);
                changed |= Nudge(ref cfg.BumperFOV,     cfg.KeyFovUp,     cfg.KeyFovDown,  ang);
            }
            else
            {
                changed |= Nudge(ref cfg.BonnetHeight,  cfg.KeyUp,        cfg.KeyDown,     move);
                changed |= Nudge(ref cfg.BonnetForward, cfg.KeyForward,   cfg.KeyBack,     move);
                changed |= Nudge(ref cfg.BonnetSide,    cfg.KeyRight,     cfg.KeyLeft,     move);
                changed |= Nudge(ref cfg.BonnetPitch,   cfg.KeyPitchDown, cfg.KeyPitchUp,  ang);
                changed |= Nudge(ref cfg.BonnetFOV,     cfg.KeyFovUp,     cfg.KeyFovDown,  ang);
            }

            if (Input.GetKeyDown(cfg.KeyReset))
            {
                // A fresh instance carries the field initialisers, which are the
                // single source of truth for defaults now that there is no config
                // framework holding them separately.
                var defaults = new Settings();
                if (bumper)
                {
                    cfg.BumperHeight  = defaults.BumperHeight;
                    cfg.BumperForward = defaults.BumperForward;
                    cfg.BumperSide    = defaults.BumperSide;
                    cfg.BumperPitch   = defaults.BumperPitch;
                    cfg.BumperFOV     = defaults.BumperFOV;
                }
                else
                {
                    cfg.BonnetHeight  = defaults.BonnetHeight;
                    cfg.BonnetForward = defaults.BonnetForward;
                    cfg.BonnetSide    = defaults.BonnetSide;
                    cfg.BonnetPitch   = defaults.BonnetPitch;
                    cfg.BonnetFOV     = defaults.BonnetFOV;
                }
                changed = true;
                ModLog.Info((bumper ? "Bumper" : "Bonnet") + " camera reset to defaults.");
            }

            if (changed)
            {
                _dirty = true;
                _saveDueAt = Time.unscaledTime + 1f;
                if (bumper)
                    ModLog.Info(
                        $"Bumper camera  height={cfg.BumperHeight:F2}  forward={cfg.BumperForward:F2}  " +
                        $"side={cfg.BumperSide:F2}  pitch={cfg.BumperPitch:F1}  fov={cfg.BumperFOV:F0}");
                else
                    ModLog.Info(
                        $"Bonnet camera  height={cfg.BonnetHeight:F2}  forward={cfg.BonnetForward:F2}  " +
                        $"side={cfg.BonnetSide:F2}  pitch={cfg.BonnetPitch:F1}  fov={cfg.BonnetFOV:F0}");
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
