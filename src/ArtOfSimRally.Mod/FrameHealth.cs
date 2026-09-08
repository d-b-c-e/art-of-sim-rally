using System;
using System.Text;

namespace ArtOfSimRally.Mod
{
    // Opt-in aggregate counters for support, not a recorder. No allocations or
    // file writes in Observe; no frame/input timeline or playback data is stored.
    internal sealed class FrameHealth
    {
        internal static readonly FrameHealth Current = new FrameHealth();
        internal long Frames { get; private set; }
        internal long EarlyHitches { get; private set; }
        internal long LaterHitches { get; private set; }
        internal double MaximumMs { get; private set; }
        private bool _enabled, _previousDriving;
        private double _last, _segmentStart, _started, _lastHitch = -1;

        internal void Observe(bool enabled, bool driving, double now)
        {
            if (!enabled) { _enabled=false; _previousDriving=false; return; }
            if (double.IsNaN(now) || double.IsInfinity(now)) { _previousDriving=false; return; }
            if (!_enabled)
            {
                Frames=EarlyHitches=LaterHitches=0; MaximumMs=0;
                _started=now; _lastHitch=-1; _previousDriving=false; _enabled=true;
            }
            if (driving)
            {
                if (!_previousDriving || now <= _last) _segmentStart=now;
                else
                {
                    double milliseconds=(now-_last)*1000;
                    Frames++;
                    if (milliseconds>MaximumMs) MaximumMs=milliseconds;
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
            output.AppendLine("Only foreground driving while Log detail for support is enabled. Pause/loading/focus transitions are excluded.");
            output.AppendLine("Segments also restart after pause/focus return; these are not stage identifiers. Counts do not identify the cause.");
            output.AppendLine();
        }
    }
}
