using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Runtime.InteropServices;

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
        private readonly ManualResetEvent stopped = new ManualResetEvent(false);
        private readonly object workerLock = new object();
        private IntPtr workerHandle;
        [DllImport("kernel32")] private static extern uint GetCurrentThreadId();
        [DllImport("kernel32")] private static extern IntPtr OpenThread(uint access, bool inherit, uint id);
        [DllImport("kernel32")] private static extern bool CancelSynchronousIo(IntPtr threadHandle);
        [DllImport("kernel32")] private static extern bool CloseHandle(IntPtr handle);
        internal bool IsAlive => thread.IsAlive;
        private NamedPipeServerStream current;
        public ControlServer(string pipeName)
        {
            name = pipeName;
            // Fail at load, before reporting ready. A background retry must not
            // conceal an unsupported runtime/security API indefinitely.
            current = Open();
            thread = new Thread(Listen) { IsBackground = true, Name = "AOSR developer capture control" }; thread.Start();
        }
        private NamedPipeServerStream Open()
        {
#if NETFRAMEWORK
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(CurrentUserSid.Read(), PipeAccessRights.FullControl, AccessControlType.Allow));
            // Unity's Mono maps Asynchronous to PIPE_NOWAIT then waits without
            // an OVERLAPPED structure. This dedicated thread uses blocking IO.
            return new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 4096, 4096, security);
#else
            return new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
#endif
        }
        private void Listen()
        {
            // The Mono pipe implementation uses synchronous native IO. Closing
            // its stream on the main thread need not interrupt that native wait.
            // Keep a thread handle so shutdown can cancel the blocked operation.
            lock (workerLock) workerHandle = OpenThread(1, false, GetCurrentThreadId());
            try
            {
            while (!stopping)
            {
                try
                {
                    using (var pipe = current ?? Open())
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
                            int completed = WaitHandle.WaitAny(new[] { stopped, request.Done.WaitHandle }, 5000);
                            if (completed == 0) { request.Cancelled = true; return; }
                            if (completed == 1) writer.WriteLine(request.Reply);
                            else { request.Cancelled = true; writer.WriteLine("ERROR game did not respond; query STATUS before retrying"); }
                        }
                    }
                }
                catch (Exception) { if (!stopping) Thread.Sleep(100); }
                finally { current = null; }
            }
            }
            finally
            {
                current?.Dispose(); current = null;
                lock (workerLock)
                {
                    if (workerHandle != IntPtr.Zero) CloseHandle(workerHandle);
                    workerHandle = IntPtr.Zero;
                }
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
            stopped.Set();
            // Repeated cancellation covers the race between checking 'stopping'
            // and entering native IO. The worker alone disposes the pipe.
            for (int i = 0; i < 20 && thread.IsAlive; i++)
            {
                lock (workerLock)
                    if (workerHandle != IntPtr.Zero) CancelSynchronousIo(workerHandle);
                thread.Join(10);
            }
        }
    }
}
