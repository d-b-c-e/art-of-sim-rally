using System;
using System.IO;
using System.Xml.Serialization;

namespace ArtOfSimRally.Mod
{
    internal static class SettingsPersistence
    {
        // UMM's ModSettings.Save catches and logs errors internally. Use its
        // exact XML serializer format ourselves so failure can reach SaveSettings
        // and keep the deferred retry pending. A failed replacement leaves the
        // previous Settings.xml intact instead of truncating it first.
        public static void Write(object settings, string path)
        {
            path = Path.GetFullPath(path);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                    new XmlSerializer(settings.GetType()).Serialize(writer, settings);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch { /* Preserve the original write exception for the caller. */ }
            }
        }
    }
}
