using UnityEngine;

namespace Mirror.Examples.Hex3D
{
    [AddComponentMenu("")]
    public class Hex3DNetworkManager : NetworkManager
    {
        [Header("Spawns")] public GameObject spawnPrefab;

        [Range(1, 8000)] public ushort spawnPrefabsCount = 1000;

        [Range(1, 10)] public byte spawnPrefabSpacing = 3;

        // Overrides the base singleton so we don't have to cast to this type everywhere.
        public new static Hex3DNetworkManager singleton => (Hex3DNetworkManager)NetworkManager.singleton;

        public override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            // Adjust spawnPrefabsCount to have an even cube root
            var cubeRoot = (ushort)Mathf.Pow(spawnPrefabsCount, 1f / 3f);
            spawnPrefabsCount = (ushort)Mathf.Pow(cubeRoot, 3f);
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterPrefab(spawnPrefab);
        }

        public override void OnStartServer()
        {
            // instantiate an empty GameObject
            var Spawns = new GameObject("Spawns");
            var SpawnsTransform = Spawns.transform;

            var spawned = 0;

            // Spawn prefabs in a cube grid centered around origin (0,0,0)
            var cubeRoot = Mathf.Pow(spawnPrefabsCount, 1f / 3f);
            var gridSize = Mathf.RoundToInt(cubeRoot);

            // Calculate the starting position to center the grid
            var startX = -(gridSize - 1) * spawnPrefabSpacing * 0.5f;
            var startY = -(gridSize - 1) * spawnPrefabSpacing * 0.5f;
            var startZ = -(gridSize - 1) * spawnPrefabSpacing * 0.5f;

            //Debug.Log($"Start Positions: X={startX}, Y={startY}, Z={startZ}, gridSize={gridSize}");

            for (var x = 0; x < gridSize; ++x)
            for (var y = 0; y < gridSize; ++y)
            for (var z = 0; z < gridSize; ++z)
                if (spawned < spawnPrefabsCount)
                {
                    var x1 = startX + x * spawnPrefabSpacing;
                    var y1 = startY + y * spawnPrefabSpacing;
                    var z1 = startZ + z * spawnPrefabSpacing;
                    var position = new Vector3(x1, y1, z1);

                    NetworkServer.Spawn(Instantiate(spawnPrefab, position, Quaternion.identity, SpawnsTransform));
                    ++spawned;
                }
        }
    }
}