using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Manages blueprint system - save and auto-deploy builds
    /// </summary>
    public class BlueprintSystem : NetworkBehaviour
    {
        public static BlueprintSystem Instance { get; private set; }

        [Header("Blueprint Settings")]
        [SerializeField] private int maxBlueprintsPerPlayer = 5;

        // Server-only storage
        private Dictionary<ulong, List<Blueprint>> playerBlueprints = new Dictionary<ulong, List<Blueprint>>();

        // ✅ PERFORMANCE FIX: Static buffer for Physics.OverlapSphereNonAlloc
        private static Collider[] blueprintColliderBuffer = new Collider[200];

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[BlueprintSystem] Server started");
        }

        /// <summary>
        /// Save current build as blueprint
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSaveBlueprint(string blueprintName, ulong playerId, Vector3 basePosition)
        {
            if (string.IsNullOrEmpty(blueprintName))
            {
                Debug.LogWarning("[BlueprintSystem] Blueprint name cannot be empty");
                return;
            }

            // Get or create player blueprint list
            if (!playerBlueprints.ContainsKey(playerId))
            {
                playerBlueprints[playerId] = new List<Blueprint>();
            }

            var blueprints = playerBlueprints[playerId];

            // Check max blueprints limit
            if (blueprints.Count >= maxBlueprintsPerPlayer)
            {
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} has reached max blueprints ({maxBlueprintsPerPlayer})");
                RpcBlueprintSaveFailed(playerId, "Maximum blueprints reached");
                return;
            }

            // Check if name already exists
            if (blueprints.Exists(b => b.blueprintName == blueprintName))
            {
                Debug.LogWarning($"[BlueprintSystem] Blueprint '{blueprintName}' already exists");
                RpcBlueprintSaveFailed(playerId, "Blueprint name already exists");
                return;
            }

            // Collect structures around base position
            Blueprint blueprint = new Blueprint(blueprintName, playerId);
            CollectStructuresForBlueprint(basePosition, blueprint, playerId);

            if (blueprint.structures.Count == 0)
            {
                Debug.LogWarning("[BlueprintSystem] No structures found to save");
                RpcBlueprintSaveFailed(playerId, "No structures found");
                return;
            }

            // Save blueprint
            blueprints.Add(blueprint);
            RpcBlueprintSaved(playerId, blueprintName, blueprint.structures.Count);
            Debug.Log($"[BlueprintSystem] Saved blueprint '{blueprintName}' with {blueprint.structures.Count} structures");
        }

        [Server]
        private void CollectStructuresForBlueprint(Vector3 basePosition, Blueprint blueprint, ulong playerId)
        {
            // Find all structures within build radius
            float buildRadius = GameConstants.BUILD_MAX_DISTANCE_FROM_SPAWN;

            // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc instead of OverlapSphere
            // buildRadius can be very large (50m), so this is critical for performance
            int count = Physics.OverlapSphereNonAlloc(basePosition, buildRadius, blueprintColliderBuffer);

            for (int i = 0; i < count; i++)
            {
                var structure = blueprintColliderBuffer[i].GetComponent<Structure>();
                if (structure != null && structure.GetOwnerId() == playerId)
                {
                    // Calculate local position relative to base
                    Vector3 localPos = structure.transform.position - basePosition;
                    Quaternion rotation = structure.transform.rotation;
                    StructureType type = structure.GetStructureType();
                    StructureCategory category = GetCategoryForType(type);
                    int cost = GetCostForType(type);

                    blueprint.structures.Add(new BlueprintStructure(type, localPos, rotation, category, cost));
                }
            }
        }

        [Server]
        private StructureCategory GetCategoryForType(StructureType type)
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
                StructureType.InfoTower => StructureCategory.Utility,
                _ => StructureCategory.Wall
            };
        }

        [Server]
        private int GetCostForType(StructureType type)
        {
            // TODO: Get actual cost from Structure or BuildValidator
            return 1; // Placeholder
        }

        /// <summary>
        /// Deploy a saved blueprint
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdDeployBlueprint(string blueprintName, ulong playerId, Vector3 deployPosition)
        {
            if (!playerBlueprints.ContainsKey(playerId))
            {
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} has no blueprints");
                RpcBlueprintDeployFailed(playerId, "No blueprints found");
                return;
            }

            var blueprints = playerBlueprints[playerId];
            var blueprint = blueprints.Find(b => b.blueprintName == blueprintName);

            if (blueprint == null)
            {
                Debug.LogWarning($"[BlueprintSystem] Blueprint '{blueprintName}' not found");
                RpcBlueprintDeployFailed(playerId, "Blueprint not found");
                return;
            }

            // Check if in build phase
            var matchManager = MatchManager.Instance;
            if (matchManager != null && matchManager.GetCurrentPhase() != Phase.Build)
            {
                Debug.LogWarning("[BlueprintSystem] Can only deploy blueprints in build phase");
                RpcBlueprintDeployFailed(playerId, "Not in build phase");
                return;
            }

            // Get player state for budget check
            var playerState = matchManager.GetPlayerState(playerId);
            if (playerState == null)
            {
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} state not found");
                return;
            }

            // Check budget for all structures
            int totalCost = CalculateTotalCost(blueprint);
            if (!HasEnoughBudget(playerState, blueprint))
            {
                Debug.LogWarning($"[BlueprintSystem] Not enough budget to deploy blueprint");
                RpcBlueprintDeployFailed(playerId, "Not enough budget");
                return;
            }

            // Deploy structures
            int deployedCount = 0;
            var buildManager = BuildManager.Instance;
            if (buildManager == null)
            {
                Debug.LogError("[BlueprintSystem] BuildManager not found");
                return;
            }

            foreach (var blueprintStruct in blueprint.structures)
            {
                Vector3 worldPos = deployPosition + blueprintStruct.localPosition;
                BuildRequest request = new BuildRequest(worldPos, blueprintStruct.rotation, blueprintStruct.type, playerId);

                if (buildManager.ValidateAndPlace(request, playerState.team))
                {
                    deployedCount++;
                }
            }

            RpcBlueprintDeployed(playerId, blueprintName, deployedCount);
            Debug.Log($"[BlueprintSystem] Deployed blueprint '{blueprintName}' - {deployedCount}/{blueprint.structures.Count} structures placed");
        }

        [Server]
        private int CalculateTotalCost(Blueprint blueprint)
        {
            int total = 0;
            foreach (var blueprintStruct in blueprint.structures)
            {
                total += blueprintStruct.cost;
            }
            return total;
        }

        [Server]
        private bool HasEnoughBudget(PlayerState playerState, Blueprint blueprint)
        {
            var budget = playerState.budget;
            int wallCost = 0, elevationCost = 0, trapCost = 0, utilityCost = 0;

            foreach (var blueprintStruct in blueprint.structures)
            {
                switch (blueprintStruct.category)
                {
                    case StructureCategory.Wall:
                        wallCost += blueprintStruct.cost;
                        break;
                    case StructureCategory.Elevation:
                        elevationCost += blueprintStruct.cost;
                        break;
                    case StructureCategory.Trap:
                        trapCost += blueprintStruct.cost;
                        break;
                    case StructureCategory.Utility:
                        utilityCost += blueprintStruct.cost;
                        break;
                }
            }

            return budget.wallPoints >= wallCost &&
                   budget.elevationPoints >= elevationCost &&
                   budget.trapPoints >= trapCost &&
                   budget.utilityPoints >= utilityCost;
        }

        /// <summary>
        /// Get list of blueprint names for a player
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdGetBlueprintList(ulong playerId)
        {
            if (!playerBlueprints.ContainsKey(playerId))
            {
                RpcBlueprintList(playerId, new List<string>());
                return;
            }

            var blueprints = playerBlueprints[playerId];
            List<string> names = new List<string>();
            foreach (var blueprint in blueprints)
            {
                names.Add(blueprint.blueprintName);
            }

            RpcBlueprintList(playerId, names);
        }

        [ClientRpc]
        private void RpcBlueprintSaved(ulong playerId, string blueprintName, int structureCount)
        {
            Debug.Log($"[Client] Blueprint '{blueprintName}' saved with {structureCount} structures");
        }

        [ClientRpc]
        private void RpcBlueprintSaveFailed(ulong playerId, string reason)
        {
            Debug.LogWarning($"[Client] Failed to save blueprint: {reason}");
        }

        [ClientRpc]
        private void RpcBlueprintDeployed(ulong playerId, string blueprintName, int deployedCount)
        {
            Debug.Log($"[Client] Blueprint '{blueprintName}' deployed - {deployedCount} structures");
        }

        [ClientRpc]
        private void RpcBlueprintDeployFailed(ulong playerId, string reason)
        {
            Debug.LogWarning($"[Client] Failed to deploy blueprint: {reason}");
        }

        [ClientRpc]
        private void RpcBlueprintList(ulong playerId, List<string> blueprintNames)
        {
            Debug.Log($"[Client] Blueprint list: {string.Join(", ", blueprintNames)}");
        }
    }
}

