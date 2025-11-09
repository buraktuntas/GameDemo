using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;
using TacticalCombat.Traps;
using TacticalCombat.Combat;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Manages building system - placement validation, breakable health, trap linking
    /// Wraps BuildValidator and adds new features
    /// </summary>
    public class BuildManager : NetworkBehaviour
    {
        public static BuildManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private BuildValidator buildValidator;
        [SerializeField] private TrapLinkSystem trapLinkSystem;

        [Header("Build Settings")]
        [SerializeField] private float maxBuildDistanceFromSpawn = GameConstants.BUILD_MAX_DISTANCE_FROM_SPAWN;

        // Server-only tracking
        private Dictionary<uint, Structure> placedStructures = new Dictionary<uint, Structure>();
        private Dictionary<ulong, Vector3> playerSpawnPositions = new Dictionary<ulong, Vector3>();

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
            
            // Find BuildValidator if not assigned
            if (buildValidator == null)
            {
                buildValidator = BuildValidator.Instance;
                if (buildValidator == null)
                {
                    buildValidator = FindFirstObjectByType<BuildValidator>();
                }
            }

            // Find TrapLinkSystem if not assigned
            if (trapLinkSystem == null)
            {
                trapLinkSystem = FindFirstObjectByType<TrapLinkSystem>();
                if (trapLinkSystem == null)
                {
                    GameObject trapLinkObj = new GameObject("[TrapLinkSystem]");
                    trapLinkSystem = trapLinkObj.AddComponent<TrapLinkSystem>();
                }
            }

            Debug.Log("[BuildManager] Server started");
        }

        /// <summary>
        /// Register player spawn position for distance validation
        /// </summary>
        [Server]
        public void RegisterPlayerSpawn(ulong playerId, Vector3 spawnPosition)
        {
            playerSpawnPositions[playerId] = spawnPosition;
        }

        /// <summary>
        /// Validate and place structure (wraps BuildValidator with additional checks)
        /// </summary>
        [Server]
        public bool ValidateAndPlace(BuildRequest request, Team playerTeam)
        {
            // Check if in build phase
            var matchManager = MatchManager.Instance;
            if (matchManager != null && matchManager.GetCurrentPhase() != Phase.Build)
            {
                Debug.LogWarning("[BuildManager] Cannot build - not in build phase");
                return false;
            }

            // Check distance from spawn (personal base limit)
            if (playerSpawnPositions.ContainsKey(request.playerId))
            {
                Vector3 spawnPos = playerSpawnPositions[request.playerId];
                float distance = Vector3.Distance(request.position, spawnPos);
                
                if (distance > maxBuildDistanceFromSpawn)
                {
                    Debug.LogWarning($"[BuildManager] Build too far from spawn: {distance}m (max: {maxBuildDistanceFromSpawn}m)");
                    return false;
                }
            }

            // Use BuildValidator for placement validation
            if (buildValidator != null)
            {
                bool valid = buildValidator.ValidateAndPlace(request, playerTeam);

                if (valid)
                {
                    // ✅ CRITICAL PERFORMANCE FIX: Get spawned structure directly from BuildValidator
                    // Instead of Physics.OverlapSphere search in coroutine
                    GameObject spawnedObj = buildValidator.GetLastSpawnedStructure();
                    if (spawnedObj != null)
                    {
                        var structure = spawnedObj.GetComponent<Structure>();
                        if (structure != null)
                        {
                            placedStructures[structure.netId] = structure;

                            // Initialize structure health if breakable
                            if (structure.GetComponent<Health>() == null)
                            {
                                var health = structure.gameObject.AddComponent<TacticalCombat.Combat.Health>();
                                // Health will be set by Structure component
                            }
                        }
                    }
                }

                return valid;
            }

            return false;
        }

        // ✅ PERFORMANCE FIX: TrackStructureAfterPlacement coroutine removed
        // Structure is now tracked immediately using BuildValidator.GetLastSpawnedStructure()

        /// <summary>
        /// Link traps together (chain trigger system)
        /// </summary>
        [Server]
        public bool LinkTraps(uint trap1Id, uint trap2Id)
        {
            if (trapLinkSystem == null)
            {
                Debug.LogWarning("[BuildManager] TrapLinkSystem not found");
                return false;
            }

            return trapLinkSystem.LinkTraps(trap1Id, trap2Id);
        }

        /// <summary>
        /// Get structure by network ID
        /// </summary>
        [Server]
        public Structure GetStructure(uint netId)
        {
            return placedStructures.ContainsKey(netId) ? placedStructures[netId] : null;
        }

        /// <summary>
        /// Notify structure destroyed (for cleanup)
        /// </summary>
        [Server]
        public void OnStructureDestroyed(uint netId)
        {
            if (placedStructures.ContainsKey(netId))
            {
                placedStructures.Remove(netId);
                
                // Remove from trap links if it was a trap
                if (trapLinkSystem != null)
                {
                    trapLinkSystem.RemoveTrap(netId);
                }
            }
        }

        /// <summary>
        /// Award score for structure built
        /// </summary>
        [Server]
        public void AwardStructureBuilt(ulong playerId)
        {
            var scoreManager = Core.ScoreManager.Instance;
            if (scoreManager != null)
            {
                scoreManager.AwardStructureBuilt(playerId);
            }
        }
    }
}

