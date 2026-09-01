using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Writes a single text file with everything needed to diagnose a force
    /// feedback problem, for a user to attach to a bug report.
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
                WriteFfbLog(sb);
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
                int start = Math.Max(0, lines.Length - 120);
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
            }
            catch (Exception ex) { sb.AppendLine("could not read: " + ex.Message); }
        }
    }
}
