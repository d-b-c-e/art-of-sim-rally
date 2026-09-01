using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Managed binding to <c>UnityForceFeedback.dll</c>, the native plugin this
    /// project supplies because art of rally ships without it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately the same module name and entry points the game's own dead
    /// <c>ForceFeedback</c> class declares, so one DLL serves both.
    /// </para>
    /// <para>
    /// <c>bool</c> returns marshal as the 4-byte Win32 <c>BOOL</c>; the native
    /// side returns <c>BOOL</c> to match. See docs/FORCE-FEEDBACK.md.
    /// </para>
    /// </remarks>
    internal static class FfbNative
    {
        private const string Dll = "UnityForceFeedback";

        [DllImport("user32")]
        private static extern int GetForegroundWindow();

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string path);

        [DllImport(Dll)] private static extern int  InitDirectInput(int hwnd);
        [DllImport(Dll)] private static extern void Aquire();
        [DllImport(Dll)] private static extern int  SetDeviceForcesXY(int x, int y);
        [DllImport(Dll)] private static extern bool StartEffect();
        [DllImport(Dll)] private static extern bool StopEffect();
        [DllImport(Dll)] private static extern bool SetAutoCenter(bool autoCentre);
        [DllImport(Dll)] private static extern void FreeDirectInput();

        /// <summary>DirectInput's nominal full-scale force.</summary>
        public const int ForceMax = 10000;

        private static bool _initialised;
        private static bool _failed;

        /// <summary>True once the device is open and an effect is running.</summary>
        public static bool Ready => _initialised && !_failed;

        /// <summary>
        /// Opens the wheel and starts the constant-force effect. Safe to call repeatedly.
        /// </summary>
        /// <param name="pluginDir">Folder holding the plugin, used to locate the native DLL.</param>
        /// <returns>True if force feedback is live.</returns>
        public static bool Initialise(string pluginDir)
        {
            if (_initialised) return !_failed;
            _initialised = true;

            try
            {
                // Preload by absolute path before the first P/Invoke binds. Unity
                // resolves native plugins out of <Data>/Plugins/x86_64, but a
                // mod assembly loaded by Unity Mod Manager is not a Unity-managed
                // plugin and does not reliably inherit that search path. Loading it explicitly makes
                // the later DllImport bind to an already-resident module, and
                // turns "wrong folder" into a clear log line instead of a
                // DllNotFoundException from inside a physics callback.
                if (LoadLibraryW(ResolveDllPath(pluginDir)) == IntPtr.Zero)
                    ModLog.Warning(
                        "Could not preload UnityForceFeedback.dll by path; " +
                        "relying on the default search order.");

                int hwnd = GetForegroundWindow();
                if (InitDirectInput(hwnd) == 0)
                {
                    ModLog.Error(
                        "InitDirectInput failed - no force-feedback device found, or " +
                        "another process holds it exclusively. See " +
                        "%LOCALAPPDATA%\\ArtOfSimRally\\ffb.log.");
                    _failed = true;
                    return false;
                }

                Aquire();
                StartEffect();
                SetAutoCenter(false);   // autocentre fights every effect we apply
                ModLog.Info("Force feedback device initialised.");
                return true;
            }
            catch (Exception ex)
            {
                // A DllNotFoundException here is the expected failure when the
                // native plugin was never built. Degrade to no FFB rather than
                // taking the game down with us.
                ModLog.Error($"Force feedback unavailable: {ex.Message}");
                _failed = true;
                return false;
            }
        }

        /// <summary>
        /// Sends a steering force. <paramref name="x"/> is clamped to
        /// +/-<see cref="ForceMax"/>; positive and negative pull opposite ways.
        /// </summary>
        public static void SetForce(int x)
        {
            if (!Ready) return;
            if (x >  ForceMax) x =  ForceMax;
            if (x < -ForceMax) x = -ForceMax;
            try { SetDeviceForcesXY(x, 0); }
            catch (Exception ex)
            {
                ModLog.Error($"SetDeviceForcesXY failed, disabling FFB: {ex.Message}");
                _failed = true;
            }
        }

        /// <summary>Stops the effect and releases the device.</summary>
        public static void Shutdown()
        {
            if (!Ready) return;
            try { StopEffect(); FreeDirectInput(); }
            catch (Exception ex) { ModLog.Warning($"FFB shutdown: {ex.Message}"); }
            _failed = true;
        }

        // The DLL belongs beside the game's other native plugins. Fall back to the
        // plugin folder so a development copy can be dropped next to the mod.
        private static string ResolveDllPath(string pluginDir)
        {
            string beside = Path.Combine(pluginDir, "UnityForceFeedback.dll");
            if (File.Exists(beside)) return beside;

            string dataDir = UnityEngine.Application.dataPath; // <game>/artofrally_Data
            return Path.Combine(dataDir, "Plugins", "x86_64", "UnityForceFeedback.dll");
        }
    }
}
