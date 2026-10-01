using System.Globalization;
using System.Xml.Linq;
using ArtOfSimRally.Testing;

internal static class CollisionAnalysis
{
    static void Require(bool ok, string message)
    { if (!ok) throw new InvalidDataException("collisions: " + message); }

    public static object Read(string directory, XElement manifest, List<float[]> forces)
    {
        var receipt = manifest.Element("collisions");
        string path = Path.Combine(directory, "collisions.csv");
        Require(receipt != null && File.Exists(path), "missing file/receipt");
        Require(new FileInfo(path).Length <= 8 * 1024 * 1024, "file exceeds bound");
        Require(ArtifactHash.FileHash(path) == receipt!.Value, "hash mismatch");
        int? expected = (int?)receipt.Attribute("count");
        Require(expected >= 0 && expected <= CollisionFormat.Capacity, "invalid count");
        int count = 0, limited = 0;
        double lastTime = -1;
        var events = new List<object>();
        using var reader = new StreamReader(path);
        Require(reader.ReadLine() == CollisionFormat.Header, "header mismatch");
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            Require(count < expected, "extra row");
            var p = line.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            Require(p.Length == 36 && p.All(v => double.IsFinite(v) && Math.Abs(v) <= float.MaxValue), "invalid/nonfinite row");
            foreach (int column in new[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 })
                Require(p[column] >= int.MinValue && p[column] <= int.MaxValue && p[column] == Math.Truncate(p[column]), "invalid integer field");
            Require(p[0] == count && p[1] >= lastTime && p[1] >= 0 && p[2] >= 0 && p[3] >= 0, "invalid identity/time");
            Require(p[4] >= -1 && p[4] < forces.Count, "invalid preceding force row");
            if (p[4] >= 0) Require(forces[(int)p[4]][0] <= p[1] + .002, "preceding force is in the future");
            Require(p[7] >= 0 && p[7] <= 31 && (p[8] == 0 || p[8] == 1) && (p[9] == 0 || p[9] == 1), "invalid layer/tag flags");
            Require(p[10] >= 0 && p[11] == Math.Min(p[10], CollisionFormat.ContactLimit), "invalid contact counts");
            Require(p[11] == 0 ? p[12] == -1 : p[12] >= 0 && p[12] < p[11], "invalid contact selection");
            Require(p[19] > 0, "invalid mass");
            Require(Math.Abs(p.Skip(23).Take(4).Sum(v => v * v) - 1) <= .001, "invalid quaternion");
            double normal2 = p.Skip(30).Take(3).Sum(v => v * v);
            Require(p[11] == 0 ? p.Skip(30).All(v => v == 0) : Math.Abs(normal2 - 1) <= .001, "invalid contact normal/point");
            bool partial = p[10] > p[11];
            if (partial) limited++;
            if (events.Count < 128)
                events.Add(new
                {
                    eventIndex = count, timeSeconds = p[1], physicsTimeSeconds = p[2], epoch = (int)p[3],
                    lastForceRow = (int)p[4], bodyId = (int)p[5], otherId = (int)p[6],
                    road = p[8] == 1, crowd = p[9] == 1, contacts = (int)p[10], examinedContacts = (int)p[11],
                    selectedContact = (int)p[12], contactSamplingLimited = partial,
                    relativeSpeedMps = Math.Sqrt(p.Skip(13).Take(3).Sum(v => v * v)),
                    normalSpeedMps = p[11] > 0 ? (double?)Math.Abs(p[13] * p[30] + p[14] * p[31] + p[15] * p[32]) : null,
                    impulsePerMassMps = Math.Sqrt(p.Skip(16).Take(3).Sum(v => v * v)) / p[19],
                    relativeVelocityMps = p.Skip(13).Take(3).ToArray(), impulseNs = p.Skip(16).Take(3).ToArray(),
                    normalWorld = p.Skip(30).Take(3).ToArray()
                });
            count++; lastTime = p[1];
        }
        Require(count == expected, "missing row");
        return new { available = true, rows = count, contactLimitedEvents = limited, omittedEvents = count - events.Count, events,
            scope = "Passive player collision entries; no crash classifier or calibrated wheel/motion output" };
    }
}
