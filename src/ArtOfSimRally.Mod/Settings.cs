using UnityEngine;
using UnityModManagerNet;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Mod settings, shown in Unity Mod Manager's in-game panel (Ctrl+F10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Plain fields rather than a loader's config-entry wrapper, so the patches
    /// read <c>Main.Settings.Gain</c> with no indirection and no dependency on how
    /// the values were loaded.
    /// </para>
    /// <para>
    /// The in-game panel matters more here than it usually would. Force feedback
    /// strength and the camera mount both have to be judged while driving, and the
    /// alternative is quitting to edit a text file for every adjustment.
    /// </para>
    /// </remarks>
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        // ---- Steering -------------------------------------------------------

        [Draw("Direct steering", Tooltip =
            "Removes gamepad steering smoothing applied to unrecognised wheels.")]
        public bool DirectSteering = true;

        [Draw("Clear hidden axis deadzone", Tooltip =
            "Clears Rewired's hidden 10% deadzone. Not the one in game options.", VisibleOn = "ShowAdvanced|true")]
        public bool ZeroAxisDeadzone = true;

        [Draw("Show button names when no icon exists", VisibleOn = "ShowAdvanced|true", Tooltip =
            "Replaces blank glyph boxes with the button name, e.g. B12.")]
        public bool GlyphTextFallback = true;

        [Draw("Bind any device", Tooltip =
            "Rebind the device you touch, not just the first joystick.", VisibleOn = "ShowAdvanced|true")]
        public bool BindAnyDevice = true;

        [Draw("Disable steering assist", Tooltip =
            "CHANGES THE CAR. Off by default. Affects leaderboards.", VisibleOn = "ShowAdvanced|true")]
        public bool DisableSteerAssist = false;

        // ---- Force feedback -------------------------------------------------

        [Draw("Force feedback", Tooltip =
            "Needs UnityForceFeedback.dll in artofrally_Data/Plugins/x86_64.")]
        public bool ForceFeedbackEnabled = true;

        [Draw("Force feedback strength", Min = 0, Max = 100, Tooltip =
            "Overall strength, like any other game. 50 is the baseline.")]
        public int Strength = 50;

        /// <summary>
        /// Strength as the multiplier the force model actually uses.
        /// </summary>
        /// <remarks>
        /// 50 maps to 1.0 so the slider's midpoint is the tuning this shipped
        /// with, and the ends are meaningfully different rather than a 0-5 range
        /// where most of the travel is unusable. Nobody thinks in gain
        /// multipliers; everybody understands a percentage.
        /// </remarks>
        public float GainFromStrength => Strength / 50f;

        [Draw("Reference torque (lower = stronger)", Min = 10f, Max = 1000f, Precision = 0, Tooltip =
            "Lower is stronger. See help at the bottom of this panel.", VisibleOn = "ShowAdvanced|true")]
        public float MzReference = 150f;

        [Draw("Smoothing", Min = 0f, Max = 0.95f, Precision = 2, Tooltip =
            "0 is raw and detailed, higher is damped.", VisibleOn = "ShowAdvanced|true")]
        public float Smoothing = 0.2f;

        [Draw("Invert force", Tooltip =
            "Flip if the wheel pulls the wrong way.", VisibleOn = "ShowAdvanced|true")]
        public bool Invert = false;

        [Draw("Show advanced options")]
        public bool ShowAdvanced = false;

        [Draw("Log peak torque (for tuning)", VisibleOn = "ShowAdvanced|true")]
        public bool DiagnosticLogging = false;

        // Set by the device picker in the settings panel, not drawn directly.
        // The name is what persists; the index is only a tiebreaker for rigs
        // where two devices report the same name (Fanatec does this).
        public string PreferredDevice = "";

        public int PreferredDeviceIndex = -1;

        // ---- Camera ---------------------------------------------------------

        [Draw("Bonnet camera", Tooltip =
            "Adds a bonnet view to the normal view rotation.")]
        public bool BonnetCameraEnabled = true;

        [Draw("Height (m)", Min = -1f, Max = 3f, Precision = 2, VisibleOn = "ShowAdvanced|true")]
        public float BonnetHeight = 0.95f;

        [Draw("Forward (m)", Min = -3f, Max = 4f, Precision = 2, VisibleOn = "ShowAdvanced|true")]
        public float BonnetForward = 1.0f;

        [Draw("Side (m)", Min = -1.5f, Max = 1.5f, Precision = 2, VisibleOn = "ShowAdvanced|true")]
        public float BonnetSide = 0f;

        [Draw("Pitch (deg)", Min = -30f, Max = 30f, Precision = 1, VisibleOn = "ShowAdvanced|true")]
        public float BonnetPitch = 3f;

        [Draw("Field of view", Min = 40f, Max = 120f, Precision = 0, VisibleOn = "ShowAdvanced|true")]
        public float BonnetFOV = 75f;

        [Draw("Cornering lean", Min = 0f, Max = 1f, Precision = 2, Tooltip =
            "Lateral shift under cornering load. 0 disables.", VisibleOn = "ShowAdvanced|true")]
        public float BonnetLean = 0.1f;

        [Draw("Numpad camera hotkeys", Tooltip =
            "Numpad adjusts the camera while looking through it.")]
        public bool CameraTuningKeys = true;

        // Rates for the hotkeys. Not drawn: tuning the tuner is a rabbit hole, and
        // these only matter if the defaults feel wrong.
        public float TuneMoveSpeed  = 0.4f;
        public float TuneAngleSpeed = 20f;

        public KeyCode KeyUp        = KeyCode.Keypad8;
        public KeyCode KeyDown      = KeyCode.Keypad2;
        public KeyCode KeyForward   = KeyCode.Keypad9;
        public KeyCode KeyBack      = KeyCode.Keypad7;
        public KeyCode KeyLeft      = KeyCode.Keypad4;
        public KeyCode KeyRight     = KeyCode.Keypad6;
        public KeyCode KeyPitchDown = KeyCode.Keypad1;
        public KeyCode KeyPitchUp   = KeyCode.Keypad3;
        public KeyCode KeyFovUp     = KeyCode.KeypadPlus;
        public KeyCode KeyFovDown   = KeyCode.KeypadMinus;
        public KeyCode KeyReset     = KeyCode.Keypad0;

        // ---- Shifter --------------------------------------------------------

        [Draw("Separate shifter", Tooltip =
            "Read a shifter directly, bypassing the game's input system.")]
        public bool ShifterEnabled = false;

        [Draw("H-pattern (off = sequential)", VisibleOn = "ShifterEnabled|true", Tooltip =
            "H-pattern returns to neutral between gates. Sequential does not.")]
        public bool ShifterIsHPattern = false;

        // Chosen in the panel's device list rather than typed.
        public int ShifterDeviceIndex = -1;
        public string ShifterDeviceName = "";

        // Button index per gate; -1 means unbound. Stored flat rather than as an
        // array because UnityModManager's XML settings round-trip simple fields
        // far more reliably than collections.
        public int GearReverseButton = -1;
        public int Gear1Button = -1;
        public int Gear2Button = -1;
        public int Gear3Button = -1;
        public int Gear4Button = -1;
        public int Gear5Button = -1;
        public int Gear6Button = -1;

        /// <summary>Button bound to a gear, 1-6. Returns -1 when unbound.</summary>
        public int GearButton(int gear)
        {
            switch (gear)
            {
                case 1: return Gear1Button;
                case 2: return Gear2Button;
                case 3: return Gear3Button;
                case 4: return Gear4Button;
                case 5: return Gear5Button;
                case 6: return Gear6Button;
                default: return -1;
            }
        }

        /// <summary>Binds a button to a gear, 1-6, or -1 for reverse.</summary>
        public void SetGearButton(int gear, int button)
        {
            switch (gear)
            {
                case -1: GearReverseButton = button; break;
                case 1:  Gear1Button = button; break;
                case 2:  Gear2Button = button; break;
                case 3:  Gear3Button = button; break;
                case 4:  Gear4Button = button; break;
                case 5:  Gear5Button = button; break;
                case 6:  Gear6Button = button; break;
            }
        }

        // ---- Telemetry ------------------------------------------------------

        [Draw("Telemetry (Forza-compatible UDP)", Tooltip =
            "Forza-compatible UDP. Use a Forza Horizon 5 profile.")]
        public bool TelemetryEnabled = false;

        [Draw("Host", VisibleOn = "ShowAdvanced|true")]
        public string TelemetryHost = "127.0.0.1";

        [Draw("Port", Min = 1, Max = 65535, VisibleOn = "ShowAdvanced|true")]
        public int TelemetryPort = 8000;

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);

        /// <summary>Called by UMM whenever a drawn value changes.</summary>
        public void OnChange()
        {
            // Force feedback holds an exclusive device handle, so it cannot simply
            // be flipped by writing a bool - release the wheel when switched off.
            if (!ForceFeedbackEnabled) FfbNative.SetForce(0);
        }
    }
}
