// [FORCE SYNC 1.1] - Restoration of PlayerController API
using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    public class PlayerController : NetworkBehaviour
    {
        public static PlayerController LocalPlayer { get; private set; }

        [Header("Network State")]
        [SyncVar] public Team team = Team.TeamA;
        [SyncVar] public RoleId role = RoleId.Builder;
        [SyncVar] public ulong playerId;
        
        [SyncVar] private bool isCarryingCore = false;
        [SyncVar] private ulong carriedCoreOwnerId = 0;
        
        private PlayerVisuals playerVisuals;
        private FPSController fpsController;
        
        private void Awake()
        {
            playerVisuals = GetComponent<PlayerVisuals>();
            fpsController = GetComponent<FPSController>();
            playerId = netId;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            RegisterWithMatchManager();
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayer = this;
            UpdateTeamColor();
        }

        private void OnDestroy()
        {
            if (isLocalPlayer && LocalPlayer == this) LocalPlayer = null;
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
        
        public void CheckAndUpdatePlayerControls()
        {
            // Placeholder for state refresh logic
        }
    }
}
