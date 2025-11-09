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
        [SerializeField] private TextMeshProUGUI gameModeText; // FFA or Team4v4

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

        [Header("Headshot Indicator")]
        [SerializeField] private GameObject headshotPanel;
        [SerializeField] private TextMeshProUGUI headshotText;

        [Header("Respawn")]
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private TextMeshProUGUI respawnText;

        [Header("Control Point")]
        [SerializeField] private TextMeshProUGUI controlPointText;
        [SerializeField] private Slider controlPointBar;
        [SerializeField] private GameObject controlPointPanel;

        [Header("Core Carrying")]
        [SerializeField] private GameObject coreCarryingPanel;
        [SerializeField] private TextMeshProUGUI coreCarryingText;
        [SerializeField] private TextMeshProUGUI returnCoreHintText;

        [Header("Sudden Death")]
        [SerializeField] private GameObject suddenDeathPanel;
        [SerializeField] private TextMeshProUGUI suddenDeathText;

        // ✅ PERFORMANCE FIX: Throttle UI updates to avoid 60 FPS string allocations
        private float lastUIUpdateTime;
        private const float UI_UPDATE_INTERVAL = 0.1f; // 10 Hz instead of 60 Hz

        // ✅ CRITICAL FIX: Cache local player reference to avoid FindFirstObjectByType every frame
        private Player.PlayerController cachedLocalPlayer;

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
                MatchManager.Instance.OnSuddenDeathActivated += OnSuddenDeathActivated;
            }

            // Hide panels initially
            if (sabotagePanel != null) sabotagePanel.SetActive(false);
            if (buildFeedbackPanel != null) buildFeedbackPanel.SetActive(false);
            if (coreCarryingPanel != null) coreCarryingPanel.SetActive(false);
            if (suddenDeathPanel != null) suddenDeathPanel.SetActive(false);
        }

        private void Update()
        {
            // ✅ PERFORMANCE FIX: Throttle UI updates (60 FPS → 10 Hz)
            if (Time.time - lastUIUpdateTime >= UI_UPDATE_INTERVAL)
            {
                UpdateTimer();
                UpdateGameModeInfo();
                UpdateCoreCarrying();
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

        private void UpdateGameModeInfo()
        {
            if (MatchManager.Instance == null)
            {
                if (gameModeText != null) gameModeText.text = "";
                return;
            }

            if (gameModeText != null)
            {
                GameMode mode = MatchManager.Instance.GetGameMode();
                gameModeText.text = mode == GameMode.FFA ? "FFA" : "4v4";
            }

            // Hide team score in FFA mode
            if (teamStatusPanel != null)
            {
                GameMode mode = MatchManager.Instance.GetGameMode();
                teamStatusPanel.SetActive(mode == GameMode.Team4v4);
            }
        }

        private void UpdateCoreCarrying()
        {
            // ✅ CRITICAL FIX: Cache local player instead of FindFirstObjectByType every 100ms
            if (cachedLocalPlayer == null)
            {
                // Try to find local player (only when null)
                var players = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.isLocalPlayer)
                    {
                        cachedLocalPlayer = player;
                        break;
                    }
                }

                // If still not found, hide panel and return
                if (cachedLocalPlayer == null)
                {
                    if (coreCarryingPanel != null)
                    {
                        coreCarryingPanel.SetActive(false);
                    }
                    return;
                }
            }

            // Check if cached player is still valid
            if (cachedLocalPlayer != null && cachedLocalPlayer.isLocalPlayer)
            {
                bool isCarrying = cachedLocalPlayer.IsCarryingCore();

                if (coreCarryingPanel != null)
                {
                    coreCarryingPanel.SetActive(isCarrying);
                }

                if (coreCarryingText != null && isCarrying)
                {
                    coreCarryingText.text = "CARRYING CORE";
                }

                if (returnCoreHintText != null && isCarrying)
                {
                    returnCoreHintText.text = "Press E at your base to return";
                }
            }
            else
            {
                // Player became invalid, reset cache
                cachedLocalPlayer = null;
                if (coreCarryingPanel != null)
                {
                    coreCarryingPanel.SetActive(false);
                }
            }
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (phaseText != null)
            {
                string phaseName = newPhase switch
                {
                    Phase.Build => "BUILD PHASE",
                    Phase.Combat => "COMBAT PHASE",
                    Phase.SuddenDeath => "SUDDEN DEATH",
                    Phase.End => "MATCH END",
                    _ => newPhase.ToString().ToUpper()
                };
                phaseText.text = phaseName;
            }

            // Show/hide resource panel based on phase
            if (resourcePanel != null)
            {
                resourcePanel.SetActive(newPhase == Phase.Build);
            }

            // Show/hide ammo panel based on phase
            if (ammoPanel != null)
            {
                ammoPanel.SetActive(newPhase == Phase.Combat || newPhase == Phase.SuddenDeath);
            }
        }

        private void OnSuddenDeathActivated()
        {
            if (suddenDeathPanel != null)
            {
                suddenDeathPanel.SetActive(true);
            }

            if (suddenDeathText != null)
            {
                suddenDeathText.text = "⚡ SUDDEN DEATH ⚡\nSECRET TUNNEL OPENED!";
            }

            // Hide after 5 seconds
            Invoke(nameof(HideSuddenDeathNotification), 5f);
        }

        private void HideSuddenDeathNotification()
        {
            if (suddenDeathPanel != null)
            {
                suddenDeathPanel.SetActive(false);
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

        public void ShowKillFeed(string killerName, string victimName, bool isHeadshot = false)
        {
            if (killFeedPanel != null && killFeedText != null)
            {
                string headshotIcon = isHeadshot ? " 💀" : "";
                killFeedText.text = $"{killerName}{headshotIcon} → {victimName}";
                killFeedPanel.SetActive(true);
                CancelInvoke(nameof(HideKillFeed));
                Invoke(nameof(HideKillFeed), 3f);
            }

            // Show headshot indicator for local player
            if (isHeadshot)
            {
                ShowHeadshotIndicator();
            }
        }

        private void HideKillFeed()
        {
            if (killFeedPanel != null)
            {
                killFeedPanel.SetActive(false);
            }
        }

        public void ShowHeadshotIndicator()
        {
            if (headshotPanel != null && headshotText != null)
            {
                headshotText.text = "HEADSHOT!";
                headshotPanel.SetActive(true);
                CancelInvoke(nameof(HideHeadshotIndicator));
                Invoke(nameof(HideHeadshotIndicator), 2f);
            }
        }

        private void HideHeadshotIndicator()
        {
            if (headshotPanel != null)
            {
                headshotPanel.SetActive(false);
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
                MatchManager.Instance.OnSuddenDeathActivated -= OnSuddenDeathActivated;
            }
        }
    }
}



