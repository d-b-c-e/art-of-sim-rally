using System;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// The whole settings panel, drawn by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity Mod Manager's <c>[Draw]</c> attributes render every field as one flat
    /// list in declaration order, with no headings and no grouping. With around
    /// twenty settings that becomes a wall, and related things end up far apart -
    /// the shifter's gear bindings sat so far below the shifter's own toggle that
    /// enabling it looked like nothing happened.
    /// </para>
    /// <para>
    /// Drawing it directly costs a little code and buys headings, collapsible
    /// sections, each device picker sitting with the feature it belongs to, and
    /// help text that is not squeezed into a tooltip UMM renders off the edge of
    /// the panel.
    /// </para>
    /// </remarks>
    internal static class SettingsPanel
    {
        private static bool _openSteering = true;
        private static bool _openFfb = true;
        private static bool _openCamera;
        private static bool _openShifter;
        private static bool _openTelemetry;
        private static bool _openTrouble;

        private static GUIStyle _wrap;
        private static GUIStyle Wrap => _wrap ?? (_wrap = new GUIStyle(GUI.skin.label) { wordWrap = true });

        public static void Draw()
        {
            var cfg = Main.Settings;
            if (cfg == null) return;

            DrawSteering(cfg);
            DrawForceFeedback(cfg);
            DrawShifter(cfg);
            DrawCamera(cfg);
            DrawTelemetry(cfg);
            DrawTrouble(cfg);
        }

        // --- sections ---------------------------------------------------------

        private static void DrawSteering(Settings cfg)
        {
            if (!Section("Steering", ref _openSteering)) return;

            cfg.DirectSteering = Toggle(cfg.DirectSteering, "Direct steering",
                "Removes the gamepad smoothing the game applies to wheels it does not recognise. " +
                "This is the same behaviour a recognised wheel already gets.");

            cfg.ZeroAxisDeadzone = Toggle(cfg.ZeroAxisDeadzone, "Remove hidden deadzone",
                "The game's input library applies its own 10% deadzone to unrecognised wheels, " +
                "separate from the one in the game's options and not shown anywhere.");

            cfg.BindAnyDevice = Toggle(cfg.BindAnyDevice, "Bind whichever device you touch",
                "The controls screen normally only binds the first controller.");

            cfg.UseDirectInputBackend = Toggle(cfg.UseDirectInputBackend, "Use DirectInput for controllers",
                "For a wheel the controls screen never responds to - a Fanatec base shows up as two " +
                "identical 'FANATEC Wheel' entries the game cannot read - and for shifters and stalks " +
                "it cannot see at all. Switches the game's input library to its DirectInput backend, " +
                "immediately. You will need to bind your wheel again, once. Turns itself off if the " +
                "keyboard stops reaching the game.");
            if (!string.IsNullOrEmpty(InputBackend.Status)) Help(InputBackend.Status);

            cfg.GlyphTextFallback = Toggle(cfg.GlyphTextFallback, "Show button names when no icon exists",
                "The game has no artwork for unrecognised wheels, so some bindings show an empty " +
                "box. This puts the button name there instead, e.g. B12.");

            GUILayout.Space(4);
            cfg.DisableSteerAssist = Toggle(cfg.DisableSteerAssist, "Disable steering assist",
                "CHANGES HOW THE CAR DRIVES. The game reduces your steering authority as the car " +
                "slides. Unlike the options above this is a driving aid, not a device fix, and " +
                "art of rally has online leaderboards.");

            End();
        }

        private static void DrawForceFeedback(Settings cfg)
        {
            if (!Section("Force feedback", ref _openFfb)) return;

            cfg.ForceFeedbackEnabled = Toggle(cfg.ForceFeedbackEnabled, "Enabled", null);

            if (cfg.ForceFeedbackEnabled)
            {
                cfg.Strength = (int)Slider(cfg.Strength, 0, 100, "Strength",
                    "How strong the wheel feels. 50 is the baseline.");

                Panel.DrawWheelPicker();

                cfg.Smoothing = Slider(cfg.Smoothing, 0f, 0.95f, "Smoothing",
                    "0 is raw and detailed but noisy over rough surfaces.", "F2");

                cfg.Invert = Toggle(cfg.Invert, "Invert direction",
                    "Turn on if the wheel pulls the wrong way.");

                cfg.DiagnosticLogging = Toggle(cfg.DiagnosticLogging, "Log detail for support", null);
            }

            End();
        }

        private static void DrawShifter(Settings cfg)
        {
            if (!Section("Shifter", ref _openShifter)) return;

            cfg.ShifterEnabled = Toggle(cfg.ShifterEnabled, "Use a separate shifter",
                "Reads a shifter directly, so it works even though the game's input system " +
                "cannot see most of them.");

            if (cfg.ShifterEnabled)
            {
                Panel.DrawShifterBinding(cfg);

                if (!cfg.ShifterIsHPattern)
                    cfg.SkipNeutral = Toggle(cfg.SkipNeutral, "Skip neutral",
                        "Reverse to first in one press instead of stopping on neutral.");
            }

            End();
        }

        private static void DrawCamera(Settings cfg)
        {
            if (!Section("Camera", ref _openCamera)) return;

            cfg.BonnetCameraEnabled = Toggle(cfg.BonnetCameraEnabled, "Bonnet camera",
                "Adds a bonnet view to the game's normal view rotation - press your change-view " +
                "button to cycle onto it. Bonnet, not cockpit: the cars have no interiors.");

            if (cfg.BonnetCameraEnabled)
            {
                cfg.CameraTuningKeys = Toggle(cfg.CameraTuningKeys, "Adjust with the numpad",
                    "While looking through it: 8/2 up-down, 7/9 back-forward, 4/6 left-right, " +
                    "1/3 tilt, +/- field of view, 0 resets. Saves automatically.");

                cfg.BonnetFOV    = Slider(cfg.BonnetFOV, 40f, 120f, "Bonnet field of view", null, "F0");
                cfg.BonnetLean   = Slider(cfg.BonnetLean, 0f, 1f, "Lean in corners",
                    "Sells the mounted feel, but is also the first thing to cause motion " +
                    "sickness. 0 turns it off. Shared by both mounted views.", "F2");

                GUILayout.Label("Position is easiest to set with the numpad while driving.", Wrap);
            }

            cfg.BumperCameraEnabled = Toggle(cfg.BumperCameraEnabled, "Bumper camera",
                "A second mounted view, lower and further forward, after the bonnet view in " +
                "the rotation. The numpad adjusts whichever of the two is on screen. " +
                "Takes effect on the next stage.");

            if (cfg.BumperCameraEnabled)
                cfg.BumperFOV = Slider(cfg.BumperFOV, 40f, 120f, "Bumper field of view", null, "F0");

            End();
        }

        private static void DrawTelemetry(Settings cfg)
        {
            if (!Section("Telemetry", ref _openTelemetry)) return;

            cfg.TelemetryEnabled = Toggle(cfg.TelemetryEnabled, "Send telemetry",
                "Forza-compatible UDP, for SimHub, dashboards, bass shakers and motion rigs. " +
                "Use a Forza Horizon 5 profile.");

            if (cfg.TelemetryEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Host", GUILayout.Width(60));
                cfg.TelemetryHost = GUILayout.TextField(cfg.TelemetryHost ?? "", GUILayout.Width(160));
                GUILayout.Label("Port", GUILayout.Width(40));
                string port = GUILayout.TextField(cfg.TelemetryPort.ToString(), GUILayout.Width(70));
                if (int.TryParse(port, out int p) && p > 0 && p <= 65535) cfg.TelemetryPort = p;
                GUILayout.EndHorizontal();

                // Live confirmation, so changing the port can be verified here
                // instead of by alt-tabbing to whatever is meant to receive it.
                // Changes apply immediately - no restart.
                string active = TelemetryPump.ActiveEndpoint;
                GUILayout.Label(active == null
                    ? "      Not sending yet - starts when you drive."
                    : "      Sending to " + active + "   (" + TelemetryPump.PacketsSent + " packets)",
                    Wrap);

                Help("Changing the host or port takes effect straight away. Useful if something " +
                     "else already owns the port - point the game at a spare one and forward it.");
            }

            End();
        }

        private static void DrawTrouble(Settings cfg)
        {
            if (!Section("Devices and troubleshooting", ref _openTrouble)) return;

            Panel.DrawInputStatus();

            GUILayout.Space(6);
            if (GUILayout.Button("Create support file on Desktop", GUILayout.Width(260)))
                SupportBundle.Create();

            GUILayout.Label(string.IsNullOrEmpty(SupportBundle.LastResult)
                ? "Collects your settings, devices, bindings and logs into one file to attach " +
                  "to a bug report."
                : SupportBundle.LastResult, Wrap);

            End();
        }

        // --- widgets ----------------------------------------------------------

        // A foldout header.
        //
        // Deliberately NOT a plain button: the device dropdowns are buttons with a
        // triangle, and when section headers looked the same it was impossible to
        // tell structure from control at a glance. Headers are now full-width bold
        // text on a rule, with a small marker; dropdowns stay indented, narrower,
        // and read as form fields.
        private static bool Section(string title, ref bool open)
        {
            GUILayout.Space(10);
            Rule();

            var header = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 4, 4)
            };
            header.normal.textColor = new Color(0.95f, 0.85f, 0.55f);

            // A label that responds to clicks, so the header does not look like a
            // button while still folding.
            if (GUILayout.Button((open ? "▾ " : "▸ ") + title.ToUpperInvariant(),
                                 header, GUILayout.ExpandWidth(true)))
                open = !open;

            Rule();
            if (open) GUILayout.Space(4);
            return open;
        }

        // One-pixel separator, drawn as a stretched box.
        private static void Rule()
        {
            var line = new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedHeight = 1
            };
            GUILayout.Box(GUIContent.none, line, GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private static void End() => GUILayout.Space(6);

        private static bool Toggle(bool value, string label, string help)
        {
            bool result = GUILayout.Toggle(value, "  " + label);
            if (!string.IsNullOrEmpty(help)) Help(help);
            return result;
        }

        private static float Slider(float value, float min, float max, string label,
                                    string help, string format = "F0")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("  " + label, GUILayout.Width(150));
            float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(180));
            GUILayout.Label(result.ToString(format), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(help)) Help(help);
            return result;
        }

        // Help sits under its control as ordinary wrapped text rather than in a
        // tooltip, which UMM draws to the left of a "?" with no way to change side
        // - so anything longer than a few words runs off the panel.
        private static void Help(string text)
        {
            var style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
            style.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
            GUILayout.Label("      " + text, style);
        }
    }
}
