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
        [SerializeField] private LobbyUI lobbyUI;
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
            if (lobbyUI == null)
                lobbyUI = FindFirstObjectByType<LobbyUI>();
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

            // ✅ FIX: Try to find LobbyUI if not assigned
            if (lobbyUI == null)
            {
                lobbyUI = FindFirstObjectByType<LobbyUI>();
                
                // If still not found, search in Canvas hierarchy
                if (lobbyUI == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        lobbyUI = canvas.GetComponentInChildren<LobbyUI>(true); // true = include inactive
                    }
                }
                
                // If still not found, search by GameObject name
                if (lobbyUI == null)
                {
                    GameObject lobbyPanel = GameObject.Find("LobbyPanel");
                    if (lobbyPanel != null)
                    {
                        lobbyUI = lobbyPanel.GetComponent<LobbyUI>();
                    }
                }
            }

            if (lobbyUI != null)
            {
                lobbyUI.ShowPanel();
            }
            else
            {
                Debug.LogError("❌ [UIFlowManager] LobbyUI not found!");
                Debug.LogError("💡 ÇÖZÜM: Unity Editor'da Tools > Tactical Combat > 🎮 Auto Setup Lobby System çalıştırın!");
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
                var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                if (mainMenuPanel != null)
                    mainMenuPanel.gameObject.SetActive(false);
                
                var joinPanel = mainMenu.transform.Find("JoinPanel");
                if (joinPanel != null)
                    joinPanel.gameObject.SetActive(false);
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
            if (lobbyUI != null)
            {
                lobbyUI.HidePanel();
            }
        }
    }
}

