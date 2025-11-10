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

        [Header("Stats Columns")]
        [SerializeField] private TextMeshProUGUI headerText; // "K/D/A/Structures/Traps/Captures/Score"

        private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();
        private Dictionary<AwardType, GameObject> awardEntries = new Dictionary<AwardType, GameObject>();

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

            // Find all players
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            // Sort by total score
            System.Array.Sort(players, (a, b) =>
            {
                var statsA = MatchManager.Instance?.GetPlayerMatchStats(a.netId);
                var statsB = MatchManager.Instance?.GetPlayerMatchStats(b.netId);
                int scoreA = statsA?.totalScore ?? 0;
                int scoreB = statsB?.totalScore ?? 0;
                return scoreB.CompareTo(scoreA); // Descending order
            });

            // Create entries
            foreach (var player in players)
            {
                if (playerListContent != null && playerEntryPrefab != null)
                {
                    GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
                    playerEntries[player.netId] = entry;
                    UpdatePlayerEntry(entry, player);
                }
            }
        }

        private void UpdatePlayerEntry(GameObject entry, PlayerController player)
        {
            // Find text components
            TextMeshProUGUI nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI killsText = entry.transform.Find("KillsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI deathsText = entry.transform.Find("DeathsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI assistsText = entry.transform.Find("AssistsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI structuresText = entry.transform.Find("StructuresText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI trapKillsText = entry.transform.Find("TrapKillsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI capturesText = entry.transform.Find("CapturesText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI defenseTimeText = entry.transform.Find("DefenseTimeText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI scoreText = entry.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            // Get stats
            var stats = MatchManager.Instance?.GetPlayerMatchStats(player.netId);
            int kills = stats?.kills ?? 0;
            int deaths = stats?.deaths ?? 0;
            int assists = stats?.assists ?? 0;
            int structures = stats?.structuresBuilt ?? 0;
            int trapKills = stats?.trapKills ?? 0;
            int captures = stats?.captures ?? 0;
            float defenseTime = stats?.defenseTime ?? 0f;
            int totalScore = stats?.totalScore ?? 0;

            string playerName = $"Player {player.netId}";

            if (nameText != null)
            {
                nameText.text = playerName;
                if (player.isLocalPlayer)
                {
                    nameText.color = Color.yellow;
                    nameText.fontStyle = FontStyles.Bold;
                }
            }

            if (killsText != null) killsText.text = kills.ToString();
            if (deathsText != null) deathsText.text = deaths.ToString();
            if (assistsText != null) assistsText.text = assists.ToString();
            if (structuresText != null) structuresText.text = structures.ToString();
            if (trapKillsText != null) trapKillsText.text = trapKills.ToString();
            if (capturesText != null) capturesText.text = captures.ToString();
            if (defenseTimeText != null) defenseTimeText.text = $"{defenseTime:F1}s";
            if (scoreText != null)
            {
                scoreText.text = totalScore.ToString();
                scoreText.color = Color.green;
                scoreText.fontStyle = FontStyles.Bold;
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

            ClearPlayerEntries();
            ClearAwardEntries();
        }
    }
}

