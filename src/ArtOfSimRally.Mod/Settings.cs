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
            "Removes the gamepad steering smoothing the game applies to wheels it does not " +
            "recognise. This is the same code path a recognised wheel already gets.")]
        public bool DirectSteering = true;

        [Draw("Clear hidden axis deadzone", Tooltip =
            "Clears Rewired's own 10% per-axis deadzone, which is separate from the one in " +
            "the game's options and is not exposed anywhere in the UI.")]
        public bool ZeroAxisDeadzone = true;

        [Draw("Disable steering assist", Tooltip =
            "CHANGES HOW THE CAR BEHAVES. The game reduces your steering authority as the car " +
            "slides. Unlike the options above this is a driving aid, not a device fix, and the " +
            "game has online leaderboards.")]
        public bool DisableSteerAssist = false;

        // ---- Force feedback -------------------------------------------------

        [Draw("Force feedback", Tooltip =
            "Requires UnityForceFeedback.dll in artofrally_Data/Plugins/x86_64.")]
        public bool ForceFeedbackEnabled = true;

        [Draw("Strength", Min = 0f, Max = 5f, Precision = 2)]
        public float Gain = 1.0f;

        [Draw("Reference torque (lower = stronger)", Min = 10f, Max = 1000f, Precision = 0, Tooltip =
            "Self-aligning torque treated as full force. Enable diagnostics and drive; the log " +
            "reports the peak seen, which is the number to put here.")]
        public float MzReference = 150f;

        [Draw("Smoothing", Min = 0f, Max = 0.95f, Precision = 2, Tooltip =
            "0 is raw and detailed but noisy on rough surfaces.")]
        public float Smoothing = 0.2f;

        [Draw("Invert force", Tooltip = "Flip if the wheel pulls the wrong way.")]
        public bool Invert = false;

        [Draw("Log peak torque (for tuning)")]
        public bool DiagnosticLogging = false;

        [Draw("Preferred wheel (optional)", Tooltip =
            "Part of your wheel's name, e.g. \"MOZA\" or \"G29\". Only needed if you have more " +
            "than one force-feedback device; otherwise the first one found is used. The log " +
            "lists every device it saw. Takes effect on restart.")]
        public string PreferredDevice = "";

        [Draw("Wheel index (-1 = auto)", Min = -1, Max = 7, Tooltip =
            "Use when several devices share a name - a Fanatec rig reports two, both called " +
            "\"FANATEC Wheel\". The log lists each with an index; put that number here. " +
            "Takes effect on restart.")]
        public int PreferredDeviceIndex = -1;

        // ---- Camera ---------------------------------------------------------

        [Draw("Bonnet camera", Tooltip =
            "Adds a bonnet view to the game's normal view rotation. Press your change-view " +
            "button to cycle to it. Bonnet, not cockpit - the cars have no interiors.")]
        public bool BonnetCameraEnabled = true;

        [Draw("Height (m)", Min = -1f, Max = 3f, Precision = 2)]
        public float BonnetHeight = 0.95f;

        [Draw("Forward (m)", Min = -3f, Max = 4f, Precision = 2)]
        public float BonnetForward = 1.0f;

        [Draw("Side (m)", Min = -1.5f, Max = 1.5f, Precision = 2)]
        public float BonnetSide = 0f;

        [Draw("Pitch (deg)", Min = -30f, Max = 30f, Precision = 1)]
        public float BonnetPitch = 3f;

        [Draw("Field of view", Min = 40f, Max = 120f, Precision = 0)]
        public float BonnetFOV = 75f;

        [Draw("Cornering lean", Min = 0f, Max = 1f, Precision = 2, Tooltip =
            "Lateral camera shift under load. Sells the mounted feel, but is also the first " +
            "thing to cause motion sickness. 0 disables.")]
        public float BonnetLean = 0.1f;

        [Draw("Numpad camera hotkeys", Tooltip =
            "Adjust the bonnet camera live while looking through it. 8/2 up-down, 7/9 " +
            "back-forward, 4/6 left-right, 1/3 tilt, +/- field of view, 0 resets.")]
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

        // ---- Telemetry ------------------------------------------------------

        [Draw("Telemetry (Forza-compatible UDP)", Tooltip =
            "Readable by SimHub, dashboards, motion rigs and bass shakers. Use a Forza " +
            "Horizon 5 profile.")]
        public bool TelemetryEnabled = false;

        [Draw("Host")]
        public string TelemetryHost = "127.0.0.1";

        [Draw("Port", Min = 1, Max = 65535)]
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
