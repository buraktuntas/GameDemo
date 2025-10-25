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
            var health = target.GetComponent<Combat.Health>();
            if (health != null && !health.IsDead())
            {
                health.TakeDamage(damage);
                Debug.Log($"Spike trap dealt {damage} damage to {target.name}");
            }

            RpcPlayTriggerEffect();
            MarkAsTriggered();

            // Destroy trap after use (one-time)
            Invoke(nameof(DestroyTrap), 2f);
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
            Debug.Log("Spike trap triggered!");
        }
    }
}



