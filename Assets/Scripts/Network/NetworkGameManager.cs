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
            // Determine team (balance teams)
            Team assignedTeam = teamACount <= teamBCount ? Team.TeamA : Team.TeamB;
            
            // Get spawn point
            Transform spawnPoint = GetSpawnPoint(assignedTeam);

            // Spawn player
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Assign team (role will be selected by player)
            var playerController = player.GetComponent<Player.PlayerController>();
            if (playerController != null)
            {
                playerController.team = assignedTeam;
                playerController.role = RoleId.Builder; // Default, can be changed in lobby
            }

            NetworkServer.AddPlayerForConnection(conn, player);

            // Update team counts
            if (assignedTeam == Team.TeamA)
                teamACount++;
            else
                teamBCount++;

            Debug.Log($"Player spawned for {assignedTeam}. Team A: {teamACount}, Team B: {teamBCount}");

            // If enough players, could auto-start match
            CheckAutoStart();
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

