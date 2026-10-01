using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

// The unified owner supplies an unregistered entry to retain legacy settings semantics.
public static class FeatureModule
{
    public static bool Load(UnityModManager.ModEntry entry) => Main.LoadFeature(entry);
}
