using System;
using System.Text;
using ArtOfSimRally.Haptics;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Independent of wheel selection, FFB availability, and wheel vibration settings.
    [HarmonyPatch(typeof(CarDynamics), "FixedUpdate")]
    internal static class ShakerLanding
    {
        private static readonly LandingSignal Signal = new LandingSignal();
        private static HapticSender _sender;
        private static CarDynamics _car;
        private static Rigidbody _body;
        private static float _lastSample = -1;
        private static bool _failed;
        private static long _events;
        public static string Status => _sender != null ? "Landing signal ready for SimHub" : _failed ?
            "Landing signal unavailable; toggle off/on while paused to retry" : "Pause to prepare landing signal";
        private static bool Enabled => Main.Enabled && Main.Settings != null && Main.Settings.TelemetryEnabled &&
            Main.Settings.ShakerLandingEnabled && Main.Settings.ShakerLandingStrength > 0;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Tick()
        {
            if (!Enabled) { Shutdown(); _failed=false; return; }
            if (_sender == null && !_failed && !GameState.IsDriving)
            {
                try { _sender=new HapticSender(); }
                catch (Exception ex) { _failed=true; ModLog.Error("Shaker landing setup: " + ex.Message); }
            }
            if (_sender == null) return;
            bool active=GameState.IsDriving && !GameState.IsRestarting && Application.isFocused &&
                _lastSample >= 0 && Time.realtimeSinceStartup-_lastSample <= .1f;
            if (!active) Reset();
            // Tick never renews active telemetry: only valid physics samples do.
            if (!active) _sender.Update(false,HapticProtocol.Now);
        }

        [HarmonyPostfix]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Observe(CarDynamics __instance)
        {
            if (_sender == null) return;
            if (!Enabled || !Application.isFocused || !GameState.IsDriving || GameState.IsRestarting)
            { Park(); return; }
            float now=Time.realtimeSinceStartup;
            if (float.IsNaN(now) || float.IsInfinity(now) || now < 0) { Park(); return; }
            if (_lastSample >= 0 && (now < _lastSample || now-_lastSample > .1f)) Park();
            if (_car != __instance)
            { Park(); _car=__instance; _body=__instance.GetComponent<Rigidbody>(); }
            var front=_car.axles?.frontAxle; var rear=_car.axles?.rearAxle;
            if (_body == null || front?.leftWheel == null || front.rightWheel == null ||
                rear?.leftWheel == null || rear.rightWheel == null) { Park(); return; }
            var p=_body.position; var v=_body.velocity; var up=_body.rotation*Vector3.up;
            float intensity=Signal.Observe(new LandingSample { Time=Time.fixedTime, X=p.x,Y=p.y,Z=p.z,
                Vx=v.x,Vy=v.y,Vz=v.z,UpY=up.y,
                Contacts=(front.leftWheel.onGroundDown?1:0)|(front.rightWheel.onGroundDown?2:0)|
                    (rear.leftWheel.onGroundDown?4:0)|(rear.rightWheel.onGroundDown?8:0) });
            if (Signal.Discontinuous) { Park(); return; }
            _lastSample=now;
            int magnitude=(int)(intensity * Math.Min(100,Math.Max(0,Main.Settings.ShakerLandingStrength))*100);
            _sender.Update(true,HapticProtocol.Now,magnitude);
            if (magnitude > 0)
            {
                _events++;
                if (Main.Settings.DiagnosticLogging)
                    ModLog.Info($"Shaker landing air={Signal.LastAirSeconds:F3}s descent={Signal.LastDescentMps:F2}m/s cue={magnitude/100f:F1}% (SimHub reception not confirmed)");
            }
        }
        private static void Reset() { Signal.Reset(); _car=null; _body=null; _lastSample=-1; }
        private static void Park() { Reset(); _sender?.Update(false,HapticProtocol.Now); }
        public static void Shutdown() { Park(); _sender?.Dispose(); _sender=null; }
        public static void AppendSupport(StringBuilder text)
        {
            text.AppendLine("=== SimHub shaker landing ===");
            text.AppendLine($"{Status}; events={_events}; datagrams={_sender?.Sent ?? 0}; dropped={_sender?.Dropped ?? 0}");
            text.AppendLine("Separate loopback haptic cue, not Forza physics. Check ArtOfSimRallyHaptics properties for reception/output.");
        }
    }
}
