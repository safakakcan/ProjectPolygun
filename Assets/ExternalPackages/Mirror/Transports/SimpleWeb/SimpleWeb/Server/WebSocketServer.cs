using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Mirror.SimpleWeb
{
    public class WebSocketServer
    {
        private readonly BufferPool bufferPool;
        private readonly ConcurrentDictionary<int, Connection> connections = new();
        private readonly ServerHandshake handShake;
        private readonly int maxMessageSize;
        public readonly ConcurrentQueue<Message> receiveQueue = new();
        private readonly ServerSslHelper sslHelper;

        private readonly TcpConfig tcpConfig;

        private int _idCounter;
        private Thread acceptThread;

        private TcpListener listener;
        private bool serverStopped;

        public WebSocketServer(TcpConfig tcpConfig, int maxMessageSize, int handshakeMaxSize, SslConfig sslConfig, BufferPool bufferPool)
        {
            this.tcpConfig = tcpConfig;
            this.maxMessageSize = maxMessageSize;
            sslHelper = new ServerSslHelper(sslConfig);
            this.bufferPool = bufferPool;
            handShake = new ServerHandshake(this.bufferPool, handshakeMaxSize);
        }

        public void Listen(int port)
        {
            listener = TcpListener.Create(port);
            listener.Start();

            Log.Verbose("[SWT-WebSocketServer]: Server Started on {0}", port);

            acceptThread = new Thread(acceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        public void Stop()
        {
            serverStopped = true;

            // Interrupt then stop so that Exception is handled correctly
            acceptThread?.Interrupt();
            listener?.Stop();
            acceptThread = null;

            Log.Verbose("[SWT-WebSocketServer]: Server stopped...closing all connections.");

            // make copy so that foreach doesn't break if values are removed
            var connectionsCopy = connections.Values.ToArray();
            foreach (var conn in connectionsCopy)
                conn.Dispose();

            connections.Clear();
        }

        private void acceptLoop()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        var client = listener.AcceptTcpClient();
                        tcpConfig.ApplyTo(client);

                        // TODO keep track of connections before they are in connections dictionary
                        //      this might not be a problem as HandshakeAndReceiveLoop checks for stop
                        //      and returns/disposes before sending message to queue
                        var conn = new Connection(client, AfterConnectionDisposed);
                        Log.Verbose("[SWT-WebSocketServer]: A client connected from {0}", conn);

                        // handshake needs its own thread as it needs to wait for message from client
                        var receiveThread = new Thread(() => HandshakeAndReceiveLoop(conn));

                        conn.receiveThread = receiveThread;

                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }
                }
                catch (SocketException)
                {
                    // check for Interrupted/Abort
                    Utils.CheckForInterupt();
                    throw;
                }
            }
            catch (ThreadInterruptedException e)
            {
                Log.InfoException(e);
            }
            catch (ThreadAbortException)
            {
                Log.Error("[SWT-WebSocketServer]: Thread Abort Exception");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void HandshakeAndReceiveLoop(Connection conn)
        {
            try
            {
                var success = sslHelper.TryCreateStream(conn);
                if (!success)
                {
                    Log.Warn("[SWT-WebSocketServer]: Failed to create SSL Stream {0}", conn);
                    conn.Dispose();
                    return;
                }

                success = handShake.TryHandshake(conn);

                if (success)
                {
                    Log.Verbose("[SWT-WebSocketServer]: Sent Handshake {0}, false", conn);
                }
                else
                {
                    Log.Warn("[SWT-WebSocketServer]: Handshake Failed {0}", conn);
                    conn.Dispose();
                    return;
                }

                // check if Stop has been called since accepting this client
                if (serverStopped)
                {
                    Log.Warn("[SWT-WebSocketServer]: Server stopped after successful handshake");
                    return;
                }

                conn.connId = Interlocked.Increment(ref _idCounter);
                connections.TryAdd(conn.connId, conn);

                receiveQueue.Enqueue(new Message(conn.connId, EventType.Connected));

                var sendThread = new Thread(() =>
                {
                    var sendConfig = new SendLoop.Config(
                        conn,
                        Constants.HeaderSize + maxMessageSize,
                        false);

                    SendLoop.Loop(sendConfig);
                });

                conn.sendThread = sendThread;
                sendThread.IsBackground = true;
                sendThread.Name = $"SendThread {conn.connId}";
                sendThread.Start();

                var receiveConfig = new ReceiveLoop.Config(
                    conn,
                    maxMessageSize,
                    true,
                    receiveQueue,
                    bufferPool);

                ReceiveLoop.Loop(receiveConfig);
            }
            catch (ThreadInterruptedException e)
            {
                Log.InfoException(e);
            }
            catch (ThreadAbortException)
            {
                Log.Error("[SWT-WebSocketServer]: Thread Abort Exception");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally
            {
                // close here in case connect fails
                conn.Dispose();
            }
        }

        private void AfterConnectionDisposed(Connection conn)
        {
            if (conn.connId != Connection.IdNotSet)
            {
                receiveQueue.Enqueue(new Message(conn.connId, EventType.Disconnected));
                connections.TryRemove(conn.connId, out _);
            }
        }

        public void Send(int id, ArrayBuffer buffer)
        {
            if (connections.TryGetValue(id, out var conn))
            {
                conn.sendQueue.Enqueue(buffer);
                conn.sendPending.Set();
            }
            else
            {
                Log.Warn("[SWT-WebSocketServer]: Cannot send message to {0} because connection was not found in dictionary. Maybe it disconnected.", id);
            }
        }

        public bool CloseConnection(int id)
        {
            if (connections.TryGetValue(id, out var conn))
            {
                Log.Info($"[SWT-WebSocketServer]: Disconnecting connection {0}", id);
                conn.Dispose();
                return true;
            }

            Log.Warn("[SWT-WebSocketServer]: Failed to kick {0} because id not found.", id);
            return false;
        }

        public string GetClientAddress(int id)
        {
            if (!connections.TryGetValue(id, out var conn))
            {
                Log.Warn("[SWT-WebSocketServer]: Cannot get address of connection {0} because connection was not found in dictionary.", id);
                return null;
            }

            return conn.remoteAddress;
        }

        public Request GetClientRequest(int id)
        {
            if (!connections.TryGetValue(id, out var conn))
            {
                Log.Warn("[SWT-WebSocketServer]: Cannot get request of connection {0} because connection was not found in dictionary.", id);
                return null;
            }

            return conn.request;
        }
    }
}