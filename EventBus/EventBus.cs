using System;
using System.Collections.Generic;
using UnityEngine;

//basic script without this, everything don't work
namespace EventBusSystem
{
    public static class EventBus<T> where T : IEvent
    {
        static readonly HashSet<IEventBinding<T>> _bindings = new();
        static readonly Queue<T> _pendingEvents = new();
        static bool _isRaising;

        public static event Action<T> OnAnyEventRaised;
        public static bool LoggingEnabled = false;

        //you call this void to register an event
        public static void Register(EventBinding<T> binding)
        {
            _bindings.Add(binding);
            Log($"Registered | bindings: {_bindings.Count}");
        }
        //you call this void to deregister an event
        public static void Deregister(EventBinding<T> binding)
        {
            _bindings.Remove(binding);
            Log($"Deregistered | bindings: {_bindings.Count}");
        }

        public static void Raise(T @event)
        {
            if (_isRaising)
            {
                _pendingEvents.Enqueue(@event);
                return;
            }

            _isRaising = true;
            Log($"Raised | listeners: {_bindings.Count}");

            OnAnyEventRaised?.Invoke(@event);

            foreach (var binding in _bindings)
            {
                binding.OnEvent?.Invoke(@event);
                binding.OnEventNoArgs?.Invoke();
            }

            _isRaising = false;

            while (_pendingEvents.Count > 0)
                Raise(_pendingEvents.Dequeue());
        }

        public static void Raise() => Raise(Activator.CreateInstance<T>());

        public static void Clear()
        {
            _bindings.Clear();
            _pendingEvents.Clear();
            OnAnyEventRaised = null;
            Log("Cleared.");
        }

        public static int BindingCount => _bindings.Count;

        static void Log(string msg)
        {
#if UNITY_EDITOR
            if (LoggingEnabled) Debug.Log($"[EventBus<{typeof(T).Name}>] {msg}");
#endif
        }
    }
}