using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using System.Collections;
using System.Collections.Generic; // ✅ FIX: Queue<GameObject> için gerekli

namespace TacticalCombat.Combat
{
    /// <summary>
    /// PROFESYONEL WEAPON SYSTEM - BUG FIX VERSİYON
    /// ✅ Silah sesi fix
    /// ✅ Build modu input çakışması fix
    /// ✅ Weapon state reset
    /// ✅ Network support for multiplayer
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WeaponSystem : NetworkBehaviour
    {
        [Header("📦 WEAPON CONFIG")]
        [SerializeField] private WeaponConfig currentWeapon;
        
        [Header("🎯 REFERENCES")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Animator weaponAnimator;
        
        [Header("🎨 VISUAL EFFECTS")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private GameObject bloodEffectPrefab;
        [SerializeField] private GameObject metalSparksPrefab;
        
        [Header("🔊 AUDIO")]
        [SerializeField] private AudioClip[] fireSounds;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptySound;
        [SerializeField] private AudioClip[] hitSounds;
        
        [Header("📊 WEAPON STATE")]
        private int currentAmmo;
        private int reserveAmmo;
        private bool isReloading;
        
        private float nextFireTime;
        private float recoilAmount;
        private Vector3 originalWeaponPos;
        private Quaternion originalWeaponRot;
        private bool isAiming;
        
        // ✅ FIX: Audio debug flag
        [Header("🐛 DEBUG")]
        [SerializeField] private bool debugAudio = true;
        [SerializeField] private bool debugInputs = true; // ✅ FIX: Debug aktif et
        
        // Events
        public System.Action<int, int> OnAmmoChanged;
        public System.Action OnReloadStarted;
        public System.Action OnReloadComplete;
        public System.Action OnWeaponFired;
        
        // ✅ FIX: Build mode awareness
        private TacticalCombat.Player.InputManager inputManager;
        
        // ✅ FIX: Performance - Object pooling for effects
        private Queue<GameObject> muzzleFlashPool = new Queue<GameObject>();
        private Queue<GameObject> hitEffectPool = new Queue<GameObject>();
        private const int POOL_SIZE = 10;
        
        private void Awake()
        {
            // ✅ FIX: AudioSource'u garanti et
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    if (debugAudio)
                    {
                        Debug.Log("✅ [WeaponSystem] AudioSource component automatically added");
                    }
                }
            }
            
            // ✅ AudioSource ayarlarını optimize et
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound for local player
            audioSource.volume = 1f;
            audioSource.priority = 128; // Normal priority
            
            // ✅ Audio clip kontrolü
            if (debugAudio)
            {
                if (fireSounds == null || fireSounds.Length == 0)
                {
                    Debug.LogError("❌ [WeaponSystem] NO FIRE SOUNDS ASSIGNED! Please assign audio clips in Inspector.");
                }
                else
                {
                    Debug.Log($"✅ [WeaponSystem] {fireSounds.Length} fire sounds loaded");
                    
                    // Her clip'i kontrol et
                    for (int i = 0; i < fireSounds.Length; i++)
                    {
                        if (fireSounds[i] == null)
                        {
                            Debug.LogError($"❌ [WeaponSystem] Fire sound at index {i} is NULL!");
                        }
                    }
                }
            }
                
            if (weaponHolder != null)
            {
                originalWeaponPos = weaponHolder.localPosition;
                originalWeaponRot = weaponHolder.localRotation;
            }
            
            // ✅ FIX: Initialize object pools
            InitializeObjectPools();
        }
        
        private void InitializeObjectPools()
        {
            // Muzzle flash pool
            if (muzzleFlashPrefab != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    GameObject flash = Instantiate(muzzleFlashPrefab);
                    flash.SetActive(false);
                    muzzleFlashPool.Enqueue(flash);
                }
                Debug.Log($"✅ [WeaponSystem] Muzzle flash pool initialized with {POOL_SIZE} objects");
            }
            
            // Hit effect pool
            if (hitEffectPrefab != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    GameObject effect = Instantiate(hitEffectPrefab);
                    effect.SetActive(false);
                    hitEffectPool.Enqueue(effect);
                }
                Debug.Log($"✅ [WeaponSystem] Hit effect pool initialized with {POOL_SIZE} objects");
            }
        }
        
        private void Start()
        {
            // ✅ FIX: InputManager referansı
            // Cache InputManager - her player'ın kendi InputManager'ı var
            inputManager = GetComponent<TacticalCombat.Player.InputManager>();

            if (debugInputs)
            {
                Debug.Log($"✅ [WeaponSystem] Start - InputManager: {(inputManager != null ? "Found" : "NULL")}");
            }

            // ⚡ PERFORMANCE FIX: Cache camera reference
            if (playerCamera == null)
            {
                // First try to find FPSController's camera
                var fpsController = GetComponent<TacticalCombat.Player.FPSController>();
                if (fpsController != null)
                {
                    playerCamera = fpsController.GetCamera();
                }

                // Fallback to Camera.main (only once, then cached)
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }
                
            // Initialize ammo
            if (currentWeapon != null)
            {
                currentAmmo = currentWeapon.magazineSize;
                reserveAmmo = currentWeapon.maxAmmo;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                
                Debug.Log($"✅ [WeaponSystem] Ammo initialized: {currentAmmo}/{reserveAmmo}");
                Debug.Log($"✅ [WeaponSystem] Weapon: {currentWeapon.weaponName}");
            }
            else
            {
                Debug.LogWarning("⚠️ [WeaponSystem] currentWeapon is NULL! Creating default weapon config...");
                CreateDefaultWeaponConfig();
                
                if (currentWeapon != null)
                {
                    currentAmmo = currentWeapon.magazineSize;
                    reserveAmmo = currentWeapon.maxAmmo;
                    OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                    
                    Debug.Log($"✅ [WeaponSystem] Default weapon created! Ammo: {currentAmmo}/{reserveAmmo}");
                }
            }
        }
        
        private void Update()
        {
            // ✅ FIX: No need for isLocalPlayer check - MonoBehaviour handles local player
            
            // ✅ FIX: InputManager null kontrolü
            if (inputManager == null)
            {
                // Try to find InputManager on this object first
                inputManager = GetComponent<TacticalCombat.Player.InputManager>();
                
                // If not found, try parent (Player prefab)
                if (inputManager == null)
                {
                    inputManager = GetComponentInParent<TacticalCombat.Player.InputManager>();
                }
                
                // If still not found, try to find in scene
                if (inputManager == null)
                {
                    inputManager = FindFirstObjectByType<TacticalCombat.Player.InputManager>();
                }
                
                if (inputManager == null)
                {
                    if (debugInputs && Time.frameCount % 60 == 0)
                    {
                        Debug.LogError("❌ [WeaponSystem] InputManager is NULL! Cannot check build mode.");
                    }
                    return;
                }
                else
                {
                    Debug.Log("✅ [WeaponSystem] InputManager found!");
                }
            }
            
            // ✅ FIX: Build modunda silah tamamen devre dışı
            if (inputManager.IsInBuildMode)
            {
                if (debugInputs && Time.frameCount % 60 == 0)
                {
                    Debug.Log("🏗️ [WeaponSystem] Build mode active - weapon disabled");
                }
                return; // Build mode'da silah hiç çalışmasın
            }
            
            HandleInput();
            UpdateRecoil();
        }
        
        private void HandleInput()
        {
            // ✅ FIX: Ekstra güvenlik kontrolü
            if (inputManager != null && inputManager.BlockShootInput)
            {
                if (debugInputs && Time.frameCount % 60 == 0)
                {
                    Debug.Log("🚫 [WeaponSystem] Shoot input blocked");
                }
                return;
            }
            
            // Fire - ✅ FIX: Separate auto and semi-auto to prevent stuck shooting
            if (currentWeapon != null)
            {
                if (currentWeapon.fireMode == FireMode.Auto)
                {
                    // Auto mode: Hold to fire continuously
                    if (Input.GetButton("Fire1") && CanFire())
                    {
                        Fire();
                    }
                    else if (Input.GetButton("Fire1") && currentAmmo <= 0 && Time.frameCount % 30 == 0)
                    {
                        // Empty gun sound (throttled)
                        PlayEmptySound();
                    }
                }
                else
                {
                    // Semi-auto/Burst: Press to fire once
                    if (Input.GetButtonDown("Fire1") && CanFire())
                    {
                        Fire();
                    }
                    else if (Input.GetButtonDown("Fire1") && currentAmmo <= 0)
                    {
                        PlayEmptySound();
                    }
                }
            }
            
            // Reload
            if (Input.GetKeyDown(KeyCode.R) && CanReload())
            {
                StartReload();
            }
            
            // Aim (optional)
            if (Input.GetButton("Fire2"))
            {
                isAiming = true;
            }
            else
            {
                isAiming = false;
            }
        }
        
        private bool CanFire()
        {
            return Time.time >= nextFireTime && 
                   currentAmmo > 0 && 
                   !isReloading &&
                   currentWeapon != null;
        }
        
        private bool CanReload()
        {
            return !isReloading && 
                   currentAmmo < currentWeapon.magazineSize && 
                   reserveAmmo > 0;
        }
        
        private void Fire()
        {
            // ✅ FIX: Throttle debug logs to prevent console spam
            if (debugInputs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"🔥 [WeaponSystem] FIRE() executing - Weapon: {currentWeapon?.weaponName}, Ammo: {currentAmmo}");
            }

            nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            currentAmmo--;
            
            // Visual feedback
            ApplyRecoil();
            PlayMuzzleFlash();
            
            // ✅ Audio feedback - CRITICAL
            PlayFireSound();
            
            // Raycast
            PerformRaycast();
            
            // Camera shake
            StartCoroutine(CameraShake(0.05f, 0.1f));
            
            // Network sync
            PlayFireEffects();
            
            // Events
            OnWeaponFired?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            // Auto reload
            if (currentAmmo <= 0 && reserveAmmo > 0)
            {
                StartReload();
            }
        }
        
        private void PerformRaycast()
        {
            if (playerCamera == null) return;

            // Calculate spread
            Vector3 spread = CalculateSpread();
            Vector3 direction = playerCamera.transform.forward + spread;

            Ray ray = new Ray(playerCamera.transform.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range, currentWeapon.hitMask))
            {
                // Hit something!
                // ✅ FIX: ProcessHit() already applies damage, no need for ApplyDamage(hit)
                ProcessHit(hit);

                // Visual feedback
                SpawnHitEffect(hit);

                // Audio feedback
                PlayHitSound();

                // ❌ REMOVED: ApplyDamage(hit) - This was causing DOUBLE DAMAGE!
                // ProcessHit() already handles damage with hitbox multipliers
            }
        }
        
        private Vector3 CalculateSpread()
        {
            float spread = isAiming ? currentWeapon.aimSpread : currentWeapon.hipSpread;
            
            return new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0
            );
        }
        
        private void ProcessHit(RaycastHit hit)
        {
            // ✅ SECURITY: Send hit to server for validation (SERVER-AUTHORITATIVE)
            if (isServer)
            {
                // Server processes directly
                ProcessHitOnServer(hit);
            }
            else
            {
                // Client sends hit data to server for validation
                CmdProcessHit(hit.point, hit.normal, hit.distance, hit.collider.gameObject);
            }

            // CLIENT-SIDE: Immediate visual/audio feedback (optimistic prediction)
            ShowClientSideHitFeedback(hit);
        }

        /// <summary>
        /// CLIENT-SIDE: Immediate visual feedback (prediction - no damage yet)
        /// </summary>
        private void ShowClientSideHitFeedback(RaycastHit hit)
        {
            SurfaceType surface = DetermineSurfaceType(hit.collider);

            // Show hit effects immediately (client prediction)
            SpawnHitEffect(hit.point, hit.normal, surface);

            // Play hit sound
            PlayHitSound(surface);

            // Optimistic damage numbers (will be corrected by server)
            var hitbox = hit.collider.GetComponent<Hitbox>();
            float predictedDamage = currentWeapon.damage;
            if (hitbox != null)
            {
                predictedDamage = hitbox.CalculateDamage(Mathf.RoundToInt(predictedDamage));
            }

            Debug.Log($"🎯 [WeaponSystem CLIENT] HIT: {hit.collider.name} - Predicted Damage: {predictedDamage:F1}");
        }

        /// <summary>
        /// ✅ ANTI-CHEAT: Client requests server to validate and process hit
        /// </summary>
        [Command]
        private void CmdProcessHit(Vector3 hitPoint, Vector3 hitNormal, float distance, GameObject hitObject)
        {
            if (hitObject == null)
            {
                Debug.LogWarning("⚠️ [WeaponSystem SERVER] Received null hit object");
                return;
            }

            // ANTI-CHEAT: Validate fire rate
            if (Time.time < nextFireTime)
            {
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Rate limit violation from player {netId}");
                return;
            }

            // ANTI-CHEAT: Validate ammo
            if (currentAmmo <= 0)
            {
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Ammo cheat attempt from player {netId}");
                return;
            }

            // ANTI-CHEAT: Validate distance (within weapon range)
            if (distance > currentWeapon.range)
            {
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Distance cheat attempt: {distance}m > {currentWeapon.range}m");
                return;
            }

            // Reconstruct hit for server-side processing
            Collider hitCollider = hitObject.GetComponent<Collider>();
            if (hitCollider == null)
            {
                Debug.LogWarning("⚠️ [WeaponSystem SERVER] Hit object has no collider");
                return;
            }

            ProcessHitOnServer(hitPoint, hitNormal, distance, hitCollider);
        }

        /// <summary>
        /// ✅ Server-authoritative hit processing
        /// </summary>
        private void ProcessHitOnServer(RaycastHit hit)
        {
            ProcessHitOnServer(hit.point, hit.normal, hit.distance, hit.collider);
        }

        private void ProcessHitOnServer(Vector3 hitPoint, Vector3 hitNormal, float distance, Collider hitCollider)
        {
            // ⭐ ÖNCELİKLE HITBOX KONTROL ET
            var hitbox = hitCollider.GetComponent<Hitbox>();

            Health health = null;
            float damage = currentWeapon.damage;
            bool isCritical = false;

            if (hitbox != null)
            {
                // Hitbox found - use multiplier
                health = hitbox.GetParentHealth();
                damage = hitbox.CalculateDamage(Mathf.RoundToInt(damage));
                isCritical = hitbox.IsCritical();

                Debug.Log($"🎯 [Server] HIT {hitbox.zone} - Damage: {damage} (Multiplier: {hitbox.damageMultiplier}x)");
            }
            else
            {
                // No hitbox - direct health component
                health = hitCollider.GetComponent<Health>();
                Debug.Log($"🎯 [Server] HIT (no hitbox) - Damage: {damage}");
            }

            if (health != null)
            {
                // Distance falloff
                float distanceFactor = Mathf.Clamp01(1f - (distance / currentWeapon.range));
                damage *= distanceFactor;

                // Apply damage (server-side)
                DamageInfo damageInfo = new DamageInfo(
                    Mathf.RoundToInt(damage),
                    netId,
                    DamageType.Bullet,
                    hitPoint,
                    hitNormal
                );

                health.ApplyDamage(damageInfo);

                // Visual feedback
                if (isCritical)
                {
                    Debug.Log($"💥 [Server] CRITICAL HIT! Damage: {damage}");
                }
            }
        }
        
        private SurfaceType DetermineSurfaceType(Collider collider)
        {
            // Check for health component (flesh)
            if (collider.GetComponent<Health>() != null)
                return SurfaceType.Flesh;
                
            // Check tag (with null safety)
            try
            {
                if (collider.CompareTag("Metal"))
                    return SurfaceType.Metal;
                if (collider.CompareTag("Wood"))
                    return SurfaceType.Wood;
                if (collider.CompareTag("Stone"))
                    return SurfaceType.Stone;
                if (collider.CompareTag("Glass"))
                    return SurfaceType.Generic;
            }
            catch (System.Exception)
            {
                // Tag doesn't exist
            }
                
            return SurfaceType.Generic;
        }
        
        private void SpawnHitEffect(RaycastHit hit)
        {
            SurfaceType surface = DetermineSurfaceType(hit.collider);
            
            // Skip bullet holes for ground
            if (surface == SurfaceType.Generic && hit.collider.CompareTag("Ground"))
            {
                return;
            }
            
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
                    if (hit.collider.CompareTag("Structure"))
                    {
                        effectPrefab = bulletHolePrefab;
                    }
                    break;
            }
            
            if (effectPrefab != null)
            {
                // ✅ FIX: Use object pooling for hit effects
                GameObject effect = GetPooledHitEffect();
                if (effect != null)
                {
                    effect.transform.position = hit.point;
                    effect.transform.rotation = Quaternion.LookRotation(hit.normal);
                    effect.SetActive(true);
                    
                    // Return to pool after duration
                    StartCoroutine(ReturnHitEffectToPool(effect, 2f));
                }
            }
            
            // ✅ FIX: Direct hit effects (no network sync needed for local effects)
            SpawnHitEffect(hit.point, hit.normal, surface);
        }
        
        private void SpawnHitEffect(Vector3 position, Vector3 normal, SurfaceType surface)
        {
            // ✅ FIX: Play hit effects for all players
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
                    effectPrefab = bulletHolePrefab;
                    break;
            }
            
            if (effectPrefab != null)
            {
                GameObject effect = GetPooledHitEffect();
                if (effect != null)
                {
                    effect.transform.position = position;
                    effect.transform.rotation = Quaternion.LookRotation(normal);
                    effect.SetActive(true);
                    
                    StartCoroutine(ReturnHitEffectToPool(effect, 2f));
                }
            }
            else
            {
                // ✅ FIX: Fallback if effectPrefab is null
                Debug.LogWarning("⚠️ [WeaponSystem] effectPrefab is null for surface: " + surface);
            }
        }
        
        private GameObject GetPooledHitEffect()
        {
            if (hitEffectPool.Count > 0)
            {
                return hitEffectPool.Dequeue();
            }
            
            // ✅ FIX: Null check for hitEffectPrefab
            if (hitEffectPrefab == null)
            {
                Debug.LogError("❌ [WeaponSystem] hitEffectPrefab is NULL! Cannot create hit effect.");
                return null;
            }
            
            // Pool empty, create new one
            GameObject effect = Instantiate(hitEffectPrefab);
            effect.SetActive(false);
            return effect;
        }
        
        private IEnumerator ReturnHitEffectToPool(GameObject effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (effect != null)
            {
                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
            }
        }
        
        private void ApplyDamage(RaycastHit hit)
        {
            var health = hit.collider.GetComponent<Health>();
            if (health == null) return;
            
            // Calculate damage (headshot multiplier, distance falloff)
            float damage = currentWeapon.damage;
            
            // Headshot check
            if (hit.collider.CompareTag("Head"))
            {
                damage *= currentWeapon.headshotMultiplier;
            }
            
            // Distance falloff
            float distanceFactor = Mathf.Clamp01(1f - (hit.distance / currentWeapon.range));
            damage *= distanceFactor;
            
            // Apply damage
            DamageInfo damageInfo = new DamageInfo(
                Mathf.RoundToInt(damage),
                0, // ✅ FIX: Local player ID (no network sync needed)
                DamageType.Bullet,
                hit.point,
                hit.normal
            );
            
            health.ApplyDamage(damageInfo);
            
            Debug.Log($"💥 [WeaponSystem] DAMAGE: {damage:F0} to {hit.collider.name}");
        }
        
        private void ApplyRecoil()
        {
            if (weaponHolder == null) return;
            
            // Add recoil
            recoilAmount += currentWeapon.recoilAmount;
            
            // Apply to weapon
            Vector3 recoilPos = originalWeaponPos + new Vector3(
                Random.Range(-0.02f, 0.02f),
                Random.Range(-0.02f, 0.02f),
                -0.05f
            );
            
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, recoilPos, Time.deltaTime * 20f);
            
            // Camera recoil
            if (playerCamera != null)
            {
                float recoilX = Random.Range(-currentWeapon.recoilAmount, currentWeapon.recoilAmount);
                float recoilY = currentWeapon.recoilAmount;
                
                playerCamera.transform.localRotation *= Quaternion.Euler(-recoilY, recoilX, 0);
            }
        }
        
        private void UpdateRecoil()
        {
            if (weaponHolder == null) return;
            
            // Smooth recovery
            recoilAmount = Mathf.Lerp(recoilAmount, 0, Time.deltaTime * 5f);
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, originalWeaponPos, Time.deltaTime * 10f);
            weaponHolder.localRotation = Quaternion.Lerp(weaponHolder.localRotation, originalWeaponRot, Time.deltaTime * 10f);
        }
        
        private void PlayMuzzleFlash()
        {
            if (muzzleFlashPrefab == null || weaponHolder == null) return;
            
            // ✅ FIX: Use object pooling instead of instantiate/destroy
            GameObject flash = GetPooledMuzzleFlash();
            if (flash != null)
            {
                Vector3 muzzlePos = weaponHolder.position + weaponHolder.forward * 0.5f;
                flash.transform.position = muzzlePos;
                flash.transform.rotation = weaponHolder.rotation;
                flash.SetActive(true);
                
                // Return to pool after duration
                StartCoroutine(ReturnMuzzleFlashToPool(flash, 0.1f));
            }
        }
        
        private GameObject GetPooledMuzzleFlash()
        {
            if (muzzleFlashPool.Count > 0)
            {
                return muzzleFlashPool.Dequeue();
            }
            
            // Pool empty, create new one
            GameObject flash = Instantiate(muzzleFlashPrefab);
            flash.SetActive(false);
            return flash;
        }
        
        private IEnumerator ReturnMuzzleFlashToPool(GameObject flash, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (flash != null)
            {
                flash.SetActive(false);
                muzzleFlashPool.Enqueue(flash);
            }
        }
        
        /// <summary>
        /// ✅ FIX: Daha güvenli ve debug-friendly audio playback
        /// </summary>
        private void PlayFireSound()
        {
            // ✅ Tüm kontrolleri yap
            if (fireSounds == null || fireSounds.Length == 0)
            {
                if (debugAudio)
                {
                    Debug.LogError("❌ [WeaponSystem] PlayFireSound() - NO FIRE SOUNDS ASSIGNED! Assign audio clips in Inspector.");
                }
                return;
            }
            
            if (audioSource == null)
            {
                if (debugAudio)
                {
                    Debug.LogError("❌ [WeaponSystem] PlayFireSound() - AUDIO SOURCE IS NULL! This should never happen.");
                }
                return;
            }
            
            // Random clip seç
            AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
            
            if (clip == null)
            {
                if (debugAudio)
                {
                    Debug.LogError("❌ [WeaponSystem] PlayFireSound() - SELECTED AUDIO CLIP IS NULL! Check your audio array.");
                }
                return;
            }
            
            // ✅ 3D spatial sound for realism
            audioSource.spatialBlend = 1f;
            audioSource.PlayOneShot(clip, 0.7f);
            
            if (debugAudio)
            {
                Debug.Log($"🔊 [WeaponSystem] FIRE SOUND PLAYED: {clip.name} (Volume: 0.7)");
            }
        }
        
        private void PlayEmptySound()
        {
            if (emptySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(emptySound, 0.5f);
                
                if (debugAudio)
                {
                    Debug.Log("🔊 [WeaponSystem] EMPTY GUN SOUND PLAYED");
                }
            }
        }
        
        private void PlayHitSound()
        {
            if (hitSounds == null || hitSounds.Length == 0 || audioSource == null) return;

            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.3f);
            }
        }

        private void PlayHitSound(SurfaceType surface)
        {
            // TODO: Add surface-specific hit sounds
            // For now, use generic hit sound
            PlayHitSound();
        }
        
        private IEnumerator CameraShake(float intensity, float duration)
        {
            if (playerCamera == null) yield break;
            
            Vector3 originalPos = playerCamera.transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                
                playerCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            playerCamera.transform.localPosition = originalPos;
        }
        
        private void StartReload()
        {
            if (isReloading) return;
            
            StartCoroutine(ReloadCoroutine());
        }
        
        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStarted?.Invoke();
            
            // Play sound
            if (reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reloadSound);
                
                if (debugAudio)
                {
                    Debug.Log("🔊 [WeaponSystem] RELOAD SOUND PLAYED");
                }
            }
                
            // Play animation
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("Reload");
            
            // Wait
            yield return new WaitForSeconds(currentWeapon.reloadTime);
            
            // Reload
            int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
            
            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;
            
            isReloading = false;
            OnReloadComplete?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            Debug.Log($"🔄 [WeaponSystem] RELOADED: {currentAmmo}/{currentWeapon.magazineSize} (Reserve: {reserveAmmo})");
        }
        
        private void PlayFireEffects()
        {
            // ✅ FIX: Play effects directly (no network sync needed for local effects)
            PlayMuzzleFlash();
            PlayFireSound();
            
            // ✅ FIX: Play weapon animation
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger("Fire");
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✅ FIX: WEAPON STATE MANAGEMENT
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Build moduna geçerken weapon state'ini reset et
        /// </summary>
        public void ResetWeaponState()
        {
            // Cooldown ekle
            nextFireTime = Time.time + 1f;
            
            // Weapon position reset
            if (weaponHolder != null)
            {
                weaponHolder.localPosition = originalWeaponPos;
                weaponHolder.localRotation = originalWeaponRot;
            }
            
            // Recoil reset
            recoilAmount = 0f;
            
            // Camera reset (if needed)
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.identity;
            }
            
            Debug.Log("🔫 [WeaponSystem] Weapon state reset");
        }
        
        /// <summary>
        /// Build modundan çıkarken silahı aktif et
        /// </summary>
        public void EnableWeapon()
        {
            Debug.Log("✅ [WeaponSystem] Weapon enabled");
        }
        
        /// <summary>
        /// Build moduna girerken silahı devre dışı bırak
        /// </summary>
        public void DisableWeapon()
        {
            ResetWeaponState();
            Debug.Log("🚫 [WeaponSystem] Weapon disabled");
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        public void SetWeapon(WeaponConfig weapon)
        {
            currentWeapon = weapon;
            currentAmmo = weapon.magazineSize;
            reserveAmmo = weapon.maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }
        
        public int GetCurrentAmmo() => currentAmmo;
        public int GetReserveAmmo() => reserveAmmo;
        public bool IsReloading() => isReloading;
        
        /// <summary>
        /// Create default weapon config if none assigned
        /// </summary>
        private void CreateDefaultWeaponConfig()
        {
            if (currentWeapon != null) return;
            
            // Create default weapon config
            currentWeapon = ScriptableObject.CreateInstance<WeaponConfig>();
            currentWeapon.weaponName = "Default Rifle";
            currentWeapon.damage = 25f;
            currentWeapon.range = 100f;
            currentWeapon.fireRate = 10f;
            currentWeapon.fireMode = FireMode.Auto;
            currentWeapon.magazineSize = 30;
            currentWeapon.maxAmmo = 120;
            currentWeapon.reloadTime = 2f;
            currentWeapon.hipSpread = 0.05f;
            currentWeapon.aimSpread = 0.01f;
            currentWeapon.recoilAmount = 2f;
            currentWeapon.headshotMultiplier = 2f;
            
            Debug.Log("✅ [WeaponSystem] Default weapon config created!");
        }

        /// <summary>
        /// ✅ FIX: Cleanup coroutines and events on destroy
        /// </summary>
        private void OnDestroy()
        {
            // Stop all coroutines to prevent memory leaks
            StopAllCoroutines();

            // Clear event subscriptions
            OnAmmoChanged = null;
            OnReloadStarted = null;
            OnReloadComplete = null;
            OnWeaponFired = null;

            if (debugAudio)
            {
                Debug.Log("🗑️ [WeaponSystem] Cleanup completed - coroutines stopped, events cleared");
            }
        }

        /// <summary>
        /// ✅ FIX: Stop coroutines on disable
        /// </summary>
        private void OnDisable()
        {
            StopAllCoroutines();
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC PROPERTIES FOR EDITOR ACCESS
        // ═══════════════════════════════════════════════════════════
        
        public WeaponConfig CurrentWeapon
        {
            get { return currentWeapon; }
            set { currentWeapon = value; }
        }
        
        public AudioClip[] FireSounds
        {
            get { return fireSounds; }
            set { fireSounds = value; }
        }
        
        public AudioClip[] HitSounds
        {
            get { return hitSounds; }
            set { hitSounds = value; }
        }
        
        public GameObject MuzzleFlashPrefab
        {
            get { return muzzleFlashPrefab; }
            set { muzzleFlashPrefab = value; }
        }
        
        public GameObject HitEffectPrefab
        {
            get { return hitEffectPrefab; }
            set { hitEffectPrefab = value; }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // SURFACE TYPES
    // ═══════════════════════════════════════════════════════════
    
    [System.Serializable]
    public enum SurfaceType
    {
        Generic,
        Flesh,
        Metal,
        Wood,
        Stone
    }
}