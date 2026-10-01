using System;

namespace ArtOfSimRally.Mod
{
    internal struct CrashContact
    {
        public double Time;
        public float Rvx, Rvy, Rvz, Nx, Ny, Nz;
        public bool Road;
    }

    // A conservative first tuning, not a calibrated crash-force model. A body
    // contact is required: hard braking and acceleration spikes alone do nothing.
    internal sealed class CrashSignal
    {
        private LandingSample _last;
        private bool _hasLast;
        private double _since = -1, _lastCrash = -1;
        private float _lastIntensity;
        public float LastNormalSpeed { get; private set; }
        public bool Discontinuous { get; private set; }
        public void Reset()
        {
            _hasLast = false; _since = _lastCrash = -1;
            _lastIntensity = LastNormalSpeed = 0; Discontinuous = true;
        }
        public void Track(LandingSample sample)
        {
            Discontinuous = false;
            if (!Finite(sample.Time) || sample.Time < 0 || !Finite(sample.X) || !Finite(sample.Y) ||
                !Finite(sample.Z) || !Finite(sample.Vx) || !Finite(sample.Vy) || !Finite(sample.Vz)) { Reset(); return; }
            if (_hasLast)
            {
                double dt = sample.Time - _last.Time;
                double x = sample.X - _last.X - _last.Vx * dt;
                double y = sample.Y - _last.Y - _last.Vy * dt;
                double z = sample.Z - _last.Z - _last.Vz * dt;
                if (dt <= 0 || dt > .1 || x*x + y*y + z*z > 25) Reset();
            }
            if (!_hasLast) _since = sample.Time;
            _last = sample; _hasLast = true;
        }
        public float Observe(CrashContact contact)
        {
            if (!_hasLast || !Finite(contact.Time) || contact.Time < _last.Time ||
                contact.Time - _last.Time > .05 || contact.Time - _since < .1 || contact.Road ||
                !Finite(contact.Rvx) || !Finite(contact.Rvy) || !Finite(contact.Rvz) ||
                !Finite(contact.Nx) || !Finite(contact.Ny) || !Finite(contact.Nz)) return 0;
            double n2 = contact.Nx*contact.Nx + contact.Ny*contact.Ny + contact.Nz*contact.Nz;
            // Road/ground and mostly vertical body contacts belong to landing;
            // ignore malformed normals rather than amplifying them.
            if (n2 < .81 || n2 > 1.21 || Math.Abs(contact.Ny) / Math.Sqrt(n2) > .65) return 0;
            double speed = Math.Abs((double)contact.Rvx*contact.Nx + (double)contact.Rvy*contact.Ny +
                (double)contact.Rvz*contact.Nz) / Math.Sqrt(n2);
            if (!Finite(speed) || speed <= 3) return 0;
            float intensity = (float)Math.Min(1, (speed - 3) / 17);
            // Suppress repeated manifold/collider contacts; allow a stronger
            // contact from the same hit to replace the weaker cue.
            if (_lastCrash >= 0 && contact.Time - _lastCrash < .35)
            {
                if (intensity <= _lastIntensity || contact.Time - _lastCrash >= .12) return 0;
            }
            else _lastCrash = contact.Time;
            _lastIntensity = intensity; LastNormalSpeed = (float)speed;
            return intensity;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
