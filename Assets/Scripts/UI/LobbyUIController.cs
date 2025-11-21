using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Network;
using TacticalCombat.Core;

namespace TacticalCombat.UI
{
    /// <summary>
    /// ✅ NEW: Professional Lobby UI Controller - AAA Quality
    /// Sıfırdan temiz, modern, robust lobby sistemi
    /// Hem Host hem Client için mükemmel çalışır
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        public static LobbyUIController Instance { get; private set; }

        [Header("Auto-Created UI References")]
        private Canvas lobbyCanvas;
        private GameObject lobbyPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI playerCountText;
        private Transform playerListContainer;
        private Button startGameButton;
        private Button readyButton;
        private Button leaveButton;
        private GameObject waitingPanel;
        private TextMeshProUGUI waitingText;
        private GameObject errorPanel;
        private TextMeshProUGUI errorText;

        [Header("Settings")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color notReadyColor = new Color(0.8f, 0.2f, 0.2f);

        private LobbyManager lobbyManager;
        private List<GameObject> playerListItems = new List<GameObject>();
        private bool isLocalPlayerReady = false;
        private Coroutine errorHideCoroutine;
        private float lastUpdateTime = 0f;
        private int lastPlayerCount = -1; // Track player count to only log on changes
        private bool cameraWasActive = false; // Track camera state to only log on changes

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Hide initially
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        private float lastCameraCheckTime = 0f;
        
        private void Update()
        {
            // ✅ CRITICAL FIX: Continuously ensure camera is active when lobby is open
            // ✅ PERFORMANCE: Throttle camera checks (every 1 second) to prevent spam
            if (lobbyPanel != null && lobbyPanel.activeSelf)
            {
                if (Time.time - lastCameraCheckTime >= 1.0f)
                {
                    EnsureCameraActive();
                    lastCameraCheckTime = Time.time;
                }
            }

            // ✅ PERFORMANCE: Throttle UI updates (every 0.5 seconds)
            if (Time.time - lastUpdateTime >= 0.5f)
            {
                if (lobbyManager != null && lobbyPanel != null && lobbyPanel.activeSelf)
                {
                    UpdateUI();
                }
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// ✅ MAIN ENTRY POINT: Show lobby UI (called from MainMenu)
        /// </summary>
        public void ShowLobby()
        {
            Debug.Log("🎮 [LobbyUIController] ShowLobby called");

            // Step 1: Ensure Canvas exists
            EnsureCanvas();

            // Step 2: Create/Find Lobby Panel
            EnsureLobbyPanel();

            // Step 3: Create all UI elements
            CreateUIElements();

            // Step 4: Setup button listeners
            SetupButtons();

            // Step 5: Show panel
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }

            // ✅ CRITICAL FIX: Start network if not already started (for host)
            StartNetworkIfNeeded();

            // Step 6: Start network and connect to LobbyManager
            StartCoroutine(ConnectToLobbyManager());

            // Step 7: Hide game world
            HideGameWorld();

            // Step 8: Ensure camera is active
            EnsureCameraActive();

            Debug.Log("✅ [LobbyUIController] Lobby UI shown");
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Start network if not already started (for host)
        /// </summary>
        private void StartNetworkIfNeeded()
        {
            // Check if network is already running
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                Debug.Log("✅ [LobbyUIController] Network already running");
                return;
            }

            // If we're trying to host, start the server
            NetworkManager networkManager = NetworkManager.singleton;
            if (networkManager != null)
            {
                // ✅ CRITICAL: Clear networkAddress for host (server listens on all interfaces)
                if (networkManager.networkAddress == "localhost" || networkManager.networkAddress == "127.0.0.1")
                {
                    networkManager.networkAddress = "";
                    Debug.Log("✅ [LobbyUIController] Cleared networkAddress for host");
                }

                // Start host (this will start both server and client)
                networkManager.StartHost();
                Debug.Log("🚀 [LobbyUIController] Network started (Host mode)");
            }
            else
            {
                Debug.LogError("❌ [LobbyUIController] NetworkManager not found! Cannot start network.");
            }
        }

        /// <summary>
        /// Hide lobby UI
        /// </summary>
        public void HideLobby()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        #region Canvas & Panel Setup

        private void EnsureCanvas()
        {
            // Find existing canvas
            lobbyCanvas = FindFirstObjectByType<Canvas>();
            
            if (lobbyCanvas == null)
            {
                // Create new canvas
                GameObject canvasObj = new GameObject("LobbyCanvas");
                lobbyCanvas = canvasObj.AddComponent<Canvas>();
                lobbyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                lobbyCanvas.sortingOrder = 100; // ✅ CRITICAL: High sorting order to be on top
                lobbyCanvas.overrideSorting = true; // ✅ CRITICAL: Override sorting to ensure visibility

                // Add CanvasScaler
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                // Add GraphicRaycaster
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add EventSystem if not exists
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                Debug.Log("✅ [LobbyUIController] Created new Canvas");
            }
            else
            {
                lobbyCanvas.gameObject.SetActive(true);
                lobbyCanvas.sortingOrder = 100; // ✅ CRITICAL: High sorting order to be on top
                lobbyCanvas.overrideSorting = true; // ✅ CRITICAL: Override sorting to ensure visibility
                Debug.Log($"✅ [LobbyUIController] Using existing Canvas: {lobbyCanvas.name}");
            }
        }

        private void EnsureLobbyPanel()
        {
            if (lobbyPanel == null)
            {
                // Try to find existing
                Transform existing = lobbyCanvas.transform.Find("LobbyPanel");
                if (existing != null)
                {
                    lobbyPanel = existing.gameObject;
                }
                else
                {
                    // Create new
                    lobbyPanel = new GameObject("LobbyPanel");
                    lobbyPanel.transform.SetParent(lobbyCanvas.transform, false);
                    
                    RectTransform rect = lobbyPanel.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;

                    Image bg = lobbyPanel.AddComponent<Image>();
                    bg.color = backgroundColor;

                    Debug.Log("✅ [LobbyUIController] Created LobbyPanel");
                }
            }
        }

        #endregion

        #region UI Element Creation

        private void CreateUIElements()
        {
            if (lobbyPanel == null)
            {
                Debug.LogError("❌ [LobbyUIController] Cannot create UI elements - lobbyPanel is null!");
                return;
            }

            Debug.Log("🎨 [LobbyUIController] Creating UI elements...");

            // Title
            CreateTitle();

            // Player Count
            CreatePlayerCount();

            // Player List Container
            CreatePlayerListContainer();
            
            // ✅ CRITICAL: Verify playerListContainer was created
            // Note: If null here, UpdateUI() will recreate it
            if (playerListContainer == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] playerListContainer is null after CreatePlayerListContainer() - will be recreated in UpdateUI()");
                // Try to find it in scene as fallback
                Transform foundContainer = lobbyPanel?.transform?.Find("PlayerListContainer/ScrollView/Viewport/Content");
                if (foundContainer != null)
                {
                    playerListContainer = foundContainer;
                    Debug.Log($"✅ [LobbyUIController] Found playerListContainer in scene: {playerListContainer.name}");
                }
            }
            else
            {
                Debug.Log($"✅ [LobbyUIController] playerListContainer verified: {playerListContainer.name}");
            }

            // Buttons
            CreateButtons();

            // Waiting Panel
            CreateWaitingPanel();

            // Error Panel
            CreateErrorPanel();
            
            Debug.Log("✅ [LobbyUIController] All UI elements created");
        }

        private void CreateTitle()
        {
            if (titleText != null) return;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(lobbyPanel.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();

            // Assign font
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                titleText.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            titleText.text = "LOBBY";
            titleText.fontSize = 64;
            titleText.color = Color.white;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.fontStyle = TMPro.FontStyles.Bold;

            RectTransform rect = titleObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.85f);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            titleObj.SetActive(true);
        }

        private void CreatePlayerCount()
        {
            if (playerCountText != null) return;

            GameObject countObj = new GameObject("PlayerCountText");
            countObj.transform.SetParent(lobbyPanel.transform, false);
            playerCountText = countObj.AddComponent<TextMeshProUGUI>();

            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                playerCountText.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            playerCountText.text = "Players: 0/8";
            playerCountText.fontSize = 28;
            playerCountText.color = Color.white;
            playerCountText.alignment = TMPro.TextAlignmentOptions.Center;

            RectTransform rect = countObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.78f);
            rect.anchorMax = new Vector2(1, 0.85f);
            rect.sizeDelta = Vector2.zero;

            countObj.SetActive(true);
        }

