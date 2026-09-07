using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

// Runs the actual net48 developer assembly and installed Harmony on the CLR.
// Resolves local game metadata but never starts Unity or initializes DirectInput.
internal static class Program
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]), game = Path.GetFullPath(args[1]);
            string probePath = Path.Combine(root, "tools/testing/Recorder/bin/Release/net48/ArtOfSimRally.DevRecorder.dll");
            var paths = new[] { Path.Combine(root, "src/ArtOfSimRally.Mod/bin/Release"), Path.Combine(root, "lib/umm"), Path.Combine(game, "artofrally_Data/Managed"), Path.GetDirectoryName(probePath) };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string name = new AssemblyName(e.Name).Name + ".dll";
                return paths.Select(p => Path.Combine(p, name)).Where(File.Exists).Select(Assembly.LoadFrom).FirstOrDefault();
            };
            var mod = Assembly.LoadFrom(Path.Combine(paths[0], "ArtOfSimRally.Mod.dll"));
            var force = Assembly.LoadFrom(Path.Combine(paths[0], "Dbce.Wheel.Ffb.dll"));
            var probe = Assembly.LoadFrom(probePath);
            var recorder = probe.GetType("ArtOfSimRally.Testing.RecorderMain", true);
            recorder.GetMethod("Attach", Static).Invoke(null, new object[] { mod, force });
            Check(recorder.GetField("patches", Static).GetValue(null) != null, "probe failed to attach");
            var session = recorder.GetField("Session", Static).GetValue(null); var sessionType = session.GetType();
            // Start bounded in-memory capture without invoking Unity's identity APIs.
            sessionType.GetMethod("Start").Invoke(session, new object[] { false, new XElement("capture", new XAttribute("origin", "synthetic")), Path.Combine(root, "results/probe-hooks-unused"), 8, 8 });
            mod.GetType("ArtOfSimRally.Mod.FfbController", true).GetMethod("Reset", Static).Invoke(null, null);
            Check((int)sessionType.GetField("epoch", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session) == 1, "Harmony Reset observation did not execute");
            var native = force.GetType("Dbce.Wheel.Ffb.WheelFfbNative", true);
            Check(!(bool)native.GetProperty("Ready").GetValue(null), "unexpected hardware initialization");
            recorder.GetField("observing", Static).SetValue(null, true);
            bool accepted = (bool)native.GetMethod("SetForce").Invoke(null, new object[] { 123 });
            Check(!accepted, "uninitialized wrapper accepted force");
            Check((bool)recorder.GetField("sent", Static).GetValue(null), "Harmony Send prefix did not execute");
            Check((int)recorder.GetField("device", Static).GetValue(null) == 123, "probe observed wrong force argument");
            Check((int)sessionType.GetField("deliveryFailures", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session) == 1, "Harmony Send postfix lost failed delivery");
            recorder.GetField("observing", Static).SetValue(null, false);
            // Drive/Frame callbacks use Unity ECalls; only Unity can execute
            // those. CLR verifies their patch installation, not their runtime.
            var patches = recorder.GetField("patches", Static).GetValue(null);
            patches.GetType().GetMethod("UnpatchAll").Invoke(patches, new object[] { "ArtOfSimRally.DevRecorder" });
            mod.GetType("ArtOfSimRally.Mod.FfbController").GetMethod("Reset", Static).Invoke(null, null);
            Check((int)sessionType.GetField("epoch", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session) == 1, "unload left probe hook installed");
            var controlType = probe.GetType("ArtOfSimRally.Testing.ControlServer", true);
            using (var control = (IDisposable)Activator.CreateInstance(controlType, new object[] { "ArtOfSimRally.DevRecorder." + System.Diagnostics.Process.GetCurrentProcess().Id }))
            {
                foreach (string command in new[] { "Start", "Status", "Stop" })
                {
                    var start = new System.Diagnostics.ProcessStartInfo("pwsh", "-NoProfile -File \"" + Path.Combine(root, "tools/testing/Record-Drive.ps1") + "\" -GameProcessId " + System.Diagnostics.Process.GetCurrentProcess().Id + " -Command " + command)
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                    using (var client = System.Diagnostics.Process.Start(start))
                    {
                        var stdout = client.StandardOutput.ReadToEndAsync(); var stderr = client.StandardError.ReadToEndAsync();
                        var timer = System.Diagnostics.Stopwatch.StartNew();
                        while (!client.HasExited && timer.Elapsed.TotalSeconds < 15)
                        {
                            controlType.GetMethod("Pump").Invoke(control, new object[] { (Func<string, string>)(c => "OK fixture-" + c) });
                            System.Threading.Thread.Sleep(5);
                        }
                        if (!client.HasExited) { client.Kill(); throw new Exception("External controller hung"); }
                        Check(client.ExitCode == 0, "External controller failed: " + stderr.Result);
                        Check(stdout.Result.Trim() == "fixture-" + command.ToUpperInvariant(), "External controller reply mismatch");
                    }
                }
            }
            Console.WriteLine("{\"status\":\"passed\",\"assertions\":" + assertions + ",\"scope\":\"actual net48 probe/Harmony attach-observe-unpatch; no Unity runtime or hardware\"}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
