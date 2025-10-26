using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

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
        
        private RoundState roundState = new RoundState();

        [Header("Configuration")]
        [SerializeField] private float buildDuration = GameConstants.BUILD_DURATION;
        [SerializeField] private float combatDuration = GameConstants.COMBAT_DURATION;
        [SerializeField] private float roundEndDuration = GameConstants.ROUND_END_DURATION;

        [Header("Team Tracking")]
        private Dictionary<ulong, PlayerState> playerStates = new Dictionary<ulong, PlayerState>();
        
        [SyncVar]
        private int teamAWins = 0;
        
        [SyncVar]
        private int teamBWins = 0;
        
        [SyncVar]
        private int currentRound = 0;

        // Events
        public System.Action<Phase> OnPhaseChangedEvent;
        public System.Action<Team> OnRoundWonEvent;
        public System.Action<Team> OnMatchWonEvent;

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
            InitializeMatch();
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
            roundState = new RoundState();
            currentPhase = Phase.Lobby;
            teamAWins = 0;
            teamBWins = 0;
            currentRound = 0;
            playerStates.Clear();
        }

        [Server]
        public void RegisterPlayer(ulong playerId, Team team, RoleId role)
        {
            if (!playerStates.ContainsKey(playerId))
            {
                playerStates[playerId] = new PlayerState(playerId, team, role);
                Debug.Log($"Player {playerId} registered: Team {team}, Role {role}");
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

            Debug.Log("Starting match...");
            StartRound();
        }

        [Server]
        private void StartRound()
        {
            currentRound++;
            Debug.Log($"Starting Round {currentRound}");
            
            // Reset player states
            foreach (var state in playerStates.Values)
            {
                state.isAlive = true;
                state.budget = BuildBudget.GetRoleBudget(state.role);
            }

            currentPhase = Phase.Build;
            remainingTime = buildDuration;
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
            currentPhase = Phase.Combat;
            remainingTime = combatDuration;
            RpcOnPhaseChanged(currentPhase);
            
            StartCoroutine(CombatPhaseTimer());
        }

        [Server]
        private IEnumerator CombatPhaseTimer()
        {
            while (remainingTime > 0 && !IsWinConditionMet())
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            EndRound();
        }

        [Server]
        private bool IsWinConditionMet()
        {
            // Check if all players of one team are dead
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

            return teamAAlive == 0 || teamBAlive == 0;
        }

        [Server]
        public void NotifyPlayerDeath(ulong playerId)
        {
            if (playerStates.ContainsKey(playerId))
            {
                playerStates[playerId].isAlive = false;
                Debug.Log($"Player {playerId} died");
                
                if (currentPhase == Phase.Combat)
                {
                    CheckWinCondition();
                }
            }
        }

        [Server]
        public void NotifyCoreDestroyed(Team team)
        {
            Debug.Log($"{team} core destroyed!");
            Team winner = team == Team.TeamA ? Team.TeamB : Team.TeamA;
            AwardRoundWin(winner);
        }

        [Server]
        private void CheckWinCondition()
        {
            if (!IsWinConditionMet()) return;

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
                AwardRoundWin(winner);
            }
        }

        [Server]
        private void EndRound()
        {
            // If time ran out, check who has more players alive or if a core is still standing
            if (!IsWinConditionMet())
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

                Team winner = teamAAlive > teamBAlive ? Team.TeamA : 
                             teamBAlive > teamAAlive ? Team.TeamB : Team.None;

                if (winner != Team.None)
                {
                    AwardRoundWin(winner);
                }
                else
                {
                    // Draw - no one wins this round
                    Debug.Log("Round ended in a draw");
                    StartCoroutine(RoundEndSequence(Team.None));
                }
            }
        }

        [Server]
        private void AwardRoundWin(Team winner)
        {
            Debug.Log($"{winner} wins the round!");
            
            if (winner == Team.TeamA)
                teamAWins++;
            else if (winner == Team.TeamB)
                teamBWins++;

            RpcOnRoundWon(winner);
            StartCoroutine(RoundEndSequence(winner));
        }

        [Server]
        private IEnumerator RoundEndSequence(Team roundWinner)
        {
            currentPhase = Phase.RoundEnd;
            remainingTime = roundEndDuration;
            RpcOnPhaseChanged(currentPhase);

            yield return new WaitForSeconds(roundEndDuration);

            // Check if match is over (BO3)
            if (teamAWins >= GameConstants.ROUNDS_TO_WIN)
            {
                EndMatch(Team.TeamA);
            }
            else if (teamBWins >= GameConstants.ROUNDS_TO_WIN)
            {
                EndMatch(Team.TeamB);
            }
            else
            {
                // Start next round
                StartRound();
            }
        }

        [Server]
        private void EndMatch(Team winner)
        {
            Debug.Log($"{winner} wins the match!");
            RpcOnMatchWon(winner);
            
            // Match ended - could return to lobby or disconnect
            currentPhase = Phase.Lobby;
        }

        // Client-side phase change handler
        private void OnPhaseChanged(Phase oldPhase, Phase newPhase)
        {
            Debug.Log($"Phase changed: {oldPhase} -> {newPhase}");
            OnPhaseChangedEvent?.Invoke(newPhase);
        }

        [ClientRpc]
        private void RpcOnPhaseChanged(Phase newPhase)
        {
            OnPhaseChangedEvent?.Invoke(newPhase);
        }

        [ClientRpc]
        private void RpcOnRoundWon(Team winner)
        {
            OnRoundWonEvent?.Invoke(winner);
        }

        [ClientRpc]
        private void RpcOnMatchWon(Team winner)
        {
            OnMatchWonEvent?.Invoke(winner);
        }

        // Public getters
        public Phase GetCurrentPhase() => currentPhase;
        public float GetRemainingTime() => remainingTime;
        public int GetTeamAWins() => teamAWins;
        public int GetTeamBWins() => teamBWins;
        public int GetCurrentRound() => currentRound;

        [Server]
        public PlayerState GetPlayerState(ulong playerId)
        {
            return playerStates.ContainsKey(playerId) ? playerStates[playerId] : null;
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