        private void CreatePlayerListContainer()
        {
            // ✅ CRITICAL FIX: Check if playerListContainer exists AND is valid (not destroyed)
            // Also try to find existing container in scene first - BUT ONLY UNDER lobbyPanel
            if (playerListContainer == null || (playerListContainer != null && playerListContainer.gameObject == null))
            {
                // ✅ CRITICAL: Only search under lobbyPanel, not in entire scene (to avoid finding GameModeSelectionPanel's container)
                if (lobbyPanel != null)
                {
                    Transform foundContainer = lobbyPanel.transform.Find("PlayerListContainer/ScrollView/Viewport/Content");
                    if (foundContainer != null)
                    {
                        Debug.Log("✅ [LobbyUIController] Found existing playerListContainer under lobbyPanel, re-assigning...");
                        playerListContainer = foundContainer;
                        return;
                    }
                }
            }
            else if (playerListContainer != null && playerListContainer.gameObject != null)
            {
                // ✅ CRITICAL: Verify it's actually under lobbyPanel
                if (IsTransformUnderParent(playerListContainer, lobbyPanel?.transform))
                {
                    Debug.Log("✅ [LobbyUIController] playerListContainer already exists and is valid");
                    return;
                }
                else
                {
                    Debug.LogWarning("⚠️ [LobbyUIController] playerListContainer exists but is NOT under lobbyPanel! Recreating...");
                    playerListContainer = null; // Force recreation
                }
            }

            if (lobbyPanel == null)
            {
                Debug.LogError("❌ [LobbyUIController] Cannot create playerListContainer - lobbyPanel is null!");
                return;
            }

            Debug.Log("🎨 [LobbyUIController] Creating playerListContainer...");

            GameObject containerObj = new GameObject("PlayerListContainer");
            containerObj.transform.SetParent(lobbyPanel.transform, false);

            RectTransform rect = containerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.3f);
            rect.anchorMax = new Vector2(0.9f, 0.75f);
            rect.sizeDelta = Vector2.zero;

