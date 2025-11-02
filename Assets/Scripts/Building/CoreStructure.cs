using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Combat;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Core Structure - Ana hedef, yıkılırsa instant round win
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class CoreStructure : NetworkBehaviour
    {
        [SerializeField] private Team team;
        [SerializeField] private int maxHealth = GameConstants.CORE_HP;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health == null)
                health = gameObject.AddComponent<Health>();
        }

        private void Start()
        {
            health.SetMaxHealth(maxHealth);
            health.OnDeathEvent += OnCoreDestroyed;
        }

        public void SetTeam(Team newTeam)
        {
            team = newTeam;
        }

        private void OnCoreDestroyed()
        {
            Debug.Log($"Core {team} DESTROYED!");

            if (isServer && MatchManager.Instance != null)
            {
                Team winner = team == Team.TeamA ? Team.TeamB : Team.TeamA;
                MatchManager.Instance.OnCoreDestroyed(winner);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeathEvent -= OnCoreDestroyed;
        }
    }
}
