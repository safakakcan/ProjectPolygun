using System;
using System.Collections.Generic;
using System.Linq;

namespace Mirror.SimpleWeb
{
    /// <summary>
    ///     Represents a client's request to the Websockets server, which is the first message from the client.
    /// </summary>
    public class Request
    {
        private static readonly char[] lineSplitChars = { '\r', '\n' };
        private static readonly char[] headerSplitChars = { ':' };
        public Dictionary<string, string> Headers = new();
        public string RequestLine;

        public Request(string message)
        {
            var all = message.Split(lineSplitChars, StringSplitOptions.RemoveEmptyEntries);
            RequestLine = all.First();
            Headers = all.Skip(1)
                .Select(header => header.Split(headerSplitChars, 2, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(split => split[0].Trim(), split => split[1].Trim());
        }
    }
}