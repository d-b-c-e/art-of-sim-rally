using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ArtOfSimRally.Testing
{
    internal sealed class ControlServer : IDisposable
    {
        private sealed class Request
        {
            public string Command, Reply;
            public volatile bool Cancelled;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }
        private readonly ConcurrentQueue<Request> queue = new ConcurrentQueue<Request>();
        private readonly string name;
        private readonly Thread thread;
        private volatile bool stopping;
        private NamedPipeServerStream current;
        public ControlServer(string pipeName)
        {
            name = pipeName; thread = new Thread(Listen) { IsBackground = true, Name = "AOSR developer capture control" }; thread.Start();
        }
        private NamedPipeServerStream Open()
        {
#if NETFRAMEWORK
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
            return new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, security);
#else
            return new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#endif
        }
        private void Listen()
        {
            while (!stopping)
            {
                try
                {
                    using (var pipe = Open())
                    {
                        current = pipe;
                        if (stopping) return;
                        pipe.WaitForConnection();
                        using (var reader = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 1024, true))
                        using (var writer = new StreamWriter(pipe, new System.Text.UTF8Encoding(false), 1024, true) { AutoFlush = true })
                        {
                            string command = reader.ReadLine();
                            if (command != "START" && command != "STOP" && command != "STATUS") { writer.WriteLine("ERROR unknown command"); continue; }
                            var request = new Request { Command = command }; queue.Enqueue(request);
                            if (request.Done.Wait(5000)) writer.WriteLine(request.Reply);
                            else { request.Cancelled = true; writer.WriteLine("ERROR game did not respond; query STATUS before retrying"); }
                        }
                    }
                }
                catch (Exception) { if (!stopping) Thread.Sleep(100); }
                finally { current = null; }
            }
        }
        // Called on the game's main thread; pipe IO waits on the background
        // thread. START/STOP allocate/write only after verifying the game is idle.
        public void Pump(Func<string, string> handle)
        {
            while (queue.TryDequeue(out Request request))
            {
                if (request.Cancelled) continue;
                try { request.Reply = handle(request.Command); }
                catch (Exception ex) { request.Reply = "ERROR " + ex.Message; }
                request.Done.Set();
            }
        }
        public void Dispose()
        {
            stopping = true;
            try { current?.Dispose(); } catch { }
            thread.Join(200);
        }
    }
}
