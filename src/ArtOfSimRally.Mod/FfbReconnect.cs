using System;

namespace ArtOfSimRally.Mod
{
    // Startup/window races get a small retry budget. Never reconnect in a
    // driving frame or while assigning an axis, even if a timer has expired.
    internal sealed class FfbReconnect
    {
        private int remaining;
        private IntPtr window;
        private double focusedSince, nextAttempt;
        public bool Pending => remaining > 0;
        public void Request() { remaining = 5; window = IntPtr.Zero; nextAttempt = 0; }
        public bool TryBegin(double now, bool enabled, bool driving, bool assigning, IntPtr focusedWindow)
        {
            if (!enabled || driving || assigning || focusedWindow == IntPtr.Zero)
            { window = IntPtr.Zero; return false; }
            if (window != focusedWindow) { window = focusedWindow; focusedSince = now; }
            if (!Pending || now < nextAttempt || now - focusedSince < .5) return false;
            remaining--; nextAttempt = now + 5;
            return true;
        }
        public void Complete(bool connected) { if (connected) remaining = 0; }
        public void Cancel() { remaining = 0; window = IntPtr.Zero; }
    }
}
