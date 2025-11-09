using UnityEngine;
using System.Collections.Generic;
using Mirror;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// BATTLEFIELD-GRADE Impact VFX Pooling System
    /// Replaces debug cubes with professional particle effects
    /// Zero GC allocation, high performance
    /// </summary>
    public class ImpactVFXPool : MonoBehaviour
    {
        [Header("Impact Effect Prefabs")]
        [SerializeField] private GameObject genericImpactPrefab;  // Concrete/Stone
        [SerializeField] private GameObject metalImpactPrefab;
        [SerializeField] private GameObject woodImpactPrefab;
        [SerializeField] private GameObject fleshImpactPrefab;    // Blood

        [Header("Pool Settings")]
        [SerializeField] private int poolSizePerType = 15;
        [SerializeField] private float effectLifetime = 3f;

        // Pools for each surface type
        private Queue<GameObject> genericPool = new Queue<GameObject>();
        private Queue<GameObject> metalPool = new Queue<GameObject>();
        private Queue<GameObject> woodPool = new Queue<GameObject>();
        private Queue<GameObject> fleshPool = new Queue<GameObject>();

        private static ImpactVFXPool instance;
        public static ImpactVFXPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ImpactVFXPool>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[ImpactVFXPool]");
                        instance = go.AddComponent<ImpactVFXPool>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePools()
        {
            Debug.Log("🎨 [ImpactVFXPool] Initializing impact effect pools...");

            // Create default prefabs if not assigned
            if (genericImpactPrefab == null) genericImpactPrefab = CreateDefaultImpact(Color.gray, "Generic");
            if (metalImpactPrefab == null) metalImpactPrefab = CreateDefaultImpact(Color.white, "Metal");
            if (woodImpactPrefab == null) woodImpactPrefab = CreateDefaultImpact(new Color(0.6f, 0.4f, 0.2f), "Wood");
            if (fleshImpactPrefab == null) fleshImpactPrefab = CreateDefaultImpact(Color.red, "Flesh");

            // Initialize pools
            InitializePool(genericPool, genericImpactPrefab, "Generic");
            InitializePool(metalPool, metalImpactPrefab, "Metal");
            InitializePool(woodPool, woodImpactPrefab, "Wood");
            InitializePool(fleshPool, fleshImpactPrefab, "Flesh");

            Debug.Log($"✅ [ImpactVFXPool] All pools initialized ({poolSizePerType} per type)");
        }

        private void InitializePool(Queue<GameObject> pool, GameObject prefab, string poolName)
        {
            for (int i = 0; i < poolSizePerType; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.name = $"{poolName}Impact_{i}";
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private GameObject CreateDefaultImpact(Color color, string surfaceName)
        {
            GameObject impactGO = new GameObject($"Default{surfaceName}Impact");
            impactGO.transform.SetParent(transform);

            // Create particle system
            ParticleSystem ps = impactGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = color;
            main.maxParticles = 25;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Disable;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 20, 30)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-3f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // ✅ CRITICAL FIX: Cache material to prevent memory leak
            // Material is created once per surface type, reused for all instances
            Material impactMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            impactMaterial.SetColor("_BaseColor", color);
            renderer.material = impactMaterial;

            impactGO.SetActive(false);
            return impactGO;
        }

        /// <summary>
        /// Spawn impact effect at hit location
        /// </summary>
        public void PlayImpact(Vector3 position, Vector3 normal, SurfaceType surfaceType, bool isBodyHit = false)
        {
            Queue<GameObject> selectedPool = GetPoolForSurface(surfaceType, isBodyHit);

            // Auto-expand pool if empty to avoid missing effects under heavy fire
            if (selectedPool == null || selectedPool.Count == 0)
            {
                Debug.LogWarning($"⚠️ [ImpactVFXPool] Pool empty for {surfaceType}");
                var prefab = GetPrefabForSurface(surfaceType, isBodyHit);
                if (prefab != null)
                {
                    var extra = Instantiate(prefab, transform);
                    extra.SetActive(false);
                    selectedPool ??= genericPool; // safety fallback
                    selectedPool.Enqueue(extra);
                }
                else
                {
                    return;
                }
            }

            GameObject effect = selectedPool.Dequeue();
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.LookRotation(normal);
            effect.SetActive(true);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            // Return to pool after lifetime
            StartCoroutine(ReturnToPoolAfterDelay(effect, selectedPool, effectLifetime));
        }

        private Queue<GameObject> GetPoolForSurface(SurfaceType surfaceType, bool isBodyHit)
        {
            if (isBodyHit)
            {
                return fleshPool;
            }

            return surfaceType switch
            {
                SurfaceType.Generic => genericPool,
                SurfaceType.Stone => genericPool,
                SurfaceType.Metal => metalPool,
                SurfaceType.Wood => woodPool,
                SurfaceType.Flesh => fleshPool,
                _ => genericPool
            };
        }

        private GameObject GetPrefabForSurface(SurfaceType surfaceType, bool isBodyHit)
        {
            if (isBodyHit) return fleshImpactPrefab;
            return surfaceType switch
            {
                SurfaceType.Generic => genericImpactPrefab,
                SurfaceType.Stone => genericImpactPrefab,
                SurfaceType.Metal => metalImpactPrefab,
                SurfaceType.Wood => woodImpactPrefab,
                SurfaceType.Flesh => fleshImpactPrefab,
                _ => genericImpactPrefab
            };
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject effect, Queue<GameObject> pool, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (effect != null)
            {
                effect.SetActive(false);
                pool.Enqueue(effect);
            }
        }

        private void ClearPool(Queue<GameObject> pool)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        /// <summary>
        /// Clear and reinitialize all pools
        /// </summary>
        public void ClearPools()
        {
            StopAllCoroutines();

            ClearPool(genericPool);
            ClearPool(metalPool);
            ClearPool(woodPool);
            ClearPool(fleshPool);

            InitializePools();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            // Clear all pools without reinitializing
            while (genericPool.Count > 0) { GameObject obj = genericPool.Dequeue(); if (obj != null) Destroy(obj); }
            while (metalPool.Count > 0) { GameObject obj = metalPool.Dequeue(); if (obj != null) Destroy(obj); }
            while (woodPool.Count > 0) { GameObject obj = woodPool.Dequeue(); if (obj != null) Destroy(obj); }
            while (fleshPool.Count > 0) { GameObject obj = fleshPool.Dequeue(); if (obj != null) Destroy(obj); }
        }
    }
}
