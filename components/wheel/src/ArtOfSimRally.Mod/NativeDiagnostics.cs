using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ArtOfSimRally.Mod
{
    internal static class NativeDiagnostics
    {
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ReadInt();

        // Observe resident modules only. A support report must not load a DLL,
        // enumerate DirectInput devices, or acquire a wheel to read its version.
        public static string Describe(string moduleName)
        {
            try
            {
                var sb = new StringBuilder();
                int count = 0;
                using (var process = Process.GetCurrentProcess())
                foreach (ProcessModule module in process.Modules)
                {
                    if (!string.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)) continue;
                    count++;
                    sb.AppendLine("  mapped file : " + module.FileName);
                    sb.AppendLine("  file sha256 : " + FileHash(module.FileName) + " (file on disk)");
                    sb.AppendLine("  native version: " + ReadExport(module.BaseAddress, "GetWheelFfbVersion", true));
                    sb.AppendLine("  last hresult: " + ReadExport(module.BaseAddress, "GetLastHResult", false));
                }
                if (count == 0) return "  (not loaded; diagnostics did not load the plugin)";
                if (count > 1) sb.AppendLine("  Multiple copies are mapped; the P/Invoke destination is ambiguous.");
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex) { return "  (module inspection unavailable: " + ex.Message + ")"; }
        }

        private static string ReadExport(IntPtr module, string name, bool version)
        {
            var address = GetProcAddress(module, name);
            if (address == IntPtr.Zero) return "(export missing)";
            var value = ((ReadInt)Marshal.GetDelegateForFunctionPointer(address, typeof(ReadInt)))();
            return version
                ? string.Format("{0}.{1}.{2}", value / 10000, (value / 100) % 100, value % 100)
                : "0x" + value.ToString("X8");
        }

        internal static string FileHash(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
            catch (Exception ex) { return "(unavailable: " + ex.GetType().Name + ")"; }
        }
    }
}
