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
        public MotionSample Motion;
    }
    internal struct MotionSample
    {
        public int Valid, ContactMask;
        public float PhysicsTime, Px, Py, Pz, Vx, Vy, Vz, Qx, Qy, Qz, Qw, LocalVx, LocalVy, LocalVz;
        public float CompressionFL, CompressionFR, CompressionRL, CompressionRR;
        public float TravelFL, TravelFR, TravelRL, TravelRR;
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
            metadata.SetAttributeValue("schema", 3);
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
                var signalPath = Path.Combine(directory, "signals.csv");
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
                using (var writer = new StreamWriter(signalPath))
                {
                    writer.WriteLine("force_row,time_s,epoch,valid,physics_time_s,contact_mask,px_m,py_m,pz_m,vx_mps,vy_mps,vz_mps,qx,qy,qz,qw,local_vx_mps,local_vy_mps,local_vz_mps,compression_fl_m,compression_fr_m,compression_rl_m,compression_rr_m,travel_fl_m,travel_fr_m,travel_rl_m,travel_rr_m");
                    for (int i = 0; i < forces.Count; i++)
                    {
                        var f = forces.Items[i]; var s = f.Motion;
                        writer.WriteLine(string.Join(",", i, F(f.Time), f.Epoch, s.Valid, F(s.PhysicsTime), s.ContactMask,
                            F(s.Px), F(s.Py), F(s.Pz), F(s.Vx), F(s.Vy), F(s.Vz), F(s.Qx), F(s.Qy), F(s.Qz), F(s.Qw),
                            F(s.LocalVx), F(s.LocalVy), F(s.LocalVz), F(s.CompressionFL), F(s.CompressionFR), F(s.CompressionRL), F(s.CompressionRR),
                            F(s.TravelFL), F(s.TravelFR), F(s.TravelRL), F(s.TravelRR)));
                    }
                }
                var receipt = new XElement(metadata);
                receipt.SetAttributeValue("complete", !frames.Truncated && !forces.Truncated && failure == null);
                receipt.SetAttributeValue("endedUtc", DateTime.UtcNow.ToString("O"));
                receipt.Add(new XElement("frames", new XAttribute("count", frames.Count), ArtifactHash.FileHash(framePath)),
                    new XElement("forces", new XAttribute("count", forces.Count), ArtifactHash.FileHash(forcePath)),
                    new XElement("signals", new XAttribute("count", forces.Count), ArtifactHash.FileHash(signalPath)),
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
