using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    public class GlueTrap : TrapBase
    {
        [Header("Glue Trap")]
        [SerializeField] private float slowDuration = 3f;
        [SerializeField] private float slowMultiplier = 0.4f;

        private void Awake()
        {
            trapType = TrapType.Static;
        }

        [Server]
        protected override void Trigger(GameObject target)
        {
            // Apply slow effect
            var slowEffect = target.AddComponent<SlowEffect>();
            slowEffect.Initialize(slowDuration, slowMultiplier);

            Debug.Log($"Glue trap slowed {target.name}");

            RpcPlayTriggerEffect();
            MarkAsTriggered();

            // Destroy after duration
            Invoke(nameof(DestroyTrap), slowDuration + 1f);
        }

        [Server]
        private void DestroyTrap()
        {
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcPlayTriggerEffect()
        {
            Debug.Log("Glue trap activated!");
        }
    }

    public class SlowEffect : MonoBehaviour
    {
        private float multiplier;
        private Player.PlayerController player;

        public void Initialize(float dur, float mult)
        {
            multiplier = mult;
            player = GetComponent<Player.PlayerController>();
            Destroy(this, dur);
        }

        private void OnDestroy()
        {
            // Movement speed restored on destroy
        }
    }
}



