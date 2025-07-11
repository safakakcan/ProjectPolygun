using UnityEngine;
using Random = System.Random;

namespace Mirror.Examples.CCU
{
    [AddComponentMenu("")]
    public class CCUNetworkManager : NetworkManager
    {
        [Header("Spawns")] public int spawnAmount = 10_000;

        public float interleave = 1;
        public GameObject spawnPrefab;

        // player spawn positions should be spread across the world.
        // not all at one place.
        // but _some_ at the same place.
        // => deterministic random is ideal
        [Range(0, 1)] public float spawnPositionRatio = 0.01f;

        private readonly Random random = new(42);

        private void SpawnAll()
        {
            // clear previous player spawn positions in case we start twice
            foreach (var position in startPositions)
                Destroy(position.gameObject);

            startPositions.Clear();

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
                    // spawn & position
                    var go = Instantiate(spawnPrefab);
                    var x = offset + spawnX * interleave;
                    var z = offset + spawnZ * interleave;
                    var position = new Vector3(x, 0, z);
                    go.transform.position = position;

                    // spawn
                    NetworkServer.Spawn(go);
                    ++spawned;

                    // add random spawn position for players.
                    // don't have them all in the same place.
                    if (random.NextDouble() <= spawnPositionRatio)
                    {
                        var spawnGO = new GameObject("Spawn");
                        spawnGO.transform.position = position;
                        spawnGO.AddComponent<NetworkStartPosition>();
                    }
                }
        }

        // overwrite random spawn position selection:
        // - needs to be deterministic so every CCU test results in the same
        // - needs to be random so not only are the spawn positions spread out
        //   randomly, we also have a random amount of players per spawn position
        public override Transform GetStartPosition()
        {
            // first remove any dead transforms
            startPositions.RemoveAll(t => t == null);

            if (startPositions.Count == 0)
                return null;

            // pick a random one
            var index = random.Next(0, startPositions.Count); // DETERMINISTIC
            return startPositions[index];
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnAll();
        }
    }
}