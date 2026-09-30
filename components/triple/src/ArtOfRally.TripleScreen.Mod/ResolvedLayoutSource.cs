using System;
using System.Text;
using Dbce.TripleScreen.Protocol;
using Newtonsoft.Json;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>Builds the effective contract from either optional imported measurements or local setup.</summary>
internal sealed class ResolvedLayoutSource
{
    private string _key;
    private LayoutLoadResult _result;

    internal LayoutLoadResult Resolve(LayoutLoadResult imported, Settings settings)
    {
        var key = string.Join("|", imported.Sha256 ?? imported.ErrorCode ?? "missing",
            settings.UseLocalLayout, settings.ManualSetupConfirmed, settings.EnableThreeViewPrototype,
            settings.EnableSeparateDisplayPrototype, settings.PanelWidthPx, settings.PanelHeightPx,
            settings.PanelWidthMm, settings.PanelHeightMm, settings.EyeDistanceMm,
            settings.LeftYawDegrees, settings.RightYawDegrees);
        if (key == _key && _result != null) return _result;
        _key = key;

        if (settings.UseLocalLayout && !settings.ManualSetupConfirmed)
            return _result = LayoutLoadResult.Failure("LOCAL_SETUP_REQUIRED", "Enter your screen measurements in Advanced setup, then choose Use these measurements.");
        if (!settings.UseLocalLayout && (!imported.IsSuccess || imported.Document == null))
            return _result = LayoutLoadResult.Failure("LAYOUT_NOT_CONFIGURED", "No imported layout is available. Open Advanced setup to enter your screen measurements.");

        var input = settings.UseLocalLayout ? LocalDocument(settings) : imported.Document;
        var mode = settings.EnableSeparateDisplayPrototype ? "separate-displays" :
            !settings.UseLocalLayout && (input.Output?.Mode == "borderless-span" || input.Output?.Mode == "nvidia-surround")
                ? input.Output.Mode : "nvidia-surround";
        var output = new OutputDocument { Mode = mode };
        if (mode != "separate-displays" && !settings.UseLocalLayout && input.Output?.Mode == mode)
        {
            output.CombinedWidthPx = input.Output.CombinedWidthPx;
            output.CombinedHeightPx = input.Output.CombinedHeightPx;
        }
        else if (mode != "separate-displays" && input.Panel != null)
        {
            var width = (long)input.Panel.NativeWidthPx * 3;
            if (width <= int.MaxValue) output.CombinedWidthPx = (int)width;
            output.CombinedHeightPx = input.Panel.NativeHeightPx;
        }

        // Never mutate the imported contract. The effective hash must describe
        // the mode actually selected in this mod, even when measurements were imported.
        var document = new LayoutDocument
        {
            SchemaVersion = 1,
            Panel = input.Panel,
            Geometry = input.Geometry,
            Output = output
        };
        if (!settings.UseLocalLayout && input.Output != null &&
            input.Output.Mode == output.Mode &&
            input.Output.CombinedWidthPx == output.CombinedWidthPx &&
            input.Output.CombinedHeightPx == output.CombinedHeightPx)
            return _result = imported;

        var json = JsonConvert.SerializeObject(document);
        return _result = LayoutContractParser.Parse(Encoding.UTF8.GetBytes(json));
    }

    private static LayoutDocument LocalDocument(Settings settings) => new()
    {
        SchemaVersion = 1,
        Panel = new PanelDocument
        {
            Count = 3,
            NativeWidthPx = settings.PanelWidthPx,
            NativeHeightPx = settings.PanelHeightPx,
            PhysicalWidthMm = settings.PanelWidthMm,
            PhysicalHeightMm = settings.PanelHeightMm
        },
        Geometry = new GeometryDocument
        {
            EyeDistanceMm = settings.EyeDistanceMm,
            LeftYawDegrees = settings.LeftYawDegrees,
            RightYawDegrees = settings.RightYawDegrees
        }
    };
}
