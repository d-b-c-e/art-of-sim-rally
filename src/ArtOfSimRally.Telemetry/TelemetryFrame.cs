using System;

namespace ArtOfSimRally.Telemetry
{
    /// <summary>Per-wheel values, always front-left, front-right, rear-left, rear-right.</summary>
    /// <remarks>
    /// A struct with named corners rather than a <c>float[4]</c>: the array form makes
    /// it far too easy to fill the corners in the wrong order, which produces a
    /// dashboard that looks plausible and is silently wrong.
    /// </remarks>
    public struct WheelValues
    {
        public float FrontLeft;
        public float FrontRight;
        public float RearLeft;
        public float RearRight;

        public WheelValues(float frontLeft, float frontRight, float rearLeft, float rearRight)
        {
            FrontLeft  = frontLeft;
            FrontRight = frontRight;
            RearLeft   = rearLeft;
            RearRight  = rearRight;
        }

        /// <summary>All four corners set to the same value.</summary>
        public static WheelValues Uniform(float value) => new WheelValues(value, value, value, value);
    }

    /// <summary>
    /// One physics-frame snapshot, in neutral units, ready to be encoded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately independent of both art of rally and Forza. The mod fills this
    /// from the game's <c>CarController</c>/<c>Drivetrain</c>/<c>Wheel</c> objects,
    /// and <see cref="ForzaPacket"/> encodes it. Keeping the middle layer neutral is
    /// what lets the encoder be unit-tested without Unity present, and what would let
    /// a second output format be added without touching the game-reading code.
    /// </para>
    /// <para>
    /// Units follow Forza's conventions so the encoder stays a pure memcpy with no
    /// hidden arithmetic: metres, metres/second, radians, watts, newton-metres.
    /// The conversions from art of rally's units live in the mod, documented in
    /// docs/TELEMETRY.md.
    /// </para>
    /// </remarks>
    public struct TelemetryFrame
    {
        /// <summary>
        /// False when not driving. Consumers use this to park the dashboard, so it
        /// must go false in menus and between stages, not just stay true forever.
        /// </summary>
        public bool IsRaceOn;

        /// <summary>Milliseconds since an arbitrary origin; only deltas are meaningful.</summary>
        public uint TimestampMs;

        public float EngineMaxRpm;
        public float EngineIdleRpm;
        public float CurrentEngineRpm;

        public float AccelerationX, AccelerationY, AccelerationZ;
        public float VelocityX, VelocityY, VelocityZ;
        public float AngularVelocityX, AngularVelocityY, AngularVelocityZ;

        /// <summary>Orientation in radians.</summary>
        public float Yaw, Pitch, Roll;

        /// <summary>Suspension compression, 0 = full droop, 1 = fully compressed.</summary>
        public WheelValues NormalizedSuspensionTravel;

        public WheelValues TireSlipRatio;
        public WheelValues WheelRotationSpeed;

        /// <summary>Non-zero where the wheel is on a rumble strip. Encoded as s32.</summary>
        public WheelValues WheelOnRumbleStrip;

        public WheelValues WheelInPuddleDepth;

        /// <summary>
        /// Surface roughness, 0..1. This is the field bass shakers and rumble motors
        /// key off, so on a rally stage it carries most of the surface texture.
        /// </summary>
        public WheelValues SurfaceRumble;

        public WheelValues TireSlipAngle;
        public WheelValues TireCombinedSlip;
        public WheelValues SuspensionTravelMeters;

        public int CarOrdinal;
        public int CarClass;
        public int CarPerformanceIndex;

        /// <summary>0 = FWD, 1 = RWD, 2 = AWD.</summary>
        public int DrivetrainType;

        public int NumCylinders;

        public float PositionX, PositionY, PositionZ;

        /// <summary>Metres per second. The single field most consumers actually read.</summary>
        public float Speed;

        /// <summary>Watts.</summary>
        public float Power;

        /// <summary>Newton-metres.</summary>
        public float Torque;

        public WheelValues TireTemp;

        public float Boost;
        public float Fuel;
        public float DistanceTraveled;

        public float BestLap;
        public float LastLap;
        public float CurrentLap;
        public float CurrentRaceTime;

        public ushort LapNumber;
        public byte   RacePosition;

        /// <summary>Throttle, 0..255.</summary>
        public byte Accel;

        /// <summary>Brake, 0..255.</summary>
        public byte Brake;

        /// <summary>Clutch, 0..255.</summary>
        public byte Clutch;

        /// <summary>Handbrake, 0..255.</summary>
        public byte HandBrake;

        /// <summary>0 = reverse, 1..n = forward gears. Neutral is reported as 0 by Forza too.</summary>
        public byte Gear;

        /// <summary>Steering, -127 (full left) to 127 (full right).</summary>
        public sbyte Steer;

        public sbyte NormalizedDrivingLine;
        public sbyte NormalizedAIBrakeDifference;

        /// <summary>
        /// Scales a 0..1 pedal input to the 0..255 byte Forza expects, clamping first.
        /// Art of rally's inputs are nominally 0..1 but smoothing can overshoot, and an
        /// unchecked cast would wrap 1.01 round to a near-zero throttle.
        /// </summary>
        public static byte ToPedal(float value01)
        {
            if (value01 <= 0f) return 0;
            if (value01 >= 1f) return 255;
            return (byte)(value01 * 255f + 0.5f);
        }

        /// <summary>
        /// Scales a -1..1 steering input to the -127..127 range Forza expects, clamping first.
        /// </summary>
        public static sbyte ToSteer(float valueNeg1To1)
        {
            if (valueNeg1To1 <= -1f) return -127;
            if (valueNeg1To1 >=  1f) return  127;
            return (sbyte)(valueNeg1To1 * 127f + (valueNeg1To1 >= 0f ? 0.5f : -0.5f));
        }
    }
}
