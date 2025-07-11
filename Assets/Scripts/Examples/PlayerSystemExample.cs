using UnityEngine;
using ProjectPolygun.Core.Events;
using ProjectPolygun.Gameplay.Player;
using ProjectPolygun.Infrastructure;

namespace ProjectPolygun.Examples
{
    /// <summary>
    ///     Example demonstrating the player system functionality
    /// </summary>
    public class PlayerSystemExample : MonoBehaviour
    {
        [Header("Example Settings")]
        [SerializeField] private bool runExampleOnStart = true;
        [SerializeField] private bool spawnLocalPlayer = true;
        [SerializeField] private int aiPlayersToSpawn = 2;
        
        [Header("Testing")]
        [SerializeField] private float damageToApply = 25f;
        [SerializeField] private float healToApply = 20f;
        
        private PlayerManager _playerManager;

        private void Start()
        {
            if (runExampleOnStart)
            {
                InitializeExample();
                RunExample();
            }
        }

        private void InitializeExample()
        {
            Debug.Log("=== Player System Example: Initialization ===");
            
            // Get or create PlayerManager
            _playerManager = FindObjectOfType<PlayerManager>();
            if (_playerManager == null)
            {
                var managerGO = new GameObject("PlayerManager");
                _playerManager = managerGO.AddComponent<PlayerManager>();
            }
            
            // Subscribe to player events
            var eventBus = ServiceLocator.EventBus;
            eventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            eventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            eventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            eventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawned);
            eventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            
            Debug.Log("Player system example initialized!");
        }

        private void RunExample()
        {
            Debug.Log("=== Player System Example: Running Demo ===");
            
            // Spawn local player if requested
            if (spawnLocalPlayer)
            {
                var localPlayer = _playerManager.SpawnPlayer("LocalTestPlayer", true);
                Debug.Log($"Spawned local player: {localPlayer.PlayerName}");
            }
            
            // Spawn AI players
            for (var i = 0; i < aiPlayersToSpawn; i++)
            {
                var aiPlayer = _playerManager.SpawnPlayer($"AIPlayer_{i + 1}", false);
                Debug.Log($"Spawned AI player: {aiPlayer.PlayerName}");
            }
            
            Debug.Log($"Total players in game: {_playerManager.PlayerCount}");
        }

        /// <summary>
        ///     Test damage system on all players
        /// </summary>
        [ContextMenu("Test Damage All Players")]
        public void TestDamageAllPlayers()
        {
            foreach (var player in _playerManager.GetAlivePlayers())
            {
                var healthSystem = player.HealthSystem;
                if (healthSystem != null)
                {
                    healthSystem.TakeDamage(damageToApply);
                    Debug.Log($"Applied {damageToApply} damage to {player.PlayerName}. Health: {healthSystem.CurrentHealth}");
                }
            }
        }

        /// <summary>
        ///     Test healing system on all players
        /// </summary>
        [ContextMenu("Test Heal All Players")]
        public void TestHealAllPlayers()
        {
            foreach (var player in _playerManager.Players.Values)
            {
                if (player == null) continue;
                
                var healthSystem = player.HealthSystem;
                if (healthSystem != null)
                {
                    var healedAmount = healthSystem.Heal(healToApply);
                    Debug.Log($"Healed {player.PlayerName} for {healedAmount}. Health: {healthSystem.CurrentHealth}");
                    
                    // Publish heal event
                    var healEvent = new PlayerHealedEvent(
                        player.PlayerId, 
                        healedAmount, 
                        healthSystem.CurrentHealth, 
                        healthSystem.MaxHealth
                    );
                    ServiceLocator.EventBus.Publish(healEvent);
                }
            }
        }

