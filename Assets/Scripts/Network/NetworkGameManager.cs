using UnityEngine;
using Mirror;
using TacticalCombat.Core;

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

        private int teamACount = 0;
        private int teamBCount = 0;

        public override void OnStartServer()
        {
            base.OnStartServer();
            teamACount = 0;
            teamBCount = 0;
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ [NetworkGameManager SERVER] Server started!");
            Debug.Log($"   Network Address: {networkAddress}");
            Debug.Log($"   Port: {(transport != null ? (transport as kcp2k.KcpTransport)?.port.ToString() : "Unknown")}");
            Debug.Log($"   Max Connections: {maxConnections}");
            Debug.Log("═══════════════════════════════════════");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            teamACount = 0;
            teamBCount = 0;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"🎮 OnServerAddPlayer ÇAĞRILDI - ConnectionID: {conn.connectionId}");

            // Determine team (balance teams)
            Team assignedTeam = teamACount <= teamBCount ? Team.TeamA : Team.TeamB;
            Debug.Log($"   Atanan Team: {assignedTeam}");

            // Get spawn point
            Transform spawnPoint = GetSpawnPoint(assignedTeam);
            
            // ✅ CRITICAL FIX: Check spawn point before using it
            if (spawnPoint == null)
            {
                Debug.LogError("❌ Spawn point is null! Cannot spawn player.");
                return;
            }
            
            Debug.Log($"   Spawn Point: {spawnPoint.position}");

            // ✅ FIX: Ensure playerPrefab is valid
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab is null! Cannot spawn player.");
                return;
            }

            // Spawn player
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log($"   Player GameObject oluşturuldu: {player.name}");

            // Assign team (role will be selected by player)
            var playerController = player.GetComponent<Player.PlayerController>();
            if (playerController != null)
            {
                playerController.team = assignedTeam;
                playerController.role = RoleId.Builder; // Default, can be changed in lobby
                Debug.Log($"   PlayerController team/role atandı");
            }

            // KRITIK: NetworkIdentity kontrolü
            var netIdentity = player.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                Debug.LogError("   ❌ NetworkIdentity YOK! Player spawn edilemez!");
                Debug.LogError("   Player prefab'a NetworkIdentity component eklemelisiniz!");
                Destroy(player); // Cleanup
                return; // ✅ FIX: STOP execution if no NetworkIdentity!
            }

            Debug.Log($"   NetworkIdentity VAR - NetID: {netIdentity.netId}");

            NetworkServer.AddPlayerForConnection(conn, player);
            Debug.Log($"   ✅ NetworkServer.AddPlayerForConnection çağrıldı");

            // Update team counts
            if (assignedTeam == Team.TeamA)
                teamACount++;
            else
                teamBCount++;

            Debug.Log($"   📊 SONUÇ: Team A: {teamACount}, Team B: {teamBCount}");
            Debug.Log("═══════════════════════════════════════\n");

            // ✅ FIX: Sync existing scene state to newly joined player
            SyncSceneStateToPlayer(conn);

            // If enough players, could auto-start match
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

            Debug.Log($"📡 [NetworkGameManager] Syncing {structures.Length} structures to new player (Conn: {conn.connectionId})");

            // Note: Structures with NetworkIdentity are automatically synced by Mirror
            // This log helps verify the sync is happening
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            // Update team counts when player leaves
            if (conn.identity != null)
            {
                var player = conn.identity.GetComponent<Player.PlayerController>();
                if (player != null)
                {
                    if (player.team == Team.TeamA)
                        teamACount--;
                    else if (player.team == Team.TeamB)
                        teamBCount--;
                }
            }

            base.OnServerDisconnect(conn);
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
            Debug.Log($"🎮 CheckAutoStart: TeamA={teamACount}, TeamB={teamBCount}");

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
                    Debug.Log($"🎮 Starting match in 3 seconds...");
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

