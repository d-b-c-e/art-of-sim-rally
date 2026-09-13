using System;
using System.Net;
using System.Net.Sockets;

namespace ArtOfSimRally.Haptics
{
    internal sealed class HapticSender : IDisposable
    {
        private readonly Socket _socket;
        private readonly IPEndPoint _target;
        private readonly byte[] _buffer = new byte[HapticProtocol.Size];
        private HapticFrame _frame;
        private uint _lastSent;
        private bool _sent;
        public long Sent { get; private set; }
        public long Dropped { get; private set; }
        public HapticSender(int port = HapticProtocol.Port)
        {
            _target = new IPEndPoint(IPAddress.Loopback, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Blocking = false;
            _frame.Session = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0);
            if (_frame.Session == 0) _frame.Session = 1;
        }
        public void Update(bool active, uint now, int landingMagnitude = 0)
        {
            bool changed = _frame.Active != active;
            _frame.Active = active;
            if (!active) _frame.Magnitude=0;
            else if (landingMagnitude > 0)
            {
                _frame.Event++; _frame.EventTick=now;
                _frame.Magnitude=Math.Min(10000, landingMagnitude);
            }
            // Regular snapshots repeat the event id/timestamp, not a new pulse.
            if (_sent && !changed && landingMagnitude <= 0 && HapticProtocol.Age(now,_lastSent) < (active ? 20u : 100u)) return;
            _frame.Sequence++; _frame.Tick=now; HapticProtocol.Encode(_frame,_buffer);
            _lastSent=now; _sent=true;
            try { _socket.SendTo(_buffer, _target); Sent++; }
            catch (SocketException) { Dropped++; } // Nonblocking, no replay queue or retry in physics.
            catch (ObjectDisposedException) { Dropped++; }
        }
        public void Dispose()
        { Update(false,HapticProtocol.Now); _socket.Close(); }
    }
}
