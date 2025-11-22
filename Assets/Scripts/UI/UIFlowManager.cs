using UnityEngine;
using Mirror;
using TacticalCombat.Network;

namespace TacticalCombat.UI
{
    /// <summary>
    /// ✅ NEW: Centralized UI Flow Manager
    /// Manages the complete UI flow: MainMenu → GameModeSelection → Lobby → Game
    /// GDD-compliant flow: Clean, simple, no code chaos
    /// </summary>
    public class UIFlowManager : MonoBehaviour
    {
        public static UIFlowManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private GameModeSelectionUI gameModeSelection;
        // ✅ REMOVED: Old LobbyUI reference - now using LobbyUIController
        // [SerializeField] private LobbyUI lobbyUI;
        [SerializeField] private EndGameScoreboard endGameScoreboard;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find UI components if not assigned
            if (mainMenu == null)
                mainMenu = FindFirstObjectByType<MainMenu>();
            if (gameModeSelection == null)
                gameModeSelection = FindFirstObjectByType<GameModeSelectionUI>();
            // ✅ REMOVED: Old LobbyUI - now using LobbyUIController (singleton, no need to find)
            if (endGameScoreboard == null)
                endGameScoreboard = FindFirstObjectByType<EndGameScoreboard>();

            // Initial state: Show MainMenu, hide everything else
            ShowMainMenu();
        }

        /// <summary>
        /// ✅ FLOW 1: Show Main Menu (initial state)
        /// </summary>
        public void ShowMainMenu()
        {
            Debug.Log("🎮 [UIFlowManager] Showing MainMenu");
            
            if (mainMenu != null)
            {
                // MainMenu will handle its own ShowPanel logic
            }

            HideGameModeSelection();
            HideLobby();
        }

        /// <summary>
        /// ✅ FLOW 2: Host clicked → Show Game Mode Selection
        /// </summary>
        public void ShowGameModeSelection()
        {
            Debug.Log("🎮 [UIFlowManager] Showing GameModeSelection");
            
            HideMainMenu();
            HideLobby();

            if (gameModeSelection != null)
            {
                gameModeSelection.ShowPanel();
            }
            else
            {
                Debug.LogError("❌ [UIFlowManager] GameModeSelectionUI not found!");
            }
        }

        /// <summary>
        /// ✅ FLOW 3: Game Mode confirmed → Show Lobby
        /// </summary>
        public void ShowLobby()
        {
            Debug.Log("🎮 [UIFlowManager] Showing Lobby");
            
            HideMainMenu();
            // ✅ FIX: DON'T hide GameModeSelection - keep it visible with lobby
            // HideGameModeSelection(); // REMOVED - keep GameModeSelection visible

            // ✅ NEW: Use LobbyUIController (singleton)
            LobbyUIController lobbyController = LobbyUIController.Instance;
            if (lobbyController != null)
            {
                lobbyController.ShowLobby();
            }
            else
            {
                Debug.LogWarning("⚠️ [UIFlowManager] LobbyUIController not found - it will be created automatically when needed");
            }
        }

        /// <summary>
        /// ✅ FLOW 4: Game started → Hide all UI (game HUD will show)
        /// </summary>
        public void HideAllUI()
        {
            Debug.Log("🎮 [UIFlowManager] Hiding all UI (game started)");
            
            HideMainMenu();
            HideGameModeSelection();
            HideLobby();
            
            // ✅ CRITICAL FIX: Ensure GameHUD is shown when game starts
            // GameHUD will be shown by OnPhaseChanged event, but we ensure it's active here too
            var gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                // GameHUD will show itself when phase changes to Build/Combat
                // But we ensure the GameObject is active
                if (!gameHUD.gameObject.activeSelf)
                {
                    gameHUD.gameObject.SetActive(true);
                    Debug.Log("✅ [UIFlowManager] GameHUD GameObject activated");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [UIFlowManager] GameHUD not found in scene!");
            }
        }

        /// <summary>
        /// ✅ FLOW 5: Match ended → Show EndGameScoreboard
        /// </summary>
        public void ShowEndGameScoreboard()
        {
            Debug.Log("🎮 [UIFlowManager] Showing EndGameScoreboard");
            
            HideMainMenu();
            HideGameModeSelection();
            HideLobby();

            var endScoreboard = FindFirstObjectByType<EndGameScoreboard>();
            if (endScoreboard != null)
            {
                // EndGameScoreboard will be shown by MatchManager.RpcOnMatchWon
                // This method is here for future use if needed
            }
        }

        // Private helper methods
        private void HideMainMenu()
        {
            if (mainMenu != null)
            {
                // ✅ CRITICAL FIX: Hide MainMenu panels
                var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                if (mainMenuPanel != null)
                    mainMenuPanel.gameObject.SetActive(false);
                
                var joinPanel = mainMenu.transform.Find("JoinPanel");
                if (joinPanel != null)
                    joinPanel.gameObject.SetActive(false);
                
                // ✅ CRITICAL FIX: Also hide/disable the MainMenu GameObject itself
                // This prevents MainMenu Canvas from blocking the game view
                if (mainMenu.gameObject.activeSelf)
                {
                    mainMenu.gameObject.SetActive(false);
                    Debug.Log("✅ [UIFlowManager] MainMenu GameObject disabled");
                }
                
                // ✅ CRITICAL FIX: Also disable MainMenu Canvas if it exists
                Canvas mainMenuCanvas = mainMenu.GetComponent<Canvas>();
                if (mainMenuCanvas != null && mainMenuCanvas.gameObject.activeSelf)
                {
                    mainMenuCanvas.gameObject.SetActive(false);
                    Debug.Log("✅ [UIFlowManager] MainMenu Canvas disabled");
                }
            }
            else
            {
                // ✅ CRITICAL FIX: Fallback - find MainMenu by type if reference is null
                var foundMainMenu = FindFirstObjectByType<MainMenu>();
                if (foundMainMenu != null)
                {
                    if (foundMainMenu.gameObject.activeSelf)
                    {
                        foundMainMenu.gameObject.SetActive(false);
                        Debug.Log("✅ [UIFlowManager] MainMenu GameObject disabled (found by type)");
                    }
                }
            }
        }

        private void HideGameModeSelection()
        {
            if (gameModeSelection != null)
            {
                gameModeSelection.HidePanel();
            }
        }

        private void HideLobby()
        {
            // ✅ NEW: Use LobbyUIController (singleton)
            LobbyUIController lobbyController = LobbyUIController.Instance;
            if (lobbyController != null)
            {
                lobbyController.HideLobby();
            }
        }
    }
}

