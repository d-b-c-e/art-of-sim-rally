using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

internal static class Program
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            var paths = new[] { Path.Combine(root, "src/ArtOfSimRally.Mod/bin/Release"), Path.Combine(root, "lib/umm"), Path.Combine(args[1], "artofrally_Data/Managed") };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => paths.Select(p => Path.Combine(p, new AssemblyName(e.Name).Name + ".dll")).Where(File.Exists).Select(Assembly.LoadFrom).FirstOrDefault();
            var mod = Assembly.LoadFrom(Path.Combine(paths[0], "ArtOfSimRally.Mod.dll"));
            var pump = mod.GetType("ArtOfSimRally.Mod.TelemetryPump", true);
            var settingsType = mod.GetType("ArtOfSimRally.Mod.Settings", true); var cfg = Activator.CreateInstance(settingsType);
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
                pump.GetMethod("Failed", Static).Invoke(null, new object[] { new IOException("simulated send failure") });
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "failed socket was retained");
                Check(!(bool)connect.Invoke(null, new[] { cfg }), "send failure retried unchanged endpoint");
                shutdown.Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "shutdown retained socket");
                Check((bool)connect.Invoke(null, new[] { cfg }), "explicit restart failed");
                shutdown.Invoke(null, null); shutdown.Invoke(null, null);
                Check(pump.GetProperty("ActiveEndpoint", Static).GetValue(null) == null, "duplicate shutdown failed");
            }
            Console.WriteLine("{\"status\":\"passed\",\"assertions\":" + assertions + ",\"scope\":\"production telemetry recovery and loopback UDP; no Unity physics or SimHub\"}"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
