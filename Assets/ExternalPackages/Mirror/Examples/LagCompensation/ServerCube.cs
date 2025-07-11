using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Mirror.Examples.LagCompensationDemo
{
    public class ServerCube : MonoBehaviour
    {
        [Header("Components")] public ClientCube client;

        [FormerlySerializedAs("collider")] public BoxCollider col;

        [Header("Movement")] public float distance = 10;

        public float speed = 3;

        [Header("Snapshot Interpolation")] [Tooltip("Send N snapshots per second. Multiples of frame rate make sense.")]
        public int sendRate = 30; // in Hz. easier to work with as int for EMA. easier to display '30' than '0.333333333'

        [Header("Lag Compensation")] public LagCompensationSettings lagCompensationSettings = new();

        public Color historyColor = Color.white;

        // store latest lag compensation result to show a visual indicator
        [Header("Debug")] public double resultDuration = 0.5;

        [Header("Latency Simulation")] [Tooltip("Latency in seconds")]
        public float latency = 0.05f; // 50 ms

        [Tooltip("Latency jitter, randomly added to latency.")] [Range(0, 1)]
        public float jitter = 0.05f;

        [Tooltip("Packet loss in %")] [Range(0, 1)]
        public float loss = 0.1f;

        [Tooltip("Scramble % of unreliable messages, just like over the real network. Mirror unreliable is unordered.")] [Range(0, 1)]
        public float scramble = 0.1f;

        // lag compensation history of <timestamp, capture>
        private readonly Queue<KeyValuePair<double, Capture2D>> history = new();

        // hold on to snapshots for a little while before delivering
        // <deliveryTime, snapshot>
        private readonly List<(double, Snapshot3D)> queue = new();

        // random
        // UnityEngine.Random.value is [0, 1] with both upper and lower bounds inclusive
        // but we need the upper bound to be exclusive, so using System.Random instead.
        // => NextDouble() is NEVER < 0 so loss=0 never drops!
        // => NextDouble() is ALWAYS < 1 so loss=1 always drops!
        private readonly System.Random random = new();

        private double lastCaptureTime;
        private float lastSendTime;
        private Capture2D resultAfter;
        private Capture2D resultBefore;
        private Capture2D resultInterpolated;

        private double resultTime;
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

            // capture lag compensation snapshots every interval.
            // NetworkTime.localTime because Unity 2019 doesn't have 'double' time yet.
            if (NetworkTime.localTime >= lastCaptureTime + lagCompensationSettings.captureInterval)
            {
                lastCaptureTime = NetworkTime.localTime;
                Capture();
            }
        }

        private void OnDrawGizmos()
        {
            // should we apply special colors to an active result?
            var showResult = NetworkTime.localTime <= resultTime + resultDuration;

            // draw interpoalted result first.
            // history meshcubes should write over it for better visibility.
            if (showResult)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(resultInterpolated.position, resultInterpolated.size);
            }

            // draw history
            Gizmos.color = historyColor;
            LagCompensation.DrawGizmos(history);

            // draw result samples after. useful to see the selection process.
            if (showResult)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(resultBefore.position, resultBefore.size);
                Gizmos.DrawWireCube(resultAfter.position, resultAfter.size);
            }
        }

        // latency simulation:
        // always a fixed value + some jitter.
        private float SimulateLatency()
        {
            return latency + Random.value * jitter;
        }

        // this is the average without randomness. for lag compensation math.
        // in a real game, use rtt instead.
        private float AverageLatency()
        {
            return latency + 0.5f * jitter;
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

        private void Capture()
        {
            // capture current state
            var capture = new Capture2D(NetworkTime.localTime, transform.position, col.size);

            // insert into history
            LagCompensation.Insert(history, lagCompensationSettings.historyLimit, NetworkTime.localTime, capture);
        }

        // client says: "I was clicked here, at this time."
        // server needs to rollback to validate.
        // timestamp is the client's snapshot interpolated timeline!
        public bool CmdClicked(Vector2 position)
        {
            // never trust the client: estimate client time instead.
            // https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking
            // the estimation is very good. the error is as low as ~6ms for the demo.
            double rtt = AverageLatency() * 2; // the function needs rtt, which is latency * 2
            var estimatedTime = LagCompensation.EstimateTime(NetworkTime.localTime, rtt, client.bufferTime);

            // compare estimated time with actual client time for debugging
            var error = Math.Abs(estimatedTime - client.localTimeline);
            Debug.Log($"CmdClicked: serverTime={NetworkTime.localTime:F3} clientTime={client.localTimeline:F3} estimatedTime={estimatedTime:F3} estimationError={error:F3} position={position}");

            // sample the history to get the nearest snapshots around 'timestamp'
            if (LagCompensation.Sample(history, estimatedTime, lagCompensationSettings.captureInterval, out resultBefore, out resultAfter, out var t))
            {
                // interpolate to get a decent estimation at exactly 'timestamp'
                resultInterpolated = Capture2D.Interpolate(resultBefore, resultAfter, t);
                resultTime = NetworkTime.localTime;

                // check if there really was a cube at that time and position
                var bounds = new Bounds(resultInterpolated.position, resultInterpolated.size);
                if (bounds.Contains(position)) return true;

                Debug.Log($"CmdClicked: interpolated={resultInterpolated} doesn't contain {position}");
            }
            else
            {
                Debug.Log($"CmdClicked: history doesn't contain {estimatedTime:F3}");
            }

            return false;
        }
    }
}