using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
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

        // ✅ REMOVED: Round Win UI (round system removed)

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

        [Header("Info Tower Hack")]
        [SerializeField] private GameObject infoTowerHackPanel;
        [SerializeField] private Slider infoTowerHackSlider;
        [SerializeField] private TextMeshProUGUI infoTowerHackText;

        [Header("Hit Marker")]
        [SerializeField] private GameObject hitMarkerPanel;
        [SerializeField] private Image hitMarkerImage;
        [SerializeField] private Image headshotMarkerImage;

        // ✅ PERFORMANCE FIX: Throttle UI updates to avoid 60 FPS string allocations
        private float lastUIUpdateTime;
        private const float UI_UPDATE_INTERVAL = 0.1f; // 10 Hz instead of 60 Hz

        // ✅ CRITICAL FIX: Cache local player reference to avoid FindFirstObjectByType every frame
        private Player.PlayerController cachedLocalPlayer;
        
        // ✅ PERFORMANCE FIX: Cache string builders to avoid GC allocation
        private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
        
        // ✅ PERFORMANCE FIX: Cache last values to avoid unnecessary UI updates
        private int lastHealthValue = -1;
        private int lastAmmoValue = -1;
        private int lastReserveAmmoValue = -1;
        private string lastTeamScoreText = "";

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
                return;
            }

            // ✅ CRITICAL FIX: Force hide all conditional panels IMMEDIATELY (before Start)
            // This prevents flickering on game start even if scene has them enabled
            if (sabotagePanel != null) sabotagePanel.SetActive(false);
            if (buildFeedbackPanel != null) buildFeedbackPanel.SetActive(false);
            if (coreCarryingPanel != null) coreCarryingPanel.SetActive(false);
            if (suddenDeathPanel != null) suddenDeathPanel.SetActive(false);
            if (infoTowerHackPanel != null) infoTowerHackPanel.SetActive(false);
            if (hitMarkerPanel != null) hitMarkerPanel.SetActive(false);
            if (controlPointPanel != null) controlPointPanel.SetActive(false);
        }

        private void Start()
        {
            // Subscribe to MatchManager events
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnPhaseChangedEvent += OnPhaseChanged;
                MatchManager.Instance.OnSuddenDeathActivated += OnSuddenDeathActivated;
                
                // ✅ CRITICAL: Check current phase and hide HUD if in Lobby/End
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == Phase.Lobby || currentPhase == Phase.End)
                {
                    gameObject.SetActive(false);
                    Debug.Log($"✅ [GameHUD] Hidden at start (phase: {currentPhase})");
                }
            }

            // ✅ FIX: Hide all conditional panels initially (prevent crosshair blocking)
            if (sabotagePanel != null) sabotagePanel.SetActive(false);
            if (buildFeedbackPanel != null) buildFeedbackPanel.SetActive(false);
            if (coreCarryingPanel != null) coreCarryingPanel.SetActive(false);
            if (suddenDeathPanel != null) suddenDeathPanel.SetActive(false);
            if (infoTowerHackPanel != null) infoTowerHackPanel.SetActive(false);
            if (hitMarkerPanel != null) hitMarkerPanel.SetActive(false);
            if (controlPointPanel != null) controlPointPanel.SetActive(false); // Only show during control point objective
        }

        private void Update()
        {
            // ✅ PERFORMANCE FIX: Throttle UI updates (60 FPS → 10 Hz)
            if (Time.time - lastUIUpdateTime >= UI_UPDATE_INTERVAL)
            {
                UpdateTimer();
                UpdateGameModeInfo();
                UpdateCoreCarrying();
                UpdateHealthAndAmmo(); // ✅ CRITICAL FIX: Update health and ammo from local player
                lastUIUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Update health and ammo from local player components
        /// This ensures UI stays updated even if PlayerHUDController events fail
        /// </summary>
        private void UpdateHealthAndAmmo()
        {
            // ✅ CRITICAL FIX: Cache local player reference to avoid FindFirstObjectByType every frame
            if (cachedLocalPlayer == null)
            {
                var players = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.isLocalPlayer)
                    {
                        cachedLocalPlayer = player;
                        break;
                    }
                }
            }
            
            if (cachedLocalPlayer == null) return;
            
            // ✅ CRITICAL FIX: Update health from Health component
            var health = cachedLocalPlayer.GetComponent<Combat.Health>();
            if (health != null)
            {
                UpdateHealth(health.CurrentHealth, health.MaxHealth);
            }
            
            // ✅ CRITICAL FIX: Update ammo from WeaponSystem component
            var weaponSystem = cachedLocalPlayer.GetComponent<Combat.WeaponSystem>();
            if (weaponSystem != null)
            {
                // ✅ FIX: Use public methods instead of private fields
                UpdateAmmo(weaponSystem.GetCurrentAmmo(), weaponSystem.GetReserveAmmo());
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

            // Hide team score in FFA mode, show team scores in Team mode
            if (teamStatusPanel != null)
            {
                GameMode mode = MatchManager.Instance.GetGameMode();
                teamStatusPanel.SetActive(mode == GameMode.Team4v4);
                
                // Update team score text in Team mode
                if (mode == GameMode.Team4v4 && teamScoreText != null)
                {
                    // ✅ PERFORMANCE FIX: Use NetworkServer.spawned instead of FindObjectsByType
                    int teamAScore = 0;
                    int teamBScore = 0;
                    
                    // ✅ PERFORMANCE FIX: Use NetworkServer.spawned (server) or NetworkClient.spawned (client)
                    if (NetworkServer.active && NetworkServer.spawned != null)
                    {
                        foreach (var kvp in NetworkServer.spawned)
                        {
                            if (kvp.Value == null) continue;
                            
                            var player = kvp.Value.GetComponent<Player.PlayerController>();
                            if (player != null && MatchManager.Instance != null)
                            {
                                // ✅ FIX: Server-side access (server can use GetPlayerMatchStats)
                                try
                                {
                                    var stats = MatchManager.Instance.GetPlayerMatchStats(player.netId);
                                    if (stats != null)
                                    {
                                        Team playerTeam = player.GetPlayerTeam();
                                        if (playerTeam == Team.TeamA)
                                            teamAScore += stats.totalScore;
                                        else if (playerTeam == Team.TeamB)
                                            teamBScore += stats.totalScore;
                                    }
                                }
                                catch (System.Exception)
                                {
                                    // Silently skip - stats might not be ready yet
                                    // This is expected during match initialization
                                }
                            }
                        }
                    }
                    else if (NetworkClient.active && NetworkClient.spawned != null)
                    {
                        // Client-side: Use NetworkClient.spawned
                        foreach (var kvp in NetworkClient.spawned)
                        {
                            if (kvp.Value == null) continue;
                            
                            var player = kvp.Value.GetComponent<Player.PlayerController>();
                            if (player != null && MatchManager.Instance != null)
                            {
                                // ✅ FIX: Client-side access (use client cache)
                                try
                                {
                                    var stats = MatchManager.Instance.GetPlayerMatchStatsClient(player.netId);
                                    if (stats != null)
                                    {
                                        Team playerTeam = player.GetPlayerTeam();
                                        if (playerTeam == Team.TeamA)
                                            teamAScore += stats.totalScore;
                                        else if (playerTeam == Team.TeamB)
                                            teamBScore += stats.totalScore;
                                    }
                                }
                                catch (System.Exception)
                                {
                                    // Silently skip - stats might not be ready yet
                                    // This is expected during match initialization
                                }
                            }
                        }
                    }
                    
                    // ✅ PERFORMANCE FIX: Only update if value changed (avoid GC allocation)
                    stringBuilder.Clear();
                    stringBuilder.Append(teamAScore);
                    stringBuilder.Append(" - ");
                    stringBuilder.Append(teamBScore);
                    string newTeamScoreText = stringBuilder.ToString();
                    if (newTeamScoreText != lastTeamScoreText)
                    {
                        teamScoreText.text = newTeamScoreText;
                        lastTeamScoreText = newTeamScoreText;
                    }
                }
                else if (teamScoreText != null)
                {
                    teamScoreText.text = ""; // Empty in FFA
                }
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
            Debug.Log($"🎮 [GameHUD] Phase changed to: {newPhase}");
            
            if (phaseText != null)
            {
                string phaseName = newPhase switch
                {
                    Phase.Lobby => "LOBBY",
                    Phase.Build => "BUILD PHASE",
                    Phase.Combat => "COMBAT PHASE",
                    Phase.SuddenDeath => "SUDDEN DEATH",
                    Phase.End => "MATCH END",
                    _ => newPhase.ToString().ToUpper()
                };
                phaseText.text = phaseName;
            }

            // ✅ CRITICAL: Hide GameHUD in Lobby and End phases
            // Show in Build, Combat, and SuddenDeath
            if (newPhase == Phase.Lobby || newPhase == Phase.End)
            {
                gameObject.SetActive(false);
                Debug.Log($"✅ [GameHUD] Hidden (phase: {newPhase})");
            }
            else
            {
                // ✅ CRITICAL FIX: Ensure GameObject is active for Build/Combat/SuddenDeath
                gameObject.SetActive(true);
                Debug.Log($"✅ [GameHUD] Shown (phase: {newPhase})");
            }

            // Show/hide resource panel based on phase (Build only)
            if (resourcePanel != null)
            {
                resourcePanel.SetActive(newPhase == Phase.Build);
            }

            // Show/hide ammo panel based on phase (Combat/SuddenDeath)
            if (ammoPanel != null)
            {
                bool shouldShowAmmo = (newPhase == Phase.Combat || newPhase == Phase.SuddenDeath);
                ammoPanel.SetActive(shouldShowAmmo);
                Debug.Log($"[GameHUD] Ammo panel {(shouldShowAmmo ? "shown" : "hidden")} (phase: {newPhase})");
            }
            
            // ✅ CRITICAL FIX: Ensure health panel is always visible during gameplay (Build/Combat/SuddenDeath)
            if (healthSlider != null && healthText != null)
            {
                bool shouldShowHealth = (newPhase == Phase.Build || newPhase == Phase.Combat || newPhase == Phase.SuddenDeath);
                
                // Force activate parent if needed
                if (healthSlider.transform.parent != transform && healthSlider.transform.parent != null)
                {
                    healthSlider.transform.parent.gameObject.SetActive(shouldShowHealth);
                }

                healthSlider.gameObject.SetActive(shouldShowHealth);
                healthText.gameObject.SetActive(shouldShowHealth);
                Debug.Log($"[GameHUD] Health panel {(shouldShowHealth ? "shown" : "hidden")} (phase: {newPhase})");
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
                // ✅ PERFORMANCE FIX: Only update if value changed (avoid GC allocation)
                if (current != lastHealthValue)
                {
                    // FPS Standard: Show only current health (like Valorant/CS:GO)
                    stringBuilder.Clear();
                    stringBuilder.Append(current);
                    healthText.text = stringBuilder.ToString();
                    lastHealthValue = current;
                }
            }
        }

        public void UpdateAmmo(int current, int reserve)
        {
            // ✅ PERFORMANCE FIX: Only update if value changed (avoid GC allocation)
            if (ammoText != null && current != lastAmmoValue)
            {
                stringBuilder.Clear();
                stringBuilder.Append(current);
                ammoText.text = stringBuilder.ToString();
                lastAmmoValue = current;
            }

            if (reserveAmmoText != null && reserve != lastReserveAmmoValue)
            {
                stringBuilder.Clear();
                stringBuilder.Append("/ ");
                stringBuilder.Append(reserve);
                reserveAmmoText.text = stringBuilder.ToString();
                lastReserveAmmoValue = reserve;
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

            // ✅ FIX: Auto-hide after 2 seconds to prevent crosshair blocking
            CancelInvoke(nameof(HideBuildFeedback));
            Invoke(nameof(HideBuildFeedback), 2f);
        }

        public void HideBuildFeedback()
        {
            if (buildFeedbackPanel != null)
            {
                buildFeedbackPanel.SetActive(false);
            }
        }

        // ✅ REMOVED: ShowRoundWin and HideRoundWin methods (round system removed)

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
            // ✅ FIX: Show panel when control point is active
            if (controlPointPanel != null)
            {
                controlPointPanel.SetActive(true);
            }

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

        /// <summary>
        /// Hide control point UI (called when objective changes)
        /// </summary>
        public void HideControlPoint()
        {
            if (controlPointPanel != null)
            {
                controlPointPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show Info Tower hack progress UI
        /// </summary>
        public void ShowInfoTowerHackProgress(string message)
        {
            if (infoTowerHackPanel != null)
            {
                infoTowerHackPanel.SetActive(true);
            }

            if (infoTowerHackText != null)
            {
                infoTowerHackText.text = message;
            }

            if (infoTowerHackSlider != null)
            {
                infoTowerHackSlider.value = 0f;
            }
        }

        /// <summary>
        /// Update Info Tower hack progress (0-1)
        /// </summary>
        public void UpdateInfoTowerHackProgress(float progress)
        {
            if (infoTowerHackSlider != null)
            {
                infoTowerHackSlider.value = progress;
            }

            if (infoTowerHackText != null)
            {
                infoTowerHackText.text = $"HACKING INFO TOWER... {Mathf.RoundToInt(progress * 100)}%";
            }
        }

        /// <summary>
        /// Hide Info Tower hack progress UI
        /// </summary>
        public void HideInfoTowerHackProgress()
        {
            if (infoTowerHackPanel != null)
            {
                infoTowerHackPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show Info Tower hack complete notification
        /// </summary>
        public void ShowInfoTowerHackComplete()
        {
            if (infoTowerHackText != null)
            {
                infoTowerHackText.text = "INFO TOWER HACKED!";
                infoTowerHackText.color = Color.green;
            }

            if (infoTowerHackSlider != null)
            {
                infoTowerHackSlider.value = 1f;
            }

            // Hide after 3 seconds
            Invoke(nameof(HideInfoTowerHackProgress), 3f);
        }

        /// <summary>
        /// Show hit marker (called when player lands a hit)
        /// </summary>
        public void ShowHitMarker(bool isHeadshot = false)
        {
            // Cancel any existing hide invoke
            CancelInvoke(nameof(HideHitMarker));

            if (hitMarkerPanel != null)
            {
                hitMarkerPanel.SetActive(true);
            }

            // Show regular or headshot marker
            if (isHeadshot)
            {
                if (hitMarkerImage != null) hitMarkerImage.enabled = false;
                if (headshotMarkerImage != null)
                {
                    headshotMarkerImage.enabled = true;
                    headshotMarkerImage.color = Color.red; // Headshot is red
                }
            }
            else
            {
                if (hitMarkerImage != null)
                {
                    hitMarkerImage.enabled = true;
                    hitMarkerImage.color = Color.white; // Normal hit is white
                }
                if (headshotMarkerImage != null) headshotMarkerImage.enabled = false;
            }

            // Hide after short duration (typical FPS style: 0.1-0.2 seconds)
            Invoke(nameof(HideHitMarker), 0.15f);
        }

        private void HideHitMarker()
        {
            if (hitMarkerPanel != null)
            {
                hitMarkerPanel.SetActive(false);
            }

            if (hitMarkerImage != null) hitMarkerImage.enabled = false;
            if (headshotMarkerImage != null) headshotMarkerImage.enabled = false;
        }

        private void OnDestroy()
        {
            // ✅ MEMORY LEAK FIX: Cancel all pending Invoke calls
            CancelInvoke();

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



