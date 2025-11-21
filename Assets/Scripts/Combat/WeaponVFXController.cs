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
        private const int POOL_SIZE = 10;

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
                    if (flash.GetComponent<AutoDestroy>() == null)
                    {
                        flash.AddComponent<AutoDestroy>();
                    }
                    muzzleFlashPool.Enqueue(flash);
                }
            }
            
            // Hit effect pool - no parenting
            if (hitEffectPrefab != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    GameObject effect = Instantiate(hitEffectPrefab);
                    effect.SetActive(false);
                    if (effect.GetComponent<AutoDestroy>() == null)
                    {
                        effect.AddComponent<AutoDestroy>();
                    }
                    hitEffectPool.Enqueue(effect);
                }
            }
        }

        public void PlayMuzzleFlashAt(Vector3 position, Vector3 direction)
        {
            if (muzzleFlashPrefab == null) return;
            
            GameObject flash = GetPooledMuzzleFlash();
            if (flash != null)
            {
                flash.transform.position = position;
                flash.transform.rotation = Quaternion.LookRotation(direction);
                flash.SetActive(true);
                
                StartCoroutine(ReturnMuzzleFlashToPool(flash, 0.1f));
            }
        }

        private GameObject GetPooledMuzzleFlash()
        {
            if (muzzleFlashPool.Count > 0)
            {
                return muzzleFlashPool.Dequeue();
            }
            
            // Expand pool if needed
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab);
                if (flash.GetComponent<AutoDestroy>() == null)
                {
                    flash.AddComponent<AutoDestroy>();
                }
                return flash;
            }
            
            return null;
        }

        private IEnumerator ReturnMuzzleFlashToPool(GameObject flash, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (flash != null)
            {
                flash.SetActive(false);
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
            if (instance.GetComponent<AutoDestroy>() == null) instance.AddComponent<AutoDestroy>();
        }

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
                if (effect.GetComponent<AutoDestroy>() == null)
                {
                    effect.AddComponent<AutoDestroy>();
                }
                return effect;
            }
            
            return null;
        }

        private IEnumerator ReturnHitEffectToPool(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
            }
        }
    }
}
