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
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Components
        private PlayerVisuals playerVisuals;
        
        private void Awake()
        {
            playerVisuals = GetComponent<PlayerVisuals>();
            playerId = netId; // Network ID'yi set et
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
        
        private void RegisterWithMatchManager()
        {
            // ✅ FIX: Sadece server'da register et
            if (!isServer) return;
            
            var matchManager = FindFirstObjectByType<MatchManager>();
            if (matchManager != null)
            {
                matchManager.RegisterPlayer(netId, team, role);
                
                if (showDebugInfo)
                {
                    Debug.Log($"📝 Player registered: Team {team}, Role {role}");
                }
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
    }
}