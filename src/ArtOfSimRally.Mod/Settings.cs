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
    public class Settings : UnityModManager.ModSettings
    {
        // ---- Steering -------------------------------------------------------

        public bool DirectSteering = true;

        public bool ZeroAxisDeadzone = true;

        public bool GlyphTextFallback = true;

        public bool BindAnyDevice = true;

        public bool DisableSteerAssist = false;

        // ---- Force feedback -------------------------------------------------

        public bool ForceFeedbackEnabled = true;

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

        /// <summary>
        /// Aligning torque treated as full force, before Strength is applied.
        /// </summary>
        /// <remarks>
        /// Not shown in the panel. It sets where the output starts clipping, which
        /// is a different thing from how strong the wheel feels, and having two
        /// dials for one sensation - one of them inverted, where lower means
        /// stronger - confused everyone who met it. Strength is the only dial now;
        /// this stays as the reference it scales against, tuned from measured peak
        /// torque across a real stage.
        /// </remarks>
        public float MzReference = 150f;

        public float Smoothing = 0.2f;

        public bool Invert = false;


        public bool DiagnosticLogging = false;

        // Set by the device picker in the settings panel, not drawn directly.
        // The name is what persists; the index is only a tiebreaker for rigs
        // where two devices report the same name (Fanatec does this).
        public string PreferredDevice = "";

        public int PreferredDeviceIndex = -1;

        // ---- Camera ---------------------------------------------------------

        public bool BonnetCameraEnabled = true;

        public float BonnetHeight = 0.95f;

        public float BonnetForward = 1.0f;

        public float BonnetSide = 0f;

        public float BonnetPitch = 3f;

        public float BonnetFOV = 75f;

        public float BonnetLean = 0.1f;

        /// <summary>Adds a bumper view after the bonnet view in the rotation.</summary>
        public bool BumperCameraEnabled = true;

        // Lower and further forward than the bonnet: just above the front
        // bumper, looking down the road. Shares BonnetLean.
        public float BumperHeight = 0.45f;
        public float BumperForward = 1.9f;
        public float BumperSide = 0f;
        public float BumperPitch = 2f;
        public float BumperFOV = 80f;

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

        public bool ShifterEnabled = false;

        public bool ShifterIsHPattern = false;

        // Chosen in the panel's device list rather than typed.
        public int ShifterDeviceIndex = -1;
        public string ShifterDeviceName = "";

        // Button index per gate; -1 means unbound. Stored flat rather than as an
        // array because UnityModManager's XML settings round-trip simple fields
        // far more reliably than collections.
        // Sequential shifters have two controls, not seven gates. Kept separate
        // from the gear buttons so switching mode does not discard either set.
        /// <summary>Step past neutral when shifting sequentially.</summary>
        /// <remarks>
        /// The game steps one index at a time through [reverse, neutral, 1st, ...],
        /// exactly as its own ShiftUp/ShiftDown do, so reverse to first takes two
        /// presses with a useless stop in between. Real sequential boxes do have
        /// neutral there, but nobody wants to press through it, and the game
        /// auto-clutches anyway.
        /// </remarks>
        public bool SkipNeutral = true;

        public int ShiftUpButton = -1;
        public int ShiftDownButton = -1;

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

        public bool TelemetryEnabled = false;

        public string TelemetryHost = "127.0.0.1";

        public int TelemetryPort = 8000;

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}
