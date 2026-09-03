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

        // Eighth export, ours rather than the game's. The game never calls it.
        [DllImport(Dll, CharSet = CharSet.Ansi)]
        private static extern void SetPreferredDevice(string name);

        [DllImport(Dll)]
        private static extern void SetPreferredDeviceIndex(int index);

        [DllImport(Dll)]
        private static extern int EnumerateDevices();

        [DllImport(Dll, CharSet = CharSet.Ansi)]
        private static extern int GetDeviceName(int index, System.Text.StringBuilder buffer, int size);

        [DllImport(Dll)]
        private static extern int GetDeviceInfo(int index, out int axes, out int buttons);

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
        /// <param name="preferredDevice">
        /// Substring of the wheel's product name to prefer, or empty for the first
        /// force-feedback device found. Only matters on a rig with more than one.
        /// </param>
        /// <param name="preferredIndex">
        /// Zero-based index from the log's device list, or -1 to choose automatically.
        /// Needed where several devices share a product name - a Fanatec rig reports
        /// two devices both called "FANATEC Wheel", which no name filter can separate.
        /// </param>
        public static bool Initialise(string pluginDir, string preferredDevice = null,
                                      int preferredIndex = -1)
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
                string dllPath = ResolveDllPath(pluginDir);
                if (LoadLibraryW(dllPath) == IntPtr.Zero)
                    ModLog.Warning(
                        "Could not preload " + dllPath + "; relying on the default search order.");
                else
                    ModLog.Info("Native plugin loaded from " + dllPath);

                // Must precede InitDirectInput - that is where selection happens.
                // Index wins over name, being the unambiguous one.
                if (preferredIndex >= 0)
                    SetPreferredDeviceIndex(preferredIndex);
                else if (!string.IsNullOrEmpty(preferredDevice))
                    SetPreferredDevice(preferredDevice);

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

        /// <summary>
        /// Names of the force-feedback devices attached, for the settings UI.
        /// </summary>
        /// <remarks>
        /// Lets the panel offer a list of real device names instead of asking for
        /// an index, which is only meaningful to whoever wrote the enumeration.
        /// Enumerating does not open or acquire anything, so it is safe to call
        /// while driving.
        /// </remarks>
        public static string[] ListDevices()
        {
            try
            {
                int count = EnumerateDevices();
                if (count <= 0) return new string[0];

                var names = new string[count];
                var buf = new System.Text.StringBuilder(260);
                for (int i = 0; i < count; i++)
                {
                    buf.Length = 0;
                    names[i] = GetDeviceName(i, buf, buf.Capacity) != 0
                        ? buf.ToString()
                        : "(device " + i + ")";
                }
                return names;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not list force feedback devices: " + ex.Message);
                return new string[0];
            }
        }

        /// <summary>
        /// Display labels for <see cref="ListDevices"/>: the name plus axis and
        /// button counts, so two devices with the same name (a Fanatec base shows
        /// two "FANATEC Wheel" entries) can be told apart. Not for storing.
        /// </summary>
        public static string[] ListDeviceLabels(string[] names)
        {
            var labels = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                labels[i] = names[i];
                try
                {
                    if (GetDeviceInfo(i, out int axes, out int buttons) != 0 && (axes > 0 || buttons > 0))
                        labels[i] = names[i] + "  (" + axes + " axes, " + buttons + " buttons)";
                }
                catch { }
            }
            return labels;
        }

        /// <summary>
        /// Closes the current device and opens another, without restarting the game.
        /// </summary>
        /// <remarks>
        /// Initialise deliberately runs once, so changing the wheel used to need a
        /// relaunch. That is a poor trade for a setting sitting in a dropdown: the
        /// obvious way to find out which of two identically named devices is the
        /// right one is to pick one and feel whether the wheel moves.
        /// </remarks>
        public static bool Reinitialise(string modDir, string preferredDevice, int preferredIndex)
        {
            try { SetForce(0); } catch { }
            Shutdown();

            _initialised = false;
            _failed = false;
            return Initialise(modDir, preferredDevice, preferredIndex);
        }

        /// <summary>Stops the effect and releases the device.</summary>
        public static void Shutdown()
        {
            if (!Ready) return;
            try { StopEffect(); FreeDirectInput(); }
            catch (Exception ex) { ModLog.Warning($"FFB shutdown: {ex.Message}"); }
            _failed = true;
            _initialised = false;
        }

        // Prefer the copy shipped with the mod, falling back to the game's native
        // plugin folder.
        //
        // The caller passes UMM's ModEntry.Path, which may or may not carry a
        // trailing separator. Path.GetDirectoryName on a path WITHOUT one returns
        // the parent, so an earlier version searched Mods\ instead of
        // Mods\ArtOfSimRally\, found nothing, silently fell through to the plugin
        // folder, and loaded a stale copy left there by an older install - which
        // then failed on an export that copy predated. Normalise the path instead
        // of trusting its shape, and try the parent too.
        private static string ResolveDllPath(string modDir)
        {
            if (!string.IsNullOrEmpty(modDir))
            {
                string dir = modDir.TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string beside = Path.Combine(dir, Dll + ".dll");
                if (File.Exists(beside)) return beside;

                string parent = Path.GetDirectoryName(dir);
                if (!string.IsNullOrEmpty(parent))
                {
                    beside = Path.Combine(parent, Dll + ".dll");
                    if (File.Exists(beside)) return beside;
                }
            }

            string dataDir = UnityEngine.Application.dataPath; // <game>/artofrally_Data
            return Path.Combine(dataDir, "Plugins", "x86_64", Dll + ".dll");
        }
    }
}
