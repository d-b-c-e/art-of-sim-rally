using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

public sealed class Settings : UnityModManager.ModSettings
{
    // Deliberately off until the attended center-panel experiment is performed.
    public bool EnableCenterPanelPreview;

    // Safe escape hatch for development only. The default refuses to touch the
    // camera unless Screen.width/height exactly match the optimizer contract.
    public bool AllowOutputResolutionMismatch;
}
