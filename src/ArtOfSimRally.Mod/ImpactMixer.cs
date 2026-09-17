using System;

namespace ArtOfSimRally.Mod
{
    internal enum ImpactKind { Landing, Crash }
    internal enum ImpactResult { Unavailable, Suppressed, Accepted, Rejected }

    // One logical active impact owns either a finite sine or constant effect.
    // Never play both together or let one feature reset the other's cue.
    internal sealed class ImpactMixer
    {
        internal sealed class Counters
        {
            public int Events, Accepted, Rejected, Suppressed;
            public float Magnitude;
            public bool HasDelivery;
            public ImpactDelivery Delivery;
            public int EarlyStops;
        }
        private readonly LandingFeedback _feedback;
        private readonly Counters[] _counts = { new Counters(), new Counters() };
        private bool _landing, _crash;
        private ImpactKind? _active;
        private float _magnitude;
        private bool _retryCrash;
        private readonly Action<ImpactDelivery> _observe;

        public ImpactMixer(ILandingOutput output, Func<double> clock = null, Action<ImpactDelivery> observe = null)
        { _observe = observe; _feedback = new LandingFeedback(output, clock, OnDelivery); }
        private void OnDelivery(ImpactDelivery delivery)
        {
            if (delivery.Kind == ImpactKind.Crash && delivery.Action != "stop") _retryCrash = false;
            var counts = Counts(delivery.Kind); counts.HasDelivery = true; counts.Delivery = delivery;
            if (delivery.Action == "stop" && delivery.ElapsedMs >= 0 && delivery.ElapsedMs < LandingFeedback.DurationMs)
                counts.EarlyStops++;
            _observe?.Invoke(delivery);
        }
        private bool Enabled(ImpactKind kind) => kind == ImpactKind.Landing ? _landing : _crash;
        public bool Available(ImpactKind kind) => Enabled(kind) && (kind == ImpactKind.Crash ? _feedback.CrashAvailable : _feedback.Available);
        public string Status(ImpactKind kind) => Enabled(kind) ? (kind == ImpactKind.Crash ? _feedback.CrashStatus : _feedback.Status) : "Off";
        public Counters Counts(ImpactKind kind) => _counts[(int)kind];

        public void Prepare(bool landing, bool crash, bool ready, bool idle)
        {
            _landing = landing; _crash = crash;
            if (!crash) _retryCrash = true;
            else if (idle && _retryCrash) { _feedback.RetryCrash(); _retryCrash = false; }
            if (_active.HasValue && !Enabled(_active.Value)) Stop("feature-disabled");
            _feedback.Prepare(landing, crash, ready, idle);
            if (!ready || (!landing && !crash)) _active = null;
        }
        public ImpactResult Trigger(ImpactKind kind, float intensity, float strength, double now)
        {
            Tick(now);
            if (!Available(kind) || !Finite(intensity) || !Finite(strength) || !Finite(now) ||
                now < 0 || intensity <= 0 || strength <= 0) return ImpactResult.Unavailable;
            float magnitude = LandingFeedback.MagnitudeFor(kind, intensity, strength);
            var counts = Counts(kind); counts.Events++; counts.Magnitude = magnitude;
            // Strongest wins; crash wins a tie. A suppressed cue is dropped,
            // never delayed until after the physical event has passed.
            if (_active.HasValue && (magnitude < _magnitude ||
                (magnitude == _magnitude && (kind == ImpactKind.Landing || _active == kind))))
            { counts.Suppressed++; return ImpactResult.Suppressed; }
            bool accepted = _feedback.Trigger(kind, intensity, strength, now);
            if (!accepted) { counts.Rejected++; _active = null; return ImpactResult.Rejected; }
            counts.Accepted++; _active = kind; _magnitude = magnitude;
            return ImpactResult.Accepted;
        }
        public void Tick(double now)
        {
            _feedback.Tick(now);
            if (!_feedback.Active) _active = null;
        }
        public void Stop(ImpactKind kind, string reason = "detector-reset") { if (_active == kind) Stop(reason); }
        public void Stop(string reason = "reset") { _feedback.Stop(reason); _active = null; }
        public void Shutdown() { _feedback.Shutdown(); _active = null; _landing = _crash = false; }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
