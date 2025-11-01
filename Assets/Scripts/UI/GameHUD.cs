using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.Core;

namespace TacticalCombat.UI
{
    public class GameHUD : MonoBehaviour
    {
        // ✅ PERFORMANCE FIX: Singleton pattern to avoid FindFirstObjectByType
        public static GameHUD Instance { get; private set; }

        [Header("Phase & Timer")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Resources")]
        [SerializeField] private TextMeshProUGUI wallPointsText;
        [SerializeField] private TextMeshProUGUI elevationPointsText;
        [SerializeField] private TextMeshProUGUI trapPointsText;
        [SerializeField] private TextMeshProUGUI utilityPointsText;
        [SerializeField] private GameObject resourcePanel;

        [Header("Health")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Ammo")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reserveAmmoText;
        [SerializeField] private GameObject ammoPanel;

        [Header("Ability")]
        [SerializeField] private Image abilityIcon;
        [SerializeField] private TextMeshProUGUI abilityCooldownText;
        [SerializeField] private Image abilityCooldownOverlay;
        [SerializeField] private GameObject abilityPanel;

        [Header("Team Status")]
        [SerializeField] private TextMeshProUGUI teamScoreText;
        [SerializeField] private GameObject teamStatusPanel;

        [Header("Build Ghost Feedback")]
        [SerializeField] private TextMeshProUGUI buildFeedbackText;
        [SerializeField] private GameObject buildFeedbackPanel;

        [Header("Sabotage Progress")]
        [SerializeField] private Slider sabotageProgressBar;
        [SerializeField] private GameObject sabotagePanel;

        [Header("Round Win")]
        [SerializeField] private GameObject roundWinPanel;
        [SerializeField] private TextMeshProUGUI roundWinText;

        [Header("Kill Feed")]
        [SerializeField] private GameObject killFeedPanel;
        [SerializeField] private TextMeshProUGUI killFeedText;

        [Header("Respawn")]
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private TextMeshProUGUI respawnText;

        [Header("Control Point")]
        [SerializeField] private TextMeshProUGUI controlPointText;
        [SerializeField] private Slider controlPointBar;
        [SerializeField] private GameObject controlPointPanel;

        // ✅ PERFORMANCE FIX: Throttle UI updates to avoid 60 FPS string allocations
        private float lastUIUpdateTime;
        private const float UI_UPDATE_INTERVAL = 0.1f; // 10 Hz instead of 60 Hz

        private void Awake()
        {
            // ✅ Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("⚠️ [GameHUD] Multiple instances detected! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Subscribe to MatchManager events
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnPhaseChangedEvent += OnPhaseChanged;
            }

            // Hide panels initially
            if (sabotagePanel != null) sabotagePanel.SetActive(false);
            if (buildFeedbackPanel != null) buildFeedbackPanel.SetActive(false);
        }

        private void Update()
        {
            // ✅ PERFORMANCE FIX: Throttle UI updates (60 FPS → 10 Hz)
            if (Time.time - lastUIUpdateTime >= UI_UPDATE_INTERVAL)
            {
                UpdateTimer();
                UpdateRoundInfo();
                lastUIUpdateTime = Time.time;
            }
        }

        private void UpdateTimer()
        {
            if (MatchManager.Instance == null)
            {
                if (timerText != null) timerText.text = "0:00";
                return;
            }

            float remaining = MatchManager.Instance.GetRemainingTime();
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            if (timerText != null)
            {
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void UpdateRoundInfo()
        {
            if (MatchManager.Instance == null)
            {
                if (roundText != null) roundText.text = "Round 0";
                return;
            }

            if (roundText != null)
            {
                int round = MatchManager.Instance.GetCurrentRound();
                roundText.text = $"Round {round}";
            }

            if (teamScoreText != null)
            {
                int teamAWins = MatchManager.Instance.GetTeamAWins();
                int teamBWins = MatchManager.Instance.GetTeamBWins();
                teamScoreText.text = $"{teamAWins} - {teamBWins}";
            }
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (phaseText != null)
            {
                phaseText.text = newPhase.ToString().ToUpper();
            }

            // Show/hide resource panel based on phase
            if (resourcePanel != null)
            {
                resourcePanel.SetActive(newPhase == Phase.Build);
            }
        }

        public void UpdateResources(BuildBudget budget)
        {
            if (wallPointsText != null)
                wallPointsText.text = $"Wall: {budget.wallPoints}";
            if (elevationPointsText != null)
                elevationPointsText.text = $"Elevation: {budget.elevationPoints}";
            if (trapPointsText != null)
                trapPointsText.text = $"Trap: {budget.trapPoints}";
            if (utilityPointsText != null)
                utilityPointsText.text = $"Utility: {budget.utilityPoints}";
        }

        public void UpdateHealth(int current, int max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            if (healthText != null)
            {
                // FPS Standard: Show only current health (like Valorant/CS:GO)
                healthText.text = current.ToString();
            }
        }

        public void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = current.ToString();
            }

            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = $"/ {reserve}";
            }
        }

