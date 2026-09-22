using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dbce.TripleScreen.Protocol;

[JsonObject(MemberSerialization.OptIn)]
public sealed class RuntimeStatusDocument
{
    [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; } = 1;
    [JsonProperty("adapterId", Order = 2)] public string AdapterId { get; set; } = string.Empty;
    [JsonProperty("adapterVersion", Order = 3)] public string AdapterVersion { get; set; } = string.Empty;
    [JsonProperty("gameId", Order = 4)] public string GameId { get; set; } = string.Empty;
    [JsonProperty("gameBuild", Order = 5)] public string? GameBuild { get; set; }
    [JsonProperty("state", Order = 6)] public string State { get; set; } = "inactive";
    [JsonProperty("acceptedLayoutSha256", Order = 7)] public string? AcceptedLayoutSha256 { get; set; }
    [JsonProperty("layoutContractVersion", Order = 8)] public int? LayoutContractVersion { get; set; }
    [JsonProperty("topology", Order = 9)] public string? Topology { get; set; }
    [JsonProperty("activeCameraCount", Order = 10)] public int ActiveCameraCount { get; set; }
    [JsonProperty("activeCapabilities", Order = 11)] public List<string> ActiveCapabilities { get; set; } = new();
    [JsonProperty("lastSuccessfulFrameUtc", Order = 12)] public string? LastSuccessfulFrameUtc { get; set; }
    [JsonProperty("diagnostics", Order = 13)] public List<RuntimeDiagnostic> Diagnostics { get; set; } = new();
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class RuntimeDiagnostic
{
    public RuntimeDiagnostic()
    {
    }

    public RuntimeDiagnostic(string code, string severity, string message)
    {
        Code = code;
        Severity = severity;
        Message = message;
    }

    [JsonProperty("code", Order = 1)] public string Code { get; set; } = string.Empty;
    [JsonProperty("severity", Order = 2)] public string Severity { get; set; } = "info";
    [JsonProperty("message", Order = 3)] public string Message { get; set; } = string.Empty;
}

public static class RuntimeStatusJson
{
    public static string Serialize(RuntimeStatusDocument status) => JsonConvert.SerializeObject(
        status,
        Formatting.Indented,
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
}
