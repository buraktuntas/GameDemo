using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Component for throwable items that can be thrown and activated
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableItem : NetworkBehaviour
    {
        [Header("Throwable Settings")]
        [SerializeField] private ThrowableType type;
        // ✅ REMOVED: impactForce - not currently used (future feature for physics impact)

        private ulong throwerId;
        private ThrowableSystem throwableSystem;
        private bool hasActivated = false;

        public void Initialize(ThrowableType throwableType, ulong thrower, ThrowableSystem system)
        {
            type = throwableType;
            throwerId = thrower;
            throwableSystem = system;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isServer || hasActivated)
                return;

            // Activate on impact
            hasActivated = true;
            
            if (throwableSystem != null)
            {
                throwableSystem.OnThrowableActivated(netId, transform.position, type);
            }

            // Destroy throwable object
            NetworkServer.Destroy(gameObject);
        }

        // For sticky bomb - stick to surfaces
        private void OnTriggerEnter(Collider other)
        {
            if (!isServer || hasActivated || type != ThrowableType.StickyBomb)
                return;

            // Stick to surface
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            transform.SetParent(other.transform);
        }
    }
}

