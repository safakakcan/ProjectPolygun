using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleMatch
{
    public class CanvasController : MonoBehaviour
    {
        /// <summary>
        ///     Cross-reference of client that created the corresponding match in openMatches below
        /// </summary>
        internal static readonly Dictionary<NetworkConnectionToClient, Guid> playerMatches = new();

        /// <summary>
        ///     Open matches that are available for joining
        /// </summary>
        internal static readonly Dictionary<Guid, MatchInfo> openMatches = new();

        /// <summary>
        ///     Network Connections of all players in a match
        /// </summary>
        internal static readonly Dictionary<Guid, HashSet<NetworkConnectionToClient>> matchConnections = new();

        /// <summary>
        ///     Player informations by Network Connection
        /// </summary>
        internal static readonly Dictionary<NetworkConnectionToClient, PlayerInfo> playerInfos = new();

        /// <summary>
        ///     Network Connections that have neither started nor joined a match yet
        /// </summary>
        internal static readonly List<NetworkConnectionToClient> waitingConnections = new();

        [Header("GUI References")] public GameObject matchList;

        public GameObject matchPrefab;
        public GameObject matchControllerPrefab;
        public Button createButton;
        public Button joinButton;
        public GameObject lobbyView;
        public GameObject roomView;
        public RoomGUI roomGUI;
        public ToggleGroup toggleGroup;

        /// <summary>
        ///     GUID of a match the local player has joined
        /// </summary>
        internal Guid localJoinedMatch = Guid.Empty;

        /// <summary>
        ///     GUID of a match the local player has created
        /// </summary>
        internal Guid localPlayerMatch = Guid.Empty;

        // Used in UI for "Player #"
        private int playerIndex = 1;

        /// <summary>
        ///     GUID of a match the local player has selected in the Toggle Group match list
        /// </summary>
        internal Guid selectedMatch = Guid.Empty;

        /// <summary>
        ///     Match Controllers listen for this to terminate their match and clean up
        /// </summary>
        public event Action<NetworkConnectionToClient> OnPlayerDisconnect;

        // RuntimeInitializeOnLoadMethod -> fast playmode without domain reload
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics()
        {
            playerMatches.Clear();
            openMatches.Clear();
            matchConnections.Clear();
            playerInfos.Clear();
            waitingConnections.Clear();
        }

        #region UI Functions

        // Called from several places to ensure a clean reset
        //  - MatchNetworkManager.Awake
        //  - OnStartServer
        //  - OnStartClient
        //  - OnClientDisconnect
        //  - ResetCanvas
        internal void InitializeData()
        {
            playerMatches.Clear();
            openMatches.Clear();
            matchConnections.Clear();
            waitingConnections.Clear();
            localPlayerMatch = Guid.Empty;
            localJoinedMatch = Guid.Empty;
        }

        // Called from OnStopServer and OnStopClient when shutting down
        private void ResetCanvas()
        {
            InitializeData();
            lobbyView.SetActive(false);
            roomView.SetActive(false);
            gameObject.SetActive(false);
        }

        #endregion

        #region Button Calls

        /// <summary>
        ///     Called from <see cref="MatchGUI.OnToggleClicked" />
        /// </summary>
        /// <param name="matchId"></param>
        [ClientCallback]
        public void SelectMatch(Guid matchId)
        {
            if (matchId == Guid.Empty)
            {
                selectedMatch = Guid.Empty;
                joinButton.interactable = false;
            }
            else
            {
                if (!openMatches.Keys.Contains(matchId))
                {
                    joinButton.interactable = false;
                    return;
                }

                selectedMatch = matchId;
                var infos = openMatches[matchId];
                joinButton.interactable = infos.players < infos.maxPlayers;
            }
        }

        /// <summary>
        ///     Assigned in inspector to Create button
        /// </summary>
        [ClientCallback]
        public void RequestCreateMatch()
        {
            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Create });
        }

        /// <summary>
        ///     Assigned in inspector to Cancel button
        /// </summary>
        [ClientCallback]
        public void RequestCancelMatch()
        {
            if (localPlayerMatch == Guid.Empty) return;

            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Cancel });
        }

        /// <summary>
        ///     Assigned in inspector to Join button
        /// </summary>
        [ClientCallback]
        public void RequestJoinMatch()
        {
            if (selectedMatch == Guid.Empty) return;

            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Join, matchId = selectedMatch });
        }

        /// <summary>
        ///     Assigned in inspector to Leave button
        /// </summary>
        [ClientCallback]
        public void RequestLeaveMatch()
        {
            if (localJoinedMatch == Guid.Empty) return;

            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Leave, matchId = localJoinedMatch });
        }

        /// <summary>
        ///     Assigned in inspector to Ready button
        /// </summary>
        [ClientCallback]
        public void RequestReadyChange()
        {
            if (localPlayerMatch == Guid.Empty && localJoinedMatch == Guid.Empty) return;

            var matchId = localPlayerMatch == Guid.Empty ? localJoinedMatch : localPlayerMatch;

            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Ready, matchId = matchId });
        }

        /// <summary>
        ///     Assigned in inspector to Start button
        /// </summary>
        [ClientCallback]
        public void RequestStartMatch()
        {
            if (localPlayerMatch == Guid.Empty) return;

            NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Start });
        }

        /// <summary>
        ///     Called from <see cref="MatchController.RpcExitGame" />
        /// </summary>
        [ClientCallback]
        public void OnMatchEnded()
        {
            localPlayerMatch = Guid.Empty;
            localJoinedMatch = Guid.Empty;
            ShowLobbyView();
        }

        #endregion

        #region Server & Client Callbacks

        // Methods in this section are called from MatchNetworkManager's corresponding methods

        [ServerCallback]
        internal void OnStartServer()
        {
            InitializeData();
            NetworkServer.RegisterHandler<ServerMatchMessage>(OnServerMatchMessage);
        }

        [ServerCallback]
        internal void OnServerReady(NetworkConnectionToClient conn)
        {
            waitingConnections.Add(conn);
            playerInfos.Add(conn, new PlayerInfo { playerIndex = playerIndex, ready = false });
            playerIndex++;

            SendMatchList();
        }

        [ServerCallback]
        internal IEnumerator OnServerDisconnect(NetworkConnectionToClient conn)
        {
            // Invoke OnPlayerDisconnect on all instances of MatchController
            OnPlayerDisconnect?.Invoke(conn);

            if (playerMatches.TryGetValue(conn, out var matchId))
            {
                playerMatches.Remove(conn);
                openMatches.Remove(matchId);

                foreach (var playerConn in matchConnections[matchId])
                {
                    var _playerInfo = playerInfos[playerConn];
                    _playerInfo.ready = false;
                    _playerInfo.matchId = Guid.Empty;
                    playerInfos[playerConn] = _playerInfo;
                    playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
                }
            }

            foreach (var kvp in matchConnections)
                kvp.Value.Remove(conn);

            var playerInfo = playerInfos[conn];
            if (playerInfo.matchId != Guid.Empty)
            {
                if (openMatches.TryGetValue(playerInfo.matchId, out var matchInfo))
                {
                    matchInfo.players--;
                    openMatches[playerInfo.matchId] = matchInfo;
                }

                HashSet<NetworkConnectionToClient> connections;
                if (matchConnections.TryGetValue(playerInfo.matchId, out connections))
                {
                    var infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

                    foreach (var playerConn in matchConnections[playerInfo.matchId])
                        if (playerConn != conn)
                            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });
                }
            }

            SendMatchList();

            yield return null;
        }

        [ServerCallback]
        internal void OnStopServer()
        {
            ResetCanvas();
        }

        [ClientCallback]
        internal void OnStartClient()
        {
            InitializeData();
            ShowLobbyView();
            createButton.gameObject.SetActive(true);
            joinButton.gameObject.SetActive(true);
            NetworkClient.RegisterHandler<ClientMatchMessage>(OnClientMatchMessage);
        }

        [ClientCallback]
        internal void OnClientDisconnect()
        {
            InitializeData();
        }

        [ClientCallback]
        internal void OnStopClient()
        {
            ResetCanvas();
        }

        #endregion

        #region Server Match Message Handlers

        [ServerCallback]
        private void OnServerMatchMessage(NetworkConnectionToClient conn, ServerMatchMessage msg)
        {
            switch (msg.serverMatchOperation)
            {
                case ServerMatchOperation.None:
                {
                    Debug.LogWarning("Missing ServerMatchOperation");
                    break;
                }
                case ServerMatchOperation.Create:
                {
                    OnServerCreateMatch(conn);
                    break;
                }
                case ServerMatchOperation.Cancel:
                {
                    OnServerCancelMatch(conn);
                    break;
                }
                case ServerMatchOperation.Join:
                {
                    OnServerJoinMatch(conn, msg.matchId);
                    break;
                }
                case ServerMatchOperation.Leave:
                {
                    OnServerLeaveMatch(conn, msg.matchId);
                    break;
                }
                case ServerMatchOperation.Ready:
                {
                    OnServerPlayerReady(conn, msg.matchId);
                    break;
                }
                case ServerMatchOperation.Start:
                {
                    OnServerStartMatch(conn);
                    break;
                }
            }
        }

        [ServerCallback]
        private void OnServerCreateMatch(NetworkConnectionToClient conn)
        {
            if (playerMatches.ContainsKey(conn)) return;

            var newMatchId = Guid.NewGuid();
            matchConnections.Add(newMatchId, new HashSet<NetworkConnectionToClient>());
            matchConnections[newMatchId].Add(conn);
            playerMatches.Add(conn, newMatchId);
            openMatches.Add(newMatchId, new MatchInfo { matchId = newMatchId, maxPlayers = 2, players = 1 });

            var playerInfo = playerInfos[conn];
            playerInfo.ready = false;
            playerInfo.matchId = newMatchId;
            playerInfos[conn] = playerInfo;

            var infos = matchConnections[newMatchId].Select(playerConn => playerInfos[playerConn]).ToArray();

            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Created, matchId = newMatchId, playerInfos = infos });

            SendMatchList();
        }

        [ServerCallback]
        private void OnServerCancelMatch(NetworkConnectionToClient conn)
        {
            if (!playerMatches.ContainsKey(conn)) return;

            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Cancelled });

            Guid matchId;
            if (playerMatches.TryGetValue(conn, out matchId))
            {
                playerMatches.Remove(conn);
                openMatches.Remove(matchId);

                foreach (var playerConn in matchConnections[matchId])
                {
                    var playerInfo = playerInfos[playerConn];
                    playerInfo.ready = false;
                    playerInfo.matchId = Guid.Empty;
                    playerInfos[playerConn] = playerInfo;
                    playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
                }

                SendMatchList();
            }
        }

        [ServerCallback]
        private void OnServerJoinMatch(NetworkConnectionToClient conn, Guid matchId)
        {
            if (!matchConnections.ContainsKey(matchId) || !openMatches.ContainsKey(matchId)) return;

            var matchInfo = openMatches[matchId];
            matchInfo.players++;
            openMatches[matchId] = matchInfo;
            matchConnections[matchId].Add(conn);

            var playerInfo = playerInfos[conn];
            playerInfo.ready = false;
            playerInfo.matchId = matchId;
            playerInfos[conn] = playerInfo;

            var infos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
            SendMatchList();

            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Joined, matchId = matchId, playerInfos = infos });

            foreach (var playerConn in matchConnections[matchId])
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });
        }

        [ServerCallback]
        private void OnServerLeaveMatch(NetworkConnectionToClient conn, Guid matchId)
        {
            var matchInfo = openMatches[matchId];
            matchInfo.players--;
            openMatches[matchId] = matchInfo;

            var playerInfo = playerInfos[conn];
            playerInfo.ready = false;
            playerInfo.matchId = Guid.Empty;
            playerInfos[conn] = playerInfo;

            foreach (var kvp in matchConnections)
                kvp.Value.Remove(conn);

            var connections = matchConnections[matchId];
            var infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

            foreach (var playerConn in matchConnections[matchId])
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });

            SendMatchList();

            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
        }

        [ServerCallback]
        private void OnServerPlayerReady(NetworkConnectionToClient conn, Guid matchId)
        {
            var playerInfo = playerInfos[conn];
            playerInfo.ready = !playerInfo.ready;
            playerInfos[conn] = playerInfo;

            var connections = matchConnections[matchId];
            var infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

            foreach (var playerConn in matchConnections[matchId])
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });
        }

        [ServerCallback]
        private void OnServerStartMatch(NetworkConnectionToClient conn)
        {
            if (!playerMatches.ContainsKey(conn)) return;

            Guid matchId;
            if (playerMatches.TryGetValue(conn, out matchId))
            {
                var matchControllerObject = Instantiate(matchControllerPrefab);
                matchControllerObject.GetComponent<NetworkMatch>().matchId = matchId;
                NetworkServer.Spawn(matchControllerObject);

                var matchController = matchControllerObject.GetComponent<MatchController>();

                foreach (var playerConn in matchConnections[matchId])
                {
                    playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Started });

                    var player = Instantiate(NetworkManager.singleton.playerPrefab);
                    player.GetComponent<NetworkMatch>().matchId = matchId;
                    NetworkServer.AddPlayerForConnection(playerConn, player);

                    if (matchController.player1 == null)
                        matchController.player1 = playerConn.identity;
                    else
                        matchController.player2 = playerConn.identity;

                    /* Reset ready state for after the match. */
                    var playerInfo = playerInfos[playerConn];
                    playerInfo.ready = false;
                    playerInfos[playerConn] = playerInfo;
                }

                matchController.startingPlayer = matchController.player1;
                matchController.currentPlayer = matchController.player1;

                playerMatches.Remove(conn);
                openMatches.Remove(matchId);
                matchConnections.Remove(matchId);
                SendMatchList();

                OnPlayerDisconnect += matchController.OnPlayerDisconnect;
            }
        }

        /// <summary>
        ///     Sends updated match list to all waiting connections or just one if specified
        /// </summary>
        /// <param name="conn"></param>
        [ServerCallback]
        internal void SendMatchList(NetworkConnectionToClient conn = null)
        {
            if (conn != null)
                conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
            else
                foreach (var waiter in waitingConnections)
                    waiter.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
        }

        #endregion

        #region Client Match Message Handler

        [ClientCallback]
        private void OnClientMatchMessage(ClientMatchMessage msg)
        {
            switch (msg.clientMatchOperation)
            {
                case ClientMatchOperation.None:
                {
                    Debug.LogWarning("Missing ClientMatchOperation");
                    break;
                }
                case ClientMatchOperation.List:
                {
                    openMatches.Clear();
                    foreach (var matchInfo in msg.matchInfos)
                        openMatches.Add(matchInfo.matchId, matchInfo);

                    RefreshMatchList();
                    break;
                }
                case ClientMatchOperation.Created:
                {
                    localPlayerMatch = msg.matchId;
                    ShowRoomView();
                    roomGUI.RefreshRoomPlayers(msg.playerInfos);
                    roomGUI.SetOwner(true);
                    break;
                }
                case ClientMatchOperation.Cancelled:
                {
                    localPlayerMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
                case ClientMatchOperation.Joined:
                {
                    localJoinedMatch = msg.matchId;
                    ShowRoomView();
                    roomGUI.RefreshRoomPlayers(msg.playerInfos);
                    roomGUI.SetOwner(false);
                    break;
                }
                case ClientMatchOperation.Departed:
                {
                    localJoinedMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
                case ClientMatchOperation.UpdateRoom:
                {
                    roomGUI.RefreshRoomPlayers(msg.playerInfos);
                    break;
                }
                case ClientMatchOperation.Started:
                {
                    lobbyView.SetActive(false);
                    roomView.SetActive(false);
                    break;
                }
            }
        }

        [ClientCallback]
        private void ShowLobbyView()
        {
            lobbyView.SetActive(true);
            roomView.SetActive(false);

            foreach (Transform child in matchList.transform)
                if (child.gameObject.GetComponent<MatchGUI>().GetMatchId() == selectedMatch)
                {
                    var toggle = child.gameObject.GetComponent<Toggle>();
                    toggle.isOn = true;
                }
        }

        [ClientCallback]
        private void ShowRoomView()
        {
            lobbyView.SetActive(false);
            roomView.SetActive(true);
        }

        [ClientCallback]
        private void RefreshMatchList()
        {
            foreach (Transform child in matchList.transform)
                Destroy(child.gameObject);

            joinButton.interactable = false;

            foreach (var matchInfo in openMatches.Values)
            {
                var newMatch = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity);
                newMatch.transform.SetParent(matchList.transform, false);
                newMatch.GetComponent<MatchGUI>().SetMatchInfo(matchInfo);

                var toggle = newMatch.GetComponent<Toggle>();
                toggle.group = toggleGroup;
                if (matchInfo.matchId == selectedMatch)
                    toggle.isOn = true;
            }
        }

        #endregion
    }
}