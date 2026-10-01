using System;
using System.Text;

namespace ArtOfSimRally.Mod
{
    // Opt-in aggregate counters for support, not a recorder. No allocations or
    // file writes in Observe; no frame/input timeline or playback data is stored.
    internal sealed class FrameHealth
    {
        internal sealed class Window
        {
            internal long Frames, Over33Ms, Over50Ms, Over100Ms;
            internal double MaximumMs;
            internal void Reset() { Frames=Over33Ms=Over50Ms=Over100Ms=0; MaximumMs=0; }
            internal void Observe(double ms)
            {
                Frames++; MaximumMs=Math.Max(MaximumMs,ms);
                if (ms>=1000.0/30) Over33Ms++;
                if (ms>=50) Over50Ms++;
                if (ms>=100) Over100Ms++;
            }
            internal void Append(StringBuilder output,string label)
            {
                output.AppendLine(label + ": intervals=" + Frames + "; maximum ms=" +
                    MaximumMs.ToString("F2",System.Globalization.CultureInfo.InvariantCulture) +
                    "; >=33.33ms=" + Over33Ms + "; >=50ms=" + Over50Ms + "; >=100ms=" + Over100Ms);
            }
        }
        internal readonly Window FirstFive = new Window(), NextTen = new Window(), Later = new Window();
        internal static readonly FrameHealth Current = new FrameHealth();
        internal long Frames { get; private set; }
        internal long EarlyHitches { get; private set; }
        internal long LaterHitches { get; private set; }
        internal double MaximumMs { get; private set; }
        internal long Revision { get; private set; }
        private bool _enabled, _previousDriving;
        private double _last, _segmentStart, _started, _lastHitch = -1;

        internal void Reset()
        {
            Frames=EarlyHitches=LaterHitches=0; MaximumMs=0; _lastHitch=-1;
            FirstFive.Reset(); NextTen.Reset(); Later.Reset();
            _enabled=_previousDriving=false; Revision++;
        }

        internal void Observe(bool enabled, bool driving, double now)
        {
            if (!enabled) { _enabled=false; _previousDriving=false; return; }
            if (double.IsNaN(now) || double.IsInfinity(now)) { _previousDriving=false; return; }
            if (!_enabled)
            {
                Frames=EarlyHitches=LaterHitches=0; MaximumMs=0;
                FirstFive.Reset(); NextTen.Reset(); Later.Reset();
                Revision++;
                _started=now; _lastHitch=-1; _previousDriving=false; _enabled=true;
            }
            if (driving)
            {
                if (!_previousDriving || now <= _last) _segmentStart=now;
                else
                {
                    double milliseconds=(now-_last)*1000;
                    Frames++;
                    Revision++;
                    if (milliseconds>MaximumMs) MaximumMs=milliseconds;
                    double elapsed=now-_segmentStart;
                    (elapsed<=5 ? FirstFive : elapsed<=15 ? NextTen : Later).Observe(milliseconds);
                    if (milliseconds>=100)
                    {
                        if (now-_segmentStart<=15) EarlyHitches++; else LaterHitches++;
                        _lastHitch=now-_started;
                    }
                }
            }
            _last=now; _previousDriving=driving;
        }

        internal void Append(StringBuilder output)
        {
            output.AppendLine("--- frame health (diagnostic aggregate) ---");
            output.AppendLine("logging currently enabled: " + _enabled);
            output.AppendLine("measured driving frame intervals: " + Frames);
            output.AppendLine("maximum interval ms: " + MaximumMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            output.AppendLine("100ms+ intervals, first 15s of a driving segment: " + EarlyHitches);
            output.AppendLine("100ms+ intervals, later in a driving segment: " + LaterHitches);
            output.AppendLine("last 100ms+ interval, seconds after diagnostics enabled: " + _lastHitch.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            FirstFive.Append(output,"first 5s of driving segments");
            NextTen.Append(output,"after 5s through 15s");
            Later.Append(output,"after 15s");
            output.AppendLine("Threshold counts overlap; they are frame intervals, not proof of stutter. Compare the same frame cap/settings.");
            output.AppendLine("Only foreground driving while Log detail for support is enabled. Pause/loading/focus transitions are excluded.");
            output.AppendLine("Segments also restart after pause/focus return; these are not stage identifiers. Counts do not identify the cause.");
            output.AppendLine();
        }
    }
}
