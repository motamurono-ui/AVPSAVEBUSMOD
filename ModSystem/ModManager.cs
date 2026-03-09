using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EventBusSystem.SaveSystem;

namespace ModSystem
{
    /// <summary>
    /// The main mod manager. Discovers, loads, manages, and unloads mods.
    /// Integrates with EventBus and SaveManager.
    ///
    /// Mod folder structure:
    ///   StreamingAssets/Mods/
    ///     my_cool_mod/
    ///       mod.json
    ///       my_cool_mod.bundle    (AssetBundle — optional)
    ///       MyCoolMod.dll         (compiled mod code — optional)
    ///       textures/             (loose PNGs — optional)
    ///       audio/                (loose WAV/OGG — optional)
    /// </summary>  
    /// 



    using EventBusSystem;

    public struct ModDiscoveredEvent : IEvent { public string ModId; public string ModName; }
    public struct ModLoadedEvent : IEvent { public string ModId; public string ModName; }
    public struct ModUnloadedEvent : IEvent { public string ModId; }
    public struct ModErrorEvent : IEvent { public string ModId; public string Error; }
    public struct ModConflictEvent : IEvent { public string AssetKey; public string WinnerModId; public string LoserModId; }
    public struct ModsAllLoadedEvent : IEvent { public int TotalLoaded; public int TotalConflicts; }
    public struct ModAssetReplacedEvent : IEvent { public string AssetKey; public string ModId; }
    public struct ModUIReplacedEvent : IEvent { public string PanelId; public string ModId; }

    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────────────
        public static string ModsRootPath
        {
            get
            {
#if UNITY_EDITOR
                // In editor, load from Assets/Mods so AssetDatabase can access them
                return System.IO.Path.Combine(Application.dataPath, "Mods");
#else //IMPORTANT IN BUILDS - DO NOT LOAD FROM ASSETS, MUST BE STREAMINGASSETS OR PERSISTENTDATA
        // In builds, load from StreamingAssets
        return System.IO.Path.Combine(Application.streamingAssetsPath, "Mods");
#endif
            }
        }

        // State
        readonly Dictionary<string, ModEntry> _mods = new();
        readonly ModRegistry _registry = new();
        readonly List<IMod> _activeMods = new(); // for Update()

        public IReadOnlyDictionary<string, ModEntry> Mods => _mods;
        public ModRegistry Registry => _registry;

