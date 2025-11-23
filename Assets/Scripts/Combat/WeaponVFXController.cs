using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TacticalCombat.Effects; // ✅ FIX: Required for AutoDestroy

namespace TacticalCombat.Combat
{
    public class WeaponVFXController : MonoBehaviour
    {
        [Header("🎨 VISUAL EFFECTS")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private GameObject bloodEffectPrefab;
        [SerializeField] private GameObject metalSparksPrefab;

        // Object pooling
        private Queue<GameObject> muzzleFlashPool = new Queue<GameObject>();
        private Queue<GameObject> hitEffectPool = new Queue<GameObject>();
        private const int POOL_SIZE = 25; // ✅ FIX: Increased from 10 to 25 for rapid-fire weapons

        private void Awake()
        {
            InitializeObjectPools();
        }

        private void InitializeObjectPools()
        {
            // Muzzle flash pool - no parenting to avoid transform sync overhead
            if (muzzleFlashPrefab != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    GameObject flash = Instantiate(muzzleFlashPrefab);
                    flash.SetActive(false);
                    // ✅ FIX: REMOVED AutoDestroy - it conflicts with pooling (destroys objects instead of returning to pool)
                    // AutoDestroy causes memory leak as objects are destroyed instead of being reused
                    muzzleFlashPool.Enqueue(flash);
                }
                Debug.Log($"✅ [WeaponVFXController] Initialized muzzle flash pool with {POOL_SIZE} objects");
            }
            else
            {
                Debug.LogWarning("⚠️ [WeaponVFXController] Muzzle Flash Prefab is NULL! Pool not initialized.");
            }
            
            // Hit effect pool - no parenting
            if (hitEffectPrefab != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    GameObject effect = Instantiate(hitEffectPrefab);
                    effect.SetActive(false);
                    // ✅ FIX: REMOVED AutoDestroy - same reason as muzzle flash pool
                    hitEffectPool.Enqueue(effect);
                }
                Debug.Log($"✅ [WeaponVFXController] Initialized hit effect pool with {POOL_SIZE} objects");
            }
        }

        public void PlayMuzzleFlashAt(Vector3 position, Vector3 direction)
        {
            if (muzzleFlashPrefab == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [WeaponVFXController] Muzzle Flash Prefab is missing! Cannot play muzzle flash.");
                #endif
                return;
            }
            
            // ✅ FIX: Ensure pool is initialized (in case Awake wasn't called)
            if (muzzleFlashPool.Count == 0 && muzzleFlashPrefab != null)
            {
                Debug.LogWarning("⚠️ [WeaponVFXController] Pool not initialized! Initializing now...");
                InitializeObjectPools();
            }
            
            GameObject flash = GetPooledMuzzleFlash();
            if (flash != null)
            {
                flash.transform.position = position;
                flash.transform.rotation = Quaternion.LookRotation(direction);
                flash.SetActive(true);
                
                // Ensure particle system plays
                var ps = flash.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
                
                StartCoroutine(ReturnMuzzleFlashToPool(flash, 0.1f));
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponVFXController] Failed to get muzzle flash from pool! Pool size: {muzzleFlashPool.Count}, Prefab: {(muzzleFlashPrefab != null ? muzzleFlashPrefab.name : "NULL")}");
                #endif
            }
        }

        private GameObject GetPooledMuzzleFlash()
        {
            // Try to get from pool first
            if (muzzleFlashPool.Count > 0)
            {
                return muzzleFlashPool.Dequeue();
            }
            
            // ✅ FIX: Expand pool if needed (dynamic pool expansion)
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab);
                flash.SetActive(false);
                // ✅ FIX: REMOVED AutoDestroy - consistent with pool initialization
                Debug.Log($"⚠️ [WeaponVFXController] Pool exhausted! Created new muzzle flash instance. Consider increasing POOL_SIZE.");
                return flash;
            }
            
            Debug.LogError("❌ [WeaponVFXController] Cannot create muzzle flash: Prefab is NULL!");
            return null;
        }

        private IEnumerator ReturnMuzzleFlashToPool(GameObject flash, float delay)
        {
            yield return new WaitForSeconds(delay);
            // ✅ FIX: Added null check and activeInHierarchy check to prevent errors
            if (flash != null && !flash.activeInHierarchy == false) // Check if object still exists
            {
                flash.SetActive(false);

                // ✅ FIX: Stop all particle systems before returning to pool
                var particleSystems = flash.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }

                muzzleFlashPool.Enqueue(flash);
            }
        }

        public void PlayHitEffect(Vector3 position, Vector3 normal, SurfaceType surface = SurfaceType.Generic)
        {
            GameObject effectPrefab = null;
            
            switch (surface)
            {
                case SurfaceType.Flesh:
                    effectPrefab = bloodEffectPrefab;
                    break;
                case SurfaceType.Metal:
                    effectPrefab = metalSparksPrefab;
                    break;
                case SurfaceType.Wood:
                case SurfaceType.Stone:
                    effectPrefab = bulletHolePrefab;
                    break;
                default:
                    effectPrefab = hitEffectPrefab != null ? hitEffectPrefab : bulletHolePrefab;
                    break;
            }

            if (effectPrefab == null) return;

            // For now, we'll just instantiate if not pooled, or use a simple pool if we want to expand this later.
            // Since we have different prefabs, a single pool won't work unless we have a pool per prefab.
            // For simplicity in this refactor, let's use the generic hitEffectPool for generic hits, 
            // and instantiate others (or expand pooling later).
            
            // Actually, let's try to use the pool if it matches hitEffectPrefab, otherwise instantiate.
            if (effectPrefab == hitEffectPrefab)
            {
                GameObject effect = GetPooledHitEffect();
                if (effect != null)
                {
                    effect.transform.position = position;
                    effect.transform.rotation = Quaternion.LookRotation(normal);
                    effect.SetActive(true);
                    StartCoroutine(ReturnHitEffectToPool(effect, 2f));
                    return;
                }
            }
            
            // Fallback for specific effects (could add pools for these later)
            GameObject instance = Instantiate(effectPrefab, position, Quaternion.LookRotation(normal));
            
            // ✅ FIX: Use AutoDestroyVFX component to ensure cleanup even if controller is disabled
            var autoDestroy = instance.GetComponent<AutoDestroyVFX>();
            if (autoDestroy == null)
            {
                autoDestroy = instance.AddComponent<AutoDestroyVFX>();
            }
            autoDestroy.delay = 2f;
        }

        // Removed DestroyAfterDelay as it causes leaks if controller is disabled
        /*
        private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        */

        private GameObject GetPooledHitEffect()
        {
            if (hitEffectPool.Count > 0)
            {
                return hitEffectPool.Dequeue();
            }
            
            // Expand pool if needed
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab);
                // ✅ FIX: REMOVED AutoDestroy - consistent with pool pattern
                return effect;
            }

            return null;
        }

        private IEnumerator ReturnHitEffectToPool(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            // ✅ FIX: Added null check and particle system cleanup
            if (effect != null && !effect.activeInHierarchy == false)
            {
                // ✅ FIX: Stop all particle systems before returning to pool
                var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }

                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
            }
        }
    }
}
