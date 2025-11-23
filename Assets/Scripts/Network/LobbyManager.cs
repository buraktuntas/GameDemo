using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using TacticalCombat.UI;
using TacticalCombat.Core;
using TacticalCombat.Player;

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
        // ✅ REMOVED: Old LobbyUI reference - now using LobbyUIController
        // [SerializeField] private TacticalCombat.UI.LobbyUI lobbyUI;

        // ✅ NEW: Game mode (Individual = true, Team = false)
        [SyncVar(hook = nameof(OnGameModeChanged))]
        private bool isIndividualMode = true;

        // Synchronized player list - only host can modify, all clients see
        private readonly SyncList<LobbyPlayerData> players = new SyncList<LobbyPlayerData>();

        // ✅ CRITICAL FIX: Local connection ID (set by server via TargetRpc, NOT a SyncVar!)
        // SyncVar would sync to ALL clients with same value - we need each client to know their OWN ID
        private uint localConnectionId = 0;

        // Events
        public System.Action<LobbyPlayerData> OnPlayerJoined;
        public System.Action<LobbyPlayerData> OnPlayerLeft;
        public System.Action<LobbyPlayerData> OnPlayerUpdated;
        public System.Action OnGameStarting;

        // Singleton instance
        public static LobbyManager Instance { get; private set; }

        // ✅ MEMORY LEAK FIX: Track active coroutines for cleanup
        private Coroutine activeRetryConnectionCoroutine = null;
        private Coroutine activeShowVisualsCoroutine = null;
        private Coroutine activeAutoStartCoroutine = null;
        private Coroutine activeCameraSetupCoroutine = null;
        private Coroutine activeDisconnectCoroutine = null;
        private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();

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

        private void OnDestroy()
        {
            // ✅ MEMORY LEAK FIX: Clean up all active coroutines
            if (activeRetryConnectionCoroutine != null) StopCoroutine(activeRetryConnectionCoroutine);
            if (activeShowVisualsCoroutine != null) StopCoroutine(activeShowVisualsCoroutine);
            if (activeAutoStartCoroutine != null) StopCoroutine(activeAutoStartCoroutine);
            if (activeCameraSetupCoroutine != null) StopCoroutine(activeCameraSetupCoroutine);
            if (activeDisconnectCoroutine != null) StopCoroutine(activeDisconnectCoroutine);

            // Stop all tracked coroutines
            foreach (var coroutine in activeCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            activeCoroutines.Clear();

            // Cancel all Invoke calls
            CancelInvoke();

            // Clear instance reference
            if (Instance == this)
            {
                Instance = null;
            }
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
            
            // ✅ NEW: Use new LobbyUIController (AAA quality)
            var lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController == null)
            {
                lobbyController = FindFirstObjectByType<TacticalCombat.UI.LobbyUIController>();
            }
            
            if (lobbyController == null)
            {
                // Create new LobbyUIController
                GameObject controllerObj = new GameObject("LobbyUIController");
                lobbyController = controllerObj.AddComponent<TacticalCombat.UI.LobbyUIController>();
                Debug.Log("✅ [LobbyManager] Created new LobbyUIController");
            }

            // Update UI with current player list
            if (lobbyController != null)
            {
                // ✅ CRITICAL: Ensure LobbyUIController is shown when client connects
                lobbyController.ShowLobby();
                
                // ✅ CRITICAL FIX: Force refresh player list when client connects
                // This ensures client sees all players (including host) that were registered before client connected
                StartCoroutine(DelayedClientRefresh());
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Delayed refresh to ensure client sees all players (including host)
        /// </summary>
        private System.Collections.IEnumerator DelayedClientRefresh()
        {
            // Wait a frame for SyncList to fully sync
            yield return null;
            
            var lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null)
            {
                Debug.Log($"[LobbyManager] 🔄 DelayedClientRefresh: Refreshing player list ({players.Count} players)");
                lobbyController.RefreshPlayerList();
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
                
                // ✅ ERROR HANDLING: Show error message to client before disconnecting
                RpcShowLobbyFullError(conn, $"Lobby is full! Maximum {maxPlayers} players allowed.");
                
                // Wait a moment for RPC to be sent before disconnecting
                // ✅ MEMORY LEAK FIX: Track coroutine (add to list for cleanup)
                var disconnectCoroutine = StartCoroutine(DisconnectPlayerAfterError(conn));
                activeCoroutines.Add(disconnectCoroutine);
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
            // ✅ FIX: Only host is automatically ready, clients start as NOT ready
            bool autoReady = isHost; // Only host auto-ready, clients must click ready button
            LobbyPlayerData newPlayer = new LobbyPlayerData(
                (uint)conn.connectionId, // ✅ FIX: Cast to uint
                playerName,
                -1, // No team assigned yet
                autoReady, // ✅ Only host is automatically ready, clients start as NOT ready
                isHost
            );

            // Add to sync list (automatically syncs to all clients)
            players.Add(newPlayer);

            Debug.Log($"[LobbyManager] Player registered: {playerName} (ID: {conn.connectionId}, Host: {isHost})");
            Debug.Log($"[LobbyManager] Total players: {players.Count}/{maxPlayers}");

            // Notify this specific client of their join
            RpcNotifyPlayerJoined(conn, newPlayer);

            // ✅ CRITICAL: Send connection ID to client (retry mechanism)
            uint connId = (uint)conn.connectionId;
            Debug.Log($"[LobbyManager] 📡 Sending TargetSetLocalConnectionId to client (ConnectionID: {connId})");
            TargetSetLocalConnectionId(conn, connId);

            // ✅ CRITICAL: Retry RPC after a delay to ensure it arrives
            // ✅ MEMORY LEAK FIX: Track coroutine for cleanup
            if (activeRetryConnectionCoroutine != null) StopCoroutine(activeRetryConnectionCoroutine);
            activeRetryConnectionCoroutine = StartCoroutine(RetrySetLocalConnectionId(conn, connId));

            // ✅ DISABLED: Auto-start removed - user must click "Start Game" button
            // CheckAutoStart(); // REMOVED - no auto-start
        }

        /// <summary>
        /// ✅ AAA FIX: Retry mechanism for TargetSetLocalConnectionId
        /// Mirror RPCs can sometimes be lost if client is not fully ready
        /// ✅ FIX: Retry 3 times with increasing delays
        /// </summary>
        [Server]
        private System.Collections.IEnumerator RetrySetLocalConnectionId(NetworkConnectionToClient conn, uint connectionId)
        {
            // ✅ FIX: Retry 3 times with exponential backoff
            float[] retryDelays = { 0.5f, 1.0f, 2.0f };

            for (int attempt = 0; attempt < retryDelays.Length; attempt++)
            {
                // Wait for client to fully initialize
                yield return new WaitForSeconds(retryDelays[attempt]);

                // Retry RPC
                if (conn != null && conn.isReady)
                {
                    Debug.Log($"[LobbyManager] 🔄 Retrying TargetSetLocalConnectionId (Attempt {attempt + 1}/{retryDelays.Length}, ConnectionID: {connectionId})");
                    TargetSetLocalConnectionId(conn, connectionId);
                }
                else
                {
                    Debug.LogWarning($"[LobbyManager] ⚠️ Cannot retry RPC (Attempt {attempt + 1}) - connection lost or not ready");
                    yield break; // Stop retrying if connection is lost
                }
            }
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
        /// ✅ AAA FIX: Robust command with multiple fallbacks for connection detection
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSetReady(bool ready, NetworkConnectionToClient sender = null)
        {
            // ✅ CRITICAL: Try multiple ways to get connection (Mirror can be inconsistent)
            NetworkConnectionToClient conn = sender ?? connectionToClient;

            if (conn == null)
            {
                Debug.LogError("[LobbyManager] ❌ CRITICAL: CmdSetReady called but no connection context available!");
                Debug.LogError("[LobbyManager]   - sender parameter: null");
                Debug.LogError("[LobbyManager]   - connectionToClient: null");
                Debug.LogError("[LobbyManager]   - This should never happen in Mirror!");
                return;
            }

            uint connectionId = (uint)conn.connectionId;
            Debug.Log($"[LobbyManager] 🎮 CmdSetReady received: ready={ready}, connectionId={connectionId}, players.Count={players.Count}");
            Debug.Log($"[LobbyManager]   Connection: {conn.address} (isReady: {conn.isReady}, isAuthenticated: {conn.isAuthenticated})");

            // Find player by connectionId
            int playerIndex = -1;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].connectionId == connectionId)
                {
                    playerIndex = i;
                    break;
                }
            }

            if (playerIndex == -1)
            {
                Debug.LogError($"[LobbyManager] ❌ Player with connectionId {connectionId} not found in lobby! Players in lobby:");
                for (int i = 0; i < players.Count; i++)
                {
                    Debug.LogError($"   [{i}] {players[i].playerName} (ID: {players[i].connectionId}, Ready: {players[i].isReady})");
                }
                return;
            }

            var oldPlayerData = players[playerIndex];
            Debug.Log($"[LobbyManager] 🔍 Found player at index {playerIndex}: '{oldPlayerData.playerName}', current ready={oldPlayerData.isReady}, new ready={ready}");

            // ✅ CRITICAL: Create new struct instance to ensure SyncList detects change
            var newPlayerData = new LobbyPlayerData(
                oldPlayerData.connectionId,
                oldPlayerData.playerName,
                oldPlayerData.teamId,
                ready, // ✅ New ready state
                oldPlayerData.isHost
            );

            // ✅ CRITICAL: Set the new struct - this MUST trigger OP_SET callback
            players[playerIndex] = newPlayerData;

            Debug.Log($"[LobbyManager] ✅ Player '{newPlayerData.playerName}' ready state changed: {oldPlayerData.isReady} -> {ready} (ConnectionID: {connectionId})");
            Debug.Log($"[LobbyManager] 📡 SyncList updated at index {playerIndex}, waiting for OP_SET callback...");

            // ✅ CRITICAL: Explicitly invoke event on server (callback will fire on clients)
            // This ensures server-side UI also updates immediately
            if (OnPlayerUpdated != null)
            {
                Debug.Log($"[LobbyManager] 🔔 Invoking OnPlayerUpdated event on server (subscribers: {OnPlayerUpdated.GetInvocationList().Length})");
                OnPlayerUpdated.Invoke(newPlayerData);
            }
            else
            {
                Debug.LogWarning("[LobbyManager] ⚠️ OnPlayerUpdated event has no subscribers!");
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
        /// ✅ AAA FIX: Comprehensive validation and logging
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdStartGame(NetworkConnectionToClient sender = null)
        {
            Debug.Log("🎮 [LobbyManager] ========== CmdStartGame RECEIVED ==========");
            Debug.Log($"[LobbyManager]   Players in lobby: {players.Count}");
            Debug.Log($"[LobbyManager]   Server active: {isServer}");

            // ✅ CRITICAL FIX: Try multiple ways to get connection (host uses LocalConnection, not connectionToClient)
            NetworkConnectionToClient conn = sender ?? connectionToClient;

            if (conn == null)
            {
                Debug.LogError("❌ [LobbyManager] CmdStartGame: No connection available!");
                Debug.LogError($"   - sender: {sender}");
                Debug.LogError($"   - connectionToClient: {connectionToClient}");
                Debug.LogError($"   - NetworkServer.localConnection: {NetworkServer.localConnection}");

                // ✅ FALLBACK: If we're server and it's the host calling, use local connection
                if (NetworkServer.active && NetworkServer.localConnection != null)
                {
                    conn = NetworkServer.localConnection as NetworkConnectionToClient;
                    Debug.Log($"✅ [LobbyManager] Using NetworkServer.localConnection (Host): {conn}");
                }

                if (conn == null)
                {
                    Debug.LogError("❌ [LobbyManager] Cannot determine caller - aborting!");
                    return;
                }
            }

            uint callerConnectionId = (uint)conn.connectionId;
            Debug.Log($"[LobbyManager]   Caller connection ID: {callerConnectionId}");

            // Verify caller is host
            bool isHost = false;
            string callerName = "Unknown";
            foreach (var player in players)
            {
                if (player.connectionId == callerConnectionId)
                {
                    callerName = player.playerName;
                    isHost = player.isHost;
                    Debug.Log($"[LobbyManager]   Caller: {callerName} (Host: {isHost})");
                    break;
                }
            }

            if (!isHost)
            {
                Debug.LogWarning($"❌ [LobbyManager] Non-host '{callerName}' tried to start game!");
                RpcShowError(conn, "Only the host can start the game!");
                return;
            }

            Debug.Log("✅ [LobbyManager] Host verified - checking game start conditions...");

            // ✅ CRITICAL: Check minimum players before starting
            Debug.Log($"[LobbyManager] Checking minimum players... (Current: {players.Count}, Min: {minPlayersToStart})");
            if (players.Count < minPlayersToStart)
            {
                if (players.Count == 1)
                {
                    // ✅ TEST MODE: Allow 1 player for testing
                    Debug.Log("⚠️ [LobbyManager] TEST MODE: Starting with 1 player (testing)");
                }
                else
                {
                    string errorMsg = $"Need at least {minPlayersToStart} players to start! (Current: {players.Count})";
                    Debug.LogWarning($"❌ [LobbyManager] {errorMsg}");
                    RpcShowError(conn, errorMsg);
                    return;
                }
            }

            // ✅ CRITICAL: Check if all players are ready (if required)
            Debug.Log($"[LobbyManager] Checking ready status... (RequireAllReady: {requireAllReady})");
            Debug.Log($"[LobbyManager] Current player states:");
            for (int i = 0; i < players.Count; i++)
            {
                Debug.Log($"   [{i}] {players[i].playerName} - Ready: {players[i].isReady}, Host: {players[i].isHost}");
            }

            if (requireAllReady)
            {
                List<string> notReadyPlayers = new List<string>();
                foreach (var player in players)
                {
                    // ✅ CRITICAL: ALL players must be ready (including host)
                    if (!player.isReady)
                    {
                        notReadyPlayers.Add(player.playerName);
                    }
                }

                if (notReadyPlayers.Count > 0)
                {
                    string notReadyList = string.Join(", ", notReadyPlayers);
                    string errorMsg = $"Players not ready: {notReadyList}";
                    Debug.LogWarning($"❌ [LobbyManager] {errorMsg}");
                    RpcShowError(conn, errorMsg);
                    return;
                }
            }

            Debug.Log("✅ [LobbyManager] All conditions met - starting game!");

            // All checks passed - start the game!
            
            // ✅ NEW: Assign random teams if in Team Mode
            if (!isIndividualMode)
            {
                AssignRandomTeams();
            }
            else
            {
                // Reset teams for individual mode (everyone is -1 or unique, but -1 is fine for FFA usually, 
                // or we can assign unique IDs if needed. For now, -1 means "No Team" / FFA)
                ResetTeams();
            }

            StartGame();
        }

        [Server]
        private void AssignRandomTeams()
        {
            Debug.Log("[LobbyManager] Assigning random teams...");
            
            // Create a list of indices to shuffle
            List<int> indices = new List<int>();
            for (int i = 0; i < players.Count; i++)
            {
                indices.Add(i);
            }
            
            // Shuffle indices
            for (int i = 0; i < indices.Count; i++)
            {
                int temp = indices[i];
                int randomIndex = UnityEngine.Random.Range(i, indices.Count);
                indices[i] = indices[randomIndex];
                indices[randomIndex] = temp;
            }
            
            // Assign teams (0 and 1) alternating
            // This ensures balanced teams (e.g. 4 players -> 2 vs 2, 3 players -> 2 vs 1)
            for (int i = 0; i < indices.Count; i++)
            {
                int playerIndex = indices[i];
                int teamId = i % 2; // 0, 1, 0, 1...
                
                var player = players[playerIndex];
                player.teamId = teamId;
                players[playerIndex] = player;
                
                Debug.Log($"[LobbyManager] Assigned {player.playerName} to Team {teamId}");
            }
        }

        [Server]
        private void ResetTeams()
        {
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.teamId = -1; // FFA
                players[i] = player;
            }
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
            
            // Update UI if needed - LobbyUIController will handle this via events
            // No direct method call needed, events will trigger UI updates
        }

        #endregion

        #region Game Start Logic

        [Server]
        private void StartGame()
        {
            Debug.Log("[LobbyManager] Starting game...");
            
            // ✅ CRITICAL: Start MatchManager FIRST (phase change triggers PlayerController updates)
            // This ensures phase is updated before visuals are shown
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.StartMatch();
            }
            else
            {
                Debug.LogError("[LobbyManager] MatchManager.Instance is NULL! Cannot start game.");
                return; // Don't continue if MatchManager is missing
            }
            
            // ✅ RACE CONDITION FIX: Wait a frame for phase change to propagate
            // Then show visuals (prevents race condition between phase change and visual updates)
            // ✅ MEMORY LEAK FIX: Track coroutine
            if (activeShowVisualsCoroutine != null) StopCoroutine(activeShowVisualsCoroutine);
            activeShowVisualsCoroutine = StartCoroutine(ShowPlayerVisualsAfterPhaseChange());
            
            // ✅ CRITICAL: Hide all UI before starting game
            RpcHideAllUI();
            
            // ✅ NEW: Apply team assignments to PlayerControllers
            foreach (var player in players)
            {
                // Find PlayerController for this connection
                NetworkConnection conn = null;
                foreach (var c in NetworkServer.connections.Values)
                {
                    if (c.connectionId == player.connectionId)
                    {
                        conn = c;
                        break;
                    }
                }
                
                if (conn != null && conn.identity != null)
                {
                    var playerController = conn.identity.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        // Map Lobby Team ID to Game Team Enum
                        // Lobby: 0=TeamA, 1=TeamB, -1=None
                        // Game: 0=None, 1=TeamA, 2=TeamB
                        Team gameTeam = Team.None;
                        if (player.teamId == 0) gameTeam = Team.TeamA;
                        else if (player.teamId == 1) gameTeam = Team.TeamB;
                        
                        // Set team (SyncVar will propagate to clients)
                        playerController.team = gameTeam;
                        
                        // Also register with MatchManager
                        if (MatchManager.Instance != null)
                        {
                            MatchManager.Instance.RegisterPlayer(playerController.netId, playerController.team, playerController.role);
                        }
                        
                        Debug.Log($"[LobbyManager] Applied team assignment: {player.playerName} -> {gameTeam}");
                    }
                }
            }

            // ✅ CRITICAL: Notify all clients AFTER MatchManager phase change
            // This ensures phase is updated before visuals are shown
            RpcGameStarting();
        }
        
        /// <summary>
        /// ✅ RACE CONDITION FIX: Show player visuals after phase change has propagated
        /// This prevents race condition between MatchManager phase change and visual updates
        /// </summary>
        private System.Collections.IEnumerator ShowPlayerVisualsAfterPhaseChange()
        {
            // Wait for phase change to propagate to all clients
            yield return new WaitForSeconds(0.1f); // 100ms should be enough for SyncVar propagation
            
            // Now show visuals (PlayerController.CheckAndUpdatePlayerControls should have already run)
            RpcShowAllPlayerVisuals();
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
        /// MatchManager phase change will trigger PlayerController.CheckAndUpdatePlayerControls automatically
        /// ✅ RACE CONDITION FIX: This is called AFTER phase change, so PlayerController should have already updated
        /// </summary>
        [ClientRpc]
        private void RpcShowAllPlayerVisuals()
        {
            // ✅ RACE CONDITION FIX: Verify phase has changed before showing visuals
            // This prevents race condition where visuals are shown before phase change
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == Phase.Lobby)
                {
                    // Phase hasn't changed yet, wait a bit more
                    Debug.LogWarning("[LobbyManager] Phase still Lobby, waiting for phase change...");
                    StartCoroutine(ShowPlayerVisualsDelayed());
                    return;
                }
            }
            
            // Phase has changed, safe to show visuals
            ShowPlayerVisualsNow();
        }
        
        /// <summary>
        /// ✅ RACE CONDITION FIX: Show visuals after phase change delay
        /// </summary>
        private System.Collections.IEnumerator ShowPlayerVisualsDelayed()
        {
            // Wait for phase change to complete
            int maxWait = 20; // 2 seconds max
            int waitCount = 0;
            
            while (waitCount < maxWait)
            {
                if (MatchManager.Instance != null)
                {
                    Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                    if (currentPhase != Phase.Lobby)
                    {
                        // Phase changed, safe to show visuals
                        break;
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
                waitCount++;
            }
            
            // Show visuals now
            ShowPlayerVisualsNow();
        }
        
        /// <summary>
        /// ✅ RACE CONDITION FIX: Actually show player visuals (called after phase change)
        /// </summary>
        private void ShowPlayerVisualsNow()
        {
            Debug.Log("[LobbyManager] ShowPlayerVisualsNow() called - showing all players and UI");
            
            // ✅ CRITICAL FIX: Find local player using NetworkClient.localPlayer (more reliable than isLocalPlayer flag)
            Player.PlayerController localPlayerController = null;
            
            // Method 1: Use NetworkClient.localPlayer (most reliable)
            if (NetworkClient.localPlayer != null)
            {
                localPlayerController = NetworkClient.localPlayer.GetComponent<Player.PlayerController>();
                if (localPlayerController != null)
                {
                    Debug.Log($"[LobbyManager] 🔍 Found local player via NetworkClient.localPlayer: {localPlayerController.name}");
                }
            }
            
            // ✅ CRITICAL FIX: Get all players once (for both local player search and activation)
            // ✅ CRITICAL: Use FindObjectsInactive to find inactive players too!
            var allPlayers = FindObjectsByType<Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[LobbyManager] Found {allPlayers.Length} PlayerController(s) in scene (including inactive)");
            
            // Method 2: Fallback - find by isLocalPlayer flag
            if (localPlayerController == null)
            {
                foreach (var player in allPlayers)
                {
                    if (player.isLocalPlayer)
                    {
                        localPlayerController = player;
                        Debug.Log($"[LobbyManager] 🔍 Found local player via isLocalPlayer flag: {player.name}");
                        break;
                    }
                }
            }
            
            // Method 3: Last resort - find by NetworkClient.connection.identity
            if (localPlayerController == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                localPlayerController = NetworkClient.connection.identity.GetComponent<Player.PlayerController>();
                if (localPlayerController != null)
                {
                    Debug.Log($"[LobbyManager] 🔍 Found local player via NetworkClient.connection.identity: {localPlayerController.name}");
                }
            }
            
            // ✅ CRITICAL FIX: Activate ALL players first (for network sync)
            foreach (var player in allPlayers)
            {
                // ✅ CRITICAL FIX: Activate player GameObject first
                if (!player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(true);
                    Debug.Log($"[LobbyManager] Activated player GameObject: {player.name}");
                }
                
                // ✅ RACE CONDITION FIX: Call CheckAndUpdatePlayerControls first to ensure phase is handled
                // This ensures PlayerController has already updated based on phase
                player.CheckAndUpdatePlayerControls();
            }
            
            // ✅ CRITICAL FIX: Now handle local player camera
            if (localPlayerController != null)
            {
                Debug.Log($"[LobbyManager] 🔍 Processing local player: {localPlayerController.name}, Active: {localPlayerController.gameObject.activeSelf}");
                
                // ✅ CRITICAL FIX: Activate player GameObject FIRST (it's inactive!)
                if (!localPlayerController.gameObject.activeSelf)
                {
                    localPlayerController.gameObject.SetActive(true);
                    Debug.Log($"[LobbyManager] ✅ Activated local player GameObject: {localPlayerController.name}");
                }
                
                // ✅ CRITICAL FIX: Also activate all parent GameObjects
                Transform current = localPlayerController.transform;
                while (current != null && current.parent != null)
                {
                    if (!current.parent.gameObject.activeSelf)
                    {
                        current.parent.gameObject.SetActive(true);
                        Debug.Log($"[LobbyManager] ✅ Activated parent GameObject: {current.parent.name}");
                    }
                    current = current.parent;
                }
                
                var fpsController = localPlayerController.GetComponent<Player.FPSController>();
                if (fpsController == null)
                {
                    Debug.LogError($"[LobbyManager] ❌ FPSController not found on local player: {localPlayerController.name}");
                }
                else
                {
                    Debug.Log($"[LobbyManager] ✅ FPSController found, playerCamera: {(fpsController.playerCamera != null ? fpsController.playerCamera.name : "NULL")}");
                    
                    // ✅ CRITICAL FIX: Setup camera first if it doesn't exist
                    if (fpsController.playerCamera == null)
                    {
                        Debug.Log($"[LobbyManager] 🔧 Setting up camera for local player...");
                        fpsController.SendMessage("SetupCamera", SendMessageOptions.DontRequireReceiver);
                        
                        // Wait a frame for SetupCamera to complete
                        StartCoroutine(SetupCameraAndActivate(fpsController, localPlayerController.name));
                    }
                    else
                    {
                        // Camera exists - activate it immediately
                        ActivatePlayerCamera(fpsController, localPlayerController.name);
                        
                        // Also retry with delay as backup
                        StartCoroutine(EnsureCameraActiveDelayed(fpsController, localPlayerController.name));
                    }
                }
            }
            else
            {
                Debug.LogError("[LobbyManager] ❌ Local player NOT FOUND! Cannot activate camera!");
                
                // ✅ LAST RESORT: Try to find ANY FPSController with a camera (including inactive)
                var allFPS = FindObjectsByType<Player.FPSController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var fps in allFPS)
                {
                    // ✅ CRITICAL: Activate FPSController GameObject first
                    if (!fps.gameObject.activeSelf)
                    {
                        fps.gameObject.SetActive(true);
                        Debug.LogWarning($"[LobbyManager] ⚠️ LAST RESORT: Activated FPSController GameObject: {fps.name}");
                    }
                    
                    if (fps.playerCamera != null)
                    {
                        Debug.LogWarning($"[LobbyManager] ⚠️ LAST RESORT: Activating camera from FPSController: {fps.name}");
                        ActivatePlayerCamera(fps, fps.name);
                        break;
                    }
                }
            }
            
            // ✅ CRITICAL FIX: Also process all players for visuals (non-local players)
            foreach (var player in allPlayers)
            {
                
                // Ensure PlayerVisuals component is active
                var playerVisuals = player.GetComponent<Player.PlayerVisuals>();
                if (playerVisuals != null)
                {
                    playerVisuals.gameObject.SetActive(true);
                    
                    // Show all renderers in PlayerVisuals
                    Renderer[] renderers = playerVisuals.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null && 
                            renderer.GetComponent<Camera>() == null && 
                            renderer.GetComponent<Canvas>() == null)
                        {
                            renderer.enabled = true;
                        }
                    }
                }
                
                // Show all renderers in player GameObject (body, weapons, etc.)
                Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in playerRenderers)
                {
                    if (renderer != null &&
                        renderer.GetComponent<Camera>() == null && 
                        renderer.GetComponent<Canvas>() == null)
                    {
                        renderer.enabled = true;
                    }
                }
                
                // ✅ CRITICAL FIX: Activate CurrentWeapon GameObject if it exists
                Transform weaponHolder = player.transform.Find("WeaponHolder");
                if (weaponHolder == null)
                {
                    // Try to find in PlayerVisual
                    var playerVisual = player.transform.Find("PlayerVisual");
                    if (playerVisual != null)
                    {
                        weaponHolder = playerVisual.Find("WeaponHolder");
                    }
                }
                
                if (weaponHolder != null)
                {
                    Transform currentWeapon = weaponHolder.Find("CurrentWeapon");
                    if (currentWeapon != null && !currentWeapon.gameObject.activeSelf)
                    {
                        currentWeapon.gameObject.SetActive(true);
                        Debug.Log($"[LobbyManager] Activated CurrentWeapon for player: {player.name}");
                    }
                }
            }
            
            // ✅ CRITICAL FIX: Disable BootstrapCamera when game starts
            var bootstrapCameraObj = GameObject.Find("BootstrapCamera");
            if (bootstrapCameraObj != null)
            {
                var bootstrapCamera = bootstrapCameraObj.GetComponent<Camera>();
                if (bootstrapCamera != null)
                {
                    bootstrapCamera.enabled = false;
                    Debug.Log("[LobbyManager] BootstrapCamera disabled");
                }
                // Don't destroy it - URPCameraBootstrap will handle that
            }
            
            // ✅ CRITICAL FIX: Show GameHUD when game starts
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.gameObject.SetActive(true);
                Debug.Log("[LobbyManager] GameHUD activated");
            }
            else
            {
                Debug.LogWarning("[LobbyManager] GameHUD not found in scene!");
            }
            
            Debug.Log("[LobbyManager] All player visuals shown - game starting");
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Setup camera and then activate it
        /// </summary>
        private System.Collections.IEnumerator SetupCameraAndActivate(Player.FPSController fpsController, string playerName)
        {
            yield return null; // Wait a frame for SetupCamera to complete
            
            // Check if camera was created
            if (fpsController != null && fpsController.playerCamera != null)
            {
                ActivatePlayerCamera(fpsController, playerName);
            }
            else
            {
                Debug.LogError($"[LobbyManager] ❌ Camera setup failed for {playerName} - camera is still null!");
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Activate player camera with all necessary settings
        /// </summary>
        private void ActivatePlayerCamera(Player.FPSController fpsController, string playerName)
        {
            if (fpsController == null || fpsController.playerCamera == null)
            {
                Debug.LogError($"[LobbyManager] ❌ Cannot activate camera - FPSController or camera is null!");
                return;
            }
            
            // ✅ CRITICAL FIX: Ensure FPSController GameObject is active
            if (!fpsController.gameObject.activeSelf)
            {
                fpsController.gameObject.SetActive(true);
                Debug.Log($"[LobbyManager] ✅ Activated FPSController GameObject: {fpsController.name}");
            }
            
            var camera = fpsController.playerCamera;
            
            // ✅ CRITICAL: Ensure camera GameObject is active
            if (!camera.gameObject.activeSelf)
            {
                camera.gameObject.SetActive(true);
                Debug.Log($"[LobbyManager] ✅ Activated camera GameObject: {camera.name}");
            }
            
            // ✅ CRITICAL: Disable BootstrapCamera first
            var bootstrapCameraObj = GameObject.Find("BootstrapCamera");
            if (bootstrapCameraObj != null)
            {
                var bootstrapCamera = bootstrapCameraObj.GetComponent<Camera>();
                if (bootstrapCamera != null)
                {
                    bootstrapCamera.enabled = false;
                    Debug.Log("[LobbyManager] BootstrapCamera disabled in ActivatePlayerCamera");
                }
            }
            
            // ✅ CRITICAL: Enable and activate camera
            camera.enabled = true;
            camera.gameObject.SetActive(true);
            
            // ✅ CRITICAL: Ensure camera GameObject and all parents are active
            Transform camTransform = camera.transform;
            while (camTransform != null)
            {
                if (!camTransform.gameObject.activeSelf)
                {
                    camTransform.gameObject.SetActive(true);
                    Debug.Log($"[LobbyManager] ✅ Activated camera parent: {camTransform.name}");
                }
                camTransform = camTransform.parent;
            }
            
            // ✅ CRITICAL: Set MainCamera tag
            if (!camera.CompareTag("MainCamera"))
            {
                camera.tag = "MainCamera";
            }
            
            // ✅ CRITICAL: Set camera depth (higher than bootstrap)
            camera.depth = 0;
            
            // ✅ CRITICAL: Ensure URP camera data exists
            if (camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() == null)
            {
                camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }
            
            // ✅ CRITICAL FIX: Force camera to render by setting target display
            camera.targetDisplay = 0; // Display 1
            
            // ✅ CRITICAL FIX: Ensure camera has proper settings for rendering
            // Check and log camera settings
            int cullingMaskValue = camera.cullingMask; // LayerMask is an int
            Debug.Log($"[LobbyManager] 📷 Camera Settings - Position: {camera.transform.position}, Rotation: {camera.transform.rotation.eulerAngles}");
            Debug.Log($"[LobbyManager] 📷 Camera Settings - FOV: {camera.fieldOfView}, Near: {camera.nearClipPlane}, Far: {camera.farClipPlane}");
            Debug.Log($"[LobbyManager] 📷 Camera Settings - CullingMask: {cullingMaskValue} (0x{cullingMaskValue:X8})");
            Debug.Log($"[LobbyManager] 📷 Camera Settings - ClearFlags: {camera.clearFlags}, Background: {camera.backgroundColor}");
            
            // ✅ CRITICAL FIX: Ensure camera culling mask includes default layers (Everything = -1)
            if (camera.cullingMask == 0)
            {
                camera.cullingMask = -1; // Everything
                Debug.LogWarning("[LobbyManager] ⚠️ Camera culling mask was 0! Set to Everything (-1)");
            }
            
            // ✅ CRITICAL FIX: Ensure camera has proper clear flags (Skybox or Solid Color)
            if (camera.clearFlags == CameraClearFlags.Nothing)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                Debug.LogWarning("[LobbyManager] ⚠️ Camera clear flags was Nothing! Set to Skybox");
            }
            
            // ✅ CRITICAL FIX: Ensure camera is at a valid position (not at origin if player moved)
            if (camera.transform.position == Vector3.zero && fpsController.transform.position != Vector3.zero)
            {
                Debug.LogWarning($"[LobbyManager] ⚠️ Camera at origin but player at {fpsController.transform.position} - camera may need repositioning");
            }
            
            // ✅ NOTE: Do NOT call camera.Render() manually in URP - URP handles rendering automatically
            // Manual Render() calls cause "Not inside a Renderpass" errors in URP
            
            Debug.Log($"[LobbyManager] ✅ Camera activated for local player: {playerName} (Enabled: {camera.enabled}, Active: {camera.gameObject.activeSelf}, Depth: {camera.depth}, Tag: {camera.tag}, TargetDisplay: {camera.targetDisplay})");
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Ensure camera is active with retry (handles timing issues)
        /// </summary>
        private System.Collections.IEnumerator EnsureCameraActiveDelayed(Player.FPSController fpsController, string playerName)
        {
            // Wait a frame for SetupCamera/OnStartLocalPlayer to complete
            yield return null;
            
            // ✅ CRITICAL FIX: Disable BootstrapCamera first
            var bootstrapCameraObj = GameObject.Find("BootstrapCamera");
            if (bootstrapCameraObj != null)
            {
                var bootstrapCamera = bootstrapCameraObj.GetComponent<Camera>();
                if (bootstrapCamera != null)
                {
                    bootstrapCamera.enabled = false;
                    Debug.Log("[LobbyManager] BootstrapCamera disabled in EnsureCameraActiveDelayed");
                }
            }
            
            // Retry up to 10 times (1 second total)
            for (int i = 0; i < 10; i++)
            {
                if (fpsController != null && fpsController.playerCamera != null)
                {
                    // Use the centralized activation method
                    ActivatePlayerCamera(fpsController, playerName);
                    Debug.Log($"[LobbyManager] Camera activated for local player: {playerName} (attempt {i + 1})");
                    yield break; // Success
                }
                
                // If camera is still null, try to setup again
                if (fpsController != null && fpsController.playerCamera == null)
                {
                    Debug.Log($"[LobbyManager] Camera still null, retrying SetupCamera (attempt {i + 1})");
                    fpsController.SendMessage("SetupCamera", SendMessageOptions.DontRequireReceiver);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.LogError($"[LobbyManager] ❌ Failed to activate camera for local player: {playerName} after 10 attempts!");
            
            // ✅ LAST RESORT: Try to find ANY camera in the scene and activate it
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != null && cam.name.Contains("Player") && !cam.name.Contains("Bootstrap"))
                {
                    cam.enabled = true;
                    cam.gameObject.SetActive(true);
                    cam.depth = 0;
                    if (!cam.CompareTag("MainCamera"))
                    {
                        cam.tag = "MainCamera";
                    }
                    Debug.LogWarning($"[LobbyManager] ⚠️ LAST RESORT: Activated camera: {cam.name}");
                    yield break;
                }
            }
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
            
            // ✅ NEW: LobbyUIController subscribes to OnGameStarting event, no direct call needed
            TacticalCombat.UI.LobbyUIController lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null)
            {
                // Event handler will handle this
            }
        }

        [TargetRpc]
        private void RpcShowError(NetworkConnection target, string errorMessage)
        {
            Debug.LogWarning($"[LobbyManager] Error: {errorMessage}");
            
            // ✅ NEW: Use LobbyUIController
            TacticalCombat.UI.LobbyUIController lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null)
            {
                lobbyController.ShowError(errorMessage);
            }
        }

        [TargetRpc]
        private void TargetSetLocalConnectionId(NetworkConnection target, uint connectionId)
        {
            // ✅ FIX: Set local connection ID on client (TargetRpc requires "Target" prefix!)
            localConnectionId = connectionId;
            Debug.Log($"[LobbyManager] ✅ TargetSetLocalConnectionId: Local connection ID set to: {connectionId}");

            // ✅ CRITICAL: Verify we can now find local player
            var localPlayer = GetLocalPlayer();
            if (localPlayer.HasValue)
            {
                Debug.Log($"[LobbyManager] ✅ Local player found after RPC: {localPlayer.Value.playerName}");
            }
            else
            {
                Debug.LogError($"[LobbyManager] ❌ Local player STILL not found after RPC! ConnectionID: {connectionId}");
            }
        }

        /// <summary>
        /// ✅ NEW: Show lobby full error to client before disconnecting
        /// </summary>
        [TargetRpc]
        private void RpcShowLobbyFullError(NetworkConnection target, string errorMessage)
        {
            Debug.LogWarning($"[LobbyManager] {errorMessage}");
            
            // Show error in UI - use LobbyUIController
            TacticalCombat.UI.LobbyUIController lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null)
            {
                lobbyController.ShowError(errorMessage);
            }
            else
            {
                Debug.LogError($"[LobbyManager] Error: {errorMessage} (LobbyUIController not found)");
            }
        }

        /// <summary>
        /// ✅ NEW: Disconnect player after showing error message
        /// </summary>
        private System.Collections.IEnumerator DisconnectPlayerAfterError(NetworkConnectionToClient conn)
        {
            // Wait for RPC to be sent (network delay)
            yield return new WaitForSeconds(0.5f);
            
            // Disconnect the player
            if (conn != null && conn.isReady)
            {
                conn.Disconnect();
                Debug.Log($"[LobbyManager] Player disconnected due to full lobby");
            }
        }

        #endregion

        #region SyncList Callbacks

        private void OnPlayersListChanged(SyncList<LobbyPlayerData>.Operation op, int index, LobbyPlayerData oldItem, LobbyPlayerData newItem)
        {
            // This runs on all clients when the players list changes
            Debug.Log($"[LobbyManager] 🔔 OnPlayersListChanged: op={op}, index={index}, isServer={isServer}, isClient={isClient}");
            Debug.Log($"[LobbyManager]   Old: {oldItem.playerName} (Ready: {oldItem.isReady})");
            Debug.Log($"[LobbyManager]   New: {newItem.playerName} (Ready: {newItem.isReady})");

            switch (op)
            {
                case SyncList<LobbyPlayerData>.Operation.OP_ADD:
                    Debug.Log($"[LobbyManager] ✅ Player added: {newItem.playerName}");
                    OnPlayerJoined?.Invoke(newItem);
                    break;

                case SyncList<LobbyPlayerData>.Operation.OP_REMOVEAT:
                    Debug.Log($"[LobbyManager] ✅ Player removed: {oldItem.playerName}");
                    OnPlayerLeft?.Invoke(oldItem);
                    break;

                case SyncList<LobbyPlayerData>.Operation.OP_SET:
                    Debug.Log($"[LobbyManager] ✅ Player updated: {newItem.playerName} (Ready: {oldItem.isReady} -> {newItem.isReady})");
                    Debug.Log($"[LobbyManager] 🔔 Invoking OnPlayerUpdated event (subscribers: {OnPlayerUpdated?.GetInvocationList()?.Length ?? 0})");
                    OnPlayerUpdated?.Invoke(newItem);
                    Debug.Log($"[LobbyManager] ✅ OnPlayerUpdated event invoked");
                    break;

                case SyncList<LobbyPlayerData>.Operation.OP_CLEAR:
                    Debug.Log($"[LobbyManager] ✅ Player list cleared");
                    break;
            }

            // ✅ CRITICAL FIX: LobbyUIController subscribes to events, so OnPlayerUpdated?.Invoke already triggered it
            // No need to manually refresh - event handlers will handle it
            TacticalCombat.UI.LobbyUIController lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null)
            {
                Debug.Log($"[LobbyManager] ✅ LobbyUIController found, event handlers should have been called for {op}");
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] ⚠️ LobbyUIController not found! Event may not be handled.");
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

        /// <summary>
        /// ✅ NEW: Get local connection ID (set by server via RPC)
        /// </summary>
        public uint GetLocalConnectionId()
        {
            return localConnectionId;
        }

        public LobbyPlayerData? GetLocalPlayer()
        {
            if (!NetworkClient.active)
            {
                Debug.LogWarning("[LobbyManager] GetLocalPlayer: NetworkClient not active!");
                return null;
            }

            // ✅ AAA FIX: Multiple fallback strategies to find local player

            // Strategy 1: Use cached localConnectionId (set by server via RPC)
            if (localConnectionId != 0)
            {
                foreach (var player in players)
                {
                    if (player.connectionId == localConnectionId)
                    {
                        Debug.Log($"[LobbyManager] GetLocalPlayer: Found via localConnectionId ({localConnectionId}): {player.playerName}");
                        return player;
                    }
                }
                Debug.LogWarning($"[LobbyManager] GetLocalPlayer: localConnectionId ({localConnectionId}) set but player not found in list!");
            }

            // Strategy 2: If we're hosting, find the host player
            if (NetworkServer.active)
            {
                foreach (var player in players)
                {
                    if (player.isHost)
                    {
                        Debug.Log($"[LobbyManager] GetLocalPlayer: Found host player: {player.playerName}");
                        return player;
                    }
                }

                // ✅ FIX: Fallback for host - if no host flag set, connection ID 0 is always host
                if (players.Count > 0)
                {
                    foreach (var player in players)
                    {
                        if (player.connectionId == 0)
                        {
                            Debug.LogWarning($"[LobbyManager] GetLocalPlayer: Using fallback - ConnectionID 0 is host: {player.playerName}");
                            return player;
                        }
                    }
                }

                Debug.LogWarning("[LobbyManager] GetLocalPlayer: We're server but no host player found!");
            }

            // Strategy 3: Try to get connection ID from NetworkClient
            // ✅ FIX: NetworkClient.connection is NetworkConnectionToServer (client-side), doesn't have connectionId
            // Client doesn't know its own connection ID directly - must be set by server via RPC
            // If we reach here, localConnectionId is 0 and we're not server, so we can't determine local player
            if (NetworkClient.connection != null && !NetworkServer.active)
            {
                Debug.LogWarning($"[LobbyManager] GetLocalPlayer: Client-side, but localConnectionId not set yet (RPC may not have arrived)");
                Debug.LogWarning($"[LobbyManager]   Waiting for RpcSetLocalConnectionId from server...");
            }

            Debug.LogError($"[LobbyManager] GetLocalPlayer: Could not find local player! Players in lobby: {players.Count}");
            for (int i = 0; i < players.Count; i++)
            {
                Debug.LogError($"  [{i}] {players[i].playerName} (ID: {players[i].connectionId}, Host: {players[i].isHost}, Ready: {players[i].isReady})");
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

