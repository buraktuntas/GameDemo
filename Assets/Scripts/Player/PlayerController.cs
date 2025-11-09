using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Player controller - SADECE network state management
    /// Hareket kontrolü FPSController'da, Build kontrolü SimpleBuildMode'da
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Network State")]
        [SyncVar] public Team team = Team.TeamA;
        [SyncVar] public RoleId role = RoleId.Builder;
        [SyncVar] public ulong playerId;
        
        [Header("Core Carrying")]
        [SyncVar] private bool isCarryingCore = false;
        [SyncVar] private ulong carriedCoreOwnerId = 0;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Components
        private PlayerVisuals playerVisuals;
        private FPSController fpsController;
        
        private void Awake()
        {
            playerVisuals = GetComponent<PlayerVisuals>();
            fpsController = GetComponent<FPSController>();
            playerId = netId; // Network ID'yi set et
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            RegisterWithMatchManager();
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // ✅ CRITICAL FIX: Subscribe to UI selection events
            SubscribeToUIEvents();

            // Update team color
            UpdateTeamColor();

            if (showDebugInfo)
            {
                Debug.Log("✅ PlayerController initialized for local player");
            }
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Subscribe to UI selection events to receive team/role choices
        /// </summary>
        private void SubscribeToUIEvents()
        {
            // Wait a frame for UI to initialize
            StartCoroutine(SubscribeToUIEventsCoroutine());
        }

        private System.Collections.IEnumerator SubscribeToUIEventsCoroutine()
        {
            yield return null; // Wait one frame

            // Find RoleSelectionUI and subscribe to confirm event
            var roleUI = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
            if (roleUI != null)
            {
                roleUI.OnRoleConfirmed += OnRoleSelectedFromUI;
                Debug.Log("✅ Subscribed to RoleSelectionUI.OnRoleConfirmed");
            }
            else
            {
                Debug.LogWarning("⚠️ RoleSelectionUI not found - cannot subscribe to events");
            }

            // Find TeamSelectionUI and subscribe to confirm event
            var teamUI = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
            if (teamUI != null)
            {
                teamUI.OnTeamConfirmed += OnTeamSelectedFromUI;
                Debug.Log("✅ Subscribed to TeamSelectionUI.OnTeamConfirmed");
            }
            else
            {
                Debug.LogWarning("⚠️ TeamSelectionUI not found - cannot subscribe to events");
            }
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when player confirms role in UI
        /// </summary>
        private void OnRoleSelectedFromUI(RoleId selectedRole)
        {
            Debug.Log($"🎯 Player selected role: {selectedRole}");

            // Get team from TeamSelectionUI
            var teamUI = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
            Team selectedTeam = Team.None;

            if (teamUI != null)
            {
                selectedTeam = teamUI.GetSelectedTeam();
                Debug.Log($"🎯 Player's team selection: {selectedTeam}");
            }

            // Send to server via Command
            CmdSetTeamAndRole(selectedTeam, selectedRole);
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when player confirms team in UI
        /// </summary>
        private void OnTeamSelectedFromUI(Team selectedTeam)
        {
            Debug.Log($"🎯 Player selected team: {selectedTeam}");
            // Team is stored, will be sent with role when role is confirmed
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Network command to set team and role from UI selection
        /// </summary>
        [Command]
        public void CmdSetTeamAndRole(Team selectedTeam, RoleId selectedRole)
        {
            Debug.Log($"[Server] CmdSetTeamAndRole called: Team={selectedTeam}, Role={selectedRole}");

            // Validate team (if None, let MatchManager auto-balance)
            // Don't call AssignTeamAutoBalance directly - it's private
            // Just pass Team.None to RegisterPlayer and it will handle it
            if (selectedTeam == Team.None)
            {
                Debug.Log($"[Server] Team was None, will be auto-balanced by MatchManager");
            }

            // ✅ FIX: Register FIRST (MatchManager may change team via auto-balance)
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.RegisterPlayer(netId, selectedTeam, selectedRole);
            }

            // ✅ FIX: Then apply the RESULT (team might have been auto-balanced by MatchManager)
            // Read back the actual team assigned by MatchManager
            var playerState = MatchManager.Instance?.GetPlayerState(netId);
            if (playerState != null)
            {
                team = playerState.team; // Use MatchManager's assigned team
                role = playerState.role;
                Debug.Log($"[Server] ✅ Player registered: Requested={selectedTeam}, Assigned={team}, Role={role}");
            }
            else
            {
                // Fallback: use requested values
                team = selectedTeam;
                role = selectedRole;
            }

            // Update visuals AFTER team is finalized
            RpcUpdateTeamColor(team);
        }

        [ClientRpc]
        private void RpcUpdateTeamColor(Team newTeam)
        {
            team = newTeam;
            UpdateTeamColor();
        }
        
        private void RegisterWithMatchManager()
        {
            // ✅ FIX: Sadece server'da register et
            if (!isServer) return;

            // ✅ FIX: Use singleton instead of FindFirstObjectByType
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.RegisterPlayer(netId, team, role);

                if (showDebugInfo)
                {
                    Debug.Log($"📝 Player registered: Team {team}, Role {role}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ MatchManager.Instance is null! Cannot register player.");
            }
        }
        
        private void UpdateTeamColor()
        {
            if (playerVisuals != null)
            {
                playerVisuals.UpdateTeamColor(team);
            }
        }
        
        // ⭐ SADECE network state getter'ları
        public Team GetPlayerTeam() => team;
        public RoleId GetPlayerRole() => role;
        public ulong GetPlayerId() => playerId;

        /// <summary>
        /// Set core carrying state (called by ObjectiveManager)
        /// </summary>
        [Server]
        public void SetCarryingCore(bool carrying, ulong coreOwnerId = 0)
        {
            isCarryingCore = carrying;
            carriedCoreOwnerId = coreOwnerId;
            RpcUpdateCoreCarrying(carrying);
        }

        [ClientRpc]
        private void RpcUpdateCoreCarrying(bool carrying)
        {
            isCarryingCore = carrying;
            
            // Update FPSController speed multiplier
            if (fpsController != null)
            {
                fpsController.speedMultiplier = carrying ? GameConstants.CORE_CARRY_SPEED_MULTIPLIER : 1f;
            }
        }

        /// <summary>
        /// Check if player is carrying a core
        /// </summary>
        public bool IsCarryingCore() => isCarryingCore;

        /// <summary>
        /// Try to return core (called when player interacts with return point)
        /// </summary>
        [Command]
        public void CmdTryReturnCore()
        {
            if (!isCarryingCore || carriedCoreOwnerId == 0)
                return;

            var objectiveManager = Core.ObjectiveManager.Instance;
            if (objectiveManager != null)
            {
                Vector3 playerPos = transform.position;
                objectiveManager.TryReturnCore(carriedCoreOwnerId, netId, playerPos);
            }
        }
    }
}
