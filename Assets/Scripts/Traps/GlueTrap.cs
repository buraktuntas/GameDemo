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

    // Slow effect component
    public class SlowEffect : MonoBehaviour
    {
        private float duration;
        private float multiplier;
        private float startTime;
        private Player.PlayerController player;

        public void Initialize(float dur, float mult)
        {
            duration = dur;
            multiplier = mult;
            startTime = Time.time;
            player = GetComponent<Player.PlayerController>();
        }

        private void Update()
        {
            if (Time.time >= startTime + duration)
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            // Movement speed is handled via PlayerController
            // This would require modifying PlayerController to check for SlowEffect
        }
    }
}



