using System.Collections.Generic;
using MessagePack;

namespace EventBusSystem.SaveSystem
{
    /// <summary>
    /// Persisted alongside your game save — tracks which mods were active
    /// and their per-mod settings/state. Add to SaveData as [Key(7)].
    /// </summary>
    /// // This is almost the same as the SAVEDATA just for mods, it is separated just for organisational purposes, you can merge them if you want but it is not recommended.
    [MessagePackObject]
    public class ModSaveData
    {
        [Key(0)] public List<string> EnabledModIds { get; set; } = new();
        [Key(1)] public List<string> DisabledModIds { get; set; } = new();
        [Key(2)] public List<ModLoadOrderEntry> LoadOrder { get; set; } = new();
        [Key(3)] public List<ModPersistedSetting> ModSettings { get; set; } = new();
    }

    [MessagePackObject]
    public class ModLoadOrderEntry
    {
        [Key(0)] public string ModId { get; set; }
        [Key(1)] public int Priority { get; set; }
    }

    [MessagePackObject]
    public class ModPersistedSetting
    {
        [Key(0)] public string ModId { get; set; }
        [Key(1)] public string Key { get; set; }
        [Key(2)] public string Value { get; set; } // serialised as string, cast on read
    }
}