using System.Text;
using Dbce.Wheel.Ffb;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class LandingController
    {
        private sealed class ToolkitOutput : ILandingOutput
        {
            public int Create(int hz, int milliseconds) => WheelFfbNative.CreatePeriodicBurst(hz, milliseconds);
            public bool Play(int slot, float magnitude, float hz) => WheelFfbNative.PlayPeriodicBurst(slot, magnitude, hz);
            public bool Stop(int slot) => WheelFfbNative.StopPeriodicBurst(slot);
            // This consumer owns only one periodic slot. Steering is separate.
            public void Release() => WheelFfbNative.ReleasePeriodics();
        }

        private static readonly LandingSignal Signal = new LandingSignal();
        private static readonly LandingFeedback Feedback = new LandingFeedback(new ToolkitOutput());
        private static CarDynamics _car;
        private static Rigidbody _body;
        private static float _lastRealtime = -1;
        public static string Status => Feedback.Status;
        private static bool Enabled => Main.Enabled && Main.Settings != null && Main.Settings.ForceFeedbackEnabled &&
            Main.Settings.LandingEffectsEnabled && Main.Settings.LandingStrength > 0;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Tick()
        {
            bool driving = GameState.IsDriving;
            Feedback.Prepare(Enabled, FfbNative.Ready, !driving && Application.isFocused);
            if (!Enabled || !FfbNative.Ready || !Application.isFocused || !driving || GameState.IsRestarting)
                Reset();
            Feedback.Tick(Time.realtimeSinceStartup);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Observe(CarDynamics car)
        {
            if (!Enabled || !FfbNative.Ready || !Feedback.Available || !Application.isFocused ||
                !GameState.IsDriving || GameState.IsRestarting) { Reset(); return; }
            float now = Time.realtimeSinceStartup;
            if (float.IsNaN(now) || float.IsInfinity(now) || now < 0) { Reset(); return; }
            if (_lastRealtime >= 0 && (now < _lastRealtime || now - _lastRealtime > .25f)) Reset();
            if (_car != car)
            {
                Reset(); _car = car; _body = car.GetComponent<Rigidbody>();
            }
            _lastRealtime = now;
            var front = car.axles?.frontAxle; var rear = car.axles?.rearAxle;
            if (_body == null || front?.leftWheel == null || front.rightWheel == null ||
                rear?.leftWheel == null || rear.rightWheel == null) { Reset(); return; }
            var p = _body.position; var v = _body.velocity;
            var up = _body.rotation * Vector3.up;
            float intensity = Signal.Observe(new LandingSample
            {
                Time = Time.fixedTime, X = p.x, Y = p.y, Z = p.z,
                Vx = v.x, Vy = v.y, Vz = v.z, UpY = up.y,
                Contacts = (front.leftWheel.onGroundDown ? 1 : 0) | (front.rightWheel.onGroundDown ? 2 : 0) |
                    (rear.leftWheel.onGroundDown ? 4 : 0) | (rear.rightWheel.onGroundDown ? 8 : 0)
            });
            if (Signal.Discontinuous) Feedback.Stop();
            if (intensity <= 0) return;
            bool accepted = Feedback.Trigger(intensity, Main.Settings.LandingStrength, now);
            if (Main.Settings.DiagnosticLogging)
                ModLog.Info($"Landing FFB air={Signal.LastAirSeconds:F3}s descent={Signal.LastDescentMps:F2}m/s " +
                    $"magnitude={Feedback.LastMagnitude:F4} duration={LandingFeedback.DurationMs}ms accepted={accepted}");
        }

        public static void Reset()
        {
            Signal.Reset(); Feedback.Stop(); _car = null; _body = null; _lastRealtime = -1;
        }
        public static void Shutdown() { Reset(); Feedback.Shutdown(); }
        public static void AppendSupport(StringBuilder text)
        {
            text.AppendLine("=== Landing vibration ===");
            text.AppendLine("Status: " + Status);
            text.AppendLine($"Session events: {Feedback.Events}; driver accepted: {Feedback.Accepted}; rejected: {Feedback.Rejected}");
            text.AppendLine($"Last requested magnitude: {Feedback.LastMagnitude:F4}; sine {LandingFeedback.Frequency} Hz; duration {LandingFeedback.DurationMs} ms");
            text.AppendLine("Driver acceptance is not measured wheel motion. Steering capture does not record the separate periodic output.");
            text.AppendLine();
        }
    }
}
