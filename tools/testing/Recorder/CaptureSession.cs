using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace ArtOfSimRally.Testing
{
    internal struct FrameSample
    {
        public float Time, Delta, Steer, Throttle, Brake, Clutch, Handbrake;
        public int Number, Driving, Direct;
    }
    internal struct ForceSample
    {
        public float Time, Fy, Slip, Ideal, Speed, Reference, Gain, Smoothing, Previous, Output;
        public int Invert, Device, Epoch;
    }

    // This assembly is a developer probe, never a dependency of the shipped mod.
    internal sealed class CaptureSession
    {
        private CaptureBuffer<FrameSample> frames;
        private CaptureBuffer<ForceSample> forces;
        private XElement metadata;
        private string directory;
        private int epoch, lastEpoch;
        private float lastOutput;
        private string failure;
        private int deliveryAttempts, deliveryFailures;
        public bool Active { get; private set; }
        public bool Pending => frames != null;
        public string Status { get; private set; } = "Idle";
        public string SavedDirectory { get; private set; }
        public string Describe => Status + "; frames=" + (frames?.Count ?? 0) + "; forces=" + (forces?.Count ?? 0) +
            "; incomplete=" + (failure != null || (frames?.Truncated ?? false) || (forces?.Truncated ?? false));

        public bool Start(bool driving, XElement identity, string root, int frameCapacity = 250000, int forceCapacity = 90000)
        {
            if (Pending || driving) { Status = "Pause before starting; finish any pending capture first."; return false; }
            directory = Path.Combine(Path.GetFullPath(root), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            metadata = new XElement(identity);
            metadata.SetAttributeValue("schema", 2);
            metadata.SetAttributeValue("startedUtc", DateTime.UtcNow.ToString("O"));
            frames = new CaptureBuffer<FrameSample>(frameCapacity);
            forces = new CaptureBuffer<ForceSample>(forceCapacity);
            epoch = lastEpoch = deliveryAttempts = deliveryFailures = 0; lastOutput = 0; failure = null;
            SavedDirectory = null; Active = true; Status = "Recording"; return true;
        }
        public void Frame(FrameSample sample) { if (Active) frames.Add(sample); }
        public void Reset() { if (Active) epoch++; }
        public void Incomplete(string reason) { if (Active) failure = reason; }
        // A broken observation must never throw into the game's physics loop.
        // Keep the incomplete buffers for an explicit save after pausing.
        public void AbortSampling(string reason) { failure = reason; Active = false; Status = "Capture interrupted: " + reason; }
        public void Delivery(bool accepted) { if (Active) { deliveryAttempts++; if (!accepted) deliveryFailures++; } }
        public void Force(ForceSample sample)
        {
            if (!Active) return;
            // A zero reset may be inlined by Mono. Observe the history as well as
            // explicit Reset calls; unexpected nonzero discontinuities stay invalid.
            if (forces.Count > 0 && epoch == lastEpoch && sample.Previous != lastOutput)
            {
                if (sample.Previous == 0) epoch++;
                else failure = "Nonzero filter history discontinuity";
            }
            sample.Epoch = epoch; forces.Add(sample); lastOutput = sample.Output; lastEpoch = epoch;
        }
        public bool Stop(bool driving)
        {
            if (!Pending || driving) { Status = "Pause before stopping the recording."; return false; }
            Active = false;
            try
            {
                if (Directory.Exists(directory)) directory += "-retry-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(directory);
                var framePath = Path.Combine(directory, "frames.csv");
                var forcePath = Path.Combine(directory, "forces.csv");
                using (var writer = new StreamWriter(framePath))
                {
                    writer.WriteLine("frame,time_s,delta_s,driving,direct_input,steer,throttle,brake,clutch,handbrake");
                    for (int i = 0; i < frames.Count; i++)
                    {
                        var f = frames.Items[i];
                        writer.WriteLine(string.Join(",", f.Number, F(f.Time), F(f.Delta), f.Driving, f.Direct, F(f.Steer), F(f.Throttle), F(f.Brake), F(f.Clutch), F(f.Handbrake)));
                    }
                }
                using (var writer = new StreamWriter(forcePath))
                {
                    writer.WriteLine("time_s,fy_n,slip_deg,ideal_deg,speed_kmh,reference_n,gain,invert,smoothing,previous,output,device,epoch");
                    for (int i = 0; i < forces.Count; i++)
                    {
                        var f = forces.Items[i];
                        writer.WriteLine(string.Join(",", F(f.Time), F(f.Fy), F(f.Slip), F(f.Ideal), F(f.Speed), F(f.Reference), F(f.Gain), f.Invert, F(f.Smoothing), F(f.Previous), F(f.Output), f.Device, f.Epoch));
                    }
                }
                var receipt = new XElement(metadata);
                receipt.SetAttributeValue("complete", !frames.Truncated && !forces.Truncated && failure == null);
                receipt.SetAttributeValue("endedUtc", DateTime.UtcNow.ToString("O"));
                receipt.Add(new XElement("frames", new XAttribute("count", frames.Count), ArtifactHash.FileHash(framePath)),
                    new XElement("forces", new XAttribute("count", forces.Count), ArtifactHash.FileHash(forcePath)),
                    new XElement("delivery", new XAttribute("attempts", deliveryAttempts), new XAttribute("rejected", deliveryFailures)),
                    new XElement("error", failure ?? ""));
                new XDocument(receipt).Save(Path.Combine(directory, "manifest.xml"));
                SavedDirectory = directory; Status = "Saved: " + directory; frames = null; forces = null; return true;
            }
            catch (Exception ex) { Status = "Save failed; retry while paused: " + ex.Message; return false; }
        }
        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
