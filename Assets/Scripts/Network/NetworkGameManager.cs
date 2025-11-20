using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Network
{
    /// <summary>
    /// Custom Network Manager for the game
    /// </summary>
    public class NetworkGameManager : NetworkManager
    {
        [Header("Game Settings")]
        // Use base class playerPrefab instead of hiding it
        [SerializeField] private Transform[] teamASpawnPoints;
        [SerializeField] private Transform[] teamBSpawnPoints;

        [Header("Lobby Settings")]
        [SerializeField] private GameObject lobbyManagerPrefab;
        
        private LobbyManager activeLobbyManager;

        private int teamACount = 0;
        private int teamBCount = 0;

        public override void OnStartServer()
        {
            base.OnStartServer();
            teamACount = 0;
            teamBCount = 0;
            
            // ✅ NEW: Instantiate lobby manager on server
            if (lobbyManagerPrefab != null)
            {
                var lobbyGO = Instantiate(lobbyManagerPrefab);
                NetworkServer.Spawn(lobbyGO);
                activeLobbyManager = lobbyGO.GetComponent<LobbyManager>();
                LogNetwork("Lobby Manager spawned on server");
            }
            else
            {
                LogWarning("lobbyManagerPrefab is NULL! Auto-start will be enabled.");
            }
            
            LogNetwork("Server started!");

            // ✅ FIX: Force networkAddress to empty for server (never bind to specific IP)
            if (!string.IsNullOrEmpty(networkAddress) && networkAddress != "0.0.0.0")
            {
                LogWarning($"Server networkAddress was '{networkAddress}' - forcing to empty (all interfaces)");
                networkAddress = ""; // Force bind to all interfaces
            }

            var kcpTransport = transport as kcp2k.KcpTransport;
            if (kcpTransport != null)
            {
                LogNetwork($"Server listening on 0.0.0.0:{kcpTransport.port} (Max: {maxConnections} connections)");
            }
            else
            {
                LogNetwork($"Port: {(transport != null ? "Unknown" : "No Transport")}, Max Connections: {maxConnections}");
            }

            // ✅ CRITICAL FIX: Help message for LAN play (only in development)
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                string localIPs = "";
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIPs += $"      - {ip}\n";
                    }
                }
                if (!string.IsNullOrEmpty(localIPs))
                {
                    LogNetwork($"LAN IP addresses:\n{localIPs}");
                }
            }
            catch { }
            #endif
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            teamACount = 0;
            teamBCount = 0;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            LogNetwork($"OnServerAddPlayer - ConnectionID: {conn.connectionId}");

            // ✅ NEW: Register player in lobby
            if (activeLobbyManager != null)
            {
                // Generate a default player name (you can customize this)
                string playerName = $"Player{conn.connectionId}";
                
                // You can also get player name from connection data if you set it up
                // For example, using NetworkAuthenticator or custom data
                
                activeLobbyManager.RegisterPlayer(conn, playerName);
            }

            // Determine team (balance teams)
            Team assignedTeam = teamACount <= teamBCount ? Team.TeamA : Team.TeamB;

            // Get spawn point
            Transform spawnPoint = GetSpawnPoint(assignedTeam);
            
            // ✅ CRITICAL FIX: Check spawn point before using it
            if (spawnPoint == null)
            {
                LogError("Spawn point is null! Cannot spawn player.");
                return;
            }

            // ✅ FIX: Ensure playerPrefab is valid
            if (playerPrefab == null)
            {
                LogError("Player prefab is null! Cannot spawn player.");
                return;
            }

            // Spawn player
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Assign team (role will be selected by player)
            var playerController = player.GetComponent<Player.PlayerController>();
            if (playerController != null)
            {
                playerController.team = assignedTeam;
                playerController.role = RoleId.Builder; // Default, can be changed in lobby
            }

            // ✅ CRITICAL: NetworkIdentity kontrolü
            var netIdentity = player.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                LogError("NetworkIdentity YOK! Player spawn edilemez! Player prefab'a NetworkIdentity component eklemelisiniz!");
                Destroy(player); // Cleanup
                return; // ✅ FIX: STOP execution if no NetworkIdentity!
            }

            NetworkServer.AddPlayerForConnection(conn, player);

            // Update team counts
            if (assignedTeam == Team.TeamA)
                teamACount++;
            else
                teamBCount++;

            LogNetwork($"Player spawned - Team: {assignedTeam}, Team A: {teamACount}, Team B: {teamBCount}");

            // ✅ FIX: Sync existing scene state to newly joined player
            SyncSceneStateToPlayer(conn);

            // ✅ CRITICAL FIX: Do NOT auto-start match if LobbyManager exists
            // Lobby system will handle game start when host clicks "Start Game"
            // Check both activeLobbyManager and LobbyManager.Instance (in case prefab wasn't assigned)
            // ✅ CRITICAL: Also check if MatchManager is in Lobby phase (prevents auto-start if match already started)
            bool lobbySystemActive = (activeLobbyManager != null) || (LobbyManager.Instance != null);
            bool isInLobbyPhase = false;
            
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                isInLobbyPhase = (currentPhase == Phase.Lobby);
            }
            
            // ✅ CRITICAL FIX: Hide player visuals immediately when spawned in lobby phase
            // This prevents the game world from being visible when lobby UI is shown
            if (lobbySystemActive || isInLobbyPhase)
            {
                var playerVisuals = player.GetComponent<Player.PlayerVisuals>();
                if (playerVisuals != null)
                {
                    Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.GetComponent<Camera>() == null && 
                            renderer.GetComponent<Canvas>() == null)
                        {
                            renderer.enabled = false;
                        }
                    }
                    LogNetwork("Player visuals hidden - in lobby phase");
                }
            }
            
            // ✅ CRITICAL FIX: NEVER auto-start when LobbyManager exists
            // GDD-compliant: Host must click "Start Game" in lobby
            if (lobbySystemActive)
            {
                LogNetwork("LobbyManager is active - auto-start DISABLED (GDD-compliant: Host must click 'Start Game')");
                return; // Exit early - no auto-start
            }
            
            // ✅ CRITICAL: Also check if match already started
            if (!isInLobbyPhase)
            {
                LogNetwork($"Match already started (phase: {MatchManager.Instance?.GetCurrentPhase()}) - skipping auto-start");
                return; // Exit early - match already running
            }
            
            // ✅ LEGACY MODE: Only auto-start if NO LobbyManager (backward compatibility)
            LogWarning("No LobbyManager found - using legacy auto-start mode");
            CheckAutoStart();
        }

        /// <summary>
        /// ✅ FIX: Sync all scene objects (structures, etc.) to newly joined player
        /// </summary>
        private void SyncSceneStateToPlayer(NetworkConnectionToClient conn)
        {
            if (!NetworkServer.active) return;

            // Find all structures (BuildableStructure components)
            var structures = FindObjectsByType<Building.Structure>(FindObjectsSortMode.None);

            LogNetwork($"Syncing {structures.Length} structures to new player (Conn: {conn.connectionId})");
            // Note: Structures with NetworkIdentity are automatically synced by Mirror
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when a client tries to connect to server
        /// </summary>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"✅ [NetworkGameManager SERVER] Client connecting! ConnectionID: {conn.connectionId}");
            Debug.Log($"   Remote Address: {conn.address}");
            Debug.Log($"   Total Connections: {NetworkServer.connections.Count}");
            Debug.Log("═══════════════════════════════════════");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"⚠️ [NetworkGameManager SERVER] Client disconnecting! ConnectionID: {conn.connectionId}");

            // ✅ NEW: Unregister player from lobby
            if (activeLobbyManager != null)
            {
                activeLobbyManager.UnregisterPlayer((uint)conn.connectionId); // ✅ FIX: Cast to uint
            }

            // ✅ FIX: Cleanup player state before disconnect
            if (conn.identity != null)
            {
                var player = conn.identity.GetComponent<Player.PlayerController>();
                if (player != null)
                {
                    // Update team counts
                    if (player.team == Team.TeamA)
                        teamACount--;
                    else if (player.team == Team.TeamB)
                        teamBCount--;

                    // ✅ CRITICAL FIX: Unregister from MatchManager (prevents crash)
                    if (MatchManager.Instance != null)
                    {
                        MatchManager.Instance.UnregisterPlayer(player.netId);
                        Debug.Log($"✅ [NetworkGameManager] Player {player.netId} unregistered from MatchManager");
                    }
                }
            }

            base.OnServerDisconnect(conn);
            Debug.Log($"✅ [NetworkGameManager] Client {conn.connectionId} fully disconnected");
        }

        // ═══════════════════════════════════════════════════════════
        // CLIENT CALLBACKS - Debug ve scene handling için
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ CRITICAL FIX: Called when client successfully connects to server
        /// </summary>
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ [NetworkGameManager CLIENT] Successfully connected to server!");
            Debug.Log($"   Network Address: {networkAddress}");
            Debug.Log($"   Is Client: {NetworkClient.isConnected}");
            Debug.Log($"   Is Server: {NetworkServer.active}");
            Debug.Log("═══════════════════════════════════════");
            
            // ✅ NEW: If we're the host, ensure we're registered in the lobby
            // This handles the case where StartHost() doesn't automatically call OnServerAddPlayer
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StartCoroutine(RegisterHostInLobby());
            }
        }
        
        /// <summary>
        /// ✅ NEW: Register host in lobby after a short delay (wait for LobbyManager to be ready)
        /// </summary>
        private System.Collections.IEnumerator RegisterHostInLobby()
        {
            // Wait for LobbyManager to be ready
            yield return new WaitForSeconds(0.5f);
            
            // Try to find LobbyManager
            LobbyManager lobbyManager = activeLobbyManager ?? LobbyManager.Instance;
            if (lobbyManager == null)
            {
                LogWarning("LobbyManager not found - cannot register host");
                yield break;
            }
            
            // Find host connection (connection with ID 0 or the first connection)
            NetworkConnectionToClient hostConnection = null;
            if (NetworkServer.connections.Count > 0)
            {
                // Host is typically the first connection (or connection with ID 0)
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn != null && conn.connectionId == 0)
                    {
                        hostConnection = conn;
                        break;
                    }
                }
                
                // If no connection with ID 0, use the first connection
                if (hostConnection == null)
                {
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        if (conn != null)
                        {
                            hostConnection = conn;
                            break;
                        }
                    }
                }
            }
            
            if (hostConnection != null)
            {
                // Check if host is already registered
                var allPlayers = lobbyManager.GetAllPlayers();
                bool alreadyRegistered = false;
                foreach (var player in allPlayers)
                {
                    if (player.connectionId == (uint)hostConnection.connectionId)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }
                
                if (!alreadyRegistered)
                {
                    // Register host with a default name
                    string hostName = System.Environment.UserName ?? "Host";
                    lobbyManager.RegisterPlayer(hostConnection, hostName);
                    LogNetwork($"✅ Host registered in lobby: {hostName} (ConnectionID: {hostConnection.connectionId})");
                }
                else
                {
                    LogNetwork("Host already registered in lobby");
                }
            }
            else
            {
                LogWarning("Could not find host connection - cannot register host in lobby");
            }
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when client disconnects from server
        /// </summary>
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.LogWarning("⚠️ [NetworkGameManager CLIENT] Disconnected from server");
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when client scene is changed (after server scene loads)
        /// This ensures player spawns correctly when client joins
        /// </summary>
        public override void OnClientSceneChanged()
        {
            base.OnClientSceneChanged();
            Debug.Log("✅ [NetworkGameManager CLIENT] Scene changed - player should spawn now");
            
            // Base implementation automatically calls OnServerAddPlayer if no player exists
            // This log helps verify the flow is working
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when client connection error occurs
        /// </summary>
        public override void OnClientError(TransportError error, string reason)
        {
            base.OnClientError(error, reason);
            Debug.LogError("═══════════════════════════════════════");
            Debug.LogError($"❌ [NetworkGameManager CLIENT] Connection error!");
            Debug.LogError($"   Error Type: {error}");
            Debug.LogError($"   Reason: {reason}");
            Debug.LogError($"   Network Address: {networkAddress}");
            
            var kcpTransport = transport as kcp2k.KcpTransport;
            if (kcpTransport != null)
            {
                Debug.LogError($"   Port: {kcpTransport.port}");
            }
            
            // ✅ CRITICAL FIX: Yaygın hatalar için çözüm önerileri
            if (reason.Contains("Connection refused") || reason.Contains("No connection"))
            {
                Debug.LogError("");
                Debug.LogError("🔧 ÇÖZÜM ÖNERİLERİ:");
                Debug.LogError("   1. Host PC'de firewall'ın port 7777'yi açtığından emin ol");
                Debug.LogError("   2. Host PC'de server'ın çalıştığından emin ol");
                Debug.LogError("   3. IP adresinin doğru olduğundan emin ol (192.168.1.110)");
                Debug.LogError("   4. Her iki PC'nin aynı ağda olduğundan emin ol");
                Debug.LogError("   5. Host PC'de NetworkManager'ın networkAddress'inin boş veya 0.0.0.0 olduğundan emin ol");
            }
            else if (reason.Contains("timeout") || reason.Contains("Timeout"))
            {
                Debug.LogError("");
                Debug.LogError("🔧 ÇÖZÜM ÖNERİLERİ:");
                Debug.LogError("   1. Firewall kontrolü yap");
                Debug.LogError("   2. Router'da port forwarding gerekebilir");
                Debug.LogError("   3. Antivirus yazılımı bağlantıyı engelliyor olabilir");
            }
            
            Debug.LogError("═══════════════════════════════════════");
        }

        private Transform GetSpawnPoint(Team team)
        {
            Transform[] spawnPoints = team == Team.TeamA ? teamASpawnPoints : teamBSpawnPoints;
            
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("No spawn points configured!");
                return transform;
            }

            int index = team == Team.TeamA ? (teamACount % spawnPoints.Length) : (teamBCount % spawnPoints.Length);
            return spawnPoints[index];
        }

        private void CheckAutoStart()
        {
            Debug.Log($"🎮 [LEGACY] CheckAutoStart: TeamA={teamACount}, TeamB={teamBCount}");

            // ✅ CRITICAL FIX: This method should NEVER run when LobbyManager exists
            // Double-check to prevent accidental auto-start
            bool lobbySystemActive = (activeLobbyManager != null) || (LobbyManager.Instance != null);
            
            if (lobbySystemActive)
            {
                Debug.LogError("❌ [NetworkGameManager] CheckAutoStart() called but LobbyManager exists! This should never happen!");
                Debug.LogError("   Aborting auto-start - lobby system should handle game start");
                return; // ABORT - lobby system should handle this
            }
            
            // ✅ CRITICAL: Check if MatchManager is in Lobby phase
            bool isInLobbyPhase = false;
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                isInLobbyPhase = (currentPhase == Phase.Lobby);
            }
            
            if (!isInLobbyPhase)
            {
                Debug.Log($"✅ [NetworkGameManager] Match already started (phase: {MatchManager.Instance?.GetCurrentPhase()}) - auto-start disabled");
                return; // Match already started, don't auto-start again
            }

            // ✅ LEGACY MODE: Only runs when NO LobbyManager exists (backward compatibility)
            Debug.LogWarning("⚠️ [LEGACY MODE] Auto-starting match (no lobby system detected)");
            
            // ✅ FIX: In editor/development, allow single-player testing
            // Production: Require at least 1 player per team
            bool canStart = false;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Development: Allow single player (for testing)
            canStart = (teamACount >= 1 || teamBCount >= 1);
            if (canStart)
            {
                Debug.Log($"✅ [DEV] Single-player mode enabled - starting match with {teamACount + teamBCount} player(s)");
            }
            #else
            // Production: Require at least 1 player per team
            canStart = (teamACount >= 1 && teamBCount >= 1);
            #endif

            if (canStart)
            {
                if (MatchManager.Instance == null)
                {
                    Debug.LogError("❌ MatchManager.Instance is NULL!");
                    return;
                }

                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                Debug.Log($"🎮 Current phase: {currentPhase}");

                if (currentPhase == Phase.Lobby)
                {
                    Debug.Log($"🎮 [LEGACY] Starting match in 3 seconds...");
                    Invoke(nameof(StartMatch), 3f);
                }
                else
                {
                    Debug.Log($"⚠️ Already started (phase: {currentPhase})");
                }
            }
            else
            {
                Debug.Log($"⚠️ Not enough players yet (need at least 1 per team in production)");
            }
        }

        private void StartMatch()
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.StartMatch();
            }
        }
    }
}

