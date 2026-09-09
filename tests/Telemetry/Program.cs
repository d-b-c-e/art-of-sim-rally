using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;

internal static class Program
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void CheckGameState(Assembly mod)
    {
        var state = mod.GetType("ArtOfSimRally.Mod.GameState", true);
        var entry = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GameEntryPoint")).FirstOrDefault(t => t != null);
        Check(entry != null, "GameEntryPoint was not resolved from the actual game assembly");
        var field = entry.GetField("eventManager", Static);
        Check(field != null && field.IsPrivate, "Actual backing field contract changed");
        var original = field.GetValue(null);
        Func<string, bool> flag = name => (bool)state.GetProperty(name, Static).GetValue(null);
        try
        {
            field.SetValue(null, null);
            Check(state.GetProperty("ExistingManager", Static).GetValue(null) == null, "Absent manager was created");
            Check(!flag("IsDriving") && !flag("IsEngineLive") && !flag("IsPlayerView") && !flag("IsRestarting"), "Absent game state not parked");
            // Exercise the production field reader against the actual game's
            // private field, without running a Unity manager constructor.
            var manager = FormatterServices.GetUninitializedObject(entry.Assembly.GetType("StageSceneManager", true));
            var statusField = field.FieldType.GetField("status");
            field.SetValue(null, manager);
            foreach (string status in new[] { "UNDERWAY", "WAITING_TO_BEGIN", "PAUSED", "FINISHING_STAGE_ANIMATION", "FINISHED", "REPLAY" })
            {
                statusField.SetValue(manager, Enum.Parse(statusField.FieldType, status));
                bool driving = status == "UNDERWAY", engine = driving || status == "WAITING_TO_BEGIN";
                Check(flag("IsDriving") == driving, "Actual driving state mismatch: " + status);
                Check(flag("IsEngineLive") == engine, "Actual engine state mismatch: " + status);
                Check(flag("IsPlayerView") == (engine || status == "PAUSED"), "Actual camera state mismatch: " + status);
            }
            field.SetValue(null, null);
            Check(!flag("IsDriving") && !flag("IsEngineLive") && !flag("IsPlayerView"), "Actual manager retained after teardown");
        }
        finally { field.SetValue(null, original); }
    }
    static void DrivingConnection(Assembly mod, Type pump, object cfg)
    {
        var entry = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GameEntryPoint")).First(t => t != null);
        var field = entry.GetField("eventManager", Static); var original = field.GetValue(null);
        var manager = FormatterServices.GetUninitializedObject(entry.Assembly.GetType("StageSceneManager", true));
        var status = field.FieldType.GetField("status");
        var main = mod.GetType("ArtOfSimRally.Mod.Main", true);
        var settings = main.GetProperty("Settings", Static); var enabled = main.GetProperty("Enabled", Static);
        var originalSettings = settings.GetValue(null); var originalEnabled = enabled.GetValue(null);
        using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
        using (var next = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
        {
            listener.Client.ReceiveTimeout = 2000;
            cfg.GetType().GetField("TelemetryHost").SetValue(cfg, "127.0.0.1");
            cfg.GetType().GetField("TelemetryPort").SetValue(cfg, ((IPEndPoint)listener.Client.LocalEndPoint).Port);
            cfg.GetType().GetField("TelemetryEnabled").SetValue(cfg, true);
            field.SetValue(null, manager); status.SetValue(manager, Enum.Parse(status.FieldType, "UNDERWAY"));
            try
            {
                settings.SetValue(null, cfg); enabled.SetValue(null, true);
                Check(!(bool)pump.GetMethod("EnsureSender", Static).Invoke(null, new[] { cfg }), "telemetry created a socket from the driving connection path");
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "driving connection retained socket");
                pump.GetMethod("Prepare", Static).Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "driving Prepare connected");
                status.SetValue(manager, Enum.Parse(status.FieldType, "PAUSED"));
                pump.GetMethod("Prepare", Static).Invoke(null, null);
                string active = (string)pump.GetProperty("ActiveEndpoint", Static).GetValue(null);
                Check(active != null, "idle watchdog did not connect");
                status.SetValue(manager, Enum.Parse(status.FieldType, "UNDERWAY"));
                cfg.GetType().GetField("TelemetryPort").SetValue(cfg, ((IPEndPoint)next.Client.LocalEndPoint).Port);
                Check((bool)pump.GetProperty("EndpointPending", Static).GetValue(null), "destination change not pending");
                Check((bool)pump.GetMethod("EnsureSender", Static).Invoke(null, new[] { cfg }), "working endpoint stopped during driving edit");
                pump.GetMethod("Prepare", Static).Invoke(null, null);
                Check((string)pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == active, "driving edit switched destination");
                var frameType = pump.GetMethod("SendFrame", Static).GetParameters()[0].ParameterType;
                var frame = Activator.CreateInstance(frameType); frameType.GetField("IsRaceOn").SetValue(frame, true);
                pump.GetMethod("SendFrame", Static).Invoke(null, new[] { frame });
                IPEndPoint peer = null;
                Check(BitConverter.ToInt32(listener.Receive(ref peer), 0) == 1, "current endpoint did not receive live frame");
                cfg.GetType().GetField("TelemetryEnabled").SetValue(cfg, false);
                pump.GetMethod("StopIfDisabled", Static).Invoke(null, null);
                for (int i = 0; i < 3; i++) Check(BitConverter.ToInt32(listener.Receive(ref peer), 0) == 0, "telemetry disable did not park during driving");
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "telemetry disable retained connection");
                pump.GetMethod("StopIfDisabled", Static).Invoke(null, null);
                Check(listener.Available == 0, "disabled telemetry repeatedly parked");
                cfg.GetType().GetField("TelemetryEnabled").SetValue(cfg, true);
                pump.GetMethod("Prepare", Static).Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "re-enable reconnected while driving");
                status.SetValue(manager, Enum.Parse(status.FieldType, "PAUSED"));
                pump.GetMethod("Prepare", Static).Invoke(null, null);
                Check(!(bool)pump.GetProperty("EndpointPending", Static).GetValue(null), "pause did not apply destination");
            }
            finally
            {
                field.SetValue(null, original); settings.SetValue(null, originalSettings); enabled.SetValue(null, originalEnabled);
                pump.GetMethod("Shutdown", Static).Invoke(null, null);
            }
        }
    }
    static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            var paths = new[] { Path.Combine(root, "src/ArtOfSimRally.Mod/bin/Release"), Path.Combine(root, "lib/umm"), Path.Combine(args[1], "artofrally_Data/Managed") };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => paths.Select(p => Path.Combine(p, new AssemblyName(e.Name).Name + ".dll")).Where(File.Exists).Select(Assembly.LoadFrom).FirstOrDefault();
            var mod = Assembly.LoadFrom(Path.Combine(paths[0], "ArtOfSimRally.Mod.dll"));
            // Force dependency resolution before looking up the real private field.
            mod.GetType("ArtOfSimRally.Mod.GameState", true).GetProperty("ExistingManager", Static).GetValue(null);
            CheckGameState(mod);
            var pump = mod.GetType("ArtOfSimRally.Mod.TelemetryPump", true);
            var settingsType = mod.GetType("ArtOfSimRally.Mod.Settings", true); var cfg = Activator.CreateInstance(settingsType);
            DrivingConnection(mod, pump, cfg);
            var connect = pump.GetMethod("EnsureSender", Static);
            var park = pump.GetMethod("Park", Static); var shutdown = pump.GetMethod("Shutdown", Static);
            int errors = 0;
            mod.GetType("ArtOfSimRally.Mod.ModLog", true).GetMethod("Attach", Static).Invoke(null, new object[] { (Action<string>)(s => { }), (Action<string>)(s => { }), (Action<string>)(s => errors++) });
            settingsType.GetField("TelemetryHost").SetValue(cfg, "127.0.0.1");
            settingsType.GetField("TelemetryPort").SetValue(cfg, -1);
            Check(!(bool)connect.Invoke(null, new[] { cfg }), "invalid port accepted");
            Check(errors == 1, "failure not reported once");
            for (int i = 0; i < 10; i++) Check(!(bool)connect.Invoke(null, new[] { cfg }), "failed endpoint retried");
            Check(errors == 1, "connection failure floods log/hot path");
            using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            using (var next = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                listener.Client.ReceiveTimeout = 2000; next.Client.ReceiveTimeout = 2000;
                settingsType.GetField("TelemetryPort").SetValue(cfg, ((IPEndPoint)listener.Client.LocalEndPoint).Port);
                Check((bool)connect.Invoke(null, new[] { cfg }), "corrected endpoint remained disabled");
                park.Invoke(null, null);
                for (int i = 0; i < 3; i++)
                {
                    IPEndPoint peer = null; byte[] packet = listener.Receive(ref peer);
                    Check(packet.Length == 324 && packet[323] == (byte)'R', "loopback packet format/sentinel mismatch");
                    Check(BitConverter.ToInt32(packet, 0) == 0, "park did not clear IsRaceOn");
                }
                Check((long)pump.GetProperty("PacketsSent", Static).GetValue(null) == 3, "park did not send three packets");
                Check((bool)connect.Invoke(null, new[] { cfg }) && (long)pump.GetProperty("PacketsSent", Static).GetValue(null) == 3, "unchanged target recreated socket");
                settingsType.GetField("TelemetryPort").SetValue(cfg, ((IPEndPoint)next.Client.LocalEndPoint).Port);
                Check((bool)connect.Invoke(null, new[] { cfg }), "destination switch failed");
                for (int i = 0; i < 3; i++) { IPEndPoint peer = null; Check(listener.Receive(ref peer).Length == 324, "old destination did not park before switch"); }
                park.Invoke(null, null);
                for (int i = 0; i < 3; i++) { IPEndPoint peer = null; Check(next.Receive(ref peer).Length == 324, "new destination did not receive"); }
                // Close the actual transport under the shared sender. Its Send
                // returns false rather than throwing; the consumer must observe it.
                var senderInstance = pump.GetField("_sender", Static).GetValue(null);
                var socket = (UdpClient)senderInstance.GetType().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(senderInstance);
                socket.Close();
                var frameType=mod.GetType("ArtOfSimRally.Mod.TelemetryPump", true).GetMethod("SendFrame", Static).GetParameters()[0].ParameterType;
                pump.GetMethod("SendFrame", Static).Invoke(null, new[] { Activator.CreateInstance(frameType) });
                Check((long)senderInstance.GetType().GetProperty("SendFailures").GetValue(senderInstance)==1, "real socket failure was not counted");
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "failed socket was retained");
                Check(!(bool)connect.Invoke(null, new[] { cfg }), "send failure retried unchanged endpoint");
                int errorsAfterSend=errors;
                for(int i=0;i<10;i++) pump.GetMethod("SendFrame", Static).Invoke(null,new[]{Activator.CreateInstance(frameType)});
                Check(errors==errorsAfterSend,"failed send repeatedly logged");
                shutdown.Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "shutdown retained socket");
                Check((bool)connect.Invoke(null, new[] { cfg }), "explicit restart failed");
                shutdown.Invoke(null, null); shutdown.Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "duplicate shutdown failed");
            }
            Console.WriteLine("{\"status\":\"passed\",\"assertions\":" + assertions + ",\"scope\":\"production game-state field access, telemetry recovery and loopback UDP; no Unity physics or SimHub\"}"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
