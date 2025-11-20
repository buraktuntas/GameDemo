using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using TacticalCombat.UI;
using TacticalCombat.Core;

namespace TacticalCombat.Network
{
    /// <summary>
    /// P2P Lobby Manager - Host manages all lobby state
    /// Handles player join/leave, ready checks, and game start
    /// </summary>
    public class LobbyManager : NetworkBehaviour
    {
        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private int minPlayersToStart = 2;
        // ✅ REMOVED: gameSceneName - no longer used (we stay in the same scene)
        [SerializeField] private bool requireAllReady = true;

        [Header("References")]
        [SerializeField] private TacticalCombat.UI.LobbyUI lobbyUI;

        // ✅ NEW: Game mode (Individual = true, Team = false)
        [SyncVar(hook = nameof(OnGameModeChanged))]
        private bool isIndividualMode = true;

        // Synchronized player list - only host can modify, all clients see
        private readonly SyncList<LobbyPlayerData> players = new SyncList<LobbyPlayerData>();

        // ✅ NEW: Cache local connection ID (set by server via RPC)
        [SyncVar(hook = nameof(OnLocalConnectionIdChanged))]
        private uint localConnectionId = 0;

        // Events
        public System.Action<LobbyPlayerData> OnPlayerJoined;
        public System.Action<LobbyPlayerData> OnPlayerLeft;
        public System.Action<LobbyPlayerData> OnPlayerUpdated;
        public System.Action OnGameStarting;

        // Singleton instance
        public static LobbyManager Instance { get; private set; }
        
        private void OnLocalConnectionIdChanged(uint oldId, uint newId)
        {
            // Connection ID changed, update local cache
        }

        #region Unity & Network Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // Subscribe to SyncList callbacks (server-side)
            players.Callback += OnPlayersListChanged;
            
            Debug.Log("[LobbyManager] Server started - Lobby ready");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Subscribe to SyncList callbacks (client-side)
            players.Callback += OnPlayersListChanged;
            
            Debug.Log("[LobbyManager] Client connected to lobby");
            
            // Update UI with current player list
            if (lobbyUI != null)
            {
                lobbyUI.RefreshPlayerList(GetAllPlayers());
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            players.Callback -= OnPlayersListChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            players.Callback -= OnPlayersListChanged;
        }

        #endregion

        #region Player Management (Server Only)

