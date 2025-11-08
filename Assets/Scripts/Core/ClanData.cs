using UnityEngine;
using System;
using System.Collections.Generic;

namespace TacticalCombat.Core
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Serializable clan data structure
    /// Used for network sync and persistence
    /// </summary>
    [System.Serializable]
    public class ClanData
    {
        [Header("Clan Identity")]
        public string clanId;              // Unique ID (GUID or database ID)
        public string clanName;            // Display name (e.g., "Shadow Warriors")
        public string clanTag;             // Short tag (e.g., "SHAD")
        public Color clanColor;            // Team color for UI
        
        [Header("Clan Progression")]
        public int clanXP;                 // Total XP accumulated
        public int clanLevel;              // Calculated from XP
        public int wins;                   // Match wins
        public int losses;                 // Match losses
        public int winStreak;              // Current win streak
        
        [Header("Clan Management")]
        public ulong ownerId;              // Clan leader's player ID
        public List<ClanMember> members;  // Member list
        public DateTime createdAt;         // Creation timestamp
        public int maxMembers;             // Max members (default: 50)
        
        [Header("Clan Customization")]
        public string logoUrl;             // URL to clan logo (for future use)
        public string bannerFrame;         // Banner frame ID (for future use)
        
        public ClanData()
        {
            clanId = System.Guid.NewGuid().ToString();
            members = new List<ClanMember>();
            createdAt = DateTime.UtcNow;
            maxMembers = 50;
            clanColor = Color.white;
            winStreak = 0;
        }
        
        public ClanData(string name, string tag, ulong ownerId) : this()
        {
            clanName = name;
            clanTag = tag;
            this.ownerId = ownerId;
            
            // Add owner as leader
            members.Add(new ClanMember
            {
                playerId = ownerId,
                rank = ClanRank.Leader,
                joinedAt = DateTime.UtcNow
            });
        }
        
        /// <summary>
        /// Calculate clan level from XP (exponential curve)
        /// Level 1: 0-100 XP
        /// Level 2: 101-250 XP
        /// Level 3: 251-500 XP
        /// etc.
        /// </summary>
        public void CalculateLevel()
        {
            int xp = clanXP;
            int level = 1;
            int requiredXP = 100;
            
            while (xp >= requiredXP && level < 100) // Max level 100
            {
                xp -= requiredXP;
                level++;
                requiredXP = Mathf.RoundToInt(requiredXP * 1.5f); // Exponential growth
            }
            
            clanLevel = level;
        }
        
        /// <summary>
        /// Get member count
        /// </summary>
        public int GetMemberCount() => members?.Count ?? 0;
        
        /// <summary>
        /// Check if clan is full
        /// </summary>
        public bool IsFull() => GetMemberCount() >= maxMembers;
        
        /// <summary>
        /// Find member by player ID
        /// </summary>
        public ClanMember GetMember(ulong playerId)
        {
            return members?.Find(m => m.playerId == playerId);
        }
        
        /// <summary>
        /// Check if player is member
        /// </summary>
        public bool IsMember(ulong playerId)
        {
            return GetMember(playerId) != null;
        }
    }
    
    /// <summary>
    /// ✅ CLAN SYSTEM: Clan member data
    /// </summary>
    [System.Serializable]
    public class ClanMember
    {
        public ulong playerId;            // NetworkIdentity.netId
        public string username;            // Display name
        public ClanRank rank;              // Member rank
        public int contributionXP;         // Individual contribution to clan XP
        public DateTime joinedAt;          // Join timestamp
        public DateTime lastActiveAt;      // Last active timestamp
        
        public ClanMember()
        {
            joinedAt = DateTime.UtcNow;
            lastActiveAt = DateTime.UtcNow;
            rank = ClanRank.Member;
        }
        
        public ClanMember(ulong id, string name) : this()
        {
            playerId = id;
            username = name;
        }
    }
}

