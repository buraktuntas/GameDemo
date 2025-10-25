using UnityEngine;
using System.Collections.Generic;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// COMBAT MANAGER - Scene Singleton
    /// Manages all combat effects, sounds, and feedback
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        private static CombatManager _instance;
        public static CombatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CombatManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CombatManager]");
                        _instance = go.AddComponent<CombatManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("🎨 EFFECT PREFABS")]
        public GameObject muzzleFlashPrefab;
        public GameObject bulletHolePrefab;
        public GameObject bloodEffectPrefab;
        public GameObject metalSparksPrefab;
        public GameObject woodChipsPrefab;
        public GameObject stoneDebrisPrefab;
        
        [Header("🔊 AUDIO CLIPS")]
        public AudioClip[] gunshotSounds;
        public AudioClip[] reloadSounds;
        public AudioClip[] hitFleshSounds;
        public AudioClip[] hitMetalSounds;
        public AudioClip[] hitWoodSounds;
        public AudioClip[] hitStoneSounds;
        public AudioClip emptyGunSound;
        
        [Header("📊 POOLS")]
        [SerializeField] private int effectPoolSize = 20;
        
        private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
        private AudioSource audioSource;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            // Add audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            
            // Initialize pools
            InitializePool("MuzzleFlash", muzzleFlashPrefab);
            InitializePool("BulletHole", bulletHolePrefab);
            InitializePool("Blood", bloodEffectPrefab);
            InitializePool("MetalSparks", metalSparksPrefab);
            
            Debug.Log("✅ CombatManager initialized");
        }
        
        private void InitializePool(string poolName, GameObject prefab)
        {
            if (prefab == null) return;
            
            effectPools[poolName] = new Queue<GameObject>();
            
            for (int i = 0; i < effectPoolSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                effectPools[poolName].Enqueue(obj);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // EFFECT SPAWNING
        // ═══════════════════════════════════════════════════════════
        
        public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
        {
            SpawnEffect("MuzzleFlash", position, rotation, 0.1f);
        }
        
        public void SpawnHitEffect(Vector3 position, Vector3 normal, SurfaceType surface)
        {
            string poolName = GetEffectPoolName(surface);
            Quaternion rotation = Quaternion.LookRotation(normal);
            SpawnEffect(poolName, position, rotation, 2f);
        }
        
        private string GetEffectPoolName(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.Flesh => "Blood",
                SurfaceType.Metal => "MetalSparks",
                SurfaceType.Wood => "BulletHole",
                SurfaceType.Stone => "BulletHole",
                _ => "BulletHole"
            };
        }
        
        private void SpawnEffect(string poolName, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (!effectPools.ContainsKey(poolName)) return;
            
            GameObject effect;
            
            if (effectPools[poolName].Count > 0)
            {
                effect = effectPools[poolName].Dequeue();
            }
            else
            {
                // Pool exhausted, create new
                Debug.LogWarning($"Effect pool '{poolName}' exhausted! Creating new instance.");
                return;
            }
            
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.SetActive(true);
            
            // Return to pool after lifetime
            StartCoroutine(ReturnToPool(effect, poolName, lifetime));
        }
        
        private System.Collections.IEnumerator ReturnToPool(GameObject obj, string poolName, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            obj.SetActive(false);
            
            if (effectPools.ContainsKey(poolName))
            {
                effectPools[poolName].Enqueue(obj);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // AUDIO
        // ═══════════════════════════════════════════════════════════
        
        public void PlayGunshotSound(Vector3 position)
        {
            if (gunshotSounds == null || gunshotSounds.Length == 0) return;
            
            AudioClip clip = gunshotSounds[Random.Range(0, gunshotSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, position, 0.5f);
        }
        
        public void PlayHitSound(Vector3 position, SurfaceType surface)
        {
            AudioClip[] clips = GetHitSounds(surface);
            if (clips == null || clips.Length == 0) return;
            
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            AudioSource.PlayClipAtPoint(clip, position, 0.3f);
        }
        
        private AudioClip[] GetHitSounds(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.Flesh => hitFleshSounds,
                SurfaceType.Metal => hitMetalSounds,
                SurfaceType.Wood => hitWoodSounds,
                SurfaceType.Stone => hitStoneSounds,
                _ => null
            };
        }
        
        public void PlayReloadSound()
        {
            if (reloadSounds == null || reloadSounds.Length == 0) return;
            
            AudioClip clip = reloadSounds[Random.Range(0, reloadSounds.Length)];
            audioSource.PlayOneShot(clip, 0.4f);
        }
        
        public void PlayEmptySound()
        {
            if (emptyGunSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(emptyGunSound, 0.5f);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // HIT MARKERS & DAMAGE NUMBERS
        // ═══════════════════════════════════════════════════════════
        
        public void ShowHitMarker(bool isHeadshot = false)
        {
            // TODO: Show UI hit marker
            Debug.Log($"💥 Hit Marker {(isHeadshot ? "(HEADSHOT)" : "")}");
        }
        
        public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (DamageNumbers.Instance != null)
            {
                DamageNumbers.Instance.ShowDamage(worldPosition, damage, isCritical);
            }
        }
    }
}
