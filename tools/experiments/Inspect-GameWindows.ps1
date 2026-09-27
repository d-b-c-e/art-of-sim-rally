param([string] $ProcessName = 'artofrally')

$ErrorActionPreference = 'Stop'
$process = Get-Process -Name $ProcessName -ErrorAction Stop | Select-Object -First 1

if (-not ('DbceGameWindowProbe' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class DbceGameWindowProbe
{
    public delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr window, uint command);
}
'@
}

$foreground = [DbceGameWindowProbe]::GetForegroundWindow()
$windows = [System.Collections.Generic.List[object]]::new()
$callback = [DbceGameWindowProbe+EnumWindowsProc] {
    param([IntPtr] $window, [IntPtr] $parameter)
    [uint32] $ownerProcessId = 0
    [void][DbceGameWindowProbe]::GetWindowThreadProcessId($window, [ref] $ownerProcessId)
    if ($ownerProcessId -ne $process.Id) { return $true }
    $rect = [DbceGameWindowProbe+Rect]::new()
    [void][DbceGameWindowProbe]::GetWindowRect($window, [ref] $rect)
    $title = [System.Text.StringBuilder]::new(256)
    [void][DbceGameWindowProbe]::GetWindowText($window, $title, $title.Capacity)
    $windows.Add([pscustomobject]@{
        handle = $window.ToInt64()
        title = $title.ToString()
        visible = [DbceGameWindowProbe]::IsWindowVisible($window)
        minimized = [DbceGameWindowProbe]::IsIconic($window)
        foreground = $window -eq $foreground
        ownerHandle = [DbceGameWindowProbe]::GetWindow($window, 4).ToInt64()
        x = $rect.Left
        y = $rect.Top
        width = $rect.Right - $rect.Left
        height = $rect.Bottom - $rect.Top
    })
    return $true
}
[void][DbceGameWindowProbe]::EnumWindows($callback, [IntPtr]::Zero)
[pscustomobject]@{
    capturedUtc = [DateTime]::UtcNow.ToString('O')
    processId = $process.Id
    foregroundHandle = $foreground.ToInt64()
    windows = $windows
} | ConvertTo-Json -Depth 4
