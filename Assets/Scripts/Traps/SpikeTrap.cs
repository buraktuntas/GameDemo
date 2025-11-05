using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    public class SpikeTrap : TrapBase
    {
        [Header("Spike Trap")]
        [SerializeField] private int damage = GameConstants.SPIKE_TRAP_DAMAGE;
        [SerializeField] private GameObject spikesVisual;

        private void Awake()
        {
            trapType = TrapType.Static;
        }

        [Server]
        protected override void Trigger(GameObject target)
        {
            // ✅ CRITICAL FIX: Use TryGetComponent instead of GetComponent (no GC allocation)
            if (target.TryGetComponent<Combat.Health>(out var health) && !health.IsDead())
            {
                health.TakeDamage(damage);
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"Spike trap dealt {damage} damage to {target.name}");
                #endif
            }

            RpcPlayTriggerEffect();
            MarkAsTriggered();

            // ✅ HIGH PRIORITY FIX: Use coroutine instead of Invoke (prevents leaks)
            StartCoroutine(DestroyTrapAfterDelay(2f));
        }
        
        private System.Collections.IEnumerator DestroyTrapAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            NetworkServer.Destroy(gameObject);
        }

        [Server]
        private void DestroyTrap()
        {
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcPlayTriggerEffect()
        {
            // Animate spikes
            if (spikesVisual != null)
            {
                spikesVisual.SetActive(true);
            }
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Spike trap triggered!");
            #endif
        }
    }
}



