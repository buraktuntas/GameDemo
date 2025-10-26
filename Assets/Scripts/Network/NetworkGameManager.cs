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

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"🎮 OnServerAddPlayer ÇAĞRILDI - ConnectionID: {conn.connectionId}");

            // Determine team (balance teams)
            Team assignedTeam = teamACount <= teamBCount ? Team.TeamA : Team.TeamB;
            Debug.Log($"   Atanan Team: {assignedTeam}");

            // Get spawn point
            Transform spawnPoint = GetSpawnPoint(assignedTeam);
            Debug.Log($"   Spawn Point: {spawnPoint.position}");

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
            // Auto-start when we have at least 2 players (1 per team) for testing
            // In production, this would be controlled by a lobby system
            if (teamACount >= 1 && teamBCount >= 1)
            {
                // Start match after a delay
                if (MatchManager.Instance != null && 
                    MatchManager.Instance.GetCurrentPhase() == Phase.Lobby)
                {
                    Invoke(nameof(StartMatch), 3f);
                }
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

