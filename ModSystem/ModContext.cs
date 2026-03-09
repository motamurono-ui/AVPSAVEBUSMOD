using UnityEngine;

namespace ModSystem
{
    /// <summary>
    /// Concrete IModContext passed to each IMod.OnLoad().
    /// Gives mods a safe, scoped API into the game — no direct ModManager access.
    /// </summary>
    public class ModContext : IModContext
    {
        readonly string _modId;
        readonly int _priority;
        readonly ModRegistry _registry;

        public ModContext(string modId, int priority, ModRegistry registry)
        {
            _modId = modId;
            _priority = priority;
            _registry = registry;
        }

        public T GetAsset<T>(string key) where T : Object
            => _registry.GetAsset<T>(key);

        public void ReplaceUI(string panelId, GameObject prefab)
        {
            _registry.RegisterUIPanel(panelId, prefab, _modId);
            EventBusSystem.EventBus<ModUIReplacedEvent>.Raise(
                new ModUIReplacedEvent { PanelId = panelId, ModId = _modId });
            Log($"Registered UI override for panel '{panelId}'.");
        }

        public void RegisterOverride(string key, object value)
        {
            _registry.RegisterValue(key, value, _modId, _priority);
            Log($"Registered value override: '{key}' = {value}");
        }

        public void RaiseEvent<T>(T @event) where T : EventBusSystem.IEvent
            => EventBusSystem.EventBus<T>.Raise(@event);

        public void Log(string message)
            => Debug.Log($"[Mod:{_modId}] {message}");
    }
}