using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TacticalCombat.Core
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Server-authoritative clan management
    /// Handles clan creation, deletion, member management, and XP
    /// </summary>
    public class ClanManager : NetworkBehaviour
    {
        public static ClanManager Instance { get; private set; }
        
        [Header("Clan Settings")]
        [SerializeField] private int maxClansPerServer = 100;
        [SerializeField] private int minClanNameLength = 3;
        [SerializeField] private int maxClanNameLength = 20;
        [SerializeField] private int minClanTagLength = 2;
        [SerializeField] private int maxClanTagLength = 6;
        
        // Server-only: Active clans dictionary
        private Dictionary<string, ClanData> activeClans = new Dictionary<string, ClanData>();
        
        // Server-only: Player to clan mapping
        private Dictionary<ulong, string> playerToClan = new Dictionary<ulong, string>();
        
        // Events
        public System.Action<ClanData> OnClanCreated;
        public System.Action<string> OnClanDeleted;
        public System.Action<ulong, string> OnPlayerJoinedClan;
        public System.Action<ulong, string> OnPlayerLeftClan;
        public System.Action<string, int> OnClanXPChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            activeClans.Clear();
            playerToClan.Clear();
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("✅ [ClanManager] Server started - Clan system initialized");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // CLAN CREATION & DELETION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Create a new clan
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdCreateClan(string clanName, string clanTag, ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            // Validate input
            if (string.IsNullOrEmpty(clanName) || string.IsNullOrEmpty(clanTag))
            {
                TargetClanCreationFailed(sender, "Clan name and tag cannot be empty");
                return;
            }
            
            if (clanName.Length < minClanNameLength || clanName.Length > maxClanNameLength)
            {
                TargetClanCreationFailed(sender, $"Clan name must be {minClanNameLength}-{maxClanNameLength} characters");
                return;
            }
            
            if (clanTag.Length < minClanTagLength || clanTag.Length > maxClanTagLength)
            {
                TargetClanCreationFailed(sender, $"Clan tag must be {minClanTagLength}-{maxClanTagLength} characters");
                return;
            }
            
            // Check if player already has a clan
            if (playerToClan.ContainsKey(playerId))
            {
                TargetClanCreationFailed(sender, "You are already in a clan");
                return;
            }
            
            // Check if clan name/tag already exists
            if (activeClans.Values.Any(c => c.clanName.Equals(clanName, StringComparison.OrdinalIgnoreCase)))
            {
                TargetClanCreationFailed(sender, "Clan name already exists");
                return;
            }
            
            if (activeClans.Values.Any(c => c.clanTag.Equals(clanTag, StringComparison.OrdinalIgnoreCase)))
            {
                TargetClanCreationFailed(sender, "Clan tag already exists");
                return;
            }
            
            // Check server limit
            if (activeClans.Count >= maxClansPerServer)
            {
                TargetClanCreationFailed(sender, "Server clan limit reached");
                return;
            }
            
            // Create clan
            ClanData newClan = new ClanData(clanName, clanTag.ToUpper(), playerId);
            activeClans[newClan.clanId] = newClan;
            playerToClan[playerId] = newClan.clanId;
            
            // Sync to all clients
            RpcClanCreated(newClan);
            
            OnClanCreated?.Invoke(newClan);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [ClanManager] Clan created: {clanName} ({clanTag}) by player {playerId}");
            #endif
            
            TargetClanCreationSuccess(sender, newClan);
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Delete a clan (leader only)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdDeleteClan(string clanId, ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                TargetClanOperationFailed(sender, "Clan not found");
                return;
            }
            
            // Only leader can delete
            if (clan.ownerId != playerId)
            {
                TargetClanOperationFailed(sender, "Only clan leader can delete the clan");
                return;
            }
            
            // Remove all members from mapping
            foreach (var member in clan.members)
            {
                playerToClan.Remove(member.playerId);
            }
            
            // Remove clan
            activeClans.Remove(clanId);
            
            // Sync to all clients
            RpcClanDeleted(clanId);
            
            OnClanDeleted?.Invoke(clanId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🗑️ [ClanManager] Clan deleted: {clan.clanName} ({clan.clanTag})");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // MEMBER MANAGEMENT
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Join a clan
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdJoinClan(string clanId, ulong playerId, string username, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                TargetClanOperationFailed(sender, "Clan not found");
                return;
            }
            
            // Check if already in a clan
            if (playerToClan.ContainsKey(playerId))
            {
                TargetClanOperationFailed(sender, "You are already in a clan");
                return;
            }
            
            // Check if clan is full
            if (clan.IsFull())
            {
                TargetClanOperationFailed(sender, "Clan is full");
                return;
            }
            
            // Add member
            ClanMember member = new ClanMember(playerId, username);
            clan.members.Add(member);
            playerToClan[playerId] = clanId;
            
            // Sync to all clients
            RpcPlayerJoinedClan(clanId, playerId, username);
            
            OnPlayerJoinedClan?.Invoke(playerId, clanId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [ClanManager] Player {username} ({playerId}) joined clan {clan.clanName}");
            #endif
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Leave a clan
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdLeaveClan(ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!playerToClan.TryGetValue(playerId, out string clanId))
            {
                TargetClanOperationFailed(sender, "You are not in a clan");
                return;
            }
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                TargetClanOperationFailed(sender, "Clan not found");
                return;
            }
            
            // Leader cannot leave (must delete clan or transfer leadership)
            if (clan.ownerId == playerId)
            {
                TargetClanOperationFailed(sender, "Clan leader cannot leave. Delete clan or transfer leadership first.");
                return;
            }
            
            // Remove member
            clan.members.RemoveAll(m => m.playerId == playerId);
            playerToClan.Remove(playerId);
            
            // Sync to all clients
            RpcPlayerLeftClan(clanId, playerId);
            
            OnPlayerLeftClan?.Invoke(playerId, clanId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"👋 [ClanManager] Player {playerId} left clan {clan.clanName}");
            #endif
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Kick a member (leader/officer only)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdKickMember(string clanId, ulong kickerId, ulong targetPlayerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                TargetClanOperationFailed(sender, "Clan not found");
                return;
            }
            
            // Check permissions
            var kicker = clan.GetMember(kickerId);
            if (kicker == null || (kicker.rank != ClanRank.Leader && kicker.rank != ClanRank.Officer))
            {
                TargetClanOperationFailed(sender, "You don't have permission to kick members");
                return;
            }
            
            // Cannot kick leader
            if (targetPlayerId == clan.ownerId)
            {
                TargetClanOperationFailed(sender, "Cannot kick clan leader");
                return;
            }
            
            // Remove member
            clan.members.RemoveAll(m => m.playerId == targetPlayerId);
            playerToClan.Remove(targetPlayerId);
            
            // Sync to all clients
            RpcPlayerLeftClan(clanId, targetPlayerId);
            
            OnPlayerLeftClan?.Invoke(targetPlayerId, clanId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"👢 [ClanManager] Player {targetPlayerId} kicked from clan {clan.clanName} by {kickerId}");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // XP & PROGRESSION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Award XP to clan (called from MatchManager after match end)
        /// </summary>
        [Server]
        public void AwardClanXP(string clanId, int xp)
        {
            if (!isServer) return;
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [ClanManager] Cannot award XP - Clan {clanId} not found");
                #endif
                return;
            }
            
            // Validate XP amount (anti-exploit)
            if (xp < 0 || xp > 1000) // Max 1000 XP per match
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [ClanManager] Invalid XP amount: {xp} (max 1000)");
                #endif
                return;
            }
            
            // Award XP
            int oldLevel = clan.clanLevel;
            clan.clanXP += xp;
            clan.CalculateLevel();
            
            // Sync to all clients
            RpcClanXPChanged(clanId, clan.clanXP, clan.clanLevel);
            
            OnClanXPChanged?.Invoke(clanId, clan.clanXP);
            
            // Check for level up
            if (clan.clanLevel > oldLevel)
            {
                RpcClanLevelUp(clanId, clan.clanLevel);
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📈 [ClanManager] Clan {clan.clanName} gained {xp} XP (Total: {clan.clanXP}, Level: {clan.clanLevel})");
            #endif
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Update clan win/loss stats
        /// </summary>
        [Server]
        public void UpdateClanMatchResult(string clanId, bool won)
        {
            if (!isServer) return;
            
            if (!activeClans.TryGetValue(clanId, out ClanData clan))
            {
                return;
            }
            
            if (won)
            {
                clan.wins++;
                clan.winStreak++;
            }
            else
            {
                clan.losses++;
                clan.winStreak = 0; // Reset streak on loss
            }
            
            // Sync to all clients
            RpcClanStatsUpdated(clanId, clan.wins, clan.losses, clan.winStreak);
        }
        
        // ═══════════════════════════════════════════════════════════
        // QUERY METHODS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Get clan by ID (server-only)
        /// </summary>
        [Server]
        public ClanData GetClan(string clanId)
        {
            activeClans.TryGetValue(clanId, out ClanData clan);
            return clan;
        }
        
        /// <summary>
        /// Get player's clan ID (server-only)
        /// </summary>
        [Server]
        public string GetPlayerClanId(ulong playerId)
        {
            playerToClan.TryGetValue(playerId, out string clanId);
            return clanId;
        }
        
        /// <summary>
        /// Get all clans (server-only)
        /// </summary>
        [Server]
        public List<ClanData> GetAllClans()
        {
            return activeClans.Values.ToList();
        }
        
        /// <summary>
        /// Check if player is in a clan (server-only)
        /// </summary>
        [Server]
        public bool IsPlayerInClan(ulong playerId)
        {
            return playerToClan.ContainsKey(playerId);
        }
        
        // ═══════════════════════════════════════════════════════════
        // NETWORK SYNC (RPCs)
        // ═══════════════════════════════════════════════════════════
        
        [ClientRpc]
        private void RpcClanCreated(ClanData clan)
        {
            // Clients can cache clan data if needed
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [ClanManager CLIENT] Clan created: {clan.clanName} ({clan.clanTag})");
            #endif
        }
        
        [ClientRpc]
        private void RpcClanDeleted(string clanId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [ClanManager CLIENT] Clan deleted: {clanId}");
            #endif
        }
        
        [ClientRpc]
        private void RpcPlayerJoinedClan(string clanId, ulong playerId, string username)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [ClanManager CLIENT] Player {username} joined clan {clanId}");
            #endif
        }
        
        [ClientRpc]
        private void RpcPlayerLeftClan(string clanId, ulong playerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [ClanManager CLIENT] Player {playerId} left clan {clanId}");
            #endif
        }
        
        [ClientRpc]
        private void RpcClanXPChanged(string clanId, int newXP, int newLevel)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [ClanManager CLIENT] Clan {clanId} XP: {newXP}, Level: {newLevel}");
            #endif
        }
        
        [ClientRpc]
        private void RpcClanLevelUp(string clanId, int newLevel)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🎉 [ClanManager CLIENT] Clan {clanId} leveled up to {newLevel}!");
            #endif
        }
        
        [ClientRpc]
        private void RpcClanStatsUpdated(string clanId, int wins, int losses, int winStreak)
        {
            // Update UI if needed
        }
        
        // ═══════════════════════════════════════════════════════════
        // TARGET RPCS (Client-specific responses)
        // ═══════════════════════════════════════════════════════════
        
        [TargetRpc]
        private void TargetClanCreationSuccess(NetworkConnection conn, ClanData clan)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [ClanManager] Clan created successfully: {clan.clanName}");
            #endif
            // Update UI
        }
        
        [TargetRpc]
        private void TargetClanCreationFailed(NetworkConnection conn, string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"❌ [ClanManager] Clan creation failed: {reason}");
            #endif
            // Show error in UI
        }
        
        [TargetRpc]
        private void TargetClanOperationFailed(NetworkConnection conn, string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"❌ [ClanManager] Operation failed: {reason}");
            #endif
            // Show error in UI
        }
    }
}

