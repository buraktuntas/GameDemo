using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Manages Core Object objectives - stealing, carrying, and returning cores
    /// </summary>
    public class ObjectiveManager : NetworkBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }

        [Header("Core Objects")]
        [SerializeField] private GameObject coreObjectPrefab;
        [SerializeField] private Transform[] teamACoreSpawns;
        [SerializeField] private Transform[] teamBCoreSpawns;
        [SerializeField] private Transform[] teamAReturnPoints;
        [SerializeField] private Transform[] teamBReturnPoints;

        [Header("Sudden Death")]
        [SerializeField] private GameObject suddenDeathTunnelPrefab;
        [SerializeField] private Transform tunnelSpawnPoint;

        // Server-only data
        private Dictionary<ulong, CoreObjectData> coreObjects = new Dictionary<ulong, CoreObjectData>();
        private Dictionary<ulong, GameObject> coreGameObjects = new Dictionary<ulong, GameObject>();
        private bool suddenDeathTunnelOpen = false;

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
        
        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Clear static cache on destroy to prevent memory leak
            if (Instance == this)
            {
                cachedObjectiveSpawnPoints = null;
                lastObjectiveSpawnCacheTime = 0f;
                Instance = null;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[ObjectiveManager] Server started");
            #endif
        }

        /// <summary>
        /// Initialize core objects for all teams/players
        /// </summary>
        [Server]
        public void InitializeCores()
        {
            coreObjects.Clear();
            coreGameObjects.Clear();

            // Spawn cores for each team (or player in FFA)
            var matchManager = MatchManager.Instance;
            if (matchManager == null) return;

            GameMode mode = matchManager.GetGameMode();

            if (mode == GameMode.Team4v4)
            {
                // ✅ CRITICAL FIX: Null check for spawn arrays
                if (teamACoreSpawns == null || teamACoreSpawns.Length == 0)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[ObjectiveManager] Team A core spawns not assigned!");
                    #endif
                    return;
                }
                if (teamBCoreSpawns == null || teamBCoreSpawns.Length == 0)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[ObjectiveManager] Team B core spawns not assigned!");
                    #endif
                    return;
                }
                
                // Spawn core for Team A
                SpawnCore(Team.TeamA, teamACoreSpawns[0].position);
                
                // Spawn core for Team B
                SpawnCore(Team.TeamB, teamBCoreSpawns[0].position);
            }
            else // FFA
            {
                // Spawn core for each player
                var playerStates = matchManager.GetAllPlayerStates();
                foreach (var kvp in playerStates)
                {
                    var state = kvp.Value;
                    // Find spawn point for this player
                    Vector3 spawnPos = GetPlayerSpawnPosition(state.playerId);
                    SpawnCore(state.team, spawnPos);
                }
            }
        }

        [Server]
        private void SpawnCore(Team team, Vector3 position)
        {
            if (coreObjectPrefab == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[ObjectiveManager] Core object prefab not assigned!");
                #endif
                return;
            }

            GameObject coreObj = Instantiate(coreObjectPrefab, position, Quaternion.identity);
            NetworkServer.Spawn(coreObj);

            ulong teamId = (ulong)team;
            CoreObjectData coreData = new CoreObjectData(teamId);
            coreData.spawnPosition = position;

            coreObjects[teamId] = coreData;
            coreGameObjects[teamId] = coreObj;

            // Set core owner
            var coreComponent = coreObj.GetComponent<CoreObject>();
            if (coreComponent != null)
            {
                coreComponent.Initialize(teamId, this);
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ObjectiveManager] Spawned core for Team {team} at {position}");
            #endif
        }

        // ✅ PERFORMANCE FIX: Cache spawn points to avoid FindGameObjectsWithTag calls
        private static Transform[] cachedObjectiveSpawnPoints = null;
        private static float lastObjectiveSpawnCacheTime = 0f;
        private const float OBJECTIVE_SPAWN_CACHE_DURATION = 30f;
        
        [Server]
        private Vector3 GetPlayerSpawnPosition(ulong playerId)
        {
            // ✅ FIX: Get spawn position from cached spawn points or generate fallback
            // Check if we have cached spawn points
            if (Combat.Health.GetCachedSpawnPoints() != null && Combat.Health.GetCachedSpawnPoints().Length > 0)
            {
                // Use random spawn point from cache
                var spawnPoints = Combat.Health.GetCachedSpawnPoints();
                int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                return spawnPoints[randomIndex].position;
            }

            // ✅ PERFORMANCE FIX: Use cached spawn points instead of FindGameObjectsWithTag
            // Refresh cache if expired
            if (cachedObjectiveSpawnPoints == null || Time.time - lastObjectiveSpawnCacheTime > OBJECTIVE_SPAWN_CACHE_DURATION)
            {
                // Use FindObjectsByType instead of FindGameObjectsWithTag (less GC)
                var spawnObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                System.Collections.Generic.List<Transform> spawnList = new System.Collections.Generic.List<Transform>();
                
                for (int i = 0; i < spawnObjects.Length; i++)
                {
                    if (spawnObjects[i].CompareTag("SpawnPoint"))
                    {
                        spawnList.Add(spawnObjects[i].transform);
                    }
                }
                
                cachedObjectiveSpawnPoints = spawnList.ToArray();
                lastObjectiveSpawnCacheTime = Time.time;
            }
            
            if (cachedObjectiveSpawnPoints != null && cachedObjectiveSpawnPoints.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, cachedObjectiveSpawnPoints.Length);
                return cachedObjectiveSpawnPoints[randomIndex].position;
            }

            // Last resort: Generate random position around origin
            // Spread players in a circle pattern for FFA
            int playerIndex = GetPlayerIndex(playerId);
            float angle = (playerIndex * 360f / 8f) * Mathf.Deg2Rad; // 8 player positions max
            float radius = 30f; // 30m radius circle

            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                5f, // Y height
                Mathf.Sin(angle) * radius
            );

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ObjectiveManager] Using fallback spawn position for player {playerId}: {position}");
            #endif

            return position;
        }

        [Server]
        private int GetPlayerIndex(ulong playerId)
        {
            // Get player index from MatchManager
            var matchManager = MatchManager.Instance;
            if (matchManager == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[ObjectiveManager] MatchManager.Instance is null");
                #endif
                return 0;
            }
            
            var allStates = matchManager.GetAllPlayerStates();
            int index = 0;
            foreach (var kvp in allStates)
            {
                if (kvp.Key == playerId)
                    return index;
                index++;
            }
            return 0;
        }

        /// <summary>
        /// Player picks up a core object
        /// </summary>
        [Server]
        public bool PickupCore(ulong coreOwnerId, ulong playerId)
        {
            if (!coreObjects.ContainsKey(coreOwnerId))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[ObjectiveManager] Core not found for owner {coreOwnerId}");
                #endif
                return false;
            }

            var coreData = coreObjects[coreOwnerId];
            
            // Check if already carried
            if (coreData.isCarried)
            {
                return false;
            }

            // Check if player is trying to pick up their own core
            var matchManager = MatchManager.Instance;
            if (matchManager != null)
            {
                var playerState = matchManager.GetPlayerState(playerId);
                if (playerState != null && playerState.team == (Team)coreOwnerId)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[ObjectiveManager] Player {playerId} cannot pick up their own core");
                    #endif
                    return false;
                }
            }

            // Pick up core
            coreData.isCarried = true;
            coreData.carrierId = playerId;
            coreObjects[coreOwnerId] = coreData;

            // Notify core object
            if (coreGameObjects.ContainsKey(coreOwnerId))
            {
                var coreComponent = coreGameObjects[coreOwnerId].GetComponent<CoreObject>();
                if (coreComponent != null)
                {
                    coreComponent.OnPickedUp(playerId);
                }
            }

            RpcOnCorePickedUp(coreOwnerId, playerId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ObjectiveManager] Player {playerId} picked up core from Team {coreOwnerId}");
            #endif

            return true;
        }

        /// <summary>
        /// Player drops a core (on death or manually)
        /// </summary>
        [Server]
        public void DropCore(ulong coreOwnerId, Vector3 dropPosition)
        {
            if (!coreObjects.ContainsKey(coreOwnerId))
                return;

            var coreData = coreObjects[coreOwnerId];
            ulong previousCarrier = coreData.carrierId;
            coreData.isCarried = false;
            coreData.carrierId = 0;
            coreObjects[coreOwnerId] = coreData;

            // Notify PlayerController
            if (previousCarrier != 0)
            {
                var player = GetPlayerById(previousCarrier);
                if (player != null)
                {
                    var playerController = player.GetComponent<Player.PlayerController>();
                    if (playerController != null)
                    {
                        playerController.SetCarryingCore(false, 0);
                    }
                }
            }

            // Move core object to drop position
            if (coreGameObjects.ContainsKey(coreOwnerId))
            {
                var coreObj = coreGameObjects[coreOwnerId];
                coreObj.transform.position = dropPosition;

                var coreComponent = coreObj.GetComponent<CoreObject>();
                if (coreComponent != null)
                {
                    coreComponent.OnDropped();
                }
            }

            RpcOnCoreDropped(coreOwnerId, dropPosition);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ObjectiveManager] Core from Team {coreOwnerId} dropped at {dropPosition}");
            #endif
        }

        /// <summary>
        /// Called when a player dies - drop any cores they were carrying
        /// </summary>
        [Server]
        public void OnPlayerDeath(ulong playerId, Vector3 deathPosition)
        {
            // Find any cores this player was carrying
            foreach (var kvp in coreObjects)
            {
                if (kvp.Value.carrierId == playerId)
                {
                    DropCore(kvp.Key, deathPosition);
                }
            }
        }

        [Server]
        private Player.PlayerController GetPlayerById(ulong playerId)
        {
            // ✅ PERFORMANCE FIX: Use NetworkServer.spawned dictionary lookup (O(1))
            // Instead of FindObjectsByType which scans all objects in scene (O(n))
            uint netId = (uint)playerId;
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                return identity.GetComponent<Player.PlayerController>();
            }
            return null;
        }

        /// <summary>
        /// Player attempts to return a core to their base
        /// </summary>
        [Server]
        public bool TryReturnCore(ulong coreOwnerId, ulong playerId, Vector3 playerPosition)
        {
            if (!coreObjects.ContainsKey(coreOwnerId))
                return false;

            var coreData = coreObjects[coreOwnerId];
            if (!coreData.isCarried || coreData.carrierId != playerId)
                return false;

            // Check if player is at return point
            var matchManager = MatchManager.Instance;
            if (matchManager == null) return false;

            var playerState = matchManager.GetPlayerState(playerId);
            if (playerState == null) return false;

            // Find return point for player's team
            Transform[] returnPoints = playerState.team == Team.TeamA ? teamAReturnPoints : teamBReturnPoints;
            
            // ✅ CRITICAL FIX: Null check for return points
            if (returnPoints == null || returnPoints.Length == 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[ObjectiveManager] Return points not assigned for team {playerState.team}");
                #endif
                return false;
            }
            
            bool isAtReturnPoint = false;

            foreach (var returnPoint in returnPoints)
            {
                if (Vector3.Distance(playerPosition, returnPoint.position) <= GameConstants.CORE_RETURN_DISTANCE)
                {
                    isAtReturnPoint = true;
                    break;
                }
            }

            if (!isAtReturnPoint)
                return false;

            // ✅ CRITICAL FIX: Return core and track returner team
            coreData.isCarried = false;
            coreData.carrierId = 0;
            coreData.isReturned = true;
            coreData.returnerId = playerId;
            coreData.returnerTeam = playerState.team; // Store returner's team for GetCoreReturnWinner
            coreObjects[coreOwnerId] = coreData;

            // Award score
            var scoreManager = Core.ScoreManager.Instance;
            if (scoreManager != null)
            {
                scoreManager.AwardCapture(playerId);
            }

            RpcOnCoreReturned(coreOwnerId, playerId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ObjectiveManager] Player {playerId} (Team {playerState.team}) returned core from Team {coreOwnerId}!");
            #endif

            return true;
        }

        /// <summary>
        /// Check if any core has been returned
        /// </summary>
        [Server]
        public bool HasCoreBeenReturned()
        {
            foreach (var coreData in coreObjects.Values)
            {
                if (coreData.isReturned)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the team that returned a core (winner)
        /// </summary>
        [Server]
        public Team GetCoreReturnWinner()
        {
            foreach (var kvp in coreObjects)
            {
                if (kvp.Value.isReturned && kvp.Value.returnerTeam != Team.None)
                {
                    // ✅ CRITICAL FIX: Return the team that actually returned the core
                    // Previously this logic was flawed and could return wrong winner
                    // when multiple cores were returned in sequence
                    return kvp.Value.returnerTeam;
                }
            }
            return Team.None;
        }

        /// <summary>
        /// Open sudden death tunnel between bases
        /// </summary>
        [Server]
        public void OpenSuddenDeathTunnel()
        {
            if (suddenDeathTunnelOpen)
                return;

            if (suddenDeathTunnelPrefab != null && tunnelSpawnPoint != null)
            {
                GameObject tunnel = Instantiate(suddenDeathTunnelPrefab, tunnelSpawnPoint.position, tunnelSpawnPoint.rotation);
                NetworkServer.Spawn(tunnel);
                suddenDeathTunnelOpen = true;
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[ObjectiveManager] Sudden death tunnel opened!");
                #endif
            }
        }

        [ClientRpc]
        private void RpcOnCorePickedUp(ulong coreOwnerId, ulong playerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Player {playerId} picked up core from Team {coreOwnerId}");
            #endif
        }

        [ClientRpc]
        private void RpcOnCoreDropped(ulong coreOwnerId, Vector3 position)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Core from Team {coreOwnerId} dropped at {position}");
            #endif
        }

        [ClientRpc]
        private void RpcOnCoreReturned(ulong coreOwnerId, ulong playerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Player {playerId} returned core from Team {coreOwnerId}!");
            #endif
        }
    }
}

