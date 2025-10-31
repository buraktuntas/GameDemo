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
        [SyncVar]
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

        private void UpdateVisuals()
        {
            if (renderers == null || renderers.Length == 0) return;

            Material mat = team == Team.TeamA ? teamAMaterial : teamBMaterial;
            if (mat != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.material = mat;
                    }
                }
            }
        }

        private int GetStructureHealth(StructureType type)
        {
            return type switch
            {
                StructureType.CoreStructure => GameConstants.CORE_HP,
                StructureType.Wall => GameConstants.WALL_HP,
                StructureType.Platform => GameConstants.PLATFORM_HP,
                StructureType.Ramp => GameConstants.RAMP_HP,
                _ => 100
            };
        }

        [Server]
        private void OnStructureDestroyed()
        {
            if (isCore)
            {
                MatchManager.Instance?.NotifyCoreDestroyed(team);
            }

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

        public static int GetStructureCost(StructureType type)
        {
            return type switch
            {
                StructureType.Wall => 2,
                StructureType.Platform => 3,
                StructureType.Ramp => 2,
                StructureType.TrapSpike => 2,
                StructureType.TrapGlue => 2,
                StructureType.TrapSpringboard => 3,
                StructureType.TrapDartTurret => 4,
                StructureType.UtilityGate => 3,
                _ => 1
            };
        }

        public static StructureCategory GetStructureCategory(StructureType type)
        {
            return type switch
            {
                StructureType.Wall => StructureCategory.Wall,
                StructureType.Platform => StructureCategory.Elevation,
                StructureType.Ramp => StructureCategory.Elevation,
                StructureType.TrapSpike => StructureCategory.Trap,
                StructureType.TrapGlue => StructureCategory.Trap,
                StructureType.TrapSpringboard => StructureCategory.Trap,
                StructureType.TrapDartTurret => StructureCategory.Trap,
                StructureType.UtilityGate => StructureCategory.Utility,
                StructureType.CoreStructure => StructureCategory.Core,
                _ => StructureCategory.Wall
            };
        }
    }
}


