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

            // ✅ CRITICAL FIX: Disable player movement/camera in Lobby phase
            CheckAndUpdatePlayerControls();

            // ✅ CRITICAL FIX: Subscribe to MatchManager phase changes
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnPhaseChangedEvent += OnMatchPhaseChanged;
            }

            if (showDebugInfo)
            {
                Debug.Log("✅ PlayerController initialized for local player");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnPhaseChangedEvent -= OnMatchPhaseChanged;
            }
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Called when match phase changes
        /// </summary>
        private void OnMatchPhaseChanged(Phase newPhase)
        {
            CheckAndUpdatePlayerControls();
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Enable/disable player controls based on match phase
        /// GDD-compliant: Lobby phase = NO gameplay, NO movement, NO shooting
        /// ✅ PUBLIC: Made public so LobbyManager can call it to ensure phase is handled
        /// </summary>
        public void CheckAndUpdatePlayerControls()
        {
            if (!isLocalPlayer) return;

            bool shouldEnableControls = false;
            bool isInLobbyPhase = false;

            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                // ✅ CRITICAL: Only enable controls when NOT in Lobby phase
                // Lobby phase = UI only, no gameplay
                isInLobbyPhase = (currentPhase == Phase.Lobby);
                shouldEnableControls = !isInLobbyPhase;
                
                if (showDebugInfo)
                {
                    Debug.Log($"🎮 [PlayerController] Phase: {currentPhase}, Controls: {(shouldEnableControls ? "ENABLED" : "DISABLED")}");
                }
            }
            else
            {
                // If MatchManager not found, enable controls (legacy mode)
                shouldEnableControls = true;
            }

            // ✅ CRITICAL FIX: Hide player visuals in Lobby phase (so game world is not visible)
            // This prevents the game from looking like it started when player spawns
            if (playerVisuals != null)
            {
                // Hide the visual renderer in lobby phase
                Renderer visualRenderer = playerVisuals.GetComponent<Renderer>();
                if (visualRenderer == null)
                {
                    visualRenderer = playerVisuals.GetComponentInChildren<Renderer>();
                }
                
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = !isInLobbyPhase;
                }
                
                // Also hide the entire PlayerVisuals GameObject if it's a child
                if (playerVisuals.transform != transform && playerVisuals.gameObject != gameObject)
                {
                    playerVisuals.gameObject.SetActive(!isInLobbyPhase);
                }
            }
            
            // ✅ CRITICAL FIX: Hide player model/body in lobby phase
            // Find all renderers in children (player body, weapons, etc.)
            // ✅ CRITICAL: NEVER hide cameras - they're needed for UI rendering
            if (isInLobbyPhase)
            {
                Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in allRenderers)
                {
                    if (renderer == null) continue;
                    
                    // ✅ CRITICAL: Don't hide camera or UI elements
                    // Camera must stay active for UI rendering
                    Camera cam = renderer.GetComponent<Camera>();
                    Canvas canvas = renderer.GetComponent<Canvas>();
                    
                    if (cam == null && canvas == null)
                    {
                        renderer.enabled = false;
                    }
                    else if (cam != null)
                    {
                        // ✅ CRITICAL: Ensure camera stays enabled for UI rendering
                        cam.enabled = true;
                        cam.gameObject.SetActive(true);
                    }
                }
                
                // ✅ CRITICAL: Ensure camera component itself is enabled
                Camera playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    playerCamera.enabled = true;
                    playerCamera.gameObject.SetActive(true);
                }
            }
            else
            {
                // Show all renderers when game starts
                Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in allRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }

            // ✅ CRITICAL: Disable/enable FPSController (movement + camera rotation)
            if (fpsController != null)
            {
                fpsController.enabled = shouldEnableControls;
            }

            // ✅ CRITICAL: Disable/enable WeaponSystem (shooting)
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent
            if (TryGetComponent<Combat.WeaponSystem>(out var weaponSystem))
            {
                weaponSystem.enabled = shouldEnableControls;
            }

            // ✅ CRITICAL: Disable/enable InputManager (all input)
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent
            if (TryGetComponent<InputManager>(out var inputManager))
            {
                if (shouldEnableControls)
                {
                    inputManager.UnblockAllInput();
                }
                else
                {
                    inputManager.BlockAllInput();
                }
            }

            // ✅ CRITICAL: In Lobby phase, unlock cursor for UI interaction
            if (!shouldEnableControls)
            {
                // Lobby phase: Unlock cursor for menu interaction
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("🔓 [PlayerController] Lobby phase - Cursor unlocked, all controls disabled, player hidden");
                }
            }

            // ✅ CRITICAL: Keep camera enabled for UI rendering (FPSController disabled = no rotation)
            // Camera will be controlled by FPSController when it's enabled
        }

        /// <summary>
        /// ✅ REMOVED: Subscribe to UI selection events
        /// Artık RoleSelectionUI ve TeamSelectionUI kullanılmıyor
        /// Yeni akış: MainMenu → LobbyUI (game mode selection dahil)
        /// </summary>
        private void SubscribeToUIEvents()
        {
            // ✅ REMOVED: RoleSelectionUI ve TeamSelectionUI artık kullanılmıyor
            // Yeni basit akış: MainMenu → LobbyUI (game mode selection dahil)
            // Team ve role seçimi artık LobbyUI içinde yapılıyor
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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ MatchManager.Instance is null! Cannot register player.");
                #endif
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
