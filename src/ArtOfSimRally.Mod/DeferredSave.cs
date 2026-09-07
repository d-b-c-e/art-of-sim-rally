using System;

namespace ArtOfSimRally.Mod
{
    // In-memory calibration changes survive failed writes. Time is supplied by
    // the caller so retry policy can be tested without Unity or a real disk.
    internal sealed class DeferredSave
    {
        public bool Pending { get; private set; }
        private double _retryAfter;
        public void MarkDirty() => Pending = true;

        public bool Flush(double now, bool driving, bool force, Func<bool> save)
        {
            if (!Pending || driving || (!force && now < _retryAfter)) return false;
            _retryAfter = now + 5;
            try
            {
                if (!save()) return false;
                Pending = false;
                return true;
            }
            catch { return false; } // Still pending; the production save reports the error.
        }
    }
}
