// [FORCE SYNC 1.1] - Restoration of PlayerController API
using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    public class PlayerController : NetworkBehaviour
    {
        /// <summary>
        /// Global reference to the local player.
        /// NOTE: Assumes a single local player (no split-screen support).
        /// </summary>
        public static PlayerController LocalPlayer { get; private set; }

        [Header("Network State")]
        [SyncVar(hook = nameof(OnTeamChanged))] 
        public Team team = Team.TeamA;
        
        [SyncVar(hook = nameof(OnRoleChanged))] 
        public RoleId role = RoleId.Builder;
        
        [SyncVar] public ulong playerId;
        
        [SyncVar] private bool isCarryingCore = false;
        [SyncVar] private ulong carriedCoreOwnerId = 0;
        
        private PlayerVisuals playerVisuals;
        private FPSController fpsController;
        
        private void Awake()
        {
            playerVisuals = GetComponent<PlayerVisuals>();
            fpsController = GetComponent<FPSController>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerId = netId; // ✅ FIX: Correct timing for netId assignment
            
            RegisterWithMatchManager();

            // ✅ PERFORMANCE: Invalidate cache when new player joins
            Core.ComponentCache.InvalidateAll();
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayer = this;
            UpdateTeamColor();
        }
        
        private void OnTeamChanged(Team oldTeam, Team newTeam)
        {
            UpdateTeamColor();
            // Optional: Notify UI or other systems
        }

        private void OnRoleChanged(RoleId oldRole, RoleId newRole)
        {
            // Optional: Update visuals based on role
        }

        private void OnDestroy()
        {
            if (isLocalPlayer && LocalPlayer == this) LocalPlayer = null;

            // ✅ PERFORMANCE: Invalidate cache when player leaves
            Core.ComponentCache.InvalidateAll();

            // ✅ FIX: Unregister from MatchManager to prevent crash
            if (isServer && MatchManager.Instance != null)
            {
                MatchManager.Instance.UnregisterPlayer(netId);
            }

            // ✅ MEMORY LEAK FIX: Cleanup placement times dictionary
            if (isServer)
            {
                Building.SimpleBuildMode.CleanupPlayerPlacementTimes(netId);
            }
        }

        private void RegisterWithMatchManager() 
        { 
            if (MatchManager.Instance != null) MatchManager.Instance.RegisterPlayer(netId, team, role); 
        }
        
        public void UpdateTeamColor() 
        { 
            if (playerVisuals != null) playerVisuals.SetTeamColor(team); 
        }

        public Team GetTeam() => team;
        public Team GetPlayerTeam() => team; // Legacy support
        public RoleId GetRole() => role;

        [Server]
        public void SetCarryingCore(bool carrying, ulong ownerId = 0)
        {
            isCarryingCore = carrying;
            carriedCoreOwnerId = ownerId;
        }

        public bool IsCarryingCore() => isCarryingCore;
    }
}
