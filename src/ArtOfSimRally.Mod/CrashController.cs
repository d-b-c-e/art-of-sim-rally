using System;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    [HarmonyPatch(typeof(PlayerCollider), "OnCollisionEnter")]
    internal static class CrashController
    {
        private static readonly CrashSignal Signal = new CrashSignal();
        private static CarDynamics _car;
        private static Rigidbody _body;
        private static float _lastRealtime = -1;
        private static bool _faulted;
        public static string Status => _faulted ? "Crash observation unavailable; create a support file" : ImpactController.Status(ImpactKind.Crash);
        private static bool Active => !_faulted && ImpactController.Available(ImpactKind.Crash) && FfbNative.Ready;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Track(CarDynamics car)
        {
            if (!Active || !Application.isFocused || !GameState.IsDriving || GameState.IsRestarting) { Reset(); return; }
            if (_car != car)
            {
                var body = car.GetComponent<Rigidbody>();
                if (body == null || body != GameState.ExistingManager?.playerManager?.playerRigidBody) return;
                Reset(); _car = car; _body = body;
            }
            float now = Time.realtimeSinceStartup;
            if (!Finite(now) || now < 0) { Reset(); return; }
            if (_lastRealtime >= 0 && (now < _lastRealtime || now - _lastRealtime > .25f))
            { Signal.Reset(); ImpactController.Stop(ImpactKind.Crash); }
            _lastRealtime = now;
            if (_body == null) { Reset(); return; }
            var p = _body.position; var v = _body.velocity;
            Signal.Track(new LandingSample { Time = Time.fixedTime, X = p.x, Y = p.y, Z = p.z, Vx = v.x, Vy = v.y, Vz = v.z });
            if (Signal.Discontinuous) ImpactController.Stop(ImpactKind.Crash);
        }

        // Observe before the original, which can finish the stage. Never call
        // game rumble/damage methods or change collision arguments/physics.
        [HarmonyPrefix]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void BeforeCollision(PlayerCollider __instance, Collision __0)
        {
            if (!Active) return;
            try
            {
                if (!Application.isFocused || !GameState.IsDriving || GameState.IsRestarting) { Reset(); return; }
                float now = Time.realtimeSinceStartup;
                if (!Finite(now) || _lastRealtime < 0 || now < _lastRealtime || now - _lastRealtime > .25f ||
                    _body == null || __instance == null || __0 == null) return;
                if (__instance.GetComponent<Rigidbody>() != _body ||
                    _body != GameState.ExistingManager?.playerManager?.playerRigidBody) return;
                var other = __0.collider;
                if (other == null || other.CompareTag("Road")) return;
                var relative = __0.relativeVelocity;
                CrashContact selected = default; float strongest = -1;
                // Bound work and avoid Collision.contacts' allocated array.
                for (int i = 0; i < Math.Min(__0.contactCount, 8); i++)
                {
                    var normal = __0.GetContact(i).normal;
                    if (Math.Abs(normal.y) > .65f) continue;
                    float speed = Math.Abs(Vector3.Dot(relative, normal));
                    if (speed > strongest)
                    {
                        strongest = speed;
                        selected = new CrashContact { Time = Time.fixedTime, Road = false,
                            Rvx = relative.x, Rvy = relative.y, Rvz = relative.z,
                            Nx = normal.x, Ny = normal.y, Nz = normal.z };
                    }
                }
                if (strongest < 0) return;
                float intensity = Signal.Observe(selected);
                if (intensity <= 0) return;
                var result = ImpactController.Trigger(ImpactKind.Crash, intensity, Main.Settings.CrashStrength, now);
                if (Main.Settings.DiagnosticLogging)
                    ModLog.Info($"Crash FFB normalSpeed={Signal.LastNormalSpeed:F2}m/s intensity={intensity:F3} " +
                        $"magnitude={ImpactController.Magnitude(ImpactKind.Crash):F4} duration={LandingFeedback.DurationMs}ms result={result}");
            }
            catch (Exception ex)
            {
                _faulted = true; Reset();
                ModLog.Warning("Crash observation disabled for this session: " + ex.Message);
            }
        }
        public static void Reset()
        {
            Signal.Reset(); ImpactController.Stop(ImpactKind.Crash);
            _car = null; _body = null; _lastRealtime = -1;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
