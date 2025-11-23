using UnityEngine;
using Mirror;
using TacticalCombat.Combat;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Core Structure - Ana yapı
    /// Bu yapı yıkılınca takım round'u kaybeder
    /// </summary>
    public class CoreStructure : NetworkBehaviour, IDamageable
    {
        [Header("Core Settings")]
        [SerializeField] private Team team = Team.TeamA;
        [SerializeField] private int maxHealth = 1000;

        // ✅ PHASE 2: Removed SyncVar hook to prevent double-fire (use manual RPC like Health.cs)
        [SyncVar] private int currentHealth;

        [SyncVar]
        private bool isDestroyed = false;

        [Header("Visual")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material teamAMaterial;
        [SerializeField] private Material teamBMaterial;

        [Header("Effects")]
        [SerializeField] private GameObject destructionEffect;
        [SerializeField] private GameObject damageEffect;

        public System.Action<int, int> OnHealthChangedEvent; // current, max
        public System.Action OnDestroyedEvent;

        private void Awake()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // ✅ PHASE 2: Sync initial health from server
            RpcOnHealthChanged(currentHealth, maxHealth);
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (meshRenderer == null) return;

            // ✅ PHASE 2: Use sharedMaterial to prevent memory leak
            // Set team color
            if (team == Team.TeamA && teamAMaterial != null)
            {
                meshRenderer.sharedMaterial = teamAMaterial;
            }
            else if (team == Team.TeamB && teamBMaterial != null)
            {
                meshRenderer.sharedMaterial = teamBMaterial;
            }

            // Show damage state
            if (currentHealth < maxHealth * 0.5f)
            {
                // Show damage effect
                if (damageEffect != null && !damageEffect.activeSelf)
                {
                    damageEffect.SetActive(true);
                }
            }
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (!isServer)
            {
                Debug.LogWarning("❌ ApplyDamage called on client!");
                return;
            }

            if (isDestroyed) return;

            currentHealth -= damageInfo.Amount;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"💥 [Server] CoreStructure ({team}) took {damageInfo.Amount} damage. Health: {currentHealth}/{maxHealth}");

            // ✅ PHASE 2: Manual RPC instead of SyncVar hook (prevents double-fire)
            RpcOnHealthChanged(currentHealth, maxHealth);

            // Notify clients
            RpcOnDamaged();

            if (currentHealth <= 0)
            {
                DestroyCore();
            }
        }

        [ClientRpc]
        private void RpcOnDamaged()
        {
            // Play hit effect
            if (damageEffect != null && !isDestroyed)
            {
                damageEffect.SetActive(true);
            }
        }

        [Server]
        private void DestroyCore()
        {
            if (isDestroyed) return;

            isDestroyed = true;

            Debug.Log($"💀 [Server] CoreStructure ({team}) destroyed!");

            // Notify MatchManager
            if (MatchManager.Instance != null)
            {
                Team winner = team == Team.TeamA ? Team.TeamB : Team.TeamA;
                MatchManager.Instance.OnCoreDestroyed(winner);
            }

            // Show destruction effect
            RpcOnDestroyed();
        }

        [ClientRpc]
        private void RpcOnDestroyed()
        {
            OnDestroyedEvent?.Invoke();

            // Play destruction effect
            if (destructionEffect != null)
            {
                // ✅ PERFORMANCE: Use object pooling if available
                GameObject effect;
                if (NetworkObjectPool.Instance != null)
                {
                    effect = NetworkObjectPool.Instance.Get(destructionEffect, transform.position, Quaternion.identity);
                    // Auto-return to pool after 5 seconds
                    StartCoroutine(ReturnEffectToPool(effect, 5f));
                }
                else
                {
                    effect = Instantiate(destructionEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 5f);
                }
            }

            // Hide core visual
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            // Disable collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        /// <summary>
        /// ✅ PERFORMANCE: Helper coroutine to return effect to pool after delay
        /// </summary>
        private System.Collections.IEnumerator ReturnEffectToPool(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null && NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.Release(effect);
            }
        }

        /// <summary>
        /// ✅ PHASE 2: Manual RPC instead of SyncVar hook (prevents double-fire)
        /// </summary>
        [ClientRpc]
        private void RpcOnHealthChanged(int newHealth, int maxHealth)
        {
            OnHealthChangedEvent?.Invoke(newHealth, maxHealth);
            UpdateVisuals();
        }
        
        /// <summary>
        /// Legacy method - kept for compatibility but not used by SyncVar hook anymore
        /// </summary>
        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            // This is no longer called by SyncVar hook
            // Health updates come via RpcOnHealthChanged
        }

        // IDamageable interface
        public bool IsAlive => !isDestroyed;
        public float HealthPercentage => (float)currentHealth / maxHealth;

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public Team GetTeam() => team;
        public bool IsDestroyed() => isDestroyed;

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = team == Team.TeamA ? Color.blue : Color.red;
            Gizmos.DrawWireCube(transform.position, transform.localScale);

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"CORE - {team}");
        }
        #endif
    }
}
