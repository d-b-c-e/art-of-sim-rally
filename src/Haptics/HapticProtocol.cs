using System;

namespace ArtOfSimRally.Haptics
{
    // Game-specific loopback side channel. Never changes the Forza packet.
    internal struct HapticFrame
    {
        public ulong Session;
        public uint Sequence, Tick, Event, EventTick;
        public bool Active;
        public int Magnitude; // 0..10000, authored haptic cue, not a physical unit.
    }

    internal static class HapticProtocol
    {
        public const int Port = 20779, Size = 40, TimeoutMs = 200;
        public static uint Now => unchecked((uint)Environment.TickCount);
        public static uint Age(uint now, uint then) => unchecked(now - then);
        public static bool Newer(uint value, uint previous) => unchecked((int)(value - previous)) > 0;
        public static void Encode(HapticFrame f, byte[] b)
        {
            Put(b, 0, 0x31484F41); Put(b, 4, f.Active ? 1u : 0u);
            Put(b, 8, (uint)f.Session); Put(b, 12, (uint)(f.Session >> 32));
            Put(b, 16, f.Sequence); Put(b, 20, f.Tick); Put(b, 24, f.Event);
            Put(b, 28, f.EventTick); Put(b, 32, (uint)f.Magnitude); Put(b, 36, 0);
        }
        public static bool Decode(byte[] b, int count, out HapticFrame f)
        {
            f = default(HapticFrame);
            if (b == null || count != Size || b.Length < Size || Get(b, 0) != 0x31484F41 ||
                Get(b, 4) > 1 || Get(b, 32) > 10000 || Get(b, 36) != 0) return false;
            f = new HapticFrame { Active = Get(b, 4) != 0, Session = Get(b, 8) | ((ulong)Get(b, 12) << 32),
                Sequence = Get(b, 16), Tick = Get(b, 20), Event = Get(b, 24), EventTick = Get(b, 28),
                Magnitude = (int)Get(b, 32) };
            return f.Session != 0 && (f.Active || f.Magnitude == 0);
        }
        private static uint Get(byte[] b, int p) => (uint)(b[p] | b[p+1] << 8 | b[p+2] << 16 | b[p+3] << 24);
        private static void Put(byte[] b, int p, uint v)
        { b[p]=(byte)v; b[p+1]=(byte)(v>>8); b[p+2]=(byte)(v>>16); b[p+3]=(byte)(v>>24); }
    }

    internal sealed class LandingPulse
    {
        public const int DurationMs = 180, AttackMs = 10, HoldMs = 30;
        private bool _seen, _active;
        private ulong _session;
        private uint _sequence, _frameTick, _event, _eventTick;
        private int _magnitude;
        public long Accepted { get; private set; }
        public long Rejected { get; private set; }
        public bool Accept(HapticFrame f, uint now)
        {
            if (f.Session == 0 || f.Magnitude < 0 || f.Magnitude > 10000 ||
                HapticProtocol.Age(now, f.Tick) > HapticProtocol.TimeoutMs ||
                (_seen && !HapticProtocol.Newer(f.Tick, _frameTick) && f.Tick != _frameTick) ||
                (_seen && f.Session == _session && !HapticProtocol.Newer(f.Sequence, _sequence)))
            { Rejected++; return false; }
            bool baseline = !_seen || f.Session != _session || HapticProtocol.Age(now, _frameTick) > HapticProtocol.TimeoutMs;
            _seen = true; _session = f.Session; _sequence = f.Sequence; _frameTick = f.Tick;
            if (baseline || !f.Active)
            { _event = f.Event; _active = false; _magnitude = 0; return true; }
            if (f.Event != _event)
            {
                if (!HapticProtocol.Newer(f.Event, _event)) { Rejected++; return false; }
                _event = f.Event;
                if (HapticProtocol.Age(now, f.EventTick) < DurationMs && f.Magnitude > 0)
                { _eventTick = f.EventTick; _magnitude = f.Magnitude; _active = true; Accepted++; }
            }
            return true;
        }
        public double Value(uint now)
        {
            if (!_seen || !_active || HapticProtocol.Age(now, _frameTick) > HapticProtocol.TimeoutMs) return 0;
            uint age = HapticProtocol.Age(now, _eventTick);
            if (age >= DurationMs) return 0;
            double envelope = age < AttackMs ? age / (double)AttackMs : age <= AttackMs + HoldMs ? 1 :
                Math.Pow((DurationMs - age) / (double)(DurationMs - AttackMs - HoldMs), 2);
            return _magnitude / 100.0 * envelope; // SimHub custom effects consume 0..100.
        }
        public bool Connected(uint now) => _seen && HapticProtocol.Age(now, _frameTick) <= HapticProtocol.TimeoutMs;
        public void Clear() { _seen=false; _active=false; _magnitude=0; }
    }
}
