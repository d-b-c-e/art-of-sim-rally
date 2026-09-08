// Offline consumer design experiment. Calls only managed ImpactMixer/SignalSample;
// no Unity, native loader, device output, collision detector or shipping tune.
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Dbce.Wheel.Ffb;
using Dbce.Wheel.Telemetry;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .000001f, message);
    static SignalSample Sample(double value, double seconds) => new SignalSample {
        Value=value, Seconds=seconds, Frame=1, Quality=SignalQuality.Fresh,
        Source=SignalSource.Derived, Unit=SignalUnit.NormalizedForce };

    // Study policy for one-shot events, stricter than a general telemetry sample:
    // a held estimate must not repeatedly trigger, and units must match the use.
    static bool FreshEvent(SignalSample sample, double now) => sample.Usable(now, .1) &&
        sample.Quality == SignalQuality.Fresh && sample.Unit == SignalUnit.NormalizedForce;

    static int Main(string[] args)
    {
        try
        {
            string directory = Path.GetFullPath(args.Length == 1 ? args[0] : Path.Combine("results", "effects-study-" + Guid.NewGuid().ToString("N")));
            if (Directory.Exists(directory)) throw new IOException("Use a new output directory; preserve previous evidence.");
            Directory.CreateDirectory(directory);
            var mixer = new ImpactMixer { Headroom=.25f };
            float oldSteering=.8f, withReserve=mixer.Mix(oldSteering, 0);
            Near(withReserve, .6f, "headroom no longer attenuates steering; re-evaluate design");
            // Independent steering gain preserves the event contribution. Applying
            // Strength to the entire mixer would lower both and miss the request.
            mixer.Reset(); mixer.Trigger(1, 1, 0);
            float light=mixer.Mix(.2f, .005);
            mixer.Reset(); mixer.Trigger(1, 1, 0);
            float heavy=mixer.Mix(.8f, .005);
            Near(light, .4f, "light steering mix"); Near(heavy, .85f, "heavy steering mix");
            Near(light-.2f*.75f, heavy-.8f*.75f, "steering gain changed event gain");
            mixer.Reset(); mixer.Headroom=0;
            Near(mixer.Mix(oldSteering, 0), oldSteering, "disabled effects alter baseline");

            var rows = new List<string> { "synthetic,headroom,steering,event_direction,seconds,output" };
            float peak=0;
            foreach (float headroom in new[] { 0f, .15f, .25f, .5f })
            foreach (float steering in new[] { -1f, -.8f, 0f, .2f, .8f, 1f })
            foreach (float direction in new[] { -1f, 1f })
            {
                mixer = new ImpactMixer { Headroom=headroom };
                for (int i=0; i<8; i++) Check(mixer.Trigger(float.MaxValue, direction, 0), "bounded burst refused early");
                Check(!mixer.Trigger(1, direction, 0), "burst beyond queue capacity accepted");
                for (int i=0; i<=100; i++)
                {
                    double t=i/1000.0; float output=mixer.Mix(steering, t);
                    Check(float.IsFinite(output) && Math.Abs(output)<=1, "burst exceeded normalized budget");
                    Check(Math.Abs(output-steering*(1-headroom))<=headroom+.000001f, "event exceeded independent reserve");
                    peak=Math.Max(peak,Math.Abs(output));
                    rows.Add(string.Join(",", "true", headroom.ToString("R",CultureInfo.InvariantCulture),
                        steering.ToString("R",CultureInfo.InvariantCulture), direction.ToString("R",CultureInfo.InvariantCulture),
                        t.ToString("R",CultureInfo.InvariantCulture), output.ToString("R",CultureInfo.InvariantCulture)));
                }
                Near(mixer.Mix(steering, .101), steering*(1-headroom), "expired impact remained active");
                mixer.Reset(); Near(mixer.Mix(0, .2), 0, "reset retained burst");
            }
            Check(FreshEvent(Sample(1,0),.05), "fresh normalized signal rejected");
            Check(!FreshEvent(Sample(1,0),.101), "stale signal accepted");
            Check(!FreshEvent(Sample(double.NaN,0),0), "NaN accepted");
            Check(!FreshEvent(Sample(double.PositiveInfinity,0),0), "infinity accepted");
            Check(!FreshEvent(Sample(1,1),0), "future sample accepted");
            var held=Sample(1,0); held.Quality=SignalQuality.Held;
            Check(held.Usable(.01,.1) && !FreshEvent(held,.01), "held telemetry must not re-trigger events");
            var wrongUnit=Sample(8000,0); wrongUnit.Unit=SignalUnit.Rpm;
            Check(wrongUnit.Usable(.01,.1) && !FreshEvent(wrongUnit,.01), "generic usable flag is not a unit check");

            mixer = new ImpactMixer { Headroom=.25f }; mixer.Trigger(1,1,0);
            Check(mixer.Mix(0,.005)>0,"dropout scenario had no active effect");
            if (!FreshEvent(Sample(1,0),.2)) mixer.Reset();
            Near(mixer.Mix(0,.2),0,"dropout left an event active");
            mixer.Trigger(1,1,.3); Near(mixer.Mix(0,.1),0,"clock rollback did not clear events");
            Near(mixer.Mix(0,.305),0,"rollback effect resurrected");
            mixer.Trigger(1,1,.4); Near(mixer.Mix(float.NaN,.405),0,"invalid structure was not zeroed");
            Near(mixer.Mix(0,.41),0,"invalid sample retained event history");

            string csv=Path.Combine(directory,"synthetic-effects.csv"); File.WriteAllLines(csv,rows);
            string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            var report = new { status="passed", assertions, syntheticOnly=true, hardwareOutput=false,
                productionTuneChanged=false, rows=rows.Count-1, peakNormalizedOutput=peak,
                headroomObservation=new { steering=oldSteering, headroom=.25f, outputWithoutEvent=withReserve },
                independentGains=new { light, heavy, eventContribution=.25f },
                ffbLibrarySha256=Hash(typeof(ImpactMixer).Assembly.Location),
                telemetryLibrarySha256=Hash(typeof(SignalSample).Assembly.Location), csvSha256=Hash(csv) };
            File.WriteAllText(Path.Combine(directory,"report.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
            Console.WriteLine(JsonSerializer.Serialize(report)); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
