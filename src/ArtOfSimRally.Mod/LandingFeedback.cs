using System;

namespace ArtOfSimRally.Mod
{
    internal interface ILandingOutput
    {
        int Create(int frequency, int durationMs);
        bool Play(int slot, float magnitude, float frequency);
        bool PlayShaped(int slot, float magnitude, float frequency, int phase, int fadeMs);
        bool Stop(int slot);
        void Release();
    }

    internal struct ImpactDelivery
    {
        public ImpactKind Kind;
        public string Action, Reason;
        public float Magnitude;
        public double PlayLatencyMs, ElapsedMs;
    }

    // One device-generated finite slot. Landing retains its three sine cycles;
    // crash begins at the positive peak and fades through one smaller rebound.
    internal sealed class LandingFeedback
    {
        public const int Frequency = 25, DurationMs = 120;
        public const float CrashFrequency = 6.25f;
        public const int CrashPhase = 9000;
        // Shared by the panel, overlap arbitration and final device submission.
        // The scale remains percent of nominal force: existing values keep their output.
        public const float MaximumStrengthPercent = 40f;
        private readonly ILandingOutput _output;
        private readonly Func<double> _clock;
        private readonly Action<ImpactDelivery> _observe;
        private int _slot = -1;
        private bool _attempted, _active;
        private double _endsAt, _startedAt, _gameTime, _observedTime, _playLatencyMs;
        private ImpactKind _kind;
        private bool _crashFailed;
        public string Status { get; private set; } = "Off";
        public int Events { get; private set; }
        public int Accepted { get; private set; }
        public int Rejected { get; private set; }
        public float LastMagnitude { get; private set; }
        public bool Available => _slot >= 0;
        public bool Active => _active;
        public bool CrashAvailable => Available && !_crashFailed;
        public string CrashStatus => _crashFailed ? "Crash kick unavailable; landing remains available. Toggle crash off/on while paused to retry; create a support file" : Status;

        // Production supplies Stopwatch time, independent of Unity's cached frame
        // time. Tests may supply a deterministic clock, or use the observed time.
        public LandingFeedback(ILandingOutput output, Func<double> clock = null, Action<ImpactDelivery> observe = null)
        { _output = output; _clock = clock; _observe = observe; }
        private double Clock => _clock == null ? _observedTime : _clock();
        public void RetryCrash() { _crashFailed = false; }
        public void Prepare(bool enabled, bool ready, bool idle)
        {
            if (!enabled || !ready)
            {
                Shutdown(enabled ? "device-unavailable" : "disabled"); Status = enabled ? "Waiting for wheel force feedback" : "Off"; return;
            }
            if (_slot >= 0 || _attempted) return;
            Status = "Pause to prepare impact vibration";
            if (!idle) return;
            _attempted = true;
            _slot = _output.Create(Frequency, DurationMs);
            Status = _slot >= 0 ? "Ready" : "Vibration setup unavailable; toggle both vibration features off, then on to retry or create a support file";
        }

        public bool Trigger(float intensity, float strengthPercent, double now)
            => Trigger(ImpactKind.Landing, intensity, strengthPercent, now);
        public bool Trigger(ImpactKind kind, float intensity, float strengthPercent, double now)
        {
            if (_slot < 0 || !Finite(intensity) || !Finite(strengthPercent) || !Finite(now) || now < 0 ||
                intensity <= 0 || strengthPercent <= 0 || (kind == ImpactKind.Crash && _crashFailed)) return false;
            // Separate from steering Strength/Smoothing. Even malformed saved
            // values cannot ask for more than 40% of the device's nominal force.
            float magnitude = MagnitudeFor(intensity, strengthPercent);
            _observedTime = now;
            double before = Clock;
            if (!Finite(before) || before < 0) { Stop("invalid-clock"); return false; }
            // Both native play paths stop the previous slot before starting.
            if (_active) { Report("stop", "replaced", before); _active = false; }
            _kind = kind;
            Events++; LastMagnitude = magnitude;
            bool accepted = kind == ImpactKind.Crash
                ? _output.PlayShaped(_slot, magnitude, CrashFrequency, CrashPhase, DurationMs)
                : _output.Play(_slot, magnitude, Frequency);
            double after = Clock;
            _playLatencyMs = Finite(after) && after >= before ? (after - before) * 1000 : -1;
            if (_playLatencyMs < 0) accepted = false;
            if (accepted)
            {
                // Starting expiry before a slow native call could truncate the
                // effect. The driver still enforces its own 120 ms hard end.
                Accepted++; _active = true; _startedAt = after; _gameTime = now;
                _endsAt = after + DurationMs / 1000.0;
                Status = "Ready (last impact accepted by wheel driver)";
                Report("start", "driver-accepted", after);
            }
            else
            {
                Rejected++; _active = false;
                bool stopped = _output.Stop(_slot);
                _startedAt = after;
                Report("reject", _playLatencyMs < 0 ? "invalid-clock" : "driver-rejected", after);
                if (kind == ImpactKind.Crash && stopped)
                {
                    // Native rejection retains the slot metadata; the next
                    // landing can rebuild its legacy effect. No crash retry.
                    _crashFailed = true;
                }
                else
                {
                    _output.Release(); _slot = -1;
                    Status = "Wheel rejected impact vibration; toggle both vibration features off, then on while paused to retry";
                }
            }
            return accepted;
        }

        public void Tick(double now)
        {
            _observedTime = now;
            if (!_active) return;
            double current = Clock;
            if (!Finite(now) || now < _gameTime || !Finite(current) || current < _startedAt) Stop("clock-discontinuity");
            else if (current >= _endsAt) Stop("duration-elapsed");
        }

        public void Stop(string reason = "reset")
        {
            if (!_active) return;
            double before = Clock;
            if (_slot >= 0 && !_output.Stop(_slot))
            {
                _output.Release(); _slot = -1;
                Status = "Vibration stop failed; toggle both vibration features off, then on while paused to retry";
                reason = "stop-failed/" + reason;
            }
            Report("stop", reason, before);
            _active = false;
        }

        public void Shutdown(string reason = "shutdown")
        {
            Stop(reason);
            if (_slot >= 0) _output.Release();
            _slot = -1; _attempted = false; _crashFailed = false;
            Status = "Off";
        }
        private void Report(string action, string reason, double at)
        {
            _observe?.Invoke(new ImpactDelivery { Kind = _kind, Action = action, Reason = reason,
                Magnitude = LastMagnitude, PlayLatencyMs = _playLatencyMs,
                ElapsedMs = Finite(at) && at >= _startedAt ? (at - _startedAt) * 1000 : -1 });
        }
        // Callers validate finite, positive inputs before evaluating this mapping.
        internal static float MagnitudeFor(float intensity, float strengthPercent)
            => Math.Min(1f, intensity) * Math.Min(MaximumStrengthPercent, strengthPercent) / 100f;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
