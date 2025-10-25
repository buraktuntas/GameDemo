using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class Health : NetworkBehaviour, IDamageable
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

        // ⭐ Interface implementation
        public void ApplyDamage(DamageInfo info)
        {
            if (!isServer)
            {
                Debug.LogWarning("❌ ApplyDamage called on client!");
                return;
            }
            
            ApplyDamageInternal(info);
        }
        
        // ⭐ Private damage method
        [Server]
        private void ApplyDamageInternal(DamageInfo info)
        {
            if (isDead) return;
            
            // Damage reduction based on armor, etc.
            int finalDamage = CalculateFinalDamage(info);
            
            currentHealth -= finalDamage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            Debug.Log($"{gameObject.name} took {finalDamage} damage ({info.Type}). Health: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                Die(info.AttackerID);
            }
        }
        
        // ⭐ Legacy method for backward compatibility
        [Server]
        public void TakeDamage(int damage, ulong attackerId = 0)
        {
            DamageInfo info = new DamageInfo(damage, attackerId, DamageType.Bullet, transform.position);
            ApplyDamageInternal(info);
        }
        
        private int CalculateFinalDamage(DamageInfo info)
        {
            // TODO: Add armor, damage reduction, etc.
            return info.Amount;
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
            if (TryGetComponent<TacticalCombat.Player.FPSController>(out var fps))
            {
                fps.SetCanMove(false);
            }
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                player.enabled = false;
            }
        }

        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            OnHealthChangedEvent?.Invoke(newHealth, maxHealth);
        }

        // ⭐ IDamageable interface properties
        public bool IsAlive => !isDead;
        public float HealthPercentage => (float)currentHealth / maxHealth;
        
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public bool IsDead() => isDead;
        
        // Public properties for UI access
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

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


