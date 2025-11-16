using UnityEngine;
using Mirror;
using TacticalCombat.Combat;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    /// <summary>
    /// ✅ NEW: BreakableHealth for structures (per Level Design Spec)
    /// Specialized health system for breakable structures with destruction effects
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public class BreakableHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SyncVar]
        private int currentHealth;
        
        [SyncVar]
        private int maxHealth;
        
        private Structure structure;
        private Health healthComponent; // Use existing Health component for damage handling
        
        public System.Action<int, int> OnHealthChangedEvent; // current, max
        public System.Action OnDestroyedEvent;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            structure = GetComponent<Structure>();
            healthComponent = GetComponent<Health>();
            
            if (structure != null)
            {
                // Get health from Health component (set by Structure.OnStartServer)
                if (healthComponent != null)
                {
                    maxHealth = healthComponent.GetMaxHealth();
                    currentHealth = maxHealth;
                    healthComponent.OnDeathEvent += OnStructureDestroyed;
                }
                else
                {
                    // Fallback: use default health
                    maxHealth = 100;
                    currentHealth = maxHealth;
                }
            }
        }
        
        /// <summary>
        /// Apply damage to structure (server-only)
        /// </summary>
        [Server]
        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (healthComponent != null)
            {
                // Use existing Health component for damage calculation
                healthComponent.ApplyDamage(damageInfo);
            }
            else
            {
                // Fallback: direct damage
                int finalDamage = damageInfo.Amount;
                currentHealth -= finalDamage;
                currentHealth = Mathf.Max(0, currentHealth);
                
                RpcOnHitVfx(damageInfo.HitPoint);
                
                if (currentHealth <= 0)
                {
                    OnStructureDestroyed();
                }
            }
        }
        
        [Server]
        private void OnStructureDestroyed()
        {
            if (currentHealth > 0) return; // Already destroyed
            
            OnDestroyedEvent?.Invoke();
            
            // Notify Structure component
            if (structure != null)
            {
                structure.TriggerDestructionEffects();
            }
            
            // Notify BuildManager for cleanup
            if (BuildManager.Instance != null)
            {
                BuildManager.Instance.OnStructureDestroyed(netId);
            }
            
            RpcOnDestroyed();
            
            // Delayed destruction to allow effects to play
            Invoke(nameof(DestroyStructure), 0.5f);
        }
        
        [Server]
        private void DestroyStructure()
        {
            // Return to pool if available, otherwise destroy
            if (NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.Release(gameObject);
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
        
        [ClientRpc]
        private void RpcOnHitVfx(Vector3 hitPoint)
        {
            // Play hit effects (sparks, dust, etc.)
            // TODO: Implement hit VFX
        }
        
        [ClientRpc]
        private void RpcOnDestroyed()
        {
            OnDestroyedEvent?.Invoke();
            
            // Hide renderers
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.enabled = false;
                }
            }
        }
        
        private void OnDestroy()
        {
            CancelInvoke(nameof(DestroyStructure));
            
            if (healthComponent != null)
            {
                healthComponent.OnDeathEvent -= OnStructureDestroyed;
            }
        }
        
        // Public getters
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public float GetHealthPercentage() => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
}

