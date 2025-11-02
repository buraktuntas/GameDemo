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

            // Update team scores
            if (MatchManager.Instance != null)
            {
                int teamAWins = MatchManager.Instance.GetTeamAWins();
                int teamBWins = MatchManager.Instance.GetTeamBWins();

                if (teamAScoreText != null)
                {
                    teamAScoreText.text = $"TEAM A - {teamAWins} Rounds";
                }

                if (teamBScoreText != null)
                {
                    teamBScoreText.text = $"TEAM B - {teamBWins} Rounds";
                }
            }

            // Populate player entries
            foreach (var player in players)
            {
                Team team = player.GetPlayerTeam();
                Transform content = team == Team.TeamA ? teamAContent : teamBContent;

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
            TextMeshProUGUI kdText = entry.transform.Find("KDText")?.GetComponent<TextMeshProUGUI>();

            string playerName = $"Player {player.netId}";
            int kills = 0; // TODO: Implement kill tracking
            int deaths = 0; // TODO: Implement death tracking
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

            if (kdText != null)
            {
                kdText.text = kd.ToString("F2");
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
