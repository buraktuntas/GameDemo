using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class Health : NetworkBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = GameConstants.PLAYER_MAX_HEALTH;

        // ✅ FIX: Remove hook to prevent double-fire (once on set, once on sync)
        // Use manual RPC instead for reliable single notification
        [SyncVar]
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

            // ✅ FIX: Manual RPC notification instead of SyncVar hook
            RpcNotifyHealthChanged(currentHealth);

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

            // ✅ FIX: Manual RPC notification instead of SyncVar hook
            RpcNotifyHealthChanged(currentHealth);
        }

        [Server]
        private void Die(ulong killerId)
        {
            if (isDead) return;

            isDead = true;
            Debug.Log($"💀 [Server] {gameObject.name} died (killed by {killerId})");

            // Notify MatchManager if this is a player
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                MatchManager.Instance?.NotifyPlayerDeath(player.playerId);
            }

            RpcOnDeath();

            // Auto-respawn after delay (Battlefield style)
            StartCoroutine(RespawnAfterDelay(5f));
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            OnDeathEvent?.Invoke();

            Debug.Log($"💀 [Client] Death event received for {gameObject.name}");

            // Disable player controls or switch to spectator
            if (TryGetComponent<TacticalCombat.Player.FPSController>(out var fps))
            {
                fps.SetCanMove(false);
            }
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                player.enabled = false;
            }

            // Hide weapon visual
            if (TryGetComponent<Combat.WeaponSystem>(out var weaponSystem))
            {
                weaponSystem.enabled = false;
            }
        }

        /// <summary>
        /// ✅ BATTLEFIELD-STYLE: Auto-respawn after death delay
        /// </summary>
        [Server]
        private System.Collections.IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (isDead)
            {
                Respawn();
            }
        }

        /// <summary>
        /// ✅ Server-authoritative respawn
        /// </summary>
        [Server]
        public void Respawn()
        {
            if (!isServer)
            {
                Debug.LogWarning("❌ Respawn called on client!");
                return;
            }

            Debug.Log($"🔄 [Server] Respawning {gameObject.name}");

            // Restore health
            currentHealth = maxHealth;
            isDead = false;

            // ✅ FIX: Manual RPC notification for health restore
            RpcNotifyHealthChanged(currentHealth);

            // Find spawn point
            Vector3 spawnPosition = FindRespawnPosition();
            transform.position = spawnPosition;

            // Notify clients
            RpcOnRespawn();
        }

        [ClientRpc]
        private void RpcOnRespawn()
        {
            Debug.Log($"🔄 [Client] Respawn event received for {gameObject.name}");

            // Re-enable player controls
            if (TryGetComponent<TacticalCombat.Player.FPSController>(out var fps))
            {
                fps.SetCanMove(true);
            }
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                player.enabled = true;
            }

            // Re-enable weapon
            if (TryGetComponent<Combat.WeaponSystem>(out var weaponSystem))
            {
                weaponSystem.enabled = true;
            }
        }

        /// <summary>
        /// Find a safe respawn position
        /// </summary>
        private Vector3 FindRespawnPosition()
        {
            // Try to find spawn points in scene
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

            if (spawnPoints.Length > 0)
            {
                // Random spawn point
                int randomIndex = Random.Range(0, spawnPoints.Length);
                return spawnPoints[randomIndex].transform.position;
            }

            // Fallback: spawn at origin with offset
            Vector3 randomOffset = new Vector3(
                Random.Range(-5f, 5f),
                2f,
                Random.Range(-5f, 5f)
            );
            return Vector3.zero + randomOffset;
        }

        // ✅ FIX: Manual RPC for health changes instead of SyncVar hook
        [ClientRpc]
        private void RpcNotifyHealthChanged(int newHealth)
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
            
            // Editor mode'da veya Play mode'da güvenli şekilde set et
            try
            {
                if (Application.isPlaying && isServer)
                {
                    currentHealth = maxHealth;
                }
                else
                {
                    // Editor mode'da veya client'da direkt set et
                    currentHealth = maxHealth;
                }
            }
            catch (System.Exception)
            {
                // Herhangi bir hata durumunda direkt set et
                currentHealth = maxHealth;
            }
        }
    }
}


