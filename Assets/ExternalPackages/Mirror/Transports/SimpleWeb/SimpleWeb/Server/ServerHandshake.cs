using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mirror.SimpleWeb
{
    /// <summary>
    ///     Handles Handshakes from new clients on the server
    ///     <para>The server handshake has buffers to reduce allocations when clients connect</para>
    /// </summary>
    internal class ServerHandshake
    {
        private const int GetSize = 3;
        private const int ResponseLength = 129;
        private const int KeyLength = 24;
        private const int MergedKeyLength = 60;

        private const string KeyHeaderString = "\r\nSec-WebSocket-Key: ";

        private readonly BufferPool bufferPool;

        // this isn't an official max, just a reasonable size for a websocket handshake
        private readonly int maxHttpHeaderSize = 3000;

        // SHA-1 is the websocket standard:
        // https://www.rfc-editor.org/rfc/rfc6455
        // we should follow the standard, even though SHA1 is considered weak:
        // https://stackoverflow.com/questions/38038841/why-is-sha-1-considered-insecure
        private readonly SHA1 sha1 = SHA1.Create();

        public ServerHandshake(BufferPool bufferPool, int handshakeMaxSize)
        {
            this.bufferPool = bufferPool;
            maxHttpHeaderSize = handshakeMaxSize;
        }

        ~ServerHandshake()
        {
            sha1.Dispose();
        }

        public bool TryHandshake(Connection conn)
        {
            var stream = conn.stream;

            using (var getHeader = bufferPool.Take(GetSize))
            {
                if (!ReadHelper.TryRead(stream, getHeader.array, 0, GetSize))
                    return false;

                getHeader.count = GetSize;

                if (!IsGet(getHeader.array))
                {
                    Log.Warn("[SWT-ServerHandshake]: First bytes from client was not 'GET' for handshake, instead was {0}", Log.BufferToString(getHeader.array, 0, GetSize));
                    return false;
                }
            }

            var msg = ReadToEndForHandshake(stream);

            if (string.IsNullOrEmpty(msg))
                return false;

            try
            {
                AcceptHandshake(stream, msg);

                conn.request = new Request(msg);
                conn.remoteAddress = conn.CalculateAddress();
                Log.Info($"[SWT-ServerHandshake]: A client connected from {0}", conn);

                return true;
            }
            catch (ArgumentException e)
            {
                Log.InfoException(e);
                return false;
            }
        }

        private string ReadToEndForHandshake(Stream stream)
        {
            using (var readBuffer = bufferPool.Take(maxHttpHeaderSize))
            {
                var readCountOrFail = ReadHelper.SafeReadTillMatch(stream, readBuffer.array, 0, maxHttpHeaderSize, Constants.endOfHandshake);
                if (!readCountOrFail.HasValue)
                    return null;

                var readCount = readCountOrFail.Value;

                var msg = Encoding.ASCII.GetString(readBuffer.array, 0, readCount);
                // GET isn't in the bytes we read here, so we need to add it back
                msg = $"GET{msg}";
                Log.Verbose("[SWT-ServerHandshake]: Client Handshake Message:\r\n{0}", msg);

                return msg;
            }
        }

        private static bool IsGet(byte[] getHeader)
        {
            // just check bytes here instead of using Encoding.ASCII
            return getHeader[0] == 71 && // G
                   getHeader[1] == 69 && // E
                   getHeader[2] == 84; // T
        }

        private void AcceptHandshake(Stream stream, string msg)
        {
            using (ArrayBuffer keyBuffer = bufferPool.Take(KeyLength + Constants.HandshakeGUIDLength),
                   responseBuffer = bufferPool.Take(ResponseLength))
            {
                GetKey(msg, keyBuffer.array);
                AppendGuid(keyBuffer.array);
                var keyHash = CreateHash(keyBuffer.array);
                CreateResponse(keyHash, responseBuffer.array);

                stream.Write(responseBuffer.array, 0, ResponseLength);
            }
        }

        private static void GetKey(string msg, byte[] keyBuffer)
        {
            var start = msg.IndexOf(KeyHeaderString, StringComparison.InvariantCultureIgnoreCase) + KeyHeaderString.Length;

            Log.Verbose("[SWT-ServerHandshake]: Handshake Key: {0}", msg.Substring(start, KeyLength));
            Encoding.ASCII.GetBytes(msg, start, KeyLength, keyBuffer, 0);
        }

        private static void AppendGuid(byte[] keyBuffer)
        {
            Buffer.BlockCopy(Constants.HandshakeGUIDBytes, 0, keyBuffer, KeyLength, Constants.HandshakeGUIDLength);
        }

        private byte[] CreateHash(byte[] keyBuffer)
        {
            Log.Verbose("[SWT-ServerHandshake]: Handshake Hashing {0}", Encoding.ASCII.GetString(keyBuffer, 0, MergedKeyLength));
            return sha1.ComputeHash(keyBuffer, 0, MergedKeyLength);
        }

        private static void CreateResponse(byte[] keyHash, byte[] responseBuffer)
        {
            var keyHashString = Convert.ToBase64String(keyHash);

            // compiler should merge these strings into 1 string before format
            var message = string.Format(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n",
                keyHashString);

            Log.Verbose("[SWT-ServerHandshake]: Handshake Response length {0}, IsExpected {1}", message.Length, message.Length == ResponseLength);
            Encoding.ASCII.GetBytes(message, 0, ResponseLength, responseBuffer, 0);
        }
    }
}