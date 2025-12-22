using UnityEngine;
using System.Collections.Generic;
using Mirror;
using TacticalCombat.Player;
using TacticalCombat.Building;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Manages player data, statistics, and budget synchronization.
    /// Extracted from MatchManager to organize data-heavy operations.
    /// </summary>
    public class MatchPlayerStore : MonoBehaviour
    {
        private MatchManager manager;
        private Dictionary<ulong, PlayerState> playerStates = new Dictionary<ulong, PlayerState>();
        private Dictionary<ulong, MatchStats> playerStats = new Dictionary<ulong, MatchStats>();

        public Dictionary<ulong, PlayerState> PlayerStates => playerStates;
        public Dictionary<ulong, MatchStats> PlayerStats => playerStats;

        private void Awake()
        {
            manager = GetComponent<MatchManager>();
        }

        [Server]
        public void RegisterPlayer(ulong netId, Team team, RoleId role)
        {
            if (playerStates.ContainsKey(netId))
            {
                var state = playerStates[netId];
                state.team = team;
                state.role = role;
                LogInfo($"[PlayerStore] Updated: {netId} -> Team {team}");
            }
            else
            {
                playerStates.Add(netId, new PlayerState(netId, team, role));
                playerStats.Add(netId, new MatchStats(netId));
                LogInfo($"[PlayerStore] Registered: {netId} -> Team {team}");
            }
            UpdatePlayerCounts();
        }

        [Server]
        public void UnregisterPlayer(ulong netId)
        {
            if (playerStates.Remove(netId))
            {
                playerStats.Remove(netId);
                LogInfo($"[PlayerStore] Unregistered: {netId}");
                UpdatePlayerCounts();
            }
        }

        [Server]
        private void UpdatePlayerCounts()
        {
            int teamA = 0, teamB = 0;
            foreach (var state in playerStates.Values)
            {
                if (state.team == Team.TeamA) teamA++;
                else if (state.team == Team.TeamB) teamB++;
            }
            manager.SetTeamCounts(teamA, teamB);
        }

        [Server]
        public void SyncAllStats()
        {
            foreach (var kvp in playerStats)
            {
                var s = kvp.Value;
                manager.RpcSendPlayerStats(kvp.Key, s.kills, s.deaths, s.assists, s.structuresBuilt, s.trapKills, s.captures, s.defenseTime, s.totalScore);
            }
        }

        [Server]
        public void SyncAllBudgets()
        {
            foreach (var kvp in playerStates)
            {
                var player = manager.FindPlayerByNetId(kvp.Key);
                if (player != null && player.TryGetComponent<SimpleBuildMode>(out var buildMode))
                {
                    buildMode.RpcUpdateBudget(kvp.Value.budget);
                }
            }
        }

        [Server]
        public void SpendBudget(ulong netId, StructureCategory category, int amount)
        {
            if (playerStates.TryGetValue(netId, out var state))
            {
                switch (category)
                {
                    case StructureCategory.Wall: state.budget.wallPoints -= amount; break;
                    case StructureCategory.Elevation: state.budget.elevationPoints -= amount; break;
                    case StructureCategory.Trap: state.budget.trapPoints -= amount; break;
                    case StructureCategory.Utility: state.budget.utilityPoints -= amount; break;
                }
                
                // Sync to individual client
                var player = manager.FindPlayerByNetId(netId);
                if (player != null && player.TryGetComponent<SimpleBuildMode>(out var buildMode))
                    buildMode.RpcUpdateBudget(state.budget);
            }
        }
    }
}
