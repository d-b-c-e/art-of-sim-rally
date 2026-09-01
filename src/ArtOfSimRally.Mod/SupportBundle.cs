using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            try
            {
                var sb = new StringBuilder();
                WriteHeader(sb);
                WriteSettings(sb);
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

        private static void WriteHeader(StringBuilder sb)
        {
            sb.AppendLine("art of sim rally - support bundle");
            sb.AppendLine("=================================");
            sb.AppendLine("generated : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("mod       : " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?"));
            sb.AppendLine("os        : " + SystemInfo.operatingSystem);
            sb.AppendLine("unity     : " + Application.unityVersion);
            sb.AppendLine("game      : " + Application.productName + " " + Application.version);
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

        // UMM's log, which is where every ModLog line from this mod ends up -
        // including the Rewired calibration before/after dump and the binding
        // target messages.
        private static void WriteModLog(StringBuilder sb)
        {
            sb.AppendLine("--- mod log (tail) ---");
            string path = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? "",
                @"artofrally_Data\Managed\UnityModManager\Log.txt");

            if (!File.Exists(path)) { sb.AppendLine("not found at " + path); sb.AppendLine(); return; }
            try
            {
                var lines = File.ReadAllLines(path);
                int start = Math.Max(0, lines.Length - 200);
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
            }
            catch (Exception ex) { sb.AppendLine("could not read: " + ex.Message); }
            sb.AppendLine();
        }

        private static void WriteFfbLog(StringBuilder sb)
        {
            sb.AppendLine("--- force feedback log ---");

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArtOfSimRally", "ffb.log");

            if (!File.Exists(path))
            {
                sb.AppendLine("NOT FOUND at " + path);
                sb.AppendLine("The native plugin has never been loaded. Most likely");
                sb.AppendLine("UnityForceFeedback.dll is missing from artofrally_Data/Plugins/x86_64,");
                sb.AppendLine("or was put somewhere else.");
                sb.AppendLine();
                return;
            }

            string[] lines;
            try { lines = File.ReadAllLines(path); }
            catch (Exception ex) { sb.AppendLine("could not read: " + ex.Message); sb.AppendLine(); return; }

            var force = new Regex(@"SetDeviceForcesXY\((-?\d+),\s*(-?\d+)\)");
            int count = 0, min = int.MaxValue, max = int.MinValue, neg = 0, pos = 0, zero = 0;
            var kept = new StringBuilder();
            int keptCount = 0;

            foreach (var line in lines)
            {
                var m = force.Match(line);
                if (m.Success)
                {
                    int x = int.Parse(m.Groups[1].Value);
                    count++;
                    if (x < min) min = x;
                    if (x > max) max = x;
                    if (x < 0) neg++; else if (x > 0) pos++; else zero++;
                    continue;
                }

                // Everything else - device enumeration, errors, lifecycle - is the
                // part a human needs to read. Cap it so a long session cannot
                // produce an unusable file.
                if (keptCount < 400) { kept.AppendLine(line); keptCount++; }
            }

            sb.AppendLine("force updates sent : " + count);
            if (count > 0)
            {
                int peak = Math.Max(Math.Abs(min), Math.Abs(max));
                sb.AppendLine("  range            : " + min + " .. " + max + "   (full scale is +/-10000)");
                sb.AppendLine("  peak             : " + (peak / 100.0).ToString("F2") + "% of full scale");
                sb.AppendLine("  sign balance     : " + neg + " negative, " + pos + " positive, " + zero + " zero");
                sb.AppendLine();
                sb.AppendLine(peak < 500
                    ? "  READING: forces are very weak. This is a tuning problem - lower"
                    + Environment.NewLine +
                      "  'Reference torque' until the peak approaches full scale."
                    : "  READING: the mod is computing strong forces. If the wheel still does"
                    + Environment.NewLine +
                      "  nothing, they are being rejected by the device - check for"
                    + Environment.NewLine +
                      "  'SetParameters FAILED' below.");
            }
            else
            {
                sb.AppendLine("  READING: no forces were ever sent. Either force feedback is off in");
                sb.AppendLine("  the settings, or no stage was driven, or initialisation failed - see below.");
            }

            sb.AppendLine();
            sb.AppendLine("non-force log lines (device list, errors, lifecycle):");
            sb.AppendLine(kept.ToString());
            if (keptCount >= 400) sb.AppendLine("  ... truncated at 400 lines ...");
            sb.AppendLine();
        }

        // Unity's own log catches anything that threw before our logging existed,
        // e.g. a DllNotFoundException from a misplaced native plugin.
        private static void WriteUnityLog(StringBuilder sb)
        {
            sb.AppendLine("--- unity player log (tail) ---");
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\LocalLow\Funselektor Labs\Art of Rally\Player.log");

            if (!File.Exists(path)) { sb.AppendLine("not found at " + path); return; }

            try
            {
                var lines = File.ReadAllLines(path);

                // The controller enumeration is printed once at startup. In a real
                // session that is tens of thousands of lines back - measured at
                // line 57,693 of 58,365 on this machine - so a plain tail misses
                // the single most useful block in the file. Find it instead.
                int rewired = -1;
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].IndexOf("Rewired version", StringComparison.OrdinalIgnoreCase) >= 0)
                    { rewired = i; break; }
                }
                if (rewired >= 0)
                {
                    sb.AppendLine("[controller enumeration at line " + (rewired + 1) + "]");
                    for (int i = rewired; i < Math.Min(lines.Length, rewired + 40); i++)
                        sb.AppendLine(lines[i]);
                    sb.AppendLine();
                }

                sb.AppendLine("[tail]");
                int start = Math.Max(0, lines.Length - 120);
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
            }
            catch (Exception ex) { sb.AppendLine("could not read: " + ex.Message); }
        }
    }
}
