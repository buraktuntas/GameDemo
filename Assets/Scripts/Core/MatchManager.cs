using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TacticalCombat.UI;
using TacticalCombat.Building;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Core
{
    public class MatchManager : NetworkBehaviour
    {
        private static readonly HashSet<GameObject> s_registeredPrefabs = new HashSet<GameObject>();
        public static MatchManager Instance { get; private set; }

        [Header("Match State")]
        [SyncVar(hook = nameof(OnPhaseChanged))]
        private Phase currentPhase = Phase.Lobby;
        
        [SyncVar]
        private float remainingTime;
        
        private MatchState matchState = new MatchState();

        [Header("Configuration")]
        [SerializeField] private float buildDuration = GameConstants.BUILD_DURATION;
        [SerializeField] private float combatDuration = GameConstants.COMBAT_DURATION;
        [SerializeField] private float suddenDeathDuration = GameConstants.SUDDEN_DEATH_DURATION;
        [SerializeField] private float endPhaseDuration = GameConstants.END_PHASE_DURATION;
        [SerializeField] private GameMode gameMode = GameMode.Team4v4;

        [Header("Team Tracking")]
        // ⚠️ NOTE: Dictionary doesn't sync to clients automatically
        // Client-side UI should query via [ClientRpc] methods or use GetPlayerState via Commands
        private Dictionary<ulong, PlayerState> playerStates = new Dictionary<ulong, PlayerState>();

        // ✅ FIX: Track player count for client-side UI (synced)
        [SyncVar] private int teamAPlayerCount = 0;
        [SyncVar] private int teamBPlayerCount = 0;
        
        // ✅ REMOVED: Clan system and round wins (clan system removed, rounds removed)
        
        [SyncVar]
        private bool suddenDeathActive = false;

        // Events
        public System.Action<Phase> OnPhaseChangedEvent;
        public System.Action<Team> OnMatchWonEvent;
        public System.Action OnSuddenDeathActivated;
        
        // Reference to ObjectiveManager (will be set when ObjectiveManager is created)
        private ObjectiveManager objectiveManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Prewarm networked pools from catalog if available
            try
            {
                var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                if (pool == null)
                {
                    var go = new GameObject("[NetworkObjectPool]");
                    pool = go.AddComponent<TacticalCombat.Core.NetworkObjectPool>();
                }

                var catalog = Resources.Load<TacticalCombat.Core.PoolCatalog>("PoolCatalog");
                if (catalog != null)
                {
                    foreach (var e in catalog.entries)
                    {
                        if (e.prefab != null && e.serverPrewarmCount > 0)
                        {
                            pool.Prewarm(e.prefab, e.serverPrewarmCount);
                        }
                    }
                }
            }
            catch { }
            
            // ✅ CRITICAL FIX: Auto-create BuildValidator if not found in scene
            EnsureBuildValidator();
            
            InitializeMatch();
        }

        [Server]
        private void EnsureBuildValidator()
        {
            // Check if BuildValidator already exists
            if (TacticalCombat.Building.BuildValidator.Instance != null)
            {
                LogInfo("[MatchManager] BuildValidator already exists in scene");
                return;
            }

            // Try to find existing BuildValidator in scene
            var existing = FindFirstObjectByType<TacticalCombat.Building.BuildValidator>();
            if (existing != null)
            {
                LogInfo("[MatchManager] Found existing BuildValidator in scene");
                return;
            }

            // Create BuildValidator GameObject
            GameObject validatorObj = new GameObject("[BuildValidator]");
            validatorObj.transform.SetParent(transform); // Parent to MatchManager for organization
            
            // Add NetworkIdentity (required for NetworkBehaviour)
            NetworkIdentity identity = validatorObj.AddComponent<NetworkIdentity>();
            identity.serverOnly = true; // BuildValidator only needs to exist on server
            
            // Add BuildValidator component
            var validator = validatorObj.AddComponent<TacticalCombat.Building.BuildValidator>();
            
            // ✅ FIX: Copy prefab references from SimpleBuildMode if available
            var buildMode = FindFirstObjectByType<TacticalCombat.Building.SimpleBuildMode>();
            if (buildMode != null)
            {
                // Use reflection to copy prefab references (since they're private SerializeField)
                var validatorType = typeof(TacticalCombat.Building.BuildValidator);
                var buildModeType = typeof(TacticalCombat.Building.SimpleBuildMode);
                
                // Map SimpleBuildMode prefabs to BuildValidator prefabs
                var wallPrefabField = validatorType.GetField("wallPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var platformPrefabField = validatorType.GetField("platformPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rampPrefabField = validatorType.GetField("rampPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                var simpleWallPrefab = buildModeType.GetField("wallPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var simpleFloorPrefab = buildModeType.GetField("floorPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var simpleStairsPrefab = buildModeType.GetField("stairsPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (wallPrefabField != null && simpleWallPrefab != null)
                {
                    wallPrefabField.SetValue(validator, simpleWallPrefab.GetValue(buildMode));
                }
                
                // Platform = Floor in SimpleBuildMode
                if (platformPrefabField != null && simpleFloorPrefab != null)
                {
                    platformPrefabField.SetValue(validator, simpleFloorPrefab.GetValue(buildMode));
                }
                
                // Ramp = Stairs in SimpleBuildMode
                if (rampPrefabField != null && simpleStairsPrefab != null)
                {
                    rampPrefabField.SetValue(validator, simpleStairsPrefab.GetValue(buildMode));
                }
                
                LogInfo("[MatchManager] BuildValidator prefabs copied from SimpleBuildMode");
            }
            else
            {
                LogWarning("SimpleBuildMode not found - BuildValidator prefabs will be empty");
            }
            
            // Spawn on network (required for NetworkBehaviour)
            NetworkServer.Spawn(validatorObj);
            
            LogInfo("[MatchManager] BuildValidator auto-created and spawned on server");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            try
            {
                var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                if (pool == null)
                {
                    var go = new GameObject("[NetworkObjectPool]");
                    pool = go.AddComponent<TacticalCombat.Core.NetworkObjectPool>();
                }

                var catalog = Resources.Load<TacticalCombat.Core.PoolCatalog>("PoolCatalog");
                if (catalog != null)
                {
                    foreach (var e in catalog.entries)
                    {
                        if (e.prefab == null) continue;

                        // Client prewarm
                        if (e.clientPrewarmCount > 0)
                        {
                            pool.Prewarm(e.prefab, e.clientPrewarmCount);
                        }

                        // Register client spawn/unspawn to use pool
                        var p = e.prefab; // capture local
                        if (!s_registeredPrefabs.Contains(p))
                        {
                            NetworkClient.RegisterPrefab(
                                p,
                                (Mirror.SpawnMessage msg) =>
                                {
                                    return pool.Get(p, msg.position, msg.rotation);
                                },
                                (GameObject spawned) =>
                                {
                                    pool.Release(spawned);
                                }
                            );
                            s_registeredPrefabs.Add(p);
                        }
                    }
                }
            }
            catch { }
        }

        [Server]
        private void InitializeMatch()
        {
            matchState = new MatchState();
            matchState.gameMode = gameMode;
            currentPhase = Phase.Lobby; // ✅ CRITICAL: Start in Lobby phase, NOT Build phase
            // ✅ REMOVED: teamAWins, teamBWins (round system removed)
            suddenDeathActive = false;
            playerStates.Clear();
            
            // Initialize match stats for all players
            matchState.playerStats.Clear();
            foreach (var kvp in playerStates)
            {
                matchState.playerStats[kvp.Key] = new MatchStats(kvp.Key);
            }
            
            // ✅ REMOVED: Clan system reset (clan system removed)
            
            // ✅ CORE STABILITY: Find ObjectiveManager (null-safe)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
            if (objectiveManager == null)
            {
                LogWarning("[MatchManager] ObjectiveManager not found in scene - core objectives may not work");
            }
            
            // ✅ CRITICAL FIX: Do NOT auto-start match - wait for lobby system to start it
            LogInfo("[MatchManager] Initialized in Lobby phase - waiting for lobby to start match");
        }

        /// <summary>
        /// ✅ REMOVED CLAN SYSTEM: Register player (clan support removed)
        /// </summary>
        [Server]
        public void RegisterPlayer(ulong playerId, Team team, RoleId role)
        {
            // ✅ REMOVED: Clan system - simple team assignment
            // Honor the team parameter from UI selection
            if (team == Team.None)
            {
                // Team was not selected (Auto-balance) - assign automatically
                team = AssignTeamAutoBalance();
            }
            
            LogInfo($"Player {playerId} registered with team: {team}, Role {role}");

            // Register or update player state
            if (!playerStates.ContainsKey(playerId))
            {
                playerStates[playerId] = new PlayerState(playerId, team, role);
            }
            else
            {
                // ✅ FIX: Update player count when team changes
                Team oldTeam = playerStates[playerId].team;
                if (oldTeam != team)
                {
                    // Remove from old team count
                    if (oldTeam == Team.TeamA) teamAPlayerCount--;
                    else if (oldTeam == Team.TeamB) teamBPlayerCount--;
                }

                // Update existing player (re-registration with new team/role)
                playerStates[playerId].team = team;
                playerStates[playerId].role = role;
                LogInfo($"Player {playerId} RE-registered: Team {team}, Role {role}");
            }

            // ✅ FIX: Update synced player counts
            UpdatePlayerCounts();

            // Update player's team visually
            UpdatePlayerTeam(playerId, team);
        }

        /// <summary>
        /// ✅ FIX: Update synced player counts for client-side UI
        /// </summary>
        [Server]
        private void UpdatePlayerCounts()
        {
            int countA = 0;
            int countB = 0;

            foreach (var state in playerStates.Values)
            {
                if (state.team == Team.TeamA) countA++;
                else if (state.team == Team.TeamB) countB++;
            }

            teamAPlayerCount = countA;
            teamBPlayerCount = countB;
        }

        [Server]
        private Team AssignTeamAutoBalance()
        {
            int teamACount = 0;
            int teamBCount = 0;

            foreach (var state in playerStates.Values)
            {
                if (state.team == Team.TeamA) teamACount++;
                else if (state.team == Team.TeamB) teamBCount++;
            }

            // Assign to team with fewer players (or TeamA if equal)
            return teamACount <= teamBCount ? Team.TeamA : Team.TeamB;
        }

        [Server]
        private void UpdatePlayerTeam(ulong playerId, Team team)
        {
            // Find player GameObject and update their team
            foreach (var playerObj in FindObjectsByType<TacticalCombat.Player.PlayerController>(FindObjectsSortMode.None))
            {
                if (playerObj.netId == playerId)
                {
                    playerObj.team = team;
                    LogInfo($"Updated Player {playerId} visual team to {team}");
                    break;
                }
            }
        }

        [Server]
        public void StartMatch()
        {
            // ✅ CRITICAL: Double-check phase before starting
            if (currentPhase != Phase.Lobby)
            {
                LogWarning($"Cannot start match - current phase is {currentPhase}, not Lobby! Match may have already started.");
                return;
            }

            // ✅ CRITICAL FIX: If LobbyManager exists, ONLY allow it to start the match
            var lobbyManager = TacticalCombat.Network.LobbyManager.Instance;
            if (lobbyManager != null)
            {
                LogInfo("[MatchManager] Lobby system is active - match starting from lobby");
            }
            else
            {
                LogWarning("StartMatch() called but LobbyManager.Instance is NULL! This might be a legacy auto-start call.");
            }

            // ✅ TEST FIX: Allow 1 player for testing (bypass minimum check)
            // Check minimum players (but allow 1 player for testing)
            if (playerStates.Count < GameConstants.MIN_PLAYERS_TO_START && playerStates.Count > 1)
            {
                LogWarning($"Cannot start match - need at least {GameConstants.MIN_PLAYERS_TO_START} players (current: {playerStates.Count})");
                return;
            }
            
            // ✅ TEST FIX: Log test mode
            if (playerStates.Count == 1)
            {
                LogInfo("[MatchManager] TEST MODE: Starting with 1 player (testing)");
            }

            LogInfo($"[MatchManager] Starting match - Mode: {gameMode}, Players: {playerStates.Count}");
            StartBuildPhase();
        }

        [Server]
        private void StartBuildPhase()
        {
            LogInfo("Starting Build Phase (3 minutes)");
            
            // Reset player states
            foreach (var state in playerStates.Values)
            {
                state.isAlive = true;
                state.budget = BuildBudget.GetRoleBudget(state.role);
            }

            // Initialize match stats
            foreach (var kvp in playerStates)
            {
                if (!matchState.playerStats.ContainsKey(kvp.Key))
                {
                    matchState.playerStats[kvp.Key] = new MatchStats(kvp.Key);
                }
            }

            currentPhase = Phase.Build;
            remainingTime = buildDuration;
            suddenDeathActive = false;
            
            // ✅ CRITICAL: Notify phase change FIRST (before showing players)
            RpcOnPhaseChanged(currentPhase);
            
            // ✅ CRITICAL: Show all players (they were hidden in Lobby phase)
            RpcShowAllPlayers();
            
            // ✅ NEW: Sync initial stats to all clients when build phase starts
            RpcSyncAllStats();
            
            // ✅ NEW: Notify BuildManager that build phase started
            if (Building.BuildManager.Instance != null)
            {
                Building.BuildManager.Instance.BeginBuildPhase();
            }
            
            // ✅ CRITICAL: Enable player controls for Build phase
            RpcEnablePlayerControls(true);
            
            LogInfo("Build phase started successfully");
            
            // ✅ NETWORK OPTIMIZATION: Start periodic stats sync coroutine
            StartCoroutine(PeriodicStatsSync());
            
            StartCoroutine(BuildPhaseTimer());
        }

        [Server]
        private IEnumerator BuildPhaseTimer()
        {
            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            TransitionToCombat();
        }

        /// <summary>
        /// ✅ NEW: Show all players when match starts (they were hidden in Lobby phase)
        /// </summary>
        [ClientRpc]
        private void RpcShowAllPlayers()
        {
            // Find all player GameObjects and show them
            var players = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var playerController in players)
            {
                var player = playerController.gameObject;
                
                // Show renderers
                var renderers = player.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
                
                // Enable colliders
                var colliders = player.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                }
            }
            
            LogInfo("[MatchManager] All players shown (match started)");
        }

        [Server]
        private void TransitionToCombat()
        {
            LogInfo("Transitioning to Combat Phase (15 minutes)");
            
            currentPhase = Phase.Combat;
            remainingTime = combatDuration;
            suddenDeathActive = false;
            
            // ✅ CRITICAL: Notify phase change FIRST
            RpcOnPhaseChanged(currentPhase);
            
            // ✅ NEW: Notify BuildManager that build phase ended (BEFORE enabling controls)
            if (Building.BuildManager.Instance != null)
            {
                Building.BuildManager.Instance.EndBuildPhase();
            }
            
            // ✅ CRITICAL: Enable player controls for Combat phase (PvP active)
            RpcEnablePlayerControls(true);
            
            // Initialize core objects if ObjectiveManager exists
            if (objectiveManager != null)
            {
                objectiveManager.InitializeCores();
            }
            
            LogInfo("Combat phase started successfully");
            
            StartCoroutine(CombatPhaseTimer());
        }

        [Server]
        private IEnumerator CombatPhaseTimer()
        {
            const float winCheckInterval = 0.5f;
            float nextCheck = 0f;

            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;

                // Check for sudden death transition (final 2 minutes)
                if (!suddenDeathActive && remainingTime <= suddenDeathDuration)
                {
                    TransitionToSuddenDeath();
                    yield break; // Exit combat phase, sudden death phase will start
                }

                // Check win condition every 0.5s
                if (Time.time >= nextCheck)
                {
                    if (IsWinConditionMet())
                    {
                        Team winner = DetermineWinnerByScore();
                        EndMatch(winner);
                        yield break;
                    }
                    nextCheck = Time.time + winCheckInterval;
                }

                yield return null;
            }

            // Combat phase ended, transition to Sudden Death
            TransitionToSuddenDeath();
        }
        
        [Server]
        private void TransitionToSuddenDeath()
        {
            LogInfo("Transitioning to Sudden Death Phase (2 minutes)");
            currentPhase = Phase.SuddenDeath;
            remainingTime = suddenDeathDuration;
            suddenDeathActive = true;
            RpcOnPhaseChanged(currentPhase);
            RpcOnSuddenDeathActivated();
            
            // Open sudden death tunnel
            if (objectiveManager != null)
            {
                objectiveManager.OpenSuddenDeathTunnel();
            }
            
            StartCoroutine(SuddenDeathPhaseTimer());
        }
        
        [Server]
        private IEnumerator SuddenDeathPhaseTimer()
        {
            const float winCheckInterval = 0.5f;
            float nextCheck = 0f;

            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;

                // Check win condition every 0.5s
                if (Time.time >= nextCheck)
                {
                    if (IsWinConditionMet())
                    {
                        Team winnerTeam = DetermineWinnerByScore();
                        EndMatch(winnerTeam);
                        yield break;
                    }
                    nextCheck = Time.time + winCheckInterval;
                }

                yield return null;
            }

            // Sudden death ended, determine winner by score
            Team finalWinner = DetermineWinnerByScore();
            EndMatch(finalWinner);
        }

        [Server]
        private Team DetermineWinnerByScore()
        {
            // If core returned, use that winner
            if (objectiveManager != null && objectiveManager.HasCoreBeenReturned())
            {
                return objectiveManager.GetCoreReturnWinner();
            }

            // Otherwise, check by team elimination or highest score
            if (gameMode == GameMode.Team4v4)
            {
                int teamAAlive = 0;
                int teamBAlive = 0;

                foreach (var state in playerStates.Values)
                {
                    if (state.isAlive)
                    {
                        if (state.team == Team.TeamA) teamAAlive++;
                        else if (state.team == Team.TeamB) teamBAlive++;
                    }
                }

                if (teamAAlive > 0 && teamBAlive == 0) return Team.TeamA;
                if (teamBAlive > 0 && teamAAlive == 0) return Team.TeamB;

                // If both teams alive, check by score
                int teamAScore = 0;
                int teamBScore = 0;

                foreach (var kvp in matchState.playerStats)
                {
                    var stats = kvp.Value;
                    var playerState = GetPlayerState(kvp.Key);
                    if (playerState != null)
                    {
                        if (playerState.team == Team.TeamA)
                            teamAScore += stats.totalScore;
                        else if (playerState.team == Team.TeamB)
                            teamBScore += stats.totalScore;
                    }
                }

                if (teamAScore > teamBScore) return Team.TeamA;
                if (teamBScore > teamAScore) return Team.TeamB;
            }
            else // FFA
            {
                // Find player with highest score
                ulong winnerId = 0;
                int maxScore = int.MinValue;

                foreach (var kvp in matchState.playerStats)
                {
                    if (kvp.Value.totalScore > maxScore)
                    {
                        maxScore = kvp.Value.totalScore;
                        winnerId = kvp.Key;
                    }
                }

                if (winnerId != 0)
                {
                    var playerState = GetPlayerState(winnerId);
                    if (playerState != null)
                        return playerState.team;
                }
            }

            return Team.None; // Draw
        }

        // ✅ REMOVED: ActivateSuddenDeath() - Now handled by TransitionToSuddenDeath()

        [Server]
        private bool IsWinConditionMet()
        {
            // Check if core was returned (primary win condition)
            if (objectiveManager != null && objectiveManager.HasCoreBeenReturned())
            {
                return true;
            }

            // Check if all players of one team are dead (secondary win condition)
            if (gameMode == GameMode.Team4v4)
            {
                int teamAAlive = 0;
                int teamBAlive = 0;

                foreach (var kvp in playerStates)
                {
                    var state = kvp.Value;
                    if (state.isAlive)
                    {
                        if (state.team == Team.TeamA) teamAAlive++;
                        else if (state.team == Team.TeamB) teamBAlive++;
                    }
                }

                return teamAAlive == 0 || teamBAlive == 0;
            }
            else // FFA mode
            {
                int aliveCount = 0;
                foreach (var state in playerStates.Values)
                {
                    if (state.isAlive) aliveCount++;
                }
                return aliveCount <= 1; // Last player standing wins
            }
        }

        [Server]
        public void NotifyPlayerDeath(ulong playerId)
        {
            if (playerStates.ContainsKey(playerId))
            {
                playerStates[playerId].isAlive = false;
                LogInfo($"Player {playerId} died. Current phase: {currentPhase}");

                // Check win condition in any phase (not just Combat)
                CheckWinCondition();
            }
        }

        [Server]
        public void OnCoreDestroyed(Team winner)
        {
            LogInfo($"Core destroyed! Winner: {winner}");
            EndMatch(winner);
        }

        [Server]
        private void CheckWinCondition()
        {
            if (!IsWinConditionMet())
            {
                return;
            }

            int teamAAlive = 0;
            int teamBAlive = 0;

            foreach (var state in playerStates.Values)
            {
                if (state.isAlive)
                {
                    if (state.team == Team.TeamA) teamAAlive++;
                    else if (state.team == Team.TeamB) teamBAlive++;
                }
            }

            Team winner = Team.None;
            if (teamAAlive > 0 && teamBAlive == 0)
                winner = Team.TeamA;
            else if (teamBAlive > 0 && teamAAlive == 0)
                winner = Team.TeamB;

            if (winner != Team.None)
            {
                LogInfo($"Winner: {winner} (TeamA: {teamAAlive}, TeamB: {teamBAlive})");
                EndMatch(winner);
            }
        }

        [Server]
        private void EndMatch(Team winner)
        {
            // ✅ FIX: EndMatch now takes winner as parameter (already determined by caller)
            // Calculate final scores
            foreach (var stats in matchState.playerStats.Values)
            {
                stats.CalculateTotalScore();
            }

            // Calculate awards
            var scoreManager = ScoreManager.Instance;
            Dictionary<ulong, AwardType> awardsDict = null;
            AwardData[] awardsArray = null;
            if (scoreManager != null)
            {
                awardsDict = scoreManager.CalculateAwards();
                
                // ✅ FIX: Convert Dictionary to Array for Mirror RPC
                if (awardsDict != null && awardsDict.Count > 0)
                {
                    awardsArray = new AwardData[awardsDict.Count];
                    int index = 0;
                    foreach (var kvp in awardsDict)
                    {
                        awardsArray[index] = new AwardData(kvp.Key, kvp.Value);
                        index++;
                    }
                }
            }

            EndMatch(winner, awardsArray);
        }

        [Server]
        private void EndMatch(Team winner, AwardData[] awards = null)
        {
            LogInfo($"Match ended! Winner: {winner}");
            
            currentPhase = Phase.End;
            remainingTime = endPhaseDuration;
            RpcOnPhaseChanged(currentPhase);
            RpcOnMatchWon(winner, awards);
            
            // ✅ NEW: Update ranking system
            UpdateRankings(winner);
            
            // ✅ REMOVED: Clan system XP award (clan system removed)
            
            // Show end screen with scoreboard and awards
            // Match stays in End phase until restart
        }

        // ✅ REMOVED: EndPhaseSequence, AwardClanXP, CalculateTeamXP (clan system and round system removed)
        
        /// <summary>
        /// ✅ NEW: Restart match (host only)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdRestartMatch(NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            // Only host can restart (connectionId 0 is host)
            if (sender != null && sender.connectionId != 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                LogWarning("[MatchManager] Only host can restart match");
                #endif
                return;
            }
            
            RestartMatch();
        }
        
        [Server]
        private void RestartMatch()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogInfo("[MatchManager] Restarting match...");
            #endif
            
            // Reset match state
            currentPhase = Phase.Lobby;
            remainingTime = 0f;
            suddenDeathActive = false;
            
            // Clear player stats (recreate MatchStats objects)
            if (matchState != null)
            {
                matchState.playerStats.Clear();
                foreach (var kvp in playerStates)
                {
                    matchState.playerStats[kvp.Key] = new MatchStats(kvp.Key);
                }
            }
            
            // Reset objective manager
            if (objectiveManager != null)
            {
                objectiveManager.InitializeCores();
            }
            
            // Reset build manager (clear placed structures)
            if (BuildManager.Instance != null)
            {
                // BuildManager will reset when new build phase starts
            }
            
            // Notify clients
            RpcOnPhaseChanged(currentPhase);
            RpcOnMatchRestarted();
            
            // Start new match after short delay
            StartCoroutine(RestartMatchSequence());
        }
        
        [Server]
        private System.Collections.IEnumerator RestartMatchSequence()
        {
            yield return new WaitForSeconds(2f);
            
            // Start new match
            StartMatch();
        }
        
        [ClientRpc]
        private void RpcOnMatchRestarted()
        {
            LogInfo("[Client] Match restarted!");
            
            // Hide end game scoreboard
            var endScoreboard = FindFirstObjectByType<UI.EndGameScoreboard>();
            if (endScoreboard != null)
            {
                endScoreboard.HideScoreboard();
            }
        }

        // Client-side phase change handler
        // ✅ CRITICAL FIX: SyncVar hook - only invoke event, don't duplicate RPC logic
        private void OnPhaseChanged(Phase oldPhase, Phase newPhase)
        {
            LogInfo($"Phase changed: {oldPhase} -> {newPhase}");
            // ✅ FIX: SyncVar hook already fires on all clients, RPC is redundant
            // But we keep RPC for explicit synchronization in case SyncVar is delayed
            // Only invoke event once to prevent double-fire
            OnPhaseChangedEvent?.Invoke(newPhase);
            
            // ✅ NEW: Update audio based on phase
            UpdatePhaseAudio(newPhase);
        }
        
        /// <summary>
        /// ✅ NEW: Update audio/music based on game phase
        /// </summary>
        private void UpdatePhaseAudio(Phase phase)
        {
            if (Audio.AudioManager.Instance == null) return;
            
            switch (phase)
            {
                case Phase.Build:
                    Audio.AudioManager.Instance.PlayBuildModeMusic();
                    break;
                case Phase.Combat:
                case Phase.SuddenDeath:
                    Audio.AudioManager.Instance.PlayCombatMusic();
                    break;
                case Phase.Lobby:
                case Phase.End:
                    Audio.AudioManager.Instance.PlayBackgroundMusic();
                    break;
            }
        }

        [ClientRpc]
        private void RpcOnPhaseChanged(Phase newPhase)
        {
            // ✅ CRITICAL FIX: Only invoke if phase actually changed (prevent double-fire)
            // SyncVar hook may have already fired, check if event was already invoked
            if (currentPhase != newPhase)
            {
                // Phase mismatch - RPC is authoritative, update SyncVar value
                currentPhase = newPhase;
            }
            // Don't invoke event again - SyncVar hook already did it
        }

        [ClientRpc]
        private void RpcOnSuddenDeathActivated()
        {
            OnSuddenDeathActivated?.Invoke();
            LogInfo("SUDDEN DEATH ACTIVATED!");
        }

        /// <summary>
        /// ✅ NEW: Enable/disable player controls based on phase
        /// </summary>
        [ClientRpc]
        private void RpcEnablePlayerControls(bool enable)
        {
            // This RPC is called to notify clients about phase changes
            // PlayerController.CheckAndUpdatePlayerControls() will handle the actual enabling/disabling
            // This ensures all clients are synchronized
        }

        [ClientRpc]
        private void RpcOnMatchWon(Team winner, AwardData[] awards)
        {
            OnMatchWonEvent?.Invoke(winner);
            
            // Convert array to dictionary for UI
            Dictionary<ulong, AwardType> awardsDict = null;
            if (awards != null && awards.Length > 0)
            {
                awardsDict = new Dictionary<ulong, AwardType>();
                foreach (var award in awards)
                {
                    awardsDict[award.playerId] = award.awardType;
                }
            }
            
            // ✅ CRITICAL: Hide game HUD and show end-game scoreboard
            var gameHUD = FindFirstObjectByType<UI.GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.gameObject.SetActive(false);
            }
            
            // Show end-game scoreboard
            var endScoreboard = FindFirstObjectByType<UI.EndGameScoreboard>();
            if (endScoreboard != null)
            {
                endScoreboard.ShowScoreboard(winner, awardsDict);
            }
            else
            {
                LogError("[MatchManager] EndGameScoreboard not found! Cannot show end screen.");
            }
            
            // ✅ NEW: Notify UIFlowManager
            if (UI.UIFlowManager.Instance != null)
            {
                UI.UIFlowManager.Instance.ShowEndGameScoreboard();
            }
            
            LogInfo($"[Client] Match won by {winner}");
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (awards != null)
            {
                foreach (var award in awards)
                {
                    LogInfo($"[Client] Award: Player {award.playerId} - {award.awardType}");
                }
            }
            #endif
        }

        // Public getters
        public Phase GetCurrentPhase() => currentPhase;
        public float GetRemainingTime() => remainingTime;
        // ✅ REMOVED: GetTeamAWins(), GetTeamBWins() (round system removed)
        public bool IsSuddenDeathActive() => suddenDeathActive;
        public GameMode GetGameMode() => gameMode;
        
        /// <summary>
        /// Get player match stats (server-only, use Command for client access)
        /// </summary>
        [Server]
        public MatchStats GetPlayerMatchStats(ulong playerId)
        {
            return matchState.playerStats.ContainsKey(playerId) ? matchState.playerStats[playerId] : null;
        }
        
        /// <summary>
        /// ✅ NEW: Client can request stats via Command
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdRequestPlayerStats(ulong playerId, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            var stats = GetPlayerMatchStats(playerId);
            if (stats != null)
            {
                RpcSendPlayerStats(playerId, stats.kills, stats.deaths, stats.assists, 
                    stats.structuresBuilt, stats.trapKills, stats.captures, stats.defenseTime, stats.totalScore);
            }
        }
        
        /// <summary>
        /// ✅ NEW: Request all player stats (for scoreboard)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdRequestAllPlayerStats(NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            
            foreach (var kvp in matchState.playerStats)
            {
                var stats = kvp.Value;
                RpcSendPlayerStats(kvp.Key, stats.kills, stats.deaths, stats.assists,
                    stats.structuresBuilt, stats.trapKills, stats.captures, stats.defenseTime, stats.totalScore);
            }
        }
        
        /// <summary>
        /// ✅ NEW: Client-side cache for player stats
        /// </summary>
        private Dictionary<ulong, MatchStats> clientStatsCache = new Dictionary<ulong, MatchStats>();
        
        /// <summary>
        /// ✅ NEW: Get player stats from cache (client-side)
        /// </summary>
        public MatchStats GetPlayerMatchStatsClient(ulong playerId)
        {
            return clientStatsCache.ContainsKey(playerId) ? clientStatsCache[playerId] : null;
        }
        
        [ClientRpc]
        private void RpcSendPlayerStats(ulong playerId, int kills, int deaths, int assists,
            int structuresBuilt, int trapKills, int captures, float defenseTime, int totalScore)
        {
            // Update client-side cache
            if (!clientStatsCache.ContainsKey(playerId))
            {
                clientStatsCache[playerId] = new MatchStats(playerId);
            }
            
            var stats = clientStatsCache[playerId];
            stats.kills = kills;
            stats.deaths = deaths;
            stats.assists = assists;
            stats.structuresBuilt = structuresBuilt;
            stats.trapKills = trapKills;
            stats.captures = captures;
            stats.defenseTime = defenseTime;
            stats.totalScore = totalScore;
        }
        
        /// <summary>
        /// ✅ NEW: Sync all stats to clients (called at phase start)
        /// </summary>
        [Server]
        private void RpcSyncAllStats()
        {
            foreach (var kvp in matchState.playerStats)
            {
                var stats = kvp.Value;
                RpcSendPlayerStats(kvp.Key, stats.kills, stats.deaths, stats.assists,
                    stats.structuresBuilt, stats.trapKills, stats.captures, stats.defenseTime, stats.totalScore);
            }
        }
        
        // ✅ NETWORK OPTIMIZATION: Batch stats sync to reduce network traffic
        private Dictionary<ulong, float> lastStatsSyncTime = new Dictionary<ulong, float>();
        private const float STATS_SYNC_INTERVAL = 0.5f; // Sync stats every 500ms max (2 Hz)
        private const float STATS_SYNC_COOLDOWN = 0.1f; // Minimum 100ms between syncs for same player
        
        /// <summary>
        /// ✅ NEW: Notify clients when stats change (called from ScoreManager)
        /// ✅ NETWORK OPTIMIZATION: Throttled to reduce network traffic
        /// </summary>
        [Server]
        public void NotifyStatsChanged(ulong playerId)
        {
            var stats = GetPlayerMatchStats(playerId);
            if (stats == null) return;
            
            // ✅ NETWORK OPTIMIZATION: Throttle stats sync to avoid spam
            float currentTime = Time.time;
            if (lastStatsSyncTime.TryGetValue(playerId, out float lastSync))
            {
                // Check if enough time has passed since last sync
                if (currentTime - lastSync < STATS_SYNC_COOLDOWN)
                {
                    // Too soon - skip this sync (will be synced on next interval)
                    return;
                }
            }
            
            // Sync stats
            RpcSendPlayerStats(playerId, stats.kills, stats.deaths, stats.assists,
                stats.structuresBuilt, stats.trapKills, stats.captures, stats.defenseTime, stats.totalScore);
            
            // Update last sync time
            lastStatsSyncTime[playerId] = currentTime;
        }
        
        /// <summary>
        /// ✅ NETWORK OPTIMIZATION: Periodic batch sync for all players (called every STATS_SYNC_INTERVAL)
        /// </summary>
        [Server]
        private System.Collections.IEnumerator PeriodicStatsSync()
        {
            while (true)
            {
                yield return new WaitForSeconds(STATS_SYNC_INTERVAL);
                
                // Sync all player stats that haven't been synced recently
                float currentTime = Time.time;
                foreach (var kvp in matchState.playerStats)
                {
                    ulong playerId = kvp.Key;
                    if (!lastStatsSyncTime.TryGetValue(playerId, out float lastSync) || 
                        currentTime - lastSync >= STATS_SYNC_INTERVAL)
                    {
                        var stats = kvp.Value;
                        RpcSendPlayerStats(playerId, stats.kills, stats.deaths, stats.assists,
                            stats.structuresBuilt, stats.trapKills, stats.captures, stats.defenseTime, stats.totalScore);
                        lastStatsSyncTime[playerId] = currentTime;
                    }
                }
            }
        }

        [Server]
        public MatchState GetMatchState()
        {
            return matchState;
        }

        // ✅ FIX: Public getters for synced player counts (client-side UI can use this)
        public int GetTeamAPlayerCount() => teamAPlayerCount;
        public int GetTeamBPlayerCount() => teamBPlayerCount;

        [Server]
        public PlayerState GetPlayerState(ulong playerId)
        {
            return playerStates.ContainsKey(playerId) ? playerStates[playerId] : null;
        }

        [Server]
        public Dictionary<ulong, PlayerState> GetAllPlayerStates()
        {
            return new Dictionary<ulong, PlayerState>(playerStates);
        }

        /// <summary>
        /// ✅ FIX: Unregister player on disconnect (prevents crash)
        /// </summary>
        [Server]
        public void UnregisterPlayer(ulong playerId)
        {
            if (playerStates.ContainsKey(playerId))
            {
                Team team = playerStates[playerId].team;
                playerStates.Remove(playerId);

                // Update synced player counts
                UpdatePlayerCounts();

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"🚪 [MatchManager] Player {playerId} unregistered (Team: {team})");
                #endif
            }
        }

        [Server]
        public bool SpendBudget(ulong playerId, StructureCategory category, int cost)
        {
            if (!playerStates.ContainsKey(playerId))
                return false;

            var budget = playerStates[playerId].budget;

            switch (category)
            {
                case StructureCategory.Wall:
                    if (budget.wallPoints >= cost)
                    {
                        budget.wallPoints -= cost;
                        playerStates[playerId].budget = budget;
                        return true;
                    }
                    break;
                case StructureCategory.Elevation:
                    if (budget.elevationPoints >= cost)
                    {
                        budget.elevationPoints -= cost;
                        playerStates[playerId].budget = budget;
                        return true;
                    }
                    break;
                case StructureCategory.Trap:
                    if (budget.trapPoints >= cost)
                    {
                        budget.trapPoints -= cost;
                        playerStates[playerId].budget = budget;
                        return true;
                    }
                    break;
                case StructureCategory.Utility:
                    if (budget.utilityPoints >= cost)
                    {
                        budget.utilityPoints -= cost;
                        playerStates[playerId].budget = budget;
                        return true;
                    }
                    break;
            }

            return false;
        }
        
        /// <summary>
        /// ✅ NEW: Update player rankings after match end
        /// </summary>
        [Server]
        private void UpdateRankings(Team winner)
        {
            if (RankingSystem.Instance == null) return;
            
            foreach (var kvp in playerStates)
            {
                ulong playerId = kvp.Key;
                Team playerTeam = kvp.Value.team;
                bool won = (playerTeam == winner);
                
                // Get match stats for performance score
                var matchStats = GetPlayerMatchStats(playerId);
                int performanceScore = matchStats != null ? matchStats.totalScore : 0;
                
                // Update MMR
                RankingSystem.Instance.UpdateMMR(playerId, won, performanceScore);
            }
        }
    }
}


