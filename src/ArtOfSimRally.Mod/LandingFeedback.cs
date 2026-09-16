using System;

namespace ArtOfSimRally.Mod
{
    internal interface ILandingOutput
    {
        int Create(int frequency, int durationMs);
        bool Play(int slot, float magnitude, float frequency);
        bool Stop(int slot);
        void Release();
    }

    // Device-generated sine, three cycles at 25 Hz. The finite duration lives
    // in the device effect: a stopped Unity callback cannot leave it vibrating.
    internal sealed class LandingFeedback
    {
        public const int Frequency = 25, DurationMs = 120;
        private readonly ILandingOutput _output;
        private int _slot = -1;
        private bool _attempted, _active;
        private double _endsAt;
        public string Status { get; private set; } = "Off";
        public int Events { get; private set; }
        public int Accepted { get; private set; }
        public int Rejected { get; private set; }
        public float LastMagnitude { get; private set; }
        public bool Available => _slot >= 0;

        public LandingFeedback(ILandingOutput output) { _output = output; }
        public void Prepare(bool enabled, bool ready, bool idle)
        {
            if (!enabled || !ready)
            {
                Shutdown(); Status = enabled ? "Waiting for wheel force feedback" : "Off"; return;
            }
            if (_slot >= 0 || _attempted) return;
            Status = "Pause to prepare impact vibration";
            if (!idle) return;
            _attempted = true;
            _slot = _output.Create(Frequency, DurationMs);
            Status = _slot >= 0 ? "Ready" : "Vibration setup unavailable; toggle both vibration features off, then on to retry or create a support file";
        }

        public bool Trigger(float intensity, float strengthPercent, double now)
        {
            if (_slot < 0 || !Finite(intensity) || !Finite(strengthPercent) || !Finite(now) || now < 0 ||
                intensity <= 0 || strengthPercent <= 0) return false;
            // Separate from steering Strength/Smoothing. Even malformed saved
            // values cannot ask for more than 20% of the device's nominal force.
            float magnitude = Math.Min(1f, intensity) * Math.Min(20f, strengthPercent) / 100f;
            Events++; LastMagnitude = magnitude;
            bool accepted = _output.Play(_slot, magnitude, Frequency);
            if (accepted)
            {
                Accepted++; _active = true; _endsAt = now + DurationMs / 1000.0;
                Status = "Ready (last impact accepted by wheel driver)";
            }
            else
            {
                Rejected++; Stop();
                // Do not retry this event on a recovered device: it is stale.
                _output.Release(); _slot = -1;
                Status = "Wheel rejected impact vibration; toggle both vibration features off, then on while paused to retry";
            }
            return accepted;
        }

        public void Tick(double now)
        {
            if (_active && (!Finite(now) || now < _endsAt - DurationMs / 1000.0 || now >= _endsAt)) Stop();
        }

        public void Stop()
        {
            if (_active && _slot >= 0 && !_output.Stop(_slot))
            {
                _output.Release(); _slot = -1;
                Status = "Vibration stop failed; toggle both vibration features off, then on while paused to retry";
            }
            _active = false;
        }

        public void Shutdown()
        {
            Stop();
            if (_slot >= 0) _output.Release();
            _slot = -1; _attempted = false;
            Status = "Off";
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
