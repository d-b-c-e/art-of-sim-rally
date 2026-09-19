namespace ArtOfSimRally.Mod
{
    internal static class SettingsMigration
    {
        public static bool NeedsHandbrakeSplit(Settings cfg) =>
            string.IsNullOrEmpty(cfg.HandbrakeButtonBinding) && WheelInput.Binding.Parse(cfg.HandbrakeBinding)?.IsButton == true;
        public static void SplitHandbrake(Settings cfg)
        {
            if (!NeedsHandbrakeSplit(cfg)) return;
            cfg.HandbrakeButtonBinding = cfg.HandbrakeBinding;
            cfg.HandbrakeBinding = "";
        }
    }
}
