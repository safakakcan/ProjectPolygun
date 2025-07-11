using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Mirror.Examples.SnapshotInterpolationDemo
{
    public class ServerCube : MonoBehaviour
    {
        [Header("Components")] public ClientCube client;

        [Header("Movement")] public float distance = 10;

        public float speed = 3;

        [Header("Snapshot Interpolation")] [Tooltip("Send N snapshots per second. Multiples of frame rate make sense.")]
        public int sendRate = 30; // in Hz. easier to work with as int for EMA. easier to display '30' than '0.333333333'

        [Header("Latency Simulation")] [Tooltip("Latency in seconds")]
        public float latency = 0.05f; // 50 ms

        [Tooltip("Latency jitter, randomly added to latency.")] [Range(0, 1)]
        public float jitter = 0.05f;

        [Tooltip("Packet loss in %")] [Range(0, 1)]
        public float loss = 0.1f;

        [Tooltip("Scramble % of unreliable messages, just like over the real network. Mirror unreliable is unordered.")] [Range(0, 1)]
        public float scramble = 0.1f;

        // hold on to snapshots for a little while before delivering
        // <deliveryTime, snapshot>
        private readonly List<(double, Snapshot3D)> queue = new();

        // random
        // UnityEngine.Random.value is [0, 1] with both upper and lower bounds inclusive
        // but we need the upper bound to be exclusive, so using System.Random instead.
        // => NextDouble() is NEVER < 0 so loss=0 never drops!
        // => NextDouble() is ALWAYS < 1 so loss=1 always drops!
        private readonly Random random = new();
        private float lastSendTime;
        private Vector3 start;
        public float sendInterval => 1f / sendRate;

        private void Start()
        {
            start = transform.position;
        }

        private void Update()
        {
            // move on XY plane
            var x = Mathf.PingPong(Time.time * speed, distance);
            transform.position = new Vector3(start.x + x, start.y, start.z);

            // broadcast snapshots every interval
            if (Time.time >= lastSendTime + sendInterval)
            {
                Send(transform.position);
                lastSendTime = Time.time;
            }

            Flush();
        }

        // latency simulation:
        // always a fixed value + some jitter.
        private float SimulateLatency()
        {
            return latency + UnityEngine.Random.value * jitter;
        }

        private void Send(Vector3 position)
        {
            // create snapshot
            // Unity 2019 doesn't have Time.timeAsDouble yet
            var snap = new Snapshot3D(NetworkTime.localTime, 0, position);

            // simulate packet loss
            var drop = random.NextDouble() < loss;
            if (!drop)
            {
                // simulate scramble (Random.Next is < max, so +1)
                var doScramble = random.NextDouble() < scramble;
                var last = queue.Count;
                var index = doScramble ? random.Next(0, last + 1) : last;

                // simulate latency
                var simulatedLatency = SimulateLatency();
                // Unity 2019 doesn't have Time.timeAsDouble yet
                var deliveryTime = NetworkTime.localTime + simulatedLatency;
                queue.Insert(index, (deliveryTime, snap));
            }
        }

        private void Flush()
        {
            // flush ready snapshots to client
            for (var i = 0; i < queue.Count; ++i)
            {
                var (deliveryTime, snap) = queue[i];

                // Unity 2019 doesn't have Time.timeAsDouble yet
                if (NetworkTime.localTime >= deliveryTime)
                {
                    client.OnMessage(snap);
                    queue.RemoveAt(i);
                    --i;
                }
            }
        }
    }
}