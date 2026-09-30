using System;
using System.IO;
using Dbce.TripleScreen.Protocol;

namespace ArtOfRally.TripleScreen.Mod;

internal sealed class LayoutSource
{
    private readonly string _canonicalPath;
    private readonly string _stagedPath;
    private string _lastPath = string.Empty;
    private DateTime _lastWriteUtc = DateTime.MinValue;
    private long _lastLength = -1;
    private LayoutLoadResult _current = LayoutLoadResult.Failure("LAYOUT_NOT_FOUND", "The optimizer has not written desired-layout.json.");

    internal LayoutSource(string stagedPath) : this(stagedPath, AdapterConstants.DesiredLayoutPath) { }

    internal LayoutSource(string stagedPath, string canonicalPath)
    {
        _stagedPath = stagedPath;
        _canonicalPath = canonicalPath;
    }

    internal LayoutLoadResult Current => _current;
    internal string CurrentPath => _lastPath;

    internal bool Refresh(bool force)
    {
        var path = File.Exists(_canonicalPath) ? _canonicalPath : _stagedPath;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                var changed = _lastLength != -1 || _current.ErrorCode != "LAYOUT_NOT_FOUND";
                _lastPath = string.Empty;
                _lastLength = -1;
                _lastWriteUtc = DateTime.MinValue;
                _current = LayoutLoadResult.Failure("LAYOUT_NOT_FOUND", "The optimizer has not written desired-layout.json.");
                return changed;
            }

            if (!force && string.Equals(path, _lastPath, StringComparison.OrdinalIgnoreCase) &&
                info.Length == _lastLength && info.LastWriteTimeUtc == _lastWriteUtc)
            {
                return false;
            }

            _lastPath = path;
            _lastLength = info.Length;
            _lastWriteUtc = info.LastWriteTimeUtc;
            _current = info.Length > LayoutContractParser.MaximumDocumentBytes
                ? LayoutLoadResult.Failure("LAYOUT_TOO_LARGE", "The desired layout exceeds 64 KiB.")
                : LayoutContractParser.Parse(File.ReadAllBytes(path));
            return true;
        }
        catch (Exception exception)
        {
            _current = LayoutLoadResult.Failure("LAYOUT_READ_ERROR", SafeMessage(exception.Message));
            return true;
        }
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown layout read error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
