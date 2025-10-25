using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Player controller with network registration and team management
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private Team playerTeam = Team.TeamA;
        [SerializeField] private RoleId playerRole = RoleId.Builder;
        
        [Header("Build Mode")]
        [SerializeField] private bool isInBuildMode = false;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Components
        private FPSController fpsController;
        private PlayerVisuals playerVisuals;
        
        // Properties for other scripts
        public Team team 
        { 
            get => playerTeam; 
            set => playerTeam = value; 
        }
        public RoleId role 
        { 
            get => playerRole; 
            set => playerRole = value; 
        }
        public ulong playerId => netId;
        
        private void Awake()
        {
            fpsController = GetComponent<FPSController>();
            playerVisuals = GetComponent<PlayerVisuals>();
            
            if (fpsController == null)
            {
                Debug.LogError("❌ FPSController not found! Please add FPSController component.");
            }
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Register with match manager
            RegisterWithMatchManager();
            
            // Update team color
            UpdateTeamColor();
            
            if (showDebugInfo)
            {
                Debug.Log("✅ PlayerController initialized for local player");
            }
        }
        
        private void Update()
        {
            if (!isLocalPlayer) return;
            
            HandleInput();
        }
        
        private void HandleInput()
        {
            // Build mode toggle (T key)
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleBuildMode();
            }
        }
        
        private void RegisterWithMatchManager()
        {
            var matchManager = FindFirstObjectByType<MatchManager>();
            if (matchManager != null)
            {
                matchManager.RegisterPlayer(netId, playerTeam, playerRole);
                
                if (showDebugInfo)
                {
                    Debug.Log($"📝 Player registered: Team {playerTeam}, Role {playerRole}");
                }
            }
        }
        
        private void UpdateTeamColor()
        {
            if (playerVisuals != null)
            {
                playerVisuals.UpdateTeamColor(playerTeam);
            }
        }
        
        public void ToggleBuildMode()
        {
            isInBuildMode = !isInBuildMode;
            
            if (showDebugInfo)
            {
                Debug.Log($"🔨 Build mode: {isInBuildMode}");
            }
            
            // Notify InputManager
            if (InputManager.Instance != null)
            {
                if (isInBuildMode)
                {
                    InputManager.Instance.EnterBuildMode();
                }
                else
                {
                    InputManager.Instance.ExitBuildMode();
                }
            }
        }
        
        public void SetBuildMode(bool enabled)
        {
            isInBuildMode = enabled;
            
            if (showDebugInfo)
            {
                Debug.Log($"🔨 Build mode set to: {enabled}");
            }
        }
        
        public bool IsInBuildMode() => isInBuildMode;
        
        public Camera GetPlayerCamera()
        {
            if (fpsController != null)
            {
                return fpsController.GetCamera();
            }
            return null;
        }
        
        public Team GetPlayerTeam() => playerTeam;
        public RoleId GetPlayerRole() => playerRole;
    }
}