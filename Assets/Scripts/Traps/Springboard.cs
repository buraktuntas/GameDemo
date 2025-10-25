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
        protected override void Trigger(GameObject target)
        {
            // Launch the player
            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                // Apply impulse - this would need to be integrated with PlayerController
                RpcLaunchPlayer(target.GetComponent<NetworkIdentity>().netId, launchForce);
            }

            RpcPlayTriggerEffect();

            // Springboard can be reused after cooldown
            isTriggered = true;
            Invoke(nameof(ResetTrap), resetTime);
        }

        [Server]
        private void ResetTrap()
        {
            isTriggered = false;
        }

        [ClientRpc]
        private void RpcLaunchPlayer(uint targetNetId, Vector3 force)
        {
            // Find the target and apply force
            if (NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
            {
                var player = identity.GetComponent<Player.PlayerController>();
                if (player != null)
                {
                    // This would require adding a LaunchPlayer method to PlayerController
                    Debug.Log($"Launching player with force {force}");
                }
            }
        }

        [ClientRpc]
        private void RpcPlayTriggerEffect()
        {
            Debug.Log("Springboard activated!");
        }
    }
}



