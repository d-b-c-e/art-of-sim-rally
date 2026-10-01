using System;
using System.IO;
using ArtOfRally.TripleScreen.Mod;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfSimRally.Mod
{
    // The only shipping load entry point. Feature entries are not registered with UMM.
    public static class UnifiedEntry
    {
        [Serializable]
        private sealed class LoadMetadata
        {
            public string Id = string.Empty;
            public string AssemblyName = string.Empty;
            public string EntryMethod = string.Empty;
            public LoadMetadata() { }
        }

        public static bool Load(UnityModManager.ModEntry owner)
        {
            string modsPath = Path.GetDirectoryName(owner.Path.TrimEnd('\\', '/'));
            string triplePath = Path.Combine(modsPath, "DbceTripleScreenArtOfRally");
            try
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(modsPath), ".dbce-art-unified", "pending.json")))
                    throw new InvalidOperationException("Interrupted setup requires rollback before features can load.");
                foreach (string directory in Directory.GetDirectories(modsPath))
                {
                    string infoPath = Path.Combine(directory, "Info.json");
                    if (!File.Exists(infoPath)) continue;
                    if (new FileInfo(infoPath).Length > 32768) throw new InvalidOperationException("Oversized mod metadata; cannot verify unique load owner.");
                    var info = JsonUtility.FromJson<LoadMetadata>(File.ReadAllText(infoPath));
                    if (info == null) continue;
                    bool isOwner = string.Equals(directory.TrimEnd('\\', '/'), owner.Path.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
                    if ((info.Id == "ArtOfSimRally" && !isOwner) ||
                        (info.Id == "DbceTripleScreenArtOfRally" &&
                         (!string.Equals(directory, triplePath, StringComparison.OrdinalIgnoreCase) ||
                          !string.IsNullOrEmpty(info.AssemblyName) || !string.IsNullOrEmpty(info.EntryMethod))))
                        throw new InvalidOperationException("Conflicting load metadata found: " + info.Id);
                }
            }
            catch (Exception exception)
            {
                owner.Logger.Error("Unified load guard refused initialization: " + exception.Message);
                return false;
            }
            if (File.Exists(Path.Combine(triplePath, "ArtOfRally.TripleScreen.Mod.dll")))
            {
                owner.Logger.Error("Legacy triple renderer is still installed. Run the unified migration before loading either feature.");
                return false;
            }
            foreach (var entry in UnityModManager.modEntries)
                if (!ReferenceEquals(entry, owner) &&
                    (entry.Info.Id == "ArtOfSimRally" ||
                     (entry.Info.Id == "DbceTripleScreenArtOfRally" &&
                      (!string.IsNullOrEmpty(entry.Info.AssemblyName) || !string.IsNullOrEmpty(entry.Info.EntryMethod)))))
                {
                    owner.Logger.Error("Conflicting legacy or unified load owner detected; no features were started.");
                    return false;
                }

            var triple = new UnityModManager.ModEntry(new UnityModManager.ModInfo
            {
                Id = "DbceTripleScreenArtOfRally", Version = "0.3.12"
            }, triplePath + Path.DirectorySeparatorChar);
            if (!Main.Load(owner)) return false;
            var wheelGui = owner.OnGUI;
            var wheelSave = owner.OnSaveGUI;
            var wheelToggle = owner.OnToggle;
            var wheelUnload = owner.OnUnload;
            try
            {
                if (!FeatureModule.Load(triple)) throw new InvalidOperationException("Triple feature initialization failed.");
                int page = 0;
                owner.OnGUI = entry =>
                {
                    page = GUILayout.Toolbar(page, new[] { "Wheel, FFB and telemetry", "Triple-screen display" });
                    if (page == 0) wheelGui(entry); else triple.OnGUI(triple);
                };
                owner.OnSaveGUI = entry => { wheelSave(entry); triple.OnSaveGUI(triple); };
                owner.OnUpdate = (entry, delta) => triple.OnUpdate(triple, delta);
                owner.OnToggle = (entry, value) =>
                {
                    bool wheelResult = wheelToggle(entry, value);
                    bool tripleResult = triple.OnToggle(triple, value);
                    return wheelResult && tripleResult;
                };
                owner.OnUnload = entry =>
                {
                    try { return triple.OnUnload(triple); }
                    finally { wheelUnload(entry); }
                };
                return true;
            }
            catch (Exception exception)
            {
                owner.Logger.Error("Unified feature initialization failed: " + exception.Message);
                try { triple.OnUnload?.Invoke(triple); }
                finally { wheelUnload(owner); }
                return false;
            }
        }
    }
}
