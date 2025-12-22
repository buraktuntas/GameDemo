using UnityEngine;
using System.Collections;
using Mirror;
using TacticalCombat.UI;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Handles the logical transitions and timers for match phases.
    /// Delegates networking and state storage back to MatchManager.
    /// </summary>
    public class MatchPhaseController : MonoBehaviour
    {
        private MatchManager manager;

        private void Awake()
        {
            manager = GetComponent<MatchManager>();
        }

        [Server]
        public void TransitionToBuild()
        {
            LogInfo("[PhaseController] Transitioning to Build Phase");
            
            manager.SetPhase(Phase.Build);
            manager.SetRemainingTime(manager.BuildDuration);
            manager.SetSuddenDeathActive(false);
            
            manager.RpcOnPhaseChanged(Phase.Build);
            manager.RpcEnablePlayerControls(true);
            
            StopAllCoroutines();
            StartCoroutine(BuildPhaseTimer());
        }

        [Server]
        private IEnumerator BuildPhaseTimer()
        {
            float time = manager.BuildDuration;
            while (time > 0)
            {
                time -= Time.deltaTime;
                manager.SetRemainingTime(time);
                yield return null;
            }
            
            TransitionToCombat();
        }

        [Server]
        public void TransitionToCombat()
        {
            LogInfo("[PhaseController] Transitioning to Combat Phase");
            
            manager.SetPhase(Phase.Combat);
            manager.SetRemainingTime(manager.CombatDuration);
            manager.SetSuddenDeathActive(false);
            
            manager.RpcOnPhaseChanged(Phase.Combat);
            
            if (Building.BuildManager.Instance != null)
                Building.BuildManager.Instance.EndBuildPhase();
                
            manager.RpcEnablePlayerControls(true);
            
            if (manager.ObjectiveMgr != null)
                manager.ObjectiveMgr.InitializeCores();
                
            StopAllCoroutines();
            StartCoroutine(CombatPhaseTimer());
        }

        [Server]
        private IEnumerator CombatPhaseTimer()
        {
            float time = manager.CombatDuration;
            while (time > 0)
            {
                time -= Time.deltaTime;
                manager.SetRemainingTime(time);
                
                // Check win condition periodically
                if (manager.CheckWinConditionInternal()) yield break;
                
                yield return null;
            }
            
            TransitionToSuddenDeath();
        }

        [Server]
        public void TransitionToSuddenDeath()
        {
            LogInfo("[PhaseController] Transitioning to Sudden Death");
            
            manager.SetPhase(Phase.SuddenDeath);
            manager.SetRemainingTime(manager.SuddenDeathDuration);
            manager.SetSuddenDeathActive(true);
            
            manager.RpcOnPhaseChanged(Phase.SuddenDeath);
            manager.RpcOnSuddenDeathActivated();
            
            StopAllCoroutines();
            StartCoroutine(SuddenDeathTimer());
        }

        [Server]
        private IEnumerator SuddenDeathTimer()
        {
            float time = manager.SuddenDeathDuration;
            while (time > 0)
            {
                time -= Time.deltaTime;
                manager.SetRemainingTime(time);
                
                if (manager.CheckWinConditionInternal()) yield break;
                
                yield return null;
            }
            
            Team winner = manager.DetermineWinnerByScore();
            manager.EndMatch(winner);
        }
    }
}
