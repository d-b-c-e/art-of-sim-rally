using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ArtOfSimRally.Haptics;

namespace ArtOfSimRally.SimHub
{
    internal sealed class HapticReceiver : IDisposable
    {
        private readonly object _sync=new object();
        private readonly LandingPulse _pulse=new LandingPulse();
        private readonly Socket _socket;
        private readonly Thread _thread;
        private volatile bool _closed;
        private long _invalid;
        public double Value { get { lock(_sync) return _pulse.Value(HapticProtocol.Now); } }
        public bool Connected { get { lock(_sync) return _pulse.Connected(HapticProtocol.Now); } }
        public long Accepted { get { lock(_sync) return _pulse.Accepted; } }
        public long Rejected { get { lock(_sync) return _pulse.Rejected+_invalid; } }
        public HapticReceiver(int port=HapticProtocol.Port)
        {
            _socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            try
            {
                _socket.ExclusiveAddressUse=true; _socket.ReceiveTimeout=100;
                _socket.Bind(new IPEndPoint(IPAddress.Loopback,port));
            }
            catch { _socket.Close(); throw; }
            _thread=new Thread(Read) { IsBackground=true, Name="ArtOfSimRally haptics" }; _thread.Start();
        }
        public int Port => ((IPEndPoint)_socket.LocalEndPoint).Port;
        private void Read()
        {
            var bytes=new byte[HapticProtocol.Size+1];
            EndPoint remote=new IPEndPoint(IPAddress.Loopback,0);
            while (!_closed)
            {
                try
                {
                    int count=_socket.ReceiveFrom(bytes,ref remote);
                    lock(_sync)
                    {
                        if (_closed) break;
                        HapticFrame frame;
                        if (!IPAddress.IsLoopback(((IPEndPoint)remote).Address) || !HapticProtocol.Decode(bytes,count,out frame)) _invalid++;
                        else _pulse.Accept(frame,HapticProtocol.Now);
                    }
                }
                catch (SocketException ex)
                {
                    if (_closed) break;
                    if (ex.SocketErrorCode != SocketError.TimedOut) { lock(_sync) _invalid++; }
                    if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.MessageSize) break;
                }
                catch (ObjectDisposedException) { break; }
            }
            lock(_sync) _pulse.Clear();
        }
        public void Dispose()
        {
            _closed=true; _socket.Close(); _thread.Join(500);
            lock(_sync) _pulse.Clear();
        }
    }
}
