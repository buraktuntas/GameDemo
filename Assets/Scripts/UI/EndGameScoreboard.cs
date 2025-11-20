using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;
using System.Collections.Generic;

namespace TacticalCombat.UI
{
    /// <summary>
    /// End-game scoreboard with awards - shown at match end
    /// </summary>
    public class EndGameScoreboard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerEntryPrefab;

        [Header("Winner Display")]
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private GameObject winnerPanel;

        [Header("Awards")]
        [SerializeField] private Transform awardsContent;
        [SerializeField] private GameObject awardEntryPrefab;

        [Header("Restart Button")]
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI restartButtonText;
        
        [Header("Return to Lobby Button")]
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private TextMeshProUGUI returnToLobbyButtonText;

        [Header("Stats Columns")]
        [SerializeField] private TextMeshProUGUI headerText; // "K/D/A/Structures/Traps/Captures/Score"

        private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();
        private Dictionary<AwardType, GameObject> awardEntries = new Dictionary<AwardType, GameObject>();
        
        // ✅ PERFORMANCE FIX: Cache text components per entry to avoid GetComponent calls
        private Dictionary<ulong, PlayerEntryComponents> cachedEntryComponents = new Dictionary<ulong, PlayerEntryComponents>();
        
        // ✅ PERFORMANCE FIX: Cache string builder to avoid GC allocation
        private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(64);
        
        // Helper class to cache entry components
        private class PlayerEntryComponents
        {
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI killsText;
            public TextMeshProUGUI deathsText;
            public TextMeshProUGUI assistsText;
            public TextMeshProUGUI structuresText;
            public TextMeshProUGUI trapKillsText;
            public TextMeshProUGUI capturesText;
            public TextMeshProUGUI defenseTimeText;
            public TextMeshProUGUI scoreText;
        }

        private void Start()
        {
            // Hide initially
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }

