using System;
using ArtOfSimRally.Telemetry;
using HarmonyLib;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Reads the car's physics state each step and emits a Forza-compatible UDP packet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs off <c>CarDynamics.FixedUpdate</c> so the sample rate matches the
    /// physics rate rather than the frame rate - telemetry consumers infer
    /// derivatives from these samples and a variable rate makes them jittery.
    /// </para>
    /// <para>
    /// Field sources and unit conversions are documented in docs/TELEMETRY.md.
    /// The encoder itself lives in ArtOfSimRally.Telemetry, which has no Unity
    /// dependency and is unit-tested without the game.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CarDynamics), "FixedUpdate")]
    internal static class TelemetryPump
    {
        private static TelemetrySender _sender;
        private static bool _senderFailed;

        // Components live on the same GameObject as CarDynamics but are private
        // there, so fetch them once rather than reflecting every physics step.
        private static CarDynamics _cachedFor;
        private static Drivetrain  _drivetrain;
        private static Rigidbody   _body;

        private static Vector3 _lastVelocity;
        private static uint    _timestampMs;
        private static float   _distanceTravelled;

        [HarmonyPostfix]
        private static void Emit(CarDynamics __instance)
        {
            var cfg = Plugin.Settings;
            if (cfg == null || !cfg.TelemetryEnabled.Value || _senderFailed) return;

            try
            {
                if (_sender == null)
                {
                    _sender = new TelemetrySender(cfg.TelemetryHost.Value, cfg.TelemetryPort.Value);
                    Plugin.Log.LogInfo(
                        $"Telemetry -> udp://{cfg.TelemetryHost.Value}:{cfg.TelemetryPort.Value}");
                }

                if (!ReferenceEquals(_cachedFor, __instance))
                {
                    _cachedFor  = __instance;
                    _drivetrain = __instance.GetComponent<Drivetrain>();
                    _body       = __instance.GetComponent<Rigidbody>();
                    _lastVelocity = Vector3.zero;
                    _distanceTravelled = 0f;
                }

                _sender.Send(BuildFrame(__instance));
            }
            catch (Exception ex)
            {
                // Telemetry must never break the game. One report, then stay quiet.
                Plugin.Log.LogError($"Telemetry disabled after error: {ex.Message}");
                _senderFailed = true;
            }
        }

        private static TelemetryFrame BuildFrame(CarDynamics cd)
        {
            float dt = Time.fixedDeltaTime;
            _timestampMs += (uint)Mathf.Max(1, Mathf.RoundToInt(dt * 1000f));

            var velocity = _body != null ? _body.velocity : Vector3.zero;
            // The game does not store acceleration, so differentiate velocity.
            var accel = dt > 0f ? (velocity - _lastVelocity) / dt : Vector3.zero;
            _lastVelocity = velocity;

            float speed = cd.velo;                       // metres/second
            _distanceTravelled += speed * dt;

            var t = cd.transform;
            var euler = t.rotation.eulerAngles;

            var axles = cd.axles;
            var frame = new TelemetryFrame
            {
                IsRaceOn    = true,
                TimestampMs = _timestampMs,

                EngineMaxRpm     = _drivetrain != null ? _drivetrain.maxRPM : 0f,
                EngineIdleRpm    = _drivetrain != null ? _drivetrain.minRPM : 0f,
                CurrentEngineRpm = _drivetrain != null ? _drivetrain.rpm    : 0f,

                AccelerationX = accel.x, AccelerationY = accel.y, AccelerationZ = accel.z,
                VelocityX = velocity.x,  VelocityY = velocity.y,  VelocityZ = velocity.z,

                AngularVelocityX = _body != null ? _body.angularVelocity.x : 0f,
                AngularVelocityY = _body != null ? _body.angularVelocity.y : 0f,
                AngularVelocityZ = _body != null ? _body.angularVelocity.z : 0f,

                Yaw   = euler.y * Mathf.Deg2Rad,
                Pitch = euler.x * Mathf.Deg2Rad,
                Roll  = euler.z * Mathf.Deg2Rad,

                PositionX = t.position.x, PositionY = t.position.y, PositionZ = t.position.z,

                Speed  = speed,
                Torque = _drivetrain != null ? _drivetrain.torque : 0f,
                // Power = torque * angular velocity, in watts.
                Power  = _drivetrain != null
                            ? _drivetrain.torque * _drivetrain.rpm * Mathf.PI / 30f
                            : 0f,

                DistanceTraveled = _distanceTravelled,
                CurrentRaceTime  = Time.timeSinceLevelLoad,
                CurrentLap       = Time.timeSinceLevelLoad,
                LapNumber        = 1,
                RacePosition     = 1,
                DrivetrainType   = 2,   // rally default; refine from powered axles later
                NumCylinders     = 4,
            };

            // gearRatios is [reverse, neutral, 1st, 2nd, ...], so Drivetrain.gear
            // is an index, not a gear number. Forza reports 0 for both reverse and
            // neutral, so anything below first collapses to 0.
            if (_drivetrain != null)
                frame.Gear = (byte)Mathf.Max(0, _drivetrain.gear - 1);

            var cc = cd.carController;
            if (cc != null)
            {
                frame.Accel     = TelemetryFrame.ToPedal(cc.throttleInput);
                frame.Brake     = TelemetryFrame.ToPedal(cc.brakeInput);
                frame.Clutch    = TelemetryFrame.ToPedal(cc.clutchInput);
                frame.HandBrake = TelemetryFrame.ToPedal(cc.handbrakeInput);
                frame.Steer     = TelemetryFrame.ToSteer(cc.steerInput);
            }

            if (axles?.allWheels != null && axles.allWheels.Length >= 4)
                FillWheels(ref frame, axles);

            return frame;
        }

        // Forza's arrays are front-left, front-right, rear-left, rear-right. The
        // game exposes the same corners via frontAxle/rearAxle, so map explicitly
        // rather than trusting allWheels ordering.
        private static void FillWheels(ref TelemetryFrame frame, Axles axles)
        {
            var fl = axles.frontAxle?.leftWheel;
            var fr = axles.frontAxle?.rightWheel;
            var rl = axles.rearAxle?.leftWheel;
            var rr = axles.rearAxle?.rightWheel;
            if (fl == null || fr == null || rl == null || rr == null) return;

            frame.TireSlipRatio = new WheelValues(
                fl.slipRatio, fr.slipRatio, rl.slipRatio, rr.slipRatio);

            frame.TireSlipAngle = new WheelValues(
                fl.slipAngle, fr.slipAngle, rl.slipAngle, rr.slipAngle);

            frame.TireCombinedSlip = new WheelValues(
                Combined(fl), Combined(fr), Combined(rl), Combined(rr));

            frame.SuspensionTravelMeters = new WheelValues(
                fl.suspensionTravel, fr.suspensionTravel,
                rl.suspensionTravel, rr.suspensionTravel);

            frame.WheelInPuddleDepth = new WheelValues(
                fl.isOnPuddle ? 1f : 0f, fr.isOnPuddle ? 1f : 0f,
                rl.isOnPuddle ? 1f : 0f, rr.isOnPuddle ? 1f : 0f);
        }

        private static float Combined(Wheel w)
            => Mathf.Sqrt(w.slipRatio * w.slipRatio + w.slipAngle * w.slipAngle);

        /// <summary>Closes the socket. Called from the plugin's teardown.</summary>
        public static void Shutdown()
        {
            _sender?.Dispose();
            _sender = null;
            _cachedFor = null;
        }
    }
}
