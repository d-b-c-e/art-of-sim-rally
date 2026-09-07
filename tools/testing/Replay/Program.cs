using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ArtOfSimRally.Mod;
using ArtOfSimRally.Testing;
using Dbce.Wheel.Ffb;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    static void Same(float actual, float expected, string message)
        => Check(float.IsFinite(actual) && Math.Abs(actual - expected) <= .000001f, message + $": {actual:R} != {expected:R}");
    static object Replay(string directory)
    {
        var manifest = XDocument.Load(Path.Combine(directory, "manifest.xml")).Root!;
        int schema = (int?)manifest.Attribute("schema") ?? 0;
        Check(schema == 1 || schema == 2, "unknown capture schema");
        Check((string?)manifest.Attribute("complete") == "true", "truncated/incomplete capture");
        Check(!string.IsNullOrWhiteSpace((string?)manifest.Element("modSha256")), "missing build identity");
        var frames = File.ReadAllLines(Path.Combine(directory, "frames.csv"));
        var forces = File.ReadAllLines(Path.Combine(directory, "forces.csv"));
        foreach (string kind in new[] { "frames", "forces" })
        {
            var element = manifest.Element(kind)!;
            Check(ArtifactHash.FileHash(Path.Combine(directory, kind + ".csv")) == element.Value, kind + " hash mismatch");
            int count = kind == "frames" ? frames.Length - 1 : forces.Length - 1;
            Check(count > 0 && count == (int?)element.Attribute("count"), kind + " count mismatch/empty");
        }
        Check(frames[0] == "frame,time_s,delta_s,driving,direct_input,steer,throttle,brake,clutch,handbrake", "frame schema mismatch");
        Check(forces[0] == "time_s,fy_n,slip_deg,ideal_deg,speed_kmh,reference_n,gain,invert,smoothing,previous,output,device" + (schema == 2 ? ",epoch" : ""), "force schema mismatch");
        float time = -1, baselineState = 0, toolkitState = 0, maxFloatDelta = 0;
        int epoch = -1, resets = 0;
        foreach (string line in forces.Skip(1))
        {
            var p = line.Split(',').Select(F).ToArray();
            Check(p.Length == (schema == 2 ? 13 : 12) && p.All(float.IsFinite), "invalid force row");
            Check(p[0] >= 0 && p[0] >= time && (p[7] == 0 || p[7] == 1), "invalid force time/inversion"); time = p[0];
            Check(Math.Abs(p[9]) <= 1 && Math.Abs(p[10]) <= 1, "force history/output outside normalized range");
            if (schema == 1 || epoch < 0) { baselineState = toolkitState = p[9]; }
            if (schema == 2)
            {
                Check(p[12] >= 0 && p[12] == (int)p[12] && p[12] >= epoch, "invalid reset epoch");
                if (epoch >= 0 && p[12] != epoch) { baselineState = toolkitState = 0; resets++; }
                epoch = (int)p[12];
                Same(toolkitState, p[9], "filter history is discontinuous");
            }
            baselineState = LegacyForceCurve.Evaluate(p[1], p[2], p[3], p[4], p[5], p[6], p[7] == 1, p[8], baselineState);
            toolkitState = ForceCurve.Smooth(toolkitState, ForceCurve.Normalised(p[1], p[2], p[3], p[4], p[5], p[6], p[7] == 1), p[8]);
            maxFloatDelta = Math.Max(maxFloatDelta, Math.Abs(toolkitState - baselineState));
            Same(toolkitState, baselineState, "before/after force comparison");
            Same(toolkitState, p[10], "offline force replay");
            Check((int)(toolkitState * 10000) == (int)(baselineState * 10000), "adoption changed device magnitude");
            Check((int)(toolkitState * 10000) == p[11], "offline device magnitude mismatch");
        }
        var timings = new List<float>(); var first15 = new List<float>(); var later = new List<float>();
        int previousFrame = -1; float previousTime = -1, segmentStart = -1; bool wasDriving = false;
        foreach (string line in frames.Skip(1))
        {
            var p = line.Split(',').Select(F).ToArray();
            Check(p.Length == 10 && p.All(float.IsFinite) && p[2] > 0, "invalid frame row");
            Check(p[0] >= 0 && p[0] == (int)p[0] && p[1] >= 0, "invalid frame number/time");
            Check((previousFrame == -1 || p[0] == previousFrame + 1) && p[1] >= previousTime, "missing/nonmonotonic frames");
            Check((p[3] == 0 || p[3] == 1) && (p[4] == 0 || p[4] == 1), "invalid frame flags");
            previousFrame = (int)p[0]; previousTime = p[1];
            if (p[3] == 1)
            {
                if (!wasDriving) segmentStart = p[1];
                float milliseconds = p[2] * 1000;
                timings.Add(milliseconds);
                (p[1] - segmentStart <= 15 ? first15 : later).Add(milliseconds);
            }
            wasDriving = p[3] == 1;
        }
        Check(timings.Count >= 2, "capture contains no drive");
        var delivery = manifest.Element("delivery");
        int? attempted = (int?)delivery?.Attribute("attempts"), rejected = (int?)delivery?.Attribute("rejected");
        if (delivery != null) Check(attempted >= forces.Length - 1 && rejected >= 0 && rejected <= attempted, "invalid delivery counters");
        timings.Sort();
        return new
        {
            pipeline = "AxleForceCurve@" + AxleForceCurve.CompatibilityVersion,
            candidateForceLibrarySha256 = ArtifactHash.FileHash(typeof(AxleForceCurve).Assembly.Location),
            captureBuild = (string?)manifest.Element("build"),
            maxFloatDelta,
            deviceMismatches = 0,
            resetBoundaries = resets,
            stateful = schema == 2,
            forceRows = forces.Length - 1,
            drivingFrames = timings.Count,
            p95FrameMs = timings[(int)((timings.Count - 1) * .95)],
            maxFrameMs = timings[^1],
            first15Seconds = new { frames = first15.Count, maxFrameMs = first15.Count == 0 ? 0 : first15.Max(), hitchesOver100Ms = first15.Count(t => t > 100) },
            after15Seconds = new { frames = later.Count, maxFrameMs = later.Count == 0 ? 0 : later.Max(), hitchesOver100Ms = later.Count(t => t > 100) },
            hitchesOver100Ms = timings.Count(t => t > 100),
            nativeDelivery = new { attempted, rejected, physicalTorqueVerified = false },
            scope = "force arithmetic and timing evidence; no game or hardware playback"
        };
    }


    static int Main(string[] args)
    {
        try
        {
            object detail;
            if (args.Length == 2 && args[0] == "--replay") detail = Replay(Path.GetFullPath(args[1]));
            else if (args.Length == 2 && args[0] == "--verify-release") detail = VerifyRelease(args[1]);
            else if (args.Length == 2 && args[0] == "--corpus") detail = Corpus(args[1]);
            else throw new ArgumentException("Use --replay <capture>, --corpus <index.json>, or --verify-release <mod.dll>");
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions, detail })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static object VerifyRelease(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            string name = metadata.GetString(type.Name), space = metadata.GetString(type.Namespace);
            Check(!space.StartsWith("ArtOfSimRally.Testing") && name != "DriveCapture" && name != "CaptureBuffer`1", "Recorder type shipped: " + space + "." + name);
        }
        foreach (var handle in metadata.AssemblyReferences)
            Check(!metadata.GetString(metadata.GetAssemblyReference(handle).Name).Contains("Recorder"), "Recorder assembly dependency shipped");
        return new { recordingFeatureAbsent = true };
    }

    static object Corpus(string indexPath)
    {
        indexPath = Path.GetFullPath(indexPath);
        using var json = JsonDocument.Parse(File.ReadAllText(indexPath));
        var root = json.RootElement;
        Check(root.GetProperty("schema").GetInt32() == 1, "Unknown corpus schema");
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Check(cases.Length > 0, "Recorded corpus is empty; capture and promote at least one drive");
        var ids = new HashSet<string>(); var reports = new List<object>();
        foreach (var item in cases)
        {
            string id = item.GetProperty("id").GetString()!;
            Check(!string.IsNullOrWhiteSpace(id) && ids.Add(id), "Empty/duplicate corpus case " + id);
            string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(indexPath)!, item.GetProperty("path").GetString()!));
            Check(ArtifactHash.FileHash(Path.Combine(path, "manifest.xml")) == item.GetProperty("manifestSha256").GetString(), "Corpus receipt changed: " + id);
            var receipt = XDocument.Load(Path.Combine(path, "manifest.xml")).Root!;
            Check((string?)receipt.Attribute("origin") == "game", "Corpus case is not a recorded game session: " + id);
            Check((int?)receipt.Attribute("schema") == 2, "Corpus requires continuous schema-2 recordings: " + id);
            reports.Add(new { id, result = Replay(path) });
        }
        return new { cases = reports, caseCount = reports.Count };
    }
}
