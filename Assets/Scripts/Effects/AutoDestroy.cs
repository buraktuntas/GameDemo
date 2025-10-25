using UnityEngine;

namespace TacticalCombat.Effects
{
    /// <summary>
    /// Automatically destroys the GameObject after a specified lifetime
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [Header("Auto Destroy Settings")]
        [Tooltip("Lifetime in seconds before destroying this object")]
        public float lifetime = 1f;
        
        [Header("Options")]
        [Tooltip("Fade out before destroying")]
        public bool fadeOut = false;
        
        [Tooltip("Fade out duration")]
        public float fadeDuration = 0.5f;
        
        private float startTime;
        private Renderer[] renderers;
        private ParticleSystem[] particles;
        
        private void Start()
        {
            startTime = Time.time;
            
            // Get components for fade out
            if (fadeOut)
            {
                renderers = GetComponentsInChildren<Renderer>();
                particles = GetComponentsInChildren<ParticleSystem>();
            }
            
            // Start destroy timer
            Invoke(nameof(DestroyObject), lifetime);
        }
        
        private void Update()
        {
            if (fadeOut && renderers != null)
            {
                float elapsed = Time.time - startTime;
                float fadeStart = lifetime - fadeDuration;
                
                if (elapsed >= fadeStart)
                {
                    float fadeProgress = (elapsed - fadeStart) / fadeDuration;
                    float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                    
                    foreach (var renderer in renderers)
                    {
                        if (renderer.material.HasProperty("_Color"))
                        {
                            Color color = renderer.material.color;
                            color.a = alpha;
                            renderer.material.color = color;
                        }
                    }
                }
            }
        }
        
        private void DestroyObject()
        {
            // Stop particles
            if (particles != null)
            {
                foreach (var ps in particles)
                {
                    if (ps != null)
                    {
                        ps.Stop();
                    }
                }
            }
            
            // Destroy object
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Manually destroy the object
        /// </summary>
        public void DestroyNow()
        {
            DestroyObject();
        }
        
        /// <summary>
        /// Reset the lifetime timer
        /// </summary>
        public void ResetTimer()
        {
            CancelInvoke(nameof(DestroyObject));
            startTime = Time.time;
            Invoke(nameof(DestroyObject), lifetime);
        }
    }
}
