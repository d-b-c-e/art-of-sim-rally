using System;
using System.IO;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Optional switch to Rewired's DirectInput backend, with a self-healing
    /// safety net.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rewired's Raw Input backend enumerates by HID usage and skips devices that
    /// do not present as a joystick. Sequential shifters commonly report as
    /// supplemental devices, so the game logs "found 1 joysticks attached" while
    /// joy.cpl and DirectInput both show three. DirectInput enumerates them.
    /// </para>
    /// <para>
    /// A first attempt at this shipped as a plain toggle and made the game
    /// unusable: the keyboard stopped responding entirely, so the title screen
    /// could not be passed and the settings panel was unreachable. Recovery meant
    /// hand-editing XML. The likely cause is that <c>nativeKeyboardSupport</c>
    /// stays on while the backend that serviced it is gone, so the keyboard is now
    /// explicitly handed to Unity as part of the same switch.
    /// </para>
    /// <para>
    /// That is still a theory, so the switch no longer trusts it. Applying writes
    /// a marker file, and the marker is only cleared once the mod has seen a real
    /// keypress. If the game starts and the marker from last time is still there,
    /// the previous attempt never registered any input - so the setting is turned
    /// off automatically before it can do the same again. Worst case is one bad
    /// launch that heals itself.
    /// </para>
    /// </remarks>
    internal static class InputBackend
    {
        private static string MarkerPath => Path.Combine(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "ArtOfSimRally"),
            "directinput-pending");

        private static bool _armed;
        private static bool _confirmed;

        /// <summary>
        /// Reverts the setting if the last attempt never saw input, then applies it
        /// if it is still enabled. Call before Rewired starts.
        /// </summary>
        public static void Apply()
        {
            var cfg = Main.Settings;
            if (cfg == null) return;

            // Self-heal first, so a bricked previous run cannot repeat.
            if (File.Exists(MarkerPath))
            {
                SafeDelete();
                if (cfg.UseDirectInput)
                {
                    cfg.UseDirectInput = false;
                    Main.SaveSettings();
                    ModLog.Error(
                        "DirectInput was enabled last launch but no input was ever detected, " +
                        "so it has been turned off automatically. Your controls should work again.");
                    return;
                }
            }

            if (!cfg.UseDirectInput) return;

            try
            {
                if (ReInput.isReady)
                    ModLog.Warning("Rewired already started - DirectInput applies from next launch.");

                // Hand keyboard and mouse to Unity. Leaving native support on while
                // switching the backend out from under it is what killed the
                // keyboard the first time this was tried.
                ReInput.configuration.nativeKeyboardSupport = false;
                ReInput.configuration.nativeMouseSupport = false;

                ReInput.configuration.windowsStandalonePrimaryInputSource =
                    Rewired.Platforms.WindowsStandalonePrimaryInputSource.DirectInput;

                Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath));
                File.WriteAllText(MarkerPath, DateTime.Now.ToString("s"));
                _armed = true;

                ModLog.Info("DirectInput backend applied (keyboard handed to Unity). " +
                            "Rebinding will be required.");
            }
            catch (Exception ex)
            {
                ModLog.Error("Could not switch to DirectInput: " + ex.Message);
                SafeDelete();
            }
        }

        /// <summary>
        /// Clears the safety marker once any input is seen. Called every frame by
        /// the watchdog; cheap and stops after the first success.
        /// </summary>
        /// <remarks>
        /// Uses <c>UnityEngine.Input</c> rather than Rewired on purpose - the point
        /// is to verify input works at all, and asking the system under test is no
        /// evidence.
        /// </remarks>
        public static void NoteInputSeen()
        {
            if (!_armed || _confirmed) return;
            if (!Input.anyKey && Input.GetAxisRaw("Mouse X") == 0f) return;

            _confirmed = true;
            SafeDelete();
            ModLog.Info("Input confirmed working under DirectInput.");
        }

        private static void SafeDelete()
        {
            try { if (File.Exists(MarkerPath)) File.Delete(MarkerPath); }
            catch { /* a stale marker only costs one extra revert */ }
        }
    }
}
