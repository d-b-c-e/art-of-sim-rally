using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// BepInEx entry point for art-of-sim-rally.
    /// </summary>
    /// <remarks>
    /// The loader-specific surface is deliberately confined to this one file.
    /// Everything else talks to <see cref="ModConfig"/> and <see cref="Log"/>,
    /// so adding a Unity Mod Manager entry point later - which is what the art of
    /// rally community actually uses for distribution - means writing a second
    /// small entry class, not touching the patches.
    /// </remarks>
    [BepInPlugin(PluginGuid, "art of sim rally", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dbce.artofsimrally";

        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Settings { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(base.Config);

            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (Settings.ForceFeedbackEnabled.Value)
                FfbNative.Initialise(pluginDir);

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("Patches applied.");
            }
            catch (Exception ex)
            {
                // A failed patch must not take the game down. Report loudly and
                // let the player keep driving without the mod's features.
                Log.LogError($"Harmony patching failed: {ex}");
            }

            Log.LogInfo(
                $"art of sim rally loaded - directSteering={Settings.DirectSteering.Value}, " +
                $"ffb={Settings.ForceFeedbackEnabled.Value}, telemetry={Settings.TelemetryEnabled.Value}");
        }

        private void OnDestroy()
        {
            TelemetryPump.Shutdown();
            FfbNative.Shutdown();
            _harmony?.UnpatchSelf();
        }

        private void OnApplicationQuit()
        {
            // Release the wheel explicitly. Leaving a constant force applied on
            // an exclusively-acquired device can leave it pulling after exit.
            FfbNative.SetForce(0);
            FfbNative.Shutdown();
        }
    }

    /// <summary>Typed wrapper over the BepInEx config file.</summary>
    internal sealed class ModConfig
    {
        /// <summary>Backing file, so live camera adjustments can be saved.</summary>
        public readonly ConfigFile File;

        public readonly ConfigEntry<bool> DirectSteering;
        public readonly ConfigEntry<bool> DisableSteerAssist;
        public readonly ConfigEntry<bool> ZeroAxisDeadzone;

        public readonly ConfigEntry<bool>  ForceFeedbackEnabled;
        public readonly ConfigEntry<float> Gain;
        public readonly ConfigEntry<float> MzReference;
        public readonly ConfigEntry<float> Smoothing;
        public readonly ConfigEntry<bool>  Invert;
        public readonly ConfigEntry<bool>  DiagnosticLogging;

        public readonly ConfigEntry<bool>  BonnetCameraEnabled;
        public readonly ConfigEntry<float> BonnetHeight;
        public readonly ConfigEntry<float> BonnetForward;
        public readonly ConfigEntry<float> BonnetSide;
        public readonly ConfigEntry<float> BonnetPitch;
        public readonly ConfigEntry<float> BonnetFOV;
        public readonly ConfigEntry<float> BonnetLean;

        public readonly ConfigEntry<bool>  CameraTuningKeys;
        public readonly ConfigEntry<float> TuneMoveSpeed;
        public readonly ConfigEntry<float> TuneAngleSpeed;
        public readonly ConfigEntry<KeyCode> KeyUp, KeyDown, KeyForward, KeyBack;
        public readonly ConfigEntry<KeyCode> KeyLeft, KeyRight, KeyPitchUp, KeyPitchDown;
        public readonly ConfigEntry<KeyCode> KeyFovUp, KeyFovDown, KeyReset;

        public readonly ConfigEntry<bool>   TelemetryEnabled;
        public readonly ConfigEntry<string> TelemetryHost;
        public readonly ConfigEntry<int>    TelemetryPort;

        public ModConfig(ConfigFile file)
        {
            File = file;

            DirectSteering = file.Bind("Steering", "DirectSteering", true,
                "Removes the gamepad steering smoothing the game applies to wheels it does not " +
                "recognise. This is exactly the code path a recognised wheel (e.g. a Logitech G29) " +
                "already gets, so it is not an advantage - it is a device fix.");

            DisableSteerAssist = file.Bind("Steering", "DisableSteerAssist", false,
                "Disables SteerAssistance, which reduces your steering authority as the car slides. " +
                "Unlike DirectSteering this IS a driving aid change and alters how the car behaves. " +
                "art of rally has online leaderboards; enable deliberately.");

            ZeroAxisDeadzone = file.Bind("Steering", "ZeroAxisDeadzone", true,
                "Clears Rewired's per-axis calibration deadzone and forces linear sensitivity. " +
                "This is a SECOND deadzone, separate from the one in the game's options screen, " +
                "applied inside GetAxisRaw before the game sees the value. Unrecognised wheels " +
                "get default values for it and nothing in the UI exposes them.");

            ForceFeedbackEnabled = file.Bind("ForceFeedback", "Enabled", true,
                "Enables force feedback. Requires UnityForceFeedback.dll in " +
                "artofrally_Data/Plugins/x86_64 (the game ships without it).");

            Gain = file.Bind("ForceFeedback", "Gain", 1.0f,
                "Overall force multiplier applied after normalisation.");

            MzReference = file.Bind("ForceFeedback", "MzReference", 150f,
                "Self-aligning torque treated as full force. Lower = stronger. Set " +
                "DiagnosticLogging true and drive; the log reports the peak |Mz| observed, " +
                "which is the number to put here.");

            Smoothing = file.Bind("ForceFeedback", "Smoothing", 0.2f,
                "0 = raw and detailed but noisy over rough surfaces, 0.9 = heavily damped.");

            Invert = file.Bind("ForceFeedback", "Invert", false,
                "Flip force direction if the wheel pulls the wrong way.");

            DiagnosticLogging = file.Bind("ForceFeedback", "DiagnosticLogging", false,
                "Log peak aligning torque every 5 seconds, for tuning MzReference.");

            BonnetCameraEnabled = file.Bind("Camera", "BonnetCamera", true,
                "Adds a bonnet-mounted view to the game's normal view rotation. Press the " +
                "change-view button to cycle to it; the choice persists like any other view. " +
                "This is a bonnet camera, not a cockpit one - the cars have no interiors.");

            BonnetHeight = file.Bind("Camera", "Height", 0.95f,
                "Metres above the car's origin. Raise until the bonnet sits low in frame.");

            BonnetForward = file.Bind("Camera", "Forward", 1.0f,
                "Metres forward of the car's origin. Increase if the car body blocks the view.");

            BonnetSide = file.Bind("Camera", "Side", 0f,
                "Metres right of centre. Negative for left-hand drive framing.");

            BonnetPitch = file.Bind("Camera", "Pitch", 3f,
                "Degrees of downward tilt. Positive looks down toward the road.");

            BonnetFOV = file.Bind("Camera", "FieldOfView", 75f,
                "Field of view in degrees. Wider exaggerates speed; narrower reads more natural.");

            BonnetLean = file.Bind("Camera", "Lean", 0.1f,
                "Lateral camera shift under cornering load, in metres at full slip. Sells the " +
                "mounted feel, but is also the first thing to cause motion sickness. 0 disables.");

            CameraTuningKeys = file.Bind("CameraTuning", "Enabled", true,
                "Adjust the bonnet camera live with hotkeys while that view is active. Changes " +
                "save automatically about a second after you stop pressing. Read through " +
                "UnityEngine.Input rather than Rewired, so these cannot clash with a bound action.");

            TuneMoveSpeed = file.Bind("CameraTuning", "MoveSpeed", 0.4f,
                "Metres per second while a position key is held.");

            TuneAngleSpeed = file.Bind("CameraTuning", "AngleSpeed", 20f,
                "Degrees per second while a pitch or FOV key is held.");

            KeyUp        = file.Bind("CameraTuning", "KeyUp",        KeyCode.Keypad8, "Raise the camera.");
            KeyDown      = file.Bind("CameraTuning", "KeyDown",      KeyCode.Keypad2, "Lower the camera.");
            KeyForward   = file.Bind("CameraTuning", "KeyForward",   KeyCode.Keypad9, "Move forward.");
            KeyBack      = file.Bind("CameraTuning", "KeyBack",      KeyCode.Keypad7, "Move back.");
            KeyLeft      = file.Bind("CameraTuning", "KeyLeft",      KeyCode.Keypad4, "Move left.");
            KeyRight     = file.Bind("CameraTuning", "KeyRight",     KeyCode.Keypad6, "Move right.");
            KeyPitchDown = file.Bind("CameraTuning", "KeyPitchDown", KeyCode.Keypad1, "Tilt down.");
            KeyPitchUp   = file.Bind("CameraTuning", "KeyPitchUp",   KeyCode.Keypad3, "Tilt up.");
            KeyFovUp     = file.Bind("CameraTuning", "KeyFovUp",     KeyCode.KeypadPlus,  "Widen field of view.");
            KeyFovDown   = file.Bind("CameraTuning", "KeyFovDown",   KeyCode.KeypadMinus, "Narrow field of view.");
            KeyReset     = file.Bind("CameraTuning", "KeyReset",     KeyCode.Keypad0, "Reset camera to defaults.");

            TelemetryEnabled = file.Bind("Telemetry", "Enabled", false,
                "Emit Forza Horizon-compatible UDP telemetry, readable by SimHub, dashboards, " +
                "motion rigs and bass shakers.");

            TelemetryHost = file.Bind("Telemetry", "Host", "127.0.0.1",
                "Destination address.");

            TelemetryPort = file.Bind("Telemetry", "Port", 8000,
                "Destination UDP port. 8000 is SimHub's default for Forza.");
        }
    }
}
