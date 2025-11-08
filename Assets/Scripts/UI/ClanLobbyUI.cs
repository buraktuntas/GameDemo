using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Network;
using TacticalCombat.Core;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.UI
{
    /// <summary>
    /// ✅ CLAN SYSTEM: Lobby UI for room browser and room management
    /// Shows room list, allows creating/joining rooms
    /// </summary>
    public class ClanLobbyUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject roomDetailsPanel;
        
        [Header("Room List")]
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomEntryPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TextMeshProUGUI roomCountText;
        
        [Header("Create Room")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Toggle privateRoomToggle;
        [SerializeField] private Button createConfirmButton;
        [SerializeField] private Button createCancelButton;
        
        [Header("Room Details")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI roomStatusText;
        [SerializeField] private Transform teamAContent;
        [SerializeField] private Transform teamBContent;
        [SerializeField] private GameObject playerEntryPrefab;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button closeDetailsButton;
        
        [Header("Clan Info")]
        [SerializeField] private TextMeshProUGUI clanAInfoText;
        [SerializeField] private TextMeshProUGUI clanBInfoText;
        [SerializeField] private TextMeshProUGUI clanAXPText;
        [SerializeField] private TextMeshProUGUI clanBXPText;
        
        [Header("Settings")]
        [SerializeField] private float refreshInterval = 2f; // Refresh room list every 2 seconds
        
        // State
        private uint selectedRoomId = 0;
        private uint currentRoomId = 0;
        private Dictionary<uint, GameObject> roomEntries = new Dictionary<uint, GameObject>();
        private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();
        private float lastRefreshTime = 0f;
        
        private void Start()
        {
            // Hide panels initially
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (roomDetailsPanel != null) roomDetailsPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            
            // Setup button listeners
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(ShowCreateRoomPanel);
            
            if (createConfirmButton != null)
                createConfirmButton.onClick.AddListener(CreateRoom);
            
            if (createCancelButton != null)
                createCancelButton.onClick.AddListener(HideCreateRoomPanel);
            
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(JoinSelectedRoom);
            
            if (leaveRoomButton != null)
                leaveRoomButton.onClick.AddListener(LeaveCurrentRoom);
            
            if (startMatchButton != null)
                startMatchButton.onClick.AddListener(StartMatch);
            
            if (closeDetailsButton != null)
                closeDetailsButton.onClick.AddListener(CloseRoomDetails);
            
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshRoomList);
            
            // Subscribe to LobbyManager events
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.roomList.Callback += OnRoomListChanged;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.roomList.Callback -= OnRoomListChanged;
            }
        }
        
        private void Update()
        {
            // Auto-refresh room list periodically
            if (Time.time - lastRefreshTime > refreshInterval)
            {
                RefreshRoomList();
                lastRefreshTime = Time.time;
            }
            
            // Update room details if viewing a room
            if (selectedRoomId > 0 && roomDetailsPanel != null && roomDetailsPanel.activeSelf)
            {
                UpdateRoomDetails();
            }
            
            // Update start match button state
            if (startMatchButton != null && LobbyManager.Instance != null)
            {
                if (currentRoomId > 0)
                {
                    var room = LobbyManager.Instance.GetRoom(currentRoomId);
                    if (room != null)
                    {
                        bool canStart = room.CanStartMatch() && IsRoomHost();
                        startMatchButton.interactable = canStart;
                    }
                }
                else
                {
                    startMatchButton.interactable = false;
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ROOM LIST MANAGEMENT
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ FIX: Handle SyncList changes
        /// </summary>
        private void OnRoomListChanged(SyncList<RoomData>.Operation op, int index, RoomData oldItem, RoomData newItem)
        {
            RefreshRoomList();
        }
        
        /// <summary>
        /// Refresh room list display
        /// </summary>
        private void RefreshRoomList()
        {
            if (roomListContent == null || LobbyManager.Instance == null) return;
            
            // Clear existing entries
            foreach (var entry in roomEntries.Values)
            {
                if (entry != null) Destroy(entry);
            }
            roomEntries.Clear();
            
            // Update room count
            if (roomCountText != null)
            {
                int count = LobbyManager.Instance.roomList.Count;
                roomCountText.text = $"Rooms: {count}";
            }
            
            // Create entries for each room
            foreach (var room in LobbyManager.Instance.roomList)
            {
                if (room == null) continue;
                
                CreateRoomEntry(room);
            }
        }
        
        /// <summary>
        /// Create a room entry in the list
        /// </summary>
        private void CreateRoomEntry(RoomData room)
        {
            if (roomEntryPrefab == null)
            {
                // Create default entry if prefab not assigned
                CreateDefaultRoomEntry(room);
                return;
            }
            
            GameObject entry = Instantiate(roomEntryPrefab, roomListContent);
            roomEntries[room.roomId] = entry;
            
            // Setup entry UI
            var nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string roomInfo = $"{room.roomName}";
                if (room.isPrivate) roomInfo += " 🔒";
                if (room.isMatchStarted) roomInfo += " [IN MATCH]";
                roomInfo += $" ({room.GetPlayerCount()}/{room.maxPlayers * 2})";
                
                nameText.text = roomInfo;
            }
            
            // Setup click listener
            var button = entry.GetComponent<Button>();
            if (button != null)
            {
                uint roomId = room.roomId; // Capture for closure
                button.onClick.AddListener(() => SelectRoom(roomId));
            }
        }
        
        /// <summary>
        /// Create default room entry if prefab not assigned
        /// </summary>
        private void CreateDefaultRoomEntry(RoomData room)
        {
            GameObject entry = new GameObject($"RoomEntry_{room.roomId}");
            entry.transform.SetParent(roomListContent);
            
            // Add button
            var button = entry.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;
            
            // Add text
            var rectTransform = entry.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = entry.AddComponent<RectTransform>();
            
            rectTransform.sizeDelta = new Vector2(400, 40);
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(entry.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{room.roomName} ({room.GetPlayerCount()}/{room.maxPlayers * 2})";
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Left;
            
            // Setup click listener
            uint roomId = room.roomId;
            button.onClick.AddListener(() => SelectRoom(roomId));
            
            roomEntries[room.roomId] = entry;
        }
        
        /// <summary>
        /// Select a room and show details
        /// </summary>
        private void SelectRoom(uint roomId)
        {
            selectedRoomId = roomId;
            ShowRoomDetails(roomId);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ROOM DETAILS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Show room details panel
        /// </summary>
        private void ShowRoomDetails(uint roomId)
        {
            if (roomDetailsPanel == null || LobbyManager.Instance == null) return;
            
            var room = LobbyManager.Instance.GetRoom(roomId);
            if (room == null) return;
            
            roomDetailsPanel.SetActive(true);
            UpdateRoomDetails();
        }
        
        /// <summary>
        /// Update room details display
        /// </summary>
        private void UpdateRoomDetails()
        {
            if (LobbyManager.Instance == null || selectedRoomId == 0) return;
            
            var room = LobbyManager.Instance.GetRoom(selectedRoomId);
            if (room == null) return;
            
            // Update room name
            if (roomNameText != null)
            {
                roomNameText.text = room.roomName;
            }
            
            // Update status
            if (roomStatusText != null)
            {
                if (room.isMatchStarted)
                {
                    roomStatusText.text = "Match in Progress";
                    roomStatusText.color = Color.red;
                }
                else if (room.CanStartMatch())
                {
                    roomStatusText.text = "Ready to Start";
                    roomStatusText.color = Color.green;
                }
                else
                {
                    roomStatusText.text = $"Waiting for players ({room.GetPlayerCount()}/{room.minPlayersToStart})";
                    roomStatusText.color = Color.yellow;
                }
            }
            
            // Update clan info
            if (clanAInfoText != null)
            {
                if (room.clanA != null)
                {
                    clanAInfoText.text = $"{room.clanA.clanName} [{room.clanA.clanTag}]";
                }
                else
                {
                    clanAInfoText.text = "Team A";
                }
            }
            
            if (clanBInfoText != null)
            {
                if (room.clanB != null)
                {
                    clanBInfoText.text = $"{room.clanB.clanName} [{room.clanB.clanTag}]";
                }
                else
                {
                    clanBInfoText.text = "Team B";
                }
            }
            
            if (clanAXPText != null && room.clanA != null)
            {
                clanAXPText.text = $"Level {room.clanA.clanLevel} | {room.clanA.clanXP} XP";
            }
            
            if (clanBXPText != null && room.clanB != null)
            {
                clanBXPText.text = $"Level {room.clanB.clanLevel} | {room.clanB.clanXP} XP";
            }
            
            // Update player lists
            UpdatePlayerList(teamAContent, room.GetClanAPlayers());
            UpdatePlayerList(teamBContent, room.GetClanBPlayers());
            
            // Update buttons
            if (joinRoomButton != null)
            {
                joinRoomButton.interactable = !room.IsFull() && !room.isMatchStarted && currentRoomId == 0;
            }
            
            if (leaveRoomButton != null)
            {
                leaveRoomButton.interactable = currentRoomId == selectedRoomId;
            }
        }
        
        /// <summary>
        /// Update player list for a team
        /// </summary>
        private void UpdatePlayerList(Transform content, List<RoomPlayer> players)
        {
            if (content == null) return;
            
            // Clear existing entries
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
            
            // Create entries
            foreach (var player in players)
            {
                CreatePlayerEntry(content, player);
            }
        }
        
        /// <summary>
        /// Create a player entry
        /// </summary>
        private void CreatePlayerEntry(Transform parent, RoomPlayer player)
        {
            GameObject entry;
            
            if (playerEntryPrefab != null)
            {
                entry = Instantiate(playerEntryPrefab, parent);
            }
            else
            {
                // Create default entry
                entry = new GameObject($"PlayerEntry_{player.playerId}");
                entry.transform.SetParent(parent);
                
                var rectTransform = entry.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 30);
                
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(entry.transform);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                var text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = player.username;
                text.fontSize = 14;
            }
            
            // Update text
            var textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string playerInfo = player.username;
                if (player.isReady) playerInfo += " ✓";
                if (player.clanId != null) playerInfo += $" [{GetClanTag(player.clanId)}]";
                textComponent.text = playerInfo;
            }
        }
        
        /// <summary>
        /// Get clan tag by ID
        /// </summary>
        private string GetClanTag(string clanId)
        {
            if (ClanManager.Instance == null || string.IsNullOrEmpty(clanId)) return "";
            
            var clan = ClanManager.Instance.GetClan(clanId);
            return clan?.clanTag ?? "";
        }
        
        /// <summary>
        /// Close room details panel
        /// </summary>
        private void CloseRoomDetails()
        {
            if (roomDetailsPanel != null)
            {
                roomDetailsPanel.SetActive(false);
            }
            selectedRoomId = 0;
        }
        
        // ═══════════════════════════════════════════════════════════
        // ROOM ACTIONS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Show create room panel
        /// </summary>
        private void ShowCreateRoomPanel()
        {
            if (createRoomPanel != null)
            {
                createRoomPanel.SetActive(true);
            }
            
            // Clear inputs
            if (roomNameInput != null) roomNameInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            if (privateRoomToggle != null) privateRoomToggle.isOn = false;
        }
        
        /// <summary>
        /// Hide create room panel
        /// </summary>
        private void HideCreateRoomPanel()
        {
            if (createRoomPanel != null)
            {
                createRoomPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Create a new room
        /// </summary>
        private void CreateRoom()
        {
            if (LobbyManager.Instance == null) return;
            
            string roomName = roomNameInput != null ? roomNameInput.text : "New Room";
            string password = passwordInput != null ? passwordInput.text : "";
            bool isPrivate = privateRoomToggle != null && privateRoomToggle.isOn;
            
            // Get player's clan ID
            string clanAId = null;
            if (ClanManager.Instance != null)
            {
                // TODO: Get actual player ID from NetworkIdentity
                ulong playerId = GetLocalPlayerId();
                if (playerId > 0)
                {
                    clanAId = ClanManager.Instance.GetPlayerClanId(playerId);
                }
            }
            
            LobbyManager.Instance.CmdCreateRoom(roomName, clanAId, null, isPrivate, password);
            
            HideCreateRoomPanel();
        }
        
        /// <summary>
        /// Join selected room
        /// </summary>
        private void JoinSelectedRoom()
        {
            if (LobbyManager.Instance == null || selectedRoomId == 0) return;
            
            // TODO: Get password if room is private
            string password = "";
            
            ulong playerId = GetLocalPlayerId();
            if (playerId > 0)
            {
                LobbyManager.Instance.CmdJoinRoom(selectedRoomId, password, playerId);
                currentRoomId = selectedRoomId;
            }
        }
        
        /// <summary>
        /// Leave current room
        /// </summary>
        private void LeaveCurrentRoom()
        {
            if (LobbyManager.Instance == null || currentRoomId == 0) return;
            
            ulong playerId = GetLocalPlayerId();
            if (playerId > 0)
            {
                LobbyManager.Instance.CmdLeaveRoom(playerId);
                currentRoomId = 0;
            }
            
            CloseRoomDetails();
        }
        
        /// <summary>
        /// Start match (host only)
        /// </summary>
        private void StartMatch()
        {
            if (LobbyManager.Instance == null || currentRoomId == 0) return;
            
            ulong playerId = GetLocalPlayerId();
            if (playerId > 0)
            {
                LobbyManager.Instance.CmdStartMatch(currentRoomId, playerId);
            }
        }
        
        /// <summary>
        /// Get local player's network ID
        /// </summary>
        private ulong GetLocalPlayerId()
        {
            // Find local player's NetworkIdentity
            var localPlayer = FindFirstObjectByType<Mirror.NetworkIdentity>();
            if (localPlayer != null && localPlayer.isLocalPlayer)
            {
                return localPlayer.netId;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Check if local player is room host
        /// </summary>
        private bool IsRoomHost()
        {
            if (LobbyManager.Instance == null || currentRoomId == 0) return false;
            
            var room = LobbyManager.Instance.GetRoom(currentRoomId);
            if (room == null) return false;
            
            ulong playerId = GetLocalPlayerId();
            return room.hostPlayerId == playerId;
        }
    }
}