        /// <summary>
        /// Called when a player connects to the server
        /// </summary>
        [Server]
        public void RegisterPlayer(NetworkConnectionToClient conn, string playerName)
        {
            if (players.Count >= maxPlayers)
            {
                Debug.LogWarning($"[LobbyManager] Lobby full! Cannot add player: {playerName}");
                // Optionally disconnect the player
                conn.Disconnect();
                return;
            }

            // Check if player already exists (reconnection case)
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].connectionId == conn.connectionId)
                {
                    Debug.LogWarning($"[LobbyManager] Player {playerName} already registered");
                    return;
                }
            }

            // Create player data
            bool isHost = players.Count == 0; // First player is host
            // ✅ NEW: Host is automatically ready (no need to click ready)
            bool autoReady = isHost;
            LobbyPlayerData newPlayer = new LobbyPlayerData(
                (uint)conn.connectionId, // ✅ FIX: Cast to uint
                playerName,
                -1, // No team assigned yet
                autoReady, // ✅ Host is automatically ready
                isHost
            );

            // Add to sync list (automatically syncs to all clients)
            players.Add(newPlayer);

            Debug.Log($"[LobbyManager] Player registered: {playerName} (ID: {conn.connectionId}, Host: {isHost})");
            Debug.Log($"[LobbyManager] Total players: {players.Count}/{maxPlayers}");

            // Notify this specific client of their join
            RpcNotifyPlayerJoined(conn, newPlayer);
            
            // ✅ NEW: Send connection ID to client
            RpcSetLocalConnectionId(conn, (uint)conn.connectionId);
            
            // ✅ DISABLED: Auto-start removed - user must click "Start Game" button
            // CheckAutoStart(); // REMOVED - no auto-start
        }

        /// <summary>
        /// Called when a player disconnects
        /// </summary>
        [Server]
        public void UnregisterPlayer(uint connectionId)
        {
            LobbyPlayerData? removedPlayer = null;

            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].connectionId == connectionId)
                {
                    removedPlayer = players[i];
                    players.RemoveAt(i);
                    break;
                }
            }

            if (removedPlayer.HasValue)
            {
                Debug.Log($"[LobbyManager] Player left: {removedPlayer.Value.playerName}");
                
                // If the host left, promote a new host
                if (removedPlayer.Value.isHost && players.Count > 0)
                {
                    PromoteNewHost();
                }
            }
        }

        [Server]
        private void PromoteNewHost()
        {
            if (players.Count == 0) return;

            var newHostData = players[0];
            newHostData.isHost = true;
            players[0] = newHostData;

            Debug.Log($"[LobbyManager] New host promoted: {newHostData.playerName}");
        }

        #endregion

        #region Player Actions (Commands from Clients)

        /// <summary>
        /// Client requests to join the lobby
        /// ✅ FIX: Mirror Command cannot have optional parameters
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdRegisterPlayer(string playerName)
        {
            // ✅ FIX: Get connection from Mirror's Command context
            // Mirror automatically provides connection context via NetworkBehaviour.connectionToClient
            if (connectionToClient == null) return;
            
            RegisterPlayer(connectionToClient, string.IsNullOrEmpty(playerName) ? "Player" : playerName);
        }

        /// <summary>
        /// Client toggles ready state
        /// ✅ FIX: Get connectionId from Command context
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSetReady(bool ready)
        {
            if (connectionToClient == null) return;
            
            uint connectionId = (uint)connectionToClient.connectionId;
            
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].connectionId == connectionId)
                {
                    var playerData = players[i];
                    playerData.isReady = ready;
                    players[i] = playerData;
                    
                    Debug.Log($"[LobbyManager] Player {playerData.playerName} ready: {ready}");
                    break;
                }
            }
        }

        /// <summary>
        /// Client requests team change
        /// ✅ FIX: Get connectionId from Command context
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSetTeam(int teamId)
        {
            if (connectionToClient == null) return;
            
            // Validate team ID
            if (teamId < -1 || teamId > 1)
            {
                Debug.LogWarning($"[LobbyManager] Invalid team ID: {teamId}");
                return;
            }

            uint connectionId = (uint)connectionToClient.connectionId;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].connectionId == connectionId)
                {
                    var playerData = players[i];
                    playerData.teamId = teamId;
                    players[i] = playerData;
                    
                    Debug.Log($"[LobbyManager] Player {playerData.playerName} changed to team {teamId}");
                    break;
                }
            }
        }

        /// <summary>
        /// Host starts the game (only host can call this)
        /// ✅ FIX: Mirror Command cannot have optional parameters
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdStartGame()
        {
            // ✅ FIX: Get connection from Mirror's Command context
            // Mirror automatically provides connection context via NetworkBehaviour.connectionToClient
            if (connectionToClient == null)
            {
                Debug.LogWarning("[LobbyManager] Could not determine caller connection!");
                return;
            }

            uint callerConnectionId = (uint)connectionToClient.connectionId;

            // Verify caller is host
            bool isHost = false;
            foreach (var player in players)
            {
                if (player.connectionId == callerConnectionId && player.isHost)
                {
                    isHost = true;
                    break;
                }
            }

            if (!isHost)
            {
                Debug.LogWarning($"[LobbyManager] Non-host tried to start game!");
                return;
            }

            // ✅ TEST FIX: Allow 1 player for testing (bypass minimum check)
            // Check minimum players (but allow 1 player for testing)
            if (players.Count < minPlayersToStart && players.Count > 1)
            {
                // ✅ FIX: Use connectionToClient directly (from Command context)
                RpcShowError(connectionToClient, $"Need at least {minPlayersToStart} players to start!");
                return;
            }
            
            // ✅ TEST FIX: Log test mode
            if (players.Count == 1)
            {
                Debug.Log("[LobbyManager] TEST MODE: Starting with 1 player (testing)");
            }

            // Check if all players are ready (if required)
            if (requireAllReady)
            {
                foreach (var player in players)
                {
                    if (!player.isReady && !player.isHost) // Host doesn't need to be ready
                    {
                        // ✅ FIX: Use connectionToClient directly (from Command context)
                        RpcShowError(connectionToClient, $"Player {player.playerName} is not ready!");
                        return;
                    }
                }
            }

            // All checks passed - start the game!
            StartGame();
        }

        #endregion

        #region Game Mode Management

        /// <summary>
        /// ✅ NEW: Set game mode (Individual or Team) - Host only
        /// </summary>
        public void SetGameMode(bool individual)
        {
            if (!isServer)
            {
                Debug.LogWarning("[LobbyManager] Only host can set game mode!");
                return;
            }

            isIndividualMode = individual;
            Debug.Log($"[LobbyManager] Game mode set to: {(individual ? "Individual" : "Team")}");
        }

        /// <summary>
        /// ✅ NEW: Get current game mode
        /// </summary>
        public bool IsIndividualMode()
        {
            return isIndividualMode;
        }

        private void OnGameModeChanged(bool oldMode, bool newMode)
        {
            Debug.Log($"[LobbyManager] Game mode changed: {(newMode ? "Individual" : "Team")}");
            
            // Update UI if needed
            if (lobbyUI != null)
            {
                lobbyUI.OnGameModeChanged(newMode);
            }
        }

        #endregion

        #region Game Start Logic

        [Server]
        private void StartGame()
        {
            Debug.Log("[LobbyManager] Starting game...");
            
            // ✅ CRITICAL: Show all player visuals before starting game (they were hidden in lobby)
            RpcShowAllPlayerVisuals();
            
            // ✅ CRITICAL: Hide all UI before starting game
            RpcHideAllUI();
            
            // ✅ CRITICAL: Start MatchManager match (not scene change - we're already in game scene)
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.StartMatch();
            }
            else
            {
                Debug.LogError("[LobbyManager] MatchManager.Instance is NULL! Cannot start game.");
            }
            
            // Notify all clients
            RpcGameStarting();
        }

        /// <summary>
        /// ✅ TEST FIX: Auto-start game if only 1 player (for testing)
        /// </summary>
        private void CheckAutoStart()
        {
            // Only auto-start if exactly 1 player (testing mode)
            if (players.Count == 1)
            {
                Debug.Log("[LobbyManager] TEST MODE: Auto-starting with 1 player (testing)");
                StartCoroutine(AutoStartDelay());
            }
        }

        private System.Collections.IEnumerator AutoStartDelay()
        {
            // ✅ FIX: Wait 10 seconds for UI to settle and player to see lobby
            // This gives time to see the lobby screen before auto-start
            yield return new WaitForSeconds(10f);
            
            // Double-check we still have 1 player
            if (players.Count == 1)
            {
                Debug.Log("[LobbyManager] TEST MODE: Starting game now...");
                StartGame();
            }
        }

        /// <summary>
        /// ✅ NEW: Hide all UI when game starts
        /// </summary>
        [ClientRpc]
        private void RpcHideAllUI()
        {
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.HideAllUI();
            }
        }
        
        /// <summary>
        /// ✅ NEW: Show all player visuals when game starts (they were hidden in lobby)
        /// </summary>
        [ClientRpc]
        private void RpcShowAllPlayerVisuals()
        {
            // Find all player controllers and show their visuals
            var allPlayers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                // Show all renderers (player body, weapons, etc.)
                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    // Don't show camera or UI elements (they should always be visible)
                    if (renderer.GetComponent<Camera>() == null && 
                        renderer.GetComponent<Canvas>() == null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
            
            Debug.Log("[LobbyManager] All player visuals shown - game starting");
        }

        #endregion

        #region Client RPCs

        [TargetRpc]
        private void RpcNotifyPlayerJoined(NetworkConnection target, LobbyPlayerData playerData)
        {
            Debug.Log($"[LobbyManager] You joined the lobby as: {playerData.playerName}");
            OnPlayerJoined?.Invoke(playerData);
        }

        [ClientRpc]
        private void RpcGameStarting()
        {
            Debug.Log("[LobbyManager] Game is starting!");
            OnGameStarting?.Invoke();
            
            if (lobbyUI != null)
            {
                lobbyUI.ShowGameStarting();
            }
        }

        [TargetRpc]
        private void RpcShowError(NetworkConnection target, string errorMessage)
        {
            Debug.LogWarning($"[LobbyManager] Error: {errorMessage}");
            
            if (lobbyUI != null)
            {
                lobbyUI.ShowError(errorMessage);
            }
        }

        [TargetRpc]
        private void RpcSetLocalConnectionId(NetworkConnection target, uint connectionId)
        {
            // ✅ NEW: Set local connection ID on client
            localConnectionId = connectionId;
            Debug.Log($"[LobbyManager] Local connection ID set to: {connectionId}");
        }

        #endregion

        #region SyncList Callbacks

        private void OnPlayersListChanged(SyncList<LobbyPlayerData>.Operation op, int index, LobbyPlayerData oldItem, LobbyPlayerData newItem)
        {
            // This runs on all clients when the players list changes
            
            switch (op)
            {
                case SyncList<LobbyPlayerData>.Operation.OP_ADD:
                    Debug.Log($"[LobbyManager] Player added: {newItem.playerName}");
                    OnPlayerJoined?.Invoke(newItem);
                    break;
                    
                case SyncList<LobbyPlayerData>.Operation.OP_REMOVEAT:
                    Debug.Log($"[LobbyManager] Player removed: {oldItem.playerName}");
                    OnPlayerLeft?.Invoke(oldItem);
                    break;
                    
                case SyncList<LobbyPlayerData>.Operation.OP_SET:
                    Debug.Log($"[LobbyManager] Player updated: {newItem.playerName}");
                    OnPlayerUpdated?.Invoke(newItem);
                    break;
                    
                case SyncList<LobbyPlayerData>.Operation.OP_CLEAR:
                    Debug.Log($"[LobbyManager] Player list cleared");
                    break;
            }

            // Refresh UI on any change
            if (lobbyUI != null)
            {
                lobbyUI.RefreshPlayerList(GetAllPlayers());
            }
        }

        #endregion

        #region Public Getters

        public List<LobbyPlayerData> GetAllPlayers()
        {
            return new List<LobbyPlayerData>(players);
        }

        public int GetPlayerCount()
        {
            return players.Count;
        }

        public int GetMaxPlayers()
        {
            return maxPlayers;
        }

        public LobbyPlayerData? GetLocalPlayer()
        {
            if (!NetworkClient.active) return null;

            // ✅ FIX: Use cached localConnectionId (set by server via RPC)
            if (localConnectionId == 0)
            {
                // Fallback: Try to find host if we're on server
                if (NetworkServer.active)
                {
                    foreach (var player in players)
                    {
                        if (player.isHost)
                        {
                            return player;
                        }
                    }
                }
                return null;
            }
            
            foreach (var player in players)
            {
                if (player.connectionId == localConnectionId)
                {
                    return player;
                }
            }

            return null;
        }

        public bool IsLocalPlayerHost()
        {
            var localPlayer = GetLocalPlayer();
            return localPlayer?.isHost ?? false;
        }

        public int GetTeamPlayerCount(int teamId)
        {
            int count = 0;
            foreach (var player in players)
            {
                if (player.teamId == teamId)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Auto Team Balance

        [Server]
        public void AutoBalanceTeams()
        {
            int teamACount = GetTeamPlayerCount(0);
            int teamBCount = GetTeamPlayerCount(1);

            // Assign unassigned players to smaller team
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.teamId == -1)
                {
                    player.teamId = teamACount <= teamBCount ? 0 : 1;
                    players[i] = player;

                    if (player.teamId == 0)
                        teamACount++;
                    else
                        teamBCount++;
                }
            }

            Debug.Log($"[LobbyManager] Teams balanced - Team A: {teamACount}, Team B: {teamBCount}");
        }

        #endregion
    }
}

