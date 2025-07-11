using UnityEngine;

namespace Mirror.Examples.Benchmark
{
    [AddComponentMenu("")]
    public class BenchmarkNetworkManager : NetworkManager
    {
        [Header("Spawns")] public GameObject spawnPrefab;

        public int spawnAmount = 5000;
        public float interleave = 1;

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
            for (var spawnZ = 0; spawnZ < sqrt; ++spawnZ)
                // spawn exactly the amount, not any more
                // (our sqrt method isn't 100% precise)
                if (spawned < spawnAmount)
                {
                    // instantiate & position
                    var go = Instantiate(spawnPrefab);
                    var x = offset + spawnX * interleave;
                    var z = offset + spawnZ * interleave;
                    go.transform.position = new Vector3(x, 0, z);

                    // spawn
                    NetworkServer.Spawn(go);
                    ++spawned;
                }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnAll();
        }
    }
}