using System.Collections.Generic;
using UnityEngine;

namespace ModSystem
{
    // Mod lifecycle need to be implemented in .dll to allow mods to run code.

    /// <summary>
    /// Every mod DLL must contain at least one class implementing IMod.
    /// The ModManager will find it via reflection and call these in order.
    /// </summary>
    public interface IMod
    {
        string ModId { get; }
        string ModName { get; }
        string ModVersion { get; }

        /// <summary>Called once after the mod's assets are loaded. Setup hooks here.</summary>
        void OnLoad(IModContext context);

        /// <summary>Called when the mod is disabled or the game shuts down.</summary>
        void OnUnload();

        /// <summary>Called every frame (optional — return false to skip).</summary>
        bool ReceivesUpdate { get; }
        void OnUpdate();
    }

    /// <summary>
    /// Passed to IMod.OnLoad — gives mods controlled access to game systems
    /// without exposing the entire ModManager.
    /// </summary>
    public interface IModContext
    {
        /// <summary>Get a loaded asset by its mod-relative path key.</summary>
        T GetAsset<T>(string key) where T : Object;

        /// <summary>Replace a named UI panel with a prefab from this mod.</summary>
        void ReplaceUI(string panelId, GameObject prefab);

        /// <summary>Register a named gameplay value override (float, int, string).</summary>
        void RegisterOverride(string key, object value);

        /// <summary>Fire an EventBus event from mod code.</summary>
        void RaiseEvent<T>(T @event) where T : EventBusSystem.IEvent;

        /// <summary>Log a message attributed to this mod.</summary>
        void Log(string message);
    }

    // Asset override entry

    public class AssetOverride
    {
        public string Key;        // e.g. "textures/player_skin"
        public Object Asset;
        public string SourceModId;
        public bool HasConflict;
        public List<string> ConflictingMods = new();
    }

    // Gameplay value override

    public class ValueOverride
    {
        public string Key;
        public object Value;
        public string SourceModId;
        public bool HasConflict;
        public List<string> ConflictingMods = new();
    }
}