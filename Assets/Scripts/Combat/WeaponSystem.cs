using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using System.Collections;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// PROFESYONEL WEAPON SYSTEM
    /// - Recoil
    /// - Spread
    /// - Ammo
    /// - Reload
    /// - Hit registration
    /// - Visual & Audio feedback
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
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private GameObject bloodEffectPrefab;
        [SerializeField] private GameObject metalSparksPrefab;
        
        [Header("🔊 AUDIO")]
        [SerializeField] private AudioClip[] fireSounds;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptySound;
        [SerializeField] private AudioClip[] hitSounds;
        
        [Header("📊 WEAPON STATE")]
        [SyncVar] private int currentAmmo;
        [SyncVar] private int reserveAmmo;
        [SyncVar] private bool isReloading;
        
        private float nextFireTime;
        private float recoilAmount;
        private Vector3 originalWeaponPos;
        private Quaternion originalWeaponRot;
        private bool isAiming;
        
        // Events
        public System.Action<int, int> OnAmmoChanged; // current, reserve
        public System.Action OnReloadStarted;
        public System.Action OnReloadComplete;
        public System.Action OnWeaponFired;
        
        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
                
            if (weaponHolder != null)
            {
                originalWeaponPos = weaponHolder.localPosition;
                originalWeaponRot = weaponHolder.localRotation;
            }
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            Debug.Log($"🎯 WeaponSystem OnStartLocalPlayer - currentWeapon: {currentWeapon?.weaponName ?? "NULL"}");
            
            // Find camera
            if (playerCamera == null)
                playerCamera = Camera.main;
                
            // Initialize ammo
            if (currentWeapon != null)
            {
                currentAmmo = currentWeapon.magazineSize;
                reserveAmmo = currentWeapon.maxAmmo;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                Debug.Log($"✅ Ammo initialized: {currentAmmo}/{reserveAmmo}");
            }
            else
            {
                Debug.LogError("❌ currentWeapon is NULL! Weapon config not assigned!");
            }
        }
        
        private void Update()
        {
            if (!isLocalPlayer) return;
            
            HandleInput();
            UpdateRecoil();
        }
        
        private void HandleInput()
        {
            // Fire
            if (Input.GetButton("Fire1"))
            {
                // ⭐ PERFORMANCE: Reduce debug spam
                if (Time.frameCount % 10 == 0) // Only log every 10 frames
                {
                    Debug.Log($"🔫 Fire1 pressed - CanFire: {CanFire()}, FireMode: {currentWeapon?.fireMode}");
                }
                
                if (CanFire())
                {
                    if (currentWeapon.fireMode == FireMode.Auto || Input.GetButtonDown("Fire1"))
                    {
                        Fire();
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
            // ⭐ PERFORMANCE: Reduce debug spam - only log every 5th shot
            if (currentAmmo % 5 == 0)
            {
                Debug.Log($"🔥 FIRE() executing - Weapon: {currentWeapon?.weaponName}, Ammo: {currentAmmo}");
            }
            
            nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            currentAmmo--;
            
            // Visual feedback
            ApplyRecoil();
            PlayMuzzleFlash();
            
            // Audio feedback
            PlayFireSound();
            
            // Raycast
            PerformRaycast();
            
            // Camera shake
            StartCoroutine(CameraShake(0.05f, 0.1f));
            
            // Network sync
            CmdFire();
            
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
                ProcessHit(hit);
                
                // Visual feedback
                SpawnHitEffect(hit);
                
                // Audio feedback
                PlayHitSound();
                
                // Damage
                ApplyDamage(hit);
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
            // ⭐ ÖNCELİKLE HITBOX KONTROL ET
            var hitbox = hit.collider.GetComponent<Hitbox>();
            
            Health health = null;
            float damage = currentWeapon.damage;
            bool isCritical = false;
            
            if (hitbox != null)
            {
                // Hitbox found - use multiplier
                health = hitbox.GetParentHealth();
                damage = hitbox.CalculateDamage(Mathf.RoundToInt(damage));
                isCritical = hitbox.IsCritical();
                
                Debug.Log($"🎯 HIT {hitbox.zone} - Damage: {damage} (Multiplier: {hitbox.damageMultiplier}x)");
            }
            else
            {
                // No hitbox - direct health component
                health = hit.collider.GetComponent<Health>();
                Debug.Log($"🎯 HIT (no hitbox) - Damage: {damage}");
            }
            
            if (health != null)
            {
                // Distance falloff
                float distanceFactor = Mathf.Clamp01(1f - (hit.distance / currentWeapon.range));
                damage *= distanceFactor;
                
                // Apply damage
                DamageInfo damageInfo = new DamageInfo(
                    Mathf.RoundToInt(damage),
                    netId,
                    DamageType.Bullet,
                    hit.point,
                    hit.normal
                );
                
                health.ApplyDamage(damageInfo);
                
                // Visual feedback
                if (isCritical)
                {
                    Debug.Log($"💥 CRITICAL HIT! Damage: {damage}");
                    // TODO: Show special effect
                }
            }
            
            // Determine surface type for effects
            SurfaceType surface = DetermineSurfaceType(hit.collider);
            Debug.Log($"🎯 HIT: {hit.collider.name} - Surface: {surface} - Distance: {hit.distance:F1}m");
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
                    return SurfaceType.Generic; // Use generic for glass
            }
            catch (System.Exception)
            {
                // Tag doesn't exist, continue with generic
            }
                
            return SurfaceType.Generic;
        }
        
        private void SpawnHitEffect(RaycastHit hit)
        {
            // ⭐ PERFORMANCE: Only spawn effects for certain surfaces
            SurfaceType surface = DetermineSurfaceType(hit.collider);
            
            // Skip bullet holes for ground/terrain to prevent spam
            if (surface == SurfaceType.Generic && hit.collider.CompareTag("Ground"))
            {
                return; // Don't spawn bullet holes on ground
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
                    // Only spawn bullet holes on structures, not ground
                    if (hit.collider.CompareTag("Structure"))
                    {
                        effectPrefab = bulletHolePrefab;
                    }
                    break;
            }
            
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                // AutoDestroy script will handle cleanup
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
                netId,
                DamageType.Bullet,
                hit.point,
                hit.normal
            );
            
            health.ApplyDamage(damageInfo);
            
            Debug.Log($"💥 DAMAGE: {damage:F0} to {hit.collider.name}");
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
            
            // Spawn at weapon tip
            Vector3 muzzlePos = weaponHolder.position + weaponHolder.forward * 0.5f;
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePos, weaponHolder.rotation);
            Destroy(flash, 0.1f);
        }
        
        private void PlayFireSound()
        {
            // ⭐ PERFORMANCE: Reduce debug spam
            if (fireSounds == null || fireSounds.Length == 0)
            {
                if (Time.frameCount % 60 == 0) // Only log once per second
                {
                    Debug.LogWarning("⚠️ fireSounds is null or empty!");
                }
                return;
            }
            
            if (audioSource == null)
            {
                if (Time.frameCount % 60 == 0) // Only log once per second
                {
                    Debug.LogWarning("⚠️ audioSource is null!");
                }
                return;
            }
            
            AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
            if (clip == null)
            {
                if (Time.frameCount % 60 == 0) // Only log once per second
                {
                    Debug.LogWarning("⚠️ Selected fire sound clip is null!");
                }
                return;
            }
            
            audioSource.PlayOneShot(clip, 0.5f);
        }
        
        private void PlayHitSound()
        {
            if (hitSounds == null || hitSounds.Length == 0 || audioSource == null) return;
            
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(clip, 0.3f);
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
                audioSource.PlayOneShot(reloadSound);
                
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
            
            Debug.Log($"🔄 RELOADED: {currentAmmo}/{currentWeapon.magazineSize} (Reserve: {reserveAmmo})");
        }
        
        [Command]
        private void CmdFire()
        {
            RpcFire();
        }
        
        [ClientRpc]
        private void RpcFire()
        {
            if (isLocalPlayer) return; // Local player already handled
            
            // Play effects for other players
            PlayMuzzleFlash();
            PlayFireSound();
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
    }
    
    // ═══════════════════════════════════════════════════════════
    // WEAPON CONFIG (ScriptableObject)
    // ═══════════════════════════════════════════════════════════
    
    [System.Serializable]
    public enum FireMode
    {
        Semi,
        Auto,
        Burst
    }
    
    [System.Serializable]
    public enum SurfaceType
    {
        Generic,
        Flesh,
        Metal,
        Wood,
        Stone
    }
    
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Tactical Combat/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("📊 STATS")]
        public string weaponName = "Assault Rifle";
        public float damage = 25f;
        public float range = 100f;
        public float fireRate = 10f; // rounds per second
        public FireMode fireMode = FireMode.Auto;
        
        [Header("🎯 ACCURACY")]
        public float hipSpread = 0.05f;
        public float aimSpread = 0.01f;
        public float recoilAmount = 2f;
        public float headshotMultiplier = 2f;
        
        [Header("📦 AMMO")]
        public int magazineSize = 30;
        public int maxAmmo = 120;
        public float reloadTime = 2f;
        
        [Header("🎯 TARGETING")]
        public LayerMask hitMask = ~0;
    }
}
