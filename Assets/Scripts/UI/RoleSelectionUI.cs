using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Core;
using System.Linq;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Role Selection UI - Oyun başında role seçimi
    /// Builder / Guardian / Ranger / Saboteur
    /// </summary>
    public class RoleSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button builderButton;
        [SerializeField] private Button guardianButton;
        [SerializeField] private Button rangerButton;
        [SerializeField] private Button saboteurButton;

        [Header("Role Descriptions")]
        [SerializeField] private TextMeshProUGUI roleDescriptionText;

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectedRoleText;

        private RoleId selectedRole = RoleId.Builder;
        private bool isConfirmed = false;

        public System.Action<RoleId> OnRoleConfirmed;

        private void Start()
        {
            // Setup button listeners
            if (builderButton != null)
            {
                builderButton.onClick.AddListener(() => SelectRole(RoleId.Builder));
            }

            if (guardianButton != null)
            {
                guardianButton.onClick.AddListener(() => SelectRole(RoleId.Guardian));
            }

            if (rangerButton != null)
            {
                rangerButton.onClick.AddListener(() => SelectRole(RoleId.Ranger));
            }

            if (saboteurButton != null)
            {
                saboteurButton.onClick.AddListener(() => SelectRole(RoleId.Saboteur));
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmRole);
            }

            // Default selection
            SelectRole(RoleId.Builder);

            // DON'T show panel automatically - wait for TeamSelectionUI to call ShowPanel()
            // Panel should start HIDDEN
            HidePanel();
        }

        private void SelectRole(RoleId role)
        {
            if (isConfirmed) return;

            selectedRole = role;

            // Update selected role text
            if (selectedRoleText != null)
            {
                selectedRoleText.text = $"Selected: {role}";
            }

            // Update description
            UpdateRoleDescription(role);

            // Highlight selected button
            HighlightButton(role);
        }

        private void UpdateRoleDescription(RoleId role)
        {
            if (roleDescriptionText == null) return;

            string description = role switch
            {
                RoleId.Builder => "BUILDER\n\n" +
                    "• High building budget (60/40/30/20)\n" +
                    "• Fast structure placement\n" +
                    "• Rapid Deploy ability\n" +
                    "• Best for: Defense & fortification",

                RoleId.Guardian => "GUARDIAN\n\n" +
                    "• Medium building budget (20/10/10/5)\n" +
                    "• Increased structure durability\n" +
                    "• Bulwark shield ability\n" +
                    "• Best for: Frontline & protection",

                RoleId.Ranger => "RANGER\n\n" +
                    "• Low building budget (10/10/5/5)\n" +
                    "• Enhanced mobility\n" +
                    "• Scout Arrow ability\n" +
                    "• Best for: Flanking & reconnaissance",

                RoleId.Saboteur => "SABOTEUR\n\n" +
                    "• Minimal building budget (5/5/5/5)\n" +
                    "• Can destroy enemy structures faster\n" +
                    "• Shadow Step ability\n" +
                    "• Best for: Disruption & infiltration",

                _ => "Select a role..."
            };

            roleDescriptionText.text = description;
        }

        private void HighlightButton(RoleId role)
        {
            // Reset all buttons
            ResetButtonColors();

            // Highlight selected
            Button selectedButton = role switch
            {
                RoleId.Builder => builderButton,
                RoleId.Guardian => guardianButton,
                RoleId.Ranger => rangerButton,
                RoleId.Saboteur => saboteurButton,
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
            Button[] buttons = { builderButton, guardianButton, rangerButton, saboteurButton };

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

        private void ConfirmRole()
        {
            if (isConfirmed) return;

            isConfirmed = true;

            Debug.Log($"✅ Role confirmed: {selectedRole}");

            // Invoke callback
            OnRoleConfirmed?.Invoke(selectedRole);

            // ✅ CRITICAL FIX: Hide other UI panels FIRST (before hiding this panel)
            HideOtherUIs();

            // Hide this panel
            HidePanel();
            
            Debug.Log("🎮 Starting game with role: " + selectedRole);
            
            // ✅ CRITICAL FIX: Ensure all blockers are disabled (start coroutine AFTER logging)
            StartCoroutine(EnsureGameStart());
        }
        
        private System.Collections.IEnumerator EnsureGameStart()
        {
            Debug.Log("⏳ [RoleSelectionUI] EnsureGameStart coroutine started");
            
            // Wait one frame for UI updates
            yield return null;
            
            Debug.Log("⏳ [RoleSelectionUI] Frame wait complete, checking blockers...");
            
            // Disable all blockers again (in case they were re-enabled)
            var allBlockers = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(g => (g.name == "Blocker" || g.name.Contains("Blocker")) && g.activeSelf)
                .ToArray();
            
            if (allBlockers.Length > 0)
            {
                Debug.Log($"⚠️ [RoleSelectionUI] Found {allBlockers.Length} active blocker(s), disabling...");
                foreach (var blk in allBlockers)
                {
                    blk.SetActive(false);
                    Debug.Log($"✅ [RoleSelectionUI] Disabled blocker: {GetFullPath(blk.transform)}");
                }
            }
            else
            {
                Debug.Log("✅ [RoleSelectionUI] No active blockers found");
            }
            
            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("🔒 [RoleSelectionUI] Cursor locked for gameplay");
            
            // Wait one more frame for camera setup
            yield return null;
            
            // Verify camera is enabled
            var fpsControllers = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
            bool cameraFound = false;
            
            foreach (var fps in fpsControllers)
            {
                if (fps != null && fps.isLocalPlayer)
                {
                    var cam = fps.GetCamera();
                    if (cam != null)
                    {
                        cam.enabled = true;
                        Debug.Log($"✅ [RoleSelectionUI] Ensured player camera is enabled: {cam.name}");
                        cameraFound = true;
                    }
                    else
                    {
                        Debug.LogError("❌ [RoleSelectionUI] Local player has no camera!");
                    }
                }
            }
            
            if (!cameraFound)
            {
                Debug.LogWarning("⚠️ [RoleSelectionUI] No local player camera found yet - may spawn later");
            }
            
            Debug.Log("✅ [RoleSelectionUI] EnsureGameStart coroutine complete");
        }
        
        private string GetFullPath(Transform t)
        {
            string path = t.name;
            Transform parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        public void ShowPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            // DON'T pause game - it breaks UI input!
            // Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HidePanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            // DON'T lock cursor here - next UI screen might need it!
            // If there's a next UI screen (Team Selection), it will handle cursor state
            // If going directly to game, the game will lock the cursor
            // Time.timeScale = 1f;
            // Cursor.lockState = CursorLockMode.Locked;  // ❌ DON'T DO THIS
            // Cursor.visible = false;                     // ❌ DON'T DO THIS
        }

        public RoleId GetSelectedRole() => selectedRole;
        public bool IsConfirmed() => isConfirmed;
        
        /// <summary>
        /// ✅ PUBLIC: Check if panel is currently visible/active
        /// Used by FPSController to determine if UI is open
        /// </summary>
        public bool IsPanelOpen()
        {
            return selectionPanel != null && selectionPanel.activeSelf;
        }

        /// <summary>
        /// Hide other UI panels that might be visible
        /// </summary>
        private void HideOtherUIs()
        {
            // ✅ CRITICAL FIX: Hide this UI's blocker first
            var blocker = transform.Find("Blocker");
            if (blocker != null)
            {
                blocker.gameObject.SetActive(false);
                Debug.Log("✅ [RoleSelectionUI] Disabled own blocker");
            }
            
            // Find and hide MainMenu
            var mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                // Hide main menu panel
                var mainMenuPanel = mainMenu.transform.Find("MainMenuPanel");
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.gameObject.SetActive(false);
                }
                // Hide background
                var background = mainMenu.transform.Find("Background");
                if (background != null)
                {
                    background.gameObject.SetActive(false);
                }
                // Hide entire main menu root if needed
                if (mainMenu.gameObject.activeSelf)
                {
                    mainMenu.gameObject.SetActive(false);
                    Debug.Log("✅ [RoleSelectionUI] Hid MainMenu");
                }
            }

            // Find and hide TeamSelection if it exists
            var teamSelection = FindFirstObjectByType<TeamSelectionUI>();
            if (teamSelection != null)
            {
                teamSelection.HidePanel();
                Debug.Log("✅ [RoleSelectionUI] Hid TeamSelectionUI");
            }
            
            // ✅ CRITICAL FIX: Also disable all blockers in scene
            var allBlockers = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(g => (g.name == "Blocker" || g.name.Contains("Blocker")) && g.activeSelf)
                .ToArray();
            
            foreach (var blk in allBlockers)
            {
                blk.SetActive(false);
                Debug.Log($"✅ [RoleSelectionUI] Disabled blocker: {blk.name}");
            }
        }
    }
}
