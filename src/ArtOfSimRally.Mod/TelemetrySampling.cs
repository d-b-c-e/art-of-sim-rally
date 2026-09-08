using System;
using Dbce.Wheel.Telemetry;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // Game sampling, not packet encoding. These helpers never change physics.
    internal static class TelemetrySampling
    {
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

        internal static float TravelMeters(float compression, float capacity)
            => !Finite(compression) || !Finite(capacity) || capacity <= 0f
                ? 0f : Math.Max(0f, Math.Min(compression, capacity));

        internal static float TravelNormalized(float compression, float capacity)
            => !Finite(capacity) || capacity <= 0f ? 0f : TravelMeters(compression, capacity) / capacity;

        internal static void FillSuspension(ref TelemetryFrame frame, WheelValues compression, WheelValues capacity)
        {
            frame.SuspensionTravelMeters = new WheelValues(
                TravelMeters(compression.FrontLeft,capacity.FrontLeft), TravelMeters(compression.FrontRight,capacity.FrontRight),
                TravelMeters(compression.RearLeft,capacity.RearLeft), TravelMeters(compression.RearRight,capacity.RearRight));
            frame.NormalizedSuspensionTravel = new WheelValues(
                TravelNormalized(compression.FrontLeft,capacity.FrontLeft), TravelNormalized(compression.FrontRight,capacity.FrontRight),
                TravelNormalized(compression.RearLeft,capacity.RearLeft), TravelNormalized(compression.RearRight,capacity.RearRight));
        }

        internal static void FillMotion(ref TelemetryFrame frame, Vector3 velocity, Vector3 acceleration, Vector3 angular, Quaternion rotation)
        {
            var v=LocalVector(velocity,rotation); var a=LocalVector(acceleration,rotation); var w=LocalVector(angular,rotation);
            frame.VelocityX=v.x; frame.VelocityY=v.y; frame.VelocityZ=v.z;
            frame.AccelerationX=a.x; frame.AccelerationY=a.y; frame.AccelerationZ=a.z;
            frame.AngularVelocityX=w.x; frame.AngularVelocityY=w.y; frame.AngularVelocityZ=w.z;
        }

        // Inverse rotation only: scale on a car's transform must not scale m/s.
        // Written in managed arithmetic so the exact projection is testable off-game.
        internal static Vector3 LocalVector(Vector3 world, Quaternion rotation)
        {
            if (!Finite(world) || !Finite(rotation.x) || !Finite(rotation.y) ||
                !Finite(rotation.z) || !Finite(rotation.w)) return Vector3.zero;
            double x=rotation.x, y=rotation.y, z=rotation.z, w=rotation.w;
            double norm=x*x+y*y+z*z+w*w;
            if (norm < 1e-12) return Vector3.zero;
            double s=2.0/norm;
            var local = new Vector3(
                (float)((1-s*(y*y+z*z))*world.x + s*(x*y+z*w)*world.y + s*(x*z-y*w)*world.z),
                (float)(s*(x*y-z*w)*world.x + (1-s*(x*x+z*z))*world.y + s*(y*z+x*w)*world.z),
                (float)(s*(x*z+y*w)*world.x + s*(y*z-x*w)*world.y + (1-s*(x*x+y*y))*world.z));
            return Finite(local) ? local : Vector3.zero;
        }
    }

    internal sealed class TelemetryMotion
    {
        private bool _hasPrevious;
        private Vector3 _position, _velocity;
        private float _clock;

        internal void Reset() => _hasPrevious = false;

        internal Vector3 Acceleration(Vector3 position, Vector3 velocity, float clock)
        {
            if (!TelemetrySampling.Finite(position) || !TelemetrySampling.Finite(velocity) ||
                !TelemetrySampling.Finite(clock)) { Reset(); return Vector3.zero; }
            float elapsed = clock - _clock;
            var acceleration = Vector3.zero;
            if (_hasPrevious && elapsed > 0f && elapsed <= 0.25f)
            {
                // A reset/teleport is not a physical acceleration impulse. Permit
                // five meters of integration error beyond predicted displacement.
                double dx=position.x-_position.x-_velocity.x*elapsed;
                double dy=position.y-_position.y-_velocity.y*elapsed;
                double dz=position.z-_position.z-_velocity.z*elapsed;
                if (dx*dx+dy*dy+dz*dz <= 25.0)
                    acceleration = new Vector3((velocity.x-_velocity.x)/elapsed,
                        (velocity.y-_velocity.y)/elapsed, (velocity.z-_velocity.z)/elapsed);
            }
            _hasPrevious=true; _position=position; _velocity=velocity; _clock=clock;
            return TelemetrySampling.Finite(acceleration) ? acceleration : Vector3.zero;
        }
    }
}
