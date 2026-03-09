using MessagePack; //IMPORTANT: Make sure to install MessagePack-CSharp from NuGet and add the [MessagePackObject] attribute to your SaveData and SaveSlotMeta classes.
using System;

namespace EventBusSystem.SaveSystem
{
    [MessagePackObject]
    public class SaveData
    {
        //if KEY word is in red MessagePack isn't installed in the project, in the readme there is a link to the nuget package.
        [Key(0)] public int Version { get; set; } = SaveSystemConfig.CurrentSaveVersion;
        [Key(1)] public long SavedAtUtc { get; set; }
        [Key(2)] public float PlaytimeSeconds { get; set; }
        [Key(3)] public string LastScene { get; set; } = "";

        // Add your game data sections here
        [Key(4)] public PlayerSaveData Player { get; set; } = new();
        [Key(5)] public WorldSaveData World { get; set; } = new();
        [Key(6)] public SettingsSaveData Settings { get; set; } = new();
        [Key(7)] public ModSaveData Mods { get; set; } = new();
    }

    [MessagePackObject]
    public class SaveSlotMeta
    {
        [Key(0)] public int SlotIndex { get; set; }
        [Key(1)] public string SlotName { get; set; } = "New Game";
        [Key(2)] public long SavedAtUtc { get; set; }
        [Key(3)] public int SaveVersion { get; set; }
        [Key(4)] public string SceneName { get; set; } = "";
        [Key(5)] public float PlaytimeSeconds { get; set; }
        [Key(6)] public bool IsEmpty { get; set; } = true;

        [IgnoreMember]
        public DateTime SavedAt =>
            DateTimeOffset.FromUnixTimeSeconds(SavedAtUtc).LocalDateTime;
    }

    // ── Replace these with your actual game data ──────────────────────────────

    [MessagePackObject]
    public class PlayerSaveData
    {
        [Key(0)] public float Health { get; set; } = 100f;
        [Key(1)] public float MaxHealth { get; set; } = 100f;
        [Key(2)] public int Level { get; set; } = 1;
        [Key(3)] public int Experience { get; set; } = 0;
        [Key(4)] public int Gold { get; set; } = 0;
        [Key(5)] public float[] Position { get; set; } = { 0f, 0f, 0f };
    }

    [MessagePackObject]
    public class WorldSaveData
    {
        [Key(0)] public int DayNumber { get; set; } = 1;
        [Key(1)] public float TimeOfDay { get; set; } = 0.5f;
        [Key(2)] public string[] CompletedQuests { get; set; } = Array.Empty<string>();
        [Key(3)] public string[] UnlockedAreas { get; set; } = Array.Empty<string>();
    }

    [MessagePackObject]
    public class SettingsSaveData
    {
        [Key(0)] public float MasterVolume { get; set; } = 1f;
        [Key(1)] public float MusicVolume { get; set; } = 0.8f;
        [Key(2)] public float SfxVolume { get; set; } = 1f;
        [Key(3)] public int QualityLevel { get; set; } = 2;
        [Key(4)] public bool Fullscreen { get; set; } = true;
    }
}