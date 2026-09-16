using UnityEngine;

namespace ArtOfSimRally.Mod
{
    internal static class LandingController
    {
        private static readonly LandingSignal Signal = new LandingSignal();
        private static CarDynamics _car;
        private static Rigidbody _body;
        private static float _lastRealtime = -1;
        public static string Status => ImpactController.Status(ImpactKind.Landing);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Observe(CarDynamics car)
        {
            if (!ImpactController.Available(ImpactKind.Landing) || !FfbNative.Ready || !Application.isFocused ||
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
            if (Signal.Discontinuous) ImpactController.Stop(ImpactKind.Landing);
            if (intensity <= 0) return;
            var result = ImpactController.Trigger(ImpactKind.Landing, intensity, Main.Settings.LandingStrength, now);
            if (Main.Settings.DiagnosticLogging)
                ModLog.Info($"Landing FFB air={Signal.LastAirSeconds:F3}s descent={Signal.LastDescentMps:F2}m/s " +
                    $"magnitude={ImpactController.Magnitude(ImpactKind.Landing):F4} duration={LandingFeedback.DurationMs}ms accepted={result == ImpactResult.Accepted} result={result}");
        }

        public static void Reset()
        {
            Signal.Reset(); ImpactController.Stop(ImpactKind.Landing); _car = null; _body = null; _lastRealtime = -1;
        }
    }
}
