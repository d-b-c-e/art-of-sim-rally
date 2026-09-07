#nullable disable
using System;
using System.IO;
using System.Runtime.InteropServices;
using Dbce.Wheel.Ffb;

namespace ArtOfSimRally.Mod
{
    /// <summary>Game paths, settings and logging around the shared native binding.</summary>
    internal static class FfbNative
    {
        private const string FileName = "UnityForceFeedback.dll";
        [DllImport("user32")] private static extern IntPtr GetForegroundWindow();
        public const int ForceMax = 10000;
        public static bool Ready => WheelFfbNative.Ready;
        public static string RequestedPath { get; private set; } = "(no preload requested)";
        public static string Status { get; private set; } = "";
        private static WheelFfbNative.DeviceInfo[] _devices = new WheelFfbNative.DeviceInfo[0];

        // Loading binds exports but does not acquire a wheel. Input and the shifter
        // need this even when force feedback is disabled.
        public static bool Load(string modDir)
        {
            RequestedPath = ResolveDllPath(modDir);
            if (!WheelFfbNative.Load(Path.GetDirectoryName(RequestedPath), FileName))
            {
                Status = WheelFfbNative.LastError;
                ModLog.Error("Native toolkit unavailable: " + Status);
                return false;
            }
            WheelFfbNative.LogTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArtOfSimRally", "ffb.log"));
            return true;
        }

        public static bool Initialise(string modDir, string preferredDevice = null,
                                      int preferredIndex = -1, string preferredGuid = "")
        {
            if (Ready) return true;
            if (!Load(modDir)) return false;
            Guid? identity = null;
            if (!string.IsNullOrEmpty(preferredGuid))
            {
                if (!Guid.TryParse(preferredGuid, out Guid parsed))
                {
                    Status = "Saved wheel identity is invalid. Choose the wheel again.";
                    ModLog.Warning(Status);
                    return false;
                }
                identity = parsed;
            }
            bool ready = WheelFfbNative.Initialise(preferredDevice ?? "", preferredIndex,
                unchecked((int)GetForegroundWindow().ToInt64()), identity);
            Status = ready ? "Connected" : WheelFfbNative.LastError;
            if (ready)
            {
                WheelFfbNative.AutoCentre(false);
                ModLog.Info("Force feedback device initialised through the toolkit.");
            }
            else ModLog.Warning("Force feedback unavailable: " + Status);
            return ready;
        }

        public static void SetForce(int x)
        {
            if (!Ready) return;
            WheelFfbNative.SetForce(Math.Max(-ForceMax, Math.Min(ForceMax, x)));
        }

        public static string[] ListDevices()
        {
            _devices = WheelFfbNative.ListFfbDevices();
            return Array.ConvertAll(_devices, d => d.Name);
        }
        public static string[] ListDeviceLabels(string[] names)
            => Array.ConvertAll(_devices, d => d.Label);
        public static string DeviceGuid(int position)
            => position >= 0 && position < _devices.Length ? _devices[position].InstanceGuid?.ToString() ?? "" : "";
        public static int SelectedPosition(Settings settings)
        {
            if (!string.IsNullOrEmpty(settings.PreferredDeviceGuid))
            {
                if (!Guid.TryParse(settings.PreferredDeviceGuid, out Guid guid)) return -1;
                return new DevicePreference { InstanceGuid = guid }.Choose(_devices);
            }
            return settings.PreferredDeviceIndex;
        }

        public static bool Reinitialise(string modDir, string name, int index, string guid = "")
        {
            Shutdown();
            FfbController.Reset();
            return Initialise(modDir, name, index, guid);
        }
        public static void Shutdown()
        {
            WheelFfbNative.Zero();
            WheelFfbNative.Stop();
            WheelFfbNative.Shutdown();
        }
        public static void ReleaseInputs() => WheelFfbNative.ShutdownAll();

        private static string ResolveDllPath(string modDir)
        {
            if (!string.IsNullOrEmpty(modDir))
            {
                string dir = modDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string beside = Path.Combine(dir, FileName);
                if (File.Exists(beside)) return Path.GetFullPath(beside);
                string parent = Path.GetDirectoryName(dir);
                if (!string.IsNullOrEmpty(parent))
                {
                    beside = Path.Combine(parent, FileName);
                    if (File.Exists(beside)) return Path.GetFullPath(beside);
                }
            }
            return Path.Combine(UnityEngine.Application.dataPath, "Plugins", "x86_64", FileName);
        }
    }
}
