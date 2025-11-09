using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Event-driven scoring system - hooks into game events and updates MatchStats
    /// </summary>
    public class ScoreManager : NetworkBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private MatchManager matchManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            matchManager = MatchManager.Instance;
            
            Debug.Log("[ScoreManager] Server started - Event-driven scoring active");
        }

        /// <summary>
        /// Award kill to player
        /// </summary>
        [Server]
        public void AwardKill(ulong killerId, ulong victimId)
        {
            var stats = matchManager?.GetPlayerMatchStats(killerId);
            if (stats != null)
            {
                stats.kills++;
                stats.CalculateTotalScore();
            }

            // Award death to victim
            var victimStats = matchManager?.GetPlayerMatchStats(victimId);
            if (victimStats != null)
            {
                victimStats.deaths++;
            }

            Debug.Log($"[ScoreManager] Kill: Player {killerId} killed Player {victimId}");
        }

        /// <summary>
        /// Award assist to player
        /// </summary>
        [Server]
        public void AwardAssist(ulong playerId)
        {
            var stats = matchManager?.GetPlayerMatchStats(playerId);
            if (stats != null)
            {
                stats.assists++;
                stats.CalculateTotalScore();
            }
        }

        /// <summary>
        /// Award structure built
        /// </summary>
        [Server]
        public void AwardStructureBuilt(ulong playerId)
        {
            var stats = matchManager?.GetPlayerMatchStats(playerId);
            if (stats != null)
            {
                stats.structuresBuilt++;
                stats.CalculateTotalScore();
            }
        }

        /// <summary>
        /// Award trap kill
        /// </summary>
        [Server]
        public void AwardTrapKill(ulong trapOwnerId, ulong victimId)
        {
            var stats = matchManager?.GetPlayerMatchStats(trapOwnerId);
            if (stats != null)
            {
                stats.trapKills++;
                stats.CalculateTotalScore();
            }

            // Award death to victim
            var victimStats = matchManager?.GetPlayerMatchStats(victimId);
            if (victimStats != null)
            {
                victimStats.deaths++;
            }

            Debug.Log($"[ScoreManager] Trap Kill: Trap owner {trapOwnerId} killed Player {victimId}");
        }

        /// <summary>
        /// Award core capture
        /// </summary>
        [Server]
        public void AwardCapture(ulong playerId)
        {
            var stats = matchManager?.GetPlayerMatchStats(playerId);
            if (stats != null)
            {
                stats.captures++;
                stats.CalculateTotalScore();
            }

            Debug.Log($"[ScoreManager] Capture: Player {playerId} captured core");
        }

        /// <summary>
        /// Update defense time (called periodically)
        /// </summary>
        [Server]
        public void UpdateDefenseTime(ulong playerId, float timeDelta)
        {
            var stats = matchManager?.GetPlayerMatchStats(playerId);
            if (stats != null)
            {
                stats.defenseTime += timeDelta;
                stats.CalculateTotalScore();
            }
        }

        /// <summary>
        /// Calculate end-game awards
        /// </summary>
        [Server]
        public Dictionary<ulong, AwardType> CalculateAwards()
        {
            var awards = new Dictionary<ulong, AwardType>();
            var matchState = matchManager?.GetMatchState();
            if (matchState == null) return awards;

            // Find player with most kills (Slayer)
            ulong slayerId = 0;
            int maxKills = 0;
            foreach (var kvp in matchState.playerStats)
            {
                if (kvp.Value.kills > maxKills)
                {
                    maxKills = kvp.Value.kills;
                    slayerId = kvp.Key;
                }
            }
            if (slayerId != 0) awards[slayerId] = AwardType.Slayer;

            // Find player with most structures (Architect)
            ulong architectId = 0;
            int maxStructures = 0;
            foreach (var kvp in matchState.playerStats)
            {
                if (kvp.Value.structuresBuilt > maxStructures)
                {
                    maxStructures = kvp.Value.structuresBuilt;
                    architectId = kvp.Key;
                }
            }
            if (architectId != 0) awards[architectId] = AwardType.Architect;

            // Find player with most defense time (Guardian)
            ulong guardianId = 0;
            float maxDefenseTime = 0f;
            foreach (var kvp in matchState.playerStats)
            {
                if (kvp.Value.defenseTime > maxDefenseTime)
                {
                    maxDefenseTime = kvp.Value.defenseTime;
                    guardianId = kvp.Key;
                }
            }
            if (guardianId != 0) awards[guardianId] = AwardType.Guardian;

            // Find player with most captures (Carrier)
            ulong carrierId = 0;
            int maxCaptures = 0;
            foreach (var kvp in matchState.playerStats)
            {
                if (kvp.Value.captures > maxCaptures)
                {
                    maxCaptures = kvp.Value.captures;
                    carrierId = kvp.Key;
                }
            }
            if (carrierId != 0) awards[carrierId] = AwardType.Carrier;

            // Find player with most trap kills (Saboteur)
            ulong saboteurId = 0;
            int maxTrapKills = 0;
            foreach (var kvp in matchState.playerStats)
            {
                if (kvp.Value.trapKills > maxTrapKills)
                {
                    maxTrapKills = kvp.Value.trapKills;
                    saboteurId = kvp.Key;
                }
            }
            if (saboteurId != 0) awards[saboteurId] = AwardType.Saboteur;

            return awards;
        }
    }
}

