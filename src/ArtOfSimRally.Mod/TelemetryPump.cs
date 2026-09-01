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
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.TelemetryEnabled || _senderFailed) return;

            try
            {
                if (_sender == null)
                {
                    _sender = new TelemetrySender(cfg.TelemetryHost, cfg.TelemetryPort);
                    ModLog.Info(
                        $"Telemetry -> udp://{cfg.TelemetryHost}:{cfg.TelemetryPort}");
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
                ModLog.Error($"Telemetry disabled after error: {ex.Message}");
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
                // False in menus, cutscenes, pauses and replays, so dashboards
                // park and motion rigs stop instead of reacting to an AI-driven car.
                IsRaceOn    = GameState.IsDriving,
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
                FillWheels(ref frame, axles, speed);

            return frame;
        }

        // Forza's arrays are front-left, front-right, rear-left, rear-right. The
        // game exposes the same corners via frontAxle/rearAxle, so map explicitly
        // rather than trusting allWheels ordering.
        private static void FillWheels(ref TelemetryFrame frame, Axles axles, float speed)
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

            // Compression is the normalised form Forza expects here, and it is
            // what suspension-driven shaker effects actually read.
            frame.NormalizedSuspensionTravel = new WheelValues(
                Mathf.Clamp01(fl.compression), Mathf.Clamp01(fr.compression),
                Mathf.Clamp01(rl.compression), Mathf.Clamp01(rr.compression));

            frame.WheelRotationSpeed = new WheelValues(
                fl.angularVelocity, fr.angularVelocity,
                rl.angularVelocity, rr.angularVelocity);

            frame.WheelInPuddleDepth = new WheelValues(
                fl.isOnPuddle ? 1f : 0f, fr.isOnPuddle ? 1f : 0f,
                rl.isOnPuddle ? 1f : 0f, rr.isOnPuddle ? 1f : 0f);

            // SurfaceRumble is the road-texture channel bass shakers key off. The
            // game has no such signal, but it does classify the surface under each
            // wheel, which on a rally stage carries most of the information -
            // gravel and offroad should shake, dry tarmac should not. Scaled by
            // speed so a stationary car is silent rather than buzzing.
            float v = Mathf.Clamp01(speed / 30f);
            frame.SurfaceRumble = new WheelValues(
                Roughness(fl) * v, Roughness(fr) * v,
                Roughness(rl) * v, Roughness(rr) * v);
        }

        private static float Combined(Wheel w)
            => Mathf.Sqrt(w.slipRatio * w.slipRatio + w.slipAngle * w.slipAngle);

        // Rough surface-texture weighting. Deliberately coarse: it drives haptics,
        // not physics, and the ordering matters far more than the exact values.
        private static float Roughness(Wheel w)
        {
            switch (w.surfaceType)
            {
                case CarDynamics.SurfaceType.tarmacdry:
                case CarDynamics.SurfaceType.tarmacwet: return 0.05f;
                case CarDynamics.SurfaceType.snow:      return 0.30f;
                case CarDynamics.SurfaceType.gravel:    return 0.55f;
                case CarDynamics.SurfaceType.snow_off:  return 0.55f;
                case CarDynamics.SurfaceType.grass:     return 0.65f;
                case CarDynamics.SurfaceType.offroad:   return 0.85f;
                default:                                return 0.30f;
            }
        }

        /// <summary>
        /// Sends a single zeroed, race-off frame so consumers park instead of
        /// freezing on the last real values.
        /// </summary>
        /// <remarks>
        /// UDP has no delivery guarantee and no close notification, so a consumer
        /// that simply stops hearing from us keeps showing whatever arrived last -
        /// a dashboard stuck at the speed the game quit at. An explicit empty frame
        /// is the only way to tell it we are done. Sent more than once because a
        /// single dropped packet would undo it.
        /// </remarks>
        public static void Park()
        {
            if (_sender == null || _senderFailed) return;
            try
            {
                var parked = new TelemetryFrame { IsRaceOn = false, TimestampMs = _timestampMs };
                for (int i = 0; i < 3; i++) _sender.Send(parked);
            }
            catch
            {
                // Best effort on the way out; nothing useful to do if it fails.
            }
        }

        /// <summary>Closes the socket. Called from the mod's teardown.</summary>
        public static void Shutdown()
        {
            _sender?.Dispose();
            _sender = null;
            _cachedFor = null;
        }
    }
}
