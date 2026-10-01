using ArtOfSimRally.Mod;
using Dbce.Wheel.Telemetry;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

static partial class Program
{
    static int crashScenarios;

    // Exercise the shipped sampler and encoder together. These are prescribed
    // velocity changes, not captured crashes or tests of SimHub/Unity scheduling.
    static void CrashTelemetry()
    {
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Client.ReceiveTimeout = 2000;
        using var sender = new TelemetrySender("127.0.0.1", ((IPEndPoint)listener.Client.LocalEndPoint).Port);
        var cases = new[]
        {
            ("front stop", new Vector3(0, 0, 20), new Vector3(0, 0, 10)),
            ("front rebound", new Vector3(0, 0, 20), new Vector3(0, 0, -3)),
            ("right slide impact", new Vector3(8, 0, 20), new Vector3(0, 0, 20)),
            ("left slide impact", new Vector3(-8, 0, 20), new Vector3(0, 0, 20))
        };
        foreach (int hz in new[] { 30, 50, 60, 120 })
        foreach (int heading in new[] { 0, 90, 225 })
        foreach (var (name, before, after) in cases)
        {
            crashScenarios++;
            string context = $"{name}, {hz} Hz, heading {heading}";
            float dt = 1f / hz;
            double radians = heading * Math.PI / 180;
            var rotation = new Quaternion(0, (float)Math.Sin(radians / 2), 0, (float)Math.Cos(radians / 2));
            // Independent heading transform builds the synthetic world trajectory.
            Vector3 World(Vector3 local) => new Vector3(
                (float)(Math.Cos(radians) * local.x + Math.Sin(radians) * local.z), local.y,
                (float)(-Math.Sin(radians) * local.x + Math.Cos(radians) * local.z));
            var oldVelocity = World(before);
            var newVelocity = World(after);
            var history = new TelemetryMotion();
            var position = new Vector3();

            byte[] Sample(Vector3 p, Vector3 v, float clock)
            {
                var frame = new TelemetryFrame { IsRaceOn = true };
                TelemetrySampling.FillMotion(ref frame, v, history.Acceleration(p, v, clock), new Vector3(), rotation);
                Check(sender.Send(frame), context + " UDP send");
                IPEndPoint peer = null;
                var bytes = listener.Receive(ref peer);
                Check(bytes.Length == ForzaPacket.Size, context + " packet size");
                return bytes;
            }
            Vector3 Acceleration(byte[] bytes) => new Vector3(
                BitConverter.ToSingle(bytes, ForzaPacket.OffAccelerationX),
                BitConverter.ToSingle(bytes, ForzaPacket.OffAccelerationX + 4),
                BitConverter.ToSingle(bytes, ForzaPacket.OffAccelerationX + 8));
            void Zero(byte[] bytes, string phase) => Vector(Acceleration(bytes), 0, 0, 0, context + " " + phase);

            Zero(Sample(position, oldVelocity, 0), "moving spawn has no impulse");
            position = World(new Vector3(before.x * dt, before.y * dt, before.z * dt));
            byte[] impact = Sample(position, newVelocity, dt);
            Vector3 acceleration = Acceleration(impact);
            Vector(acceleration, (after.x - before.x) / dt, (after.y - before.y) / dt,
                (after.z - before.z) / dt, context + " signed peak survives packet encoding");
            // Across sample rates, integral of the impulse must equal delta-v.
            Vector(new Vector3(acceleration.x * dt, acceleration.y * dt, acceleration.z * dt),
                after.x - before.x, after.y - before.y, after.z - before.z, context + " impulse area");
            Near(BitConverter.ToSingle(impact, ForzaPacket.OffVelocityX), after.x, context + " lateral velocity");
            Near(BitConverter.ToSingle(impact, ForzaPacket.OffVelocityX + 8), after.z, context + " forward velocity");

            position = new Vector3(position.x + newVelocity.x * dt, position.y + newVelocity.y * dt, position.z + newVelocity.z * dt);
            Zero(Sample(position, newVelocity, 2 * dt), "impact not held after constant velocity resumes");
            // An abrupt reset with a similar velocity change is not an impact.
            position = new Vector3(position.x + 100, position.y, position.z);
            Zero(Sample(position, oldVelocity, 3 * dt), "teleport suppressed");
            position = new Vector3(position.x + oldVelocity.x * dt, position.y + oldVelocity.y * dt, position.z + oldVelocity.z * dt);
            Zero(Sample(position, oldVelocity, 4 * dt), "teleport baseline does not replay impact");
            history.Reset();
            Zero(Sample(position, newVelocity, 5 * dt), "explicit lifecycle reset suppresses velocity change");
        }
    }
}
