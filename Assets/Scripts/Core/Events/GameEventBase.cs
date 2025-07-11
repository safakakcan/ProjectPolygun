using System;
using ProjectPolygun.Core.Interfaces;

namespace ProjectPolygun.Core.Events
{
    /// <summary>
    ///     Base implementation for game events
    /// </summary>
    public abstract class GameEventBase : IGameEvent
    {
        protected GameEventBase()
        {
            Timestamp = DateTime.UtcNow;
        }

        public DateTime Timestamp { get; }
    }
}