            // Subscribe to match end event
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnMatchWonEvent += OnMatchWon;
            }

            // Setup restart button
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            if (restartButtonText != null)
            {
                restartButtonText.text = "YENİDEN OYNA";
            }
            
            // ✅ NEW: Setup return to lobby button
            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
            }
            
            if (returnToLobbyButtonText != null)
            {
                returnToLobbyButtonText.text = "LOBBY'YE DÖN";
            }
        }

        private void OnMatchWon(Team winner)
        {
            ShowScoreboard(winner);
        }

        public void ShowScoreboard(Team winner, Dictionary<ulong, AwardType> awards = null)
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(true);
            }

            // Show winner
            if (winnerText != null)
            {
                string winnerName = winner == Team.TeamA ? "TEAM A" : winner == Team.TeamB ? "TEAM B" : "DRAW";
                winnerText.text = $"{winnerName} WINS!";
            }

            if (winnerPanel != null)
            {
                winnerPanel.SetActive(true);
            }

            // Update player list
            UpdatePlayerList();

            // Show awards
            if (awards != null)
            {
                ShowAwards(awards);
            }
            
            // ✅ NEW: Show restart button (only for host)
            if (restartButton != null)
            {
                bool isHost = MatchManager.Instance != null && MatchManager.Instance.isServer;
                restartButton.gameObject.SetActive(isHost);
                restartButton.interactable = isHost;
            }
            
            // ✅ NEW: Show return to lobby button (for everyone)
            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(true);
                returnToLobbyButton.interactable = true;
            }
        }
        
        /// <summary>
        /// ✅ NEW: Hide scoreboard
        /// </summary>
        public void HideScoreboard()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }

        private void UpdatePlayerList()
        {
            // Clear old entries
            ClearPlayerEntries();

            // ✅ PERFORMANCE FIX: Use NetworkServer.spawned or NetworkClient.spawned instead of FindObjectsByType
            List<PlayerController> playersList = new List<PlayerController>();
            
            if (NetworkServer.active)
            {
                foreach (var kvp in NetworkServer.spawned)
                {
                    var player = kvp.Value.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        playersList.Add(player);
                    }
                }
            }
            else if (NetworkClient.active)
            {
                foreach (var kvp in NetworkClient.spawned)
                {
                    var player = kvp.Value.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        playersList.Add(player);
                    }
                }
            }
            
            PlayerController[] players = playersList.ToArray();

            // Sort by total score
            System.Array.Sort(players, (a, b) =>
            {
                // ✅ FIX: Use client-side stats cache
                var statsA = MatchManager.Instance?.GetPlayerMatchStatsClient(a.netId);
                var statsB = MatchManager.Instance?.GetPlayerMatchStatsClient(b.netId);
                int scoreA = statsA?.totalScore ?? 0;
                int scoreB = statsB?.totalScore ?? 0;
                return scoreB.CompareTo(scoreA); // Descending order
            });
            
            // ✅ NEW: Request stats if not cached (first time)
            if (MatchManager.Instance != null && NetworkClient.active)
            {
                MatchManager.Instance.CmdRequestAllPlayerStats();
            }

            // Create entries
            foreach (var player in players)
            {
                if (playerListContent != null && playerEntryPrefab != null)
                {
                    GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
                    playerEntries[player.netId] = entry;

                    // ✅ PERFORMANCE FIX: Cache components on first creation
                    if (!cachedEntryComponents.ContainsKey(player.netId))
                    {
                        var components = new PlayerEntryComponents
                        {
                            nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                            killsText = entry.transform.Find("KillsText")?.GetComponent<TextMeshProUGUI>(),
                            deathsText = entry.transform.Find("DeathsText")?.GetComponent<TextMeshProUGUI>(),
                            assistsText = entry.transform.Find("AssistsText")?.GetComponent<TextMeshProUGUI>(),
                            structuresText = entry.transform.Find("StructuresText")?.GetComponent<TextMeshProUGUI>(),
                            trapKillsText = entry.transform.Find("TrapKillsText")?.GetComponent<TextMeshProUGUI>(),
                            capturesText = entry.transform.Find("CapturesText")?.GetComponent<TextMeshProUGUI>(),
                            defenseTimeText = entry.transform.Find("DefenseTimeText")?.GetComponent<TextMeshProUGUI>(),
                            scoreText = entry.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>()
                        };
                        cachedEntryComponents[player.netId] = components;
                    }

                    // Update entry data
                    UpdatePlayerEntry(entry, player);
                }
            }
        }

        private void UpdatePlayerEntry(GameObject entry, PlayerController player)
        {
            // ✅ PERFORMANCE FIX: Use cached components instead of GetComponent every update
            if (!cachedEntryComponents.TryGetValue(player.netId, out var components))
            {
                // Fallback: cache components if not already cached
                components = new PlayerEntryComponents
                {
                    nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                    killsText = entry.transform.Find("KillsText")?.GetComponent<TextMeshProUGUI>(),
                    deathsText = entry.transform.Find("DeathsText")?.GetComponent<TextMeshProUGUI>(),
                    assistsText = entry.transform.Find("AssistsText")?.GetComponent<TextMeshProUGUI>(),
                    structuresText = entry.transform.Find("StructuresText")?.GetComponent<TextMeshProUGUI>(),
                    trapKillsText = entry.transform.Find("TrapKillsText")?.GetComponent<TextMeshProUGUI>(),
                    capturesText = entry.transform.Find("CapturesText")?.GetComponent<TextMeshProUGUI>(),
                    defenseTimeText = entry.transform.Find("DefenseTimeText")?.GetComponent<TextMeshProUGUI>(),
                    scoreText = entry.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>()
                };
                cachedEntryComponents[player.netId] = components;
            }

            // ✅ FIX: Use client-side stats cache
            var stats = MatchManager.Instance?.GetPlayerMatchStatsClient(player.netId);
            int kills = stats?.kills ?? 0;
            int deaths = stats?.deaths ?? 0;
            int assists = stats?.assists ?? 0;
            int structures = stats?.structuresBuilt ?? 0;
            int trapKills = stats?.trapKills ?? 0;
            int captures = stats?.captures ?? 0;
            float defenseTime = stats?.defenseTime ?? 0f;
            int totalScore = stats?.totalScore ?? 0;

            // ✅ PERFORMANCE FIX: Use StringBuilder to avoid GC allocation
            stringBuilder.Clear();
            stringBuilder.Append("Player ");
            stringBuilder.Append(player.netId);
            string playerName = stringBuilder.ToString();

            // ✅ PERFORMANCE FIX: Use cached components and StringBuilder
            if (components.nameText != null)
            {
                components.nameText.text = playerName;
                if (player.isLocalPlayer)
                {
                    components.nameText.color = Color.yellow;
                    components.nameText.fontStyle = FontStyles.Bold;
                }
            }

            if (components.killsText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(kills);
                components.killsText.text = stringBuilder.ToString();
            }
            if (components.deathsText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(deaths);
                components.deathsText.text = stringBuilder.ToString();
            }
            if (components.assistsText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(assists);
                components.assistsText.text = stringBuilder.ToString();
            }
            if (components.structuresText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(structures);
                components.structuresText.text = stringBuilder.ToString();
            }
            if (components.trapKillsText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(trapKills);
                components.trapKillsText.text = stringBuilder.ToString();
            }
            if (components.capturesText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(captures);
                components.capturesText.text = stringBuilder.ToString();
            }
            if (components.defenseTimeText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(defenseTime.ToString("F1"));
                stringBuilder.Append("s");
                components.defenseTimeText.text = stringBuilder.ToString();
            }
            if (components.scoreText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(totalScore);
                components.scoreText.text = stringBuilder.ToString();
                components.scoreText.color = Color.green;
                components.scoreText.fontStyle = FontStyles.Bold;
            }
        }

        private void ShowAwards(Dictionary<ulong, AwardType> awards)
        {
            // Clear old awards
            ClearAwardEntries();

            foreach (var kvp in awards)
            {
                ulong playerId = kvp.Key;
                AwardType award = kvp.Value;

                if (awardsContent != null && awardEntryPrefab != null)
                {
                    GameObject entry = Instantiate(awardEntryPrefab, awardsContent);
                    awardEntries[award] = entry;

                    // Update award entry
                    TextMeshProUGUI awardText = entry.transform.Find("AwardText")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI playerText = entry.transform.Find("PlayerText")?.GetComponent<TextMeshProUGUI>();

                    if (awardText != null)
                    {
                        awardText.text = GetAwardName(award);
                    }

                    if (playerText != null)
                    {
                        playerText.text = $"Player {playerId}";
                    }
                }
            }
        }

        private string GetAwardName(AwardType award)
        {
            return award switch
            {
                AwardType.Slayer => "🏆 SLAYER",
                AwardType.Architect => "🏗️ ARCHITECT",
                AwardType.Guardian => "🛡️ GUARDIAN",
                AwardType.Carrier => "📦 CARRIER",
                AwardType.Saboteur => "💣 SABOTEUR",
                _ => award.ToString()
            };
        }

        private void ClearPlayerEntries()
        {
            foreach (var entry in playerEntries.Values)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            playerEntries.Clear();
            // ✅ PERFORMANCE FIX: Clear cached components when entries are destroyed
            cachedEntryComponents.Clear();
        }

        private void ClearAwardEntries()
        {
            foreach (var entry in awardEntries.Values)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            awardEntries.Clear();
        }

        /// <summary>
        /// ✅ NEW: Restart match button handler
        /// </summary>
        private void OnRestartButtonClicked()
        {
            // Only host can restart
            if (MatchManager.Instance != null && MatchManager.Instance.isServer)
            {
                MatchManager.Instance.CmdRestartMatch();
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[EndGameScoreboard] Only host can restart match");
                #endif
            }
        }
        
        /// <summary>
        /// ✅ NEW: Return to lobby (disconnect and show main menu)
        /// </summary>
        private void OnReturnToLobbyClicked()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔙 [EndGameScoreboard] Return to Lobby clicked");
            Debug.Log("═══════════════════════════════════════");
            
            // Hide scoreboard
            HideScoreboard();
            
            // ✅ CRITICAL: Disconnect from network (both client and host)
            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
                Debug.Log("✅ Client disconnected");
            }
            
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
                Debug.Log("✅ Host stopped");
            }
            
            // ✅ CRITICAL: Show MainMenu via UIFlowManager
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.ShowMainMenu();
            }
            else
            {
                // Fallback: Direct show
                var mainMenu = FindFirstObjectByType<MainMenu>();
                if (mainMenu != null)
                {
                    var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                    if (mainMenuPanel != null)
                    {
                        mainMenuPanel.gameObject.SetActive(true);
                    }
                }
            }
            
            Debug.Log("✅ Returned to Main Menu");
        }

        private void OnDestroy()
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnMatchWonEvent -= OnMatchWon;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            }
            
            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.RemoveListener(OnReturnToLobbyClicked);
            }

            ClearPlayerEntries();
            ClearAwardEntries();
        }
    }
}

