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
        // ✅ REMOVED: maxBuildDistanceFromSpawn - using buildZoneSize instead (30x30m zone)
        [SerializeField] private float buildZoneSize = GameConstants.BUILD_ZONE_SIZE; // 30x30m build zone

        [Header("Build Phase State")]
        [SyncVar] private bool isBuildPhase = false;

        // Server-only tracking
        private Dictionary<uint, Structure> placedStructures = new Dictionary<uint, Structure>();
        private Dictionary<ulong, Vector3> playerSpawnPositions = new Dictionary<ulong, Vector3>();
        private Dictionary<ulong, int> playerStructureCounts = new Dictionary<ulong, int>(); // Track structure count per player
        private Dictionary<ulong, int> playerTrapCounts = new Dictionary<ulong, int>(); // Track trap count per player

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
            
            // ✅ CORE STABILITY: Find BuildValidator if not assigned (null-safe)
            if (buildValidator == null)
            {
                buildValidator = BuildValidator.Instance;
                if (buildValidator == null)
                {
                    buildValidator = FindFirstObjectByType<BuildValidator>();
                }
                
                if (buildValidator == null)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[BuildManager] BuildValidator not found - build validation may not work correctly");
                    #endif
                }
            }

            // ✅ CORE STABILITY: Find TrapLinkSystem if not assigned (null-safe)
            if (trapLinkSystem == null)
            {
                trapLinkSystem = FindFirstObjectByType<TrapLinkSystem>();
                if (trapLinkSystem == null)
                {
                    // Auto-create TrapLinkSystem if not found
                    GameObject trapLinkObj = new GameObject("[TrapLinkSystem]");
                    trapLinkSystem = trapLinkObj.AddComponent<TrapLinkSystem>();
                    Debug.Log("[BuildManager] Auto-created TrapLinkSystem");
                }
            }

            Debug.Log("[BuildManager] Server started");
        }
        
        /// <summary>
        /// ✅ NEW: Begin build phase (called from MatchManager)
        /// </summary>
        [Server]
        public void BeginBuildPhase()
        {
            isBuildPhase = true;
            
            // Reset structure counts
            playerStructureCounts.Clear();
            playerTrapCounts.Clear();
            
            // ✅ CORE STABILITY: Initialize counts for all players (null-safe)
            var matchManager = MatchManager.Instance;
            if (matchManager != null)
            {
                var allStates = matchManager.GetAllPlayerStates();
                if (allStates != null)
                {
                    foreach (var kvp in allStates)
                    {
                        playerStructureCounts[kvp.Key] = 0;
                        playerTrapCounts[kvp.Key] = 0;
                    }
                }
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BuildManager] MatchManager.Instance is null - cannot initialize player structure counts");
                #endif
            }
            
            RpcBuildPhaseChanged(true);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[BuildManager] Build phase started");
            #endif
        }
        
        /// <summary>
        /// ✅ NEW: End build phase (called from MatchManager)
        /// </summary>
        [Server]
        public void EndBuildPhase()
        {
            isBuildPhase = false;
            RpcBuildPhaseChanged(false);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[BuildManager] Build phase ended");
            #endif
        }
        
        [ClientRpc]
        private void RpcBuildPhaseChanged(bool enabled)
        {
            isBuildPhase = enabled;
            // UI can subscribe to this or check isBuildPhase
        }
        
        /// <summary>
        /// ✅ NEW: Check if position is within build zone for player
        /// </summary>
        [Server]
        private bool IsInBuildZone(ulong playerId, Vector3 position)
        {
            if (!playerSpawnPositions.ContainsKey(playerId))
                return false;
            
            Vector3 spawnPos = playerSpawnPositions[playerId];
            float distance = Vector3.Distance(position, spawnPos);
            
            // Build zone is 30x30m square (or circle with 15m radius)
            return distance <= buildZoneSize * 0.5f; // 15m radius = 30m diameter
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
            // ✅ NEW: Check build phase state
            if (!isBuildPhase)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BuildManager] Cannot build - not in build phase");
                #endif
                return false;
            }

            // ✅ NEW: Check build zone (30x30m safe zone)
            if (!IsInBuildZone(request.playerId, request.position))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BuildManager] Build outside build zone (30x30m)");
                #endif
                return false;
            }
            
            // ✅ NEW: Check structure limits
            if (!playerStructureCounts.ContainsKey(request.playerId))
            {
                playerStructureCounts[request.playerId] = 0;
            }
            
            // Check if structure is a trap
            bool isTrap = Structure.GetStructureCategory(request.type) == StructureCategory.Trap;
            
            if (isTrap)
            {
                if (!playerTrapCounts.ContainsKey(request.playerId))
                {
                    playerTrapCounts[request.playerId] = 0;
                }
                
                if (playerTrapCounts[request.playerId] >= GameConstants.MAX_TRAPS_PER_PLAYER)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[BuildManager] Trap limit reached: {playerTrapCounts[request.playerId]}/{GameConstants.MAX_TRAPS_PER_PLAYER}");
                    #endif
                    return false;
                }
            }
            
            if (playerStructureCounts[request.playerId] >= GameConstants.MAX_STRUCTURES_PER_PLAYER)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BuildManager] Structure limit reached: {playerStructureCounts[request.playerId]}/{GameConstants.MAX_STRUCTURES_PER_PLAYER}");
                #endif
                return false;
            }
            
            // ✅ NEW: Check total structure count (map-wide limit)
            int totalStructures = 0;
            foreach (var count in playerStructureCounts.Values)
            {
                totalStructures += count;
            }
            
            if (totalStructures >= GameConstants.MAX_TOTAL_STRUCTURES)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[BuildManager] Total structure limit reached: {totalStructures}/{GameConstants.MAX_TOTAL_STRUCTURES}");
                #endif
                return false;
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
                            
                            // ✅ NEW: Increment structure count
                            playerStructureCounts[request.playerId]++;
                            
                            // ✅ NEW: Increment trap count if trap
                            if (isTrap)
                            {
                                playerTrapCounts[request.playerId]++;
                            }

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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BuildManager] TrapLinkSystem not found");
                #endif
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
                var structure = placedStructures[netId];
                ulong ownerId = structure.GetOwnerId();
                
                // ✅ NEW: Decrement structure count
                if (playerStructureCounts.ContainsKey(ownerId))
                {
                    playerStructureCounts[ownerId]--;
                }
                
                // ✅ NEW: Decrement trap count if trap
                if (Structure.GetStructureCategory(structure.GetStructureType()) == StructureCategory.Trap)
                {
                    if (playerTrapCounts.ContainsKey(ownerId))
                    {
                        playerTrapCounts[ownerId]--;
                    }
                }
                
                placedStructures.Remove(netId);
                
                // Remove from trap links if it was a trap
                if (trapLinkSystem != null)
                {
                    trapLinkSystem.RemoveTrap(netId);
                }
            }
        }
        
        /// <summary>
        /// ✅ NEW: Get structure count for player (for UI)
        /// </summary>
        [Server]
        public int GetPlayerStructureCount(ulong playerId)
        {
            return playerStructureCounts.ContainsKey(playerId) ? playerStructureCounts[playerId] : 0;
        }
        
        /// <summary>
        /// ✅ NEW: Get trap count for player (for UI)
        /// </summary>
        [Server]
        public int GetPlayerTrapCount(ulong playerId)
        {
            return playerTrapCounts.ContainsKey(playerId) ? playerTrapCounts[playerId] : 0;
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