        /// <summary>
        ///     Kill a random player for testing
        /// </summary>
        [ContextMenu("Kill Random Player")]
        public void KillRandomPlayer()
        {
            var alivePlayers = _playerManager.GetAlivePlayers();
            if (alivePlayers.Count == 0)
            {
                Debug.Log("No alive players to kill!");
                return;
            }
            
            var randomPlayer = alivePlayers[Random.Range(0, alivePlayers.Count)];
            var healthSystem = randomPlayer.HealthSystem;
            
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(healthSystem.CurrentHealth);
                Debug.Log($"Killed player: {randomPlayer.PlayerName}");
            }
        }

        /// <summary>
        ///     Respawn all dead players immediately
        /// </summary>
        [ContextMenu("Respawn All Dead Players")]
        public void RespawnAllDeadPlayers()
        {
            var deadPlayers = _playerManager.GetDeadPlayers();
            Debug.Log($"Respawning {deadPlayers.Count} dead players");
            
            foreach (var player in deadPlayers)
            {
                _playerManager.RespawnPlayer(player.PlayerId);
            }
        }

        /// <summary>
        ///     Toggle invulnerability on local player
        /// </summary>
        [ContextMenu("Toggle Local Player Invulnerability")]
        public void ToggleLocalPlayerInvulnerability()
        {
            var localPlayer = _playerManager.Players.Values
                .FirstOrDefault(p => p != null && p.IsLocalPlayer);
                
            if (localPlayer?.HealthSystem is HealthSystem healthSystem)
            {
                var newState = !healthSystem.IsInvulnerable;
                healthSystem.SetInvulnerable(newState);
                
                Debug.Log($"Local player invulnerability: {newState}");
                
                // Publish invulnerability event
                if (newState)
                {
                    var invulnStartEvent = new PlayerInvulnerabilityStartedEvent(localPlayer.PlayerId, -1f);
                    ServiceLocator.EventBus.Publish(invulnStartEvent);
                }
                else
                {
                    var invulnEndEvent = new PlayerInvulnerabilityEndedEvent(localPlayer.PlayerId);
                    ServiceLocator.EventBus.Publish(invulnEndEvent);
                }
            }
            else
            {
                Debug.Log("No local player found or health system unavailable");
            }
        }

        /// <summary>
        ///     Display current player stats
        /// </summary>
        [ContextMenu("Show Player Stats")]
        public void ShowPlayerStats()
        {
            Debug.Log("=== Current Player Stats ===");
            Debug.Log($"Total Players: {_playerManager.PlayerCount}");
            Debug.Log($"Alive Players: {_playerManager.GetAlivePlayers().Count}");
            Debug.Log($"Dead Players: {_playerManager.GetDeadPlayers().Count}");
            
            foreach (var player in _playerManager.Players.Values)
            {
                if (player == null) continue;
                
                var healthSystem = player.HealthSystem;
                var status = player.IsAlive ? "ALIVE" : "DEAD";
                var health = healthSystem?.CurrentHealth ?? 0f;
                var maxHealth = healthSystem?.MaxHealth ?? 100f;
                var isLocal = player.IsLocalPlayer ? " (LOCAL)" : "";
                
                Debug.Log($"Player {player.PlayerName}{isLocal}: {status} - Health: {health:F1}/{maxHealth:F1}");
            }
        }

        #region Event Handlers

        private void OnPlayerJoined(PlayerJoinedEvent eventData)
        {
            Debug.Log($"[EVENT] Player joined: {eventData.PlayerName} (ID: {eventData.PlayerId})");
        }

        private void OnPlayerDeath(PlayerDeathEvent eventData)
        {
            var killerName = eventData.KillerId > 0 ? $"Player {eventData.KillerId}" : "Unknown";
            Debug.Log($"[EVENT] Player {eventData.PlayerId} was killed by {killerName} using {eventData.WeaponUsed}");
        }

        private void OnPlayerDamaged(PlayerDamagedEvent eventData)
        {
            Debug.Log($"[EVENT] Player {eventData.PlayerId} took {eventData.DamageAmount:F1} damage. Health remaining: {eventData.HealthRemaining:F1}");
        }

        private void OnPlayerRespawned(PlayerRespawnEvent eventData)
        {
            Debug.Log($"[EVENT] Player {eventData.PlayerId} respawned at {eventData.SpawnPosition}");
        }

        private void OnPlayerHealed(PlayerHealedEvent eventData)
        {
            Debug.Log($"[EVENT] Player {eventData.PlayerId} healed for {eventData.HealAmount:F1}. Health: {eventData.CurrentHealth:F1}/{eventData.MaxHealth:F1}");
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from events
            var eventBus = ServiceLocator.EventBus;
            if (eventBus != null)
            {
                eventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
                eventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
                eventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
                eventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawned);
                eventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            }
        }
    }
}

// Extension method to make LINQ work
namespace System.Linq
{
    public static class Extensions
    {
        public static T FirstOrDefault<T>(this System.Collections.Generic.IEnumerable<T> source, System.Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    return item;
            }
            return default(T);
        }
    }
} 