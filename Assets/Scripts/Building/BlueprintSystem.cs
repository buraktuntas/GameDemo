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
            
            // Subscribe to build phase start for auto-deploy
            var matchManager = MatchManager.Instance;
            if (matchManager != null)
            {
                matchManager.OnPhaseChangedEvent += OnPhaseChanged;
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[BlueprintSystem] Server started - Auto-deploy enabled");
            #endif
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            var matchManager = MatchManager.Instance;
            if (matchManager != null)
            {
                matchManager.OnPhaseChangedEvent -= OnPhaseChanged;
            }
        }
        
        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Build)
            {
                // Auto-deploy last blueprint for all players when build phase starts
                StartCoroutine(AutoDeployAllBlueprints());
            }
        }
        
        /// <summary>
        /// Auto-deploy last blueprint for all players at build phase start
        /// </summary>
        [Server]
        private System.Collections.IEnumerator AutoDeployAllBlueprints()
        {
            var matchManager = MatchManager.Instance;
            if (matchManager == null) yield break;
            
            // Wait a short delay to ensure all systems are initialized
            yield return new WaitForSeconds(0.5f);
            
            foreach (var kvp in playerBlueprints)
            {
                ulong playerId = kvp.Key;
                var blueprints = kvp.Value;
                
                if (blueprints.Count == 0) continue;
                
                // Get last blueprint (most recently saved)
                var lastBlueprint = blueprints[blueprints.Count - 1];
                
                // Get player spawn position
                Vector3 deployPosition = GetPlayerSpawnPosition(playerId);
                if (deployPosition == Vector3.zero) continue;
                
                // Deploy blueprint (server-side, no command needed)
                DeployBlueprintInternal(lastBlueprint, playerId, deployPosition);
                
                // Small delay between players to prevent lag spike
                yield return new WaitForSeconds(GameConstants.BLUEPRINT_AUTO_DEPLOY_DELAY);
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[BlueprintSystem] Auto-deploy completed for all players");
            #endif
        }
        
        [Server]
        private Vector3 GetPlayerSpawnPosition(ulong playerId)
        {
            // Try to get from BuildManager
            var buildManager = BuildManager.Instance;
            if (buildManager != null)
            {
                // BuildManager should have player spawn positions registered
                // For now, try to find player GameObject
                var player = FindPlayerById(playerId);
                if (player != null)
                {
                    return player.transform.position;
                }
            }
            
            // Fallback: Try to find spawn point by tag
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPoints.Length > 0)
            {
                // Return first spawn point (should be improved with team-based spawns)
                return spawnPoints[0].transform.position;
            }
            
            return Vector3.zero;
        }
        
        [Server]
        private UnityEngine.GameObject FindPlayerById(ulong playerId)
        {
            // Use NetworkServer.spawned for O(1) lookup
            uint netId = (uint)playerId;
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                return identity.gameObject;
            }
            return null;
        }
        
        /// <summary>
        /// Internal blueprint deployment (server-side, no validation needed)
        /// </summary>
        [Server]
        private void DeployBlueprintInternal(Blueprint blueprint, ulong playerId, Vector3 deployPosition)
        {
            var matchManager = MatchManager.Instance;
            if (matchManager == null) return;
            
            var playerState = matchManager.GetPlayerState(playerId);
            if (playerState == null) return;
            
            var buildManager = BuildManager.Instance;
            if (buildManager == null) return;
            
            int deployedCount = 0;
            
            foreach (var blueprintStruct in blueprint.structures)
            {
                Vector3 worldPos = deployPosition + blueprintStruct.localPosition;
                BuildRequest request = new BuildRequest(worldPos, blueprintStruct.rotation, blueprintStruct.type, playerId);
                
                if (buildManager.ValidateAndPlace(request, playerState.team))
                {
                    deployedCount++;
                }
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlueprintSystem] Auto-deployed blueprint '{blueprint.blueprintName}' for player {playerId} - {deployedCount}/{blueprint.structures.Count} structures");
            #endif
            
            // Notify client
            RpcBlueprintAutoDeployed(playerId, blueprint.blueprintName, deployedCount);
        }

        /// <summary>
        /// Save current build as blueprint
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSaveBlueprint(string blueprintName, ulong playerId, Vector3 basePosition)
        {
            if (string.IsNullOrEmpty(blueprintName))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BlueprintSystem] Blueprint name cannot be empty");
                #endif
                RpcBlueprintSaveFailed(playerId, "Blueprint name cannot be empty");
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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} has reached max blueprints ({maxBlueprintsPerPlayer})");
                #endif
                RpcBlueprintSaveFailed(playerId, "Maximum blueprints reached");
                return;
            }

            // Check if name already exists
            if (blueprints.Exists(b => b.blueprintName == blueprintName))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Blueprint '{blueprintName}' already exists");
                #endif
                RpcBlueprintSaveFailed(playerId, "Blueprint name already exists");
                return;
            }

            // Collect structures around base position
            Blueprint blueprint = new Blueprint(blueprintName, playerId);
            CollectStructuresForBlueprint(basePosition, blueprint, playerId);

            if (blueprint.structures.Count == 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BlueprintSystem] No structures found to save");
                #endif
                RpcBlueprintSaveFailed(playerId, "No structures found");
                return;
            }

            // Save blueprint
            blueprints.Add(blueprint);
            RpcBlueprintSaved(playerId, blueprintName, blueprint.structures.Count);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlueprintSystem] Saved blueprint '{blueprintName}' with {blueprint.structures.Count} structures");
            #endif
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
            // Use Structure's static method for consistency
            return Structure.GetStructureCategory(type);
        }

        [Server]
        private int GetCostForType(StructureType type)
        {
            // Use Structure's static method for consistency
            return Structure.GetStructureCost(type);
        }

        /// <summary>
        /// Deploy a saved blueprint
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdDeployBlueprint(string blueprintName, ulong playerId, Vector3 deployPosition)
        {
            if (!playerBlueprints.ContainsKey(playerId))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} has no blueprints");
                #endif
                RpcBlueprintDeployFailed(playerId, "No blueprints found");
                return;
            }

            var blueprints = playerBlueprints[playerId];
            var blueprint = blueprints.Find(b => b.blueprintName == blueprintName);

            if (blueprint == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Blueprint '{blueprintName}' not found");
                #endif
                RpcBlueprintDeployFailed(playerId, "Blueprint not found");
                return;
            }

            // Check if in build phase
            var matchManager = MatchManager.Instance;
            if (matchManager != null && matchManager.GetCurrentPhase() != Phase.Build)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BlueprintSystem] Can only deploy blueprints in build phase");
                #endif
                RpcBlueprintDeployFailed(playerId, "Not in build phase");
                return;
            }

            // Get player state for budget check
            var playerState = matchManager.GetPlayerState(playerId);
            if (playerState == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Player {playerId} state not found");
                #endif
                return;
            }

            // Check budget for all structures
            int totalCost = CalculateTotalCost(blueprint);
            if (!HasEnoughBudget(playerState, blueprint))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BlueprintSystem] Not enough budget to deploy blueprint");
                #endif
                RpcBlueprintDeployFailed(playerId, "Not enough budget");
                return;
            }

            // Deploy structures
            int deployedCount = 0;
            var buildManager = BuildManager.Instance;
            if (buildManager == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[BlueprintSystem] BuildManager not found");
                #endif
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
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BlueprintSystem] Deployed blueprint '{blueprintName}' - {deployedCount}/{blueprint.structures.Count} structures placed");
            #endif
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
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Blueprint '{blueprintName}' saved with {structureCount} structures");
            #endif
        }

        [ClientRpc]
        private void RpcBlueprintSaveFailed(ulong playerId, string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[Client] Failed to save blueprint: {reason}");
            #endif
        }

        [ClientRpc]
        private void RpcBlueprintDeployed(ulong playerId, string blueprintName, int deployedCount)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Blueprint '{blueprintName}' deployed - {deployedCount} structures");
            #endif
        }

        [ClientRpc]
        private void RpcBlueprintDeployFailed(ulong playerId, string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[Client] Failed to deploy blueprint: {reason}");
            #endif
        }

        [ClientRpc]
        private void RpcBlueprintList(ulong playerId, List<string> blueprintNames)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Blueprint list: {string.Join(", ", blueprintNames)}");
            #endif
        }
        
        [ClientRpc]
        private void RpcBlueprintAutoDeployed(ulong playerId, string blueprintName, int deployedCount)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Blueprint '{blueprintName}' auto-deployed - {deployedCount} structures");
            #endif
        }
    }
}

