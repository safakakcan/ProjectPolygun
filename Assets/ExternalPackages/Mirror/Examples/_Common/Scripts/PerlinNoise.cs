using UnityEditor;
using UnityEngine;

namespace Mirror.Examples.Common.Controllers.Player
{
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class PerlinNoise : MonoBehaviour
    {
        public float scale = 20f;
        public float heightMultiplier = .03f;
        public float offsetX = 5f;
        public float offsetY = 5f;

        [ContextMenu("Generate Terrain")]
        private void GenerateTerrain()
        {
            var terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                Debug.LogError("No Terrain component found on this GameObject.");
                return;
            }
#if UNITY_EDITOR
            Undo.RecordObject(terrain, "Generate Perlin Noise Terrain");
#endif
            terrain.terrainData = GenerateTerrainData(terrain.terrainData);
        }

        private TerrainData GenerateTerrainData(TerrainData terrainData)
        {
            var width = terrainData.heightmapResolution;
            var height = terrainData.heightmapResolution;

            var heights = new float[width, height];

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var xCoord = (float)x / width * scale + offsetX;
                var yCoord = (float)y / height * scale + offsetY;

                heights[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
            }

            terrainData.SetHeights(0, 0, heights);
            return terrainData;
        }
    }
}