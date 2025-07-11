// wraps around a transport and adds latency/loss/scramble simulation.
//
// reliable: latency
// unreliable: latency, loss, scramble (unreliable isn't ordered so we scramble)
//
// IMPORTANT: use Time.unscaledTime instead of Time.time.
//            some games might have Time.timeScale modified.
//            see also: https://github.com/vis2k/Mirror/issues/2907

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Mirror
{
    internal struct QueuedMessage
    {
        public int connectionId;
        public byte[] bytes;
        public double time;
        public int channelId;

        public QueuedMessage(int connectionId, byte[] bytes, double time, int channelId)
        {
            this.connectionId = connectionId;
            this.bytes = bytes;
            this.time = time;
            this.channelId = channelId;
        }
    }

    [HelpURL("https://mirror-networking.gitbook.io/docs/transports/latency-simulaton-transport")]
    [DisallowMultipleComponent]
    public class LatencySimulation : Transport, PortTransport
    {
        public Transport wrap;

        [Header("Common")]
        // latency always needs to be applied to both channels!
        // fixes a bug in prediction where predictedTime would have no latency, but [Command]s would have 100ms latency resulting in heavy, hard to debug jittering!
        // in real world, all UDP channels go over the same socket connection with the same latency.
        [Tooltip("Latency in milliseconds (1000 = 1 second). Always applied to both reliable and unreliable, otherwise unreliable NetworkTime may be behind reliable [SyncVars/Commands/Rpcs] or vice versa!")]
        [Range(0, 10000)]
        public float latency = 100;

        [Tooltip("Jitter latency via perlin(Time * jitterSpeed) * jitter")] [FormerlySerializedAs("latencySpikeMultiplier")] [Range(0, 1)]
        public float jitter = 0.02f;

        [Tooltip("Jitter latency via perlin(Time * jitterSpeed) * jitter")] [FormerlySerializedAs("latencySpikeSpeedMultiplier")]
        public float jitterSpeed = 1;

        [Header("Reliable Messages")]
        // note: packet loss over reliable manifests itself in latency.
        //       don't need (and can't add) a loss option here.
        // note: reliable is ordered by definition. no need to scramble.
        [Header("Unreliable Messages")]
        [Tooltip("Packet loss in %\n2% recommended for long term play testing, upto 5% for short bursts.\nAnything higher, or for a prolonged amount of time, suggests user has a connection fault.")]
        [Range(0, 100)]
        public float unreliableLoss = 2;

        [Tooltip("Scramble % of unreliable messages, just like over the real network. Mirror unreliable is unordered.")] [Range(0, 100)]
        public float unreliableScramble = 2;

        // message queues
        // list so we can insert randomly (scramble)
        private readonly List<QueuedMessage> clientToServer = new();

        // random
        // UnityEngine.Random.value is [0, 1] with both upper and lower bounds inclusive
        // but we need the upper bound to be exclusive, so using System.Random instead.
        // => NextDouble() is NEVER < 0 so loss=0 never drops!
        // => NextDouble() is ALWAYS < 1 so loss=1 always drops!
        private readonly Random random = new();
        private readonly List<QueuedMessage> serverToClient = new();

        public void Awake()
        {
            if (wrap == null)
                throw new Exception("LatencySimulationTransport requires an underlying transport to wrap around.");
        }

        // forward enable/disable to the wrapped transport
        private void OnEnable()
        {
            wrap.enabled = true;
        }

        private void OnDisable()
        {
            wrap.enabled = false;
        }

        // implement PortTransport in case the underlying Tranpsport is a PortTransport too.
        // otherwise gameplay code like 'if Transport is PortTransport' would completely break with Latency Simulation.
        public ushort Port
        {
            get
            {
                if (wrap is PortTransport port)
                    return port.Port;

                Debug.LogWarning($"LatencySimulation: attempted to get Port but {wrap} is not a PortTransport.");
                return 0;
            }
            set
            {
                if (wrap is PortTransport port)
                {
                    port.Port = value;
                    return;
                }

                Debug.LogWarning($"LatencySimulation: attempted to set Port but {wrap} is not a PortTransport.");
            }
        }

        // noise function can be replaced if needed
        protected virtual float Noise(float time)
        {
            return Mathf.PerlinNoise(time, time);
        }

        // helper function to simulate latency
        private float SimulateLatency(int channeldId)
        {
            // spike over perlin noise.
            // no spikes isn't realistic.
            // sin is too predictable / no realistic.
            // perlin is still deterministic and random enough.
#if !UNITY_2020_3_OR_NEWER
            float spike = Noise((float)NetworkTime.localTime * jitterSpeed) * jitter;
#else
            var spike = Noise((float)Time.unscaledTimeAsDouble * jitterSpeed) * jitter;
#endif

            // base latency
            switch (channeldId)
            {
                case Channels.Reliable:
                    return latency / 1000 + spike;
                case Channels.Unreliable:
                    return latency / 1000 + spike;
                default:
                    return 0;
            }
        }

        // helper function to simulate a send with latency/loss/scramble
        private void SimulateSend(
            int connectionId,
            ArraySegment<byte> segment,
            int channelId,
            float latency,
            List<QueuedMessage> messageQueue)
        {
            // segment is only valid after returning. copy it.
            // (allocates for now. it's only for testing anyway.)
            var bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);

            // simulate latency
#if !UNITY_2020_3_OR_NEWER
            double sendTime = NetworkTime.localTime + latency;
#else
            var sendTime = Time.unscaledTimeAsDouble + latency;
#endif

            // construct message
            var message = new QueuedMessage
            (
                connectionId,
                bytes,
                sendTime,
                channelId
            );

            // drop & scramble can only be simulated on Unreliable channel.
            if (channelId == Channels.Unreliable)
            {
                // simulate drop
                var drop = random.NextDouble() < unreliableLoss / 100;
                if (!drop)
                {
                    // simulate scramble (Random.Next is < max, so +1)
                    var scramble = random.NextDouble() < unreliableScramble / 100;
                    var last = messageQueue.Count;
                    var index = scramble ? random.Next(0, last + 1) : last;

                    // simulate latency
                    messageQueue.Insert(index, message);
                }
            }
            // any other channel may be relialbe / sequenced / ordered / etc.
            // in that case we only simulate latency (above)
            else
            {
                messageQueue.Add(message);
            }
        }

        public override bool Available()
        {
            return wrap.Available();
        }

        public override void ClientConnect(string address)
        {
            wrap.OnClientConnected = OnClientConnected;
            wrap.OnClientDataReceived = OnClientDataReceived;
            wrap.OnClientError = OnClientError;
            wrap.OnClientTransportException = OnClientTransportException;
            wrap.OnClientDisconnected = OnClientDisconnected;
            wrap.ClientConnect(address);
        }

        public override void ClientConnect(Uri uri)
        {
            wrap.OnClientConnected = OnClientConnected;
            wrap.OnClientDataReceived = OnClientDataReceived;
            wrap.OnClientError = OnClientError;
            wrap.OnClientTransportException = OnClientTransportException;
            wrap.OnClientDisconnected = OnClientDisconnected;
            wrap.ClientConnect(uri);
        }

        public override bool ClientConnected()
        {
            return wrap.ClientConnected();
        }

        public override void ClientDisconnect()
        {
            wrap.ClientDisconnect();
            clientToServer.Clear();
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            var latency = SimulateLatency(channelId);
            SimulateSend(0, segment, channelId, latency, clientToServer);
        }

        public override Uri ServerUri()
        {
            return wrap.ServerUri();
        }

        public override bool ServerActive()
        {
            return wrap.ServerActive();
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return wrap.ServerGetClientAddress(connectionId);
        }

        public override void ServerDisconnect(int connectionId)
        {
            wrap.ServerDisconnect(connectionId);
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            var latency = SimulateLatency(channelId);
            SimulateSend(connectionId, segment, channelId, latency, serverToClient);
        }

        public override void ServerStart()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            wrap.OnServerConnected = OnServerConnected;
#pragma warning restore CS0618 // Type or member is obsolete
            wrap.OnServerConnectedWithAddress = OnServerConnectedWithAddress;
            wrap.OnServerDataReceived = OnServerDataReceived;
            wrap.OnServerError = OnServerError;
            wrap.OnServerTransportException = OnServerTransportException;
            wrap.OnServerDisconnected = OnServerDisconnected;
            wrap.ServerStart();
        }

        public override void ServerStop()
        {
            wrap.ServerStop();
            serverToClient.Clear();
        }

        public override void ClientEarlyUpdate()
        {
            wrap.ClientEarlyUpdate();
        }

        public override void ServerEarlyUpdate()
        {
            wrap.ServerEarlyUpdate();
        }

        public override void ClientLateUpdate()
        {
            // flush messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < clientToServer.Count; ++i)
            {
                // message ready to be sent?
                var message = clientToServer[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ClientSend(new ArraySegment<byte>(message.bytes), message.channelId);
                    clientToServer.RemoveAt(i);
                    --i;
                }
            }

            // update wrapped transport too
            wrap.ClientLateUpdate();
        }

        public override void ServerLateUpdate()
        {
            // flush messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < serverToClient.Count; ++i)
            {
                // message ready to be sent?
                var message = serverToClient[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ServerSend(message.connectionId, new ArraySegment<byte>(message.bytes), message.channelId);
                    serverToClient.RemoveAt(i);
                    --i;
                }
            }

            // update wrapped transport too
            wrap.ServerLateUpdate();
        }

        public override int GetBatchThreshold(int channelId)
        {
            return wrap.GetBatchThreshold(channelId);
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            return wrap.GetMaxPacketSize(channelId);
        }

        public override void Shutdown()
        {
            wrap.Shutdown();
        }

        public override string ToString()
        {
            return $"{nameof(LatencySimulation)} {wrap}";
        }
    }
}