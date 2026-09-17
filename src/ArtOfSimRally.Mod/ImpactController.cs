using System;
using System.Diagnostics;
using System.Text;
using Dbce.Wheel.Ffb;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class ImpactController
    {
        private sealed class ToolkitOutput : ILandingOutput
        {
            public int Create(ImpactKind kind, int hz, int milliseconds) => kind == ImpactKind.Crash
                ? WheelFfbNative.CreateConstantBurst(milliseconds) : WheelFfbNative.CreatePeriodicBurst(hz, milliseconds);
            public bool Play(ImpactKind kind, int slot, float magnitude, float hz) => kind == ImpactKind.Crash
                ? WheelFfbNative.PlayConstantBurst(slot, magnitude) : WheelFfbNative.PlayPeriodicBurst(slot, magnitude, hz);
            public bool Stop(ImpactKind kind, int slot) => kind == ImpactKind.Crash
                ? WheelFfbNative.StopConstantBurst(slot) : WheelFfbNative.StopPeriodicBurst(slot);
            // The mixer is the sole owner of both impact effect families.
            public void Release() { WheelFfbNative.ReleaseConstantBursts(); WheelFfbNative.ReleasePeriodics(); }
        }
        internal static Func<double> MonotonicNow = () => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        private static readonly ImpactMixer Mixer = new ImpactMixer(new ToolkitOutput(), () => MonotonicNow(), LogDelivery);
        private static void LogDelivery(ImpactDelivery delivery)
        {
            if (delivery.Action == "reject")
                ModLog.Warning($"{delivery.Kind} impact output rejected ({delivery.Reason}): {WheelFfbNative.LastError}");
            if (Main.Settings == null || !Main.Settings.DiagnosticLogging) return;
            ModLog.Info($"Impact output kind={delivery.Kind} action={delivery.Action} reason={delivery.Reason} " +
                $"magnitude={delivery.Magnitude:F4} playCall={delivery.PlayLatencyMs:F2}ms elapsedAfterReturn={delivery.ElapsedMs:F2}ms " +
                $"steering={FfbController.CurrentForce:F4}; command timing, not measured wheel motion");
        }
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
            string stop = !Main.Enabled ? "mod-disabled" : Main.Settings == null || !Main.Settings.ForceFeedbackEnabled ? "ffb-disabled" :
                !FfbNative.Ready ? "device-unavailable" : !Application.isFocused ? "focus-lost" :
                GameState.IsRestarting ? "restart" : !driving ? "not-driving" :
                (!Enabled(ImpactKind.Landing) && !Enabled(ImpactKind.Crash)) ? "impacts-disabled" : null;
            if (stop != null) Reset(stop);
            Mixer.Prepare(Enabled(ImpactKind.Landing), Enabled(ImpactKind.Crash), FfbNative.Ready,
                !driving && Application.isFocused);
            if (!Enabled(ImpactKind.Landing)) LandingController.Reset();
            if (!Enabled(ImpactKind.Crash)) CrashController.Reset();
            Mixer.Tick(Time.realtimeSinceStartup);
        }
        public static void Stop(ImpactKind kind, string reason = "detector-reset") => Mixer.Stop(kind, reason);
        public static void Reset(string reason = "force-reset") { Mixer.Stop(reason); LandingController.Reset(); CrashController.Reset(); }
        public static void Shutdown() { Reset("shutdown"); Mixer.Shutdown(); }
        public static void AppendSupport(StringBuilder text)
        {
            foreach (var kind in new[] { ImpactKind.Landing, ImpactKind.Crash })
            {
                var c = Mixer.Counts(kind);
                text.AppendLine("=== " + kind + " vibration ===");
                text.AppendLine("Status: " + (kind == ImpactKind.Crash ? CrashController.Status : Status(kind)));
                text.AppendLine($"Session events: {c.Events}; driver accepted: {c.Accepted}; rejected: {c.Rejected}; overlap suppressed: {c.Suppressed}");
                string waveform = kind == ImpactKind.Crash ? "constant-force pulse; fixed positive X direction; no envelope"
                    : $"sine {LandingFeedback.Frequency} Hz; phase zero; no envelope";
                text.AppendLine($"Last requested magnitude: {c.Magnitude:F4}; {waveform}; duration {LandingFeedback.DurationMs} ms");
                if (c.HasDelivery)
                    text.AppendLine($"Last delivery: {c.Delivery.Action}/{c.Delivery.Reason}; native play call {c.Delivery.PlayLatencyMs:F2} ms; elapsed after return {c.Delivery.ElapsedMs:F2} ms; stops before 120 ms: {c.EarlyStops} (includes intended interrupts/replacements)");
            }
            text.AppendLine("One active impact; separate cached sine/constant handles; strongest cue wins and stops the old effect before replacement. Driver acceptance is not measured wheel motion.");
            text.AppendLine("Crash requests require exact finite-duration readback; rejection disables crashes until toggled off/on while paused, preserving landing availability.");
            text.AppendLine($"Strength scale: percent of nominal wheel force; landing maximum {LandingFeedback.MaximumStrengthPercent}; crash maximum {LandingFeedback.MaximumCrashStrengthPercent}, new-settings default {LandingFeedback.DefaultCrashStrengthPercent}. Separate from steering and SimHub gains.");
            text.AppendLine("Nominal caps do not guarantee headroom when steering and impacts mix. Steering capture does not record either separate impact output.");
            text.AppendLine();
            LandingController.AppendSupport(text);
        }
    }
}
