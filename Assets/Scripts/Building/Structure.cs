using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Combat;

namespace TacticalCombat.Building
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(StructuralIntegrity))]
    public class Structure : NetworkBehaviour
    {
        [Header("Structure Info")]
        [SyncVar(hook = nameof(OnTeamChanged))]
        public Team team;

        [SyncVar]
        public StructureType structureType;

        [SyncVar]
        public StructureCategory category;

        [SyncVar]
        public ulong ownerId;

        [Header("Visual")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material teamAMaterial;
        [SerializeField] private Material teamBMaterial;

        [Header("Destruction Effects")]
        [SerializeField] private GameObject destructionEffect;
        [SerializeField] private AudioClip destructionSound;

        private Health health;
        private bool isCore = false;

        private void Awake()
        {
            health = GetComponent<Health>();
            isCore = (structureType == StructureType.CoreStructure);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // Set health based on structure type
            int hp = GetStructureHealth(structureType);
            health.SetMaxHealth(hp);

            // Subscribe to death
            health.OnDeathEvent += OnStructureDestroyed;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdateVisuals();
        }

        public void Initialize(Team ownerTeam, StructureType type, StructureCategory cat, ulong owner)
        {
            team = ownerTeam;
            structureType = type;
            category = cat;
            ownerId = owner;
            UpdateVisuals();
        }

        /// <summary>
        /// ✅ HIGH PRIORITY: SyncVar hook for team changes (update visuals when team changes)
        /// </summary>
        private void OnTeamChanged(Team oldTeam, Team newTeam)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (renderers == null || renderers.Length == 0) return;

            Material mat = team == Team.TeamA ? teamAMaterial : teamBMaterial;
            if (mat != null)
            {
                // ✅ CRITICAL FIX: Use sharedMaterial to prevent memory leak
                // rend.material creates new instance every time!
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.sharedMaterial = mat;  // No instance created - prevents leak
                    }
                }
            }
        }

        /// <summary>
        /// ✅ FIX: Made public static for StructureSO access
        /// </summary>
        public static int GetStructureHealth(StructureType type)
        {
            return type switch
            {
                // Core
                StructureType.CoreStructure => GameConstants.CORE_HP,
                
                // Walls
                StructureType.WoodWall => GameConstants.WOOD_WALL_HP,
                StructureType.MetalWall => GameConstants.METAL_WALL_HP,
                
                // Elevation
                StructureType.Platform => GameConstants.PLATFORM_HP,
                StructureType.Ramp => GameConstants.RAMP_HP,
                
                // Utility
                StructureType.UtilityGate => GameConstants.GATE_HP,
                StructureType.MotionSensor => GameConstants.MOTION_SENSOR_HP,
                StructureType.InfoTower => GameConstants.INFO_TOWER_HP,
                
                // Traps (traps don't have health, they trigger once)
                _ => 100
            };
        }

        [Server]
        private void OnStructureDestroyed()
        {
            if (isCore)
            {
                Team winner = team == Team.TeamA ? Team.TeamB : Team.TeamA;
                MatchManager.Instance?.OnCoreDestroyed(winner);
            }

            // Play destruction effects
            RpcPlayDestructionEffects();

            // ✅ MEMORY LEAK FIX: Unsubscribe from event before destruction
            if (health != null)
            {
                health.OnDeathEvent -= OnStructureDestroyed;
            }

            // Delayed destruction to allow effects to play
            Invoke(nameof(DestroyStructure), 0.5f);
        }

        [Server]
        private void DestroyStructure()
        {
            // Return to pool if available, otherwise destroy
            if (NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.Release(gameObject);
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        /// <summary>
        /// ✅ HIGH PRIORITY: Public method to trigger destruction effects (called from StructuralIntegrity)
        /// </summary>
        [Server]
        public void TriggerDestructionEffects()
        {
            RpcPlayDestructionEffects();
        }

        [ClientRpc]
        private void RpcPlayDestructionEffects()
        {
            // Spawn destruction particle effect
            if (destructionEffect != null)
            {
                GameObject effect = Instantiate(destructionEffect, transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }

            // Play destruction sound
            if (destructionSound != null)
            {
                AudioSource.PlayClipAtPoint(destructionSound, transform.position);
            }

            // Hide renderers
            if (renderers != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.enabled = false;
                    }
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"💥 Structure destroyed: {structureType} (Team: {team})");
            #endif
        }

        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Cancel Invoke on destroy to prevent memory leaks
            CancelInvoke(nameof(DestroyStructure)); // Cancel delayed destruction if object destroyed early
            
            // ✅ MEMORY LEAK FIX: Cleanup event subscription on destroy
            if (health != null)
            {
                health.OnDeathEvent -= OnStructureDestroyed;
            }
        }

        public static int GetStructureCost(StructureType type)
        {
            return type switch
            {
                // Walls (GDD: Düşük/Orta maliyet)
                StructureType.WoodWall => 2,      // Düşük maliyet
                StructureType.MetalWall => 4,      // Orta maliyet
                
                // Elevation
                StructureType.Platform => 3,
                StructureType.Ramp => 2,
                
                // Traps (GDD: Orta/Yüksek maliyet)
                StructureType.TrapSpike => 3,      // Orta maliyet
                StructureType.TrapGlue => 3,      // Orta maliyet
                StructureType.TrapElectric => 5,  // Yüksek maliyet
                StructureType.TrapSpringboard => 3,
                StructureType.TrapDartTurret => 4,
                
                // Utility
                StructureType.UtilityGate => 3,    // Orta maliyet
                StructureType.MotionSensor => 1,  // Düşük maliyet (çok düşük dayanıklılık)
                StructureType.InfoTower => 5,
                
                _ => 1
            };
        }

        public static StructureCategory GetStructureCategory(StructureType type)
        {
            return type switch
            {
                // Walls
                StructureType.WoodWall => StructureCategory.Wall,
                StructureType.MetalWall => StructureCategory.Wall,
                
                // Elevation
                StructureType.Platform => StructureCategory.Elevation,
                StructureType.Ramp => StructureCategory.Elevation,
                
                // Traps
                StructureType.TrapSpike => StructureCategory.Trap,
                StructureType.TrapGlue => StructureCategory.Trap,
                StructureType.TrapElectric => StructureCategory.Trap,
                StructureType.TrapSpringboard => StructureCategory.Trap,
                StructureType.TrapDartTurret => StructureCategory.Trap,
                
                // Utility
                StructureType.UtilityGate => StructureCategory.Utility,
                StructureType.MotionSensor => StructureCategory.Utility,
                StructureType.InfoTower => StructureCategory.Utility,
                
                // Core
                StructureType.CoreStructure => StructureCategory.Core,
                
                _ => StructureCategory.Wall
            };
        }

        /// <summary>
        /// Get structure type (for BlueprintSystem)
        /// </summary>
        public StructureType GetStructureType()
        {
            return structureType;
        }

        /// <summary>
        /// Get owner ID (for BlueprintSystem)
        /// </summary>
        public ulong GetOwnerId()
        {
            return ownerId;
        }
    }
}


