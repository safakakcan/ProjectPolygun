using UnityEngine;

namespace ProjectPolygun.Core.Events
{
    /// <summary>
    ///     Event fired when a player takes damage
    /// </summary>
    public class PlayerDamagedEvent : GameEventBase
    {
        public PlayerDamagedEvent(uint playerId, float damageAmount, float healthRemaining, GameObject damageSource = null)
        {
            PlayerId = playerId;
            DamageAmount = damageAmount;
            HealthRemaining = healthRemaining;
            DamageSource = damageSource;
        }

        public uint PlayerId { get; }
        public float DamageAmount { get; }
        public float HealthRemaining { get; }
        public GameObject DamageSource { get; }
    }

    /// <summary>
    ///     Event fired when a player dies
    /// </summary>
    public class PlayerDeathEvent : GameEventBase
    {
        public PlayerDeathEvent(uint playerId, uint killerId, string weaponUsed, Vector3 deathPosition)
        {
            PlayerId = playerId;
            KillerId = killerId;
            WeaponUsed = weaponUsed;
            DeathPosition = deathPosition;
        }

        public uint PlayerId { get; }
        public uint KillerId { get; }
        public string WeaponUsed { get; }
        public Vector3 DeathPosition { get; }
    }

    /// <summary>
    ///     Event fired when a player respawns
    /// </summary>
    public class PlayerRespawnEvent : GameEventBase
    {
        public PlayerRespawnEvent(uint playerId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            PlayerId = playerId;
            SpawnPosition = spawnPosition;
            SpawnRotation = spawnRotation;
        }

        public uint PlayerId { get; }
        public Vector3 SpawnPosition { get; }
        public Quaternion SpawnRotation { get; }
    }

    /// <summary>
    ///     Event fired when a weapon is fired
    /// </summary>
    public class WeaponFiredEvent : GameEventBase
    {
        public WeaponFiredEvent(uint playerId, string weaponName, Vector3 fireOrigin, Vector3 fireDirection, bool hit = false, uint hitPlayerId = 0)
        {
            PlayerId = playerId;
            WeaponName = weaponName;
            FireOrigin = fireOrigin;
            FireDirection = fireDirection;
            Hit = hit;
            HitPlayerId = hitPlayerId;
        }

        public uint PlayerId { get; }
        public string WeaponName { get; }
        public Vector3 FireOrigin { get; }
        public Vector3 FireDirection { get; }
        public bool Hit { get; }
        public uint HitPlayerId { get; }
    }

    /// <summary>
    ///     Event fired when a player joins the game
    /// </summary>
    public class PlayerJoinedEvent : GameEventBase
    {
        public PlayerJoinedEvent(uint playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }

        public uint PlayerId { get; }
        public string PlayerName { get; }
    }

    /// <summary>
    ///     Event fired when a player leaves the game
    /// </summary>
    public class PlayerLeftEvent : GameEventBase
    {
        public PlayerLeftEvent(uint playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }

        public uint PlayerId { get; }
        public string PlayerName { get; }
    }

    /// <summary>
    ///     Event fired when game state changes (starting, playing, ended)
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

        public GameStateChangedEvent(GameState newState, GameState previousState)
        {
            NewState = newState;
            PreviousState = previousState;
        }

        public GameState NewState { get; }
        public GameState PreviousState { get; }
    }
}