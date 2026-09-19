using System;

namespace ArtOfSimRally.Mod
{
    // Resolve only persisted identities. The native toolkit validates presence and
    // FFB capability under strict GUID selection; never fall back by name/index.
    internal static class FfbSelection
    {
        public static bool FollowsSteering(Settings cfg) => cfg.FfbDeviceMode == "steering" ||
            (cfg.FfbDeviceMode != "explicit" && string.IsNullOrEmpty(cfg.PreferredDeviceGuid) &&
             string.IsNullOrEmpty(cfg.PreferredDevice) && cfg.PreferredDeviceIndex < 0);
        public static bool TryTarget(Settings cfg, out string name, out int index, out string guid, out string reason)
        {
            name = cfg.PreferredDevice; index = cfg.PreferredDeviceIndex; guid = cfg.PreferredDeviceGuid;
            reason = "";
            if (FollowsSteering(cfg))
            {
                var steering = WheelInput.Binding.Parse(cfg.SteerBinding);
                if (steering == null || steering.IsButton)
                { reason = "Bind Steering in Controls, or select an FFB device explicitly."; return false; }
                name = steering.Device; index = steering.DeviceIndex; guid = steering.InstanceGuid?.ToString("D") ?? "";
            }
            Guid identity;
            if (!Guid.TryParse(guid, out identity) || identity == Guid.Empty)
            { reason = "Saved selection has no verified device identity. Select the wheel again."; return false; }
            return true;
        }
    }
}
