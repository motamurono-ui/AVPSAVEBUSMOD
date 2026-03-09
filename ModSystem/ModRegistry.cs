using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModSystem
{
    /// <summary>
    /// Central store for all asset and value overrides across all mods.
    /// Handles merge strategy: higher-priority mod wins
    /// </summary>
    public class ModRegistry
    {
        // Asset overrides: "modId/assetname" or "textures/player_skin" etc.
        readonly Dictionary<string, AssetOverride> _assets = new();

        // Gameplay value overrides: any string key => object value
        readonly Dictionary<string, ValueOverride> _values = new();

        // UI panel overrides: panelId => prefab
        readonly Dictionary<string, (GameObject Prefab, string ModId)> _uiPanels = new();

        // ── Assets ────────────────────────────────────────────────────────────

        /// <summary>
        /// Register an asset. If a key already exists, merge:
        /// higher priority mod wins, conflict is flagged on both.
        /// </summary>
        public void RegisterAsset(string key, Object asset, string modId, int priority)
        {
            if (_assets.TryGetValue(key, out var existing))
            {
                // Flag conflict on existing entry
                if (!existing.ConflictingMods.Contains(modId))
                    existing.ConflictingMods.Add(modId);
                existing.HasConflict = true;

                // Higher priority wins
                var existingMod = ModManager.Instance.GetMod(existing.SourceModId);
                int existingPriority = existingMod?.Manifest.priority ?? 0;

                if (priority >= existingPriority)
                {
                    EventBusSystem.EventBus<ModConflictEvent>.Raise(
                        new ModConflictEvent
                        {
                            AssetKey = key,
                            WinnerModId = modId,
                            LoserModId = existing.SourceModId,
                        });

                    existing.Asset = asset;
                    existing.SourceModId = modId;
                    Debug.LogWarning($"[ModRegistry] Conflict on '{key}': '{modId}' overrides '{existingMod?.Manifest.modId}'.");
                }
                else
                {
                    Debug.LogWarning($"[ModRegistry] Conflict on '{key}': '{modId}' lost to '{existing.SourceModId}' (lower priority).");
                }
            }
            else
            {
                _assets[key] = new AssetOverride
                {
                    Key = key,
                    Asset = asset,
                    SourceModId = modId,
                    HasConflict = false,
                };
            }
        }

        public T GetAsset<T>(string key) where T : Object
        {
            if (_assets.TryGetValue(key, out var entry))
                return entry.Asset as T;
            return null;
        }

        public bool HasAsset(string key) => _assets.ContainsKey(key);

        //Values

        public void RegisterValue(string key, object value, string modId, int priority)
        {
            if (_values.TryGetValue(key, out var existing))
            {
                if (!existing.ConflictingMods.Contains(modId))
                    existing.ConflictingMods.Add(modId);
                existing.HasConflict = true;

                var existingMod = ModManager.Instance.GetMod(existing.SourceModId);
                int existingPriority = existingMod?.Manifest.priority ?? 0;

                if (priority >= existingPriority)
                {
                    existing.Value = value;
                    existing.SourceModId = modId;
                }
            }
            else
            {
                _values[key] = new ValueOverride
                {
                    Key = key,
                    Value = value,
                    SourceModId = modId,
                };
            }
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_values.TryGetValue(key, out var entry) && entry.Value is T v)
                return v;
            return defaultValue;
        }

        //UI

        public void RegisterUIPanel(string panelId, GameObject prefab, string modId)
        {
            _uiPanels[panelId] = (prefab, modId);
        }

        public (GameObject Prefab, string ModId) GetUIPanel(string panelId)
        {
            _uiPanels.TryGetValue(panelId, out var entry);
            return entry;
        }

        public bool HasUIPanel(string panelId) => _uiPanels.ContainsKey(panelId);

        //Diagnostics

        public IReadOnlyDictionary<string, AssetOverride> AllAssets => _assets;
        public IReadOnlyDictionary<string, ValueOverride> AllValues => _values;
        public int ConflictCount => _assets.Values.Count(a => a.HasConflict)
                                  + _values.Values.Count(v => v.HasConflict);

        public void RemoveModEntries(string modId)
        {
            var assetKeys = _assets.Where(kv => kv.Value.SourceModId == modId)
                                   .Select(kv => kv.Key).ToList();
            foreach (var k in assetKeys) _assets.Remove(k);

            var valueKeys = _values.Where(kv => kv.Value.SourceModId == modId)
                                   .Select(kv => kv.Key).ToList();
            foreach (var k in valueKeys) _values.Remove(k);

            var uiKeys = _uiPanels.Where(kv => kv.Value.ModId == modId)
                                  .Select(kv => kv.Key).ToList();
            foreach (var k in uiKeys) _uiPanels.Remove(k);
        }
    }
}