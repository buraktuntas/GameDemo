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
    /// Game Mode Selection + Lobby UI - Combined
    /// Tek ekranda hem oyun modu seçimi hem de lobby özellikleri
    /// </summary>
    public class GameModeSelectionUI : MonoBehaviour
    {
        [Header("Mode Selection UI")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button individualModeButton;
        [SerializeField] private Button teamModeButton;
        [SerializeField] private Button confirmButton;

        [Header("Mode Descriptions")]
        [SerializeField] private TextMeshProUGUI modeTitleText;
        [SerializeField] private TextMeshProUGUI modeDescriptionText;
        [SerializeField] private GameObject individualDescriptionPanel;
        [SerializeField] private GameObject teamDescriptionPanel;

        [Header("Lobby UI - Player List")]
        [SerializeField] private GameObject lobbySection; // Lobby bölümü (mod seçiminden sonra görünür)
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;

        [Header("Lobby UI - Host Controls")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI startGameButtonText;
        [SerializeField] private GameObject hostControlsPanel;

        [Header("Lobby UI - Player Controls")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private GameObject playerControlsPanel;

        [Header("Lobby UI - Info")]
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI gameModeText;

        [Header("Visuals")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color selectedButtonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color normalButtonColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color notReadyColor = new Color(0.8f, 0.2f, 0.2f);

        private bool isIndividualMode = true; // Default: Individual
        private bool isConfirmed = false;
        private LobbyManager lobbyManager;
        private List<GameObject> playerListItems = new List<GameObject>();
        private bool isLocalPlayerReady = false;

        public System.Action<bool> OnGameModeSelected; // true = Individual, false = Team

        private void Start()
        {
            // Setup button listeners
            if (individualModeButton != null)
            {
                individualModeButton.onClick.AddListener(() => SelectMode(true));
            }

            if (teamModeButton != null)
            {
                teamModeButton.onClick.AddListener(() => SelectMode(false));
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmMode);
            }

            // Setup lobby buttons
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            }

            // Get LobbyManager reference
            lobbyManager = LobbyManager.Instance;
            if (lobbyManager != null)
            {
                lobbyManager.OnPlayerJoined += OnPlayerJoined;
                lobbyManager.OnPlayerLeft += OnPlayerLeft;
                lobbyManager.OnPlayerUpdated += OnPlayerUpdated;
                lobbyManager.OnGameStarting += OnGameStarting;
            }

            // Hide lobby section initially
            if (lobbySection != null)
            {
                lobbySection.SetActive(false);
            }

            // Default selection
            SelectMode(true);
            HidePanel();
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

        private float lastLobbyUpdateTime = 0f;
        private const float LOBBY_UPDATE_INTERVAL = 0.2f; // Update every 200ms

        private void Update()
        {
            // Update lobby UI if confirmed and network is active (throttled)
            if (isConfirmed && (NetworkServer.active || NetworkClient.isConnected))
            {
                if (Time.time - lastLobbyUpdateTime >= LOBBY_UPDATE_INTERVAL)
                {
                    UpdateLobbyUI();
                    lastLobbyUpdateTime = Time.time;
                }
            }
        }

        public void ShowPanel()
        {
            Debug.Log("🎮 [GameModeSelectionUI] ShowPanel called!");
            
            // ✅ STEP 1: Ensure Canvas is visible and configured
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // Try to find or create canvas
                canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("GameModeSelectionCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    
                    // Make this GameObject a child of canvas
                    transform.SetParent(canvas.transform, false);
                }
            }
            
            if (canvas != null)
            {
                // Ensure canvas is active
                if (!canvas.gameObject.activeSelf)
                {
                    canvas.gameObject.SetActive(true);
                }
                
                // Set Canvas to Screen Space Overlay (full screen)
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                
                // Set Canvas sortingOrder to be on top
                canvas.sortingOrder = 100;
                
                Debug.Log($"✅ [GameModeSelectionUI] Canvas configured: {canvas.renderMode}, sortingOrder: {canvas.sortingOrder}");
            }
            
            // ✅ STEP 2: Ensure root GameObject is active and full screen
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            // ✅ STEP 3: Configure root RectTransform to be full screen
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.sizeDelta = Vector2.zero;
                rootRect.anchoredPosition = Vector2.zero;
                Debug.Log("✅ [GameModeSelectionUI] Root RectTransform set to full screen");
            }
            
            // ✅ STEP 4: Configure background to be full screen and opaque
            if (backgroundImage != null)
            {
                RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.sizeDelta = Vector2.zero;
                    bgRect.anchoredPosition = Vector2.zero;
                }
                
                // Make background fully opaque to hide game world
                backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-gray, fully opaque
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                // Create background if it doesn't exist
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform, false);
                backgroundImage = bgObj.AddComponent<Image>();
                backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
                
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;
                
                // Make it render first (behind everything)
                bgObj.transform.SetAsFirstSibling();
                Debug.Log("✅ [GameModeSelectionUI] Background created");
            }
            
            // ✅ STEP 5: Show selectionPanel (full screen) with background
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
                
                // Configure selectionPanel to be full screen
                RectTransform panelRect = selectionPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = Vector2.zero;
                    panelRect.anchorMax = Vector2.one;
                    panelRect.sizeDelta = Vector2.zero;
                    panelRect.anchoredPosition = Vector2.zero;
                }
                
                // Ensure selectionPanel has a background
                Image panelBg = selectionPanel.GetComponent<Image>();
                if (panelBg == null)
                {
                    panelBg = selectionPanel.AddComponent<Image>();
                }
                panelBg.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-gray, fully opaque
                
                Debug.Log("✅ [GameModeSelectionUI] selectionPanel activated and configured with background");
            }
            else
            {
                Debug.LogWarning("⚠️ [GameModeSelectionUI] selectionPanel is null!");
            }

            // ✅ STEP 6: Reset state
            isConfirmed = false;
            SelectMode(isIndividualMode);

            // ✅ STEP 7: Hide confirm button (auto-confirm on mode selection)
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }

            // ✅ STEP 8: Enable mode selection buttons
            if (individualModeButton != null)
            {
                individualModeButton.interactable = true;
            }
            if (teamModeButton != null)
            {
                teamModeButton.interactable = true;
            }

            // ✅ STEP 9: Hide lobby section initially
            if (lobbySection != null)
            {
                lobbySection.SetActive(false);
            }

            // ✅ STEP 10: Hide game world (cameras, players, weapons)
            HideGameWorld();
            
            // ✅ STEP 11: Hide other UIs
            HideOtherUIs();
            
            // ✅ STEP 12: Force cursor unlock for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log($"✅ [GameModeSelectionUI] Panel shown - Full screen layout configured");
        }
        
        /// <summary>
        /// ✅ NEW: Hide game world (cameras, players, weapons) when UI is shown
        /// </summary>
        private void HideGameWorld()
        {
            // Hide all player cameras
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                // Don't hide UI cameras
                if (cam.GetComponent<Canvas>() == null && cam.name != "UICamera")
                {
                    cam.enabled = false;
                    Debug.Log($"✅ [GameModeSelectionUI] Camera disabled: {cam.name}");
                }
            }
            
            // Hide player weapons (magenta weapon model)
            var weaponSystems = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            foreach (var weapon in weaponSystems)
            {
                if (weapon.gameObject != null)
                {
                    weapon.gameObject.SetActive(false);
                    Debug.Log($"✅ [GameModeSelectionUI] Weapon hidden: {weapon.gameObject.name}");
                }
            }
            
            // Hide player models (optional - can keep them if needed)
            var playerControllers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in playerControllers)
            {
                // Only hide visual parts, keep the controller active
                var visuals = player.GetComponent<Player.PlayerVisuals>();
                if (visuals != null && visuals.gameObject != null)
                {
                    visuals.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// ✅ NEW: Show game world when UI is hidden
        /// </summary>
        private void ShowGameWorld()
        {
            // Re-enable all player cameras
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.GetComponent<Canvas>() == null && cam.name != "UICamera")
                {
                    cam.enabled = true;
                }
            }
            
            // Show player weapons
            var weaponSystems = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            foreach (var weapon in weaponSystems)
            {
                if (weapon.gameObject != null)
                {
                    weapon.gameObject.SetActive(true);
                }
            }
            
            // Show player models
            var playerControllers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in playerControllers)
            {
                var visuals = player.GetComponent<Player.PlayerVisuals>();
                if (visuals != null && visuals.gameObject != null)
                {
                    visuals.gameObject.SetActive(true);
                }
            }
        }

        public void HidePanel()
        {
            // Show game world again
            ShowGameWorld();
            
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// ✅ NEW: Check if game mode selection panel is currently open
        /// Used by FPSController to determine if UI is open
        /// </summary>
        public bool IsPanelOpen()
        {
            // Check if selectionPanel is active
            if (selectionPanel != null)
            {
                return selectionPanel.activeSelf;
            }
            // If no selectionPanel, check if GameObject itself is active
            return gameObject.activeInHierarchy;
        }

        private void SelectMode(bool individual)
        {
            isIndividualMode = individual;

            // Update button visuals
            UpdateButtonVisuals();

            // Update description
            UpdateDescription();

            Debug.Log($"[GameModeSelectionUI] Mode selected: {(individual ? "Individual" : "Team")}");

            // ✅ NEW: Auto-confirm mode selection (no confirm button needed)
            if (!isConfirmed)
            {
                ConfirmMode();
            }
        }

        private void UpdateButtonVisuals()
        {
            // Individual button
            if (individualModeButton != null)
            {
                var colors = individualModeButton.colors;
                colors.normalColor = isIndividualMode ? selectedButtonColor : normalButtonColor;
                individualModeButton.colors = colors;
            }

            // Team button
            if (teamModeButton != null)
            {
                var colors = teamModeButton.colors;
                colors.normalColor = !isIndividualMode ? selectedButtonColor : normalButtonColor;
                teamModeButton.colors = colors;
            }
        }

        private void UpdateDescription()
        {
            if (modeDescriptionText != null)
            {
                if (isIndividualMode)
                {
                    modeDescriptionText.text = "Her oyuncu kendi başına oynar.\n" +
                                              "En çok skor yapan oyuncu kazanır.\n" +
                                              "Takım sınırlaması yok.";
                }
                else
                {
                    modeDescriptionText.text = "Oyuncular iki takıma ayrılır.\n" +
                                              "Takım bazlı hedefler ve strateji.\n" +
                                              "Takım skoru önemlidir.";
                }
            }

            // Show/hide description panels
            if (individualDescriptionPanel != null)
            {
                individualDescriptionPanel.SetActive(isIndividualMode);
            }

            if (teamDescriptionPanel != null)
            {
                teamDescriptionPanel.SetActive(!isIndividualMode);
            }
        }

        private void ConfirmMode()
        {
            bool wasConfirmed = isConfirmed;
            isConfirmed = true;

            Debug.Log($"✅ [GameModeSelectionUI] Mode confirmed: {(isIndividualMode ? "Individual (FFA)" : "Team (4v4)")}");

            // ✅ CRITICAL: Start network ONLY if not already started
            if (!wasConfirmed)
            {
                NetworkManager networkManager = NetworkManager.singleton;
                if (networkManager != null && !NetworkServer.active && !NetworkClient.isConnected)
                {
                    // Configure network address for host
                    string currentAddress = networkManager.networkAddress;
                    if (currentAddress == "localhost" || currentAddress == "127.0.0.1")
                    {
                        networkManager.networkAddress = "";
                        Debug.Log("✅ [GameModeSelectionUI] Host: networkAddress cleared (server will listen on all interfaces)");
                    }

                    // Start host
                    networkManager.StartHost();
                    Debug.Log("🚀 [GameModeSelectionUI] Network started (Host mode)");
                }
            }

            // ✅ CRITICAL: Store game mode in LobbyManager (update if already confirmed)
            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager != null)
            {
                lobbyManager.SetGameMode(isIndividualMode);
                Debug.Log($"✅ [GameModeSelectionUI] Game mode set in LobbyManager: {(isIndividualMode ? "Individual" : "Team")}");
            }
            else
            {
                Debug.LogError("❌ [GameModeSelectionUI] LobbyManager.Instance is NULL! Cannot set game mode.");
            }

            // Invoke callback only if first time
            if (!wasConfirmed)
            {
                OnGameModeSelected?.Invoke(isIndividualMode);
            }

            // ✅ NEW: Hide confirm button (no longer needed)
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }

            // ✅ NEW: Make mode selection buttons smaller and move to bottom
            // Keep them visible but smaller - they can still change mode
            if (individualModeButton != null)
            {
                // Make button smaller
                RectTransform buttonRect = individualModeButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(150, 40); // Smaller size
                }
            }
            if (teamModeButton != null)
            {
                // Make button smaller
                RectTransform buttonRect = teamModeButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(150, 40); // Smaller size
                }
            }

            // ✅ NEW: Show lobby section in the same UI (top 70% of screen)
            if (lobbySection != null)
            {
                lobbySection.SetActive(true);
                Debug.Log("✅ [GameModeSelectionUI] Lobby section activated");
                
                // Position lobby section at top (70% of screen)
                RectTransform lobbyRect = lobbySection.GetComponent<RectTransform>();
                if (lobbyRect != null)
                {
                    lobbyRect.anchorMin = new Vector2(0f, 0.3f); // Start at 30% from bottom
                    lobbyRect.anchorMax = new Vector2(1f, 1f); // End at top
                    lobbyRect.sizeDelta = Vector2.zero;
                    lobbyRect.anchoredPosition = Vector2.zero;
                }
            }

            // ✅ NEW: Move mode selection buttons to bottom (bottom 30% of screen)
            if (selectionPanel != null)
            {
                RectTransform panelRect = selectionPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    // Position at bottom 30% of screen
                    panelRect.anchorMin = new Vector2(0f, 0f);
                    panelRect.anchorMax = new Vector2(1f, 0.3f);
                    panelRect.sizeDelta = Vector2.zero;
                    panelRect.anchoredPosition = Vector2.zero;
                }
            }

            // Update lobby UI immediately
            StartCoroutine(WaitForNetworkAndUpdateLobby());
        }

        private System.Collections.IEnumerator WaitForNetworkAndUpdateLobby()
        {
            // Wait for network to be ready
            yield return new WaitForSeconds(0.5f);
            
            // Wait for LobbyManager to be ready
            while (lobbyManager == null)
            {
                lobbyManager = LobbyManager.Instance;
                yield return new WaitForSeconds(0.1f);
            }

            // ✅ NEW: Wait for host to be registered in lobby (if we're the host)
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                int maxWaitTime = 50; // 5 seconds max wait
                int waitCount = 0;
                while (lobbyManager.GetPlayerCount() == 0 && waitCount < maxWaitTime)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitCount++;
                }
                
                if (lobbyManager.GetPlayerCount() == 0)
                {
                    Debug.LogWarning("⚠️ [GameModeSelectionUI] Host not registered in lobby after waiting - UI may not show host");
                }
                else
                {
                    Debug.Log($"✅ [GameModeSelectionUI] Host registered in lobby - {lobbyManager.GetPlayerCount()} player(s)");
                }
            }

            // Update lobby UI
            UpdateLobbyUI();
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
                if (mainMenuCanvas != null && mainMenuCanvas.gameObject.name.Contains("MainMenu"))
                {
                    mainMenuCanvas.gameObject.SetActive(false);
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
                    Debug.Log($"✅ [GameModeSelectionUI] Hidden MainMenu Canvas: {canvas.gameObject.name}");
                }
            }

            // Hide LobbyUI (we're using integrated lobby now)
            var lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.HidePanel();
            }

            // Hide TeamSelectionUI and RoleSelectionUI
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

        #region Lobby Functions

        private void UpdateLobbyUI()
        {
            if (!isConfirmed || lobbyManager == null) return;

            UpdatePlayerCount();
            UpdateGameModeText();
            UpdateReadyButton();
            UpdateHostControls();
            RefreshPlayerList(lobbyManager.GetAllPlayers());
        }

        private void UpdatePlayerCount()
        {
            if (playerCountText == null || lobbyManager == null) return;
            int current = lobbyManager.GetPlayerCount();
            int max = lobbyManager.GetMaxPlayers();
            playerCountText.text = $"Oyuncular: {current}/{max}";
        }

        private void UpdateGameModeText()
        {
            if (gameModeText == null) return;
            gameModeText.text = isIndividualMode ? "Mod: Bireysel (FFA)" : "Mod: Takım (2v2)";
        }

        private void UpdateReadyButton()
        {
            if (readyButton == null || readyButtonText == null || lobbyManager == null) return;

            bool isHost = lobbyManager.IsLocalPlayerHost();
            
            // Host is automatically ready
            if (isHost)
            {
                readyButton.interactable = false;
                readyButtonText.text = "HAZIR [OK] (HOST)";
                var colors = readyButton.colors;
                colors.normalColor = readyColor;
                readyButton.colors = colors;
            }
            else
            {
                readyButton.interactable = true;
                readyButtonText.text = isLocalPlayerReady ? "HAZIR [OK]" : "HAZIR DEĞİL";
                var colors = readyButton.colors;
                colors.normalColor = isLocalPlayerReady ? readyColor : notReadyColor;
                readyButton.colors = colors;
            }
        }

        private void UpdateHostControls()
        {
            if (startGameButton == null || startGameButtonText == null || lobbyManager == null) return;

            bool isHost = lobbyManager.IsLocalPlayerHost();
            
            if (hostControlsPanel != null)
            {
                hostControlsPanel.SetActive(isHost);
            }

            if (playerControlsPanel != null)
            {
                playerControlsPanel.SetActive(!isHost);
            }

            if (isHost && startGameButton != null)
            {
                int playerCount = lobbyManager.GetPlayerCount();
                bool allReady = AreAllPlayersReady();
                bool canStart = (playerCount == 1) || (playerCount >= 2 && allReady);

                startGameButton.interactable = canStart;

                if (startGameButtonText != null)
                {
                    if (playerCount == 1)
                    {
                        startGameButtonText.text = "OYUNU BAŞLAT (TEST)";
                    }
                    else if (!allReady)
                    {
                        int readyCount = GetReadyPlayerCount();
                        startGameButtonText.text = $"BEKLENİYOR ({readyCount}/{playerCount} HAZIR)";
                    }
                    else
                    {
                        startGameButtonText.text = "OYUNU BAŞLAT";
                    }
                }
            }
        }

        private bool AreAllPlayersReady()
        {
            if (lobbyManager == null) return false;
            foreach (var player in lobbyManager.GetAllPlayers())
            {
                if (!player.isReady && !player.isHost)
                {
                    return false;
                }
            }
            return true;
        }

        private int GetReadyPlayerCount()
        {
            if (lobbyManager == null) return 0;
            int readyCount = 0;
            foreach (var player in lobbyManager.GetAllPlayers())
            {
                if (player.isReady || player.isHost)
                {
                    readyCount++;
                }
            }
            return readyCount;
        }

        private void RefreshPlayerList(List<LobbyPlayerData> players)
        {
            if (playerListContainer == null) return;

            // Clear existing items
            foreach (var item in playerListItems)
            {
                if (item != null)
                    Destroy(item);
            }
            playerListItems.Clear();

            // Create new items
            if (playerListItemPrefab != null)
            {
                foreach (var player in players)
                {
                    CreatePlayerListItem(player);
                }
            }
            else
            {
                // Fallback: Create simple text items
                foreach (var player in players)
                {
                    GameObject item = new GameObject($"Player_{player.playerName}");
                    item.transform.SetParent(playerListContainer, false);
                    
                    TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
                    text.text = $"{player.playerName} {(player.isHost ? "[HOST]" : "")} - {(player.isReady ? "[OK] HAZIR" : "[X] HAZIR DEĞİL")}";
                    text.fontSize = 24;
                    
                    playerListItems.Add(item);
                }
            }
        }

        private void CreatePlayerListItem(LobbyPlayerData player)
        {
            if (playerListItemPrefab == null || playerListContainer == null) return;

            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            playerListItems.Add(item);

            // Setup item data
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var readyText = item.transform.Find("ReadyText")?.GetComponent<TextMeshProUGUI>();
            var hostIcon = item.transform.Find("HostIcon")?.gameObject;

            if (nameText != null)
            {
                nameText.text = player.playerName;
            }

            if (readyText != null)
            {
                readyText.text = player.isReady ? "[OK] HAZIR" : "[X] HAZIR DEĞİL";
                readyText.color = player.isReady ? readyColor : notReadyColor;
            }

            if (hostIcon != null)
            {
                hostIcon.SetActive(player.isHost);
            }
        }

        private void OnReadyButtonClicked()
        {
            if (lobbyManager == null) return;
            
            isLocalPlayerReady = !isLocalPlayerReady;
            lobbyManager.CmdSetReady(isLocalPlayerReady);
            UpdateReadyButton();
        }

        private void OnStartGameButtonClicked()
        {
            if (lobbyManager == null) return;
            lobbyManager.CmdStartGame();
        }

        private void OnPlayerJoined(LobbyPlayerData player)
        {
            UpdateLobbyUI();
        }

        private void OnPlayerLeft(LobbyPlayerData player)
        {
            UpdateLobbyUI();
        }

        private void OnPlayerUpdated(LobbyPlayerData player)
        {
            UpdateLobbyUI();
        }

        private void OnGameStarting()
        {
            Debug.Log("[GameModeSelectionUI] Game is starting!");
        }

        #endregion
    }
}

