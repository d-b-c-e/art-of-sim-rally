using System;
using Dbce.Wheel.Telemetry;
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
    /// The encoder itself lives in Dbce.Wheel.Telemetry (dbce-wheel-mod-toolkit), which has no Unity
    /// dependency and is unit-tested without the game.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CarDynamics), "FixedUpdate")]
    internal static class TelemetryPump
    {
        private static TelemetrySender _sender;
        private static bool _senderFailed;

        // What the live socket was actually created with. Compared against the
        // settings while idle. Keep a working endpoint during driving, then
        // switch after pausing without resolving DNS in the physics callback.
        private static string _senderHost;
        private static int _senderPort;

        // Components live on the same GameObject as CarDynamics but are private
        // there, so fetch them once rather than reflecting every physics step.
        private static CarDynamics _cachedFor;
        private static Drivetrain  _drivetrain;
        private static Rigidbody   _body;

        private static readonly TelemetryMotion Motion = new TelemetryMotion();
        private static uint    _timestampMs;
        private static float   _distanceTravelled;

        [HarmonyPostfix]
        private static void Emit(CarDynamics __instance)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.TelemetryEnabled) return;
            if (_sender == null || _senderFailed) return;

            try
            {
                if (!ReferenceEquals(_cachedFor, __instance))
                {
                    _cachedFor  = __instance;
                    _drivetrain = __instance.GetComponent<Drivetrain>();
                    _body       = __instance.GetComponent<Rigidbody>();
                    Motion.Reset();
                    _distanceTravelled = 0f;
                }

                SendFrame(BuildFrame(__instance));
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        private static void SendFrame(TelemetryFrame frame)
        {
            // The shared sender reports failure with false, not an exception.
            // Observe that contract so a dead socket is not retried every step.
            if (_sender != null && !_sender.Send(frame))
                Failed(new System.IO.IOException(_sender.LastError ?? "Telemetry send failed"));
        }

        // A failed destination stays quiet until it changes or telemetry is
        // explicitly restarted. Remember failed attempts too, so an invalid
        // hostname/port cannot trigger connection work on every physics step.
        private static bool EnsureSender(Settings cfg)
        {
            if (GameState.IsDriving) return _sender != null && !_senderFailed;
            bool changed = _senderPort != cfg.TelemetryPort ||
                !string.Equals(_senderHost, cfg.TelemetryHost, StringComparison.Ordinal);
            if (_senderFailed && !changed) return false;
            try
            {
                if (changed) { Park(); Shutdown(); }
                if (_sender == null)
                {
                    if (string.IsNullOrEmpty(cfg.TelemetryHost)) return false;
                    _senderHost = cfg.TelemetryHost;
                    _senderPort = cfg.TelemetryPort;
                    _sender = new TelemetrySender(_senderHost, _senderPort);
                    _senderFailed = false;
                    ModLog.Info($"Telemetry -> udp://{_senderHost}:{_senderPort}");
                }
                return true;
            }
            catch (Exception ex) { Failed(ex); return false; }
        }

        /// <summary>Prepare only after the idle watchdog releases force.</summary>
        public static void Prepare()
        {
            var cfg = Main.Settings;
            if (Main.Enabled && cfg != null && cfg.TelemetryEnabled && !GameState.IsDriving)
                EnsureSender(cfg);
        }

        /// <summary>Disabling telemetry must park even if driving continues.</summary>
        public static void StopIfDisabled()
        {
            var cfg = Main.Settings;
            if ((!Main.Enabled || cfg == null || !cfg.TelemetryEnabled) && (_sender != null || _senderFailed))
            {
                Park(); Shutdown();
            }
        }

        public static bool EndpointPending => Main.Settings != null && Main.Settings.TelemetryEnabled &&
            (_senderPort != Main.Settings.TelemetryPort || !string.Equals(_senderHost, Main.Settings.TelemetryHost, StringComparison.Ordinal));

        private static void Failed(Exception ex)
        {
            _senderFailed = true;
            _sender?.Dispose();
            _sender = null;
            ModLog.Error($"Telemetry disabled after error: {ex.Message}");
        }

        private static TelemetryFrame BuildFrame(CarDynamics cd)
        {
            float dt = Time.fixedDeltaTime;
            _timestampMs += (uint)Mathf.Max(1, Mathf.RoundToInt(dt * 1000f));

            if (!GameState.IsEngineLive || GameState.IsRestarting)
            {
                Motion.Reset();
                return new TelemetryFrame { IsRaceOn = false, TimestampMs = _timestampMs };
            }

            var velocity = _body != null ? _body.velocity : Vector3.zero;
            var t = cd.transform;
            var rotation = t.rotation;
            // Differentiate in world space before projecting into the vehicle's
            // current orientation. Rotating coordinates alone is not acceleration.
            var accel = Motion.Acceleration(t.position, velocity, Time.fixedTime);

            float speed = cd.velo;                       // metres/second
            _distanceTravelled += speed * dt;

            var euler = rotation.eulerAngles;

            var axles = cd.axles;
            var frame = new TelemetryFrame
            {
                // False in menus, cutscenes, pauses and replays, so dashboards
                // park and motion rigs stop instead of reacting to an AI-driven car.
                IsRaceOn    = GameState.IsEngineLive,   // on the line too - see GameState
                TimestampMs = _timestampMs,

                EngineMaxRpm     = _drivetrain != null ? _drivetrain.maxRPM : 0f,
                EngineIdleRpm    = _drivetrain != null ? _drivetrain.minRPM : 0f,
                CurrentEngineRpm = _drivetrain != null ? _drivetrain.rpm    : 0f,

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
            TelemetrySampling.FillMotion(ref frame, velocity, accel,
                _body != null ? _body.angularVelocity : Vector3.zero, rotation);

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

            // The game stores compression in meters; Forza also needs its ratio
            // to each wheel's available suspension travel (0 extended, 1 compressed).
            TelemetrySampling.FillSuspension(ref frame,
                new WheelValues(fl.compression, fr.compression, rl.compression, rr.compression),
                new WheelValues(fl.suspensionTravel, fr.suspensionTravel, rl.suspensionTravel, rr.suspensionTravel));

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
            Motion.Reset();
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

        /// <summary>Where packets are going right now, or null if not sending.</summary>
        public static string ActiveEndpoint =>
            _sender == null ? null : _senderHost + ":" + _senderPort;

        /// <summary>Packets successfully sent on the current socket.</summary>
        public static long PacketsSent => _sender?.PacketsSent ?? 0;

        /// <summary>Closes the socket. Called from the mod's teardown.</summary>
        public static void Shutdown()
        {
            Motion.Reset();
            _sender?.Dispose();
            _sender = null;
            _senderHost = null;
            _senderPort = 0;
            _senderFailed = false;
            _cachedFor = null;
        }
    }
}
