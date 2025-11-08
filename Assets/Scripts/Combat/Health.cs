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

        // ✅ HIGH PRIORITY: Track last damage time for combat lockout
        private float lastDamageTime = 0f;
        private const float COMBAT_LOCKOUT_DURATION = 3f; // 3 seconds after taking damage

        // ✅ FIX: Cache NetworkManager reference (performance optimization)
        private static Network.NetworkGameManager cachedNetworkManager;

        private void Start()
        {
            if (isServer)
            {
                currentHealth = maxHealth;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Initial health update for UI (after SyncVar is synced)
            if (isLocalPlayer)
            {
                // Wait one frame to ensure all components are initialized
                StartCoroutine(NotifyInitialHealth());
            }
        }

        private System.Collections.IEnumerator NotifyInitialHealth()
        {
            yield return null; // Wait one frame for all components to subscribe

            // Fire initial event so UI shows correct health
            OnHealthChangedEvent?.Invoke(currentHealth, maxHealth);
            Debug.Log($"🏥 [Client] Initial health: {currentHealth}/{maxHealth}");
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

            // ✅ HIGH PRIORITY: Update last damage time for combat lockout
            lastDamageTime = Time.time;

            currentHealth -= finalDamage;
            currentHealth = Mathf.Max(0, currentHealth);

            // ✅ FIX: Manual RPC notification instead of SyncVar hook
            RpcNotifyHealthChanged(currentHealth);

            // ✅ HIT FEEDBACK: Camera shake for victim
            RpcNotifyHit(finalDamage);

            Debug.Log($"💥 [Server] {gameObject.name} took {finalDamage} damage ({info.Type}){(info.IsHeadshot ? " - HEADSHOT" : "")}. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die(info.AttackerID, info.IsHeadshot);
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
        private void Die(ulong killerId, bool isHeadshot = false)
        {
            if (isDead) return;

            isDead = true;
            Debug.Log($"💀 [Server] {gameObject.name} died (killed by {killerId}{(isHeadshot ? " - HEADSHOT" : "")})");

            // Notify MatchManager if this is a player
            if (TryGetComponent<TacticalCombat.Player.PlayerController>(out var player))
            {
                MatchManager.Instance?.NotifyPlayerDeath(player.playerId);
            }

            // Show kill feed
            RpcShowKillFeed(killerId, netId, isHeadshot);

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
            RpcShowRespawnCountdown(delay);

            for (int i = Mathf.CeilToInt(delay); i > 0; i--)
            {
                RpcUpdateRespawnCountdown(i);
                yield return new WaitForSeconds(1f);
            }

            if (isDead)
            {
                RpcHideRespawnCountdown();
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

        // ✅ PERFORMANCE FIX: Cache spawn points to prevent GC allocation
        private static Transform[] cachedSpawnPoints = null;
        private static float lastSpawnPointCacheTime = 0f;
        private const float SPAWN_POINT_CACHE_DURATION = 30f; // Refresh cache every 30 seconds

        /// <summary>
        /// Find a safe respawn position
        /// ✅ PERFORMANCE: Uses cached spawn points to prevent GC spikes from FindGameObjectsWithTag
        /// </summary>
        private Vector3 FindRespawnPosition()
        {
            // Refresh cache if expired or null
            if (cachedSpawnPoints == null || Time.time - lastSpawnPointCacheTime > SPAWN_POINT_CACHE_DURATION)
            {
                RefreshSpawnPointCache();
            }

            if (cachedSpawnPoints != null && cachedSpawnPoints.Length > 0)
            {
                // Random spawn point
                int randomIndex = Random.Range(0, cachedSpawnPoints.Length);
                if (cachedSpawnPoints[randomIndex] != null)
                {
                    return cachedSpawnPoints[randomIndex].position;
                }
            }

            // ✅ FIX: Cache NetworkManager reference (don't call FindFirstObjectByType every respawn!)
            if (cachedNetworkManager == null)
            {
                cachedNetworkManager = FindFirstObjectByType<Network.NetworkGameManager>();
            }

            if (cachedNetworkManager != null)
            {
                // Use reflection to get spawn points (private field)
                var spawnPoints = cachedNetworkManager.GetType().GetField("teamASpawnPoints",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnPoints != null)
                {
                    var points = spawnPoints.GetValue(cachedNetworkManager) as Transform[];
                    if (points != null && points.Length > 0)
                    {
                        return points[Random.Range(0, points.Length)].position;
                    }
                }
            }

            // Final fallback: spawn at origin with offset
            Vector3 randomOffset = new Vector3(
                Random.Range(-5f, 5f),
                2f,
                Random.Range(-5f, 5f)
            );
            return Vector3.zero + randomOffset;
        }

        /// <summary>
        /// ✅ PERFORMANCE: Cache spawn points once to avoid repeated FindGameObjectsWithTag calls
        /// </summary>
        private static void RefreshSpawnPointCache()
        {
            // ✅ FIX: Use FindObjectsByType instead of FindGameObjectsWithTag (less GC)
            var spawnObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            System.Collections.Generic.List<Transform> spawnList = new System.Collections.Generic.List<Transform>();
            
            for (int i = 0; i < spawnObjects.Length; i++)
            {
                if (spawnObjects[i].CompareTag("SpawnPoint"))
                {
                    spawnList.Add(spawnObjects[i].transform);
                }
            }

            cachedSpawnPoints = spawnList.ToArray();
            lastSpawnPointCacheTime = Time.time;
            
            Debug.Log($"✅ [Health] Spawn point cache refreshed: {cachedSpawnPoints.Length} points found");
        }

        // ✅ FIX: Manual RPC for health changes instead of SyncVar hook
        [ClientRpc]
        private void RpcNotifyHealthChanged(int newHealth)
        {
            OnHealthChangedEvent?.Invoke(newHealth, maxHealth);
        }

        /// <summary>
        /// ✅ HIT FEEDBACK: Notify victim player for camera shake
        /// </summary>
        [ClientRpc]
        private void RpcNotifyHit(int damage)
        {
            // Only apply feedback to local player (victim)
            if (!isLocalPlayer) return;

            // Camera shake
            if (TryGetComponent<TacticalCombat.Player.CameraShake>(out var cameraShake))
            {
                cameraShake.ShakeOnHit(damage, maxHealth);
            }

            // Optional: Add screen flash, damage indicator, sound, etc.
            Debug.Log($"🎯 [Client] HIT! Took {damage} damage");
        }

        [ClientRpc]
        private void RpcShowKillFeed(ulong killerId, ulong victimId, bool isHeadshot)
        {
            if (TacticalCombat.UI.GameHUD.Instance == null) return;

            string killerName = GetPlayerName(killerId);
            string victimName = GetPlayerName(victimId);

            TacticalCombat.UI.GameHUD.Instance.ShowKillFeed(killerName, victimName, isHeadshot);
        }

        private string GetPlayerName(ulong playerId)
        {
            var players = FindObjectsByType<TacticalCombat.Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.netId == playerId)
                {
                    return $"Player {playerId}";
                }
            }
            return $"Player {playerId}";
        }

        [ClientRpc]
        private void RpcShowRespawnCountdown(float seconds)
        {
            if (!isLocalPlayer) return;
            if (TacticalCombat.UI.GameHUD.Instance != null)
            {
                TacticalCombat.UI.GameHUD.Instance.ShowRespawnCountdown(seconds);
            }
        }

        [ClientRpc]
        private void RpcUpdateRespawnCountdown(float seconds)
        {
            if (!isLocalPlayer) return;
            if (TacticalCombat.UI.GameHUD.Instance != null)
            {
                TacticalCombat.UI.GameHUD.Instance.ShowRespawnCountdown(seconds);
            }
        }

        [ClientRpc]
        private void RpcHideRespawnCountdown()
        {
            if (!isLocalPlayer) return;
            if (TacticalCombat.UI.GameHUD.Instance != null)
            {
                TacticalCombat.UI.GameHUD.Instance.HideRespawnCountdown();
            }
        }

        // ⭐ IDamageable interface properties
        public bool IsAlive => !isDead;
        public float HealthPercentage => (float)currentHealth / maxHealth;
        
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public bool IsDead() => isDead;

        /// <summary>
        /// ✅ HIGH PRIORITY: Check if player is in combat (took damage recently)
        /// </summary>
        public bool IsInCombat()
        {
            return Time.time - lastDamageTime < COMBAT_LOCKOUT_DURATION;
        }

        /// <summary>
        /// Get time since last damage (for combat lockout)
        /// </summary>
        public float GetTimeSinceLastDamage()
        {
            return Time.time - lastDamageTime;
        }
        
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


