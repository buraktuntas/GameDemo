using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    public partial class FPSController
    {
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        // ✅ AAA QUALITY: Cached debug strings (prevents GC allocation)
        private string cachedDebugGrounded = "";
        private string cachedDebugVelocity = "";
        private string cachedDebugSprinting = "";
        private string cachedDebugStamina = "";
        private float lastDebugUpdateTime = 0f;
        private const float DEBUG_UPDATE_INTERVAL = 0.1f; // Update every 100ms (10 FPS)
        
        // ✅ PERFORMANCE FIX: Cache UI component references
        private TacticalCombat.UI.MainMenu cachedMainMenu;
        private TacticalCombat.UI.GameModeSelectionUI cachedGameModeSelection;
        private TacticalCombat.UI.RoleSelectionUI cachedRoleSelection;
        private TacticalCombat.UI.TeamSelectionUI cachedTeamSelection;
        private TacticalCombat.UI.LobbyUIController cachedLobbyController;
        private float lastUICacheTime = 0f;
        private const float UI_CACHE_REFRESH_INTERVAL = 0.1f; // ✅ AAA FIX: Reduced to 100ms for faster UI state detection

        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;

            // ✅ AAA FIX: Throttle string updates to prevent GC allocation
            if (Time.time - lastDebugUpdateTime >= DEBUG_UPDATE_INTERVAL)
            {
                cachedDebugGrounded = $"Grounded: {IsGrounded()}";
                cachedDebugVelocity = $"Velocity: {moveDirection.magnitude:F2}";
                cachedDebugSprinting = $"Sprinting: {IsSprinting()}";
                if (useStamina)
                {
                    cachedDebugStamina = $"Stamina: {currentStamina:F0}/{maxStamina}";
                }
                lastDebugUpdateTime = Time.time;
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("<b>FPS Controller Debug</b>");
            GUILayout.Label(cachedDebugGrounded);
            GUILayout.Label(cachedDebugVelocity);
            GUILayout.Label(cachedDebugSprinting);
            if (useStamina)
            {
                GUILayout.Label(cachedDebugStamina);
            }
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !showDebugInfo) return;

            // ✅ NETWORK DESYNC DEBUG: Show target position vs actual position
            if (hasTargetPosition && !isLocalPlayer)
            {
                // Draw target position (where server says we should be)
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetPosition, 0.3f);

                // Draw line from current to target
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, targetPosition + Vector3.up);

                // Draw distance text (visible in Scene view)
                float distance = Vector3.Distance(transform.position, targetPosition);
                UnityEngine.GUI.color = Color.white;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"Desync: {distance:F2}m\n{(isLocalPlayer ? "LOCAL" : "REMOTE")}"
                );
                #endif
            }

            if (!isLocalPlayer) return; // Rest is local player only

            // Draw player forward direction (BLUE)
            Gizmos.color = Color.blue;
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Vector3 end = start + transform.forward * 3f;

            for (int i = -1; i <= 1; i++)
            {
                Vector3 offset = transform.right * i * 0.05f;
                Gizmos.DrawLine(start + offset, end + offset);
            }

            // Draw camera direction (RED)
            if (playerCamera != null)
            {
                Gizmos.color = Color.red;
                Vector3 camStart = playerCamera.transform.position;
                Vector3 camEnd = camStart + playerCamera.transform.forward * 5f;
                Gizmos.DrawLine(camStart, camEnd);
            }

            // Draw ground check (GREEN/YELLOW)
            Gizmos.color = IsGrounded() ? Color.green : Color.yellow;
            Vector3 checkStart = transform.position + Vector3.up * 0.1f;
            Vector3 checkEnd = checkStart + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(checkStart, checkEnd);
            Gizmos.DrawWireSphere(checkEnd, 0.1f);
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Clean scene AudioListeners with delay (prevents infinite loop)
        /// Only called once for local player on spawn
        /// </summary>
        private System.Collections.IEnumerator CleanSceneAudioListenersDelayed()
        {
            // Wait one frame to ensure all objects are initialized
            yield return null;

            // Find all AudioListeners in the scene
            var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int disabledCount = 0;

            foreach (var listener in allListeners)
            {
                // Skip if null or is our own listener
                if (listener == null || listener.gameObject == playerCamera.gameObject)
                    continue;

                // Disable scene/bootstrap listeners
                if (listener.gameObject.scene.IsValid()) // Scene object (not prefab)
                {
                    listener.enabled = false;
                    disabledCount++;

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (showDebugInfo)
                    {
                        Debug.Log($"🔇 Disabled AudioListener on: {listener.gameObject.name}");
                    }
                    #endif
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (showDebugInfo && disabledCount > 0)
            {
                Debug.Log($"🔊 Cleaned {disabledCount} scene AudioListener(s) - Local player listener active");
            }
            #endif
        }
        
        /// <summary>
        /// ✅ CRITICAL: Check if any UI panel is currently open
        /// Prevents FPSController from interfering with UI clicks
        /// ✅ PERFORMANCE FIX: Cache UI component references to avoid FindFirstObjectByType calls
        /// </summary>
        private bool IsAnyUIOpen()
        {
            // ✅ CRITICAL FIX: Check MatchManager phase FIRST (Lobby phase = UI open)
            if (TacticalCombat.Core.MatchManager.Instance != null)
            {
                TacticalCombat.Core.Phase currentPhase = TacticalCombat.Core.MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == TacticalCombat.Core.Phase.Lobby)
                {
                    return true; // Lobby phase = UI must be open
                }
            }
            
            // ✅ PERFORMANCE FIX: Refresh UI cache periodically (not every frame)
            if (Time.time - lastUICacheTime >= UI_CACHE_REFRESH_INTERVAL)
            {
                cachedMainMenu = FindFirstObjectByType<TacticalCombat.UI.MainMenu>();
                cachedGameModeSelection = FindFirstObjectByType<TacticalCombat.UI.GameModeSelectionUI>();
                cachedRoleSelection = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
                cachedTeamSelection = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
                cachedLobbyController = TacticalCombat.UI.LobbyUIController.Instance; // ✅ Cache singleton too
                lastUICacheTime = Time.time;
            }
            
            // ✅ FIX: Use cached references and public IsPanelOpen() methods
            
            // Check MainMenu using cached reference
            if (cachedMainMenu != null && cachedMainMenu.IsPanelOpen())
            {
                return true;
            }

            // Check GameModeSelectionUI using cached reference
            if (cachedGameModeSelection != null && cachedGameModeSelection.IsPanelOpen())
            {
                return true;
            }

            // ✅ NEW: Check LobbyUIController (CRITICAL - this was missing!)
            // ✅ PERFORMANCE FIX: Cache LobbyUIController to avoid singleton property access overhead
            if (cachedLobbyController == null)
            {
                cachedLobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            }
            if (cachedLobbyController != null && cachedLobbyController.IsLobbyVisible())
            {
                return true;
            }

            // Check RoleSelectionUI using cached reference
            if (cachedRoleSelection != null && cachedRoleSelection.IsPanelOpen())
            {
                return true;
            }

            // Check TeamSelectionUI using cached reference
            if (cachedTeamSelection != null && cachedTeamSelection.IsPanelOpen())
            {
                return true;
            }

            // No UI open - safe to handle cursor locking
            return false;
        }
    }
}
