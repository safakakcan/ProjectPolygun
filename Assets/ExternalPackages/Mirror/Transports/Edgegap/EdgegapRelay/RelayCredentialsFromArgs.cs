// parse session_id and user_id from command line args.
// mac: "open mirror.app --args session_id=123 user_id=456"

using System;
using UnityEngine;

namespace Edgegap
{
    public class RelayCredentialsFromArgs : MonoBehaviour
    {
        private void Awake()
        {
            var cmd = Environment.CommandLine;

            // parse session_id via regex
            var sessionId = EdgegapKcpTransport.ReParse(cmd, "session_id=(\\d+)", "111111");
            var userID = EdgegapKcpTransport.ReParse(cmd, "user_id=(\\d+)", "222222");
            Debug.Log($"Parsed sessionId: {sessionId} user_id: {userID}");

            // configure transport
            var transport = GetComponent<EdgegapKcpTransport>();
            transport.sessionId = uint.Parse(sessionId);
            transport.userId = uint.Parse(userID);
        }
    }
}