using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectPolygun.Core.Events;
using ProjectPolygun.Core.Interfaces;
using ProjectPolygun.Infrastructure;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     Manages all players in the game session
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float respawnDelay = 3f;
        
        private readonly Dictionary<uint, PlayerController> _players = new();
        private IEventBus _eventBus;
        private uint _nextPlayerId = 1;

        public IReadOnlyDictionary<uint, PlayerController> Players => _players;
        public int PlayerCount => _players.Count;
        public int MaxPlayers => 16; // As per our requirements

        private void Awake()
        {
            // Register as service
            var container = ServiceLocator.Container;
            container.RegisterInstance<PlayerManager>(this);
        }

        private void Start()
        {
            _eventBus = ServiceLocator.EventBus;
            SubscribeToEvents();
            
            // Setup default spawn points if none assigned
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                CreateDefaultSpawnPoints();
            }
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            _eventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            _eventBus.Subscribe<PlayerLeftEvent>(OnPlayerLeft);
        }

        /// <summary>
        ///     Spawn a new player
        /// </summary>
        public PlayerController SpawnPlayer(string playerName = "Player", bool isLocalPlayer = false)
        {
            if (PlayerCount >= MaxPlayers)
            {
                Debug.LogWarning("Cannot spawn player: Maximum player count reached");
                return null;
            }

            var spawnPoint = GetRandomSpawnPoint();
            var playerId = _nextPlayerId++;

            // Instantiate player
            GameObject playerGO;
            if (playerPrefab != null)
            {
                playerGO = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                // Create basic player if no prefab assigned
                playerGO = CreateBasicPlayer();
                playerGO.transform.position = spawnPoint.position;
                playerGO.transform.rotation = spawnPoint.rotation;
            }

            // Setup player controller
            var playerController = playerGO.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = playerGO.AddComponent<PlayerController>();
            }

            // Configure player
            playerController.SetPlayerId(playerId);
            playerController.SetPlayerName(playerName);
            playerController.SetIsLocalPlayer(isLocalPlayer);

            // Add to player list
            _players[playerId] = playerController;

            Debug.Log($"Spawned player {playerName} (ID: {playerId}) at {spawnPoint.position}");
            
            return playerController;
        }

        /// <summary>
        ///     Remove a player from the game
        /// </summary>
        public void RemovePlayer(uint playerId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                _players.Remove(playerId);
                
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
                
                Debug.Log($"Removed player {playerId}");
            }
        }

        /// <summary>
        ///     Get player by ID
        /// </summary>
        public PlayerController GetPlayer(uint playerId)
        {
            _players.TryGetValue(playerId, out var player);
            return player;
        }

        /// <summary>
        ///     Get all alive players
        /// </summary>
        public List<PlayerController> GetAlivePlayers()
        {
            return _players.Values.Where(p => p != null && p.IsAlive).ToList();
        }

        /// <summary>
        ///     Get all dead players
        /// </summary>
        public List<PlayerController> GetDeadPlayers()
        {
            return _players.Values.Where(p => p != null && !p.IsAlive).ToList();
        }

        /// <summary>
        ///     Respawn a player after delay
        /// </summary>
        public void RespawnPlayer(uint playerId)
        {
            StartCoroutine(RespawnPlayerCoroutine(playerId));
        }

        private System.Collections.IEnumerator RespawnPlayerCoroutine(uint playerId)
        {
            yield return new WaitForSeconds(respawnDelay);
            
            var player = GetPlayer(playerId);
            if (player != null)
            {
                var spawnPoint = GetRandomSpawnPoint();
                player.Respawn(spawnPoint.position, spawnPoint.rotation);
            }
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                // Return default position if no spawn points
                return transform;
            }

            // Try to find an unoccupied spawn point
            var availableSpawns = spawnPoints.Where(sp => !IsSpawnPointOccupied(sp)).ToArray();
            
            if (availableSpawns.Length == 0)
            {
                // All spawn points occupied, use random one
                availableSpawns = spawnPoints;
            }

            var randomIndex = Random.Range(0, availableSpawns.Length);
            return availableSpawns[randomIndex];
        }

        private bool IsSpawnPointOccupied(Transform spawnPoint)
        {
            // Check if any player is too close to this spawn point
            const float minDistance = 2f;
            
            foreach (var player in _players.Values)
            {
                if (player != null && Vector3.Distance(player.Position, spawnPoint.position) < minDistance)
                {
                    return true;
                }
            }
            
            return false;
        }

        private void CreateDefaultSpawnPoints()
        {
            // Create basic spawn points in a circle
            var spawnPointsList = new List<Transform>();
            var spawnCount = 8;
            var radius = 10f;

            for (var i = 0; i < spawnCount; i++)
            {
                var angle = i * (360f / spawnCount) * Mathf.Deg2Rad;
                var position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    1f,
                    Mathf.Sin(angle) * radius
                );

                var spawnPointGO = new GameObject($"SpawnPoint_{i}");
                spawnPointGO.transform.SetParent(transform);
                spawnPointGO.transform.position = position;
                spawnPointGO.transform.LookAt(Vector3.zero);

                spawnPointsList.Add(spawnPointGO.transform);
            }

            spawnPoints = spawnPointsList.ToArray();
            Debug.Log($"Created {spawnCount} default spawn points");
        }

        private GameObject CreateBasicPlayer()
        {
            var playerGO = new GameObject("Player");
            
            // Add basic components
            var controller = playerGO.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            
            // Add visual representation
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(playerGO.transform);
            capsule.transform.localPosition = Vector3.up;
            capsule.name = "PlayerMesh";
            
            // Remove capsule collider (CharacterController handles collision)
            Destroy(capsule.GetComponent<CapsuleCollider>());
            
            return playerGO;
        }

        #region Event Handlers

        private void OnPlayerDeath(PlayerDeathEvent eventData)
        {
            Debug.Log($"PlayerManager: Player {eventData.PlayerId} died, scheduling respawn");
            RespawnPlayer(eventData.PlayerId);
        }

        private void OnPlayerJoined(PlayerJoinedEvent eventData)
        {
            Debug.Log($"PlayerManager: Player {eventData.PlayerName} (ID: {eventData.PlayerId}) joined the game");
        }

        private void OnPlayerLeft(PlayerLeftEvent eventData)
        {
            Debug.Log($"PlayerManager: Player {eventData.PlayerName} (ID: {eventData.PlayerId}) left the game");
            // Note: Player is already removed from dictionary by PlayerController.OnDestroy
        }

        #endregion

        /// <summary>
        ///     Spawn local player for testing
        /// </summary>
        [ContextMenu("Spawn Local Player")]
        public void SpawnLocalPlayerTest()
        {
            SpawnPlayer("LocalPlayer", true);
        }

        /// <summary>
        ///     Spawn AI player for testing
        /// </summary>
        [ContextMenu("Spawn AI Player")]
        public void SpawnAIPlayerTest()
        {
            SpawnPlayer($"AIPlayer_{PlayerCount}", false);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
                _eventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
                _eventBus.Unsubscribe<PlayerLeftEvent>(OnPlayerLeft);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn points
            if (spawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var spawn in spawnPoints)
                {
                    if (spawn != null)
                    {
                        Gizmos.DrawWireCube(spawn.position, Vector3.one);
                        Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 2f);
                    }
                }
            }
        }
    }
} 