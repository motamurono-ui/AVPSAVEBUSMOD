using System.Collections.Generic;
using UnityEngine;

namespace ModSystem
{
    /// <summary>
    /// Handles replacing live UI panels with mod-provided prefabs at runtime.
    ///
    /// Usage in your UI system:
    ///   var panel = ModUIManager.Instance.GetPanel("hud_healthbar");
    ///   if (panel != null) Instantiate(panel, canvas.transform);
    ///
    /// Register your panels in the Unity scene by calling RegisterPanel() on
    /// any GameObject that should be replaceable by mods.
    /// </summary>
    public class ModUIManager : MonoBehaviour
    {
        public static ModUIManager Instance { get; private set; }

        // Original panels: panelId → original GameObject in scene
        readonly Dictionary<string, GameObject> _originals = new();
        // Active panel instances (original or mod replacement)
        readonly Dictionary<string, GameObject> _active = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            // React to UI replacement events fired by mods
            var binding = new EventBusSystem.EventBinding<ModUIReplacedEvent>(OnUIReplaced);
            EventBusSystem.EventBus<ModUIReplacedEvent>.Register(binding);
        }

        /// <summary>
        /// Register an original scene panel so mods can replace it.
        /// Call this from your UI's Awake/Start.
        /// </summary>
        public void RegisterPanel(string panelId, GameObject panel)
        {
            _originals[panelId] = panel;
            _active[panelId] = panel;

            // If a mod already registered an override before this panel
            // was registered, apply it immediately
            if (ModManager.Instance != null &&
                ModManager.Instance.Registry.HasUIPanel(panelId))
            {
                ApplyOverride(panelId);
            }
        }

        /// <summary>Get the currently active panel (original or mod replacement).</summary>
        public GameObject GetActivePanel(string panelId)
        {
            _active.TryGetValue(panelId, out var panel);
            return panel;
        }

        void OnUIReplaced(ModUIReplacedEvent e) => ApplyOverride(e.PanelId);

        void ApplyOverride(string panelId)
        {
            var (prefab, modId) = ModManager.Instance.Registry.GetUIPanel(panelId);
            if (prefab == null) return;

            // Deactivate original
            if (_originals.TryGetValue(panelId, out var original))
                original.SetActive(false);

            // Destroy old replacement if present
            if (_active.TryGetValue(panelId, out var current) && current != original)
                Destroy(current);

            // Find the canvas to parent under
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var instance = Instantiate(prefab, canvas.transform);
            _active[panelId] = instance;

            Debug.Log($"[ModUIManager] Panel '{panelId}' replaced by mod '{modId}'.");
        }

        /// <summary>Restore the original panel (called when a mod is unloaded).</summary>
        public void RestorePanel(string panelId)
        {
            if (!_originals.TryGetValue(panelId, out var original)) return;

            if (_active.TryGetValue(panelId, out var current) && current != original)
                Destroy(current);

            original.SetActive(true);
            _active[panelId] = original;
        }
    }
}