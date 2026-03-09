using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using EventBusSystem;
using EventBusSystem.SaveSystem;

namespace ModSystem
{
    public class ModManagerStarter : MonoBehaviour
    {
        [SerializeField] bool loadModsOnStart = true;

        void Awake()
        {
            var go = new GameObject("[ModManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<ModManager>();
        }

        void Start()
        {
            if (loadModsOnStart)
                StartCoroutine(LoadModsNextFrame());
        }

        // Wait one frame so UIScreenManager and UIModBridge
        // have fully initialized their OnEnable/Awake
        IEnumerator LoadModsNextFrame()
        {
            yield return null;
            _ = LoadMods();
        }

        async Task LoadMods()
        {
            var manager = ModManager.Instance;

            manager.DiscoverMods();

            ModSaveData modSave = null;
            if (SaveManager.Instance != null && SaveManager.Instance.SlotExists(0))
            {
                var save = await SaveManager.Instance.LoadAsync(0);
                modSave = save?.Mods;
            }

            await manager.LoadAllAsync(modSave);
        }
    }
}