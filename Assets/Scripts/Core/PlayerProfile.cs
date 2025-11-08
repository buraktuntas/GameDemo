using UnityEngine;

namespace TacticalCombat.Core
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Extended PlayerState with clan support and progression
    /// Maintains backward compatibility with existing PlayerState
    /// </summary>
    [System.Serializable]
    public class PlayerProfile : PlayerState
    {
        [Header("Clan Information")]
        public string clanId;              // Current clan ID (null if no clan)
        public ClanRank clanRank;          // Rank in clan (if member)
        
        [Header("Player Progression")]
        public int playerXP;               // Individual XP (for personal unlocks)
        public int playerLevel;            // Individual level
        public PlayerStats stats;          // Detailed statistics
        
        [Header("Unlocks")]
        public System.Collections.Generic.List<string> unlockedWeaponSkins;
        public System.Collections.Generic.List<string> unlockedTraps;
        public System.Collections.Generic.List<string> unlockedStructures;
        public System.Collections.Generic.List<string> unlockedTitles;
        
        [Header("Player Identity")]
        public string username;            // Display name
        public string selectedTitle;       // Currently selected title (e.g., "Architect", "Slayer")
        public int avatarId;              // Avatar selection (for future use)
        
        public PlayerProfile() : base(0, Team.None, RoleId.Builder)
        {
            // Initialize with defaults
            stats = new PlayerStats();
            unlockedWeaponSkins = new System.Collections.Generic.List<string>();
            unlockedTraps = new System.Collections.Generic.List<string>();
            unlockedStructures = new System.Collections.Generic.List<string>();
            unlockedTitles = new System.Collections.Generic.List<string>();
            playerXP = 0;
            playerLevel = 1;
            clanRank = ClanRank.Member;
        }
        
        /// <summary>
        /// Create PlayerProfile from existing PlayerState (backward compatibility)
        /// </summary>
        public PlayerProfile(PlayerState state) : base(state.playerId, state.team, state.role)
        {
            // Copy existing state
            isAlive = state.isAlive;
            budget = state.budget;
            
            // Initialize new fields
            stats = new PlayerStats();
            unlockedWeaponSkins = new System.Collections.Generic.List<string>();
            unlockedTraps = new System.Collections.Generic.List<string>();
            unlockedStructures = new System.Collections.Generic.List<string>();
            unlockedTitles = new System.Collections.Generic.List<string>();
            playerXP = 0;
            playerLevel = 1;
            clanRank = ClanRank.Member;
            clanId = null; // No clan by default
        }
        
        /// <summary>
        /// Calculate player level from XP
        /// </summary>
        public void CalculateLevel()
        {
            int xp = playerXP;
            int level = 1;
            int requiredXP = 100;
            
            while (xp >= requiredXP && level < 100) // Max level 100
            {
                xp -= requiredXP;
                level++;
                requiredXP = Mathf.RoundToInt(requiredXP * 1.2f); // Slightly slower than clan level
            }
            
            playerLevel = level;
        }
        
        /// <summary>
        /// Add XP and update level
        /// </summary>
        public void AddXP(int amount)
        {
            playerXP += amount;
            CalculateLevel();
            
            // Check for level-up rewards
            CheckLevelUpRewards();
        }
        
        /// <summary>
        /// Check and unlock level-up rewards
        /// </summary>
        private void CheckLevelUpRewards()
        {
            // Level 5: Unlock weapon skin
            if (playerLevel >= 5 && !unlockedWeaponSkins.Contains("BasicSkin"))
            {
                unlockedWeaponSkins.Add("BasicSkin");
            }
            
            // Level 10: Unlock new trap
            if (playerLevel >= 10 && !unlockedTraps.Contains("AdvancedTrap"))
            {
                unlockedTraps.Add("AdvancedTrap");
            }
            
            // Level 15: Unlock new structure
            if (playerLevel >= 15 && !unlockedStructures.Contains("AdvancedStructure"))
            {
                unlockedStructures.Add("AdvancedStructure");
            }
            
            // Level 20: Unlock title
            if (playerLevel >= 20 && !unlockedTitles.Contains("Veteran"))
            {
                unlockedTitles.Add("Veteran");
            }
        }
        
        /// <summary>
        /// Check if player has unlocked item
        /// </summary>
        public bool HasUnlocked(string itemId, UnlockType type)
        {
            return type switch
            {
                UnlockType.WeaponSkin => unlockedWeaponSkins.Contains(itemId),
                UnlockType.Trap => unlockedTraps.Contains(itemId),
                UnlockType.Structure => unlockedStructures.Contains(itemId),
                UnlockType.Title => unlockedTitles.Contains(itemId),
                _ => false
            };
        }
        
        /// <summary>
        /// Update stats from match results
        /// </summary>
        public void UpdateStatsFromMatch(MatchResult result)
        {
            if (result == null) return;
            
            stats.totalKills += result.kills;
            stats.totalDeaths += result.deaths;
            stats.totalAssists += result.assists;
            stats.headshots += result.headshots;
            
            if (result.won)
            {
                stats.matchesWon++;
            }
            else
            {
                stats.matchesLost++;
            }
            
            stats.totalSurvivalTime += result.survivalTime;
            stats.structuresBuilt += result.structuresBuilt;
            stats.trapsPlaced += result.trapsPlaced;
            stats.structuresDestroyed += result.structuresDestroyed;
            stats.coresDestroyed += result.coresDestroyed;
            stats.totalDamageDealt += result.damageDealt;
            stats.totalDamageTaken += result.damageTaken;
            
            // Update longest survival
            if (result.survivalTime > stats.longestSurvivalTime)
            {
                stats.longestSurvivalTime = Mathf.RoundToInt(result.survivalTime);
            }
            
            // Update averages
            stats.UpdateAverageSurvivalTime();
        }
    }
    
    /// <summary>
    /// Match result data for stat updates
    /// </summary>
    [System.Serializable]
    public class MatchResult
    {
        public bool won;
        public int kills;
        public int deaths;
        public int assists;
        public int headshots;
        public float survivalTime;        // Seconds survived
        public int structuresBuilt;
        public int trapsPlaced;
        public int structuresDestroyed;
        public int coresDestroyed;
        public int damageDealt;
        public int damageTaken;
        public int xpGained;
        
        public MatchResult()
        {
            // Initialize defaults
        }
    }
    
    /// <summary>
    /// Unlock type enum
    /// </summary>
    public enum UnlockType
    {
        WeaponSkin,
        Trap,
        Structure,
        Title
    }
}

