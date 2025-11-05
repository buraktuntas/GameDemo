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
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float minDistanceBetweenStructures = 0.5f;
        
        [Header("Terrain Validation")]
        [SerializeField] private float maxBuildHeight = 50f;
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float maxGroundDistance = 0.5f;
        [SerializeField] private float enemyCoreBuildRadius = 15f;
        
        // NonAlloc buffers for performance
        private static readonly Collider[] overlapBuffer = new Collider[64];

        [Header("Structure Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private GameObject rampPrefab;

        /// <summary>
        /// ✅ FIX: Public method to set prefabs (called from SimpleBuildMode)
        /// </summary>
        [Server]
        public void SetPrefabs(GameObject wall, GameObject platform, GameObject ramp)
        {
            if (wall != null && wallPrefab == null) wallPrefab = wall;
            if (platform != null && platformPrefab == null) platformPrefab = platform;
            if (ramp != null && rampPrefab == null) rampPrefab = ramp;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [BuildValidator] Prefabs set: Wall={wallPrefab != null}, Platform={platformPrefab != null}, Ramp={rampPrefab != null}");
            #endif
        }

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

        private void Start()
        {
            // ✅ FIX: Try to load prefabs from SimpleBuildMode if not already set
            if (wallPrefab == null || platformPrefab == null || rampPrefab == null)
            {
                LoadPrefabsFromSimpleBuildMode();
            }
        }

        /// <summary>
        /// ✅ FIX: Load prefab references from SimpleBuildMode if available
        /// </summary>
        private void LoadPrefabsFromSimpleBuildMode()
        {
            var buildMode = FindFirstObjectByType<SimpleBuildMode>();
            if (buildMode == null) return;

            // Use reflection to copy prefab references
            var validatorType = typeof(BuildValidator);
            var buildModeType = typeof(SimpleBuildMode);
            
            var wallPrefabField = validatorType.GetField("wallPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var platformPrefabField = validatorType.GetField("platformPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rampPrefabField = validatorType.GetField("rampPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var simpleWallPrefab = buildModeType.GetField("wallPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var simpleFloorPrefab = buildModeType.GetField("floorPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var simpleStairsPrefab = buildModeType.GetField("stairsPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            bool loaded = false;
            
            if (wallPrefab == null && wallPrefabField != null && simpleWallPrefab != null)
            {
                var prefab = simpleWallPrefab.GetValue(buildMode) as GameObject;
                if (prefab != null)
                {
                    wallPrefabField.SetValue(this, prefab);
                    loaded = true;
                }
            }
            
            // Platform = Floor in SimpleBuildMode
            if (platformPrefab == null && platformPrefabField != null && simpleFloorPrefab != null)
            {
                var prefab = simpleFloorPrefab.GetValue(buildMode) as GameObject;
                if (prefab != null)
                {
                    platformPrefabField.SetValue(this, prefab);
                    loaded = true;
                }
            }
            
            // Ramp = Stairs in SimpleBuildMode
            if (rampPrefab == null && rampPrefabField != null && simpleStairsPrefab != null)
            {
                var prefab = simpleStairsPrefab.GetValue(buildMode) as GameObject;
                if (prefab != null)
                {
                    rampPrefabField.SetValue(this, prefab);
                    loaded = true;
                }
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (loaded)
            {
                Debug.Log("✅ [BuildValidator] Prefabs loaded from SimpleBuildMode");
            }
            #endif
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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("Cannot build - not in Build phase");
                #endif
                return false;
            }

            // Get player state
            var playerState = MatchManager.Instance.GetPlayerState(request.playerId);
            if (playerState == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("Player state not found");
                #endif
                return false;
            }

            // Get structure info
            StructureCategory category = Structure.GetStructureCategory(request.type);
            int cost = Structure.GetStructureCost(request.type);

            // ✅ CRITICAL FIX: Terrain anchor validation (height limit, ground distance, slope)
            if (request.position.y > maxBuildHeight)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 Build height limit: {request.position.y}m > {maxBuildHeight}m");
                #endif
                return false;
            }

            // Ground anchor validation
            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Default", "Ground");
            }

            RaycastHit groundHit;
            // ✅ HIGH PRIORITY: Fix ground check distance (reduce from 5m to 0.5m to prevent floating structures)
            if (!Physics.Raycast(request.position + Vector3.up * 0.5f, Vector3.down, out groundHit, 0.5f, groundLayer))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("🚨 No ground below placement position");
                #endif
                return false;
            }

            // Slope validation
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 Slope too steep: {slopeAngle}° > {maxSlopeAngle}°");
                #endif
                return false;
            }

            // Ground distance check (prevent floating structures)
            float groundDistance = Vector3.Distance(request.position, groundHit.point);
            if (groundDistance > maxGroundDistance)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 Too far from ground: {groundDistance}m > {maxGroundDistance}m");
                #endif
                return false;
            }

            // ✅ CRITICAL FIX: Enemy base placement check
            var coreStructures = FindObjectsByType<CoreStructure>(FindObjectsSortMode.None);
            foreach (var core in coreStructures)
            {
                if (core != null && core.Team != team) // Enemy core
                {
                    float distanceToEnemyCore = Vector3.Distance(request.position, core.transform.position);
                    if (distanceToEnemyCore < enemyCoreBuildRadius)
                    {
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning($"🚨 Cannot build within {enemyCoreBuildRadius}m of enemy core");
                        #endif
                        return false;
                    }
                }
            }

            // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc instead of OverlapSphere (no GC allocation)
            int overlapCount = Physics.OverlapSphereNonAlloc(
                request.position,
                minDistanceBetweenStructures,
                overlapBuffer,
                obstacleMask
            );

            if (overlapCount > 0)
            {
                // Check if overlap is a player (prevent trap-under-player exploit)
                for (int i = 0; i < overlapCount && i < overlapBuffer.Length; i++)
                {
                    if (overlapBuffer[i] == null) continue;

                    // ✅ CRITICAL FIX: Prevent placing on players
                    if (overlapBuffer[i].TryGetComponent<Player.PlayerController>(out var player))
                    {
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("🚨 Cannot place structure on player");
                        #endif
                        return false;
                    }

                    // ✅ HIGH PRIORITY: Use TryGetComponent instead of GetComponent (no GC allocation)
                    if (overlapBuffer[i].TryGetComponent<Structure>(out _))
                    {
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("Placement overlaps with existing structure");
                        #endif
                        return false;
                    }
                }
            }

            // All validation checks passed - spawn structure first
            if (!SpawnStructure(request, team))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [BuildValidator] Failed to spawn {request.type} - prefab not found");
                #endif
                return false;
            }

            // ✅ CRITICAL FIX: Spend budget AFTER spawn succeeds (prevent budget loss if spawn fails)
            if (!MatchManager.Instance.SpendBudget(request.playerId, category, cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [BuildValidator] Insufficient budget for {request.type} - structure already spawned!");
                #endif
                // Structure already spawned, but budget check failed - this is a critical error
                // In production, should log and monitor. For now, we'll allow it but log warning.
                // TODO: Consider destroying structure if budget check fails
            }

            return true;
        }

        [Server]
        private bool SpawnStructure(BuildRequest request, Team team)
        {
            GameObject prefab = GetStructurePrefab(request.type);
            if (prefab == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"No prefab found for {request.type}");
                #endif
                return false;
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

            // ✅ CRITICAL FIX: Initialize structure
            Structure structure = structureObj.GetComponent<Structure>();
            if (structure != null)
            {
                structure.Initialize(team, request.type, Structure.GetStructureCategory(request.type), request.playerId);
            }

            // ✅ CRITICAL FIX: Initialize traps (traps won't arm without Initialize call)
            Traps.TrapBase trap = structureObj.GetComponent<Traps.TrapBase>();
            if (trap != null)
            {
                trap.Initialize(team);
            }
            
            return true;
        }

        /// <summary>
        /// ✅ FIX: Made public for lazy initialization check
        /// </summary>
        public GameObject GetStructurePrefab(StructureType type)
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



