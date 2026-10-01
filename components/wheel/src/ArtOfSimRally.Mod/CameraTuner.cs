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
    /// a key is held would hammer the disk. The persistent watchdog saves once idle,
    /// at least a second after the last adjustment, even after leaving this view.
    /// </para>
    /// <para>
    /// Input is read through <c>UnityEngine.Input</c> rather than Rewired, so these
    /// keys sit outside the game's binding system. They can still trigger game
    /// actions on the same key; the numpad defaults reduce that overlap.
    /// </para>
    /// </remarks>
    internal static class CameraTuner
    {
        private static float _saveDueAt;
        private static readonly DeferredSave Save = new DeferredSave();
        private static bool _waitForRelease;
        private static bool _resetButtonPressed;
        internal static void ReadResetButton() => _resetButtonPressed = WheelInput.ShortcutPressed(WheelInput.Channel.CameraReset);

        public static void SuppressUntilRelease() => _waitForRelease = true;

        internal static void MarkDirty()
        {
            Save.MarkDirty();
            _saveDueAt = Time.unscaledTime + 1f;
        }

        // Called independently of the mounted camera's LateUpdate. Keep retries
        // pending after failure and perform no persistence in the driving path.
        public static void Flush(bool shutdown = false)
        {
            if (!shutdown && Time.unscaledTime < _saveDueAt) return;
            if (Save.Flush(Time.unscaledTime, !shutdown && Main.Enabled && GameState.IsDriving,
                    shutdown, Main.SaveSettings))
                ModLog.Info("Camera settings saved.");
        }

        /// <summary>
        /// Polls adjustment keys for the given view. Called from the camera's
        /// LateUpdate patch, so it only runs while that view is actually active.
        /// </summary>
        public static void Update(BonnetCamera.View view)
        {
            bool resetButton = _resetButtonPressed; _resetButtonPressed = false;
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.CameraTuningKeys) return;
            if (view == BonnetCamera.View.None) return;
            if (Main.SettingsVisible || !Application.isFocused || CameraKeys.Listening >= 0 || CameraKeys.ModifierHeld())
            {
                SuppressUntilRelease();
                return;
            }
            // A captured key or the key that closed the panel must be released
            // before it can adjust/reset a mount on the next LateUpdate.
            if (_waitForRelease)
            {
                if (Input.anyKey || CameraKeys.AnyButtonHeld) return;
                _waitForRelease = false;
            }

            // Per-second rates, scaled by real time so behaviour does not change
            // with frame rate or when the game is paused.
            float dt   = Time.unscaledDeltaTime;
            float move = cfg.TuneMoveSpeed * dt;
            float ang  = cfg.TuneAngleSpeed * dt;

            bool changed = false;
            bool bumper = view == BonnetCamera.View.Bumper;

            if (bumper)
            {
                changed |= Nudge(ref cfg.BumperHeight, 0, 1, move);
                changed |= Nudge(ref cfg.BumperForward, 2, 3, move);
                changed |= Nudge(ref cfg.BumperSide, 5, 4, move);
                changed |= Nudge(ref cfg.BumperPitch, 6, 7, ang);
                changed |= Nudge(ref cfg.BumperFOV, 8, 9, ang);
            }
            else
            {
                changed |= Nudge(ref cfg.BonnetHeight, 0, 1, move);
                changed |= Nudge(ref cfg.BonnetForward, 2, 3, move);
                changed |= Nudge(ref cfg.BonnetSide, 5, 4, move);
                changed |= Nudge(ref cfg.BonnetPitch, 6, 7, ang);
                changed |= Nudge(ref cfg.BonnetFOV, 8, 9, ang);
            }

            if (Input.GetKeyDown(cfg.KeyReset) || resetButton)
            {
                // A fresh instance carries the field initialisers, which are the
                // single source of truth for defaults now that there is no config
                // framework holding them separately.
                cfg.ResetCameraMount(bumper);
                changed = true;
            }

            if (changed) MarkDirty();
        }

        private static bool Nudge(ref float value, int increase, int decrease, float step)
        {
            float delta = 0f;
            if (CameraKeys.Held(increase)) delta += step;
            if (CameraKeys.Held(decrease)) delta -= step;
            if (delta == 0f) return false;

            value += delta;
            return true;
        }
    }
}