        // Singleton

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            foreach (var mod in _activeMods)
                if (mod.ReceivesUpdate) mod.OnUpdate();
        }

        void OnDestroy() => UnloadAll();

        // Discovery

        /// <summary>
        /// Scan the Mods folder, parse all mod.json manifests, and register
        /// each mod as Discovered (not yet loaded).
        /// </summary>
        public void DiscoverMods()
        {
            _mods.Clear();

            if (!Directory.Exists(ModsRootPath))
            {
                Directory.CreateDirectory(ModsRootPath);
                Debug.Log($"[ModManager] Created mods folder at: {ModsRootPath}");
                return;
            }

            foreach (var dir in Directory.GetDirectories(ModsRootPath))
            {
                string manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = JsonUtility.FromJson<ModManifest>(json);

                    if (string.IsNullOrEmpty(manifest.modId))
                    {
                        Debug.LogWarning($"[ModManager] Skipping mod in '{dir}': modId is empty.");
                        continue;
                    }

                    _mods[manifest.modId] = new ModEntry
                    {
                        Manifest = manifest,
                        Status = ModStatus.Discovered,
                        RootPath = dir,
                    };

                    EventBusSystem.EventBus<ModDiscoveredEvent>.Raise(
                        new ModDiscoveredEvent
                        {
                            ModId = manifest.modId,
                            ModName = manifest.modName,
                        });

                    Debug.Log($"[ModManager] Discovered: {manifest.modName} v{manifest.version} by {manifest.author}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModManager] Failed to parse mod.json in '{dir}': {ex.Message}");
                }
            }

            Debug.Log($"[ModManager] Discovered {_mods.Count} mod(s).");
        }

        //Loading

        /// <summary>
        /// Load all discovered mods in priority order (lowest first so highest wins).
        /// Pass a ModSaveData to restore enabled/disabled state from a save file.
        /// </summary>
        public async Task LoadAllAsync(ModSaveData saveData = null)
        {
            // Apply load order + enabled state from save
            if (saveData != null)
                ApplySaveData(saveData);

            // Sort by priority ascending (high priority loads last → wins conflicts)
            var ordered = _mods.Values
                .Where(m => m.Status == ModStatus.Discovered)
                .OrderBy(m => m.Manifest.priority)
                .ToList();

            // Resolve dependencies first
            ordered = ResolveDependencyOrder(ordered);

            int totalConflicts = 0;

            foreach (var entry in ordered)
                await LoadModAsync(entry);

            totalConflicts = _registry.ConflictCount;

            EventBusSystem.EventBus<ModsAllLoadedEvent>.Raise(
                new ModsAllLoadedEvent
                {
                    TotalLoaded = _mods.Values.Count(m => m.Status == ModStatus.Active),
                    TotalConflicts = totalConflicts,
                });

            Debug.Log($"[ModManager] All mods loaded. Active: " +
                      $"{_mods.Values.Count(m => m.Status == ModStatus.Active)} | " +
                      $"Conflicts: {totalConflicts}");
        }

        /// <summary>Load a single mod by ID.</summary>
        public async Task LoadModAsync(string modId)
        {
            if (_mods.TryGetValue(modId, out var entry))
                await LoadModAsync(entry);
        }

        async Task LoadModAsync(ModEntry entry)
        {
            if (entry.Status == ModStatus.Disabled) return;
            if (entry.Status == ModStatus.Active) return;

            entry.Status = ModStatus.Loading;
            var manifest = entry.Manifest;

            try
            {
                // 1. Load AssetBundle (if specified)
                if (!string.IsNullOrEmpty(manifest.assetBundleName))
                {
                    string bundlePath = Path.Combine(entry.RootPath, manifest.assetBundleName);
                    var bundleAssets = await ModAssetLoader.LoadBundleAsync(manifest.modId, bundlePath);

                    foreach (var kv in bundleAssets)
                    {
                        _registry.RegisterAsset(kv.Key, kv.Value, manifest.modId, manifest.priority);
                        entry.LoadedAssetKeys.Add(kv.Key);

                        EventBusSystem.EventBus<ModAssetReplacedEvent>.Raise(
                            new ModAssetReplacedEvent
                            { AssetKey = kv.Key, ModId = manifest.modId });
                    }
                }

                // 2. Load loose textures
                string texFolder = Path.Combine(entry.RootPath, "textures");
                var looseTextures = await ModAssetLoader.LoadLooseTexturesAsync(manifest.modId, texFolder);
                foreach (var kv in looseTextures)
                {
                    _registry.RegisterAsset(kv.Key, kv.Value, manifest.modId, manifest.priority);
                    entry.LoadedAssetKeys.Add(kv.Key);
                }

                // 3. Load loose audio
                string audioFolder = Path.Combine(entry.RootPath, "audio");
                var looseAudio = await ModAssetLoader.LoadLooseAudioAsync(manifest.modId, audioFolder);
                foreach (var kv in looseAudio)
                {
                    _registry.RegisterAsset(kv.Key, kv.Value, manifest.modId, manifest.priority);
                    entry.LoadedAssetKeys.Add(kv.Key);
                }

                // 4. Load DLL and initialise IMod instances
                if (!string.IsNullOrEmpty(manifest.dllName))
                {
                    string dllPath = Path.Combine(entry.RootPath, manifest.dllName);
                    var mods = ModDllLoader.LoadMod(manifest.modId, dllPath);

                    var ctx = new ModContext(manifest.modId, manifest.priority, _registry);

                    foreach (var mod in mods)
                    {
                        mod.OnLoad(ctx);
                        _activeMods.Add(mod);
                        entry.Instance = mod; // store last (usually only one)
                    }
                }

                // 5. Track conflicts
                entry.ConflictKeys = entry.LoadedAssetKeys
                    .Where(k => _registry.AllAssets.TryGetValue(k, out var a) && a.HasConflict)
                    .ToList();

                entry.Status = entry.ConflictKeys.Count > 0
                    ? ModStatus.Conflicted
                    : ModStatus.Active;

                EventBusSystem.EventBus<ModLoadedEvent>.Raise(
                    new ModLoadedEvent
                    {
                        ModId = manifest.modId,
                        ModName = manifest.modName,
                    });

                Debug.Log($"[ModManager] Loaded: {manifest.modName} " +
                          $"({entry.LoadedAssetKeys.Count} assets, " +
                          $"{entry.ConflictKeys.Count} conflicts)");
            }
            catch (Exception ex)
            {
                entry.Status = ModStatus.Error;
                entry.ErrorMessage = ex.Message;

                EventBusSystem.EventBus<ModErrorEvent>.Raise(
                    new ModErrorEvent
                    { ModId = manifest.modId, Error = ex.Message });

                Debug.LogError($"[ModManager] Error loading mod '{manifest.modId}': {ex.Message}");
            }
        }

        // Unloading

        public void UnloadMod(string modId)
        {
            if (!_mods.TryGetValue(modId, out var entry)) return;

            entry.Instance?.OnUnload();
            _activeMods.RemoveAll(m => m.ModId == modId);

            ModAssetLoader.UnloadBundle(modId);
            ModDllLoader.UnloadMod(modId);
            _registry.RemoveModEntries(modId);

            entry.LoadedAssetKeys.Clear();
            entry.Status = ModStatus.Discovered;

            EventBusSystem.EventBus<ModUnloadedEvent>.Raise(
                new ModUnloadedEvent { ModId = modId });

            Debug.Log($"[ModManager] Unloaded mod '{modId}'.");
        }

        public void UnloadAll()
        {
            foreach (var id in _mods.Keys.ToList()) UnloadMod(id);
            ModAssetLoader.UnloadAll();
        }

        // Enable / Disable

        public void EnableMod(string modId)
        {
            if (_mods.TryGetValue(modId, out var entry))
                entry.Status = ModStatus.Discovered;
        }

        public void DisableMod(string modId)
        {
            if (!_mods.TryGetValue(modId, out var entry)) return;
            if (entry.Status == ModStatus.Active) UnloadMod(modId);
            entry.Status = ModStatus.Disabled;
        }

        // Save / Load integration

        /// <summary>Collect current mod state into a ModSaveData for SaveManager.</summary>
        public ModSaveData CollectSaveData()
        {
            var data = new ModSaveData();

            foreach (var kv in _mods)
            {
                if (kv.Value.Status == ModStatus.Disabled)
                    data.DisabledModIds.Add(kv.Key);
                else
                    data.EnabledModIds.Add(kv.Key);

                data.LoadOrder.Add(new ModLoadOrderEntry
                {
                    ModId = kv.Key,
                    Priority = kv.Value.Manifest.priority,
                });
            }

            return data;
        }

        void ApplySaveData(ModSaveData saveData)
        {
            foreach (var id in saveData.DisabledModIds)
                if (_mods.TryGetValue(id, out var e))
                    e.Status = ModStatus.Disabled;

            foreach (var entry in saveData.LoadOrder)
                if (_mods.TryGetValue(entry.ModId, out var e))
                    e.Manifest.priority = entry.Priority;
        }

        // Helpers

        public ModEntry GetMod(string modId) =>
            _mods.TryGetValue(modId, out var e) ? e : null;

        public T GetAsset<T>(string key) where T : UnityEngine.Object =>
            _registry.GetAsset<T>(key);

        public T GetValue<T>(string key, T defaultValue = default) =>
            _registry.GetValue(key, defaultValue);

        // ensure dependencies load before dependents
        List<ModEntry> ResolveDependencyOrder(List<ModEntry> mods)
        {
            var sorted = new List<ModEntry>();
            var visited = new HashSet<string>();

            void Visit(ModEntry entry)
            {
                if (visited.Contains(entry.Manifest.modId)) return;
                visited.Add(entry.Manifest.modId);

                foreach (var dep in entry.Manifest.dependencies)
                {
                    if (_mods.TryGetValue(dep, out var depEntry))
                        Visit(depEntry);
                    else
                        Debug.LogWarning($"[ModManager] Mod '{entry.Manifest.modId}' depends on " +
                                         $"'{dep}' which is not installed.");
                }

                sorted.Add(entry);
            }

            foreach (var m in mods) Visit(m);
            return sorted;
        }
    }
}