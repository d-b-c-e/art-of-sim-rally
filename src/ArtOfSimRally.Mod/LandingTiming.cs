using System;
using System.Text;

namespace ArtOfSimRally.Mod
{
    // Last-event diagnostic only: no timeline, IO, delay or effect output.
    // Suspension compression is a proxy, not measured wheel force/contact time.
    internal sealed class LandingTiming
    {
        internal bool Available { get; private set; }
        internal bool Pending { get; private set; }
        internal double ContactTime { get; private set; }
        internal int ContactMask { get; private set; }
        internal float CompressionAtContact { get; private set; }
        internal double CompressionDelayMs { get; private set; } = -1;
        internal double AllWheelsDelayMs { get; private set; } = -1;
        internal string Status { get; private set; } = "No diagnostic landing observed";
        private double _lastTime;

        internal void Start(double time,int mask,float compression)
        {
            if (!Finite(time) || time<0 || mask<1 || mask>15) { Cancel(); return; }
            Available=Pending=true; ContactTime=_lastTime=time; ContactMask=mask;
            CompressionAtContact=Finite(compression) && compression>=0 ? compression : -1;
            CompressionDelayMs=AllWheelsDelayMs=-1; Status="Observing (up to 200ms)";
            Observe(time,mask,compression);
        }
        internal bool Observe(double time,int mask,float compression)
        {
            if (!Pending) return false;
            if (!Finite(time) || time<_lastTime || time-_lastTime>.1 || mask<0 || mask>15)
            { Cancel(); return false; }
            _lastTime=time;
            double elapsed=time-ContactTime;
            if (elapsed<=.2)
            {
                if (CompressionDelayMs<0 && mask!=0 && Finite(compression) && compression>=.02f)
                    CompressionDelayMs=elapsed*1000;
                if (AllWheelsDelayMs<0 && mask==15) AllWheelsDelayMs=elapsed*1000;
            }
            if ((CompressionDelayMs>=0 && AllWheelsDelayMs>=0) || elapsed>=.2)
            {
                Pending=false; Status=CompressionDelayMs>=0 && AllWheelsDelayMs>=0 ? "Observed" : "Window ended; -1 means not observed";
                return true;
            }
            return false;
        }
        internal void Cancel() { if (Pending) Status="Interrupted; -1 means not observed"; Pending=false; }
        internal static float Ratio(bool contact,float compression,float travel)
            => contact && Finite(compression) && Finite(travel) && travel>0 && compression>=0 ? Math.Min(1,compression/travel) : 0;
        internal void Append(StringBuilder text)
        {
            text.AppendLine("=== Last diagnostic landing timing ===");
            text.AppendLine(Status);
            if (Available)
                text.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "physics time={0:F4}s; first mask={1}; max compression/travel at detector={2:F4}; compression >=2% delay={3:F2}ms; all-wheel contact delay={4:F2}ms",
                    ContactTime,ContactMask,CompressionAtContact,CompressionDelayMs,AllWheelsDelayMs));
            text.AppendLine("Relative to the detector's first contact. Not visual touchdown, driver latency or calibrated load; physics field ordering can differ.");
            text.AppendLine();
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
