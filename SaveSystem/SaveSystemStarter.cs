using UnityEngine;

namespace EventBusSystem.SaveSystem
{
    /// <summary>Add this to your persistent/bootstrap scene. That's it.</summary>
    public class SaveSystemStarter : MonoBehaviour
    {
        [SerializeField] bool enableAutoSave = true;

        void Awake()
        {
            var go = new GameObject("[SaveSystem]");
            DontDestroyOnLoad(go);
            go.AddComponent<SaveManager>();
            if (enableAutoSave)
                go.AddComponent<AutoSaveController>();
        }
    }
}