using UnityEngine;

namespace TacticalCombat.Core
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Player statistics for progression
    /// </summary>
    [System.Serializable]
    public class PlayerStats
    {
        [Header("Combat Stats")]
        public int totalKills;
        public int totalDeaths;
        public int totalAssists;
        public int headshots;
        
        [Header("Match Stats")]
        public int matchesWon;
        public int matchesLost;
        public int matchesPlayed => matchesWon + matchesLost;
        public float winRate => matchesPlayed > 0 ? (float)matchesWon / matchesPlayed : 0f;
        
        [Header("Survival Stats")]
        public float totalSurvivalTime;    // Total seconds alive across all matches
        public float avgSurvivalTime;      // Average survival time per match
        public int longestSurvivalTime;    // Longest survival in seconds
        
        [Header("Building Stats")]
        public int structuresBuilt;
        public int trapsPlaced;
        public int structuresDestroyed;    // Enemy structures destroyed
        
        [Header("Objective Stats")]
        public int coresDestroyed;
        public int sabotagesCompleted;
        public int controlPointsCaptured;
        
        [Header("Performance Metrics")]
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalHealing;           // If healing system exists
        
        public PlayerStats()
        {
            // Initialize all stats to 0
        }
        
        /// <summary>
        /// Calculate total XP contribution from stats
        /// </summary>
        public int CalculateXPContribution()
        {
            int xp = 0;
            
            // Combat XP
            xp += totalKills * 10;         // 10 XP per kill
            xp += totalAssists * 5;        // 5 XP per assist
            xp += headshots * 15;          // 15 XP per headshot
            
            // Match XP
            xp += matchesWon * 100;        // 100 XP per win
            xp += matchesLost * 25;        // 25 XP per loss
            
            // Building XP
            xp += structuresBuilt * 2;     // 2 XP per structure
            xp += trapsPlaced * 3;         // 3 XP per trap
            
            // Objective XP
            xp += coresDestroyed * 50;     // 50 XP per core destroyed
            xp += sabotagesCompleted * 30;  // 30 XP per sabotage
            
            // Survival bonus
            xp += Mathf.RoundToInt(totalSurvivalTime / 60f); // 1 XP per minute survived
            
            return xp;
        }
        
        /// <summary>
        /// Update average survival time
        /// </summary>
        public void UpdateAverageSurvivalTime()
        {
            if (matchesPlayed > 0)
            {
                avgSurvivalTime = totalSurvivalTime / matchesPlayed;
            }
        }
        
        /// <summary>
        /// Reset stats (for new season or testing)
        /// </summary>
        public void Reset()
        {
            totalKills = 0;
            totalDeaths = 0;
            totalAssists = 0;
            headshots = 0;
            matchesWon = 0;
            matchesLost = 0;
            totalSurvivalTime = 0f;
            avgSurvivalTime = 0f;
            longestSurvivalTime = 0;
            structuresBuilt = 0;
            trapsPlaced = 0;
            structuresDestroyed = 0;
            coresDestroyed = 0;
            sabotagesCompleted = 0;
            controlPointsCaptured = 0;
            totalDamageDealt = 0;
            totalDamageTaken = 0;
            totalHealing = 0;
        }
    }
}

