using ProjectPolygun.Core.Interfaces;
using ProjectPolygun.Core.Systems;
using UnityEngine;

namespace ProjectPolygun.Infrastructure
{
    /// <summary>
    ///     Main bootstrapper that initializes all core systems and dependency injection
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;

        [Header("Core Systems")] [SerializeField]
        private bool initializeOnAwake = true;

        private EventBus _eventBus;
        private ServiceContainer _serviceContainer;

        public static GameBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameBootstrapper>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameBootstrapper");
                        _instance = go.AddComponent<GameBootstrapper>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            // Ensure singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                if (initializeOnAwake) Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        ///     Initialize all core systems
        /// </summary>
        public void Initialize()
        {
            Debug.Log("GameBootstrapper: Initializing core systems...");

            // Initialize Service Container
            InitializeServiceContainer();

            // Initialize Event Bus
            InitializeEventBus();

            // Register core services
            RegisterCoreServices();

            Debug.Log("GameBootstrapper: Core systems initialized successfully");
        }

        private void InitializeServiceContainer()
        {
            if (_serviceContainer == null) _serviceContainer = gameObject.AddComponent<ServiceContainer>();
        }

        private void InitializeEventBus()
        {
            if (_eventBus == null) _eventBus = gameObject.AddComponent<EventBus>();
        }

        private void RegisterCoreServices()
        {
            // Register service container with itself for easy access
            _serviceContainer.RegisterInstance<IServiceContainer>(_serviceContainer);

            // Register event bus
            _serviceContainer.RegisterInstance<IEventBus>(_eventBus);

            Debug.Log("GameBootstrapper: Core services registered");
        }

        /// <summary>
        ///     Get the service container instance
        /// </summary>
        public static IServiceContainer GetServiceContainer()
        {
            return Instance._serviceContainer;
        }

        /// <summary>
        ///     Get the event bus instance
        /// </summary>
        public static IEventBus GetEventBus()
        {
            return Instance._eventBus;
        }
    }
}