using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dbce.TripleScreen.Protocol;

/// <summary>Strict reader for the canonical optimizer layout contract.</summary>
public static class LayoutContractParser
{
    public const int SupportedSchemaVersion = 1;
    public const int MaximumDocumentBytes = 64 * 1024;

    public static LayoutLoadResult Parse(byte[] utf8)
    {
        if (utf8 is null || utf8.Length == 0)
        {
            return LayoutLoadResult.Failure("LAYOUT_EMPTY", "The desired layout is empty.");
        }

        if (utf8.Length > MaximumDocumentBytes)
        {
            return LayoutLoadResult.Failure("LAYOUT_TOO_LARGE", "The desired layout exceeds 64 KiB.");
        }

        try
        {
            var text = new UTF8Encoding(false, true).GetString(utf8);
            JObject root;
            using (var textReader = new StringReader(text))
            using (var jsonReader = new JsonTextReader(textReader)
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double,
                MaxDepth = 16
            })
            {
                root = JObject.Load(jsonReader, new JsonLoadSettings
                {
                    CommentHandling = CommentHandling.Load,
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    LineInfoHandling = LineInfoHandling.Load
                });
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.Comment)
                        return LayoutLoadResult.Failure("LAYOUT_COMMENTS_UNSUPPORTED", "JSON comments are not allowed in the desired layout.");

                    return LayoutLoadResult.Failure("LAYOUT_TRAILING_CONTENT", "The desired layout contains trailing JSON content.");
                }
            }

            if (root.DescendantsAndSelf().Any(token => token.Type == JTokenType.Comment))
            {
                return LayoutLoadResult.Failure("LAYOUT_COMMENTS_UNSUPPORTED", "JSON comments are not allowed in the desired layout.");
            }

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                FloatParseHandling = FloatParseHandling.Double,
                DateParseHandling = DateParseHandling.None
            });
            var document = root.ToObject<LayoutDocument>(serializer);
            if (document is null)
            {
                return LayoutLoadResult.Failure("LAYOUT_INVALID", "The desired layout did not contain an object.");
            }

            var validation = Validate(document);
            if (validation is not null)
            {
                return validation;
            }

            return LayoutLoadResult.Success(document, Sha256(utf8));
        }
        catch (DecoderFallbackException)
        {
            return LayoutLoadResult.Failure("LAYOUT_NOT_UTF8", "The desired layout must be valid UTF-8.");
        }
        catch (JsonException exception)
        {
            return LayoutLoadResult.Failure("LAYOUT_INVALID_JSON", Limit(exception.Message));
        }
        catch (Exception exception)
        {
            return LayoutLoadResult.Failure("LAYOUT_READ_ERROR", Limit(exception.Message));
        }
    }

    private static LayoutLoadResult? Validate(LayoutDocument document)
    {
        if (document.SchemaVersion != SupportedSchemaVersion)
        {
            return LayoutLoadResult.Failure("LAYOUT_VERSION_UNSUPPORTED", $"Layout schema version {document.SchemaVersion} is unsupported.");
        }

        var panel = document.Panel;
        if (panel is null)
        {
            return LayoutLoadResult.Failure("LAYOUT_PANEL_REQUIRED", "The panel object is required.");
        }

        if (panel.Count != 3)
        {
            return LayoutLoadResult.Failure("LAYOUT_PANEL_COUNT", "Exactly three panels are required.");
        }

        if (panel.NativeWidthPx <= 0 || panel.NativeHeightPx <= 0 ||
            !PositiveFinite(panel.PhysicalWidthMm) || !PositiveFinite(panel.PhysicalHeightMm))
        {
            return LayoutLoadResult.Failure("LAYOUT_PANEL_DIMENSIONS", "Panel pixel and physical dimensions must be positive.");
        }

        if (!NonNegativeFinite(panel.BezelWidthMm) ||
            (panel.CurveRadiusMm.HasValue && !PositiveFinite(panel.CurveRadiusMm.Value)))
        {
            return LayoutLoadResult.Failure("LAYOUT_PANEL_GEOMETRY", "Bezel width and curve radius are invalid.");
        }

        var geometry = document.Geometry;
        if (geometry is null)
        {
            return LayoutLoadResult.Failure("LAYOUT_GEOMETRY_REQUIRED", "The geometry object is required.");
        }

        if (!PositiveFinite(geometry.EyeDistanceMm) || !Finite(geometry.EyeHeightAbovePanelCenterMm) ||
            !Angle(geometry.LeftYawDegrees) || !Angle(geometry.RightYawDegrees))
        {
            return LayoutLoadResult.Failure("LAYOUT_GEOMETRY_INVALID", "Eye distance, eye height, and panel yaw values are invalid.");
        }

        var output = document.Output;
        if (output is null || string.IsNullOrWhiteSpace(output.Mode))
        {
            return LayoutLoadResult.Failure("LAYOUT_OUTPUT_REQUIRED", "The output mode is required.");
        }

        if (output.Mode != "nvidia-surround" && output.Mode != "borderless-span" && output.Mode != "separate-displays")
        {
            return LayoutLoadResult.Failure("LAYOUT_OUTPUT_MODE", "The requested output mode is unknown.");
        }

        if (output.CombinedWidthPx.HasValue != output.CombinedHeightPx.HasValue ||
            output.CombinedWidthPx.GetValueOrDefault(1) <= 0 || output.CombinedHeightPx.GetValueOrDefault(1) <= 0)
        {
            return LayoutLoadResult.Failure("LAYOUT_OUTPUT_DIMENSIONS", "Combined output width and height must both be omitted or both be positive.");
        }

        return null;
    }

    private static bool Angle(double value) => Finite(value) && value >= 0d && value < 90d;
    private static bool PositiveFinite(double value) => Finite(value) && value > 0d;
    private static bool NonNegativeFinite(double value) => Finite(value) && value >= 0d;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static string Sha256(byte[] bytes)
    {
        using var algorithm = SHA256.Create();
        var hash = algorithm.ComputeHash(bytes);
        var output = new StringBuilder(hash.Length * 2);
        foreach (var value in hash) output.Append(value.ToString("x2"));
        return output.ToString();
    }

    private static string Limit(string message)
    {
        var singleLine = (message ?? "Unknown JSON error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 450 ? singleLine : singleLine.Substring(0, 450);
    }
}

public sealed class LayoutLoadResult
{
    private LayoutLoadResult(bool isSuccess, LayoutDocument? document, string? sha256, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Document = document;
        Sha256 = sha256;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public LayoutDocument? Document { get; }
    public string? Sha256 { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static LayoutLoadResult Success(LayoutDocument document, string sha256) => new(true, document, sha256, null, null);
    public static LayoutLoadResult Failure(string code, string message) => new(false, null, null, code, message);
}
