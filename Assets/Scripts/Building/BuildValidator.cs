using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    public class BuildValidator : NetworkBehaviour
    {
        // ✅ PERFORMANCE FIX: Singleton pattern to avoid FindFirstObjectByType
        public static BuildValidator Instance { get; private set; }

        [Header("Validation Settings")]
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float minDistanceBetweenStructures = 0.5f;

        [Header("Structure Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private GameObject rampPrefab;

        private void Awake()
        {
            // ✅ Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("⚠️ [BuildValidator] Multiple instances detected! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [Server]
        public bool ValidateAndPlace(BuildRequest request, Team team)
        {
            // Phase check
            if (MatchManager.Instance.GetCurrentPhase() != Phase.Build)
            {
                Debug.Log("Cannot build - not in Build phase");
                return false;
            }

            // Get player state
            var playerState = MatchManager.Instance.GetPlayerState(request.playerId);
            if (playerState == null)
            {
                Debug.Log("Player state not found");
                return false;
            }

            // Get structure info
            StructureCategory category = Structure.GetStructureCategory(request.type);
            int cost = Structure.GetStructureCost(request.type);

            // Validation checks BEFORE spending budget
            Collider[] overlaps = Physics.OverlapSphere(request.position, minDistanceBetweenStructures, obstacleMask);
            if (overlaps.Length > 0)
            {
                Debug.Log("Placement overlaps with existing structure");
                return false;
            }

            // Spend budget last (after all validation passes)
            if (!MatchManager.Instance.SpendBudget(request.playerId, category, cost))
            {
                Debug.Log($"Insufficient budget for {request.type}");
                return false;
            }

            // All checks passed - spawn structure
            SpawnStructure(request, team);
            return true;
        }

        [Server]
        private void SpawnStructure(BuildRequest request, Team team)
        {
            GameObject prefab = GetStructurePrefab(request.type);
            if (prefab == null)
            {
                Debug.LogWarning($"No prefab found for {request.type}");
                return;
            }

            // Use NetworkObjectPool if available (prevents Instantiate/Destroy GC spikes)
            GameObject structureObj;
            if (NetworkObjectPool.Instance != null)
            {
                structureObj = NetworkObjectPool.Instance.Get(prefab, request.position, request.rotation);
            }
            else
            {
                structureObj = Instantiate(prefab, request.position, request.rotation);
                NetworkServer.Spawn(structureObj);
            }

            // Initialize structure
            Structure structure = structureObj.GetComponent<Structure>();
            if (structure != null)
            {
                structure.Initialize(team, request.type, Structure.GetStructureCategory(request.type), request.playerId);
            }
        }

        private GameObject GetStructurePrefab(StructureType type)
        {
            return type switch
            {
                StructureType.Wall => wallPrefab,
                StructureType.Platform => platformPrefab,
                StructureType.Ramp => rampPrefab,
                _ => wallPrefab
            };
        }
    }
}



