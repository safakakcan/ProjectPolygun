using System;

namespace ProjectPolygun.Core.Interfaces
{
    /// <summary>
    ///     Event bus for decoupled communication between systems
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        ///     Subscribe to an event type
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Handler method to call when event is published</param>
        void Subscribe<T>(Action<T> handler) where T : IGameEvent;

        /// <summary>
        ///     Unsubscribe from an event type
        /// </summary>
        /// <typeparam name="T">Event type to unsubscribe from</typeparam>
        /// <param name="handler">Handler method to remove</param>
        void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;

        /// <summary>
        ///     Publish an event to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type to publish</typeparam>
        /// <param name="eventData">Event data to publish</param>
        void Publish<T>(T eventData) where T : IGameEvent;

        /// <summary>
        ///     Clear all subscriptions
        /// </summary>
        void Clear();
    }
}