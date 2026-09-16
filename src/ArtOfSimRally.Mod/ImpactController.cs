using System.Text;
using Dbce.Wheel.Ffb;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class ImpactController
    {
        private sealed class ToolkitOutput : ILandingOutput
        {
            public int Create(int hz, int milliseconds) => WheelFfbNative.CreatePeriodicBurst(hz, milliseconds);
            public bool Play(int slot, float magnitude, float hz) => WheelFfbNative.PlayPeriodicBurst(slot, magnitude, hz);
            public bool Stop(int slot) => WheelFfbNative.StopPeriodicBurst(slot);
            // The mixer is the sole owner of periodic resources.
            public void Release() => WheelFfbNative.ReleasePeriodics();
        }
        private static readonly ImpactMixer Mixer = new ImpactMixer(new ToolkitOutput());
        public static bool Enabled(ImpactKind kind) => Main.Enabled && Main.Settings != null &&
            Main.Settings.ForceFeedbackEnabled && (kind == ImpactKind.Landing
                ? Main.Settings.LandingEffectsEnabled && Main.Settings.LandingStrength > 0
                : Main.Settings.CrashEffectsEnabled && Main.Settings.CrashStrength > 0);
        public static bool Available(ImpactKind kind) => Enabled(kind) && Mixer.Available(kind);
        public static string Status(ImpactKind kind) => Enabled(kind) ? Mixer.Status(kind) : "Off";
        public static ImpactResult Trigger(ImpactKind kind, float intensity, float strength, double now)
            => Mixer.Trigger(kind, intensity, strength, now);
        public static float Magnitude(ImpactKind kind) => Mixer.Counts(kind).Magnitude;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Tick()
        {
            bool driving = GameState.IsDriving;
            Mixer.Prepare(Enabled(ImpactKind.Landing), Enabled(ImpactKind.Crash), FfbNative.Ready,
                !driving && Application.isFocused);
            if (!FfbNative.Ready || !Application.isFocused || !driving || GameState.IsRestarting ||
                (!Enabled(ImpactKind.Landing) && !Enabled(ImpactKind.Crash))) Reset();
            if (!Enabled(ImpactKind.Landing)) LandingController.Reset();
            if (!Enabled(ImpactKind.Crash)) CrashController.Reset();
            Mixer.Tick(Time.realtimeSinceStartup);
        }
        public static void Stop(ImpactKind kind) => Mixer.Stop(kind);
        public static void Reset() { LandingController.Reset(); CrashController.Reset(); Mixer.Stop(); }
        public static void Shutdown() { Reset(); Mixer.Shutdown(); }
        public static void AppendSupport(StringBuilder text)
        {
            foreach (var kind in new[] { ImpactKind.Landing, ImpactKind.Crash })
            {
                var c = Mixer.Counts(kind);
                text.AppendLine("=== " + kind + " vibration ===");
                text.AppendLine("Status: " + (kind == ImpactKind.Crash ? CrashController.Status : Status(kind)));
                text.AppendLine($"Session events: {c.Events}; driver accepted: {c.Accepted}; rejected: {c.Rejected}; overlap suppressed: {c.Suppressed}");
                text.AppendLine($"Last requested magnitude: {c.Magnitude:F4}; sine {LandingFeedback.Frequency} Hz; duration {LandingFeedback.DurationMs} ms");
            }
            text.AppendLine("One shared finite effect; strongest cue wins. Driver acceptance is not measured wheel motion.");
            text.AppendLine("Steering capture does not record the separate periodic output.");
            text.AppendLine();
            LandingController.AppendSupport(text);
        }
    }
}
