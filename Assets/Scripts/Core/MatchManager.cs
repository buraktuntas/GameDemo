using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TacticalCombat.UI;
using TacticalCombat.Building;

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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("✅ [MatchManager] BuildValidator already exists in scene");
                #endif
                return;
            }

            // Try to find existing BuildValidator in scene
            var existing = FindFirstObjectByType<TacticalCombat.Building.BuildValidator>();
            if (existing != null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("✅ [MatchManager] Found existing BuildValidator in scene");
                #endif
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
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("✅ [MatchManager] BuildValidator prefabs copied from SimpleBuildMode");
                #endif
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [MatchManager] SimpleBuildMode not found - BuildValidator prefabs will be empty");
                #endif
            }
            
            // Spawn on network (required for NetworkBehaviour)
            NetworkServer.Spawn(validatorObj);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("✅ [MatchManager] BuildValidator auto-created and spawned on server");
            #endif
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
            currentPhase = Phase.Lobby;
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
            
            // Find ObjectiveManager
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
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
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ Player {playerId} registered with team: {team}, Role {role}");
            #endif

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
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"♻️ Player {playerId} RE-registered: Team {team}, Role {role}");
                #endif
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
                    Debug.Log($"🎨 Updated Player {playerId} visual team to {team}");
                    break;
                }
            }
        }

        [Server]
        public void StartMatch()
        {
            if (currentPhase != Phase.Lobby)
            {
                Debug.LogWarning("Cannot start match - not in lobby phase");
                return;
            }

            // Check minimum players
            if (playerStates.Count < GameConstants.MIN_PLAYERS_TO_START)
            {
                Debug.LogWarning($"Cannot start match - need at least {GameConstants.MIN_PLAYERS_TO_START} players");
                return;
            }

            Debug.Log($"Starting match - Mode: {gameMode}, Players: {playerStates.Count}");
            StartBuildPhase();
        }

        [Server]
        private void StartBuildPhase()
        {
            Debug.Log("Starting Build Phase (3 minutes)");
            
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
            RpcOnPhaseChanged(currentPhase);
            
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

        [Server]
        private void TransitionToCombat()
        {
            Debug.Log("Transitioning to Combat Phase (15 minutes)");
            currentPhase = Phase.Combat;
            remainingTime = combatDuration;
            suddenDeathActive = false;
            RpcOnPhaseChanged(currentPhase);
            
            // Initialize core objects if ObjectiveManager exists
            if (objectiveManager != null)
            {
                objectiveManager.InitializeCores();
            }
            
            StartCoroutine(CombatPhaseTimer());
        }

        [Server]
        private IEnumerator CombatPhaseTimer()
        {
            const float winCheckInterval = 0.5f;
            const float suddenDeathCheckInterval = 1f;
            float nextCheck = 0f;
            float nextSuddenDeathCheck = 0f;
            bool suddenDeathTriggered = false;

            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;

                // Check for sudden death (final 2 minutes)
                if (!suddenDeathTriggered && remainingTime <= suddenDeathDuration)
                {
                    ActivateSuddenDeath();
                    suddenDeathTriggered = true;
                }

                // Check win condition every 0.5s
                if (Time.time >= nextCheck)
                {
                    if (IsWinConditionMet()) break;
                    nextCheck = Time.time + winCheckInterval;
                }

                yield return null;
            }

            // ✅ FIX: Determine winner when time runs out
            Team winner = DetermineWinnerByScore();
            EndMatch(winner);
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

        [Server]
        private void ActivateSuddenDeath()
        {
            Debug.Log("⚡ SUDDEN DEATH ACTIVATED - Secret tunnel opens!");
            suddenDeathActive = true;
            matchState.suddenDeathActive = true;
            RpcOnSuddenDeathActivated();
            
            // TODO: Open secret tunnel between bases (will be handled by ObjectiveManager)
            if (objectiveManager != null)
            {
                objectiveManager.OpenSuddenDeathTunnel();
            }
        }

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
                Debug.Log($"💀 Player {playerId} died. Current phase: {currentPhase}");

                // Check win condition in any phase (not just Combat)
                CheckWinCondition();
            }
        }

        [Server]
        public void OnCoreDestroyed(Team winner)
        {
            Debug.Log($"💥 Core destroyed! Winner: {winner}");
            EndMatch(winner);
        }

        [Server]
        private void CheckWinCondition()
        {
            Debug.Log($"🔍 Checking win condition...");

            if (!IsWinConditionMet())
            {
                Debug.Log($"❌ Win condition not met yet");
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

            Debug.Log($"⚔️ Alive count: TeamA={teamAAlive}, TeamB={teamBAlive}");

            Team winner = Team.None;
            if (teamAAlive > 0 && teamBAlive == 0)
                winner = Team.TeamA;
            else if (teamBAlive > 0 && teamAAlive == 0)
                winner = Team.TeamB;

            if (winner != Team.None)
            {
                Debug.Log($"🏆 Winner: {winner}");
                EndMatch(winner);
            }
            else
            {
                Debug.Log($"⚠️ No winner yet (both teams have players alive or both dead)");
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
            Debug.Log($"🏆 Match ended! Winner: {winner}");
            
            currentPhase = Phase.End;
            remainingTime = endPhaseDuration;
            RpcOnPhaseChanged(currentPhase);
            RpcOnMatchWon(winner, awards);
            
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
                Debug.LogWarning("[MatchManager] Only host can restart match");
                #endif
                return;
            }
            
            RestartMatch();
        }
        
        [Server]
        private void RestartMatch()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("🔄 [MatchManager] Restarting match...");
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
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Client] Match restarted!");
            #endif
            
            // Hide end game scoreboard
            var endScoreboard = FindFirstObjectByType<UI.EndGameScoreboard>();
            if (endScoreboard != null)
            {
                endScoreboard.HideScoreboard();
            }
        }

        // Client-side phase change handler
        private void OnPhaseChanged(Phase oldPhase, Phase newPhase)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Phase changed: {oldPhase} -> {newPhase}");
            #endif
            OnPhaseChangedEvent?.Invoke(newPhase);
        }

        [ClientRpc]
        private void RpcOnPhaseChanged(Phase newPhase)
        {
            OnPhaseChangedEvent?.Invoke(newPhase);
        }

        [ClientRpc]
        private void RpcOnSuddenDeathActivated()
        {
            OnSuddenDeathActivated?.Invoke();
            Debug.Log("⚡ SUDDEN DEATH ACTIVATED!");
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
            
            // Show end-game scoreboard
            var endScoreboard = FindFirstObjectByType<UI.EndGameScoreboard>();
            if (endScoreboard != null)
            {
                endScoreboard.ShowScoreboard(winner, awardsDict);
            }
            
            Debug.Log($"[Client] Match won by {winner}");
            if (awards != null)
            {
                foreach (var award in awards)
                {
                    Debug.Log($"[Client] Award: Player {award.playerId} - {award.awardType}");
                }
            }
        }

        // Public getters
        public Phase GetCurrentPhase() => currentPhase;
        public float GetRemainingTime() => remainingTime;
        // ✅ REMOVED: GetTeamAWins(), GetTeamBWins() (round system removed)
        public bool IsSuddenDeathActive() => suddenDeathActive;
        public GameMode GetGameMode() => gameMode;
        public MatchStats GetPlayerMatchStats(ulong playerId)
        {
            return matchState.playerStats.ContainsKey(playerId) ? matchState.playerStats[playerId] : null;
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
    }
}


