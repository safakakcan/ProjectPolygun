using UnityEngine;
using ProjectPolygun.Core.Events;

namespace ProjectPolygun.Core.Events
{
    /// <summary>
    /// Event fired when a player takes damage
    /// </summary>
    public class PlayerDamagedEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public float DamageAmount { get; }
        public float HealthRemaining { get; }
        public GameObject DamageSource { get; }

        public PlayerDamagedEvent(uint playerId, float damageAmount, float healthRemaining, GameObject damageSource = null)
        {
            PlayerId = playerId;
            DamageAmount = damageAmount;
            HealthRemaining = healthRemaining;
            DamageSource = damageSource;
        }
    }

    /// <summary>
    /// Event fired when a player dies
    /// </summary>
    public class PlayerDeathEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public uint KillerId { get; }
        public string WeaponUsed { get; }
        public Vector3 DeathPosition { get; }

        public PlayerDeathEvent(uint playerId, uint killerId, string weaponUsed, Vector3 deathPosition)
        {
            PlayerId = playerId;
            KillerId = killerId;
            WeaponUsed = weaponUsed;
            DeathPosition = deathPosition;
        }
    }

    /// <summary>
    /// Event fired when a player respawns
    /// </summary>
    public class PlayerRespawnEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public Vector3 SpawnPosition { get; }
        public Quaternion SpawnRotation { get; }

        public PlayerRespawnEvent(uint playerId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            PlayerId = playerId;
            SpawnPosition = spawnPosition;
            SpawnRotation = spawnRotation;
        }
    }

    /// <summary>
    /// Event fired when a weapon is fired
    /// </summary>
    public class WeaponFiredEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public string WeaponName { get; }
        public Vector3 FireOrigin { get; }
        public Vector3 FireDirection { get; }
        public bool Hit { get; }
        public uint HitPlayerId { get; }

        public WeaponFiredEvent(uint playerId, string weaponName, Vector3 fireOrigin, Vector3 fireDirection, bool hit = false, uint hitPlayerId = 0)
        {
            PlayerId = playerId;
            WeaponName = weaponName;
            FireOrigin = fireOrigin;
            FireDirection = fireDirection;
            Hit = hit;
            HitPlayerId = hitPlayerId;
        }
    }

    /// <summary>
    /// Event fired when a player joins the game
    /// </summary>
    public class PlayerJoinedEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public string PlayerName { get; }

        public PlayerJoinedEvent(uint playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// Event fired when a player leaves the game
    /// </summary>
    public class PlayerLeftEvent : GameEventBase
    {
        public uint PlayerId { get; }
        public string PlayerName { get; }

        public PlayerLeftEvent(uint playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// Event fired when game state changes (starting, playing, ended)
    /// </summary>
    public class GameStateChangedEvent : GameEventBase
    {
        public enum GameState
        {
            Lobby,
            Starting,
            Playing,
            Ended
        }

        public GameState NewState { get; }
        public GameState PreviousState { get; }

        public GameStateChangedEvent(GameState newState, GameState previousState)
        {
            NewState = newState;
            PreviousState = previousState;
        }
    }
} 