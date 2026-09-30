using System;
using System.IO;
using System.Text;
using Dbce.TripleScreen.Protocol;
using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

internal sealed class StatusPublisher
{
    private readonly UnityModManager.ModEntry.ModLogger _logger;
    private string _lastSemanticState = string.Empty;
    private DateTime _lastWriteUtc = DateTime.MinValue;
    private DateTime _lastFailureLogUtc = DateTime.MinValue;

    internal StatusPublisher(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;

    internal void Publish(ProjectionOutcome outcome, string gameBuild, bool force = false)
    {
        var layout = outcome.Layout;
        var document = layout.Document;
        var status = new RuntimeStatusDocument
        {
            AdapterId = AdapterConstants.AdapterId,
            AdapterVersion = AdapterConstants.AdapterVersion,
            GameId = AdapterConstants.GameId,
            GameBuild = string.IsNullOrWhiteSpace(gameBuild) ? null : gameBuild,
            State = outcome.State,
            AcceptedLayoutSha256 = layout.IsSuccess ? layout.Sha256 : null,
            LayoutContractVersion = layout.IsSuccess ? document?.SchemaVersion : null,
            Topology = layout.IsSuccess ? document?.Output?.Mode : null,
            ActiveCameraCount = outcome.ActiveCameraCount,
            ActiveCapabilities = outcome.ActiveCapabilities,
            LastSuccessfulFrameUtc = outcome.LastSuccessfulFrameUtc?.ToString("O")
        };
        status.Diagnostics.Add(outcome.Diagnostic);
        var semanticState = string.Join("|", new[]
        {
            status.State,
            status.AcceptedLayoutSha256 ?? string.Empty,
            status.Topology ?? string.Empty,
            status.ActiveCameraCount.ToString(),
            string.Join(",", status.ActiveCapabilities),
            outcome.Diagnostic.Code,
            outcome.Diagnostic.Severity,
            outcome.Diagnostic.Message
        });
        var now = DateTime.UtcNow;
        if (!force && string.Equals(semanticState, _lastSemanticState, StringComparison.Ordinal) &&
            (status.State != "active" || now - _lastWriteUtc < TimeSpan.FromSeconds(10))) return;

        var json = RuntimeStatusJson.Serialize(status) + Environment.NewLine;

        try
        {
            WriteAtomic(AdapterConstants.StatusPath, json);
            _lastSemanticState = semanticState;
            _lastWriteUtc = now;
        }
        catch (Exception exception)
        {
            if (now - _lastFailureLogUtc >= TimeSpan.FromSeconds(30))
            {
                _logger.Error("Could not publish DBCE triple-screen runtime status: " + SafeMessage(exception.Message));
                _lastFailureLogUtc = now;
            }
        }
    }

    private static void WriteAtomic(string destination, string contents)
    {
        var directory = Path.GetDirectoryName(destination);
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Status destination has no directory.");
        Directory.CreateDirectory(directory);
        var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, contents, new UTF8Encoding(false));
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown status write error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
