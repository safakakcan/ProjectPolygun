using System;

namespace ProjectPolygun.Core.Interfaces
{
    /// <summary>
    /// Base interface for all game events. Events are used for decoupled communication between systems.
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// Timestamp when the event was created
        /// </summary>
        DateTime Timestamp { get; }
    }
} 