            // ScrollView
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(containerObj.transform, false);
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.sizeDelta = Vector2.zero;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false; // ✅ CRITICAL: Don't show mask graphic
            Image viewportBg = viewport.AddComponent<Image>();
            viewportBg.color = Color.clear;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = Vector2.zero; // ✅ CRITICAL: Ensure no offset
            viewportRect.offsetMax = Vector2.zero; // ✅ CRITICAL: Ensure no offset

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            // ✅ CRITICAL: Content should stretch to fill viewport width, but height is controlled by ContentSizeFitter
            contentRect.anchorMin = new Vector2(0, 1); // Top-left anchor
            contentRect.anchorMax = new Vector2(1, 1); // Top-right anchor
            contentRect.pivot = new Vector2(0.5f, 1f); // Top-center pivot
            contentRect.sizeDelta = new Vector2(0, 0); // Width stretches, height starts at 0
            contentRect.anchoredPosition = Vector2.zero; // ✅ CRITICAL: Start at top (0,0)

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = false; // ✅ CRITICAL: Don't control height, use sizeDelta
            layout.childControlWidth = true; // ✅ CRITICAL: Control width to stretch
            layout.childForceExpandWidth = true; // ✅ CRITICAL: Force expand width
            layout.childForceExpandHeight = false; // ✅ CRITICAL: Don't expand height
            layout.childAlignment = TextAnchor.UpperLeft; // ✅ CRITICAL: Align from top-left (UpperCenter can cause positioning issues)

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // ✅ CRITICAL: Height adjusts to content
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // ✅ CRITICAL: Width is controlled by anchor
            
            // ✅ CRITICAL: Set initial size - ContentSizeFitter will adjust height as items are added
            contentRect.sizeDelta = new Vector2(0, 0); // ✅ CRITICAL: Let ContentSizeFitter handle size
            contentRect.offsetMin = new Vector2(0, 0); // ✅ CRITICAL: Ensure no offset
            contentRect.offsetMax = new Vector2(0, 0); // ✅ CRITICAL: Ensure no offset
            
            // ✅ CRITICAL: Force initial layout calculation
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic; // ✅ CRITICAL: Allow scrolling
            scrollRect.elasticity = 0.1f;

            // ✅ CRITICAL: Ensure Content is active and visible
            content.SetActive(true);
            containerObj.SetActive(true);
            
            // ✅ CRITICAL FIX: Assign AFTER SetActive and all component setup
            // This ensures the Transform is fully initialized before assignment
            playerListContainer = content.transform;
            
