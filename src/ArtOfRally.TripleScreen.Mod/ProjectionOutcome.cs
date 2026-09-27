using System;
using System.Collections.Generic;
using Dbce.TripleScreen.Protocol;

namespace ArtOfRally.TripleScreen.Mod;

internal sealed class ProjectionOutcome
{
    private ProjectionOutcome(
        string state,
        LayoutLoadResult layout,
        int activeCameraCount,
        IEnumerable<string> capabilities,
        DateTime? lastSuccessfulFrameUtc,
        RuntimeDiagnostic diagnostic)
    {
        State = state;
        Layout = layout;
        ActiveCameraCount = activeCameraCount;
        ActiveCapabilities = new List<string>(capabilities);
        LastSuccessfulFrameUtc = lastSuccessfulFrameUtc;
        Diagnostic = diagnostic;
    }

    internal string State { get; }
    internal LayoutLoadResult Layout { get; }
    internal int ActiveCameraCount { get; }
    internal List<string> ActiveCapabilities { get; }
    internal DateTime? LastSuccessfulFrameUtc { get; }
    internal RuntimeDiagnostic Diagnostic { get; }

    internal static ProjectionOutcome Inactive(LayoutLoadResult layout, string code, string message) =>
        Create("inactive", layout, 0, null, code, "info", message);

    internal static ProjectionOutcome Starting(LayoutLoadResult layout, string code, string message) =>
        Create("starting", layout, 0, null, code, "info", message);

    internal static ProjectionOutcome CenterPreviewActive(LayoutLoadResult layout, DateTime lastFrameUtc, string code, string message) =>
        Create("active", layout, 1, lastFrameUtc, code, "warning", message, "asymmetric-frustum");

    internal static ProjectionOutcome ThreeViewActive(LayoutLoadResult layout, DateTime lastFrameUtc, string code, string message) =>
        Create("active", layout, 3, lastFrameUtc, code, "warning", message,
            "asymmetric-frustum", "three-projections", "surround-compositor");

    internal static ProjectionOutcome SeparateViewActive(LayoutLoadResult layout, DateTime lastFrameUtc, string code, string message) =>
        Create("active", layout, 3, lastFrameUtc, code, "warning", message,
            "asymmetric-frustum", "three-projections", "independent-displays");

    internal static ProjectionOutcome Degraded(LayoutLoadResult layout, string code, string message) =>
        Create("degraded", layout, 0, null, code, "warning", message);

    internal static ProjectionOutcome Rejected(string code, string message) =>
        Create("rejected", LayoutLoadResult.Failure(code, message), 0, null, code, "error", message);

    internal static ProjectionOutcome Error(LayoutLoadResult layout, string code, string message) =>
        Create("error", layout, 0, null, code, "error", message);

    private static ProjectionOutcome Create(
        string state,
        LayoutLoadResult layout,
        int cameras,
        DateTime? lastFrameUtc,
        string code,
        string severity,
        string message,
        params string[] capabilities) =>
        new(state, layout, cameras, capabilities, lastFrameUtc, new RuntimeDiagnostic(code, severity, message));
}
