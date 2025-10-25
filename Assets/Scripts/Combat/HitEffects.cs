using UnityEngine;
using System.Collections;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Hit effects system for combat feedback
    /// </summary>
    public class HitEffects : MonoBehaviour
    {
        [Header("Particle Effects")]
        public ParticleSystem hitParticle;
        public ParticleSystem bloodParticle;
        public ParticleSystem sparkParticle;
        
        [Header("Screen Effects")]
        public GameObject hitScreenEffect;
        public float screenShakeIntensity = 0.5f;
        public float screenShakeDuration = 0.2f;
        
        [Header("Audio")]
        public AudioClip hitSound;
        public AudioClip criticalHitSound;
        public AudioClip headshotSound;
        
        [Header("Settings")]
        public float effectDuration = 2f;
        public bool useScreenShake = true;
        public bool useHitEffects = true;
        
        private Camera playerCamera;
        private AudioSource audioSource;
        private static HitEffects instance;
        
        public static HitEffects Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<HitEffects>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("HitEffects");
                        instance = go.AddComponent<HitEffects>();
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
                InitializeHitEffects();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeHitEffects()
        {
            // Audio source ekle
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Player camera'yı bul
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
            
            // Default particle effects oluştur
            CreateDefaultParticleEffects();
            
            Debug.Log("✅ Hit Effects system initialized");
        }
        
        private void CreateDefaultParticleEffects()
        {
            // Hit particle effect
            if (hitParticle == null)
            {
                GameObject hitGO = new GameObject("HitParticle");
                hitGO.transform.SetParent(transform);
                hitParticle = hitGO.AddComponent<ParticleSystem>();
                
                var main = hitParticle.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 5f;
                main.startSize = 0.1f;
                main.startColor = Color.yellow;
                main.maxParticles = 20;
                
                var emission = hitParticle.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, 15)
                });
            }
            
            // Blood particle effect
            if (bloodParticle == null)
            {
                GameObject bloodGO = new GameObject("BloodParticle");
                bloodGO.transform.SetParent(transform);
                bloodParticle = bloodGO.AddComponent<ParticleSystem>();
                
                var main = bloodParticle.main;
                main.startLifetime = 1f;
                main.startSpeed = 3f;
                main.startSize = 0.05f;
                main.startColor = Color.red;
                main.maxParticles = 30;
                
                var emission = bloodParticle.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, 25)
                });
            }
            
            // Spark particle effect
            if (sparkParticle == null)
            {
                GameObject sparkGO = new GameObject("SparkParticle");
                sparkGO.transform.SetParent(transform);
                sparkParticle = sparkGO.AddComponent<ParticleSystem>();
                
                var main = sparkParticle.main;
                main.startLifetime = 0.3f;
                main.startSpeed = 8f;
                main.startSize = 0.02f;
                main.startColor = Color.white;
                main.maxParticles = 50;
                
                var emission = sparkParticle.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, 40)
                });
            }
        }
        
        public void PlayHitEffect(Vector3 hitPosition, HitType hitType = HitType.Normal)
        {
            if (!useHitEffects) return;
            
            StartCoroutine(PlayHitEffectCoroutine(hitPosition, hitType));
        }
        
        private IEnumerator PlayHitEffectCoroutine(Vector3 hitPosition, HitType hitType)
        {
            // Particle effect seç
            ParticleSystem selectedParticle = null;
            AudioClip selectedSound = null;
            
            switch (hitType)
            {
                case HitType.Normal:
                    selectedParticle = hitParticle;
                    selectedSound = hitSound;
                    break;
                case HitType.Critical:
                    selectedParticle = bloodParticle;
                    selectedSound = criticalHitSound;
                    break;
                case HitType.Headshot:
                    selectedParticle = bloodParticle;
                    selectedSound = headshotSound;
                    break;
                case HitType.Metal:
                    selectedParticle = sparkParticle;
                    selectedSound = hitSound;
                    break;
            }
            
            // Particle effect oynat
            if (selectedParticle != null)
            {
                selectedParticle.transform.position = hitPosition;
                selectedParticle.Play();
            }
            
            // Ses oynat
            if (selectedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(selectedSound);
            }
            
            // Screen shake
            if (useScreenShake && playerCamera != null)
            {
                StartCoroutine(ScreenShakeCoroutine());
            }
            
            // Hit screen effect
            if (hitScreenEffect != null)
            {
                StartCoroutine(ShowHitScreenEffect());
            }
            
            yield return new WaitForSeconds(effectDuration);
        }
        
        private IEnumerator ScreenShakeCoroutine()
        {
            if (playerCamera == null) yield break;
            
            Vector3 originalPosition = playerCamera.transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < screenShakeDuration)
            {
                float x = Random.Range(-1f, 1f) * screenShakeIntensity;
                float y = Random.Range(-1f, 1f) * screenShakeIntensity;
                
                playerCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            playerCamera.transform.localPosition = originalPosition;
        }
        
        private IEnumerator ShowHitScreenEffect()
        {
            if (hitScreenEffect == null) yield break;
            
            hitScreenEffect.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            hitScreenEffect.SetActive(false);
        }
        
        // Utility methods
        public void PlayHitEffectAtTransform(Transform target, HitType hitType = HitType.Normal)
        {
            if (target != null)
            {
                PlayHitEffect(target.position, hitType);
            }
        }
        
        public void PlayHitEffectAtPlayer(Transform player, HitType hitType = HitType.Normal)
        {
            if (player != null)
            {
                Vector3 headPosition = player.position + Vector3.up * 1.8f;
                PlayHitEffect(headPosition, hitType);
            }
        }
    }
    
    public enum HitType
    {
        Normal,
        Critical,
        Headshot,
        Metal
    }
}
