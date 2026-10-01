using System;
using System.IO;
using System.IO.Pipes;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using ArtOfSimRally.Testing;

internal static class Program
{
    static int assertions;
    static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    static int Main(string[] args)
    {
        try
        {
            bool mono = Type.GetType("Mono.Runtime") != null;
            float boundary = float.Parse("0.41239998", CultureInfo.InvariantCulture);
            int command = (int)(boundary * 10000);
            Check(command == (mono ? 4123 : 4124), "runtime conversion contract changed");
            foreach (string state in new[] { "listening", "reading", "queued" })
            {
                string name = "AOSR-runtime-" + Guid.NewGuid().ToString("N");
                using (var server = new ControlServer(name))
                using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut))
                {
                    Thread.Sleep(50);
                    if (state != "listening")
                    {
                        client.Connect(2000);
                        if (state == "queued")
                        {
                            var bytes = System.Text.Encoding.UTF8.GetBytes("START\n");
                            client.Write(bytes, 0, bytes.Length); client.Flush();
                        }
                    }
                    Thread.Sleep(50);
                    var elapsed = Stopwatch.StartNew(); server.Dispose();
                    Check(!server.IsAlive && elapsed.ElapsedMilliseconds < 1000, "IPC shutdown hung: " + state);
                    server.Pump(_ => throw new Exception("Cancelled command executed during shutdown"));
                }
            }
            int rows = 0;
            if (args.Length == 1)
            {
                Check(mono, "Real Mono command audit must run under the game's runtime");
                foreach (string line in File.ReadAllLines(Path.Combine(args[0], "forces.csv")))
                {
                    if (line.StartsWith("time_s,")) continue;
                    string[] cells = line.Split(','); float output = float.Parse(cells[10], CultureInfo.InvariantCulture);
                    int recorded = int.Parse(cells[11], CultureInfo.InvariantCulture);
                    Check((int)(output * 10000) == recorded, "Mono device conversion mismatch at row " + rows);
                    rows++;
                }
            }
            Console.WriteLine("{\"status\":\"passed\",\"assertions\":" + assertions + ",\"runtime\":\"" + (mono ? "Unity Mono" : "CLR") + "\",\"boundaryCommand\":" + command + ",\"recordedRows\":" + rows + ",\"hardwareOutput\":false}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
