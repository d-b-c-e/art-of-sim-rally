#if NETFRAMEWORK
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ArtOfSimRally.Testing
{
    // Unity 2019's WindowsIdentity.User getter throws NotImplementedException.
    // Read the process token's SID without changing identity or permissions.
    internal static class CurrentUserSid
    {
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(IntPtr token, int kind, IntPtr buffer, int length, out int required);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern uint GetLengthSid(IntPtr sid);

        internal static SecurityIdentifier Read()
        {
            if (!OpenProcessToken(GetCurrentProcess(), 8, out var token)) throw new Win32Exception(Marshal.GetLastWin32Error());
            IntPtr buffer = IntPtr.Zero;
            try
            {
                GetTokenInformation(token, 1, IntPtr.Zero, 0, out int length);
                if (length <= 0 || length > 65536) throw new Win32Exception(Marshal.GetLastWin32Error());
                buffer = Marshal.AllocHGlobal(length);
                if (!GetTokenInformation(token, 1, buffer, length, out length)) throw new Win32Exception(Marshal.GetLastWin32Error());
                IntPtr sid = Marshal.ReadIntPtr(buffer);
                int bytes = checked((int)GetLengthSid(sid));
                if (bytes < 8 || bytes > 256) throw new InvalidOperationException("Invalid process SID length");
                var data = new byte[bytes]; Marshal.Copy(sid, data, 0, bytes);
                return new SecurityIdentifier(data, 0);
            }
            finally { if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer); CloseHandle(token); }
        }
    }
}
#endif
