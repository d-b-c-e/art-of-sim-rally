using System;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Logging indirection so the patches stay free of any mod-loader types.
    /// </summary>
    /// <remarks>
    /// The patch classes are the valuable part of this mod and the part most
    /// likely to outlive a loader choice. Routing their output through here means
    /// swapping Unity Mod Manager for BepInEx, or supporting both, touches one
    /// file rather than every patch.
    /// </remarks>
    internal static class ModLog
    {
        private static Action<string> _info    = _ => { };
        private static Action<string> _warning = _ => { };
        private static Action<string> _error   = _ => { };

        /// <summary>Points logging at a loader's sinks. Called once at startup.</summary>
        public static void Attach(Action<string> info, Action<string> warning, Action<string> error)
        {
            if (info    != null) _info    = info;
            if (warning != null) _warning = warning;
            if (error   != null) _error   = error;
        }

        public static void Info(string message)    => _info(message);
        public static void Warning(string message) => _warning(message);
        public static void Error(string message)   => _error(message);
    }
}
