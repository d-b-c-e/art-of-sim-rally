namespace ArtOfSimRally.Mod
{
    // Keep the release frame blocked too: Unity/Rewired key-up and cached
    // navigation events can still be delivered during that frame.
    internal sealed class MenuInputBarrier
    {
        internal bool Captured { get; private set; }
        private int _neutralFrame = -1;
        internal void Capture() { Captured = true; _neutralFrame = -1; }
        internal bool Blocks(bool visible, bool focused, bool held, int frame)
        {
            if (visible) { Capture(); return true; }
            if (!Captured) return false;
            if (!focused || held) { _neutralFrame = -1; return true; }
            if (_neutralFrame < 0) { _neutralFrame = frame; return true; }
            if (frame <= _neutralFrame + 1) return true;
            Captured = false;
            return false;
        }
        internal void Reset() { Captured = false; _neutralFrame = -1; }
    }
}
