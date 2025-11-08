using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using TacticalCombat.Core;

namespace TacticalCombat.Network
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Lobby manager for room-based matchmaking
    /// Extends Mirror NetworkManager with room system
    /// </summary>
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        
        [Header("Room Settings")]
        [SerializeField] private int maxRooms = 50;
        [SerializeField] private float roomTimeout = 300f; // 5 minutes
        
        // ✅ CRITICAL: SyncList for room list (syncs to all clients)
        public readonly SyncList<RoomData> roomList = new SyncList<RoomData>();
        
        // Server-only: Room management
        private Dictionary<uint, RoomData> rooms = new Dictionary<uint, RoomData>();
        private Dictionary<ulong, uint> playerToRoom = new Dictionary<ulong, uint>(); // Player ID -> Room ID
        private uint nextRoomId = 1;
        
        // Events
        public System.Action<RoomData> OnRoomCreated;
        public System.Action<uint> OnRoomDeleted;
        public System.Action<uint, ulong> OnPlayerJoinedRoom;
        public System.Action<uint, ulong> OnPlayerLeftRoom;
        public System.Action<uint> OnMatchStarted;
        
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
            rooms.Clear();
            playerToRoom.Clear();
            roomList.Clear();
            nextRoomId = 1;
            
            // ✅ FIX: Start room cleanup coroutine
            StartCoroutine(RoomCleanupCoroutine());
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("✅ [LobbyManager] Server started - Lobby system initialized");
            #endif
        }
        
        /// <summary>
        /// ✅ FIX: Cleanup empty/inactive rooms periodically
        /// </summary>
        [Server]
        private System.Collections.IEnumerator RoomCleanupCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f); // Check every 30 seconds
                CleanupInactiveRooms();
            }
        }
        
        /// <summary>
        /// ✅ FIX: Remove rooms that are empty or inactive for too long
        /// </summary>
        [Server]
        private void CleanupInactiveRooms()
        {
            if (!isServer) return;
            
            List<uint> roomsToDelete = new List<uint>();
            float currentTime = Time.time;
            
            foreach (var kvp in rooms)
            {
                RoomData room = kvp.Value;
                
                // Delete empty rooms
                if (room.GetPlayerCount() == 0)
                {
                    roomsToDelete.Add(kvp.Key);
                    continue;
                }
                
                // Delete inactive rooms (no activity for roomTimeout seconds)
                if (currentTime - room.lastActivity > roomTimeout && !room.isMatchStarted)
                {
                    roomsToDelete.Add(kvp.Key);
                    continue;
                }
            }
            
            // Delete rooms
            foreach (uint roomId in roomsToDelete)
            {
                if (rooms.TryGetValue(roomId, out RoomData room))
                {
                    // Remove all players from mapping
                    foreach (var player in room.players)
                    {
                        playerToRoom.Remove(player.playerId);
                    }
                    
                    // Remove room
                    rooms.Remove(roomId);
                    roomList.Remove(room);
                    
                    // Sync to all clients
                    RpcRoomDeleted(roomId);
                    
                    OnRoomDeleted?.Invoke(roomId);
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"🧹 [LobbyManager] Cleaned up inactive room: {room.roomName} (ID: {roomId})");
                    #endif
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ROOM CREATION & DELETION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Create a new room
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdCreateRoom(string roomName, string clanAId, string clanBId, bool isPrivate, string password, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            // Validate input
            if (string.IsNullOrEmpty(roomName))
            {
                TargetRoomOperationFailed(sender, "Room name cannot be empty");
                return;
            }
            
            if (roomName.Length < 3 || roomName.Length > 30)
            {
                TargetRoomOperationFailed(sender, "Room name must be 3-30 characters");
                return;
            }
            
            // Check server limit
            if (rooms.Count >= maxRooms)
            {
                TargetRoomOperationFailed(sender, "Server room limit reached");
                return;
            }
            
            // Get player ID
            ulong playerId = sender.identity.netId;
            
            // Check if player is already in a room
            if (playerToRoom.ContainsKey(playerId))
            {
                TargetRoomOperationFailed(sender, "You are already in a room");
                return;
            }
            
            // Get clan data if provided
            ClanData clanA = null;
            ClanData clanB = null;
            
            if (!string.IsNullOrEmpty(clanAId) && ClanManager.Instance != null)
            {
                clanA = ClanManager.Instance.GetClan(clanAId);
            }
            
            if (!string.IsNullOrEmpty(clanBId) && ClanManager.Instance != null)
            {
                clanB = ClanManager.Instance.GetClan(clanBId);
            }
            
            // Create room
            RoomData newRoom = new RoomData
            {
                roomId = nextRoomId++,
                roomName = roomName,
                hostPlayerId = playerId,
                clanA = clanA,
                clanB = clanB,
                isPrivate = isPrivate,
                password = password
            };
            
            // Add host to room
            RoomPlayer host = new RoomPlayer(playerId, GetPlayerUsername(playerId));
            if (clanA != null)
            {
                host.clanId = clanA.clanId;
                host.assignedTeam = Team.TeamA;
            }
            newRoom.players.Add(host);
            
            // Store room
            rooms[newRoom.roomId] = newRoom;
            playerToRoom[playerId] = newRoom.roomId;
            
            // Add to sync list
            roomList.Add(newRoom);
            
            // Sync to all clients
            RpcRoomCreated(newRoom);
            
            OnRoomCreated?.Invoke(newRoom);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [LobbyManager] Room created: {roomName} (ID: {newRoom.roomId}) by player {playerId}");
            #endif
            
            TargetRoomCreated(sender, newRoom);
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Delete a room (host only)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdDeleteRoom(uint roomId, ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!rooms.TryGetValue(roomId, out RoomData room))
            {
                TargetRoomOperationFailed(sender, "Room not found");
                return;
            }
            
            // Only host can delete
            if (room.hostPlayerId != playerId)
            {
                TargetRoomOperationFailed(sender, "Only room host can delete the room");
                return;
            }
            
            // Remove all players from mapping
            foreach (var player in room.players)
            {
                playerToRoom.Remove(player.playerId);
            }
            
            // Remove room
            rooms.Remove(roomId);
            roomList.Remove(room);
            
            // Sync to all clients
            RpcRoomDeleted(roomId);
            
            OnRoomDeleted?.Invoke(roomId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🗑️ [LobbyManager] Room deleted: {room.roomName} (ID: {roomId})");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // ROOM JOINING & LEAVING
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Join a room
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdJoinRoom(uint roomId, string password, ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!rooms.TryGetValue(roomId, out RoomData room))
            {
                TargetRoomOperationFailed(sender, "Room not found");
                return;
            }
            
            // Check if already in a room
            if (playerToRoom.ContainsKey(playerId))
            {
                TargetRoomOperationFailed(sender, "You are already in a room");
                return;
            }
            
            // Check if room is full
            if (room.IsFull())
            {
                TargetRoomOperationFailed(sender, "Room is full");
                return;
            }
            
            // Check if match already started
            if (room.isMatchStarted)
            {
                TargetRoomOperationFailed(sender, "Match has already started");
                return;
            }
            
            // Check password if private
            if (room.isPrivate && room.password != password)
            {
                TargetRoomOperationFailed(sender, "Incorrect password");
                return;
            }
            
            // Get player's clan
            string playerClanId = null;
            if (ClanManager.Instance != null)
            {
                playerClanId = ClanManager.Instance.GetPlayerClanId(playerId);
            }
            
            // Determine team assignment
            Team assignedTeam = Team.None;
            if (playerClanId != null)
            {
                // Auto-assign to clan's team
                if (room.clanA != null && room.clanA.clanId == playerClanId)
                {
                    assignedTeam = Team.TeamA;
                }
                else if (room.clanB != null && room.clanB.clanId == playerClanId)
                {
                    assignedTeam = Team.TeamB;
                }
            }
            
            // Auto-balance if no clan assignment
            if (assignedTeam == Team.None)
            {
                int clanACount = room.GetClanAPlayers().Count;
                int clanBCount = room.GetClanBPlayers().Count;
                assignedTeam = clanACount <= clanBCount ? Team.TeamA : Team.TeamB;
            }
            
            // Add player to room
            RoomPlayer newPlayer = new RoomPlayer(playerId, GetPlayerUsername(playerId))
            {
                clanId = playerClanId,
                assignedTeam = assignedTeam
            };
            room.players.Add(newPlayer);
            playerToRoom[playerId] = roomId;
            
            // ✅ FIX: Update last activity timestamp
            room.lastActivity = Time.time;

            // ✅ CRITICAL FIX: SyncList update requires Remove + Insert (not direct assignment)
            int index = roomList.IndexOf(room);
            if (index >= 0)
            {
                roomList.RemoveAt(index); // Remove old
                roomList.Insert(index, room); // Insert new = triggers sync to clients
            }
            
            // Sync to all clients
            RpcPlayerJoinedRoom(roomId, playerId, GetPlayerUsername(playerId));
            
            OnPlayerJoinedRoom?.Invoke(roomId, playerId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [LobbyManager] Player {playerId} joined room {room.roomName} (Team: {assignedTeam})");
            #endif
            
            TargetRoomJoined(sender, room);
        }
        
        /// <summary>
        /// ✅ SERVER-ONLY: Leave a room
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdLeaveRoom(ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!playerToRoom.TryGetValue(playerId, out uint roomId))
            {
                TargetRoomOperationFailed(sender, "You are not in a room");
                return;
            }
            
            if (!rooms.TryGetValue(roomId, out RoomData room))
            {
                TargetRoomOperationFailed(sender, "Room not found");
                return;
            }
            
            // Host leaving - delete room or transfer host
            if (room.hostPlayerId == playerId)
            {
                // Transfer host to next player or delete room
                if (room.players.Count > 1)
                {
                    var nextHost = room.players.FirstOrDefault(p => p.playerId != playerId);
                    if (nextHost != null)
                    {
                        room.hostPlayerId = nextHost.playerId;
                        RpcHostTransferred(roomId, nextHost.playerId);
                    }
                    else
                    {
                        // No other players - delete room
                        CmdDeleteRoom(roomId, playerId, sender);
                        return;
                    }
                }
                else
                {
                    // Only host in room - delete room
                    CmdDeleteRoom(roomId, playerId, sender);
                    return;
                }
            }
            
            // Remove player
            room.players.RemoveAll(p => p.playerId == playerId);
            playerToRoom.Remove(playerId);
            
            // ✅ FIX: Update last activity timestamp
            room.lastActivity = Time.time;

            // ✅ CRITICAL FIX: SyncList update requires Remove + Insert
            int index = roomList.IndexOf(room);
            if (index >= 0)
            {
                roomList.RemoveAt(index);
                roomList.Insert(index, room); // Triggers sync
            }
            
            // Sync to all clients
            RpcPlayerLeftRoom(roomId, playerId);
            
            OnPlayerLeftRoom?.Invoke(roomId, playerId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"👋 [LobbyManager] Player {playerId} left room {room.roomName}");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // MATCH STARTING
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ SERVER-ONLY: Start match (host only, when ready)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdStartMatch(uint roomId, ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            if (!rooms.TryGetValue(roomId, out RoomData room))
            {
                TargetRoomOperationFailed(sender, "Room not found");
                return;
            }
            
            // Only host can start
            if (room.hostPlayerId != playerId)
            {
                TargetRoomOperationFailed(sender, "Only room host can start the match");
                return;
            }
            
            // Check if can start
            if (!room.CanStartMatch())
            {
                TargetRoomOperationFailed(sender, "Not enough players to start match");
                return;
            }
            
            // Mark room as started
            room.isMatchStarted = true;

            // ✅ CRITICAL FIX: SyncList update requires Remove + Insert
            int index = roomList.IndexOf(room);
            if (index >= 0)
            {
                roomList.RemoveAt(index);
                roomList.Insert(index, room); // Triggers sync
            }
            
            // Sync to all clients
            RpcMatchStarted(roomId);
            
            OnMatchStarted?.Invoke(roomId);
            
            // ✅ INTEGRATION: Notify MatchManager to start match
            if (MatchManager.Instance != null)
            {
                // Register all players with MatchManager
                foreach (var player in room.players)
                {
                    Team team = player.assignedTeam;
                    if (team == Team.None)
                    {
                        // Auto-assign team
                        team = room.GetClanAPlayers().Contains(player) ? Team.TeamA : Team.TeamB;
                    }
                    
                    MatchManager.Instance.RegisterPlayer(player.playerId, team, player.selectedRole);
                }
                
                // Start match
                MatchManager.Instance.StartMatch();
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🎮 [LobbyManager] Match started in room {room.roomName} (ID: {roomId})");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Get player username (placeholder - should get from PlayerController)
        /// </summary>
        [Server]
        private string GetPlayerUsername(ulong playerId)
        {
            // TODO: Get from PlayerController or PlayerProfile
            return $"Player_{playerId}";
        }
        
        /// <summary>
        /// Get room by ID (works on client via SyncList)
        /// </summary>
        public RoomData GetRoom(uint roomId)
        {
            // ✅ FIX: Client can access via SyncList, server via dictionary
            if (isServer)
            {
                rooms.TryGetValue(roomId, out RoomData room);
                return room;
            }
            else
            {
                // Client: Search in SyncList
                return roomList.FirstOrDefault(r => r != null && r.roomId == roomId);
            }
        }
        
        /// <summary>
        /// Get player's room ID (server-only, client can search SyncList)
        /// </summary>
        public uint GetPlayerRoomId(ulong playerId)
        {
            if (isServer)
            {
                playerToRoom.TryGetValue(playerId, out uint roomId);
                return roomId;
            }
            else
            {
                // Client: Search in SyncList
                foreach (var room in roomList)
                {
                    if (room != null && room.IsPlayerInRoom(playerId))
                    {
                        return room.roomId;
                    }
                }
                return 0;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // NETWORK SYNC (RPCs)
        // ═══════════════════════════════════════════════════════════
        
        [ClientRpc]
        private void RpcRoomCreated(RoomData room)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Room created: {room.roomName}");
            #endif
            // Update UI room list
        }
        
        [ClientRpc]
        private void RpcRoomDeleted(uint roomId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Room deleted: {roomId}");
            #endif
            // Update UI room list
        }
        
        [ClientRpc]
        private void RpcPlayerJoinedRoom(uint roomId, ulong playerId, string username)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Player {username} joined room {roomId}");
            #endif
            // Update UI room details
        }
        
        [ClientRpc]
        private void RpcPlayerLeftRoom(uint roomId, ulong playerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Player {playerId} left room {roomId}");
            #endif
            // Update UI room details
        }
        
        [ClientRpc]
        private void RpcMatchStarted(uint roomId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Match started in room {roomId}");
            #endif
            // Transition to game scene
        }
        
        [ClientRpc]
        private void RpcHostTransferred(uint roomId, ulong newHostId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"📢 [LobbyManager CLIENT] Host transferred to {newHostId} in room {roomId}");
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // TARGET RPCS (Client-specific responses)
        // ═══════════════════════════════════════════════════════════
        
        [TargetRpc]
        private void TargetRoomCreated(NetworkConnection conn, RoomData room)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [LobbyManager] Room created successfully: {room.roomName}");
            #endif
            // Update UI - show room details
        }
        
        [TargetRpc]
        private void TargetRoomJoined(NetworkConnection conn, RoomData room)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [LobbyManager] Joined room: {room.roomName}");
            #endif
            // Update UI - show room details
        }
        
        [TargetRpc]
        private void TargetRoomOperationFailed(NetworkConnection conn, string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"❌ [LobbyManager] Operation failed: {reason}");
            #endif
            // Show error in UI
        }
    }
}

