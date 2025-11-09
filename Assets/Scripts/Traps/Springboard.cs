using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    public class Springboard : TrapBase
    {
        [Header("Springboard")]
        [SerializeField] private Vector3 launchForce = new Vector3(0, 15, 10);
        [SerializeField] private float resetTime = 2f;

        private void Awake()
        {
            trapType = TrapType.Mechanical;
        }

        [Server]
        public override void Trigger(GameObject target)
        {
            // ✅ CRITICAL FIX: Use TryGetComponent instead of GetComponent (no GC allocation)
            if (!target.TryGetComponent<NetworkIdentity>(out var targetIdentity))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Springboard: Target has no NetworkIdentity");
                #endif
                return;
            }
            
            // Calculate launch force in world space
            Vector3 worldLaunchForce = transform.TransformDirection(launchForce);
            
            // ✅ CRITICAL FIX: Send RPC to launch player
            RpcLaunchPlayer(targetIdentity.netId, worldLaunchForce);

            RpcPlayTriggerEffect();

            // Springboard can be reused after cooldown
            isTriggered = true;
            
            // ✅ HIGH PRIORITY FIX: Use coroutine instead of Invoke (prevents leaks)
            StartCoroutine(ResetTrapAfterDelay(resetTime));
        }
        
        private System.Collections.IEnumerator ResetTrapAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetTrap();
        }

        [Server]
        private void ResetTrap()
        {
            isTriggered = false;
            RpcOnTrapReset(); // ✅ MEDIUM PRIORITY FIX: Notify clients of reset
        }
        
        [ClientRpc]
        private void RpcOnTrapReset()
        {
            // Visual feedback (particles, animation, etc.)
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Springboard reset");
            #endif
        }

        [ClientRpc]
        private void RpcLaunchPlayer(uint targetNetId, Vector3 force)
        {
            // ✅ CRITICAL FIX: Find target and apply impulse
            if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
                return;
            
            // ✅ CRITICAL FIX: Use TryGetComponent and FPSController.ApplyImpulse
            if (identity.TryGetComponent<Player.FPSController>(out var fpsController))
            {
                fpsController.ApplyImpulse(force);
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"🚀 Springboard launched player with force {force}");
                #endif
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Springboard: FPSController not found on target");
                #endif
            }
        }

        [ClientRpc]
        private void RpcPlayTriggerEffect()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Springboard activated!");
            #endif
        }
    }
}



