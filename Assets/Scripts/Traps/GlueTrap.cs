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
        public override void Trigger(GameObject target)
        {
            // ✅ CRITICAL FIX: Apply slow effect
            var slowEffect = target.AddComponent<SlowEffect>();
            slowEffect.Initialize(slowDuration, slowMultiplier);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Glue trap slowed {target.name}");
            #endif

            RpcPlayTriggerEffect();
            MarkAsTriggered();

            // ✅ HIGH PRIORITY FIX: Use coroutine instead of Invoke (prevents leaks)
            StartCoroutine(DestroyTrapAfterDelay(slowDuration + 1f));
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
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Glue trap activated!");
            #endif
        }
    }

    /// <summary>
    /// ✅ CRITICAL FIX: SlowEffect now properly modifies movement speed
    /// </summary>
    public class SlowEffect : MonoBehaviour
    {
        private float multiplier;
        private Player.FPSController fpsController;

        public void Initialize(float dur, float mult)
        {
            multiplier = mult;
            
            // ✅ CRITICAL FIX: Use TryGetComponent instead of GetComponent (no GC allocation)
            if (!TryGetComponent<Player.FPSController>(out fpsController))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("SlowEffect: FPSController not found!");
                #endif
                Destroy(this);
                return;
            }
            
            // ✅ CRITICAL FIX: Apply slow multiplier
            fpsController.speedMultiplier *= multiplier;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"SlowEffect applied: {multiplier}x speed for {dur}s");
            #endif
            
            Destroy(this, dur);
        }

        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Restore movement speed
            if (fpsController != null)
            {
                fpsController.speedMultiplier /= multiplier;
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"SlowEffect removed: speed restored");
                #endif
            }
        }
    }
}



