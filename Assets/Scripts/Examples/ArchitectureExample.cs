using UnityEngine;
using ProjectPolygun.Core.Interfaces;
using ProjectPolygun.Core.Events;
using ProjectPolygun.Infrastructure;

namespace ProjectPolygun.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the core architecture
    /// This shows proper SOLID principles and dependency injection usage
    /// </summary>
    public class ArchitectureExample : MonoBehaviour
    {
        [Header("Example Settings")]
        [SerializeField] private bool runExampleOnStart = true;
        
        private IEventBus _eventBus;
        private IServiceContainer _serviceContainer;

        private void Start()
        {
            if (runExampleOnStart)
            {
                InitializeExample();
                RunExample();
            }
        }

        /// <summary>
        /// Initialize the example by getting services from the container
        /// </summary>
        private void InitializeExample()
        {
            Debug.Log("=== Architecture Example: Initialization ===");
            
            // Method 1: Use Service Locator (easiest)
            _eventBus = ServiceLocator.EventBus;
            _serviceContainer = ServiceLocator.Container;
            
            // Method 2: Direct access to GameBootstrapper
            // _eventBus = GameBootstrapper.GetEventBus();
            // _serviceContainer = GameBootstrapper.GetServiceContainer();
            
            Debug.Log("Services obtained successfully!");
        }

        /// <summary>
        /// Run example demonstrating event system usage
        /// </summary>
        private void RunExample()
        {
            Debug.Log("=== Architecture Example: Event System Demo ===");
            
            // Subscribe to events
            _eventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            _eventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            
            // Simulate some game events
            SimulateGameEvents();
        }

        /// <summary>
        /// Simulate various game events to show the system working
        /// </summary>
        private void SimulateGameEvents()
        {
            Debug.Log("=== Simulating Game Events ===");
            
            // Simulate player joining
            var playerJoinEvent = new PlayerJoinedEvent(1, "TestPlayer");
            _eventBus.Publish(playerJoinEvent);
            
            // Simulate game state change
            var gameStateEvent = new GameStateChangedEvent(
                GameStateChangedEvent.GameState.Playing, 
                GameStateChangedEvent.GameState.Lobby
            );
            _eventBus.Publish(gameStateEvent);
            
            // Simulate player death
            var deathEvent = new PlayerDeathEvent(1, 2, "AssaultRifle", transform.position);
            _eventBus.Publish(deathEvent);
        }

        /// <summary>
        /// Event handler for player joined events
        /// </summary>
        private void OnPlayerJoined(PlayerJoinedEvent eventData)
        {
            Debug.Log($"Player {eventData.PlayerName} (ID: {eventData.PlayerId}) joined at {eventData.Timestamp}");
        }

        /// <summary>
        /// Event handler for player death events
        /// </summary>
        private void OnPlayerDeath(PlayerDeathEvent eventData)
        {
            Debug.Log($"Player {eventData.PlayerId} was killed by Player {eventData.KillerId} using {eventData.WeaponUsed}");
        }

        /// <summary>
        /// Event handler for game state changes
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent eventData)
        {
            Debug.Log($"Game state changed from {eventData.PreviousState} to {eventData.NewState}");
        }

        /// <summary>
        /// Example of how to create and use an object pool
        /// </summary>
        [ContextMenu("Demo Object Pool")]
        public void DemoObjectPool()
        {
            Debug.Log("=== Object Pool Demo ===");
            
            // Create a pool for GameObjects (bullets, shells, etc.)
            var bulletPool = new ObjectPool<GameObject>(
                createFunc: () => {
                    var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bullet.name = "PooledBullet";
                    return bullet;
                },
                onGet: (bullet) => {
                    bullet.SetActive(true);
                    Debug.Log($"Got bullet from pool: {bullet.name}");
                },
                onReturn: (bullet) => {
                    bullet.SetActive(false);
                    Debug.Log($"Returned bullet to pool: {bullet.name}");
                }
            );
            
            // Prewarm the pool
            bulletPool.Prewarm(5);
            Debug.Log($"Pool prewarmed with {bulletPool.Count} objects");
            
            // Use the pool
            var bullet1 = bulletPool.Get();
            var bullet2 = bulletPool.Get();
            
            Debug.Log($"Pool has {bulletPool.Count} objects remaining");
            
            // Return objects to pool
            bulletPool.Return(bullet1);
            bulletPool.Return(bullet2);
            
            Debug.Log($"Pool now has {bulletPool.Count} objects");
        }

        private void OnDestroy()
        {
            // Always unsubscribe from events to prevent memory leaks
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
                _eventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
                _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            }
        }
    }
} 