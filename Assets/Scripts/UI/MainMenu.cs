using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Mirror;
using UnityEngine.SceneManagement;
using System.Linq;
using TacticalCombat.Core;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Main Menu UI - Host/Join game
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject joinPanel;

        [Header("Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;

        [Header("Join Menu")]
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button backButton;

        [Header("Settings")]
        // ✅ REMOVED: gameSceneName - not currently used (lobby system handles scene transitions)
        // ✅ REMOVED: lobbySceneName - not currently used (future feature for lobby scene transition)

        private NetworkManager networkManager;
        
        private void Update()
        {
            // ✅ CRITICAL FIX: Continuously ensure cursor is unlocked when menu is visible
            if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                
                // ✅ PRODUCTION: Removed verbose debug logging from Update() (performance)
                // Debug logging moved to GameLogger.LogUI() with #if UNITY_EDITOR guards
            }
            
            if (joinPanel != null && joinPanel.activeSelf)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
        
        private System.Collections.IEnumerator EnsureInputModuleActive()
        {
            // Wait for EventSystem to initialize
            yield return null; // Wait one frame
            
            if (EventSystem.current == null) yield break;
            
            var standaloneModule = EventSystem.current.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null && !standaloneModule.enabled)
            {
                standaloneModule.enabled = true;
                Debug.Log("✅ [MainMenu] StandaloneInputModule re-enabled");
            }
            
            // Force EventSystem to activate the input module
            EventSystem.current.UpdateModules();
            
            var currentModule = EventSystem.current.currentInputModule;
            if (currentModule == null)
            {
                GameLogger.LogWarning("⚠️ [MainMenu] InputModule still null after frame wait, trying manual activation...");
                // Try to manually set the input module
                if (standaloneModule != null)
                {
                    standaloneModule.ActivateModule();
                    yield return null;
                    currentModule = EventSystem.current.currentInputModule;
                    if (currentModule != null)
                    {
                        Debug.Log($"✅ [MainMenu] InputModule activated manually: {currentModule.GetType().Name}");
                    }
                }
            }
            else
            {
                Debug.Log($"✅ [MainMenu] InputModule is now active: {currentModule.GetType().Name}");
            }
        }
        
        private System.Collections.IEnumerator TestButtonClick()
        {
            // Wait for UI to fully initialize
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("🔍 Testing button configuration after delay...");
            
            // Check if EventSystem is processing
            if (EventSystem.current == null)
            {
                GameLogger.LogError("❌ EventSystem.current is NULL!");
                yield break;
            }
            
            var inputModule = EventSystem.current.currentInputModule;
            if (inputModule == null)
            {
                GameLogger.LogError("❌ EventSystem has NO InputModule! This will prevent clicks!");
                GameLogger.LogError("   Trying to fix...");
                
                var standaloneModule = EventSystem.current.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    standaloneModule.ActivateModule();
                    yield return null;
                    inputModule = EventSystem.current.currentInputModule;
                    if (inputModule != null)
                    {
                        Debug.Log($"✅ InputModule fixed: {inputModule.GetType().Name}");
                    }
                }
            }
            else
            {
                Debug.Log($"✅ InputModule: {inputModule.GetType().Name}");
            }
            
            // Check if buttons can be clicked
            if (hostButton != null)
            {
                var raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                Debug.Log($"✅ Found {raycasters.Length} GraphicRaycaster(s)");
                
                if (raycasters.Length == 0)
                {
                    GameLogger.LogError("❌ NO GraphicRaycaster found! Buttons won't receive clicks!");
                }
            }
        }

        private void Start()
        {
            // ✅ CRITICAL FIX: Ensure EventSystem exists FIRST
            EnsureEventSystem();
            
            // Get NetworkManager
            networkManager = NetworkManager.singleton;
            if (networkManager == null)
            {
                GameLogger.LogError("❌ NetworkManager not found!");
                return;
            }

            // Disable NetworkManager's default HUD
            if (networkManager != null)
            {
                // Completely destroy the NetworkManagerHUD component
                var hudComponent = networkManager.GetComponent<Mirror.NetworkManagerHUD>();
                if (hudComponent != null)
                {
                    Destroy(hudComponent);
                    Debug.Log("🚫 NetworkManagerHUD destroyed");
                }
            }

            // ✅ CRITICAL FIX: Remove all listeners first to avoid duplicates
            if (hostButton != null)
            {
                hostButton.onClick.RemoveAllListeners();
                hostButton.onClick.AddListener(OnHostButtonClicked);
                Debug.Log($"✅ Host button listener added (listeners now: {hostButton.onClick.GetPersistentEventCount()})");
            }
            else
            {
                GameLogger.LogError("❌ Host button is NULL! Assign in Inspector.");
            }

            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(OnJoinButtonClicked);
                GameLogger.LogUI($"✅ Join button listener added (listeners now: {joinButton.onClick.GetPersistentEventCount()})");
            }
            else
            {
                GameLogger.LogError("❌ Join button is NULL! Assign in Inspector.");
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitButtonClicked);
                Debug.Log($"✅ Quit button listener added (listeners now: {quitButton.onClick.GetPersistentEventCount()})");
            }

            if (connectButton != null)
            {
                connectButton.onClick.RemoveAllListeners();
                connectButton.onClick.AddListener(OnConnectButtonClicked);
                Debug.Log($"✅ Connect button listener added");
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
                Debug.Log($"✅ Back button listener added");
            }
            
            // ✅ CRITICAL FIX: Test button click manually
            StartCoroutine(TestButtonClick());

            // ✅ CRITICAL FIX: Verify buttons are properly configured
            VerifyButtonConfiguration();
            
            // ✅ CRITICAL: Hide LobbyUI at start (MainMenu should be visible)
            HideLobbyUIAtStart();

            // Show main menu by default
            ShowMainMenu();
        }

        /// <summary>
        /// ✅ NEW: Hide LobbyUI at start to prevent overlap
        /// </summary>
        private void HideLobbyUIAtStart()
        {
            var lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.HidePanel();
                Debug.Log("✅ [MainMenu] LobbyUI hidden at start");
            }

            // Also hide GameModeSelection
            var gameModeSelection = FindFirstObjectByType<GameModeSelectionUI>();
            if (gameModeSelection != null)
            {
                gameModeSelection.HidePanel();
            }
        }
        
        private void VerifyButtonConfiguration()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameLogger.LogUI("Verifying button configuration");
            #endif
            
            Button[] buttons = { hostButton, joinButton, quitButton, connectButton, backButton };
            string[] names = { "Host", "Join", "Quit", "Connect", "Back" };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    GameLogger.LogError($"{names[i]} button is NULL!");
                    continue;
                }
                
                Image img = buttons[i].GetComponent<Image>();
                if (img != null && !img.raycastTarget)
                {
                    GameLogger.LogError($"RAYCAST TARGET IS FALSE! Button won't receive clicks!");
                    img.raycastTarget = true; // Fix it
                }
                
                // Check if button is blocked by parent
                if (!buttons[i].gameObject.activeInHierarchy)
                {
                    GameLogger.LogWarning($"Button GameObject is INACTIVE!");
                }
            }
        }
        
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                GameLogger.LogUI("EventSystem created");
            }
            else
            {
                // ✅ CRITICAL FIX: Check and fix InputModule
                var standaloneModule = EventSystem.current.GetComponent<StandaloneInputModule>();
                var inputSystemModule = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                
                if (standaloneModule == null && inputSystemModule == null)
                {
                    // NO input module - add StandaloneInputModule
                    EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
                    GameLogger.LogUI("StandaloneInputModule added to EventSystem");
                }
                else if (inputSystemModule != null && standaloneModule == null)
                {
                    // Only InputSystemUIInputModule exists - that's fine but log it
                    GameLogger.LogWarning("EventSystem has InputSystemUIInputModule (no StandaloneInputModule). If clicks don't work, try disabling InputSystemUIInputModule or configure it properly");
                }
                else if (standaloneModule != null && !standaloneModule.enabled)
                {
                    standaloneModule.enabled = true;
                    GameLogger.LogUI("StandaloneInputModule was disabled - now enabled");
                }
                
                // ✅ CRITICAL FIX: Force EventSystem to initialize InputModule
                if (standaloneModule != null)
                {
                    EventSystem.current.UpdateModules();
                    EventSystem.current.SetSelectedGameObject(null); // Force refresh
                    StartCoroutine(EnsureInputModuleActive());
                }
            }
            
            // Ensure Canvas has GraphicRaycaster
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                
                // ✅ CRITICAL: Check Canvas sorting order - menu should be on top
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                int highestOrder = int.MinValue;
                foreach (var c in allCanvases)
                {
                    if (c.sortingOrder > highestOrder)
                        highestOrder = c.sortingOrder;
                }
                
                if (canvas.sortingOrder < highestOrder)
                {
                    GameLogger.LogWarning($"Canvas sorting order ({canvas.sortingOrder}) is lower than another canvas ({highestOrder})! Menu might be blocked.");
                    canvas.sortingOrder = highestOrder + 1;
                }
            }
            else
            {
                GameLogger.LogWarning("No Canvas found in parent hierarchy!");
            }
        }

        private void OnHostButtonClicked()
        {
            // ✅ NEW: Play UI sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("button_click", 0.8f);
            }
            
            GameLogger.LogUI("Host button clicked - showing lobby");

            // Hide Main Menu
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
            if (joinPanel != null)
            {
                joinPanel.SetActive(false);
            }

            // ✅ SIMPLE FLOW: Direct to Lobby (game mode selection is in LobbyUI)
            ShowLobby();
        }

        private System.Collections.IEnumerator ShowGameModeSelectionDelayed()
        {
            // Wait a frame to ensure network is initialized
            yield return null;
            
            // ✅ CRITICAL: Use UIFlowManager for clean flow
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.ShowGameModeSelection();
            }
            else
            {
                // Fallback: Direct show
                ShowGameModeSelection();
            }
        }

        /// <summary>
        /// ✅ NEW: Show Game Mode Selection (Host only)
        /// </summary>
        private void ShowGameModeSelection()
        {
            GameLogger.LogUI("Looking for GameModeSelectionUI");
            
            // ✅ STEP 1: Hide MainMenu panels COMPLETELY
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
            if (joinPanel != null)
            {
                joinPanel.SetActive(false);
            }
            
            // ✅ STEP 2: Hide MainMenu root GameObject if it exists
            var mainMenuRoot = transform.Find("MainMenu");
            if (mainMenuRoot != null)
            {
                mainMenuRoot.gameObject.SetActive(false);
            }
            
            // ✅ STEP 3: Hide this GameObject's Canvas if it exists
            Canvas mainMenuCanvas = GetComponentInParent<Canvas>();
            if (mainMenuCanvas != null && mainMenuCanvas.gameObject.name.Contains("MainMenu"))
            {
                mainMenuCanvas.gameObject.SetActive(false);
            }
            
            // ✅ STEP 4: Try multiple methods to find GameModeSelectionUI
            GameModeSelectionUI gameModeSelection = null;
            
            // Method 1: FindFirstObjectByType (finds active components)
            gameModeSelection = FindFirstObjectByType<GameModeSelectionUI>();
            
            // Method 2: If not found, search by GameObject name (even if inactive)
            if (gameModeSelection == null)
            {
                GameObject gameModePanel = GameObject.Find("GameModeSelectionPanel");
                if (gameModePanel != null)
                {
                    Debug.Log($"✅ Found GameModeSelectionPanel GameObject");
                    gameModeSelection = gameModePanel.GetComponent<GameModeSelectionUI>();
                    if (gameModeSelection == null)
                    {
                        Debug.Log("   Adding GameModeSelectionUI component...");
                        gameModeSelection = gameModePanel.AddComponent<GameModeSelectionUI>();
                    }
                }
            }
            
            // Method 3: Search in Canvas hierarchy
            if (gameModeSelection == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    gameModeSelection = canvas.GetComponentInChildren<GameModeSelectionUI>(true); // true = include inactive
                }
            }
            
            if (gameModeSelection != null)
            {
                GameLogger.LogUI($"Found GameModeSelectionUI at: {gameModeSelection.gameObject.name}");
                gameModeSelection.ShowPanel();
            }
            else
            {
                GameLogger.LogWarning("GameModeSelectionUI not found! Going directly to Lobby...");
                ShowLobby();
            }
        }

        /// <summary>
        /// ✅ NEW: Show Lobby UI
        /// </summary>
        private void ShowLobby()
        {
            GameLogger.LogUI("Looking for LobbyUI");
            
            // Try multiple methods to find LobbyUI
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
            
            // Method 2: Search by GameObject name (even if inactive)
            if (lobbyUI == null)
            {
                GameObject lobbyPanel = GameObject.Find("LobbyPanel");
                if (lobbyPanel != null)
                {
                    lobbyUI = lobbyPanel.GetComponent<LobbyUI>();
                }
            }
            
            // Method 3: Search in Canvas hierarchy
            if (lobbyUI == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    lobbyUI = canvas.GetComponentInChildren<LobbyUI>(true); // true = include inactive
                }
            }
            
            if (lobbyUI != null)
            {
                GameLogger.LogUI($"Found LobbyUI at: {lobbyUI.gameObject.name}");
                lobbyUI.ShowPanel();
            }
            else
            {
                GameLogger.LogError("LobbyUI not found! Cannot proceed to lobby. Please ensure LobbyPanel exists in the scene with LobbyUI component.");
                // Don't load game scene - stay in menu
            }
        }

        // ✅ OLD METHODS - Kept for compatibility but not used in new flow
        private void ShowRoleSelection()
        {
            // Role Selection UI'ını göster
            var roleSelection = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
            if (roleSelection != null)
            {
                roleSelection.ShowPanel();
                Debug.Log("→ Opening Role Selection...");
            }
            else
            {
                GameLogger.LogWarning("⚠️ RoleSelectionUI not found!");
            }
        }

        private void ShowTeamSelection()
        {
            // Team Selection UI'ını göster
            var teamSelection = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
            if (teamSelection != null)
            {
                teamSelection.ShowPanel();
                Debug.Log("→ Opening Team Selection...");
            }
            else
            {
                GameLogger.LogWarning("⚠️ TeamSelectionUI not found!");
            }
        }

        private void OnJoinButtonClicked()
        {
            // ✅ NEW: Play UI sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("button_click", 0.8f);
            }
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮🎮🎮 JOIN BUTTON CLICKED! 🎮🎮🎮");
            Debug.Log("═══════════════════════════════════════");
            ShowJoinMenu();
        }

        private void OnConnectButtonClicked()
        {
            // ✅ NEW: Play UI sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("button_click", 0.8f);
            }
            
            string ipAddress = ipAddressInput != null ? ipAddressInput.text : "localhost";

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "localhost";
            }

            // ✅ CRITICAL FIX: IP adresini temizle (boşlukları kaldır)
            ipAddress = ipAddress.Trim();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"🎮 [MainMenu] Connecting to {ipAddress}...");
            Debug.Log("═══════════════════════════════════════");

            if (networkManager != null)
            {
                // ✅ CRITICAL FIX: Check NetworkManager state before connecting
                if (NetworkClient.isConnected)
                {
                    GameLogger.LogWarning("⚠️ [MainMenu] Already connected to server!");
                    return;
                }

                if (NetworkServer.active)
                {
                    GameLogger.LogWarning("⚠️ [MainMenu] Server is active, cannot start client!");
                    return;
                }

                // ✅ CRITICAL FIX: Check transport before connecting
                if (networkManager.transport == null)
                {
                    GameLogger.LogError("❌ [MainMenu] NetworkManager has no transport! Cannot connect.");
                    return;
                }

                var transport = networkManager.transport as kcp2k.KcpTransport;
                if (transport != null)
                {
                    Debug.Log($"✅ [MainMenu] Transport: KcpTransport");
                    Debug.Log($"✅ [MainMenu] Port: {transport.port}");
                    Debug.Log($"✅ [MainMenu] DualMode: {transport.DualMode}");
                }
                else
                {
                    GameLogger.LogWarning($"⚠️ [MainMenu] Transport type: {networkManager.transport.GetType().Name}");
                }

                Debug.Log($"✅ [MainMenu] Setting network address to: {ipAddress}");
                networkManager.networkAddress = ipAddress;
                
                Debug.Log($"✅ [MainMenu] Starting client connection...");
                Debug.Log($"   Target: {ipAddress}:{(transport != null ? transport.port.ToString() : "7777")}");
                
                networkManager.StartClient();

                // Hide Main Menu
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }
                if (joinPanel != null)
                {
                    joinPanel.SetActive(false);
                }

                // ✅ NEW FLOW: Client → Direct to Lobby
                ShowLobby();
            }
            else
            {
                GameLogger.LogError("❌ [MainMenu] NetworkManager is NULL! Cannot connect.");
            }
        }

        private void OnBackButtonClicked()
        {
            ShowMainMenu();
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("🎮 Quitting game...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (joinPanel != null)
            {
                joinPanel.SetActive(false);
            }

            // ✅ NEW: Hide other UIs when showing main menu
            HideOtherUIs();

            // ✅ CRITICAL FIX: Disable any blockers that might be blocking clicks
            DisableOtherUIBlockers();

            // ✅ CRITICAL FIX: Ensure EventSystem before showing menu
            EnsureEventSystem();

            // ✅ CRITICAL FIX: Force cursor unlock multiple times
            StartCoroutine(ForceCursorUnlock());
            
            // ✅ CRITICAL FIX: Hide crosshair when menu is open!
            HideCrosshair();
            
            Debug.Log("✅ [MainMenu] Menu shown, cursor unlocked");
        }

        /// <summary>
        /// ✅ NEW: Hide other UIs when showing main menu
        /// </summary>
        private void HideOtherUIs()
        {
            // Hide LobbyUI
            var lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.HidePanel();
            }

            // Hide GameModeSelection
            var gameModeSelection = FindFirstObjectByType<GameModeSelectionUI>();
            if (gameModeSelection != null)
            {
                gameModeSelection.HidePanel();
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
        
        private void DisableOtherUIBlockers()
        {
            // Find all "Blocker" GameObjects that might be blocking MainMenu
            var blockers = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(g => g.name == "Blocker" || g.name.Contains("Blocker"))
                .Where(g => g.activeSelf)
                .Where(g => !g.transform.IsChildOf(transform)) // Don't disable MainMenu's own blockers
                .ToArray();
            
            foreach (var blocker in blockers)
            {
                GameLogger.LogWarning($"⚠️ [MainMenu] Found blocking UI: {GetFullPath(blocker.transform)}");
                blocker.SetActive(false);
                GameLogger.LogUI($"✅ [MainMenu] Disabled blocker: {blocker.name}");
            }
            
            // Also check for TeamSelectionUI and RoleSelectionUI blockers
            var teamSelection = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
            if (teamSelection != null)
            {
                var teamBlocker = teamSelection.transform.Find("Blocker");
                if (teamBlocker != null && teamBlocker.gameObject.activeSelf)
                {
                    teamBlocker.gameObject.SetActive(false);
                    Debug.Log("✅ [MainMenu] Disabled TeamSelectionUI blocker");
                }
            }
            
            var roleSelection = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
            if (roleSelection != null)
            {
                var roleBlocker = roleSelection.transform.Find("Blocker");
                if (roleBlocker != null && roleBlocker.gameObject.activeSelf)
                {
                    roleBlocker.gameObject.SetActive(false);
                    Debug.Log("✅ [MainMenu] Disabled RoleSelectionUI blocker");
                }
            }
        }
        
        private string GetFullPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
        
        private System.Collections.IEnumerator ForceCursorUnlock()
        {
            // Force unlock cursor multiple times to override any locks
            for (int i = 0; i < 5; i++)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Final check
            if (Cursor.lockState != CursorLockMode.None)
            {
                GameLogger.LogWarning($"⚠️ [MainMenu] Cursor still locked after force unlock! State: {Cursor.lockState}");
            }
        }

        private void HideCrosshair()
        {
            // Find and hide all crosshair/combat UI elements
            var combatUI = FindFirstObjectByType<TacticalCombat.UI.CombatUI>();
            if (combatUI != null)
            {
                combatUI.gameObject.SetActive(false);
                Debug.Log("✅ CombatUI hidden (crosshair blocking clicks is gone!)");
            }

            // Also find standalone crosshair controller
            var crosshairController = FindFirstObjectByType<TacticalCombat.UI.CrosshairController>();
            if (crosshairController != null)
            {
                crosshairController.gameObject.SetActive(false);
                Debug.Log("✅ Crosshair hidden");
            }

            // Find SimpleCrosshair if exists
            var simpleCrosshair = FindFirstObjectByType<TacticalCombat.UI.SimpleCrosshair>();
            if (simpleCrosshair != null)
            {
                simpleCrosshair.gameObject.SetActive(false);
                Debug.Log("✅ SimpleCrosshair hidden");
            }
        }

        private void ShowJoinMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (joinPanel != null)
            {
                joinPanel.SetActive(true);
            }

            // CRITICAL: Show and unlock cursor for menu interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Set default IP
            if (ipAddressInput != null && string.IsNullOrEmpty(ipAddressInput.text))
            {
                ipAddressInput.text = "localhost";
            }
        }
        
        /// <summary>
        /// ✅ PUBLIC: Check if menu panel is currently visible/active
        /// Used by FPSController to determine if UI is open
        /// </summary>
        public bool IsPanelOpen()
        {
            // Check if main menu panel OR join panel is open
            return (mainMenuPanel != null && mainMenuPanel.activeSelf) ||
                   (joinPanel != null && joinPanel.activeSelf);
        }
    }
}
