using UnityEngine;
using Mirror;
using UnityEngine.InputSystem; // Optional Input System bridge
using TacticalCombat.Core;
using TacticalCombat.Effects;
using TacticalCombat.Player; // ✅ FIX: InputManager için gerekli
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

        // ✅ FIX: Track coroutines to prevent memory leaks
        private Coroutine currentCameraShakeCoroutine;
        private Coroutine currentReloadCoroutine;
        private Vector3 originalWeaponPos;
        private Quaternion originalWeaponRot;
        private bool isAiming;
        
        [Header("Input System")]
        [SerializeField] private InputActionAsset actionsAsset; // Assign InputSystem_Actions in Inspector
        
        // ✅ Input System bridge (optional)
        private PlayerInput playerInput;
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction aimAction;
        private bool fireHeld;
        private bool firePressed;
        private bool reloadPressed;
        private bool aimHeld;
        
        // ✅ FIX: Audio debug flag
        [Header("🐛 DEBUG")]
        [SerializeField] private bool debugAudio = true;
        [SerializeField] private bool debugInputs = true; // ✅ FIX: Debug aktif et
        
        // ✅ FIX: Use 'event' keyword to prevent external null assignment and memory leaks
        public event System.Action<int, int> OnAmmoChanged;
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadComplete;
        public event System.Action OnWeaponFired;
        
        // ✅ FIX: Build mode awareness
        private TacticalCombat.Player.InputManager inputManager;
        
        // ✅ FIX: Performance - Object pooling for effects
        private Queue<GameObject> muzzleFlashPool = new Queue<GameObject>();
        private Queue<GameObject> hitEffectPool = new Queue<GameObject>();
        private const int POOL_SIZE = 10;
        
        private void Awake()
        {
            // Validate NetworkIdentity placement: WeaponSystem must live on the same GameObject
            // as the root NetworkIdentity (no child NetworkIdentity allowed by Mirror)
            var myIdentity = GetComponent<NetworkIdentity>();
            var parentIdentity = GetComponentInParent<NetworkIdentity>();
            if (parentIdentity != null && parentIdentity.gameObject != gameObject && myIdentity != null)
            {
                Debug.LogError("[WeaponSystem] NetworkIdentity detected on child. Please move WeaponSystem to the root object with NetworkIdentity and remove child NetworkIdentity.", this);
            }

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
            
            // ✅ PERFORMANCE: Initialize PlayerInput once in Awake (shared with FPSController)
            // This avoids runtime AddComponent calls in OnEnable/OnStartLocalPlayer
            InitializePlayerInput();
        }
        
        private void InitializePlayerInput()
        {
            // Only initialize if actionsAsset is assigned (optional feature)
            if (actionsAsset == null) return;
            
            try
            {
                playerInput = GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    // Add once in Awake - shared by FPSController and WeaponSystem
                    // FPSController usually runs Awake first, but if not, we add it here
                    playerInput = gameObject.AddComponent<PlayerInput>();
                    playerInput.actions = actionsAsset;
                    playerInput.defaultActionMap = "Player";
                    
                    if (debugInputs)
                    {
                        Debug.Log("[WeaponSystem] PlayerInput initialized in Awake");
                    }
                }
                else if (playerInput.actions == null && actionsAsset != null)
                {
                    // PlayerInput exists but no asset assigned - assign it
                    playerInput.actions = actionsAsset;
                    playerInput.defaultActionMap = "Player";
                }
            }
            catch (System.Exception e)
            {
                if (debugInputs)
                {
                    Debug.LogWarning($"[WeaponSystem] Failed to initialize PlayerInput: {e.Message}");
                }
            }
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
        
        private void Start()
        {
            inputManager = GetComponent<TacticalCombat.Player.InputManager>();

            if (debugInputs)
            {
                Debug.Log($"[WeaponSystem] Start - InputManager: {(inputManager != null ? "Found" : "Not found yet, will retry")}");
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

        private void OnEnable()
        {
            // ✅ Hook Input System actions (PlayerInput already initialized in Awake)
            try
            {
                // PlayerInput should already exist from Awake, but check anyway
                if (playerInput == null)
                {
                    playerInput = GetComponent<PlayerInput>();
                }

                // Ensure actions are assigned (should be done in Awake)
                if (playerInput != null && playerInput.actions == null && actionsAsset != null)
                {
                    playerInput.actions = actionsAsset;
                    playerInput.defaultActionMap = "Player";
                }

                if (playerInput.actions != null)
                {
                    var map = playerInput.actions.FindActionMap("Player", true);
                    if (map != null)
                    {
                        playerInput.defaultActionMap = "Player";
                        map.Enable();

                        // Map to existing actions in InputSystem_Actions: Attack/Reload/Aim
                        fireAction = map.FindAction("Attack", false);
                        reloadAction = map.FindAction("Reload", false);
                        aimAction = map.FindAction("Aim", false);

                        if (fireAction != null)
                        {
                            fireAction.performed += OnFirePerformed;
                            fireAction.canceled += OnFireCanceled;
                            fireAction.Enable();
                        }
                        if (reloadAction != null)
                        {
                            reloadAction.performed += OnReloadPerformed;
                            reloadAction.Enable();
                        }
                        if (aimAction != null)
                        {
                            aimAction.performed += OnAimPerformed;
                            aimAction.canceled += OnAimCanceled;
                            aimAction.Enable();
                        }
                    }
                }
            }
            catch { /* Input System not present or not configured */ }
        }
        
        private void Update()
        {
            // Retry finding InputManager if missing
            if (inputManager == null)
            {
                inputManager = GetComponent<TacticalCombat.Player.InputManager>();
                if (inputManager == null) return;
            }

            if (inputManager.IsInBuildMode) return;

            HandleInput();
            UpdateRecoil();
        }
        
        private void HandleInput()
        {
            if (inputManager == null || inputManager.BlockShootInput) return;
            
            // Fire - ✅ FIX: Separate auto and semi-auto to prevent stuck shooting
            if (currentWeapon != null)
            {
                // Determine fire inputs (Input System + legacy fallback)
                bool fireHeldInput = fireHeld || Input.GetButton("Fire1");
                bool firePressedInput = firePressed || Input.GetButtonDown("Fire1");

                if (currentWeapon.fireMode == FireMode.Auto)
                {
                    // Auto mode: Hold to fire continuously
                    if (fireHeldInput && CanFire())
                    {
                        Fire();
                    }
                    else if (fireHeldInput && currentAmmo <= 0 && Time.frameCount % 30 == 0)
                    {
                        // Empty gun sound (throttled)
                        PlayEmptySound();
                    }
                }
                else
                {
                    // Semi-auto/Burst: Press to fire once
                    if (firePressedInput && CanFire())
                    {
                        Fire();
                    }
                    else if (firePressedInput && currentAmmo <= 0)
                    {
                        PlayEmptySound();
                    }
                }
            }
            
            // Reload
            if ((reloadPressed || Input.GetKeyDown(KeyCode.R)) && CanReload())
            {
                StartReload();
                reloadPressed = false;
            }
            
            // Aim (optional)
            isAiming = aimHeld || Input.GetButton("Fire2");

            // Reset one-shot press
            firePressed = false;
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
            nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            currentAmmo--;
            
            // Visual feedback
            ApplyRecoil();
            PlayMuzzleFlash();
            
            // ✅ Audio feedback - CRITICAL
            PlayFireSound();
            
            // Raycast
            PerformRaycast();

            // ✅ FIX: Stop previous camera shake before starting new one to prevent coroutine leak
            if (currentCameraShakeCoroutine != null)
            {
                StopCoroutine(currentCameraShakeCoroutine);
            }
            currentCameraShakeCoroutine = StartCoroutine(CameraShake(0.05f, 0.1f));

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

            // ✅ FIX: Raycast all hits, skip our own colliders
            RaycastHit[] hits = Physics.RaycastAll(ray, currentWeapon.range, currentWeapon.hitMask);

            // Find first valid hit (not ourselves)
            RaycastHit? validHit = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                // Skip our own colliders
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                    continue;

                // Find closest valid hit
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    validHit = hit;
                }
            }

            if (validHit.HasValue)
            {
                // Hit something!
                ProcessHit(validHit.Value);

                // Visual feedback
                SpawnHitEffect(validHit.Value);

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
                // Only send if hit object has NetworkIdentity (can be synced)
                GameObject hitObj = hit.collider?.gameObject;
                if (hitObj != null && hitObj.GetComponent<NetworkIdentity>() != null)
                {
                    CmdProcessHit(hit.point, hit.normal, hit.distance, hitObj);
                }
                else
                {
                    // Hit non-networked object (wall, floor, etc) - still send for validation
                    CmdProcessHit(hit.point, hit.normal, hit.distance, null);
                }
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
            bool isBodyHit = hit.collider.GetComponent<Hitbox>() != null || hit.collider.GetComponent<Health>() != null;

            // ✅ PROFESSIONAL VFX: Use pooled impact effects (Battlefield quality)
            if (ImpactVFXPool.Instance != null)
            {
                ImpactVFXPool.Instance.PlayImpact(hit.point, hit.normal, surface, isBodyHit);
            }

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
            SurfaceType surface = DetermineSurfaceType(hitCollider);
            bool isBodyHit = hitbox != null || hitCollider.GetComponent<Health>() != null;

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
                    hitNormal,
                    0f,
                    isCritical  // Pass headshot info
                );

                health.ApplyDamage(damageInfo);

                // ✅ BATTLEFIELD QUALITY: Show impact VFX to ALL clients
                RpcShowImpactEffect(hitPoint, hitNormal, surface, isBodyHit, isCritical);

                // Visual feedback
                if (isCritical)
                {
                    Debug.Log($"💥 [Server] CRITICAL HIT! Damage: {damage}");
                }
            }
            else
            {
                // Hit environment - still show impact
                RpcShowImpactEffect(hitPoint, hitNormal, surface, false, false);
            }
        }

        /// <summary>
        /// ✅ Network sync impact effects to all clients
        /// </summary>
        [ClientRpc]
        private void RpcShowImpactEffect(Vector3 hitPoint, Vector3 hitNormal, SurfaceType surface, bool isBodyHit, bool isCritical)
        {
            // Show impact effect on all clients
            if (ImpactVFXPool.Instance != null)
            {
                ImpactVFXPool.Instance.PlayImpact(hitPoint, hitNormal, surface, isBodyHit);
            }

            // Play hit sound
            PlayHitSound(surface);

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
            
            // Skip bullet holes for ground (safe tag check)
            try
            {
                if (surface == SurfaceType.Generic && hit.collider.CompareTag("Ground"))
                {
                    return;
                }
            }
            catch { }
            
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
                    try
                    {
                        if (hit.collider.CompareTag("Structure"))
                        {
                            effectPrefab = bulletHolePrefab;
                        }
                    }
                    catch { }
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
            
            // Headshot check (safe tag check)
            try
            {
                if (hit.collider.CompareTag("Head"))
                {
                    damage *= currentWeapon.headshotMultiplier;
                }
            }
            catch { }
            
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

        // ═══════════════════════════════════════════════════════════
        // INPUT SYSTEM BRIDGE HANDLERS
        // ═══════════════════════════════════════════════════════════
        private void OnFirePerformed(InputAction.CallbackContext ctx)
        {
            fireHeld = true;
            firePressed = true;
        }

        private void OnFireCanceled(InputAction.CallbackContext ctx)
        {
            fireHeld = false;
            firePressed = false;
        }

        private void OnReloadPerformed(InputAction.CallbackContext ctx)
        {
            reloadPressed = true;
        }

        private void OnAimPerformed(InputAction.CallbackContext ctx)
        {
            aimHeld = true;
        }

        private void OnAimCanceled(InputAction.CallbackContext ctx)
        {
            aimHeld = false;
        }

        private void PlayHitSound(SurfaceType surface)
        {
            // TODO: Add surface-specific hit sounds
            // For now, use generic hit sound
            PlayHitSound();
        }
        
        private IEnumerator CameraShake(float intensity, float duration)
        {
            if (playerCamera == null)
            {
                currentCameraShakeCoroutine = null; // ✅ FIX: Clear reference on early exit
                yield break;
            }

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
            currentCameraShakeCoroutine = null; // ✅ FIX: Clear coroutine reference when complete
        }
        
        private void StartReload()
        {
            if (isReloading) return;

            // ✅ FIX: Stop previous reload coroutine before starting new one
            if (currentReloadCoroutine != null)
            {
                StopCoroutine(currentReloadCoroutine);
            }
            currentReloadCoroutine = StartCoroutine(ReloadCoroutine());
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
            currentReloadCoroutine = null; // ✅ FIX: Clear coroutine reference
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
        /// ✅ FIX: Cleanup events and coroutines on disable (respawn, death, etc.)
        /// </summary>
        private void OnDisable()
        {
            // Unhook Input System actions
            try
            {
                if (fireAction != null)
                {
                    fireAction.performed -= OnFirePerformed;
                    fireAction.canceled -= OnFireCanceled;
                    fireAction.Disable();
                }
                if (reloadAction != null)
                {
                    reloadAction.performed -= OnReloadPerformed;
                    reloadAction.Disable();
                }
                if (aimAction != null)
                {
                    aimAction.performed -= OnAimPerformed;
                    aimAction.canceled -= OnAimCanceled;
                    aimAction.Disable();
                }
            }
            catch { }

            // Stop tracked coroutines to prevent memory leaks
            if (currentCameraShakeCoroutine != null)
            {
                StopCoroutine(currentCameraShakeCoroutine);
                currentCameraShakeCoroutine = null;
            }

            if (currentReloadCoroutine != null)
            {
                StopCoroutine(currentReloadCoroutine);
                currentReloadCoroutine = null;
            }

            // Clear event subscriptions to prevent memory leaks on respawn
            // Using 'event' keyword prevents external null assignment but we need to clear internally
            if (OnAmmoChanged != null)
            {
                foreach (System.Delegate d in OnAmmoChanged.GetInvocationList())
                {
                    OnAmmoChanged -= (System.Action<int, int>)d;
                }
            }

            if (OnReloadStarted != null)
            {
                foreach (System.Delegate d in OnReloadStarted.GetInvocationList())
                {
                    OnReloadStarted -= (System.Action)d;
                }
            }

            if (OnReloadComplete != null)
            {
                foreach (System.Delegate d in OnReloadComplete.GetInvocationList())
                {
                    OnReloadComplete -= (System.Action)d;
                }
            }

            if (OnWeaponFired != null)
            {
                foreach (System.Delegate d in OnWeaponFired.GetInvocationList())
                {
                    OnWeaponFired -= (System.Action)d;
                }
            }

            Debug.Log($"🔇 [WeaponSystem] OnDisable - {gameObject.name} | isServer: {isServer} | isClient: {isClient} | Frame: {Time.frameCount}");
        }

        /// <summary>
        /// ✅ FIX: Cleanup coroutines on destroy
        /// </summary>
        private void OnDestroy()
        {
            // Stop all coroutines to prevent memory leaks
            StopAllCoroutines();

            // Safe debug logging
            if (gameObject != null)
            {
                Debug.Log($"🗑️ [WeaponSystem] Cleanup completed - {gameObject.name}");
            }
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

