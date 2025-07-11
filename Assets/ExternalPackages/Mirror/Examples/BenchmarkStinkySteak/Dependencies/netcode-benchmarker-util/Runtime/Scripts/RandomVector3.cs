using UnityEngine;

namespace StinkySteak.NetcodeBenchmark
{
    public static class RandomVector3
    {
        public static Vector3 Get(float max)
        {
            var x = Random.Range(-max, max);
            var y = Random.Range(-max, max);
            var z = Random.Range(-max, max);

            return new Vector3(x, y, z);
        }
    }
}