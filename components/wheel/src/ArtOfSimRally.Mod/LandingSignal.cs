using System;

namespace ArtOfSimRally.Mod
{
    internal struct LandingSample
    {
        public double Time;
        public int Contacts;
        public float X, Y, Z, Vx, Vy, Vz, UpY;
    }

    // Game-specific contact detector. A descending car must leave established
    // ground contact, remain fully airborne, then touch down. This is a cue for
    // vibration, not a calibrated collision impulse or a steering direction.
    internal sealed class LandingSignal
    {
        private LandingSample _last;
        private bool _hasLast, _armed;
        private double _groundSince = -1, _airSince = -1, _lastLanding = -1;
        public float LastAirSeconds { get; private set; }
        public float LastDescentMps { get; private set; }
        public bool Discontinuous { get; private set; }

        public void Reset()
        {
            _hasLast = _armed = false;
            _groundSince = _airSince = _lastLanding = -1;
            LastAirSeconds = LastDescentMps = 0;
            Discontinuous = true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public float Observe(LandingSample sample)
        {
            Discontinuous = false;
            if (!Finite(sample.Time) || sample.Time < 0 || sample.Contacts < 0 || sample.Contacts > 15 ||
                !Finite(sample.X) || !Finite(sample.Y) || !Finite(sample.Z) ||
                !Finite(sample.Vx) || !Finite(sample.Vy) || !Finite(sample.Vz) ||
                !Finite(sample.UpY) || sample.UpY < .5f || sample.UpY > 1.01f)
            { Reset(); return 0; }

            if (_hasLast)
            {
                double dt = sample.Time - _last.Time;
                double dx = sample.X - _last.X - _last.Vx * dt;
                double dy = sample.Y - _last.Y - _last.Vy * dt;
                double dz = sample.Z - _last.Z - _last.Vz * dt;
                // Duplicate/reversed clocks, long gaps and teleports break the
                // transition. Never replay an impact missed during a stall/reset.
                if (dt <= 0 || dt > .1 || dx * dx + dy * dy + dz * dz > 25)
                    Reset();
            }

            float intensity = 0;
            if (sample.Contacts == 0)
            {
                if (_airSince < 0) _airSince = sample.Time;
                _groundSince = -1;
            }
            else
            {
                if (_airSince >= 0)
                {
                    double airborne = sample.Time - _airSince;
                    float descent = _hasLast ? -_last.Vy : 0;
                    double speed2 = sample.Vx * (double)sample.Vx + sample.Vy * (double)sample.Vy + sample.Vz * (double)sample.Vz;
                    if (_armed && airborne >= .12 && airborne <= 5 && descent > 1.5f && speed2 >= 9 &&
                        (_lastLanding < 0 || sample.Time - _lastLanding >= .5))
                    {
                        // Full cue at 10 m/s pre-contact descent; tune after
                        // attended comparison, not from acceleration spikes.
                        intensity = Math.Min(1f, (descent - 1.5f) / 8.5f);
                        LastAirSeconds = (float)airborne;
                        LastDescentMps = descent;
                        _lastLanding = sample.Time;
                    }
                    _armed = false;
                    _airSince = -1;
                    _groundSince = sample.Time;
                }
                if (_groundSince < 0) _groundSince = sample.Time;
                if (sample.Time - _groundSince >= .1) _armed = true;
            }
            _last = sample; _hasLast = true;
            return intensity;
        }
    }
}
