using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Network;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Lobby UI Controller - Manages all lobby UI elements
    /// Shows player list, ready states, team assignments, and controls
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gameStartingPanel;
        [SerializeField] private GameObject errorPanel;

        [Header("Player List")]
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;

        [Header("Lobby Info")]
        [SerializeField] private TextMeshProUGUI lobbyTitleText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI roomCodeText; // Optional for Steam/LAN codes

        [Header("Host Controls")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button autoBalanceButton;
        [SerializeField] private TextMeshProUGUI startGameButtonText;
        
        [Header("Game Mode Selection (Integrated)")]
        [SerializeField] private GameObject gameModeSelectionPanel;
        [SerializeField] private Button individualModeButton;
        [SerializeField] private Button teamModeButton;
        [SerializeField] private TextMeshProUGUI gameModeText;

        [Header("Player Controls")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private Button teamAButton;
        [SerializeField] private Button teamBButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private GameObject teamSelectionPanel; // ✅ NEW: Panel for team selection (only shown in team mode)

        [Header("Waiting State")]
        [SerializeField] private GameObject waitingForHostPanel;
        [SerializeField] private TextMeshProUGUI waitingText;

        [Header("Error Display")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private float errorDisplayDuration = 3f;

        [Header("Colors")]
        [SerializeField] private Color teamAColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color teamBColor = new Color(1f, 0.4f, 0.2f);
        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color notReadyColor = new Color(0.8f, 0.2f, 0.2f);

        // Internal state
        private LobbyManager lobbyManager;
        private List<GameObject> playerListItems = new List<GameObject>();
        private bool isLocalPlayerReady = false;
        private int localPlayerTeam = -1;
        
        // ✅ PERFORMANCE FIX: Cache components and throttle updates
        private Canvas cachedCanvas;
        private TextMeshProUGUI cachedTeamAText;
        private TextMeshProUGUI cachedTeamBText;
        private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.2f; // Update every 200ms instead of every frame
        private int lastTeamACount = -1;
        private int lastTeamBCount = -1;

        private void Start()
        {
            // ✅ CRITICAL: Hide panel FIRST before doing anything else
            HidePanel();

            // Setup buttons (can be done without LobbyManager)
            SetupButtons();

            // ✅ CRITICAL: Ensure MainMenu is visible at start
            EnsureMainMenuVisible();
            
            // ✅ FIX: Don't start coroutine here - GameObject might be inactive
            // Coroutine will be started in ShowPanel() when GameObject is active
        }
        
        /// <summary>
        /// ✅ NEW: Wait for LobbyManager to be created by NetworkManager
        /// </summary>
        private System.Collections.IEnumerator WaitForLobbyManager()
        {
            // Wait for LobbyManager to be spawned
            int maxWaitTime = 50; // 5 seconds max wait
            int waitCount = 0;
            
            while (lobbyManager == null && waitCount < maxWaitTime)
            {
                lobbyManager = LobbyManager.Instance;
                if (lobbyManager == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitCount++;
                }
            }
            
            if (lobbyManager == null)
            {
                Debug.LogWarning("[LobbyUI] LobbyManager not found after waiting. It will be created when network starts.");
                // Don't return - we'll try again when ShowPanel is called
            }
            else
            {
                Debug.Log("[LobbyUI] LobbyManager found!");
                
                // Subscribe to events
                lobbyManager.OnPlayerJoined += OnPlayerJoined;
                lobbyManager.OnPlayerLeft += OnPlayerLeft;
                lobbyManager.OnPlayerUpdated += OnPlayerUpdated;
                lobbyManager.OnGameStarting += OnGameStarting;
            }
        }

        /// <summary>
        /// ✅ NEW: Ensure MainMenu is visible when LobbyUI starts (if not connected)
        /// </summary>
        private void EnsureMainMenuVisible()
        {
            // Only show MainMenu if we're not connected yet
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                var mainMenu = FindFirstObjectByType<MainMenu>();
                if (mainMenu != null)
                {
                    var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                    if (mainMenuPanel != null && !mainMenuPanel.gameObject.activeSelf)
                    {
                        mainMenuPanel.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnPlayerJoined -= OnPlayerJoined;
                lobbyManager.OnPlayerLeft -= OnPlayerLeft;
                lobbyManager.OnPlayerUpdated -= OnPlayerUpdated;
                lobbyManager.OnGameStarting -= OnGameStarting;
            }
        }

        private void Update()
        {
            // ✅ PERFORMANCE FIX: Throttle UI updates (every 200ms instead of every frame)
            if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
            {
                UpdatePlayerCount();
                UpdateHostControls();
                lastUpdateTime = Time.time;
            }
        }

        #region Panel Management

        /// <summary>
        /// ✅ SIMPLE: Show the entire lobby UI (with game mode selection integrated)
        /// </summary>
        public void ShowPanel()
        {
            LogUI("LobbyUI ShowPanel called");
            
            // ✅ STEP 1: Configure Canvas (full screen)
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("LobbyCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    transform.SetParent(canvas.transform, false);
                }
            }
            
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }
            
            // ✅ STEP 2: Configure root GameObject (full screen)
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.sizeDelta = Vector2.zero;
                rootRect.anchoredPosition = Vector2.zero;
            }
            
            // ✅ STEP 3: Show lobby panel (full screen)
            if (lobbyPanel == null)
            {
                Transform lobbyPanelTransform = transform.Find("LobbyPanel");
                if (lobbyPanelTransform != null)
                {
                    lobbyPanel = lobbyPanelTransform.gameObject;
                }
                else
                {
                    // Use GameObject itself as panel
                    lobbyPanel = gameObject;
                }
            }
            
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
                
                // Configure lobby panel to be full screen
                RectTransform panelRect = lobbyPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = Vector2.zero;
                    panelRect.anchorMax = Vector2.one;
                    panelRect.sizeDelta = Vector2.zero;
                    panelRect.anchoredPosition = Vector2.zero;
                }
                
                // Add background if not exists
                Image panelBg = lobbyPanel.GetComponent<Image>();
                if (panelBg == null)
                {
                    panelBg = lobbyPanel.AddComponent<Image>();
                }
                panelBg.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-gray, fully opaque
            }
            
            // ✅ STEP 4: Show game mode selection panel
            if (gameModeSelectionPanel != null)
            {
                gameModeSelectionPanel.SetActive(true);
            }
            
            // ✅ STEP 5: Start network FIRST (this creates LobbyManager), then wait for it
            StartCoroutine(StartNetworkAndWaitForLobbyManager());
            
            // ✅ STEP 6: Hide other UIs (AGGRESSIVE - hide MainMenu completely)
            HideOtherUIs();
            
            // ✅ STEP 7: Hide game world (players, weapons - but keep cameras)
            HideGameWorld();
            
            // ✅ STEP 8: Force cursor unlock
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // ✅ STEP 9: Update UI (will be updated again when LobbyManager is ready)
            UpdateUI();
            
            LogUI("LobbyUI ShowPanel completed - waiting for LobbyManager...");
        }
        
        /// <summary>
        /// ✅ NEW: Start network and wait for LobbyManager, then update UI
        /// </summary>
        private System.Collections.IEnumerator StartNetworkAndWaitForLobbyManager()
        {
            // Step 1: Start network if not already started (for host)
            if (!NetworkServer.active && !NetworkClient.isConnected)
            {
                NetworkManager networkManager = NetworkManager.singleton;
                if (networkManager != null)
                {
                    // Configure network address for host
                    string currentAddress = networkManager.networkAddress;
                    if (currentAddress == "localhost" || currentAddress == "127.0.0.1")
                    {
                        networkManager.networkAddress = "";
                    }
                    
                    // Start host (this will create LobbyManager)
                    networkManager.StartHost();
                    LogUI("Network started (Host mode) - waiting for LobbyManager...");
                }
                else
                {
                    LogWarning("NetworkManager not found!");
                    yield break;
                }
            }
            
            // Step 2: Wait for LobbyManager to be spawned by NetworkManager
            int maxWaitTime = 50; // 5 seconds max wait
            int waitCount = 0;
            
            while (lobbyManager == null && waitCount < maxWaitTime)
            {
                lobbyManager = LobbyManager.Instance;
                if (lobbyManager == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitCount++;
                }
            }
            
            if (lobbyManager == null)
            {
                LogWarning("LobbyManager not found after waiting. Network might not have started properly.");
            }
            else
            {
                LogUI("LobbyManager found!");
                
                // Subscribe to events if not already subscribed
                lobbyManager.OnPlayerJoined += OnPlayerJoined;
                lobbyManager.OnPlayerLeft += OnPlayerLeft;
                lobbyManager.OnPlayerUpdated += OnPlayerUpdated;
                lobbyManager.OnGameStarting += OnGameStarting;
                
                // Update UI now that LobbyManager is ready
                UpdateUI();
            }
        }
        
        /// <summary>
        /// ✅ NEW: Hide game world (cameras, players, weapons) when UI is shown
        /// </summary>
        private void HideGameWorld()
        {
            // ✅ FIX: Don't hide cameras - just hide game objects
            // UI needs at least one camera to render (Screen Space Overlay still needs a camera)
            
            // Hide player weapons (magenta weapon model)
            var weaponSystems = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            foreach (var weapon in weaponSystems)
            {
                if (weapon.gameObject != null)
                {
                    weapon.gameObject.SetActive(false);
                }
            }
            
            // Hide player models (but keep cameras active)
            var playerControllers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in playerControllers)
            {
                var visuals = player.GetComponent<Player.PlayerVisuals>();
                if (visuals != null && visuals.gameObject != null)
                {
                    visuals.gameObject.SetActive(false);
                }
            }
            
            // ✅ FIX: Keep cameras active - UI needs them for rendering
            // Screen Space Overlay doesn't need a camera, but we keep them active anyway
            // to avoid "No cameras rendering" warning
        }

        /// <summary>
        /// ✅ NEW: Hide the entire lobby UI
        /// </summary>
        public void HidePanel()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            gameStartingPanel?.SetActive(false);
            errorPanel?.SetActive(false);
            waitingForHostPanel?.SetActive(false);
        }

        /// <summary>
        /// ✅ NEW: Check if lobby panel is currently open
        /// Used by FPSController to determine if UI is open
        /// </summary>
        public bool IsPanelOpen()
        {
            // Check if lobbyPanel is active
            if (lobbyPanel != null)
            {
                return lobbyPanel.activeSelf;
            }
            // If no lobbyPanel, check if GameObject itself is active
            return gameObject.activeInHierarchy;
        }

        private void ShowLobbyPanel()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }
            else
            {
                // If lobbyPanel is null, ensure GameObject itself is active
                gameObject.SetActive(true);
            }
            
            gameStartingPanel?.SetActive(false);
            errorPanel?.SetActive(false);
            waitingForHostPanel?.SetActive(false);
        }

        private void HideOtherUIs()
        {
            // ✅ AGGRESSIVE: Hide ALL MainMenu elements
            var mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                // Hide all child panels
                var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.gameObject.SetActive(false);
                }

                var joinPanel = mainMenu.transform.Find("JoinPanel");
                if (joinPanel != null)
                {
                    joinPanel.gameObject.SetActive(false);
                }

                var rootPanel = mainMenu.transform.Find("MainMenu");
                if (rootPanel != null)
                {
                    rootPanel.gameObject.SetActive(false);
                }
                
                // Hide the entire MainMenu GameObject
                mainMenu.gameObject.SetActive(false);
                
                // Hide MainMenu's Canvas if it exists
                Canvas mainMenuCanvas = mainMenu.GetComponentInParent<Canvas>();
                if (mainMenuCanvas != null && (mainMenuCanvas.gameObject.name.Contains("MainMenu") || mainMenuCanvas.gameObject.name.Contains("Main Menu")))
                {
                    mainMenuCanvas.gameObject.SetActive(false);
                    LogUI($"Hidden MainMenu Canvas: {mainMenuCanvas.gameObject.name}");
                }
            }
            
            // ✅ AGGRESSIVE: Hide ALL Canvas elements that might be MainMenu related
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                // Don't hide our own canvas
                if (canvas == GetComponentInParent<Canvas>()) continue;
                
                // Hide MainMenu related canvases
                if (canvas.gameObject.name.Contains("MainMenu") || 
                    canvas.gameObject.name.Contains("Main Menu"))
                {
                    canvas.gameObject.SetActive(false);
                    LogUI($"Hidden MainMenu Canvas: {canvas.gameObject.name}");
                }
            }

            // Hide GameModeSelection (no longer used - we integrated it into LobbyUI)
            var gameModeSelection = FindFirstObjectByType<GameModeSelectionUI>();
            if (gameModeSelection != null)
            {
                gameModeSelection.HidePanel();
            }

            // Hide TeamSelectionUI and RoleSelectionUI if they exist
            var teamSelection = FindFirstObjectByType<TeamSelectionUI>();
            if (teamSelection != null)
            {
                teamSelection.HidePanel();
            }

            var roleSelection = FindFirstObjectByType<RoleSelectionUI>();
            if (roleSelection != null)
            {
                roleSelection.HidePanel();
            }
        }

        public void ShowGameStarting()
        {
            lobbyPanel?.SetActive(false);
            gameStartingPanel?.SetActive(true);
        }

        public void ShowError(string message)
        {
            if (errorPanel != null && errorText != null)
            {
                errorText.text = message;
                errorPanel.SetActive(true);
                
                // Auto-hide after duration
                Invoke(nameof(HideError), errorDisplayDuration);
            }
        }

        private void HideError()
        {
            errorPanel?.SetActive(false);
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            // ✅ NEW: Game mode selection buttons
            if (individualModeButton != null)
            {
                individualModeButton.onClick.AddListener(() => OnGameModeSelected(true));
            }
            
            if (teamModeButton != null)
            {
                teamModeButton.onClick.AddListener(() => OnGameModeSelected(false));
            }
            
            // Host buttons
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
            
            if (autoBalanceButton != null)
                autoBalanceButton.onClick.AddListener(OnAutoBalanceClicked);

            // Player buttons
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyClicked);
            
            if (teamAButton != null)
                teamAButton.onClick.AddListener(() => OnTeamClicked(0));
            
            if (teamBButton != null)
                teamBButton.onClick.AddListener(() => OnTeamClicked(1));
            
            if (leaveButton != null)
                leaveButton.onClick.AddListener(OnLeaveClicked);
        }
        
        /// <summary>
        /// ✅ NEW: Handle game mode selection (Individual = true, Team = false)
        /// </summary>
        private void OnGameModeSelected(bool isIndividual)
        {
            if (lobbyManager != null)
            {
                lobbyManager.SetGameMode(isIndividual);
                LogUI($"Game mode selected: {(isIndividual ? "Individual" : "Team")}");
            }
            
            // Update UI
            if (gameModeText != null)
            {
                gameModeText.text = isIndividual ? "Mod: Bireysel (FFA)" : "Mod: Takım (2v2)";
            }
            
            // Update button visuals
            if (individualModeButton != null)
            {
                var colors = individualModeButton.colors;
                colors.normalColor = isIndividual ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);
                individualModeButton.colors = colors;
            }
            
            if (teamModeButton != null)
            {
                var colors = teamModeButton.colors;
                colors.normalColor = !isIndividual ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);
                teamModeButton.colors = colors;
            }
            
            // Show/hide team selection
            OnGameModeChanged(isIndividual);
        }

        #endregion

        #region Button Callbacks

        private void OnStartGameClicked()
        {
            if (lobbyManager != null && lobbyManager.IsLocalPlayerHost())
            {
                // ✅ FIX: Command artık parametre almıyor, Mirror otomatik connection context sağlar
                lobbyManager.CmdStartGame();
            }
        }

        private void OnAutoBalanceClicked()
        {
            if (lobbyManager != null && lobbyManager.IsLocalPlayerHost())
            {
                // This should be a server command
                LogUI("Auto-balance requested");
                // lobbyManager.CmdAutoBalanceTeams(); // Add this command to LobbyManager if needed
            }
        }

        private void OnReadyClicked()
        {
            if (lobbyManager == null) return;

            var localPlayer = lobbyManager.GetLocalPlayer();
            if (!localPlayer.HasValue) return;

            isLocalPlayerReady = !isLocalPlayerReady;
            // ✅ FIX: Command artık connectionId parametresi almıyor, Mirror otomatik sağlar
            lobbyManager.CmdSetReady(isLocalPlayerReady);

            UpdateReadyButton();
        }

        private void OnTeamClicked(int teamId)
        {
            if (lobbyManager == null) return;

            var localPlayer = lobbyManager.GetLocalPlayer();
            if (!localPlayer.HasValue) return;

            localPlayerTeam = teamId;
            // ✅ FIX: Command artık connectionId parametresi almıyor, Mirror otomatik sağlar
            lobbyManager.CmdSetTeam(teamId);

            UpdateTeamButtons();
        }

        private void OnLeaveClicked()
        {
            if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            UpdateLobbyTitle();
            UpdatePlayerCount();
            UpdateReadyButton(); // Update ready button first (affects host controls)
            UpdateHostControls(); // Then update host controls (checks ready status)
            UpdateTeamButtons();
            
            // ✅ NEW: Refresh player list when UI updates
            if (lobbyManager != null)
            {
                RefreshPlayerList(lobbyManager.GetAllPlayers());
            }
        }

        private void UpdateLobbyTitle()
        {
            if (lobbyTitleText == null) return;

            if (lobbyManager != null && lobbyManager.IsLocalPlayerHost())
            {
                lobbyTitleText.text = "LOBBY (HOST)";
                lobbyTitleText.color = Color.yellow;
            }
            else
            {
                lobbyTitleText.text = "LOBBY";
                lobbyTitleText.color = Color.white;
            }
        }

        private void UpdatePlayerCount()
        {
            if (playerCountText == null || lobbyManager == null) return;

            int current = lobbyManager.GetPlayerCount();
            int max = lobbyManager.GetMaxPlayers();
            playerCountText.text = $"Players: {current}/{max}";
        }

        private void UpdateHostControls()
        {
            bool isHost = lobbyManager != null && lobbyManager.IsLocalPlayerHost();

            // Show/hide host-only buttons
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(isHost);
            
            if (autoBalanceButton != null)
                autoBalanceButton.gameObject.SetActive(isHost);

            // Update start button interactability
            if (startGameButton != null && isHost)
            {
                int playerCount = lobbyManager.GetPlayerCount();
                
                // ✅ NEW: Check if all players are ready (or test mode with 1 player)
                bool allReady = AreAllPlayersReady();
                bool canStart = (playerCount == 1) || (playerCount >= 2 && allReady); // Test mode: 1 player, Normal: 2+ and all ready
                
                startGameButton.interactable = canStart;

                if (startGameButtonText != null)
                {
                    if (playerCount == 1)
                    {
                        startGameButtonText.text = "START GAME (TEST)";
                    }
                    else if (!allReady)
                    {
                        int readyCount = GetReadyPlayerCount();
                        startGameButtonText.text = $"WAITING ({readyCount}/{playerCount} READY)";
                    }
                    else
                    {
                        startGameButtonText.text = "START GAME";
                    }
                }
            }

            // Show waiting panel for non-hosts
            if (waitingForHostPanel != null)
            {
                waitingForHostPanel.SetActive(!isHost);
            }
        }

        private void UpdateReadyButton()
        {
            if (readyButton == null || readyButtonText == null) return;

            // ✅ NEW: Host is automatically ready - show status but disable button
            if (lobbyManager != null && lobbyManager.IsLocalPlayerHost())
            {
                readyButton.gameObject.SetActive(true);
                readyButton.interactable = false; // Host can't toggle ready (always ready)
                readyButtonText.text = "READY [OK] (HOST)";
                
                var colors = readyButton.colors;
                colors.normalColor = readyColor;
                readyButton.colors = colors;
                return;
            }

            // Non-host players can toggle ready
            readyButton.gameObject.SetActive(true);
            readyButton.interactable = true;
            readyButtonText.text = isLocalPlayerReady ? "READY [OK]" : "NOT READY";
            
            var colors2 = readyButton.colors;
            colors2.normalColor = isLocalPlayerReady ? readyColor : notReadyColor;
            readyButton.colors = colors2;
        }

        private void UpdateTeamButtons()
        {
            if (teamAButton == null || teamBButton == null) return;

            // Highlight selected team
            var teamAColors = teamAButton.colors;
            var teamBColors = teamBButton.colors;

            teamAColors.normalColor = localPlayerTeam == 0 ? teamAColor : Color.gray;
            teamBColors.normalColor = localPlayerTeam == 1 ? teamBColor : Color.gray;

            teamAButton.colors = teamAColors;
            teamBButton.colors = teamBColors;

            // ✅ PERFORMANCE FIX: Update team counts only if changed
            if (lobbyManager != null)
            {
                int teamACount = lobbyManager.GetTeamPlayerCount(0);
                int teamBCount = lobbyManager.GetTeamPlayerCount(1);

                // ✅ PERFORMANCE FIX: Cache text components
                if (cachedTeamAText == null && teamAButton != null)
                    cachedTeamAText = teamAButton.GetComponentInChildren<TextMeshProUGUI>();
                if (cachedTeamBText == null && teamBButton != null)
                    cachedTeamBText = teamBButton.GetComponentInChildren<TextMeshProUGUI>();

                // ✅ PERFORMANCE FIX: Only update if count changed (avoid GC allocation)
                if (teamACount != lastTeamACount && cachedTeamAText != null)
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("TEAM A (");
                    stringBuilder.Append(teamACount);
                    stringBuilder.Append(")");
                    cachedTeamAText.text = stringBuilder.ToString();
                    lastTeamACount = teamACount;
                }
                
                if (teamBCount != lastTeamBCount && cachedTeamBText != null)
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("TEAM B (");
                    stringBuilder.Append(teamBCount);
                    stringBuilder.Append(")");
                    cachedTeamBText.text = stringBuilder.ToString();
                    lastTeamBCount = teamBCount;
                }
            }
        }

        #endregion

        #region Ready System

        /// <summary>
        /// ✅ NEW: Check if all players are ready
        /// </summary>
        private bool AreAllPlayersReady()
        {
            if (lobbyManager == null) return false;
            
            var players = lobbyManager.GetAllPlayers();
            if (players.Count == 0) return false;
            
            foreach (var player in players)
            {
                // Host is always considered ready (auto-ready)
                if (!player.isReady && !player.isHost)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// ✅ NEW: Get count of ready players
        /// </summary>
        private int GetReadyPlayerCount()
        {
            if (lobbyManager == null) return 0;
            
            var players = lobbyManager.GetAllPlayers();
            int readyCount = 0;
            
            foreach (var player in players)
            {
                // Host is always considered ready
                if (player.isReady || player.isHost)
                {
                    readyCount++;
                }
            }
            
            return readyCount;
        }

        #endregion

        #region Player List Management

        public void RefreshPlayerList(List<LobbyPlayerData> players)
        {
            // Clear existing items
            ClearPlayerList();

            // Create new items
            foreach (var player in players)
            {
                CreatePlayerListItem(player);
            }

            LogUI($"Refreshed player list - {players.Count} players");
        }

        private void ClearPlayerList()
        {
            foreach (var item in playerListItems)
            {
                if (item != null)
                    Destroy(item);
            }
            playerListItems.Clear();
        }

        private void CreatePlayerListItem(LobbyPlayerData player)
        {
            if (playerListItemPrefab == null || playerListContainer == null)
            {
                Debug.LogWarning("[LobbyUI] Player list prefab or container not assigned!");
                return;
            }

            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            playerListItems.Add(item);

            // Setup item data (assuming prefab has these components)
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var readyText = item.transform.Find("ReadyText")?.GetComponent<TextMeshProUGUI>();
            var teamText = item.transform.Find("TeamText")?.GetComponent<TextMeshProUGUI>();
            var hostIcon = item.transform.Find("HostIcon")?.gameObject;
            var background = item.GetComponent<Image>();

            // ✅ PERFORMANCE FIX: Set player name with StringBuilder
            if (nameText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(player.playerName);
                if (player.isHost)
                    stringBuilder.Append(" [HOST]"); // Host indicator
                nameText.text = stringBuilder.ToString();
            }

            // Set ready state
            if (readyText != null)
            {
                readyText.text = player.isReady ? "[OK] READY" : "[X] NOT READY";
                readyText.color = player.isReady ? readyColor : notReadyColor;
            }

            // Set team
            if (teamText != null)
            {
                switch (player.teamId)
                {
                    case 0:
                        teamText.text = "TEAM A";
                        teamText.color = teamAColor;
                        break;
                    case 1:
                        teamText.text = "TEAM B";
                        teamText.color = teamBColor;
                        break;
                    default:
                        teamText.text = "NO TEAM";
                        teamText.color = Color.gray;
                        break;
                }
            }

            // Set host icon
            if (hostIcon != null)
                hostIcon.SetActive(player.isHost);

            // Set background color based on team
            if (background != null)
            {
                Color bgColor = Color.white;
                switch (player.teamId)
                {
                    case 0:
                        bgColor = teamAColor;
                        bgColor.a = 0.2f;
                        break;
                    case 1:
                        bgColor = teamBColor;
                        bgColor.a = 0.2f;
                        break;
                    default:
                        bgColor = Color.gray;
                        bgColor.a = 0.1f;
                        break;
                }
                background.color = bgColor;
            }
        }

        #endregion

        #region Event Handlers

        private void OnPlayerJoined(LobbyPlayerData player)
        {
            LogUI($"Player joined: {player.playerName}");
            UpdateUI();
        }

        private void OnPlayerLeft(LobbyPlayerData player)
        {
            LogUI($"Player left: {player.playerName}");
            UpdateUI();
        }

        private void OnPlayerUpdated(LobbyPlayerData player)
        {
            LogUI($"Player updated: {player.playerName}");
            UpdateUI();
        }

        private void OnGameStarting()
        {
            LogUI("Game starting!");
            ShowGameStarting();
        }

        /// <summary>
        /// ✅ NEW: Called when game mode changes (Individual vs Team)
        /// </summary>
        public void OnGameModeChanged(bool isIndividualMode)
        {
            LogUI($"Game mode changed: {(isIndividualMode ? "Individual" : "Team")}");
            
            // Show/hide team selection buttons based on mode
            if (teamSelectionPanel != null)
            {
                teamSelectionPanel.SetActive(!isIndividualMode); // Show in team mode only
            }

            if (teamAButton != null)
            {
                teamAButton.gameObject.SetActive(!isIndividualMode);
            }

            if (teamBButton != null)
            {
                teamBButton.gameObject.SetActive(!isIndividualMode);
            }

            // Update player list to reflect team assignments
            UpdateUI(); // This will refresh the player list automatically via UpdateUI -> RefreshPlayerList
        }

        #endregion
    }
}

