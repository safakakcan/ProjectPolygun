using ProjectPolygun.Examples;
using ProjectPolygun.Gameplay.Player;
using UnityEngine;

namespace ProjectPolygun.Infrastructure
{
    /// <summary>
    ///     Initializes a scene with all necessary core components for testing
    ///     Add this to an empty GameObject in your scene to get started quickly
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Scene Setup")] [SerializeField]
        private bool setupOnStart = true;

        [SerializeField] private bool addExampleComponent = true;
        [SerializeField] private bool addPlayerSystemExample = true;
        [SerializeField] private bool createPlayerManager = true;

        private void Start()
        {
            if (setupOnStart) SetupScene();
        }

        /// <summary>
        ///     Sets up the scene with core architecture components
        /// </summary>
        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("=== Scene Initializer: Setting up scene ===");

            // Ensure GameBootstrapper exists
            EnsureGameBootstrapper();

            // Create PlayerManager if requested
            if (createPlayerManager)
            {
                EnsurePlayerManager();
            }

            // Add example component if requested
            if (addExampleComponent)
            {
                AddExampleComponent();
            }

            // Add player system example if requested
            if (addPlayerSystemExample)
            {
                AddPlayerSystemExample();
            }

            Debug.Log("=== Scene setup complete! ===");
        }

        private void EnsureGameBootstrapper()
        {
            // Check if GameBootstrapper already exists
            var existingBootstrapper = FindObjectOfType<GameBootstrapper>();

            if (existingBootstrapper == null)
            {
                // Create new GameBootstrapper
                var bootstrapperGO = new GameObject("GameBootstrapper");
                bootstrapperGO.AddComponent<GameBootstrapper>();
                Debug.Log("Created GameBootstrapper");
            }
            else
            {
                Debug.Log("GameBootstrapper already exists");
            }
        }

        private void EnsurePlayerManager()
        {
            // Check if PlayerManager already exists
            var existingManager = FindObjectOfType<PlayerManager>();

            if (existingManager == null)
            {
                // Create new PlayerManager
                var managerGO = new GameObject("PlayerManager");
                managerGO.AddComponent<PlayerManager>();
                Debug.Log("Created PlayerManager");
            }
            else
            {
                Debug.Log("PlayerManager already exists");
            }
        }

        private void AddExampleComponent()
        {
            // Check if example already exists
            var existingExample = FindObjectOfType<ArchitectureExample>();

            if (existingExample == null)
            {
                // Add to this GameObject or create new one
                if (GetComponent<ArchitectureExample>() == null)
                {
                    gameObject.AddComponent<ArchitectureExample>();
                    Debug.Log("Added ArchitectureExample component");
                }
            }
            else
            {
                Debug.Log("ArchitectureExample already exists");
            }
        }

        private void AddPlayerSystemExample()
        {
            // Check if player system example already exists
            var existingPlayerExample = FindObjectOfType<PlayerSystemExample>();

            if (existingPlayerExample == null)
            {
                // Add to this GameObject
                if (GetComponent<PlayerSystemExample>() == null)
                {
                    gameObject.AddComponent<PlayerSystemExample>();
                    Debug.Log("Added PlayerSystemExample component");
                }
            }
            else
            {
                Debug.Log("PlayerSystemExample already exists");
            }
        }

        /// <summary>
        ///     Create a basic test environment
        /// </summary>
        [ContextMenu("Create Test Environment")]
        public void CreateTestEnvironment()
        {
            Debug.Log("Creating test environment...");

            // Create a simple floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(10, 1, 10);

            // Create some walls
            CreateWall("Wall_North", new Vector3(0, 2.5f, 50), new Vector3(100, 5, 1));
            CreateWall("Wall_South", new Vector3(0, 2.5f, -50), new Vector3(100, 5, 1));
            CreateWall("Wall_East", new Vector3(50, 2.5f, 0), new Vector3(1, 5, 100));
            CreateWall("Wall_West", new Vector3(-50, 2.5f, 0), new Vector3(1, 5, 100));

            // Create some spawn points
            CreateSpawnPoint("SpawnPoint_1", new Vector3(-20, 1, -20));
            CreateSpawnPoint("SpawnPoint_2", new Vector3(20, 1, -20));
            CreateSpawnPoint("SpawnPoint_3", new Vector3(-20, 1, 20));
            CreateSpawnPoint("SpawnPoint_4", new Vector3(20, 1, 20));

            // Create some obstacles for interesting gameplay
            CreateObstacle("Obstacle_1", new Vector3(0, 1, 0), new Vector3(2, 2, 2));
            CreateObstacle("Obstacle_2", new Vector3(10, 1, 10), new Vector3(1, 2, 4));
            CreateObstacle("Obstacle_3", new Vector3(-10, 1, -10), new Vector3(4, 2, 1));

            Debug.Log("Test environment created!");
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            
            // Make walls gray
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }
        }

        private void CreateSpawnPoint(string name, Vector3 position)
        {
            var spawnPoint = new GameObject(name);
            spawnPoint.transform.position = position;

            // Add a visual indicator
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.transform.SetParent(spawnPoint.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = new Vector3(2, 0.1f, 2);

            // Make it green
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = Color.green;
        }

        private void CreateObstacle(string name, Vector3 position, Vector3 scale)
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            
            // Make obstacles brown
            var renderer = obstacle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.6f, 0.3f, 0.1f); // Brown
            }
        }

        /// <summary>
        ///     Quick setup for FPS testing
        /// </summary>
        [ContextMenu("Quick FPS Setup")]
        public void QuickFPSSetup()
        {
            SetupScene();
            CreateTestEnvironment();
            
            // Find PlayerManager and spawn a local player
            var playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager != null)
            {
                playerManager.SpawnLocalPlayerTest();
                Debug.Log("Spawned local player for FPS testing!");
            }
            else
            {
                Debug.LogWarning("PlayerManager not found - cannot spawn player");
            }
        }
    }
}