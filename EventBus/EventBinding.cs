using System;

namespace EventBusSystem
{
    public interface IEventBinding<T> where T : IEvent
    {
        Action<T> OnEvent { get; set; }
        Action OnEventNoArgs { get; set; }
    }

    public class EventBinding<T> : IEventBinding<T> where T : IEvent
    {
        private Action<T> _onEvent = _ => { };
        private Action _onEventNoArgs = () => { };

        Action<T> IEventBinding<T>.OnEvent { get => _onEvent; set => _onEvent = value; }
        Action IEventBinding<T>.OnEventNoArgs { get => _onEventNoArgs; set => _onEventNoArgs = value; }

        public EventBinding(Action<T> onEvent) => _onEvent = onEvent;
        public EventBinding(Action onEventNoArgs) => _onEventNoArgs = onEventNoArgs;

        public EventBinding<T> Add(Action<T> onEvent) { _onEvent += onEvent; return this; }
        public EventBinding<T> Add(Action onEventNoArgs) { _onEventNoArgs += onEventNoArgs; return this; }
        public EventBinding<T> Remove(Action<T> onEvent) { _onEvent -= onEvent; return this; }
        public EventBinding<T> Remove(Action onEventNoArgs) { _onEventNoArgs -= onEventNoArgs; return this; }
    }
}