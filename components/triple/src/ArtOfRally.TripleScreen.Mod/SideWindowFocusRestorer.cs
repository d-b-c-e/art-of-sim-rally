using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>Raises this game's Unity secondary windows when the game regains focus.</summary>
internal static class SideWindowFocusRestorer
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y,
        int width, int height, uint flags);

    internal static int Raise()
    {
        var ownProcess = (uint)Process.GetCurrentProcess().Id;
        var raised = 0;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var windowProcess);
            if (windowProcess != ownProcess || !IsWindowVisible(window)) return true;
            var title = new StringBuilder(64);
            GetWindowText(window, title, title.Capacity);
            if (!string.Equals(title.ToString(), "Unity Secondary Display", StringComparison.Ordinal))
                return true;
            if (SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0,
                    SwpNoSize | SwpNoMove | SwpNoActivate)) raised++;
            return true;
        }, IntPtr.Zero);
        return raised;
    }
}
