using UnityEngine;

namespace Mirror.Examples.Hex2D
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(HexSpatialHash2DInterestManagement))]
    public class Hex2DNetworkManager : NetworkManager
    {
        [Header("Spawns")] public GameObject spawnPrefab;

        [Range(1, 3000)] [Tooltip("Number of prefabs to spawn in a flat 2D grid across the scene.")]
        public ushort spawnPrefabsCount = 1000;

        [Range(1, 10)] [Tooltip("Spacing between grid points in meters.")]
        public byte spawnPrefabSpacing = 3;

        [Header("Diagnostics")] [ReadOnly] [SerializeField]
        private HexSpatialHash2DInterestManagement hexSpatialHash2DInterestManagement;

        // Overrides the base singleton so we don’t have to cast to this type everywhere.
        public new static Hex2DNetworkManager singleton => (Hex2DNetworkManager)NetworkManager.singleton;

        public override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            if (hexSpatialHash2DInterestManagement == null)
                hexSpatialHash2DInterestManagement = GetComponent<HexSpatialHash2DInterestManagement>();
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterPrefab(spawnPrefab);
        }

        public override void OnStartServer()
        {
            // Instantiate an empty GameObject to parent spawns
            var spawns = new GameObject("Spawns");
            var spawnsTransform = spawns.transform;

            var spawned = 0;

            // Spawn prefabs in a 2D grid centered around origin (0,0,0)
            var gridSize = (int)Mathf.Sqrt(spawnPrefabsCount); // Square grid size based on count

            // Calculate the starting position to center the grid at (0,0,0)
            var halfGrid = (gridSize - 1) * spawnPrefabSpacing * 0.5f;
            var startX = -halfGrid;
            var startZorY = -halfGrid; // Z for XZ, Y for XY

            //Debug.Log($"Start Positions: X={startX}, Z/Y={startZorY}, gridSize={gridSize}");

            // Use a 2D loop for a flat grid
            for (var x = 0; x < gridSize && spawned < spawnPrefabsCount; ++x)
            for (var zOrY = 0; zOrY < gridSize && spawned < spawnPrefabsCount; ++zOrY)
            {
                var position = Vector3.zero;

                if (hexSpatialHash2DInterestManagement.checkMethod == HexSpatialHash2DInterestManagement.CheckMethod.XZ_FOR_3D)
                {
                    var xPos = startX + x * spawnPrefabSpacing;
                    var zPos = startZorY + zOrY * spawnPrefabSpacing;
                    position = new Vector3(xPos, 0.5f, zPos);
                }
                else // XY_FOR_2D
                {
                    var xPos = startX + x * spawnPrefabSpacing;
                    var yPos = startZorY + zOrY * spawnPrefabSpacing;
                    position = new Vector3(xPos, yPos, -0.5f);
                }

                var instance = Instantiate(spawnPrefab, position, Quaternion.identity, spawnsTransform);
                NetworkServer.Spawn(instance);
                ++spawned;
            }

            //Debug.Log($"Spawned {spawned} objects in a {gridSize}x{gridSize} 2D grid.");
        }
    }
}