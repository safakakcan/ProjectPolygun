using UnityEngine;

namespace Mirror.Examples.PredictionBenchmark
{
    [AddComponentMenu("")]
    public class NetworkManagerStackedPrediction : NetworkManager
    {
        [Header("Spawns")] public int spawnAmount = 1000;

        public GameObject spawnPrefab;
        public float interleave = 1;

        // 500 objects need around 100 iterations to be stable
        [Tooltip("Stacked Cubes are only stable if solver iterations are high enough!\nDefault is 1, max is 255.")]
        public int solverIterations = 200;

        public int solverVelocityIterations = 1;

        public override void Awake()
        {
            base.Awake();

            // ensure vsync is disabled for the benchmark, otherwise results are capped
            QualitySettings.vSyncCount = 0;

            // stacked cubes are only stable if solver iteration is high enough!
            var before = Physics.defaultSolverIterations;
            Physics.defaultSolverIterations = solverIterations;
            Debug.Log($"Physics.defaultSolverIterations: {before} -> {Physics.defaultSolverIterations}");

            before = Physics.defaultSolverVelocityIterations;
            Physics.defaultSolverVelocityIterations = solverVelocityIterations;
            Debug.Log($"Physics.defaultSolverVelocityIterations: {before} -> {Physics.defaultSolverVelocityIterations}");
        }

        private void SpawnAll()
        {
            // calculate sqrt so we can spawn N * N = Amount
            var sqrt = Mathf.Sqrt(spawnAmount);

            // calculate spawn xz start positions
            // based on spawnAmount * distance
            var offset = -sqrt / 2 * interleave;

            // spawn exactly the amount, not one more.
            var spawned = 0;
            for (var spawnX = 0; spawnX < sqrt; ++spawnX)
            for (var spawnY = 0; spawnY < sqrt; ++spawnY)
                // spawn exactly the amount, not any more
                // (our sqrt method isn't 100% precise)
                if (spawned < spawnAmount)
                {
                    // it's important to have them at least 'Physics.defaultContactOffset' apart.
                    // otherwise the physics engine will detect collisions and make them unstable.
                    var spacing = interleave + Physics.defaultContactOffset;
                    var x = offset + spawnX * spacing;
                    var y = spawnY * spacing;

                    // instantiate & position
                    var go = Instantiate(spawnPrefab);
                    go.transform.position = new Vector3(x, y, 0);

                    // spawn
                    NetworkServer.Spawn(go);
                    ++spawned;
                }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnAll();

            // disable rendering on server to reduce noise in profiling.
            // keep enabled in host mode though.
            // if (mode == NetworkManagerMode.ServerOnly)
            //     Camera.main.enabled = false;
        }
    }
}