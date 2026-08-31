using System;
using System.Net;
using System.Net.Sockets;

namespace ArtOfSimRally.Telemetry
{
    /// <summary>
    /// Fire-and-forget UDP sender for <see cref="ForzaPacket"/> frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called from the game's physics loop, so the two rules are: never allocate per
    /// frame, and never let a network error reach the caller. A telemetry problem
    /// must not stall or crash the game — dropping a packet is always the right
    /// answer, and UDP callers expect loss anyway.
    /// </para>
    /// <para>
    /// Not thread-safe: the shared scratch buffer assumes a single caller. That
    /// matches how it is used (one Unity FixedUpdate), and a lock on the hot path
    /// would cost more than it protects.
    /// </para>
    /// </remarks>
    public sealed class TelemetrySender : IDisposable
    {
        /// <summary>Port SimHub and most Forza consumers listen on by default.</summary>
        public const int DefaultPort = 8000;

        private readonly byte[]     _buffer = ForzaPacket.CreateBuffer();
        private readonly IPEndPoint _endpoint;
        private UdpClient           _client;
        private bool                _disposed;

        /// <summary>Number of packets successfully handed to the socket.</summary>
        public long PacketsSent { get; private set; }

        /// <summary>Number of sends that failed. Non-zero is not necessarily a problem.</summary>
        public long SendFailures { get; private set; }

        /// <summary>Most recent send error, for surfacing in the mod's settings UI.</summary>
        public string LastError { get; private set; }

        /// <param name="host">Destination host. Loopback for a consumer on this PC.</param>
        /// <param name="port">Destination UDP port.</param>
        /// <exception cref="ArgumentException"><paramref name="host"/> cannot be resolved.</exception>
        public TelemetrySender(string host = "127.0.0.1", int port = DefaultPort)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1-65535.");

            IPAddress address;
            if (!IPAddress.TryParse(host ?? string.Empty, out address))
            {
                // A hostname is unusual here but valid for a telemetry box on the LAN.
                // Resolve once at construction; never on the hot path.
                try
                {
                    var entries = Dns.GetHostAddresses(host);
                    if (entries.Length == 0)
                        throw new ArgumentException($"Host '{host}' resolved to no addresses.", nameof(host));
                    address = entries[0];
                }
                catch (Exception ex) when (!(ex is ArgumentException))
                {
                    throw new ArgumentException($"Could not resolve host '{host}': {ex.Message}", nameof(host), ex);
                }
            }

            _endpoint = new IPEndPoint(address, port);
            _client   = new UdpClient();

            // Broadcast is off by default; enabling it lets a user point telemetry at
            // 255.255.255.255 for a rig on the same LAN without knowing its address.
            try { _client.EnableBroadcast = true; } catch { /* not fatal */ }
        }

        /// <summary>
        /// Encodes and sends one frame. Never throws; failures are counted instead.
        /// </summary>
        /// <returns><c>true</c> if the packet reached the socket.</returns>
        public bool Send(in TelemetryFrame frame)
        {
            if (_disposed) return false;

            try
            {
                ForzaPacket.Write(frame, _buffer);
                _client.Send(_buffer, ForzaPacket.Size, _endpoint);
                PacketsSent++;
                return true;
            }
            catch (Exception ex)
            {
                // Swallowing is deliberate — see the class remarks. Recording the
                // message keeps it diagnosable without a logger dependency here.
                SendFailures++;
                LastError = ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var client = _client;
            _client = null;
            if (client == null) return;

            try { client.Close(); } catch { /* shutting down anyway */ }
#if !NET35
            try { ((IDisposable)client).Dispose(); } catch { }
#endif
        }
    }
}
