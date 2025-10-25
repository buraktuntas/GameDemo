using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class Health : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = GameConstants.PLAYER_MAX_HEALTH;
        
        [SyncVar(hook = nameof(OnHealthChanged))]
        private int currentHealth;

        [SyncVar]
        private bool isDead = false;

        public System.Action<int, int> OnHealthChangedEvent; // current, max
        public System.Action OnDeathEvent;

        private void Start()
        {
            if (isServer)
            {
                currentHealth = maxHealth;
            }
        }

        [Server]
        public void TakeDamage(int damage, ulong attackerId = 0)
        {
            if (isDead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die(attackerId);
            }
        }

        [Server]
        public void Heal(int amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        [Server]
        private void Die(ulong killerId)
        {
            if (isDead) return;

            isDead = true;
            Debug.Log($"{gameObject.name} died");

            // Notify MatchManager if this is a player
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                MatchManager.Instance?.NotifyPlayerDeath(player.playerId);
            }

            RpcOnDeath();
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            OnDeathEvent?.Invoke();
            
            // Disable player controls or switch to spectator
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                player.enabled = false;
            }
        }

        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            OnHealthChangedEvent?.Invoke(newHealth, maxHealth);
        }

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public bool IsDead() => isDead;

        public void SetMaxHealth(int max)
        {
            maxHealth = max;
            if (isServer)
            {
                currentHealth = maxHealth;
            }
        }
    }
}


