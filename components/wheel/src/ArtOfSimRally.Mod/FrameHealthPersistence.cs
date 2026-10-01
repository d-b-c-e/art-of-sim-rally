using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class FrameHealthPersistence
    {
        private static FrameHealthStore _store;
        internal static void Initialize()
        {
            FrameHealth.Current.Reset();
            try
            {
                var identity = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    typeof(FrameHealthPersistence).Assembly, typeof(AssemblyInformationalVersionAttribute));
                _store = new FrameHealthStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ArtOfSimRally", "last-session-frame-health.xml"), identity?.InformationalVersion ?? "unknown", DateTime.UtcNow);
            }
            catch (Exception ex) { _store = null; ModLog.Warning("Diagnostic retention unavailable: " + ex.GetType().Name); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Flush(bool shutdown = false)
        {
            _store?.Flush(FrameHealth.Current, !shutdown && GameState.IsDriving, shutdown, Time.realtimeSinceStartup, DateTime.UtcNow);
        }

        internal static void AppendPrevious(StringBuilder output) => _store?.AppendPrevious(output);
    }
}
