using UnityEngine;
using ProjectPolygun.Core.Events;
using ProjectPolygun.Infrastructure;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     Main player controller that composes all player systems using SOLID principles
    /// </summary>
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [Header("Player Settings")]
        [SerializeField] private uint playerId;
        [SerializeField] private string playerName = "Player";
        [SerializeField] private bool isLocalPlayerOverride = false;
        
        [Header("Components")]
        [SerializeField] private Transform cameraTransform;
        
        // Composed systems - following composition over inheritance
        private IHealthSystem _healthSystem;
        private PlayerInputHandler _inputHandler;
        private FPSMovementSystem _movementSystem;
        
        private bool _isInitialized;

        // IPlayerController implementation
        public uint PlayerId => playerId;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public bool IsLocalPlayer => isLocalPlayerOverride; // Will be overridden by networking
        public bool IsAlive => _healthSystem?.IsAlive ?? true;
        
        // Additional properties
        public string PlayerName => playerName;
        public IHealthSystem HealthSystem => _healthSystem;
        public FPSMovementSystem MovementSystem => _movementSystem;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_isInitialized || !IsAlive) return;
            
            HandleInput(Time.deltaTime);
            UpdatePlayer(Time.deltaTime);
        }

        private void InitializeComponents()
        {
            // Get or add health system
            _healthSystem = GetComponent<IHealthSystem>();
            if (_healthSystem == null)
            {
                var healthComponent = gameObject.AddComponent<HealthSystem>();
                _healthSystem = healthComponent;
            }

            // Get or add input handler (only for local player)
            _inputHandler = GetComponent<PlayerInputHandler>();
            if (_inputHandler == null && IsLocalPlayer)
            {
                _inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            }

            // Get or add movement system
            _movementSystem = GetComponent<FPSMovementSystem>();
            if (_movementSystem == null)
            {
                _movementSystem = gameObject.AddComponent<FPSMovementSystem>();
            }

            // Ensure CharacterController exists
            if (GetComponent<CharacterController>() == null)
            {
                gameObject.AddComponent<CharacterController>();
            }

            // Setup camera for local player
            if (IsLocalPlayer && cameraTransform == null)
            {
                SetupCamera();
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize health system
            _healthSystem?.Initialize(100f);

            // Subscribe to health events
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath += OnPlayerDeath;
                _healthSystem.OnHealthChanged += OnHealthChanged;
            }

            // Setup input for local player only
            if (IsLocalPlayer && _inputHandler != null)
            {
                _inputHandler.SetInputEnabled(true);
                
                // Setup camera controls
                if (cameraTransform != null && _movementSystem != null)
                {
                    // The movement system will handle camera rotation
                }
            }
            else if (_inputHandler != null)
            {
                _inputHandler.SetInputEnabled(false);
            }

            // Publish player joined event
            var joinEvent = new PlayerJoinedEvent(PlayerId, PlayerName);
            ServiceLocator.EventBus.Publish(joinEvent);

            _isInitialized = true;
            
            Debug.Log($"Player {PlayerName} (ID: {PlayerId}) initialized. IsLocal: {IsLocalPlayer}");
        }

        public void HandleInput(float deltaTime)
        {
            // Input is handled by PlayerInputHandler and used by MovementSystem
            // This method is here for interface compliance and future networking
            
            if (!IsLocalPlayer || _inputHandler == null) return;
            
            // Handle weapon input here when weapons are implemented
            if (_inputHandler.FirePressed)
            {
                // TODO: Fire weapon
            }
            
            if (_inputHandler.ReloadPressed)
            {
                // TODO: Reload weapon
            }
        }

        public void UpdatePlayer(float deltaTime)
        {
            // Additional player logic can go here
            // Movement is handled by FPSMovementSystem
            // Health is handled by HealthSystem
            
            // Update any player-specific state here
        }

        public void Respawn(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // Reset health
            _healthSystem?.Reset();
            
            // Teleport to spawn position
            _movementSystem?.TeleportTo(spawnPosition, spawnRotation);
            
            // Enable systems
            EnablePlayer();
            
            // Publish respawn event
            var respawnEvent = new PlayerRespawnEvent(PlayerId, spawnPosition, spawnRotation);
            ServiceLocator.EventBus.Publish(respawnEvent);
            
            Debug.Log($"Player {PlayerName} respawned at {spawnPosition}");
        }

        /// <summary>
        ///     Set player ID (used by networking)
        /// </summary>
        public void SetPlayerId(uint id)
        {
            playerId = id;
        }

        /// <summary>
        ///     Set player name
        /// </summary>
        public void SetPlayerName(string name)
        {
            playerName = name;
        }

        /// <summary>
        ///     Set whether this is the local player
        /// </summary>
        public void SetIsLocalPlayer(bool isLocal)
        {
            isLocalPlayerOverride = isLocal;
            
            // Update input handler
            if (_inputHandler != null)
            {
                _inputHandler.SetInputEnabled(isLocal);
            }
            
            // Update camera
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.SetActive(isLocal);
            }
        }

        /// <summary>
        ///     Enable player systems
        /// </summary>
        public void EnablePlayer()
        {
            if (_inputHandler != null)
                _inputHandler.SetInputEnabled(IsLocalPlayer);
                
            if (_movementSystem != null)
                _movementSystem.SetMovementEnabled(true);
                
            enabled = true;
        }

        /// <summary>
        ///     Disable player systems
        /// </summary>
        public void DisablePlayer()
        {
            if (_inputHandler != null)
                _inputHandler.SetInputEnabled(false);
                
            if (_movementSystem != null)
                _movementSystem.SetMovementEnabled(false);
                
            enabled = false;
        }

        private void SetupCamera()
        {
            // Create camera if it doesn't exist
            var cameraGO = new GameObject("PlayerCamera");
            cameraGO.transform.SetParent(transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.5f, 0);
            cameraGO.transform.localRotation = Quaternion.identity;
            
            var camera = cameraGO.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            
            // Add audio listener
            cameraGO.AddComponent<AudioListener>();
            
            cameraTransform = cameraGO.transform;
        }

        private void OnPlayerDeath()
        {
            Debug.Log($"Player {PlayerName} died!");
            
            // Disable player systems
            DisablePlayer();
            
            // TODO: Trigger death effects, ragdoll, etc.
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            // TODO: Update UI health bar
            // This can be handled by UI system listening to health events
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath -= OnPlayerDeath;
                _healthSystem.OnHealthChanged -= OnHealthChanged;
            }
            
            // Publish player left event
            var leftEvent = new PlayerLeftEvent(PlayerId, PlayerName);
            ServiceLocator.EventBus.Publish(leftEvent);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw player info in scene view
            Gizmos.color = IsLocalPlayer ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
} 