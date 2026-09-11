using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Dbce.Wheel.Ffb;

// Consumes a validated recording but never opens the native driver. Event
// amplitude/direction below are experiments, not inferred physical wheel torque.
internal static class CapturedLanding
{
    public static int Run(string capture, string output)
    {
        capture = Path.GetFullPath(capture); output = Path.GetFullPath(output);
        if (Directory.Exists(output)) throw new IOException("Use a new study output directory.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "ArtOfSimRally.sln"))) root = root.Parent;
        if (root == null) throw new IOException("Cannot locate the consumer replay runner.");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true };
        start.ArgumentList.Add(Path.Combine(root.FullName,"tools/testing/Replay/bin/Release/net8.0/Replay.dll"));
        start.ArgumentList.Add("--replay"); start.ArgumentList.Add(capture);
        using var replay = Process.Start(start)!;
        var stdout = replay.StandardOutput.ReadToEndAsync(); var stderr = replay.StandardError.ReadToEndAsync();
        if (!replay.WaitForExit(30000)) { replay.Kill(); throw new IOException("Replay timed out."); }
        if (replay.ExitCode != 0) throw new IOException("Capture did not pass replay: " + stderr.Result);
        using var document = JsonDocument.Parse(stdout.Result);
        var detail = document.RootElement.GetProperty("detail");
        var landings = detail.GetProperty("signals").GetProperty("events").EnumerateArray()
            .Where(e => e.GetProperty("kind").GetString() == "landing-candidate").ToArray();
        if (landings.Length == 0) throw new IOException("No confirmed-contact landing candidates in this recording.");
        string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        string forcesPath = Path.Combine(capture,"forces.csv");
        string forceHash = Hash(forcesPath);
        var manifest = System.Xml.Linq.XDocument.Load(Path.Combine(capture,"manifest.xml")).Root!;
        if (forceHash != manifest.Element("forces")!.Value) throw new IOException("Capture changed after replay.");
        float[][] forces = File.ReadLines(forcesPath).Skip(1)
            .Select(l => l.Split(',').Select(s => float.Parse(s,CultureInfo.InvariantCulture)).ToArray()).ToArray();
        var csv = new List<string> { "landing_row,relative_seconds,steering_gain,impact_gain,direction,recorded_force,impact_only,candidate_force,clipped" };
        var trials = new List<object>(); int assertions = 0;
        void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
        foreach (var landing in landings)
        {
            int first = landing.GetProperty("forceRow").GetInt32();
            double contact = forces[first][0];
            foreach (float steeringGain in new[] { 1f, .5f })
            foreach (float impactGain in new[] { 0f, .05f, .1f })
            foreach (float direction in new[] { -1f, 1f })
            {
                // Zero structural input avoids ImpactMixer's permanent steering
                // headroom reduction. Add its bounded event output separately;
                // measure any clipping instead of silently promising headroom.
                var mixer = new ImpactMixer { Headroom=impactGain };
                Check(mixer.Trigger(1, direction, contact), "event rejected");
                float peakImpact=0, peakOutput=0; int clipped=0, rows=0;
                for (int row=first; row<forces.Length && forces[row][0]-contact<=.4; row++)
                {
                    float baseline=forces[row][10]*steeringGain;
                    float impact=mixer.Mix(0,forces[row][0]);
                    float unbounded=baseline+impact, candidate=Math.Clamp(unbounded,-1,1);
                    bool clip=unbounded!=candidate; if (clip) clipped++;
                    Check(float.IsFinite(candidate) && Math.Abs(impact)<=impactGain+.000001f, "event budget exceeded");
                    if (impactGain==0) Check(candidate==baseline,"disabled effects changed steering");
                    peakImpact=Math.Max(peakImpact,Math.Abs(impact)); peakOutput=Math.Max(peakOutput,Math.Abs(candidate)); rows++;
                    csv.Add(string.Join(",", new double[] {first,forces[row][0]-contact,steeringGain,impactGain,direction,forces[row][10],impact,candidate,clip?1:0}
                        .Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
                }
                Check(mixer.Mix(0,contact+1)==0,"expired event persisted");
                trials.Add(new { firstContactRow=first, steeringGain, impactGain, direction, rows, peakImpact, peakOutput, clippedSamples=clipped });
            }
        }
        Directory.CreateDirectory(output);
        File.WriteAllLines(Path.Combine(output,"landing-envelopes.csv"),csv);
        File.WriteAllText(Path.Combine(output,"replay.json"),stdout.Result);
        var report = new { status="passed", assertions, recordedLandingCandidates=landings.Length,
            forceLibrarySha256=Hash(typeof(ImpactMixer).Assembly.Location), captureManifestSha256=Hash(Path.Combine(capture,"manifest.xml")),
            forcesSha256=forceHash, csvSha256=Hash(Path.Combine(output,"landing-envelopes.csv")), trials,
            hardwareOutput=false, productionTuneChanged=false,
            limits="Fixed unit event amplitude and both hypothetical torque directions. Gains are study inputs, not recommended tuning. Vertical motion does not determine steering torque direction; no crash detector or calibrated impact strength is inferred." };
        File.WriteAllText(Path.Combine(output,"report.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
        Console.WriteLine(JsonSerializer.Serialize(report)); return 0;
    }
}
