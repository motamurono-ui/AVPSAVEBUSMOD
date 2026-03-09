using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;


//the event bus auto cleanup, well... auto cleans the bus(es)

namespace EventBusSystem
{
    public class EventBusAutoCleanup : MonoBehaviour
    {
        static List<Type> _busTypes;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneUnloaded += _ => ClearAllBuses();
        }

        public static void ClearAllBuses()
        {
            _busTypes ??= FindAllBusTypes();
            foreach (var type in _busTypes)
                type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, null);
            Debug.Log($"[EventBusAutoCleanup] Cleared {_busTypes.Count} bus(es).");
        }

        static List<Type> FindAllBusTypes()
        {
            var result = new List<Type>();
            var openType = typeof(EventBus<>);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var t in asm.GetTypes())
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == openType)
                        result.Add(t);
            return result;
        }
    }
}