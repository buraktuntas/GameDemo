using UnityEngine;

namespace TacticalCombat.Effects
{
    public class AutoDestroy : MonoBehaviour
    {
        [Header("Auto Destroy Settings")]
        public float lifetime = 2f;
        public bool destroyOnStart = true;

        private void Start()
        {
            if (destroyOnStart)
            {
                Destroy(gameObject, lifetime);
            }
        }

        public void DestroyAfter(float delay)
        {
            Destroy(gameObject, delay);
        }
    }
}