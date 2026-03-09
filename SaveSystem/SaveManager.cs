using System;
using System.Threading.Tasks;
using UnityEngine;

//<Summary>
// Central hub for saving/loading game state.
// Raise events on success/failure for UI and other systems to react to.
// Also keep in mind here stay the void that you problably need to setup the UI for the save/load menu, auto the auto-saving system.
//<Summary>


namespace EventBusSystem.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EventBus<SaveSystemReadyEvent>.Raise();
        }

        public async Task SaveAsync(int slot, SaveData data, string name = null)
        {
            if (!Valid(slot)) return;
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                data.SavedAtUtc = now;
                data.Version = SaveSystemConfig.CurrentSaveVersion;
                data.LastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                var meta = new SaveSlotMeta
                {
                    SlotIndex = slot,
                    SlotName = name ?? $"Slot {slot}",
                    SavedAtUtc = now,
                    SaveVersion = SaveSystemConfig.CurrentSaveVersion,
                    SceneName = data.LastScene,
                    PlaytimeSeconds = data.PlaytimeSeconds,
                    IsEmpty = false,
                };

                await SaveFileHandler.WriteAsync(slot, data, meta);
                EventBus<GameSavedEvent>.Raise(new GameSavedEvent
                { SlotIndex = slot, SlotName = meta.SlotName, TimestampUtc = now });
            }
            catch (Exception ex) { Err(SaveErrorType.FileWriteFailed, slot, ex.Message); }
        }

        public async Task<SaveData> LoadAsync(int slot)
        {
            if (!Valid(slot)) return null;
            try
            {
                if (!SaveFileHandler.SlotExists(slot))
                { Err(SaveErrorType.FileNotFound, slot, "Slot is empty."); return null; }

                var data = await SaveFileHandler.ReadAsync(slot);

                if (data.Version != SaveSystemConfig.CurrentSaveVersion)
                { Err(SaveErrorType.VersionMismatch, slot, $"Save is v{data.Version}, current is v{SaveSystemConfig.CurrentSaveVersion}."); return null; }

                var meta = await SaveFileHandler.ReadMetaAsync(slot);
                EventBus<GameLoadedEvent>.Raise(new GameLoadedEvent
                { SlotIndex = slot, SlotName = meta.SlotName, TimestampUtc = data.SavedAtUtc });

                return data;
            }
            catch (Exception ex) { Err(SaveErrorType.FileReadFailed, slot, ex.Message); return null; }
        }

        public void DeleteSlot(int slot)
        {
            if (!Valid(slot)) return;
            SaveFileHandler.Delete(slot);
            EventBus<SaveDeletedEvent>.Raise(new SaveDeletedEvent { SlotIndex = slot });
        }

        public async Task<SaveSlotMeta[]> GetAllSlotMetaAsync()
        {
            var r = new SaveSlotMeta[SaveSystemConfig.MaxSlots];
            for (int i = 0; i < r.Length; i++)
                r[i] = await SaveFileHandler.ReadMetaAsync(i);
            return r;
        }

        public bool SlotExists(int slot) => SaveFileHandler.SlotExists(slot);

        bool Valid(int s)
        {
            if (s >= 0 && s < SaveSystemConfig.MaxSlots) return true;
            Debug.LogError($"[SaveManager] Slot {s} out of range."); return false;
        }

        void Err(SaveErrorType t, int s, string m)
        {
            Debug.LogError($"[SaveManager] {t} | slot {s}: {m}");
            EventBus<SaveErrorEvent>.Raise(new SaveErrorEvent { ErrorType = t, SlotIndex = s, Message = m });
        }
    }
}