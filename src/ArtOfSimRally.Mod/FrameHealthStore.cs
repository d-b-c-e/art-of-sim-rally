using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ArtOfSimRally.Mod
{
    // Small opt-in aggregate snapshot, not a recorder. The caller releases
    // outputs before an idle/shutdown write; Observe never touches the store.
    internal sealed class FrameHealthStore
    {
        internal const int MaxBytes = 8192;
        private readonly string _path, _identity, _session = Guid.NewGuid().ToString("N");
        private readonly DateTime _started;
        private readonly XElement _previous;
        private readonly string _previousStatus;
        private long _savedRevision = -1;
        private double _nextAttempt;
        private bool _shutdownAttempted;
        internal string LastError { get; private set; }
        internal int Writes { get; private set; }

        internal FrameHealthStore(string path, string identity, DateTime startedUtc)
        {
            _path = Path.GetFullPath(path); _identity = identity; _started = startedUtc;
            if (string.IsNullOrEmpty(identity) || identity.Length > 256 || startedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Expected bounded build identity and UTC session time");
            try
            {
                if (!File.Exists(_path)) { _previousStatus = "No previous diagnostic snapshot."; return; }
                using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length > MaxBytes) throw new InvalidDataException("Snapshot exceeds size limit");
                    using (var reader = XmlReader.Create(stream, new XmlReaderSettings {
                        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBytes }))
                        _previous = XElement.Load(reader);
                }
                Validate(_previous, startedUtc);
                _previousStatus = "Previous session; captured at the recorded idle/exit boundary, not a crash trace.";
            }
            catch (Exception ex)
            {
                _previous = null;
                _previousStatus = "Previous diagnostic snapshot unavailable: " + ex.GetType().Name;
            }
        }

        internal bool Flush(FrameHealth health, bool driving, bool shutdown, double now, DateTime utc)
        {
            if (driving || health.Frames == 0 || _shutdownAttempted || double.IsNaN(now) || double.IsInfinity(now)) return false;
            if (!shutdown && (_savedRevision == health.Revision || (now < _nextAttempt && now >= _nextAttempt - 5))) return false;
            if (shutdown) _shutdownAttempted = true;
            _nextAttempt = now + 5;
            string temp = null;
            try
            {
                if (utc.Kind != DateTimeKind.Utc || utc < _started) throw new InvalidDataException("Session clock moved backwards");
                var xml = new XElement("frameHealth", new XAttribute("schema", 1),
                    new XAttribute("session", _session), new XAttribute("build", _identity),
                    new XAttribute("startedUtc", _started.ToString("o")), new XAttribute("capturedUtc", utc.ToString("o")),
                    new XAttribute("boundary", shutdown ? "exit" : "idle"),
                    new XAttribute("frames", health.Frames), new XAttribute("earlyHitches", health.EarlyHitches),
                    new XAttribute("laterHitches", health.LaterHitches), new XAttribute("maximumMs", health.MaximumMs));
                string content = xml.ToString(SaveOptions.DisableFormatting);
                if (Encoding.UTF8.GetByteCount(content) > MaxBytes) throw new InvalidDataException("Snapshot exceeds size limit");
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temp, content, new UTF8Encoding(false));
                if (File.Exists(_path)) File.Replace(temp, _path, null); else File.Move(temp, _path);
                _savedRevision = health.Revision; Writes++; LastError = null; return true;
            }
            catch (Exception ex) { LastError = ex.GetType().Name + ": could not retain diagnostic summary."; return false; }
            finally { if (temp != null) try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        private static void Validate(XElement x, DateTime now)
        {
            if (x.Name != "frameHealth" || (int?)x.Attribute("schema") != 1 || x.HasElements || !string.IsNullOrWhiteSpace(x.Value))
                throw new InvalidDataException("Unknown diagnostic format");
            if (!Guid.TryParse((string)x.Attribute("session"), out var session) || session == Guid.Empty ||
                string.IsNullOrEmpty((string)x.Attribute("build")) || ((string)x.Attribute("build")).Length > 256)
                throw new InvalidDataException("Missing session identity");
            string boundary = (string)x.Attribute("boundary");
            if (boundary != "idle" && boundary != "exit") throw new InvalidDataException("Unknown boundary");
            DateTime started, captured;
            if (!DateTime.TryParseExact((string)x.Attribute("startedUtc"), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out started) ||
                !DateTime.TryParseExact((string)x.Attribute("capturedUtc"), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out captured) ||
                started.Kind != DateTimeKind.Utc || captured.Kind != DateTimeKind.Utc || captured < started ||
                captured > now.AddMinutes(5) || captured < now.AddDays(-30))
                throw new InvalidDataException("Stale or inconsistent snapshot time");
            long frames = (long?)x.Attribute("frames") ?? -1, early = (long?)x.Attribute("earlyHitches") ?? -1,
                later = (long?)x.Attribute("laterHitches") ?? -1;
            double maximum = (double?)x.Attribute("maximumMs") ?? double.NaN;
            if (frames <= 0 || early < 0 || later < 0 || early > frames || later > frames - early ||
                double.IsNaN(maximum) || double.IsInfinity(maximum) || maximum < 0)
                throw new InvalidDataException("Invalid frame counters");
        }

        internal void AppendPrevious(StringBuilder output)
        {
            output.AppendLine("--- previous session frame health ---");
            output.AppendLine(_previousStatus);
            if (_previous != null)
                foreach (var attribute in _previous.Attributes())
                    output.AppendLine(attribute.Name + ": " + attribute.Value);
            if (LastError != null) output.AppendLine("Current snapshot retention: " + LastError);
            output.AppendLine("Aggregates do not identify a stutter cause. No per-frame timeline is retained.");
            output.AppendLine();
        }
    }
}