        public void UpdateAbilityCooldown(float remaining, float max)
        {
            if (abilityCooldownText != null)
            {
                if (remaining > 0)
                {
                    abilityCooldownText.text = $"{remaining:F1}s";
                }
                else
                {
                    abilityCooldownText.text = "READY";
                }
            }

            if (abilityCooldownOverlay != null)
            {
                abilityCooldownOverlay.fillAmount = remaining / max;
            }
        }

        public void ShowBuildFeedback(bool valid, string message)
        {
            if (buildFeedbackPanel != null)
            {
                buildFeedbackPanel.SetActive(true);
            }

            if (buildFeedbackText != null)
            {
                buildFeedbackText.text = message;
                buildFeedbackText.color = valid ? Color.green : Color.red;
            }
        }

        public void HideBuildFeedback()
        {
            if (buildFeedbackPanel != null)
            {
                buildFeedbackPanel.SetActive(false);
            }
        }

        public void ShowRoundWin(string winnerTeam, int teamAScore, int teamBScore)
        {
            if (roundWinPanel != null && roundWinText != null)
            {
                roundWinText.text = $"{winnerTeam} WINS!\n{teamAScore} - {teamBScore}";
                roundWinPanel.SetActive(true);
                Invoke(nameof(HideRoundWin), 3f);
            }
        }

        private void HideRoundWin()
        {
            if (roundWinPanel != null)
            {
                roundWinPanel.SetActive(false);
            }
        }

        public void ShowKillFeed(string killerName, string victimName)
        {
            if (killFeedPanel != null && killFeedText != null)
            {
                killFeedText.text = $"{killerName} → {victimName}";
                killFeedPanel.SetActive(true);
                CancelInvoke(nameof(HideKillFeed));
                Invoke(nameof(HideKillFeed), 3f);
            }
        }

        private void HideKillFeed()
        {
            if (killFeedPanel != null)
            {
                killFeedPanel.SetActive(false);
            }
        }

        public void ShowRespawnCountdown(float seconds)
        {
            if (respawnPanel != null && respawnText != null)
            {
                respawnText.text = $"Respawning in {Mathf.CeilToInt(seconds)}...";
                respawnPanel.SetActive(true);
            }
        }

        public void HideRespawnCountdown()
        {
            if (respawnPanel != null)
            {
                respawnPanel.SetActive(false);
            }
        }

        public void UpdateSabotageProgress(float progress)
        {
            if (sabotagePanel != null)
            {
                sabotagePanel.SetActive(progress > 0);
            }

            if (sabotageProgressBar != null)
            {
                sabotageProgressBar.value = progress;
            }
        }

        public void UpdateControlPoint(Team controllingTeam, float progress)
        {
            if (controlPointText != null)
            {
                if (controllingTeam == Team.None)
                {
                    controlPointText.text = "CONTESTED";
                }
                else
                {
                    controlPointText.text = $"{controllingTeam} CONTROLS";
                }
            }

            if (controlPointBar != null)
            {
                controlPointBar.value = (progress + 1f) / 2f; // Convert -1 to 1 range to 0 to 1
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnPhaseChangedEvent -= OnPhaseChanged;
            }
        }
    }
}



