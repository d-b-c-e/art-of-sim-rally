using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Opt-in evidence, not game/input injection. Update/FixedUpdate append value
    // types to bounded arrays: no formatting, growing collections or file IO.
    internal static class DriveCapture
    {
        private struct Frame
        {
            public float Time, Delta, Steer, Throttle, Brake, Clutch, Handbrake;
            public int Number, Driving, Direct;
        }
        private struct Force
        {
            public float Time, Fy, Slip, Ideal, Speed, Reference, Gain, Smoothing, Previous, Output;
            public int Invert, Device, Epoch;
        }
        private static CaptureBuffer<Frame> _frames;
        private static CaptureBuffer<Force> _forces;
        private static XElement _metadata;
        private static string _directory;
        private static int _forceEpoch;
        public static bool Active { get; private set; }
        public static bool Pending => _frames != null;
        public static string Status { get; private set; } = "Capture a drive for timing analysis and offline force replay.";

        internal static void Start(string outputRoot = null)
        {
            if (Pending || GameState.IsDriving) return;
            try
            {
                var root = outputRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ArtOfSimRally", "captures");
                _directory = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
                var assembly = Assembly.GetExecutingAssembly();
                var identity = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly,
                    typeof(AssemblyInformationalVersionAttribute));
                _forceEpoch = 0;
                _metadata = new XElement("capture", new XAttribute("schema", 2),
                    new XAttribute("startedUtc", DateTime.UtcNow.ToString("O")),
                    new XElement("build", identity?.InformationalVersion ?? "unknown"),
                    new XElement("modSha256", NativeDiagnostics.FileHash(assembly.Location)),
                    new XElement("forcePipeline", "AxleForceCurve@" + Dbce.Wheel.Ffb.AxleForceCurve.CompatibilityVersion),
                    new XElement("forceLibrarySha256", NativeDiagnostics.FileHash(typeof(Dbce.Wheel.Ffb.AxleForceCurve).Assembly.Location)),
                    new XElement("game", Application.version), new XElement("unity", Application.unityVersion),
                    new XElement("native", NativeDiagnostics.Describe("UnityForceFeedback.dll")));
                var settings = new XElement("settings");
                if (Main.Settings != null)
                    foreach (var field in typeof(Settings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                        settings.Add(new XElement("field", new XAttribute("name", field.Name),
                            Convert.ToString(field.GetValue(Main.Settings), CultureInfo.InvariantCulture)));
                _metadata.Add(settings);
                // About 14 MB, reserved only after the user requests a capture.
                _frames = new CaptureBuffer<Frame>(250000);
                _forces = new CaptureBuffer<Force>(90000);
                Active = true;
                Status = "Recording in memory. Pause after the drive, then stop and save.";
            }
            catch (Exception ex)
            {
                _frames = null; _forces = null; Active = false;
                Status = "Could not start capture: " + ex.Message;
            }
        }

        public static void RecordFrame(float delta, bool driving)
        {
            if (!Active) return;
            _frames.Add(new Frame { Time = Time.realtimeSinceStartup, Delta = delta, Number = Time.frameCount,
                Driving = driving ? 1 : 0, Direct = WheelInput.Enabled ? 1 : 0,
                Steer = WheelInput.Value(WheelInput.Channel.Steer), Throttle = WheelInput.Value(WheelInput.Channel.Throttle),
                Brake = WheelInput.Value(WheelInput.Channel.Brake), Clutch = WheelInput.Value(WheelInput.Channel.Clutch),
                Handbrake = WheelInput.Value(WheelInput.Channel.Handbrake) });
        }

        public static void RecordForce(float fy, float slip, float ideal, float speed, float previous, float output)
        {
            if (!Active) return;
            var cfg = Main.Settings;
            _forces.Add(new Force { Time = Time.realtimeSinceStartup, Fy = fy, Slip = slip, Ideal = ideal,
                Speed = speed, Reference = cfg.FyReference, Gain = cfg.GainFromStrength, Invert = cfg.Invert ? 1 : 0,
                Smoothing = cfg.Smoothing, Previous = previous, Output = output, Device = (int)(output * 10000), Epoch = _forceEpoch });
        }

        // Recorded resets let replay carry its own filter state across steps.
        public static void RecordForceReset() { if (Active) _forceEpoch++; }

        public static void Stop(string reason = "user")
        {
            if (!Pending || (reason != "shutdown" && GameState.IsDriving)) return;
            Active = false;
            try
            {
                // A failed save keeps the arrays for a retry, using a new directory
                // so neither an incomplete nor a completed case is overwritten.
                if (Directory.Exists(_directory)) _directory += "-retry-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(_directory);
                string framesPath = Path.Combine(_directory, "frames.csv");
                string forcesPath = Path.Combine(_directory, "forces.csv");
                using (var writer = new StreamWriter(framesPath))
                {
                    writer.WriteLine("frame,time_s,delta_s,driving,direct_input,steer,throttle,brake,clutch,handbrake");
                    for (int i = 0; i < _frames.Count; i++)
                    {
                        var f = _frames.Items[i];
                        writer.WriteLine(string.Join(",", f.Number, F(f.Time), F(f.Delta), f.Driving, f.Direct,
                            F(f.Steer), F(f.Throttle), F(f.Brake), F(f.Clutch), F(f.Handbrake)));
                    }
                }
                using (var writer = new StreamWriter(forcesPath))
                {
                    writer.WriteLine("time_s,fy_n,slip_deg,ideal_deg,speed_kmh,reference_n,gain,invert,smoothing,previous,output,device,epoch");
                    for (int i = 0; i < _forces.Count; i++)
                    {
                        var f = _forces.Items[i];
                        writer.WriteLine(string.Join(",", F(f.Time), F(f.Fy), F(f.Slip), F(f.Ideal), F(f.Speed),
                            F(f.Reference), F(f.Gain), f.Invert, F(f.Smoothing), F(f.Previous), F(f.Output), f.Device, f.Epoch));
                    }
                }
                var manifest = new XElement(_metadata);
                manifest.Add(new XAttribute("complete", !_frames.Truncated && !_forces.Truncated),
                    new XAttribute("stopReason", reason), new XAttribute("endedUtc", DateTime.UtcNow.ToString("O")),
                    new XElement("frames", new XAttribute("count", _frames.Count), NativeDiagnostics.FileHash(framesPath)),
                    new XElement("forces", new XAttribute("count", _forces.Count), NativeDiagnostics.FileHash(forcesPath)));
                // Completion receipt is written last; no receipt means incomplete.
                new XDocument(manifest).Save(Path.Combine(_directory, "manifest.xml"));
                Status = (_frames.Truncated || _forces.Truncated ? "TRUNCATED capture: " : "Saved capture: ") + _directory;
                ModLog.Info(Status);
                _frames = null; _forces = null;
            }
            catch (Exception ex) { Status = "Capture save failed; retry while paused: " + ex.Message; }
        }

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
