using System;
using System.IO;
using System.Reflection;
using System.Text;
using Rewired;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Writes a single text file with everything needed to diagnose a force
    /// feedback OR a binding problem, for a user to attach to a bug report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built after a Fanatec user reported no force feedback. Their raw log was
    /// 575 KB and 13,701 lines, of which all but about fifteen were
    /// <c>SetDeviceForcesXY</c> noise — and the answer came from summarising those
    /// numbers rather than reading them. So this does that triage up front instead
    /// of asking someone to send a huge file and hoping.
    /// </para>
    /// <para>
    /// It keeps every line that is not a force update, because those carry the
    /// device enumeration and any errors, and replaces the force updates with
    /// statistics. Range and sign balance answer the first question in any FFB
    /// report — is the mod computing forces at all, or is the device rejecting
    /// them — which are opposite problems that look identical from the outside.
    /// </para>
    /// <para>
    /// Binding problems needed a second pass, because logs are the wrong source
    /// for them. The game prints its controller list once at startup, which in a
    /// real session is tens of thousands of lines back, and what is actually bound
    /// is never printed at all. So the controller and binding sections are read
    /// live from Rewired when the button is pressed, and the mod's own log is
    /// collected from UMM rather than assumed to be in Unity's.
    /// </para>
    /// </remarks>
    internal static class SupportBundle
    {
        /// <summary>Path written by the last successful call, for display in the UI.</summary>
        public static string LastPath { get; private set; }

        /// <summary>Message to show in the settings panel after a run.</summary>
        public static string LastResult { get; private set; }

        /// <summary>Gathers diagnostics into one file on the desktop.</summary>
        public static void Create()
        {
            if (GameState.IsDriving)
            {
                LastResult = "Pause the game before creating a support file, so collecting logs cannot cause a driving hitch.";
                return;
            }
            try
            {
                var sb = new StringBuilder();
                WriteHeader(sb);
                WriteSettings(sb);
                Main.WriteLoadedMods(sb);
                FrameHealth.Current.Append(sb);
                WriteRuntimeInputs(sb);
                WriteControllers(sb);
                WriteBindings(sb);
                WriteFfbLog(sb);
                WriteModLog(sb);
                WriteUnityLog(sb);

                string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    dir = Path.GetTempPath();

                string path = Path.Combine(dir,
                    "art-of-sim-rally-support-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

                File.WriteAllText(path, sb.ToString());

                LastPath = path;
                LastResult = "Written to " + path;
                ModLog.Info("Support bundle: " + path);
            }
            catch (Exception ex)
            {
                LastResult = "Failed: " + ex.Message;
                ModLog.Error("Support bundle failed: " + ex);
            }
        }

        /// <summary>
        /// The vendored dbce-wheel-mod-toolkit release, baked in at build time by
        /// the csproj from lib/toolkit/VERSION, which is not shipped.
        /// </summary>
        private static string ToolkitPin
        {
            get
            {
                try
                {
                    foreach (var a in Assembly.GetExecutingAssembly()
                                 .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false))
                    {
                        var m = (AssemblyMetadataAttribute)a;
                        if (m.Key == "ToolkitPin") return m.Value;
                    }
                }
                catch { }
                return "(unknown)";
            }
        }

        private static void WriteHeader(StringBuilder sb)
        {
            sb.AppendLine("art of sim rally - support bundle");
            sb.AppendLine("=================================");
            sb.AppendLine("generated : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("mod       : " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?"));
            var identity = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute));
            sb.AppendLine("build     : " + (identity?.InformationalVersion ?? "(unknown)"));
            sb.AppendLine("mod sha256: " + NativeDiagnostics.FileHash(Assembly.GetExecutingAssembly().Location));
            sb.AppendLine("os        : " + SystemInfo.operatingSystem);
            sb.AppendLine("unity     : " + Application.unityVersion);
            sb.AppendLine("game      : " + Application.productName + " " + Application.version);

            // Two different numbers, and conflating them produces a false alarm.
            //
            // "toolkit pin" is the vendored release, baked in at build time.
            // "native abi" is the DLL's own GetWheelFfbVersion, which moves only
            // when native\wheelffb changes - so a build pinned at v0.7.1 reports
            // an ABI of 0.4.0 and that is CORRECT, because nothing in the native
            // layer moved across those releases. An earlier revision printed only
            // the ABI and told people a number lower than their release meant a
            // stale DLL, which would have flagged every healthy install.
            //
            // The installer intentionally installs two copies. Compare hashes
            // against the candidate manifest; a plugin-directory path alone is
            // not evidence of a stale DLL. Report every resident same-name copy.
            sb.AppendLine("toolkit pin: " + ToolkitPin + "  (dbce-wheel-mod-toolkit, vendored)");
            var forceLibrary = typeof(Dbce.Wheel.Ffb.AxleForceCurve).Assembly;
            sb.AppendLine("force pipeline: AxleForceCurve@" + Dbce.Wheel.Ffb.AxleForceCurve.CompatibilityVersion);
            sb.AppendLine("managed force library: " + forceLibrary.GetName().Version + "  " + forceLibrary.Location);
            sb.AppendLine("managed force library SHA-256: " + NativeDiagnostics.FileHash(forceLibrary.Location));
            sb.AppendLine("native binding: " + (Dbce.Wheel.Ffb.WheelFfbNative.LoadedFrom ?? "(not loaded)"));
            sb.AppendLine("native last error: " + (Dbce.Wheel.Ffb.WheelFfbNative.LastError ?? "(none)"));
            sb.AppendLine("preload requested: " + FfbNative.RequestedPath);
            sb.AppendLine(NativeDiagnostics.Describe("UnityForceFeedback.dll"));
            sb.AppendLine();
        }

        // Settings are dumped by reflection so a field added later is included
        // without anyone remembering to update this.
        private static void WriteSettings(StringBuilder sb)
        {
            sb.AppendLine("--- settings ---");
            var s = Main.Settings;
            if (s == null) { sb.AppendLine("(unavailable)"); sb.AppendLine(); return; }

            foreach (var f in s.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v;
                try { v = f.GetValue(s); } catch { v = "<unreadable>"; }
                sb.AppendLine("  " + f.Name.PadRight(24) + " = " + v);
            }
            sb.AppendLine();
        }

        private static void WriteRuntimeInputs(StringBuilder sb)
        {
            sb.AppendLine("--- direct inputs (cached latest sample; not a history) ---");
            sb.AppendLine("direct input enabled: " + WheelInput.Enabled);
            sb.AppendLine("device state: " + WheelInput.DeviceSummary);
            sb.AppendLine("assignment status: " + WheelInput.Status);
            foreach (var channel in WheelInput.Channels)
                sb.AppendLine(channel + ": " + WheelInput.Describe(channel) + "; value=" +
                    WheelInput.Value(channel).ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine("game restart active: " + GameState.IsRestarting);
            sb.AppendLine("other CameraMod loaded: " + Main.OtherCameraModLoaded);
            try
            {
                var car = GameEntryPoint.EventManager?.playerManager?.carcontroller;
                if (car != null)
                    sb.AppendLine("live car steering limiter=" + car.steerAssistance + "; correction factor=" + car.steerCorrectionFactor);
            }
            catch { sb.AppendLine("live car state unavailable"); }
            sb.AppendLine("DisableSteerAssist is a legacy spawn-only boolean override, not a saved numeric assist value.");
            sb.AppendLine();
        }

        // Live Rewired state, read at the moment the user presses the button.
        // Far more reliable than scraping it back out of a log: the game prints
        // its controller list once at startup, and by the time anyone thinks to
        // collect diagnostics that is tens of thousands of lines in the past.
        private static void WriteControllers(StringBuilder sb)
        {
            sb.AppendLine("--- controllers (live) ---");
            try
            {
                if (!ReInput.isReady) { sb.AppendLine("Rewired not ready"); sb.AppendLine(); return; }

                var player = PadManager.GetPlayer();
                var joysticks = ReInput.controllers.Joysticks;
                sb.AppendLine("joysticks attached : " + (joysticks?.Count ?? 0));
                sb.AppendLine("assigned to player : " + (player?.controllers.joystickCount ?? 0));
                sb.AppendLine("input backend      : " + InputBackend.Describe());
                sb.AppendLine();

                if (joysticks == null) { sb.AppendLine(); return; }

                for (int i = 0; i < joysticks.Count; i++)
                {
                    var j = joysticks[i];
                    bool assigned = player != null && player.controllers.ContainsController(j);
                    bool recognised = j.hardwareTypeGuid != Guid.Empty;

                    sb.AppendLine("[" + i + "] " + j.name);
                    sb.AppendLine("     recognised by Rewired : " + (recognised ? "yes" : "NO"));
                    sb.AppendLine("     assigned to player    : " + (assigned ? "yes" : "NO"));
                    sb.AppendLine("     axes / buttons        : " + j.axisCount + " / " + j.buttonCount);
                    sb.AppendLine("     hardware id           : " + j.hardwareIdentifier);

                    // Deadzone matters because an unrecognised device gets
                    // Rewired's 0.1 default, which the game's own options screen
                    // cannot see or change.
                    var map = j.calibrationMap;
                    if (map != null && map.axisCount > 0)
                    {
                        var dz = new StringBuilder();
                        for (int a = 0; a < map.axisCount && a < 12; a++)
                        {
                            var axis = map.GetAxis(a);
                            if (axis != null) dz.Append(axis.deadZone.ToString("F3")).Append(' ');
                        }
                        sb.AppendLine("     axis deadzones        : " + dz);
                    }
                    sb.AppendLine();
                }

                if (joysticks.Count > 1)
                {
                    sb.AppendLine("NOTE: more than one joystick is present. The game's own controls");
                    sb.AppendLine("screen only ever binds Joysticks[0], so without 'Bind any device'");
                    sb.AppendLine("the others cannot be configured, and where two share a name it is");
                    sb.AppendLine("not obvious which one index 0 is.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex) { sb.AppendLine("failed: " + ex.Message); sb.AppendLine(); }
        }

        // What is actually bound. The single most useful thing for a "my wheel
        // does nothing" report, and impossible to infer from any log.
        private static void WriteBindings(StringBuilder sb)
        {
            sb.AppendLine("--- bindings (live) ---");
            try
            {
                if (!ReInput.isReady) { sb.AppendLine("Rewired not ready"); sb.AppendLine(); return; }

                var player = PadManager.GetPlayer();
                if (player == null) { sb.AppendLine("no player"); sb.AppendLine(); return; }

                int total = 0;
                foreach (var j in player.controllers.Joysticks)
                {
                    sb.AppendLine(j.name + ":");
                    int n = 0;
                    foreach (var map in player.controllers.maps.GetMaps<JoystickMap>(j.id))
                    {
                        if (map == null) continue;
                        foreach (var aem in map.AllMaps)
                        {
                            sb.AppendLine("    " + aem.actionDescriptiveName.PadRight(24) +
                                          " <- " + aem.elementIdentifierName);
                            n++; total++;
                        }
                    }
                    if (n == 0) sb.AppendLine("    (nothing bound to this device)");
                    sb.AppendLine();
                }

                if (total == 0)
                {
                    sb.AppendLine("READING: nothing is bound to any joystick. The wheel will not");
                    sb.AppendLine("control the car regardless of force feedback. Bind it in the");
                    sb.AppendLine("game's controls screen first.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex) { sb.AppendLine("failed: " + ex.Message); sb.AppendLine(); }
        }

        private static void WriteModLog(StringBuilder sb)
        {
            sb.AppendLine("--- mod log (tail) ---");
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "",
                @"artofrally_Data\Managed\UnityModManager\Log.txt");
            SupportLogs.AppendTail(sb, path, 200);
            sb.AppendLine();
        }

        private static void WriteFfbLog(StringBuilder sb)
        {
            sb.AppendLine("--- force feedback log ---");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArtOfSimRally", "ffb.log");
            sb.AppendLine("source: " + path);
            try { SupportLogs.AppendNative(sb, SupportLogs.ReadTail(path)); }
            catch (Exception ex)
            {
                sb.AppendLine("log unavailable: " + ex.Message);
                sb.AppendLine("Logging can be disabled with DBCE_FFB_LOG=0. An absent log is not proof of a missing plugin; see observed module identity above.");
            }
            sb.AppendLine();
        }

        private static void WriteUnityLog(StringBuilder sb)
        {
            sb.AppendLine("--- unity player log (tail) ---");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\LocalLow\Funselektor Labs\Art of Rally\Player.log");
            SupportLogs.AppendTail(sb, path, 120);
            sb.AppendLine();
        }
    }
}
