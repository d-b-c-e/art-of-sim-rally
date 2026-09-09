using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using ArtOfSimRally.Testing;

// Offline descriptive measurements only. No Unity, input injection or native output.
internal static class SignalAnalysis
{
    const string Header = "force_row,time_s,epoch,valid,physics_time_s,contact_mask,px_m,py_m,pz_m,vx_mps,vy_mps,vz_mps,qx,qy,qz,qw,local_vx_mps,local_vy_mps,local_vz_mps,compression_fl_m,compression_fr_m,compression_rl_m,compression_rr_m,travel_fl_m,travel_fr_m,travel_rl_m,travel_rr_m";
    const int MaximumEvents = 128;
    sealed record Row(float[] Values, float[] Force)
    {
        public float Time => Values[4];
        public int Epoch => (int)Values[2];
        public int Mask => (int)Values[5];
        public Vector3 Position => new(Values[6], Values[7], Values[8]);
        public Vector3 Velocity => new(Values[9], Values[10], Values[11]);
        public Quaternion Rotation => new(Values[12], Values[13], Values[14], Values[15]);
    }
    static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidDataException("signals: " + message);
    }
    public static object Read(string directory, XElement manifest, List<float[]> forces)
    {
        string path = Path.Combine(directory, "signals.csv");
        var receipt = manifest.Element("signals");
        Require(receipt != null, "missing receipt");
        Require(new FileInfo(path).Length <= 64 * 1024 * 1024, "file exceeds bounded capture size");
        Require(ArtifactHash.FileHash(path) == receipt!.Value, "hash mismatch");
        Require((int?)receipt.Attribute("count") == forces.Count, "count mismatch");
        var events = new List<object>();
        int rows = 0, unavailable = 0, boundaries = 0, accelerationRows = 0, landings = 0, recoveries = 0, roadPairs = 0;
        double roadSquared = 0, maxLocalVertical = 0;
        Row? previous = null, firstContact = null;
        float? airborneStart = null, highSlipStart = null;
        float airborneDuration = 0;
        Vector3? contactAcceleration = null;
        void Clear()
        {
            previous = firstContact = null; airborneStart = highSlipStart = null; contactAcceleration = null;
        }
        void Event(string kind, Row row, float duration, Vector3? acceleration)
        {
            if (events.Count == MaximumEvents) return;
            events.Add(new
            {
                kind, forceRow = (int)row.Values[0], timeSeconds = row.Values[1], physicsTimeSeconds = row.Time,
                epoch = row.Epoch, precedingSeconds = duration,
                contactMask = row.Mask, speedKmh = row.Force[4], slipDegrees = row.Force[2],
                previousForce = row.Force[9], force = row.Force[10], deviceMagnitude = (int)row.Force[11],
                localAccelerationMps2 = acceleration.HasValue ? new[] { acceleration.Value.X, acceleration.Value.Y, acceleration.Value.Z } : null,
                normalizedCompression = Enumerable.Range(0, 4).Select(w => row.Values[23 + w] > 0 ? (double?)((double)row.Values[19 + w] / row.Values[23 + w]) : null).ToArray()
            });
        }
        using var reader = new StreamReader(path);
        Require(reader.ReadLine() == Header, "header mismatch");
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            Require(rows < forces.Count, "extra row");
            var p = line.Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            Require(p.Length == 27 && p.All(float.IsFinite), "invalid/nonfinite row");
            var force = forces[rows];
            Require(p[0] == rows && p[1] == force[0] && p[2] == force[12], "force alignment mismatch");
            rows++;
            Require(p[3] == 0 || p[3] == 1, "invalid availability flag");
            if (p[3] == 0)
            {
                Require(p.Skip(4).All(v => v == 0), "unavailable row contains measurements");
                unavailable++; Clear(); continue;
            }
            Require(p[4] >= 0 && p[5] >= 0 && p[5] <= 15 && p[5] == (int)p[5], "invalid clock/contact mask");
            Require(p.Skip(6).Take(3).All(v => Math.Abs(v) <= 10000000) && p.Skip(9).Take(3).All(v => Math.Abs(v) <= 10000), "motion outside capture bounds");
            var current = new Row(p, force);
            Require(Math.Abs(current.Rotation.LengthSquared() - 1) <= .001f, "invalid rotation quaternion");
            var projected = Vector3.Transform(current.Velocity, Quaternion.Inverse(current.Rotation));
            var recorded = new Vector3(p[16], p[17], p[18]);
            Require(Vector3.Distance(projected, recorded) <= .002f + .0001f * projected.Length(), "local velocity projection mismatch");
            Vector3? acceleration = null;
            if (previous != null)
            {
                float dt = current.Time - previous.Time;
                bool continuous = current.Epoch == previous.Epoch && dt > .000001f && dt <= .10001f &&
                    Vector3.Distance(current.Position, previous.Position) <= 5 + Math.Max(current.Velocity.Length(), previous.Velocity.Length()) * dt;
                if (!continuous) { boundaries++; Clear(); }
                else
                {
                    // Differentiate world velocity, then project. Differentiating
                    // rotating local axes would invent acceleration while turning.
                    acceleration = Vector3.Transform((current.Velocity - previous.Velocity) / dt, Quaternion.Inverse(current.Rotation));
                    maxLocalVertical = Math.Max(maxLocalVertical, Math.Abs(acceleration.Value.Y)); accelerationRows++;
                    if (current.Mask == 15 && previous.Mask == 15)
                        for (int w = 0; w < 4; w++)
                            if (p[23 + w] > 0 && previous.Values[23 + w] > 0)
                            {
                                double delta = (double)p[19 + w] / p[23 + w] - (double)previous.Values[19 + w] / previous.Values[23 + w];
                                roadSquared += delta * delta; roadPairs++;
                            }
                }
            }
            if (current.Mask == 0)
            {
                airborneStart ??= current.Time; firstContact = null; contactAcceleration = null;
            }
            else if (airborneStart.HasValue)
            {
                if (firstContact == null)
                {
                    firstContact = current; airborneDuration = current.Time - airborneStart.Value; contactAcceleration = acceleration;
                }
                if (current.Time - firstContact.Time >= .03999f)
                {
                    if (airborneDuration >= .07999f) { landings++; Event("landing-candidate", firstContact, airborneDuration, contactAcceleration); }
                    airborneStart = null; firstContact = null; contactAcceleration = null;
                }
            }
            if (current.Mask != 0 && force[4] >= 12 && force[3] > 0)
            {
                if (force[2] >= 2 * force[3]) highSlipStart ??= current.Time;
                else if (force[2] <= force[3] && highSlipStart.HasValue)
                {
                    recoveries++; Event("slide-recovery-candidate", current, current.Time - highSlipStart.Value, acceleration); highSlipStart = null;
                }
            }
            else highSlipStart = null;
            previous = current;
        }
        Require(rows == forces.Count, "missing row");
        return new
        {
            available = true, rows, unavailableRows = unavailable, discontinuities = boundaries, accelerationRows,
            maxAbsLocalVerticalAccelerationMps2 = accelerationRows == 0 ? (double?)null : maxLocalVertical,
            groundedCompressionDeltaRms = roadPairs == 0 ? (double?)null : Math.Sqrt(roadSquared / roadPairs), groundedWheelPairs = roadPairs,
            landingCandidates = landings, slideRecoveryCandidates = recoveries,
            omittedEvents = landings + recoveries - events.Count, events,
            scope = "observed context and descriptive candidates; no collision classification, calibrated impulse, effect tuning or hardware output"
        };
    }
}
