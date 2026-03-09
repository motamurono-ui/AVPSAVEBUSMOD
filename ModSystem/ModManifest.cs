using System;
using System.Collections.Generic;

namespace ModSystem
{
    /// <summary>
    /// Deserialised from each mod's mod.json file.
    ///
    /// Example mod.json:
    /// {
    ///   "modId":      "skin_pack",
    ///   "modName":    "Skin Pack",
    ///   "version":    "1.0.0",
    ///   "author":     "TheMODDER",
    ///   "description":"Replaces skins.",
    ///   "priority":   10,
    ///   "assetBundleName": "coolskinpack",
    ///   "dllName":    "CoolSkinPack.dll",
    ///   "dependencies": [],
    ///   "tags": ["cosmetic", "skin"]
    /// }
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        public string modId;
        public string modName;
        public string version;
        public string author;
        public string description;
        public int priority;           // higher = loaded last (wins conflicts)
        public string assetBundleName;    // optional — omit if no assets
        public string dllName;            // optional — omit if no code
        public List<string> dependencies = new();
        public List<string> tags = new();
    }

    //tracking status of each mod during loading and runtime
    public enum ModStatus
    {
        Discovered,   // found on disk, not yet loaded
        Loading,      // currently loading assets/dll
        Active,       // fully loaded and running
        Disabled,     // user-disabled, not loaded
        Error,        // failed to load
        Conflicted,   // loaded but has unresolved conflicts
    }

    public class ModEntry
    {
        public ModManifest Manifest;
        public ModStatus Status;
        public string RootPath;       // absolute path to mod folder
        public IMod Instance;       // null if no DLL
        public string ErrorMessage;
        public List<string> LoadedAssetKeys = new();
        public List<string> ConflictKeys = new();
    }
}