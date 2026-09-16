using System;

namespace ArtOfSimRally.Mod
{
    internal enum ImpactKind { Landing, Crash }
    internal enum ImpactResult { Unavailable, Suppressed, Accepted, Rejected }

    // Both cues own the SAME finite native effect. Never sum their amplitudes
    // or let one feature release a slot still used by the other.
    internal sealed class ImpactMixer
    {
        internal sealed class Counters
        {
            public int Events, Accepted, Rejected, Suppressed;
            public float Magnitude;
        }
        private readonly LandingFeedback _feedback;
        private readonly Counters[] _counts = { new Counters(), new Counters() };
        private bool _landing, _crash;
        private ImpactKind? _active;
        private double _endsAt;
        private float _magnitude;

        public ImpactMixer(ILandingOutput output) { _feedback = new LandingFeedback(output); }
        private bool Enabled(ImpactKind kind) => kind == ImpactKind.Landing ? _landing : _crash;
        public bool Available(ImpactKind kind) => Enabled(kind) && _feedback.Available;
        public string Status(ImpactKind kind) => Enabled(kind) ? _feedback.Status : "Off";
        public Counters Counts(ImpactKind kind) => _counts[(int)kind];

        public void Prepare(bool landing, bool crash, bool ready, bool idle)
        {
            _landing = landing; _crash = crash;
            if (_active.HasValue && !Enabled(_active.Value)) Stop();
            _feedback.Prepare(landing || crash, ready, idle);
            if (!ready || (!landing && !crash)) _active = null;
        }
        public ImpactResult Trigger(ImpactKind kind, float intensity, float strength, double now)
        {
            Tick(now);
            if (!Available(kind) || !Finite(intensity) || !Finite(strength) || !Finite(now) ||
                now < 0 || intensity <= 0 || strength <= 0) return ImpactResult.Unavailable;
            float magnitude = Math.Min(1f, intensity) * Math.Min(20f, strength) / 100f;
            var counts = Counts(kind); counts.Events++; counts.Magnitude = magnitude;
            // Strongest wins; crash wins a tie. A suppressed cue is dropped,
            // never delayed until after the physical event has passed.
            if (_active.HasValue && (magnitude < _magnitude ||
                (magnitude == _magnitude && (kind == ImpactKind.Landing || _active == kind))))
            { counts.Suppressed++; return ImpactResult.Suppressed; }
            bool accepted = _feedback.Trigger(intensity, strength, now);
            if (!accepted) { counts.Rejected++; _active = null; return ImpactResult.Rejected; }
            counts.Accepted++; _active = kind; _magnitude = magnitude;
            _endsAt = now + LandingFeedback.DurationMs / 1000.0;
            return ImpactResult.Accepted;
        }
        public void Tick(double now)
        {
            _feedback.Tick(now);
            if (!Finite(now) || now >= _endsAt || now < _endsAt - LandingFeedback.DurationMs / 1000.0)
                _active = null;
        }
        public void Stop(ImpactKind kind) { if (_active == kind) Stop(); }
        public void Stop() { _feedback.Stop(); _active = null; }
        public void Shutdown() { _feedback.Shutdown(); _active = null; _landing = _crash = false; }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
