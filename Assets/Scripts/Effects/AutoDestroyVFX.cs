using UnityEngine;

namespace TacticalCombat.Effects
{
    public class AutoDestroyVFX : MonoBehaviour
    {
        public float delay = 2f;

        private void OnEnable()
        {
            Invoke(nameof(DestroySelf), delay);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
        
        private void OnDisable()
        {
            CancelInvoke();
        }
    }
}
