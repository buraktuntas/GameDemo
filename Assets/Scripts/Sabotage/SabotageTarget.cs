using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Sabotage
{
    public class SabotageTarget : NetworkBehaviour
    {
        [Header("Sabotage Settings")]
        [SyncVar]
        private bool isDisabled = false;

        [SyncVar]
        private Team ownerTeam;
        
        public System.Action<bool> OnSabotageStateChanged;

        public void Initialize(Team team)
        {
            ownerTeam = team;
        }

        [Server]
        public void Disable(float duration)
        {
            if (isDisabled) return;

            isDisabled = true;
            Debug.Log($"{gameObject.name} disabled by sabotage");

            // Disable functionality
            DisableFunctionality();

            RpcOnDisabled();

            // Re-enable after duration
            Invoke(nameof(Enable), duration);
        }

        [Server]
        private void Enable()
        {
            isDisabled = false;
            EnableFunctionality();
            RpcOnEnabled();
        }

        private void DisableFunctionality()
        {
            // Disable trap
            var trap = GetComponent<Traps.TrapBase>();
            if (trap != null)
            {
                trap.enabled = false;
            }

            // Or reduce structure health
            var structure = GetComponent<Building.Structure>();
            if (structure != null)
            {
                var health = GetComponent<Combat.Health>();
                if (health != null)
                {
                    health.TakeDamage(health.GetCurrentHealth() / 2);
                }
            }
        }

        private void EnableFunctionality()
        {
            var trap = GetComponent<Traps.TrapBase>();
            if (trap != null)
            {
                trap.enabled = true;
            }
        }

        [ClientRpc]
        private void RpcOnDisabled()
        {
            OnSabotageStateChanged?.Invoke(true);
            Debug.Log("Target sabotaged!");
        }

        [ClientRpc]
        private void RpcOnEnabled()
        {
            OnSabotageStateChanged?.Invoke(false);
        }

        public bool IsDisabled() => isDisabled;
        public Team GetOwnerTeam() => ownerTeam;
        public bool CanBeSabotaged(Team sabotagingTeam)
        {
            return !isDisabled && ownerTeam != sabotagingTeam;
        }

        // ✅ CRITICAL FIX: Cancel Invoke on destroy to prevent memory leaks
        private void OnDestroy()
        {
            CancelInvoke(nameof(Enable)); // Cancel Enable Invoke if object destroyed before duration
        }
    }
}

