using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Network
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Room data structure for lobby system
    /// Serializable for network sync
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        [Header("Room Identity")]
        public uint roomId;                // Unique room ID
        public string roomName;            // Display name
        public ulong hostPlayerId;        // Host player's netId
        
        [Header("Clan Teams")]
        public ClanData clanA;            // Left team (Clan A)
        public ClanData clanB;            // Right team (Clan B)
        
        [Header("Room Settings")]
        public int maxPlayers;           // Max players per team (default: 4)
        public int minPlayersToStart;     // Min players to start match (default: 4 = 2v2)
        public bool isMatchStarted;       // Match in progress
        public bool isPrivate;            // Private room (password protected)
        public string password;           // Room password (if private)
        
        [Header("Player Lists")]
        public List<RoomPlayer> players;   // All players in room
        
        [Header("Room State")]
        public float createdAt;           // Creation timestamp
        public float lastActivity;        // Last activity timestamp
        
        public RoomData()
        {
            players = new List<RoomPlayer>();
            maxPlayers = 4; // 4v4 max
            minPlayersToStart = 4; // 2v2 minimum
            isMatchStarted = false;
            isPrivate = false;
            createdAt = Time.time;
            lastActivity = Time.time;
        }
        
        /// <summary>
        /// Get players in Clan A
        /// </summary>
        public List<RoomPlayer> GetClanAPlayers()
        {
            return players.FindAll(p => p.clanId == (clanA?.clanId ?? ""));
        }
        
        /// <summary>
        /// Get players in Clan B
        /// </summary>
        public List<RoomPlayer> GetClanBPlayers()
        {
            return players.FindAll(p => p.clanId == (clanB?.clanId ?? ""));
        }
        
        /// <summary>
        /// Get total player count
        /// </summary>
        public int GetPlayerCount() => players?.Count ?? 0;
        
        /// <summary>
        /// Check if room is full
        /// </summary>
        public bool IsFull() => GetPlayerCount() >= (maxPlayers * 2); // 2 teams
        
        /// <summary>
        /// Check if room can start match
        /// </summary>
        public bool CanStartMatch()
        {
            if (isMatchStarted) return false;
            if (GetPlayerCount() < minPlayersToStart) return false;
            
            // Check if both teams have at least 1 player
            int clanACount = GetClanAPlayers().Count;
            int clanBCount = GetClanBPlayers().Count;
            
            return clanACount >= 1 && clanBCount >= 1;
        }
        
        /// <summary>
        /// Check if player is in room
        /// </summary>
        public bool IsPlayerInRoom(ulong playerId)
        {
            return players.Exists(p => p.playerId == playerId);
        }
        
        /// <summary>
        /// Get player by ID
        /// </summary>
        public RoomPlayer GetPlayer(ulong playerId)
        {
            return players.Find(p => p.playerId == playerId);
        }
    }
    
    /// <summary>
    /// ✅ CLAN SYSTEM: Player data in room
    /// </summary>
    [System.Serializable]
    public class RoomPlayer
    {
        public ulong playerId;           // NetworkIdentity.netId
        public string username;           // Display name
        public string clanId;             // Clan ID (null if no clan)
        public Team assignedTeam;         // Assigned team (TeamA or TeamB)
        public RoleId selectedRole;       // Selected role
        public bool isReady;              // Ready to start match
        
        public RoomPlayer()
        {
            assignedTeam = Team.None;
            selectedRole = RoleId.Builder;
            isReady = false;
        }
        
        public RoomPlayer(ulong id, string name) : this()
        {
            playerId = id;
            username = name;
        }
    }
}

