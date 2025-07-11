using UnityEngine;

namespace ProjectPolygun.Core.Events
{
    /// <summary>
    ///     Event fired when a player starts moving
    /// </summary>
    public class PlayerMovementStartedEvent : GameEventBase
    {
        public PlayerMovementStartedEvent(uint playerId, Vector3 position, Vector3 direction)
        {
            PlayerId = playerId;
            Position = position;
            Direction = direction;
        }

        public uint PlayerId { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
    }

    /// <summary>
    ///     Event fired when a player stops moving
    /// </summary>
    public class PlayerMovementStoppedEvent : GameEventBase
    {
        public PlayerMovementStoppedEvent(uint playerId, Vector3 position)
        {
            PlayerId = playerId;
            Position = position;
        }

        public uint PlayerId { get; }
        public Vector3 Position { get; }
    }

    /// <summary>
    ///     Event fired when a player jumps
    /// </summary>
    public class PlayerJumpedEvent : GameEventBase
    {
        public PlayerJumpedEvent(uint playerId, Vector3 position)
        {
            PlayerId = playerId;
            Position = position;
        }

        public uint PlayerId { get; }
        public Vector3 Position { get; }
    }

    /// <summary>
    ///     Event fired when a player starts crouching
    /// </summary>
    public class PlayerCrouchStartedEvent : GameEventBase
    {
        public PlayerCrouchStartedEvent(uint playerId, Vector3 position)
        {
            PlayerId = playerId;
            Position = position;
        }

        public uint PlayerId { get; }
        public Vector3 Position { get; }
    }

    /// <summary>
    ///     Event fired when a player stops crouching
    /// </summary>
    public class PlayerCrouchEndedEvent : GameEventBase
    {
        public PlayerCrouchEndedEvent(uint playerId, Vector3 position)
        {
            PlayerId = playerId;
            Position = position;
        }

        public uint PlayerId { get; }
        public Vector3 Position { get; }
    }

    /// <summary>
    ///     Event fired when a player's health is restored
    /// </summary>
    public class PlayerHealedEvent : GameEventBase
    {
        public PlayerHealedEvent(uint playerId, float healAmount, float currentHealth, float maxHealth)
        {
            PlayerId = playerId;
            HealAmount = healAmount;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }

        public uint PlayerId { get; }
        public float HealAmount { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
    }

    /// <summary>
    ///     Event fired when a player becomes invulnerable
    /// </summary>
    public class PlayerInvulnerabilityStartedEvent : GameEventBase
    {
        public PlayerInvulnerabilityStartedEvent(uint playerId, float duration)
        {
            PlayerId = playerId;
            Duration = duration;
        }

        public uint PlayerId { get; }
        public float Duration { get; }
    }

    /// <summary>
    ///     Event fired when a player's invulnerability ends
    /// </summary>
    public class PlayerInvulnerabilityEndedEvent : GameEventBase
    {
        public PlayerInvulnerabilityEndedEvent(uint playerId)
        {
            PlayerId = playerId;
        }

        public uint PlayerId { get; }
    }

    /// <summary>
    ///     Event fired when a player's stats are updated (kills, deaths, score)
    /// </summary>
    public class PlayerStatsUpdatedEvent : GameEventBase
    {
        public PlayerStatsUpdatedEvent(uint playerId, int kills, int deaths, int score)
        {
            PlayerId = playerId;
            Kills = kills;
            Deaths = deaths;
            Score = score;
        }

        public uint PlayerId { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int Score { get; }
    }
} 