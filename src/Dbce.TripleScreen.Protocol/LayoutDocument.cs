using Newtonsoft.Json;

namespace Dbce.TripleScreen.Protocol;

[JsonObject(MemberSerialization.OptIn)]
public sealed class LayoutDocument
{
    [JsonProperty("schemaVersion", Required = Required.Always)]
    public int SchemaVersion { get; set; }

    [JsonProperty("panel", Required = Required.Always)]
    public PanelDocument? Panel { get; set; }

    [JsonProperty("geometry", Required = Required.Always)]
    public GeometryDocument? Geometry { get; set; }

    [JsonProperty("output", Required = Required.Always)]
    public OutputDocument? Output { get; set; }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class PanelDocument
{
    [JsonProperty("count", Required = Required.Always)]
    public int Count { get; set; }

    [JsonProperty("nativeWidthPx", Required = Required.Always)]
    public int NativeWidthPx { get; set; }

    [JsonProperty("nativeHeightPx", Required = Required.Always)]
    public int NativeHeightPx { get; set; }

    [JsonProperty("physicalWidthMm", Required = Required.Always)]
    public double PhysicalWidthMm { get; set; }

    [JsonProperty("physicalHeightMm", Required = Required.Always)]
    public double PhysicalHeightMm { get; set; }

    [JsonProperty("curveRadiusMm")]
    public double? CurveRadiusMm { get; set; }

    [JsonProperty("bezelWidthMm")]
    public double BezelWidthMm { get; set; }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class GeometryDocument
{
    [JsonProperty("eyeDistanceMm", Required = Required.Always)]
    public double EyeDistanceMm { get; set; }

    [JsonProperty("eyeHeightAbovePanelCenterMm")]
    public double EyeHeightAbovePanelCenterMm { get; set; }

    [JsonProperty("leftYawDegrees", Required = Required.Always)]
    public double LeftYawDegrees { get; set; }

    [JsonProperty("rightYawDegrees", Required = Required.Always)]
    public double RightYawDegrees { get; set; }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class OutputDocument
{
    [JsonProperty("mode", Required = Required.Always)]
    public string? Mode { get; set; }

    [JsonProperty("combinedWidthPx")]
    public int? CombinedWidthPx { get; set; }

    [JsonProperty("combinedHeightPx")]
    public int? CombinedHeightPx { get; set; }
}
