using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using TacticalCombat.Core;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Team Selection UI - Manuel takım seçimi (opsiyonel)
    /// TeamA (Blue) vs TeamB (Red)
    /// </summary>
    public class TeamSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button teamAButton;
        [SerializeField] private Button teamBButton;
        [SerializeField] private Button autoButton;

        [Header("Team Info")]
        [SerializeField] private TextMeshProUGUI teamACountText;
        [SerializeField] private TextMeshProUGUI teamBCountText;

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectedTeamText;

        private Team selectedTeam = Team.None;
        private bool isConfirmed = false;

        public System.Action<Team> OnTeamConfirmed;
        
        // ✅ PERFORMANCE FIX: Cache components and throttle updates
        private Canvas cachedCanvas;
        private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.2f; // Update every 200ms instead of every frame
        private int lastTeamACount = -1;
        private int lastTeamBCount = -1;

        private void Start()
        {
            // Setup button listeners
            if (teamAButton != null)
            {
                teamAButton.onClick.AddListener(() => SelectTeam(Team.TeamA));
            }

            if (teamBButton != null)
            {
                teamBButton.onClick.AddListener(() => SelectTeam(Team.TeamB));
            }

            if (autoButton != null)
            {
                autoButton.onClick.AddListener(() => SelectTeam(Team.None)); // Auto-balance
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmTeam);
            }

            // Default to auto
            SelectTeam(Team.None);

            // Update team counts
            UpdateTeamCounts();

            // DON'T show panel automatically - wait for MainMenu to call ShowPanel()
            // Panel should start HIDDEN
            HidePanel();
        }

        private void Update()
        {
            // ✅ PERFORMANCE FIX: Time-based throttling instead of frame-based
            if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
            {
                UpdateTeamCounts();
                lastUpdateTime = Time.time;
            }
        }

        private void SelectTeam(Team team)
        {
            if (isConfirmed) return;

            selectedTeam = team;

            // Update selected team text
            if (selectedTeamText != null)
            {
                // ✅ PERFORMANCE FIX: Use StringBuilder to avoid GC allocation
                stringBuilder.Clear();
                stringBuilder.Append("Selected: ");
                string teamName = team switch
                {
                    Team.TeamA => "Team A (Blue)",
                    Team.TeamB => "Team B (Red)",
                    _ => "Auto Balance"
                };
                stringBuilder.Append(teamName);
                selectedTeamText.text = stringBuilder.ToString();
            }

            // Highlight selected button
            HighlightButton(team);
        }

        private void UpdateTeamCounts()
        {
            if (MatchManager.Instance == null) return;

            // Get team player counts
            int teamACount = GetTeamPlayerCount(Team.TeamA);
            int teamBCount = GetTeamPlayerCount(Team.TeamB);

            // ✅ PERFORMANCE FIX: Only update if count changed (avoid GC allocation)
            if (teamACount != lastTeamACount && teamACountText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(teamACount);
                stringBuilder.Append(" Players");
                teamACountText.text = stringBuilder.ToString();
                lastTeamACount = teamACount;
            }

            if (teamBCount != lastTeamBCount && teamBCountText != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(teamBCount);
                stringBuilder.Append(" Players");
                teamBCountText.text = stringBuilder.ToString();
                lastTeamBCount = teamBCount;
            }
        }

        private int GetTeamPlayerCount(Team team)
        {
            // TODO: Get from MatchManager
            return 0;
        }

        private void HighlightButton(Team team)
        {
            // Reset all buttons
            ResetButtonColors();

            // Highlight selected
            Button selectedButton = team switch
            {
                Team.TeamA => teamAButton,
                Team.TeamB => teamBButton,
                Team.None => autoButton,
                _ => null
            };

            if (selectedButton != null)
            {
                ColorBlock colors = selectedButton.colors;
                colors.normalColor = new Color(0.2f, 0.8f, 0.2f); // Green
                selectedButton.colors = colors;
            }
        }

        private void ResetButtonColors()
        {
            Button[] buttons = { teamAButton, teamBButton, autoButton };

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = Color.white;
                    button.colors = colors;
                }
            }
        }

        private void ConfirmTeam()
        {
            if (isConfirmed) return;

            isConfirmed = true;

            Debug.Log($"✅ Team confirmed: {selectedTeam}");

            // Invoke callback
            OnTeamConfirmed?.Invoke(selectedTeam);

            // Hide panel
            HidePanel();

            // Show Role Selection next
            ShowRoleSelection();
        }

        private void ShowRoleSelection()
        {
            // Role Selection UI'ını göster
            var roleSelection = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
            if (roleSelection != null)
            {
                roleSelection.ShowPanel();
            }
            else
            {
                Debug.LogWarning("⚠️ RoleSelectionUI not found in scene!");
            }
        }

        public void ShowPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            // ✅ CRITICAL FIX: Ensure EventSystem exists for UI clicks
            EnsureEventSystem();
            
            // ✅ CRITICAL FIX: Hide crosshair that might block clicks
            HideCrosshair();

            // DON'T pause game - it breaks UI input!
            // Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // ✅ CRITICAL FIX: Force cursor unlock (FPSController might have locked it)
            StartCoroutine(ForceCursorUnlock());
        }
        
        private System.Collections.IEnumerator ForceCursorUnlock()
        {
            // Wait one frame to ensure FPSController doesn't override
            yield return null;
            
            // Force unlock cursor multiple times to override FPSController
            for (int i = 0; i < 3; i++)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("✅ EventSystem created for TeamSelectionUI");
            }
            
            // ✅ PERFORMANCE FIX: Cache canvas reference
            if (cachedCanvas == null)
            {
                cachedCanvas = GetComponentInParent<Canvas>();
            }
            
            // Ensure Canvas has GraphicRaycaster
            Canvas canvas = cachedCanvas;
            if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("✅ GraphicRaycaster added to Canvas");
            }
        }
        
        private void HideCrosshair()
        {
            // Find and hide all crosshair/combat UI elements that might block clicks
            var combatUI = FindFirstObjectByType<TacticalCombat.UI.CombatUI>();
            if (combatUI != null)
            {
                combatUI.gameObject.SetActive(false);
            }

            var crosshairController = FindFirstObjectByType<TacticalCombat.UI.CrosshairController>();
            if (crosshairController != null)
            {
                crosshairController.gameObject.SetActive(false);
            }

            var simpleCrosshair = FindFirstObjectByType<TacticalCombat.UI.SimpleCrosshair>();
            if (simpleCrosshair != null)
            {
                simpleCrosshair.gameObject.SetActive(false);
            }
        }

        public void HidePanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            // ✅ CRITICAL FIX: DON'T lock cursor here - RoleSelectionUI needs it!
            // Next UI (RoleSelection) will manage cursor state
            // If going directly to game, game will lock cursor
            // Time.timeScale = 1f;
            // Cursor.lockState = CursorLockMode.Locked;  // ❌ DON'T DO THIS
            // Cursor.visible = false;                     // ❌ DON'T DO THIS
        }

        public Team GetSelectedTeam() => selectedTeam;
        public bool IsConfirmed() => isConfirmed;
        
        /// <summary>
        /// ✅ PUBLIC: Check if panel is currently visible/active
        /// Used by FPSController to determine if UI is open
        /// </summary>
        public bool IsPanelOpen()
        {
            return selectionPanel != null && selectionPanel.activeSelf;
        }
    }
}
