using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Xml.Linq;
using ArtOfSimRally.Testing;

static class Program
{
    static int assertions;
    static void Check(bool ok, string reason) { assertions++; if (!ok) throw new Exception(reason); }
    static XElement Identity() => new XElement("capture", new XAttribute("origin", "synthetic"), new XElement("modSha256", "synthetic-fixture"));
    static void Samples(CaptureSession session)
    {
        for (int i = 0; i < 4; i++) session.Frame(new FrameSample { Number = 100 + i, Time = i + 1, Delta = .016f, Driving = 1, Direct = 1, Steer = .2f, Throttle = .5f, Brake = 0, Clutch = 0, Handbrake = 0 });
        float previous = 0;
        for (int i = 0; i < 4; i++)
        {
            if (i == 2) { session.Reset(); previous = 0; }
            float fy = i % 2 == 0 ? 8000 : -1500;
            float output = LegacyForceCurve.Evaluate(fy, 8, 8.5f, 40, 11500, .3f, false, .5f, previous);
            session.Force(new ForceSample { Time = i + 1, Fy = fy, Slip = 8, Ideal = 8.5f, Speed = 40, Reference = 11500, Gain = .3f, Smoothing = .5f, Previous = previous, Output = output, Device = (int)(output * 10000), Invert = 0,
                Motion = new MotionSample { Valid = 1, PhysicsTime = i * .02f, ContactMask = 15, Qx = 0, Qy = 0, Qz = 0, Qw = 1,
                    Px = 0, Py = 0, Pz = i * .2f, Vx = 0, Vy = 0, Vz = 10, LocalVx = 0, LocalVy = 0, LocalVz = 10,
                    CompressionFL = .1f, CompressionFR = .12f, CompressionRL = .08f, CompressionRR = .15f, TravelFL = .2f, TravelFR = .2f, TravelRL = .25f, TravelRR = .25f } });
            session.Delivery(true); previous = output;
        }
    }
    static string Capture(string root)
    {
        var session = new CaptureSession();
        Check(!session.Start(true, Identity(), root), "START accepted while driving");
        Check(session.Start(false, Identity(), root), "START failed");
        Check(!Directory.Exists(root), "START wrote to disk");
        Check(!session.Start(false, Identity(), root), "START overwrote pending capture");
        Samples(session);
        Check(!Directory.Exists(root), "sampling wrote to disk");
        Check(!session.Stop(true) && session.Active, "STOP accepted while driving");
        Check(session.Stop(false) && !session.Pending && !session.Active, "STOP failed");
        string saved = session.SavedDirectory;
        var receipt = XDocument.Load(Path.Combine(saved, "manifest.xml")).Root;
        Check((bool)receipt.Attribute("complete"), "complete capture rejected");
        Check((int)receipt.Attribute("schema") == 3, "motion schema not declared");
        foreach (string kind in new[] { "frames", "forces", "signals" })
        {
            Check((int)receipt.Element(kind).Attribute("count") == 4, "wrong count");
            Check(receipt.Element(kind).Value == ArtifactHash.FileHash(Path.Combine(saved, kind + ".csv")), "wrong hash");
        }
        var signalRows = File.ReadAllLines(Path.Combine(saved, "signals.csv")).Skip(1).Select(row => row.Split(',')).ToArray();
        Check(signalRows.All(row => row.Length == 27 && row[3] == "1" && row[5] == "15"), "signal layout/availability/contact lost");
        Check(signalRows[2][0] == "2" && signalRows[2][1] == "3" && signalRows[2][2] == "1", "motion did not follow force reset epoch");
        Check(signalRows[1][4] == "0.02" && signalRows[1][11] == "10" && signalRows[1][18] == "10" && signalRows[1][19] == "0.1" && signalRows[1][26] == "0.25", "signal values/units lost");
        Check(!session.Stop(false), "duplicate STOP rewrote evidence");
        Check(session.Start(false, Identity(), root, 1, 1), "second START failed");
        Samples(session); Check(session.Stop(false), "truncated save failed");
        Check(!(bool)XDocument.Load(Path.Combine(session.SavedDirectory, "manifest.xml")).Root.Attribute("complete"), "overflow silently complete");
        Check(session.Start(false, Identity(), root, 8, 8), "third START failed");
        Samples(session); session.AbortSampling("broken hook");
        Check(!session.Active && session.Pending, "probe failure lost buffers/kept sampling");
        Check(session.Stop(false), "aborted save failed");
        Check(!(bool)XDocument.Load(Path.Combine(session.SavedDirectory, "manifest.xml")).Root.Attribute("complete"), "broken hook silently complete");
        string blocker = Path.Combine(root, "blocked"); File.WriteAllText(blocker, "keep");
        Check(session.Start(false, Identity(), blocker, 8, 8), "retry setup failed"); Samples(session);
        Check(!session.Stop(false) && session.Pending && !session.Active, "failed write lost buffers");
        File.Move(blocker, blocker + ".old");
        Check(session.Stop(false) && !session.Pending, "retry failed");
        Check(File.ReadAllText(blocker + ".old") == "keep", "writer damaged existing file");
        Check(session.Start(false, Identity(), root, 1, 10001), "allocation setup failed");
        session.Force(default); // JIT/warmup before allocation measurement.
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) session.Force(new ForceSample { Motion = new MotionSample { Valid = 1, Qw = 1 } });
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "motion sampling allocated managed objects");
        Check(session.Stop(false), "allocation capture save failed");
        return saved;
    }
    static void Pipe()
    {
        string name = "AOSR-test-" + Guid.NewGuid().ToString("N");
        using var server = new ControlServer(name);
        foreach (string command in new[] { "START", "STATUS", "STOP", "INVALID" })
        {
            var client = Task.Run(() =>
            {
                using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
                pipe.Connect(3000);
                using var writer = new StreamWriter(pipe, new System.Text.UTF8Encoding(false), 1024, true) { AutoFlush = true };
                using var reader = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 1024, true);
                writer.WriteLine(command);
                return reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(7)).GetAwaiter().GetResult();
            });
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (!client.IsCompleted && timer.Elapsed.TotalSeconds < 8) { server.Pump(c => "OK " + c); Thread.Sleep(5); }
            Check(client.IsCompleted, "pipe client hung");
            Check(client.GetAwaiter().GetResult() == (command == "INVALID" ? "ERROR unknown command" : "OK " + command), "pipe command/reply mismatch");
        }
    }
    static void Contract(string modPath, string gameDir)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(modPath));
        var paths = new[] { root, Path.Combine(gameDir, "artofrally_Data", "Managed"), Path.GetFullPath("lib/umm"), Path.GetFullPath("lib/toolkit/dotnet") };
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (string directory in paths)
            {
                string candidate = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(candidate)) return context.LoadFromAssemblyPath(candidate);
            }
            return null;
        };
        var mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(modPath));
        var force = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath("lib/toolkit/dotnet/Dbce.Wheel.Ffb.dll"));
        var access = new SubjectAccess(mod, force); // Compiles every getter without invoking Unity/native APIs.
        Check(access.Drive.GetParameters().Single().ParameterType.Name == "CarDynamics", "physics signature changed");
        Check(access.Send.ReturnType == typeof(bool) && access.Send.GetParameters().Single().ParameterType == typeof(int), "native observation signature changed");
        Check(!access.Update.IsStatic && access.Shutdown.IsStatic && access.Reset.IsStatic, "lifecycle signatures changed");
        Check(access.Smoothed() == 0, "unexpected filter initialization");
        var main = mod.GetType("ArtOfSimRally.Mod.Main", true);
        var cfg = Activator.CreateInstance(mod.GetType("ArtOfSimRally.Mod.Settings", true));
        var settings = main.GetProperty("Settings", BindingFlags.NonPublic | BindingFlags.Static);
        settings.SetValue(null, cfg);
        var tune = access.ReadTune();
        Check(tune.Reference == 11500 && tune.Smoothing == .2f && tune.Gain == 1 && !tune.Invert, "probe reads wrong settings");
    }
    static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            Check(!Directory.Exists(root), "use a fresh test evidence directory");
            string capture = Capture(root); Pipe();
            if (args.Length == 3) Contract(args[1], args[2]);
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions, syntheticCapture = capture })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
