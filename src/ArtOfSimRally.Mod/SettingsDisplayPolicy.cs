using System;

namespace ArtOfSimRally.Mod
{
    internal static class SettingsDisplayPolicy
    {
        internal static float Scale(int height, float hostScale, bool followHost)
        {
            hostScale = float.IsNaN(hostScale) ? 1 : Math.Max(.5f, Math.Min(5, hostScale));
            // An explicit non-default UMM scale wins. Auto only compensates for
            // the host's 1x default on a high-resolution display.
            return followHost || Math.Abs(hostScale - 1) > .01f ? hostScale : Math.Max(1, Math.Min(2, height / 1080f));
        }
        internal static float Width(int screenWidth, float scale, float hostWidth)
            => Math.Max(200, Math.Min(screenWidth - 100, hostWidth > 0 ? hostWidth - 70 : 860 * scale));
        internal static float PageHeight(int screenHeight, float scale, float hostHeight)
            => Math.Max(120, Math.Min(480 * scale, Math.Min(screenHeight * .45f,
                hostHeight > 0 ? Math.Max(120, hostHeight - 260 * scale) : screenHeight * .45f)));
    }
}
