using System;

namespace ArtOfSimRally.Mod
{
    internal interface ILandingOutput
    {
        int Create(ImpactKind kind, int frequency, int durationMs);
        bool Play(ImpactKind kind, int slot, float magnitude, float frequency);
        bool Stop(ImpactKind kind, int slot);
        void Release();
    }

    internal struct ImpactDelivery
    {
        public ImpactKind Kind;
        public string Action, Reason;
        public float Magnitude;
        public double PlayLatencyMs, ElapsedMs;
    }

    // One active impact, backed by separate finite native effect types. Stop the
    // previous owner before playing the other; ordinary steering is independent.
    internal sealed class LandingFeedback
    {
        public const int Frequency = 25, DurationMs = 120;
        // Shared by the panel, overlap arbitration and final device submission.
        // The scale remains percent of nominal force: existing values keep their output.
        public const float MaximumStrengthPercent = 40f;
        public const float MaximumCrashStrengthPercent = 100f, DefaultCrashStrengthPercent = 50f;
        private readonly ILandingOutput _output;
        private readonly Func<double> _clock;
        private readonly Action<ImpactDelivery> _observe;
        private int _slot = -1, _crashSlot = -1;
        private bool _attempted, _crashAttempted, _active;
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
        public bool CrashAvailable => _crashSlot >= 0 && !_crashFailed;
        private string _crashStatus = "Off";
        public string CrashStatus => _crashFailed ? "Crash kick unavailable. Toggle crash off/on while paused to retry; create a support file" : _crashStatus;

        // Production supplies Stopwatch time, independent of Unity's cached frame
        // time. Tests may supply a deterministic clock, or use the observed time.
        public LandingFeedback(ILandingOutput output, Func<double> clock = null, Action<ImpactDelivery> observe = null)
        { _output = output; _clock = clock; _observe = observe; }
        private double Clock => _clock == null ? _observedTime : _clock();
        public void RetryCrash() { _crashFailed = false; if (_crashSlot < 0) _crashAttempted = false; }
        public void Prepare(bool enabled, bool ready, bool idle)
            => Prepare(enabled, false, ready, idle);
        public void Prepare(bool landing, bool crash, bool ready, bool idle)
        {
            bool enabled = landing || crash;
            if (!enabled || !ready)
            {
                Shutdown(enabled ? "device-unavailable" : "disabled");
                Status = _crashStatus = enabled ? "Waiting for wheel force feedback" : "Off"; return;
            }
            if (landing && _slot < 0 && !_attempted)
            {
                Status = "Pause to prepare landing vibration";
                if (idle)
                {
                    _attempted = true;
                    _slot = _output.Create(ImpactKind.Landing, Frequency, DurationMs);
                    Status = _slot >= 0 ? "Ready" : "Landing setup unavailable; toggle both impact features off, then on to retry or create a support file";
                }
            }
            if (crash && _crashSlot < 0 && !_crashAttempted && !_crashFailed)
            {
                _crashStatus = "Pause to prepare crash kick";
                if (idle)
                {
                    _crashAttempted = true;
                    _crashSlot = _output.Create(ImpactKind.Crash, 0, DurationMs);
                    _crashFailed = _crashSlot < 0;
                    _crashStatus = "Ready";
                }
            }
        }

        public bool Trigger(float intensity, float strengthPercent, double now)
            => Trigger(ImpactKind.Landing, intensity, strengthPercent, now);
        public bool Trigger(ImpactKind kind, float intensity, float strengthPercent, double now)
        {
            if (!(kind == ImpactKind.Crash ? CrashAvailable : Available) ||
                !Finite(intensity) || !Finite(strengthPercent) || !Finite(now) || now < 0 ||
                intensity <= 0 || strengthPercent <= 0) return false;
            // Separate from steering Strength/Smoothing. Even malformed saved
            // values cannot exceed each effect's nominal command cap.
            float magnitude = MagnitudeFor(kind, intensity, strengthPercent);
            _observedTime = now;
            double before = Clock;
            if (!Finite(before) || before < 0) { Stop("invalid-clock"); return false; }
            // Separate native types cannot replace each other implicitly.
            // A failed stop invalidates all impact resources; never start a new cue.
            if (_active) Stop("replaced");
            if (!(kind == ImpactKind.Crash ? CrashAvailable : Available)) return false;
            _kind = kind;
            Events++; LastMagnitude = magnitude;
            before = Clock;
            if (!Finite(before) || before < 0) return false;
            bool accepted = _output.Play(kind, Slot(kind), magnitude, kind == ImpactKind.Landing ? Frequency : 0);
            double after = Clock;
            _playLatencyMs = Finite(after) && after >= before ? (after - before) * 1000 : -1;
            if (_playLatencyMs < 0) accepted = false;
            if (accepted)
            {
                // Starting expiry before a slow native call could truncate the
                // effect. The driver still enforces its own 120 ms hard end.
                Accepted++; _active = true; _startedAt = after; _gameTime = now;
                _endsAt = after + DurationMs / 1000.0;
                if (kind == ImpactKind.Landing) Status = "Ready (last landing accepted by wheel driver)";
                else _crashStatus = "Ready (last crash accepted by wheel driver)";
                Report("start", "driver-accepted", after);
            }
            else
            {
                Rejected++; _active = false;
                bool stopped = _output.Stop(kind, Slot(kind));
                _startedAt = after;
                Report("reject", _playLatencyMs < 0 ? "invalid-clock" : "driver-rejected", after);
                if (kind == ImpactKind.Crash && stopped)
                {
                    // Crash failure does not invalidate the separate landing
                    // effect. Never retry an old impact automatically.
                    _crashFailed = true;
                }
                else
                {
                    ReleaseAll();
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
            if (Slot(_kind) >= 0 && !_output.Stop(_kind, Slot(_kind)))
            {
                ReleaseAll();
                Status = "Vibration stop failed; toggle both vibration features off, then on while paused to retry";
                reason = "stop-failed/" + reason;
            }
            Report("stop", reason, before);
            _active = false;
        }

        public void Shutdown(string reason = "shutdown")
        {
            Stop(reason);
            if (_slot >= 0 || _crashSlot >= 0) ReleaseAll();
            _attempted = _crashAttempted = _crashFailed = false;
            Status = _crashStatus = "Off";
        }
        private int Slot(ImpactKind kind) => kind == ImpactKind.Crash ? _crashSlot : _slot;
        private void ReleaseAll()
        {
            _output.Release(); _slot = _crashSlot = -1;
            // An output/stop failure needs an explicit reset before rebuilding.
            _attempted = _crashAttempted = true;
            _crashFailed = true;
        }
        private void Report(string action, string reason, double at)
        {
            _observe?.Invoke(new ImpactDelivery { Kind = _kind, Action = action, Reason = reason,
                Magnitude = LastMagnitude, PlayLatencyMs = _playLatencyMs,
                ElapsedMs = Finite(at) && at >= _startedAt ? (at - _startedAt) * 1000 : -1 });
        }
        // Callers validate finite, positive inputs before evaluating this mapping.
        internal static float MagnitudeFor(ImpactKind kind, float intensity, float strengthPercent)
            => Math.Min(1f, intensity) * Math.Min(kind == ImpactKind.Crash ? MaximumCrashStrengthPercent : MaximumStrengthPercent, strengthPercent) / 100f;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
