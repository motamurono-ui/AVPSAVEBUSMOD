using System;
using UnityEngine;

namespace EventBusSystem.SaveSystem
{
    public class AutoSaveController : MonoBehaviour
    {
        float _timer;
        bool _active = true;

        /// <summary>Assign this to return the game current live state.</summary>
        public Func<SaveData> GetCurrentSaveData;

        public void Enable() => _active = true;
        public void Disable() => _active = false;

        void Update()
        {
            if (!_active || GetCurrentSaveData == null) return;
            _timer += Time.deltaTime;
            if (_timer < SaveSystemConfig.AutoSaveIntervalSeconds) return;

            _timer = 0f;
            int slot = SaveSystemConfig.AutoSaveSlotIndex;
            EventBus<AutoSaveBegunEvent>.Raise(new AutoSaveBegunEvent { SlotIndex = slot });
            _ = SaveManager.Instance.SaveAsync(slot, GetCurrentSaveData(), "Auto Save");
        }
    }
}