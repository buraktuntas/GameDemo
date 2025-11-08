using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Mirror;
using UnityEngine.SceneManagement;
using System.Linq;

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
        [SerializeField] private string gameSceneName = "GameScene";

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
                
                // ✅ DEBUG: Log mouse click to see if clicks are being registered
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log($"🖱️ MOUSE CLICK DETECTED at position: {Input.mousePosition}");
                    Debug.Log($"   EventSystem.current: {EventSystem.current?.name ?? "NULL"}");
                    Debug.Log($"   Cursor lock: {Cursor.lockState}, visible: {Cursor.visible}");
                    
                    // ✅ CRITICAL: Test raycast manually
                    if (EventSystem.current != null)
                    {
                        PointerEventData pointerData = new PointerEventData(EventSystem.current);
                        pointerData.position = Input.mousePosition;
                        
                        var results = new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(pointerData, results);
                        
                        Debug.Log($"   Raycast results: {results.Count}");
                        foreach (var result in results)
                        {
                            Debug.Log($"     - {result.gameObject.name} (Button: {result.gameObject.GetComponent<Button>() != null})");
                        }
                        
                        if (results.Count == 0)
                        {
                            Debug.LogError("   ❌ NO UI ELEMENTS HIT BY RAYCAST! This means clicks won't work!");
                        }
                        else
                        {
                            // ✅ CRITICAL: Check if blocker is blocking the click
                            var firstResult = results[0];
                            if (firstResult.gameObject.name.Contains("Blocker") || 
                                firstResult.gameObject.name == "Blocker")
                            {
                                Debug.LogError($"   ❌ BLOCKER IS BLOCKING CLICKS! First hit: {firstResult.gameObject.name}");
                                Debug.LogError($"   Solution: Disabling blocker GameObject...");
                                
                                // Find and disable all blockers
                                var blockers = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                                    .Where(g => g.name == "Blocker" && g.activeSelf)
                                    .ToArray();
                                
                                foreach (var blocker in blockers)
                                {
                                    // Only disable if it's not part of MainMenu
                                    if (!blocker.transform.IsChildOf(transform))
                                    {
                                        blocker.SetActive(false);
                                        Debug.Log($"   ✅ Disabled blocker: {blocker.name} at {GetFullPath(blocker.transform)}");
                                    }
                                }
                                
                                // Note: Cannot retry raycast in Update (not a coroutine)
                                // Blocker will be disabled on next click
                            }
                            
                            if (hostButton != null)
                            {
                                bool hostButtonHit = false;
                                foreach (var result in results)
                                {
                                    if (result.gameObject == hostButton.gameObject || 
                                        result.gameObject.transform.IsChildOf(hostButton.transform))
                                    {
                                        hostButtonHit = true;
                                        break;
                                    }
                                }
                                
                                if (hostButtonHit)
                                {
                                    Debug.Log($"   ✅ Host button WAS HIT by raycast!");
                                    
                                    // ✅ CRITICAL: If host button is hit but not first, manually trigger click
                                    if (results[0].gameObject != hostButton.gameObject && 
                                        !results[0].gameObject.transform.IsChildOf(hostButton.transform))
                                    {
                                        Debug.LogWarning($"   ⚠️ Host button hit but blocked by: {results[0].gameObject.name}");
                                        Debug.LogWarning($"   Manually triggering Host button click...");
                                        OnHostButtonClicked();
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"   ⚠️ Host button was NOT hit - something else is blocking it!");
                                }
                            }
                        }
                    }
                    
                    // Manually test if button would receive click
                    if (hostButton != null && hostButton.interactable)
                    {
                        Debug.Log($"   Host button is interactable and should receive click");
                    }
                }
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
                Debug.LogWarning("⚠️ [MainMenu] InputModule still null after frame wait, trying manual activation...");
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
                Debug.LogError("❌ EventSystem.current is NULL!");
                yield break;
            }
            
            var inputModule = EventSystem.current.currentInputModule;
            if (inputModule == null)
            {
                Debug.LogError("❌ EventSystem has NO InputModule! This will prevent clicks!");
                Debug.LogError("   Trying to fix...");
                
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
                    Debug.LogError("❌ NO GraphicRaycaster found! Buttons won't receive clicks!");
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
                Debug.LogError("❌ NetworkManager not found!");
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
                Debug.LogError("❌ Host button is NULL! Assign in Inspector.");
            }

            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(OnJoinButtonClicked);
                Debug.Log($"✅ Join button listener added (listeners now: {joinButton.onClick.GetPersistentEventCount()})");
            }
            else
            {
                Debug.LogError("❌ Join button is NULL! Assign in Inspector.");
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
            
            // Show main menu by default
            ShowMainMenu();
        }
        
        private void VerifyButtonConfiguration()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 VERIFYING BUTTON CONFIGURATION");
            Debug.Log("═══════════════════════════════════════");
            
            Button[] buttons = { hostButton, joinButton, quitButton, connectButton, backButton };
            string[] names = { "Host", "Join", "Quit", "Connect", "Back" };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    Debug.LogError($"❌ {names[i]} button is NULL!");
                    continue;
                }
                
                Debug.Log($"\n✅ {names[i]} Button:");
                Debug.Log($"   - Active: {buttons[i].gameObject.activeInHierarchy}");
                Debug.Log($"   - Interactable: {buttons[i].interactable}");
                Debug.Log($"   - Enabled: {buttons[i].enabled}");
                Debug.Log($"   - Listeners: {buttons[i].onClick.GetPersistentEventCount()}");
                
                Image img = buttons[i].GetComponent<Image>();
                if (img != null)
                {
                    Debug.Log($"   - Image RaycastTarget: {img.raycastTarget}");
                    if (!img.raycastTarget)
                    {
                        Debug.LogError($"      ⚠️ RAYCAST TARGET IS FALSE! Button won't receive clicks!");
                        img.raycastTarget = true; // Fix it
                        Debug.Log($"      ✅ Fixed: raycastTarget set to true");
                    }
                }
                
                // Check if button is blocked by parent
                if (!buttons[i].gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"   ⚠️ Button GameObject is INACTIVE!");
                }
            }
            
            Debug.Log("═══════════════════════════════════════\n");
        }
        
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ [MainMenu] EventSystem created");
            }
            else
            {
                Debug.Log($"✅ [MainMenu] EventSystem exists: {EventSystem.current.name}");
                
                // ✅ CRITICAL FIX: Check and fix InputModule
                var standaloneModule = EventSystem.current.GetComponent<StandaloneInputModule>();
                var inputSystemModule = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                
                if (standaloneModule == null && inputSystemModule == null)
                {
                    // NO input module - add StandaloneInputModule
                    EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
                    Debug.Log("✅ [MainMenu] StandaloneInputModule added to EventSystem");
                }
                else if (inputSystemModule != null && standaloneModule == null)
                {
                    // Only InputSystemUIInputModule exists - that's fine but log it
                    Debug.Log($"⚠️ [MainMenu] EventSystem has InputSystemUIInputModule (no StandaloneInputModule)");
                    Debug.Log($"   If clicks don't work, try disabling InputSystemUIInputModule or configure it properly");
                }
                else if (standaloneModule != null)
                {
                    // StandaloneInputModule exists - ensure it's enabled
                    if (!standaloneModule.enabled)
                    {
                        standaloneModule.enabled = true;
                        Debug.Log("✅ [MainMenu] StandaloneInputModule was disabled - now enabled");
                    }
                    else
                    {
                        Debug.Log($"✅ [MainMenu] StandaloneInputModule is enabled and active");
                    }
                }
                
                // ✅ CRITICAL FIX: Force EventSystem to initialize InputModule
                // Unity needs to call Update() on EventSystem before currentInputModule is set
                if (standaloneModule != null)
                {
                    // Force StandaloneInputModule to activate
                    EventSystem.current.UpdateModules();
                    EventSystem.current.SetSelectedGameObject(null); // Force refresh
                    
                    // Wait one frame for EventSystem to initialize (do it in coroutine)
                    StartCoroutine(EnsureInputModuleActive());
                }
                
                // Check current input module (may still be null until next frame)
                var currentModule = EventSystem.current.currentInputModule;
                if (currentModule == null)
                {
                    Debug.LogWarning("⚠️ [MainMenu] EventSystem.currentInputModule is NULL (will initialize next frame)");
                }
                else
                {
                    Debug.Log($"✅ [MainMenu] Current InputModule: {currentModule.GetType().Name}");
                }
            }
            
            // Ensure Canvas has GraphicRaycaster
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("✅ [MainMenu] GraphicRaycaster added to Canvas");
                }
                else
                {
                    Debug.Log("✅ [MainMenu] GraphicRaycaster exists");
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
                    Debug.LogWarning($"⚠️ [MainMenu] Canvas sorting order ({canvas.sortingOrder}) is lower than another canvas ({highestOrder})!");
                    Debug.LogWarning("   Menu might be blocked by another UI!");
                    canvas.sortingOrder = highestOrder + 1;
                    Debug.Log($"✅ [MainMenu] Canvas sorting order increased to {canvas.sortingOrder}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [MainMenu] No Canvas found in parent hierarchy!");
            }
        }

        private void OnHostButtonClicked()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮🎮🎮 HOST BUTTON CLICKED! 🎮🎮🎮");
            Debug.Log("═══════════════════════════════════════");

            if (networkManager != null)
            {
                // ✅ CRITICAL FIX: Host başlatılırken networkAddress'i boş bırak veya local IP kullan
                // Server tüm interface'lerde dinlemeli, sadece localhost'ta değil
                string currentAddress = networkManager.networkAddress;
                
                // Eğer localhost veya 127.0.0.1 ise, boş bırak (server tüm interface'lerde dinler)
                if (currentAddress == "localhost" || currentAddress == "127.0.0.1")
                {
                    // Boş bırak - Mirror server tüm interface'lerde dinleyecek
                    networkManager.networkAddress = "";
                    Debug.Log("✅ [MainMenu] Host: networkAddress cleared (server will listen on all interfaces)");
                }
                else
                {
                    Debug.Log($"✅ [MainMenu] Host: Using networkAddress: {currentAddress}");
                }

                // ✅ CRITICAL FIX: Transport port kontrolü
                var transport = networkManager.transport as kcp2k.KcpTransport;
                if (transport != null)
                {
                    Debug.Log($"✅ [MainMenu] Host: Transport type: KcpTransport, Port: {transport.port}");
                    Debug.Log($"   DualMode: {transport.DualMode} (should be true for IPv4/IPv6 support)");
                }
                else
                {
                    Debug.LogWarning("⚠️ [MainMenu] Host: Transport is not KcpTransport!");
                }

                networkManager.StartHost();

                // Hide Main Menu
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }

                // Show Team Selection first (Correct flow: Team → Role)
                ShowTeamSelection();
            }
        }

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
                Debug.LogWarning("⚠️ RoleSelectionUI not found! Trying Team Selection...");
                ShowTeamSelection();
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
                Debug.LogWarning("⚠️ TeamSelectionUI not found! Loading game scene...");

                // Load game scene as fallback
                if (!string.IsNullOrEmpty(gameSceneName))
                {
                    SceneManager.LoadScene(gameSceneName);
                }
            }
        }

        private void OnJoinButtonClicked()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮🎮🎮 JOIN BUTTON CLICKED! 🎮🎮🎮");
            Debug.Log("═══════════════════════════════════════");
            ShowJoinMenu();
        }

        private void OnConnectButtonClicked()
        {
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
                    Debug.LogWarning("⚠️ [MainMenu] Already connected to server!");
                    return;
                }

                if (NetworkServer.active)
                {
                    Debug.LogWarning("⚠️ [MainMenu] Server is active, cannot start client!");
                    return;
                }

                // ✅ CRITICAL FIX: Check transport before connecting
                if (networkManager.transport == null)
                {
                    Debug.LogError("❌ [MainMenu] NetworkManager has no transport! Cannot connect.");
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
                    Debug.LogWarning($"⚠️ [MainMenu] Transport type: {networkManager.transport.GetType().Name}");
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

                // Show Team Selection first (Correct flow: Team → Role)
                ShowTeamSelection();
            }
            else
            {
                Debug.LogError("❌ [MainMenu] NetworkManager is NULL! Cannot connect.");
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
                Debug.LogWarning($"⚠️ [MainMenu] Found blocking UI: {GetFullPath(blocker.transform)}");
                blocker.SetActive(false);
                Debug.Log($"✅ [MainMenu] Disabled blocker: {blocker.name}");
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
                Debug.LogWarning($"⚠️ [MainMenu] Cursor still locked after force unlock! State: {Cursor.lockState}");
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
