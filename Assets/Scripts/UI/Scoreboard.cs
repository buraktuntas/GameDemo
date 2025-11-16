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
    /// Scoreboard UI - TAB tuşu ile açılır/kapanır
    /// Tüm oyuncuları Team, Kill, Death ile gösterir
    /// </summary>
    public class Scoreboard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform teamAContent;
        [SerializeField] private Transform teamBContent;
        [SerializeField] private GameObject playerEntryPrefab;

        [Header("Team Headers")]
        [SerializeField] private TextMeshProUGUI teamAScoreText;
        [SerializeField] private TextMeshProUGUI teamBScoreText;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private float updateInterval = 0.5f;

        // State
        private bool isVisible = false;
        private float lastUpdateTime = 0f;
        private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();

        private void Start()
        {
            // Hide initially
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }

            // Create default prefab if not assigned
            if (playerEntryPrefab == null)
            {
                CreateDefaultPlayerEntryPrefab();
            }
        }

        private void Update()
        {
            // Toggle scoreboard with TAB
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleScoreboard();
            }

            // Hold TAB to show (Valorant style)
            if (Input.GetKey(toggleKey))
            {
                if (!isVisible)
                {
                    ShowScoreboard();
                }
            }
            else
            {
                if (isVisible)
                {
                    HideScoreboard();
                }
            }

            // Update scoreboard periodically if visible
            if (isVisible && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateScoreboard();
                lastUpdateTime = Time.time;
            }
        }

        public void ToggleScoreboard()
        {
            if (isVisible)
            {
                HideScoreboard();
            }
            else
            {
                ShowScoreboard();
            }
        }

        public void ShowScoreboard()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(true);
                isVisible = true;
                UpdateScoreboard();
            }
        }

        public void HideScoreboard()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
                isVisible = false;
            }
        }

        private void UpdateScoreboard()
        {
            // Clear old entries
            ClearEntries();

            // Find all players
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            // Update team scores (if team mode)
            if (MatchManager.Instance != null)
            {
                GameMode mode = MatchManager.Instance.GetGameMode();
                
                if (mode == GameMode.Team4v4)
                {
                    // Calculate team scores from match stats
                    int teamAScore = 0;
                    int teamBScore = 0;

                    foreach (var player in players)
                    {
                        // ✅ FIX: Use client-side stats cache
                        var stats = MatchManager.Instance?.GetPlayerMatchStatsClient(player.netId);
                        if (stats != null)
                        {
                            if (player.GetPlayerTeam() == Team.TeamA)
                                teamAScore += stats.totalScore;
                            else if (player.GetPlayerTeam() == Team.TeamB)
                                teamBScore += stats.totalScore;
                        }
                    }
                    
                    // ✅ NEW: Request stats if not cached (first time)
                    if (MatchManager.Instance != null && NetworkClient.active)
                    {
                        MatchManager.Instance.CmdRequestAllPlayerStats();
                    }

                    if (teamAScoreText != null)
                    {
                        teamAScoreText.text = $"TEAM A - {teamAScore}";
                    }

                    if (teamBScoreText != null)
                    {
                        teamBScoreText.text = $"TEAM B - {teamBScore}";
                    }
                }
                else // FFA
                {
                    if (teamAScoreText != null) teamAScoreText.text = "FFA MODE";
                    if (teamBScoreText != null) teamBScoreText.text = "";
                }
            }

            // Sort players by total score
            System.Array.Sort(players, (a, b) =>
            {
                // ✅ FIX: Use client-side stats cache
                var statsA = MatchManager.Instance?.GetPlayerMatchStatsClient(a.netId);
                var statsB = MatchManager.Instance?.GetPlayerMatchStatsClient(b.netId);
                int scoreA = statsA?.totalScore ?? 0;
                int scoreB = statsB?.totalScore ?? 0;
                return scoreB.CompareTo(scoreA); // Descending order
            });

            // Populate player entries
            foreach (var player in players)
            {
                Team team = player.GetPlayerTeam();
                Transform content = team == Team.TeamA ? teamAContent : teamBContent;

                // In FFA mode, use single column
                if (MatchManager.Instance != null && MatchManager.Instance.GetGameMode() == GameMode.FFA)
                {
                    content = teamAContent; // Use Team A column for FFA
                }

                if (content != null && playerEntryPrefab != null)
                {
                    GameObject entry = Instantiate(playerEntryPrefab, content);
                    playerEntries[player.netId] = entry;

                    // Update entry data
                    UpdatePlayerEntry(entry, player);
                }
            }
        }

        private void UpdatePlayerEntry(GameObject entry, PlayerController player)
        {
            // Find text components in entry
            TextMeshProUGUI nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI killsText = entry.transform.Find("KillsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI deathsText = entry.transform.Find("DeathsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI assistsText = entry.transform.Find("AssistsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI structuresText = entry.transform.Find("StructuresText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI trapKillsText = entry.transform.Find("TrapKillsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI capturesText = entry.transform.Find("CapturesText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI scoreText = entry.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            // Get match stats
            // ✅ FIX: Use client-side stats cache
            var stats = MatchManager.Instance?.GetPlayerMatchStatsClient(player.netId);
            int kills = stats?.kills ?? 0;
            int deaths = stats?.deaths ?? 0;
            int assists = stats?.assists ?? 0;
            int structures = stats?.structuresBuilt ?? 0;
            int trapKills = stats?.trapKills ?? 0;
            int captures = stats?.captures ?? 0;
            int totalScore = stats?.totalScore ?? 0;

            string playerName = $"Player {player.netId}";
            float kd = deaths > 0 ? (float)kills / deaths : kills;

            if (nameText != null)
            {
                nameText.text = playerName;

                // Highlight local player
                if (player.isLocalPlayer)
                {
                    nameText.color = Color.yellow;
                    nameText.fontStyle = FontStyles.Bold;
                }
            }

            if (killsText != null)
            {
                killsText.text = kills.ToString();
            }

            if (deathsText != null)
            {
                deathsText.text = deaths.ToString();
            }

            if (assistsText != null)
            {
                assistsText.text = assists.ToString();
            }

            if (structuresText != null)
            {
                structuresText.text = structures.ToString();
            }

            if (trapKillsText != null)
            {
                trapKillsText.text = trapKills.ToString();
            }

            if (capturesText != null)
            {
                capturesText.text = captures.ToString();
            }

            if (scoreText != null)
            {
                scoreText.text = totalScore.ToString();
                // Highlight highest score
                if (totalScore > 0)
                {
                    scoreText.color = Color.green;
                    scoreText.fontStyle = FontStyles.Bold;
                }
            }
        }

        private void ClearEntries()
        {
            // Destroy all existing entries
            foreach (var entry in playerEntries.Values)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }

            playerEntries.Clear();
        }

        /// <summary>
        /// Create a simple default player entry prefab
        /// </summary>
        private void CreateDefaultPlayerEntryPrefab()
        {
            GameObject prefab = new GameObject("PlayerEntryPrefab");
            prefab.AddComponent<RectTransform>();

            LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
            layoutElement.minHeight = 30;

            HorizontalLayoutGroup layout = prefab.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);

            // Name
            CreateChildText(prefab, "NameText", "Player", 200);

            // Kills
            CreateChildText(prefab, "KillsText", "0", 50);

            // Deaths
            CreateChildText(prefab, "DeathsText", "0", 50);

            // K/D
            CreateChildText(prefab, "KDText", "0.00", 60);

            playerEntryPrefab = prefab;

            Debug.Log("✅ Default PlayerEntryPrefab created");
        }

        private void CreateChildText(GameObject parent, string name, string defaultText, float preferredWidth)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            LayoutElement layout = textObj.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
        }

        private void OnDestroy()
        {
            ClearEntries();
        }
    }
}
