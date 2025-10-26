using UnityEngine;

namespace TacticalCombat.Effects
{
    /// <summary>
    /// Simple script to automatically destroy GameObjects after a set lifetime
    /// Used for effects like muzzle flash and hit effects
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [Header("Auto Destroy Settings")]
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private bool destroyOnStart = true;
        
        private void Start()
        {
            if (destroyOnStart)
            {
                Destroy(gameObject, lifetime);
            }
        }
        
        /// <summary>
        /// Manually trigger destruction
        /// </summary>
        public void DestroyNow()
        {
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Destroy after specified time
        /// </summary>
        public void DestroyAfter(float time)
        {
            Destroy(gameObject, time);
        }
    }
}