            // ✅ CRITICAL: Verify assignment immediately
            if (playerListContainer == null)
            {
                Debug.LogError("❌ [LobbyUIController] Failed to assign playerListContainer!");
                return;
            }
            else if (playerListContainer.gameObject == null)
            {
                Debug.LogError("❌ [LobbyUIController] playerListContainer GameObject is null!");
                return;
            }
            else
            {
                Debug.Log($"✅ [LobbyUIController] playerListContainer created successfully: {playerListContainer.name} (Active: {playerListContainer.gameObject.activeSelf})");
            }
        }

        private void CreateButtons()
        {
            // Start Game Button (Host only)
            if (startGameButton == null)
            {
                startGameButton = CreateButton("StartGameButton", "START GAME", new Vector2(0.4f, 0.15f), new Vector2(0.6f, 0.25f));
                startGameButton.GetComponent<Image>().color = readyColor;
            }

            // Ready Button
            if (readyButton == null)
            {
                readyButton = CreateButton("ReadyButton", "NOT READY", new Vector2(0.2f, 0.15f), new Vector2(0.4f, 0.25f));
                readyButton.GetComponent<Image>().color = notReadyColor;
            }

            // Leave Button
            if (leaveButton == null)
            {
                leaveButton = CreateButton("LeaveButton", "LEAVE", new Vector2(0.6f, 0.15f), new Vector2(0.8f, 0.25f));
                leaveButton.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
            }
        }

        private Button CreateButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(lobbyPanel.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = buttonColor;

            Button btn = btnObj.AddComponent<Button>();

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();

            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                btnText.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            btnText.text = text;
            btnText.fontSize = 32;
            btnText.color = Color.white;
            btnText.alignment = TMPro.TextAlignmentOptions.Center;
            btnText.fontStyle = TMPro.FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            btnObj.SetActive(true);
            return btn;
        }

        private void CreateWaitingPanel()
        {
            if (waitingPanel != null) return;

            waitingPanel = new GameObject("WaitingPanel");
            waitingPanel.transform.SetParent(lobbyPanel.transform, false);

            Image bg = waitingPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            RectTransform rect = waitingPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("WaitingText");
            textObj.transform.SetParent(waitingPanel.transform, false);
            waitingText = textObj.AddComponent<TextMeshProUGUI>();

            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                waitingText.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            waitingText.text = "Waiting for host to start game...";
            waitingText.fontSize = 36;
            waitingText.color = Color.white;
            waitingText.alignment = TMPro.TextAlignmentOptions.Center;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            waitingPanel.SetActive(false);
        }

        private void CreateErrorPanel()
        {
            if (errorPanel != null) return;

            errorPanel = new GameObject("ErrorPanel");
            errorPanel.transform.SetParent(lobbyPanel.transform, false);

            Image bg = errorPanel.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            RectTransform rect = errorPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.4f);
            rect.anchorMax = new Vector2(0.8f, 0.6f);
            rect.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("ErrorText");
            textObj.transform.SetParent(errorPanel.transform, false);
            errorText = textObj.AddComponent<TextMeshProUGUI>();

            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                errorText.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            errorText.text = "";
            errorText.fontSize = 28;
            errorText.color = Color.white;
            errorText.alignment = TMPro.TextAlignmentOptions.Center;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            errorPanel.SetActive(false);
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnStartGameClicked()
        {
            if (lobbyManager != null)
            {
                lobbyManager.CmdStartGame();
            }
        }

        private void OnReadyClicked()
        {
            if (lobbyManager != null)
            {
                isLocalPlayerReady = !isLocalPlayerReady;
                lobbyManager.CmdSetReady(isLocalPlayerReady);
                UpdateReadyButton();
            }
        }

        private void OnLeaveClicked()
        {
            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }

            HideLobby();
            
            // Return to main menu
            MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.ShowMainMenu();
            }
        }

        private void UpdateReadyButton()
        {
            if (readyButton == null) return;

            Image btnImg = readyButton.GetComponent<Image>();
            TextMeshProUGUI btnText = readyButton.GetComponentInChildren<TextMeshProUGUI>();

            if (isLocalPlayerReady)
            {
                btnImg.color = readyColor;
                if (btnText != null) btnText.text = "READY";
            }
            else
            {
                btnImg.color = notReadyColor;
                if (btnText != null) btnText.text = "NOT READY";
            }
        }

        #endregion

        #region LobbyManager Connection

        private IEnumerator ConnectToLobbyManager()
        {
            Debug.Log("🔗 [LobbyUIController] Connecting to LobbyManager...");

            // Wait for network to start
            float timeout = 15f;
            float elapsed = 0f;

            while (lobbyManager == null && elapsed < timeout)
            {
                lobbyManager = LobbyManager.Instance;
                if (lobbyManager == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }

            if (lobbyManager == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] LobbyManager not found after timeout. Will retry...");
                // Don't show error immediately - LobbyManager might spawn later
                // Start a coroutine to periodically check
                StartCoroutine(PeriodicLobbyManagerCheck());
                yield break;
            }

            Debug.Log("✅ [LobbyUIController] Connected to LobbyManager");

            // Subscribe to events
            SubscribeToLobbyManagerEvents();

            // ✅ CRITICAL: Wait a moment for host to register (if host)
            if (NetworkServer.active)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Update UI
            UpdateUI();
            
            // ✅ CRITICAL: Force refresh player list to ensure host is visible
            RefreshPlayerList();
        }

        private void SubscribeToLobbyManagerEvents()
        {
            if (lobbyManager == null) return;

            // Unsubscribe first to avoid duplicates
            lobbyManager.OnPlayerJoined -= OnPlayerJoined;
            lobbyManager.OnPlayerLeft -= OnPlayerLeft;
            lobbyManager.OnPlayerUpdated -= OnPlayerUpdated;
            lobbyManager.OnGameStarting -= OnGameStarting;

            // Subscribe
            lobbyManager.OnPlayerJoined += OnPlayerJoined;
            lobbyManager.OnPlayerLeft += OnPlayerLeft;
            lobbyManager.OnPlayerUpdated += OnPlayerUpdated;
            lobbyManager.OnGameStarting += OnGameStarting;
        }

        private IEnumerator PeriodicLobbyManagerCheck()
        {
            while (lobbyManager == null)
            {
                yield return new WaitForSeconds(0.5f);
                lobbyManager = LobbyManager.Instance;
                
                if (lobbyManager != null)
                {
                    Debug.Log("✅ [LobbyUIController] LobbyManager found! Connecting...");
                    SubscribeToLobbyManagerEvents();
                    
                    // ✅ CRITICAL: Wait a moment for host to register (if host)
                    if (NetworkServer.active)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                    
                    UpdateUI();
                    RefreshPlayerList(); // Force refresh
                    break;
                }
            }
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            Debug.Log($"🔍 [LobbyUIController] UpdateUI called - lobbyManager: {lobbyManager != null}, lobbyPanel: {lobbyPanel != null}, playerListContainer: {playerListContainer != null}");
            
            if (lobbyManager == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] UpdateUI - lobbyManager is null");
                return;
            }

            // ✅ CRITICAL: Ensure UI elements exist
            // Try to find existing playerListContainer first (might have been created but reference lost)
            // ✅ CRITICAL: Only search under lobbyPanel to avoid finding GameModeSelectionPanel's container
            if (playerListContainer == null || (playerListContainer != null && playerListContainer.gameObject == null))
            {
                // ✅ CRITICAL: Only search under lobbyPanel, not in entire scene
                Transform foundContainer = null;
                if (lobbyPanel != null)
                {
                    foundContainer = lobbyPanel.transform.Find("PlayerListContainer/ScrollView/Viewport/Content");
                }
                
                if (foundContainer != null && IsTransformUnderParent(foundContainer, lobbyPanel?.transform))
                {
                    Debug.Log("✅ [LobbyUIController] Found existing playerListContainer under lobbyPanel, re-assigning...");
                    playerListContainer = foundContainer;
                }
                else
                {
                    Debug.LogWarning("⚠️ [LobbyUIController] UpdateUI - playerListContainer is null, recreating...");
                    CreatePlayerListContainer();
                    
                    // ✅ CRITICAL: Re-verify after creation
                    if (playerListContainer == null)
                    {
                        // Try one more time to find it
                        if (lobbyPanel != null)
                        {
                            foundContainer = lobbyPanel.transform.Find("PlayerListContainer/ScrollView/Viewport/Content");
                            if (foundContainer != null && IsTransformUnderParent(foundContainer, lobbyPanel.transform))
                            {
                                playerListContainer = foundContainer;
                                Debug.Log("✅ [LobbyUIController] Found playerListContainer after creation");
                            }
                            else
                            {
                                Debug.LogError("❌ [LobbyUIController] Failed to create playerListContainer in UpdateUI!");
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogError("❌ [LobbyUIController] lobbyPanel is null! Cannot create playerListContainer!");
                            return;
                        }
                    }
                }
            }
            else if (playerListContainer != null && !IsTransformUnderParent(playerListContainer, lobbyPanel?.transform))
            {
                // ✅ CRITICAL: If container exists but is NOT under lobbyPanel, recreate it
                Debug.LogWarning("⚠️ [LobbyUIController] playerListContainer is NOT under lobbyPanel! Recreating...");
                playerListContainer = null;
                CreatePlayerListContainer();
            }

            // Update player count
            var players = lobbyManager.GetAllPlayers();
            if (playerCountText != null)
            {
                playerCountText.text = $"Players: {players.Count}/8";
            }

            // Update host controls visibility
            bool isHost = NetworkServer.active;
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isHost);
            }

            // ✅ FIX: Ready button should always be visible and interactable for all players
            if (readyButton != null)
            {
                readyButton.gameObject.SetActive(true);
                readyButton.interactable = true;
                UpdateReadyButton();
            }

            // ✅ FIX: Waiting panel should only show when game is starting, not during normal lobby
            // Hide waiting panel - players can use ready button instead
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(false);
            }

            // Refresh player list
            RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            Debug.Log($"🔍 [LobbyUIController] RefreshPlayerList called - lobbyManager: {lobbyManager != null}, container: {playerListContainer != null}");
            
            if (lobbyManager == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] Cannot refresh player list - LobbyManager is null");
                return;
            }
            
            if (playerListContainer == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] Cannot refresh player list - playerListContainer is null");
                // Try to find it
                if (lobbyPanel != null)
                {
                    Transform foundContainer = lobbyPanel.transform.Find("PlayerListContainer/ScrollView/Viewport/Content");
                    if (foundContainer != null)
                    {
                        playerListContainer = foundContainer;
                        Debug.Log($"✅ [LobbyUIController] Found playerListContainer: {playerListContainer.name}");
                    }
                    else
                    {
                        Debug.LogError("❌ [LobbyUIController] playerListContainer not found in hierarchy!");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("❌ [LobbyUIController] lobbyPanel is also null!");
                    return;
                }
            }

            // Get players
            var players = lobbyManager.GetAllPlayers();
            Debug.Log($"🔍 [LobbyUIController] Got {players.Count} players from LobbyManager, current items: {playerListItems.Count}");
            
            // ✅ PERFORMANCE: Only refresh if player count changed or items don't match
            bool needsRefresh = false;
            
            if (players.Count != playerListItems.Count)
            {
                Debug.Log($"🔍 [LobbyUIController] Player count mismatch - needs refresh");
                needsRefresh = true;
            }
            else
            {
                // Check if any player data changed (connection IDs don't match)
                for (int i = 0; i < players.Count && i < playerListItems.Count; i++)
                {
                    if (playerListItems[i] == null || 
                        !playerListItems[i].name.Contains(players[i].connectionId.ToString()))
                    {
                        Debug.Log($"🔍 [LobbyUIController] Player data mismatch at index {i} - needs refresh");
                        needsRefresh = true;
                        break;
                    }
                }
            }
            
            if (!needsRefresh)
            {
                Debug.Log($"🔍 [LobbyUIController] No refresh needed - checking visibility");
                
                // ✅ CRITICAL: Check if items are actually visible
                bool anyItemVisible = false;
                for (int i = 0; i < playerListItems.Count; i++)
                {
                    if (playerListItems[i] != null && playerListItems[i].activeSelf)
                    {
                        RectTransform itemRect = playerListItems[i].GetComponent<RectTransform>();
                        if (itemRect != null)
                        {
                            // Check if item is within viewport bounds
                            Vector3[] corners = new Vector3[4];
                            itemRect.GetWorldCorners(corners);
                            anyItemVisible = true;
                            Debug.Log($"🔍 [LobbyUIController] Item {i} ({playerListItems[i].name}) is active, position: {itemRect.anchoredPosition}, size: {itemRect.sizeDelta}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [LobbyUIController] Item {i} is null or inactive!");
                    }
                }
                
                if (!anyItemVisible && playerListItems.Count > 0)
                {
                    Debug.LogWarning($"⚠️ [LobbyUIController] Items exist but none are visible! Forcing refresh...");
                    needsRefresh = true;
                }
                else if (anyItemVisible)
                {
                    Debug.Log($"✅ [LobbyUIController] Items are visible, no refresh needed");
                    return;
                }
                else
                {
                    Debug.Log($"🔍 [LobbyUIController] No items to check, skipping");
                    return;
                }
            }
            
            Debug.Log($"🔍 [LobbyUIController] Refresh needed - proceeding with refresh");
            
            // ✅ PERFORMANCE: Only log when player count changes
            if (players.Count != lastPlayerCount)
            {
                Debug.Log($"📋 [LobbyUIController] Refreshing player list - {players.Count} players");
                lastPlayerCount = players.Count;
            }

            if (players.Count == 0)
            {
                // Only log warning once when count becomes 0
                if (lastPlayerCount != 0)
                {
                    Debug.LogWarning("⚠️ [LobbyUIController] No players in lobby! Host might not be registered yet.");
                }
            }

            // Clear existing items
            Debug.Log($"🔍 [LobbyUIController] Clearing {playerListItems.Count} existing items");
            foreach (var item in playerListItems)
            {
                if (item != null) Destroy(item);
            }
            playerListItems.Clear();

            // Create list items
            Debug.Log($"🔍 [LobbyUIController] Creating {players.Count} player list items");
            foreach (var playerData in players)
            {
                Debug.Log($"🔍 [LobbyUIController] Creating item for player: {playerData.playerName} (ID: {playerData.connectionId})");
                CreatePlayerListItem(playerData);
            }
            Debug.Log($"🔍 [LobbyUIController] Created {playerListItems.Count} items");
            
            // ✅ CRITICAL: Force layout rebuild after all items are created
            if (playerListContainer != null)
            {
                RectTransform containerRect = playerListContainer as RectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                
                // ✅ DEBUG: Log container state
                Debug.Log($"🔍 [LobbyUIController] After refresh - Container size: {containerRect.sizeDelta}, " +
                         $"Position: {containerRect.anchoredPosition}, " +
                         $"Active: {containerRect.gameObject.activeSelf}, " +
                         $"Child count: {containerRect.childCount}, " +
                         $"Items list count: {playerListItems.Count}");
                
                // ✅ CRITICAL: Also rebuild parent ScrollView if exists
                ScrollRect scrollRect = playerListContainer.GetComponentInParent<ScrollRect>();
                if (scrollRect != null && scrollRect.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                    Debug.Log($"🔍 [LobbyUIController] ScrollRect content size: {scrollRect.content.sizeDelta}, " +
                             $"Viewport: {scrollRect.viewport?.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ [LobbyUIController] ScrollRect not found!");
                }
                
                // ✅ CRITICAL: Force canvas update to ensure all items are visible
                Canvas.ForceUpdateCanvases();
                
                // ✅ DEBUG: Verify all items are active and visible
                for (int i = 0; i < playerListItems.Count; i++)
                {
                    if (playerListItems[i] != null)
                    {
                        RectTransform itemRect = playerListItems[i].GetComponent<RectTransform>();
                        Debug.Log($"🔍 [LobbyUIController] Item {i}: {playerListItems[i].name}, " +
                                 $"Active: {playerListItems[i].activeSelf}, " +
                                 $"Position: {itemRect?.anchoredPosition}, " +
                                 $"Size: {itemRect?.sizeDelta}, " +
                                 $"Parent: {playerListItems[i].transform.parent?.name}");
                    }
                }
            }

            // ✅ PERFORMANCE: Only log when items count changes
            if (playerListItems.Count != players.Count)
            {
                Debug.Log($"✅ [LobbyUIController] Player list refreshed - {playerListItems.Count} items created");
            }
        }

        private void CreatePlayerListItem(LobbyPlayerData playerData)
        {
            // ✅ CRITICAL: Verify playerListContainer is valid
            if (playerListContainer == null || playerListContainer.gameObject == null)
            {
                Debug.LogError($"❌ [LobbyUIController] Cannot create player item - playerListContainer is null! Player: {playerData.playerName}");
                return;
            }

            GameObject itemObj = new GameObject($"PlayerItem_{playerData.connectionId}");
            itemObj.transform.SetParent(playerListContainer, false);

            // ✅ CRITICAL: Ensure item is active and visible
            itemObj.SetActive(true);

            // ✅ CRITICAL FIX: Add RectTransform explicitly (Unity should add it automatically, but let's be sure)
            RectTransform rect = itemObj.AddComponent<RectTransform>();
            
            // ✅ CRITICAL FIX: For VerticalLayoutGroup children, use stretch anchors (full width, fixed height)
            // VerticalLayoutGroup automatically positions items from top to bottom
            rect.anchorMin = new Vector2(0, 1); // Left-top anchor
            rect.anchorMax = new Vector2(1, 1); // Right-top anchor
            rect.pivot = new Vector2(0.5f, 1f); // Top-center pivot
            rect.sizeDelta = new Vector2(0, 60); // Width stretches (0), height fixed (60)
            rect.localScale = Vector3.one; // ✅ CRITICAL: Ensure scale is correct
            // ✅ CRITICAL: Don't set anchoredPosition - let VerticalLayoutGroup handle it
            // ✅ CRITICAL: Set offsets to stretch width and set height
            rect.offsetMin = new Vector2(0, -60); // Left, Bottom (negative because anchor is at top)
            rect.offsetMax = new Vector2(0, 0); // Right, Top

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.5f, 0.5f, 1f); // ✅ FIX: Much brighter for visibility
            bg.raycastTarget = false; // ✅ PERFORMANCE: Don't need raycast for list items
            
            // ✅ DEBUG: Make background very visible for testing
            Debug.Log($"🔍 [LobbyUIController] Item background color: {bg.color}, enabled: {bg.enabled}");

            HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Player Name
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(itemObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            // ✅ CRITICAL: For HorizontalLayoutGroup children, use left anchor
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.sizeDelta = new Vector2(300, 0);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                nameText.font = TMPro.TMP_Settings.defaultFontAsset;
            }
            nameText.text = playerData.playerName + (playerData.isHost ? " (Host)" : "");
            nameText.fontSize = 24;
            nameText.color = new Color(1f, 1f, 1f, 1f); // ✅ FIX: Ensure fully opaque white
            nameText.alpha = 1f; // ✅ CRITICAL: Ensure alpha is 1
            nameText.alignment = TMPro.TextAlignmentOptions.Left;

            // Team Text
            GameObject teamObj = new GameObject("TeamText");
            teamObj.transform.SetParent(itemObj.transform, false);
            RectTransform teamRect = teamObj.AddComponent<RectTransform>();
            teamRect.anchorMin = new Vector2(0, 0);
            teamRect.anchorMax = new Vector2(0, 1);
            teamRect.pivot = new Vector2(0, 0.5f);
            teamRect.sizeDelta = new Vector2(150, 0);
            teamRect.anchoredPosition = Vector2.zero;
            teamRect.offsetMin = Vector2.zero;
            teamRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI teamText = teamObj.AddComponent<TextMeshProUGUI>();
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                teamText.font = TMPro.TMP_Settings.defaultFontAsset;
            }
            teamText.text = playerData.teamId == 0 ? "Team A" : playerData.teamId == 1 ? "Team B" : "No Team";
            teamText.fontSize = 20;
            teamText.color = new Color(0.7f, 0.7f, 0.7f, 1f); // ✅ FIX: Brighter gray, fully opaque
            teamText.alpha = 1f; // ✅ CRITICAL: Ensure alpha is 1
            teamText.alignment = TMPro.TextAlignmentOptions.Center;

            // Ready Status
            GameObject readyObj = new GameObject("ReadyText");
            readyObj.transform.SetParent(itemObj.transform, false);
            RectTransform readyRect = readyObj.AddComponent<RectTransform>();
            // ✅ CRITICAL: For HorizontalLayoutGroup, use left anchor (layout handles positioning)
            readyRect.anchorMin = new Vector2(0, 0);
            readyRect.anchorMax = new Vector2(0, 1);
            readyRect.pivot = new Vector2(0, 0.5f);
            readyRect.sizeDelta = new Vector2(200, 0);
            readyRect.anchoredPosition = Vector2.zero;
            readyRect.offsetMin = Vector2.zero;
            readyRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI readyText = readyObj.AddComponent<TextMeshProUGUI>();
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                readyText.font = TMPro.TMP_Settings.defaultFontAsset;
            }
            // ✅ FIX: Use ASCII characters instead of Unicode to avoid font warnings
            readyText.text = playerData.isReady ? "[READY]" : "[NOT READY]";
            readyText.fontSize = 20;
            // ✅ FIX: Ensure colors are fully opaque
            Color readyTextColor = playerData.isReady ? readyColor : notReadyColor;
            readyTextColor.a = 1f; // Force alpha to 1
            readyText.color = readyTextColor;
            readyText.alpha = 1f; // ✅ CRITICAL: Ensure alpha is 1
            readyText.alignment = TMPro.TextAlignmentOptions.Right;

            // ✅ CRITICAL: Final verification - ensure item is active and properly parented
            if (itemObj.transform.parent != playerListContainer)
            {
                Debug.LogWarning($"⚠️ [LobbyUIController] Player item parent mismatch! Re-parenting...");
                itemObj.transform.SetParent(playerListContainer, false);
            }
            
            // ✅ CRITICAL: Verify parent hierarchy
            string parentPath = GetTransformPath(itemObj.transform);
            if (!parentPath.Contains("LobbyPanel"))
            {
                Debug.LogError($"❌ [LobbyUIController] Player item is NOT under LobbyPanel! Path: {parentPath}");
                Debug.LogError($"   Expected parent: {playerListContainer?.name}, Actual parent: {itemObj.transform.parent?.name}");
            }
            
            itemObj.SetActive(true);
            playerListItems.Add(itemObj);
            
            // ✅ CRITICAL: Force layout rebuild for this item
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            
            // ✅ CRITICAL: Also rebuild parent container
            if (playerListContainer != null)
            {
                RectTransform containerRect = playerListContainer as RectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                
                // ✅ DEBUG: Log container size after rebuild
                Debug.Log($"🔍 [LobbyUIController] Container size after item creation: {containerRect.sizeDelta}, Position: {containerRect.anchoredPosition}, Active: {containerRect.gameObject.activeSelf}, Child count: {containerRect.childCount}");
            }
            
            // ✅ CRITICAL: Force canvas update to ensure visibility
            Canvas.ForceUpdateCanvases();
            
            // ✅ CRITICAL: Verify item is actually visible after all setup
            if (rect != null)
            {
                // Verify item position and size
                if (rect.sizeDelta.y <= 0)
                {
                    Debug.LogWarning($"⚠️ [LobbyUIController] Item {playerData.playerName} has zero or negative height! Forcing size...");
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 60f);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                
                // Ensure item is within reasonable bounds
                if (Mathf.Abs(rect.anchoredPosition.y) > 10000f)
                {
                    Debug.LogWarning($"⚠️ [LobbyUIController] Item {playerData.playerName} is positioned way off screen! Position: {rect.anchoredPosition}");
                }
            }
            
            // ✅ DEBUG: Always log item creation details
            Debug.Log($"✅ [LobbyUIController] Created player list item: {playerData.playerName} " +
                     $"(Parent: {itemObj.transform.parent?.name}, " +
                     $"Path: {parentPath}, " +
                     $"Active: {itemObj.activeSelf}, " +
                     $"Position: {rect.anchoredPosition}, " +
                     $"Size: {rect.sizeDelta}, " +
                     $"Scale: {rect.localScale}, " +
                     $"Item Count: {playerListItems.Count}, " +
                     $"Container Child Count: {playerListContainer?.childCount})");
        }
        
        /// <summary>
        /// ✅ HELPER: Get full path of a Transform for debugging
        /// </summary>
        private string GetTransformPath(Transform transform)
        {
            if (transform == null) return "null";
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion

        #region Event Handlers

        private void OnPlayerJoined(LobbyPlayerData playerData)
        {
            Debug.Log($"✅ [LobbyUIController] Player joined: {playerData.playerName}");
            RefreshPlayerList();
            UpdateUI();
        }

        private void OnPlayerLeft(LobbyPlayerData playerData)
        {
            Debug.Log($"✅ [LobbyUIController] Player left: {playerData.playerName}");
            RefreshPlayerList();
            UpdateUI();
        }

        private void OnPlayerUpdated(LobbyPlayerData playerData)
        {
            RefreshPlayerList();
            UpdateUI();
        }

        private void OnGameStarting()
        {
            Debug.Log("🎮 [LobbyUIController] Game starting!");
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(true);
                if (waitingText != null)
                {
                    waitingText.text = "Game starting...";
                }
            }
        }

        #endregion

        #region Utility

        private void HideGameWorld()
        {
            // Hide player weapons
            var weaponSystems = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            foreach (var weapon in weaponSystems)
            {
                if (weapon != null && weapon.gameObject != null)
                {
                    weapon.gameObject.SetActive(false);
                }
            }

            // Hide player models (but keep cameras)
            var playerControllers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in playerControllers)
            {
                if (player == null) continue;
                var visuals = player.GetComponent<Player.PlayerVisuals>();
                if (visuals != null && visuals.gameObject != null)
                {
                    Camera cam = visuals.GetComponentInChildren<Camera>();
                    if (cam == null)
                    {
                        visuals.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void EnsureCameraActive()
        {
            // ✅ CRITICAL FIX: More aggressive camera activation
            bool cameraFound = false;
            bool cameraStateChanged = false;

            // Method 1: Find local player's camera
            var playerControllers = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in playerControllers)
            {
                if (player != null && player.isLocalPlayer)
                {
                    Camera playerCamera = player.GetComponentInChildren<Camera>();
                    if (playerCamera == null)
                    {
                        var fpsController = player.GetComponent<Player.FPSController>();
                        if (fpsController != null)
                        {
                            playerCamera = fpsController.GetComponentInChildren<Camera>();
                        }
                    }

                    if (playerCamera != null)
                    {
                        if (!playerCamera.enabled || !playerCamera.gameObject.activeSelf)
                        {
                            playerCamera.enabled = true;
                            playerCamera.gameObject.SetActive(true);
                            cameraStateChanged = true;
                        }
                        cameraFound = true;
                        if (cameraStateChanged && !cameraWasActive)
                        {
                            Debug.Log($"✅ [LobbyUIController] Local player camera activated: {playerCamera.name}");
                        }
                        cameraWasActive = true;
                        return;
                    }
                }
            }

            // Method 2: Find any camera with "PlayerCamera" tag (if tag exists)
            try
            {
                GameObject[] playerCameras = GameObject.FindGameObjectsWithTag("PlayerCamera");
                foreach (var camObj in playerCameras)
                {
                    Camera cam = camObj.GetComponent<Camera>();
                    if (cam != null)
                    {
                        if (!cam.enabled || !cam.gameObject.activeSelf)
                        {
                            cam.enabled = true;
                            cam.gameObject.SetActive(true);
                            cameraStateChanged = true;
                        }
                        cameraFound = true;
                        if (cameraStateChanged && !cameraWasActive)
                        {
                            Debug.Log($"✅ [LobbyUIController] PlayerCamera tag camera activated: {cam.name}");
                        }
                        cameraWasActive = true;
                        return;
                    }
                }
            }
            catch (UnityException)
            {
                // Tag doesn't exist, skip this method
                // This is expected if "PlayerCamera" tag is not defined
            }

            // Method 3: Find any camera in scene
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                // Skip UI cameras
                if (cam.GetComponent<Canvas>() != null) continue;
                if (cam.name.Contains("UI")) continue;
                
                if (!cam.enabled || !cam.gameObject.activeSelf)
                {
                    cam.enabled = true;
                    cam.gameObject.SetActive(true);
                    cameraStateChanged = true;
                }
                cameraFound = true;
                if (cameraStateChanged && !cameraWasActive)
                {
                    Debug.Log($"✅ [LobbyUIController] Fallback camera activated: {cam.name}");
                }
                cameraWasActive = true;
                break;
            }

            if (!cameraFound)
            {
                if (!cameraWasActive)
                {
                    Debug.LogWarning("⚠️ [LobbyUIController] No camera found! Creating fallback camera...");
                    CreateFallbackCamera();
                }
                cameraWasActive = false;
            }
        }

        private void CreateFallbackCamera()
        {
            GameObject fallbackCamObj = new GameObject("FallbackCamera");
            Camera fallbackCam = fallbackCamObj.AddComponent<Camera>();
            fallbackCam.clearFlags = CameraClearFlags.SolidColor;
            fallbackCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            fallbackCam.cullingMask = 0; // Don't render anything, just prevent "No cameras" warning
            fallbackCam.depth = -100; // Behind everything
            Debug.Log("✅ [LobbyUIController] Fallback camera created");
        }

        /// <summary>
        /// Show error message (public for external calls)
        /// </summary>
        public void ShowError(string message)
        {
            if (errorPanel != null && errorText != null)
            {
                errorText.text = message;
                errorPanel.SetActive(true);

                if (errorHideCoroutine != null)
                {
                    StopCoroutine(errorHideCoroutine);
                }
                errorHideCoroutine = StartCoroutine(HideErrorAfterDelay(3f));
            }
            else
            {
                Debug.LogError($"[LobbyUIController] Error: {message}");
            }
        }

        private IEnumerator HideErrorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
            }
        }

        /// <summary>
        /// ✅ HELPER: Check if a Transform is under a specific parent
        /// </summary>
        private bool IsTransformUnderParent(Transform child, Transform parent)
        {
            if (child == null || parent == null) return false;
            
            Transform current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        #endregion

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
    }
}

