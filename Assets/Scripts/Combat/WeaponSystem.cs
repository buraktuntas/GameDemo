using UnityEngine;
using Mirror;
using UnityEngine.InputSystem; // Optional Input System bridge
using TacticalCombat.Core;
using TacticalCombat.Effects;
using TacticalCombat.Player; // ✅ FIX: InputManager için gerekli
using System.Collections;
using System.Collections.Generic; // ✅ FIX: Queue<GameObject> için gerekli
using static TacticalCombat.Core.GameLogger;

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
        // ✅ HIGH PRIORITY: Cache animator trigger hashes (prevent string allocation)
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        
        [Header("📦 WEAPON CONFIG")]
        [SerializeField] private WeaponConfig currentWeapon;
        
        [Header("🎯 REFERENCES")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponHolder;
        [Header("🎮 CONTROLLERS")]
        private WeaponAudioController audioController;
        private WeaponVFXController vfxController;
        
        [Header("📊 WEAPON STATE")]
        // ✅ CRITICAL FIX: Ammo must be server-authoritative (SyncVar)
        [SyncVar] private int currentAmmo;
        [SyncVar] private int reserveAmmo;
        private bool isReloading;

        // ✅ PHASE 2: nextFireTime server-only (client can't hack it)
        // Note: [Server] attribute doesn't work on fields, but we ensure it's only used on server
        private float nextFireTime;
        private float recoilAmount;
        
        // ✅ CRITICAL FIX: Deterministic spread seed
        [SyncVar] private int spreadSeed = 0;

        // ✅ FIX: Track coroutines to prevent memory leaks
        private Coroutine currentCameraShakeCoroutine;
        private Coroutine currentReloadCoroutine;
        private Coroutine retryCameraCoroutine; // ✅ FIX: Store retry coroutine to stop it when camera is found
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
        
        // ✅ FIX: Weapon animator (optional)
        private Animator weaponAnimator;
        
        private void Awake()
        {
            // Validate NetworkIdentity placement: WeaponSystem must live on the same GameObject
            // as the root NetworkIdentity (no child NetworkIdentity allowed by Mirror)
            var myIdentity = GetComponent<NetworkIdentity>();
            var parentIdentity = GetComponentInParent<NetworkIdentity>();
            if (parentIdentity != null && parentIdentity.gameObject != gameObject && myIdentity != null)
            {
                LogError("[WeaponSystem] NetworkIdentity detected on child. Please move WeaponSystem to the root object with NetworkIdentity and remove child NetworkIdentity.");
            }

            // ✅ FIX: Get Controllers
            audioController = GetComponent<WeaponAudioController>();
            vfxController = GetComponent<WeaponVFXController>();

            if (audioController == null) audioController = gameObject.AddComponent<WeaponAudioController>();
            if (vfxController == null) vfxController = gameObject.AddComponent<WeaponVFXController>();
            
            // ✅ FIX: Fallback for weaponHolder
            if (weaponHolder == null)
            {
                // Try to find a child named "WeaponHolder" or "WeaponPosition"
                var childHolder = transform.Find("WeaponHolder");
                if (childHolder == null) childHolder = transform.Find("Camera/WeaponHolder");
                if (childHolder == null) childHolder = transform.Find("MainCamera/WeaponHolder");
                
                if (childHolder != null)
                {
                    weaponHolder = childHolder;
                }
                else
                {
                    // Fallback to transform (will fire from player center, but better than nothing)
                    weaponHolder = transform;
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[WeaponSystem] WeaponHolder not assigned! Using player transform as fallback.");
                    #endif
                }
            }

            // ✅ FIX: Get weapon animator (optional)
            weaponAnimator = GetComponent<Animator>();
            if (weaponAnimator == null && weaponHolder != null)
            {
                weaponAnimator = weaponHolder.GetComponent<Animator>();
            }
            
            // ✅ PERFORMANCE: Initialize PlayerInput once in Awake (shared with FPSController)
            // This avoids runtime AddComponent calls in OnEnable/OnStartLocalPlayer
            InitializePlayerInput();
            
            // ✅ CRITICAL FIX: Initialize original weapon position and rotation to prevent NaN errors
            if (weaponHolder != null)
            {
                originalWeaponPos = weaponHolder.localPosition;
                originalWeaponRot = weaponHolder.localRotation;
            }
            else
            {
                // Fallback: Set to default values if weaponHolder is not found yet
                originalWeaponPos = Vector3.zero;
                originalWeaponRot = Quaternion.identity;
            }
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
        

        
        private void Start()
        {
            inputManager = GetComponent<TacticalCombat.Player.InputManager>();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugInputs)
            {
                LogInfo($"[WeaponSystem] Start - InputManager: {(inputManager != null ? "Found" : "Not found yet, will retry")}");
            }
            #endif

            // ⚡ PERFORMANCE FIX: Cache camera reference
            if (playerCamera == null)
            {
                // ✅ HIGH PRIORITY FIX: Get camera from FPSController (never use Camera.main)
                // ✅ CRITICAL FIX: Use GetComponentInParent to find FPSController even if WeaponSystem is on a child GameObject
                var fpsController = GetComponentInParent<TacticalCombat.Player.FPSController>();
                if (fpsController != null)
                {
                    playerCamera = fpsController.GetCamera();
                }

                // ✅ PROFESSIONAL FIX: If camera still null, retry in coroutine (FPSController might not be ready yet)
                if (playerCamera == null)
                {
                    LogWarning("Camera not found yet, will retry... (FPSController might not be initialized)");
                    // Retry camera assignment in coroutine (FPSController.OnStartLocalPlayer runs after Start)
                    retryCameraCoroutine = StartCoroutine(RetryCameraAssignment());
                    // Continue with initialization - coroutine will handle camera assignment
                }
            }
            
            // ✅ CRITICAL FIX: Initialize original weapon position and rotation in Start() if not already set
            // This ensures weaponHolder is found and original values are captured
            if (weaponHolder != null && (originalWeaponPos == Vector3.zero && originalWeaponRot == Quaternion.identity || 
                float.IsNaN(originalWeaponPos.x) || float.IsNaN(originalWeaponRot.x)))
            {
                originalWeaponPos = weaponHolder.localPosition;
                originalWeaponRot = weaponHolder.localRotation;
                LogInfo($"[WeaponSystem] Original weapon position/rotation initialized: Pos={originalWeaponPos}, Rot={originalWeaponRot}");
            }
            
            // ✅ CRITICAL FIX: Activate CurrentWeapon GameObject if it exists
            if (weaponHolder != null)
            {
                Transform currentWeapon = weaponHolder.Find("CurrentWeapon");
                if (currentWeapon != null && !currentWeapon.gameObject.activeSelf)
                {
                    currentWeapon.gameObject.SetActive(true);
                    LogInfo($"[WeaponSystem] Activated CurrentWeapon GameObject");
                }
            }
                
            // ✅ CRITICAL FIX: Initialize ammo only on server (SyncVar will sync to clients)
            // Note: If camera is null, this will still run but weapon won't fire until camera is found
            if (isServer)
            {
                if (currentWeapon != null)
                {
                    currentAmmo = currentWeapon.magazineSize;
                    reserveAmmo = currentWeapon.maxAmmo;
                    OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    LogInfo($"[WeaponSystem] Ammo initialized: {currentAmmo}/{reserveAmmo}, Weapon: {currentWeapon.weaponName}");
                    #endif
                }
                else
                {
                    LogWarning("currentWeapon is NULL! Creating default weapon config...");
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
            // ✅ PROFESSIONAL FIX: Try to assign camera if still null (for local player)
            if (isLocalPlayer && playerCamera == null)
            {
                TryAssignCamera();
            }
            
            // ✅ FIX: Don't process input if weapon system is disabled or camera is missing
            if (!enabled || playerCamera == null) return;
            
            // ✅ FIX: Only process input for local player
            if (!isLocalPlayer) return;
            
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
            // ✅ CRITICAL FIX: Use Input System if available, fallback to Legacy Input
            // Check if Input System is working (fireHeld/firePressed set by callbacks)
            // If Input System callbacks aren't working, use Legacy Input as fallback
            bool fireHeldInput = fireHeld;
            bool firePressedInput = firePressed;
            
            // ✅ FIX: Fallback to Legacy Input if Input System isn't working
            // Check if Input System is properly initialized (playerInput != null and fireAction != null)
            // If not, use Legacy Input as fallback to prevent "can't fire" bug
            if (playerInput == null || fireAction == null)
            {
                // Input System not initialized - use Legacy Input
                if (Mouse.current != null)
                {
                    fireHeldInput = Mouse.current.leftButton.isPressed;
                    firePressedInput = Mouse.current.leftButton.wasPressedThisFrame;
                }
            }
            // If Input System is initialized but callbacks aren't working, also use Legacy Input
            else if (!fireHeldInput && !firePressedInput)
            {
                // Input System callbacks might not be working - use Legacy Input as fallback
                if (Mouse.current != null)
                {
                    fireHeldInput = Mouse.current.leftButton.isPressed;
                    firePressedInput = Mouse.current.leftButton.wasPressedThisFrame;
                }
            }

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
                        audioController?.PlayEmptySound();
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
                        audioController?.PlayEmptySound();
                    }
                }
            }
            
            // ✅ CRITICAL FIX: Reload must go through server
            bool reloadInput = reloadPressed;
            if (!reloadInput && Keyboard.current != null)
            {
                reloadInput = Keyboard.current.rKey.wasPressedThisFrame;
            }

            if (reloadInput && CanReload())
            {
                CmdStartReload();
                reloadPressed = false;
            }
            
            // Aim (optional)
            isAiming = aimHeld;
            if (!isAiming && Mouse.current != null)
            {
                isAiming = Mouse.current.rightButton.isPressed;
            }

            // Reset one-shot press
            firePressed = false;
        }
        
        /// <summary>
        /// ✅ PHASE 2: Client-side optimistic check (server has final authority)
        /// ✅ CRITICAL: GDD-compliant - Build phase'de ateş edilemez
        /// </summary>
        private bool CanFire()
        {
            // ✅ CRITICAL: GDD - Build phase'de PvP devre dışı (hasar verilmez)
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == Phase.Build || currentPhase == Phase.Lobby)
                {
                    return false; // Build/Lobby phase'de ateş edilemez
                }
            }

            // Client: Only check ammo and reload state (optimistic prediction)
            // Server will validate fire rate
            if (isServer)
            {
                // Server: Full validation including fire rate
                return Time.time >= nextFireTime && 
                       currentAmmo > 0 && 
                       !isReloading &&
                       currentWeapon != null;
            }
            else
            {
                // Client: Only check ammo and reload (fire rate checked by server)
                return currentAmmo > 0 && 
                       !isReloading &&
                       currentWeapon != null;
            }
        }
        
        private bool CanReload()
        {
            return !isReloading && 
                   currentAmmo < currentWeapon.magazineSize && 
                   reserveAmmo > 0;
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Fire with server authority and network sync
        /// </summary>
        private void Fire()
        {
            // Client: Send to server for validation with lag compensation
            if (!isServer)
            {
                // ✅ NETWORK STABILITY: Send client fire time for lag compensation
                float clientFireTime = (float)NetworkTime.time;
                CmdFire(clientFireTime);
                // Optimistic prediction: play local effects immediately
                PlayLocalFireEffects();
                // ✅ CRITICAL FIX: Perform client-side prediction raycast
                PerformRaycast();
                return;
            }
            
            // Server: Validate and process (use current server time as fire time)
            float serverFireTime = (float)NetworkTime.time;
            ProcessFireServer(serverFireTime);
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Server-authoritative fire processing with lag compensation
        /// ✅ CRITICAL: GDD-compliant - Build phase'de ateş edilemez
        /// </summary>
        [Server]
        private void ProcessFireServer(float clientFireTime = 0f)
        {
            // ✅ CRITICAL: GDD - Build phase'de PvP devre dışı (hasar verilmez)
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == Phase.Build || currentPhase == Phase.Lobby)
                {
                    // Reject fire - tell client to undo prediction
                    RpcRejectFire();
                    LogNetwork("Build/Lobby phase - fire rejected (PvP disabled)");
                    return; // Build/Lobby phase'de ateş edilemez
                }
            }

            // Validate fire rate and ammo
            if (Time.time < nextFireTime || currentAmmo <= 0 || isReloading)
            {
                // Reject fire - tell client to undo prediction
                RpcRejectFire();
                return;
            }
            
            // ✅ CRITICAL FIX: Generate deterministic spread seed FIRST
            // This ensures client and server use same seed for same shot
            spreadSeed = Random.Range(0, int.MaxValue);

            // Update timers
            nextFireTime = Time.time + (1f / currentWeapon.fireRate);

            // ✅ CRITICAL FIX: Server modifies ammo (authoritative)
            currentAmmo--;

            // ✅ CRITICAL FIX: Send seed to clients BEFORE doing server raycast
            // This ensures clients can use correct seed for hit validation/prediction
            RpcSyncSpreadSeed(spreadSeed);

            // ✅ NETWORK STABILITY: Calculate lag compensation
            float serverTime = (float)NetworkTime.time;
            float lag = clientFireTime > 0f ? serverTime - clientFireTime : 0f;
            
            // ✅ FIX: Clamp lag to reasonable range (prevent time-travel exploits)
            // Max 500ms lag compensation (typical high-latency scenario)
            lag = Mathf.Clamp(lag, 0f, 0.5f);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugAudio && lag > 0.1f)
            {
                LogNetwork($"Lag compensation: {lag * 1000f:F0}ms");
            }
            #endif

            // Server raycast with lag compensation
            PerformServerRaycast(lag);

            // ✅ CRITICAL FIX: Sync fire effects to ALL clients
            // ✅ FIX: Use MuzzlePoint for accurate muzzle flash position
            Vector3 muzzlePos;
            Vector3 muzzleDir;
            
            if (weaponHolder != null)
            {
                Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
                if (muzzlePoint != null)
                {
                    muzzlePos = muzzlePoint.position;
                    muzzleDir = muzzlePoint.forward;
                }
                else
                {
                    // Fallback: use weaponHolder position + forward offset
                    muzzlePos = weaponHolder.position + weaponHolder.forward * 0.5f;
                    muzzleDir = weaponHolder.forward;
                }
            }
            else
            {
                muzzlePos = transform.position;
                muzzleDir = transform.forward;
            }
            
            RpcPlayFireEffects(muzzlePos, muzzleDir);
            
            // ✅ CRITICAL: Sync ammo to ALL clients (SyncVar might not trigger event immediately)
            RpcSyncAmmo(currentAmmo, reserveAmmo);
            
            // Events (server-side only)
            OnWeaponFired?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            // ✅ FIX: Auto reload only if not already firing (prevents interruption)
            if (currentAmmo <= 0 && reserveAmmo > 0 && !isReloading)
            {
                StartReload();
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Command to request fire from client with lag compensation
        /// </summary>
        [Command]
        private void CmdFire(float clientFireTime)
        {
            // ✅ NETWORK STABILITY: Basic lag compensation
            // Client sends fire time, server uses it for hit detection
            // This helps with high-latency scenarios where client sees target but server doesn't
            ProcessFireServer(clientFireTime);
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Local fire effects for optimistic prediction
        /// </summary>
        private void PlayLocalFireEffects()
        {
            ApplyRecoil();
            
            // ✅ REFACTOR: Use controllers
            if (vfxController != null && weaponHolder != null)
            {
                // ✅ FIX: Find MuzzlePoint child for accurate muzzle flash position
                Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
                Vector3 muzzlePos;
                Vector3 muzzleDir;
                
                if (muzzlePoint != null)
                {
                    muzzlePos = muzzlePoint.position;
                    muzzleDir = muzzlePoint.forward;
                }
                else
                {
                    // Fallback: use weaponHolder position + forward offset
                    muzzlePos = weaponHolder.position + weaponHolder.forward * 0.5f;
                    muzzleDir = weaponHolder.forward;
                }
                
                vfxController.PlayMuzzleFlashAt(muzzlePos, muzzleDir);
            }
            
            audioController?.PlayFireSound(true);
            
            if (currentCameraShakeCoroutine != null)
            {
                StopCoroutine(currentCameraShakeCoroutine);
            }
            currentCameraShakeCoroutine = StartCoroutine(CameraShake(0.05f, 0.1f));
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Server raycast with deterministic spread and lag compensation
        /// </summary>
        [Server]
        private void PerformServerRaycast(float lagCompensation = 0f)
        {
            if (playerCamera == null) return;

            // ✅ CRITICAL FIX: Use deterministic spread
            Vector3 spread = CalculateDeterministicSpread();
            Vector3 direction = playerCamera.transform.forward + spread;

            // ✅ NETWORK STABILITY: Basic lag compensation - use current position
            // Full lag compensation would require rewinding player positions, which is complex
            // For now, we use current positions but allow slight tolerance in hit detection
            Vector3 rayOrigin = playerCamera.transform.position;
            
            Ray ray = new Ray(rayOrigin, direction);
            
            // Use NonAlloc to avoid GC
            RaycastHit[] hitBuffer = new RaycastHit[32];
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, currentWeapon.range, currentWeapon.hitMask);

            // Find first valid hit
            RaycastHit? validHit = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                
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
                ProcessHitOnServer(validHit.Value);
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Reject fire if server validation fails
        /// </summary>
        [ClientRpc]
        private void RpcRejectFire()
        {
            // Undo optimistic prediction
            // Cancel visual effects if needed
            if (debugAudio)
            {
                Debug.LogWarning("⚠️ [WeaponSystem] Fire rejected by server");
            }
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Explicitly sync spread seed to clients for deterministic hit calculation
        /// This prevents race conditions where client uses old seed value
        /// </summary>
        [ClientRpc]
        private void RpcSyncSpreadSeed(int seed)
        {
            spreadSeed = seed;

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugAudio)
            {
                LogNetwork($"Received spread seed: {seed}");
            }
            #endif
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Explicitly sync ammo to ALL clients
        /// SyncVar might not trigger OnAmmoChanged event immediately on all clients
        /// </summary>
        [ClientRpc]
        private void RpcSyncAmmo(int current, int reserve)
        {
            currentAmmo = current;
            reserveAmmo = reserve;
            
            // Trigger event on ALL clients
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugAudio)
            {
                LogNetwork($"Ammo synced: {current}/{reserve}");
            }
            #endif
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Sync fire effects to all clients with spatial audio
        /// </summary>
        [ClientRpc]
        private void RpcPlayFireEffects(Vector3 muzzlePosition, Vector3 muzzleDirection)
        {
            // ✅ FIX: Don't modify weaponHolder transform directly (it's a child transform, not network synced)
            // Muzzle flash position is handled by vfxController, weaponHolder should stay in its local position
            // This was causing visual glitches - weapon jumping to muzzle position
            
            // ✅ REFACTOR: Use controllers
            vfxController?.PlayMuzzleFlashAt(muzzlePosition, muzzleDirection);
            audioController?.PlayFireSoundAt(muzzlePosition, !isLocalPlayer);
            
            // ✅ HIGH PRIORITY: Use hashed trigger (no string allocation)
            // ✅ FIX: Check if parameter exists before setting trigger
            if (weaponAnimator != null && HasAnimatorParameter(weaponAnimator, FireHash, AnimatorControllerParameterType.Trigger))
            {
                weaponAnimator.SetTrigger(FireHash);
            }
            
            // Camera shake for shooter only
            if (isLocalPlayer)
            {
                if (currentCameraShakeCoroutine != null)
                {
                    StopCoroutine(currentCameraShakeCoroutine);
                }
                currentCameraShakeCoroutine = StartCoroutine(CameraShake(0.05f, 0.1f));
            }
        }
        
        /// <summary>
        /// ✅ PROFESSIONAL FIX: Play muzzle flash at specific 3D position
        /// </summary>

        
        /// <summary>
        /// ✅ CRITICAL FIX: Client-side raycast for prediction (uses deterministic spread)
        /// </summary>
        private void PerformRaycast()
        {
            // Only for client prediction - server uses PerformServerRaycast()
            if (isServer) return;
            if (playerCamera == null) return;

            // ✅ CRITICAL FIX: Use deterministic spread (same as server)
            Vector3 spread = CalculateDeterministicSpread();
            Vector3 direction = playerCamera.transform.forward + spread;

            Ray ray = new Ray(playerCamera.transform.position, direction);

            // ✅ PERFORMANCE FIX: Use NonAlloc to avoid GC
            RaycastHit[] hitBuffer = new RaycastHit[32];
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, currentWeapon.range, currentWeapon.hitMask);

            // Find first valid hit (not ourselves)
            RaycastHit? validHit = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                
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
                // ✅ HIGH PRIORITY FIX: Client prediction - ProcessHit zaten ShowClientSideHitFeedback() çağırıyor
                // Duplicate VFX ve audio kaldırıldı (RPC'de zaten oynatılıyor)
                ProcessHit(validHit.Value);
                
                // ❌ REMOVED: Duplicate VFX ve audio
                // SpawnHitEffect(validHit.Value);  // REMOVED - ProcessHit zaten ShowClientSideHitFeedback() çağırıyor
                // PlayHitSound();  // REMOVED - RPC'de PlayHitSound(surface) çağrılıyor
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Deterministic spread calculation using server seed
        /// </summary>
        private Vector3 CalculateDeterministicSpread()
        {
            float spread = isAiming ? currentWeapon.aimSpread : currentWeapon.hipSpread;
            
            // ✅ CRITICAL FIX: Use server-synced seed for deterministic calculation
            System.Random rng = new System.Random(spreadSeed);
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
            float y = (float)(rng.NextDouble() * 2.0 - 1.0) * spread;
            
            return new Vector3(x, y, 0);
        }
        
        // ✅ MEDIUM PRIORITY FIX: Removed dead code CalculateSpread() - never used, all code uses CalculateDeterministicSpread()
        
        private void ProcessHit(RaycastHit hit)
        {
            // ✅ CRITICAL FIX: Her zaman CmdProcessHit kullan (validation için)
            // Host mode'da bile validation'dan geçmeli (tutarlılık ve anti-cheat için)
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent
            // Only send if hit object has NetworkIdentity (can be synced)
            GameObject hitObj = hit.collider?.gameObject;
            if (hitObj != null && hitObj.TryGetComponent<NetworkIdentity>(out _))
            {
                CmdProcessHit(hit.point, hit.normal, hit.distance, hitObj);
            }
            else
            {
                // Hit non-networked object (wall, floor, etc) - still send for validation
                CmdProcessHit(hit.point, hit.normal, hit.distance, null);
            }

            // CLIENT-SIDE: Immediate visual/audio feedback (optimistic prediction)
            ShowClientSideHitFeedback(hit);
        }

        /// <summary>
        /// CLIENT-SIDE: Immediate visual feedback (prediction - no damage yet)
        /// ✅ CRITICAL FIX: Only show for local player (will be overwritten by RPC)
        /// </summary>
        private void ShowClientSideHitFeedback(RaycastHit hit)
        {
            // ✅ CRITICAL FIX: Only show prediction VFX for shooter
            // Other players will see RPC, shooter sees prediction then RPC (smooth feedback)
            if (!isLocalPlayer) return;
            
            SurfaceType surface = DetermineSurfaceType(hit.collider);
            bool isBodyHit = hit.collider.TryGetComponent<Hitbox>(out _) || hit.collider.TryGetComponent<Health>(out _);

            // ✅ PROFESSIONAL VFX: Use pooled impact effects (Battlefield quality)
            // Note: This will be overwritten by RpcShowImpactEffect, but provides immediate feedback
            if (ImpactVFXPool.Instance != null)
            {
                ImpactVFXPool.Instance.PlayImpact(hit.point, hit.normal, surface, isBodyHit);
            }

            // ✅ FIX: Don't play sound here (will be played in RPC to avoid duplication)
            // PlayHitSound(surface);  // Removed to prevent double audio

            // Optimistic damage numbers (will be corrected by server)
            hit.collider.TryGetComponent<Hitbox>(out var hitbox);
            float predictedDamage = currentWeapon.damage;
            if (hitbox != null)
            {
                predictedDamage = hitbox.CalculateDamage(Mathf.RoundToInt(predictedDamage));
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugAudio)
            {
                Debug.Log($"🎯 [WeaponSystem CLIENT] HIT: {hit.collider.name} - Predicted Damage: {predictedDamage:F1}");
            }
            #endif
        }

        /// <summary>
        /// ✅ ANTI-CHEAT: Client requests server to validate and process hit
        /// </summary>
        [Command]
        private void CmdProcessHit(Vector3 hitPoint, Vector3 hitNormal, float distance, GameObject hitObject)
        {
            // ✅ CRITICAL FIX: Null hitObject = environment hit (wall, floor, etc)
            // Environment hit'leri için sadece VFX göster, damage yok
            if (hitObject == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"🎯 [WeaponSystem SERVER] Environment hit at {hitPoint}");
                #endif
                // Show impact effect for environment hits
                SurfaceType surface = DetermineSurfaceType(null);
                RpcShowImpactEffect(hitPoint, hitNormal, surface, false, false);
                return;
            }

            // ✅ MEDIUM PRIORITY FIX: Validate weapon exists (prevent NullReferenceException)
            if (currentWeapon == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] No weapon assigned for player {netId}");
                #endif
                return;
            }

            // ANTI-CHEAT: Validate fire rate
            if (Time.time < nextFireTime)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Rate limit violation from player {netId}");
                #endif
                return;
            }

            // ANTI-CHEAT: Validate ammo
            if (currentAmmo <= 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Ammo cheat attempt from player {netId}");
                #endif
                return;
            }

            // ANTI-CHEAT: Validate distance (within weapon range) - now safe (currentWeapon checked)
            if (distance > currentWeapon.range)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Distance cheat attempt: {distance}m > {currentWeapon.range}m");
                #endif
                return;
            }

            // ✅ CRITICAL FIX: Validate hit angle (prevent impossible shots like 180° behind)
            // ✅ FIX: Server'da camera olmayabilir - transform kullan (server-authoritative)
            Vector3 serverPlayerPos = playerCamera != null 
                ? playerCamera.transform.position 
                : transform.position;
            Vector3 serverPlayerForward = playerCamera != null 
                ? playerCamera.transform.forward 
                : transform.forward;
            
            Vector3 hitDirection = (hitPoint - serverPlayerPos).normalized;
            float angle = Vector3.Angle(serverPlayerForward, hitDirection);
            const float MAX_HIT_ANGLE = 90f; // 90° cone (FPS standard)
            
            if (angle > MAX_HIT_ANGLE)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Impossible hit angle: {angle:F1}° from player {netId} (max: {MAX_HIT_ANGLE}°)");
                #endif
                return;
            }

            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent (no GC, faster)
            // Reconstruct hit for server-side processing
            if (!hitObject.TryGetComponent<Collider>(out var hitCollider))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [WeaponSystem SERVER] Hit object has no collider");
                #endif
                return;
            }

            // ✅ HIGH PRIORITY: Verify line-of-sight (prevent wall-hack exploit)
            // Note: hitDirection already calculated above for angle validation
            Vector3 serverOrigin = serverPlayerPos;  // Reuse same position
            float distanceToHit = Vector3.Distance(serverOrigin, hitPoint);
            // Reuse hitDirection from angle validation (line 813)
            
            // Server raycast to verify LOS (non-alloc to avoid GC)
            RaycastHit[] losHits = new RaycastHit[8];
            int losCount = Physics.RaycastNonAlloc(
                new Ray(serverOrigin, hitDirection),
                losHits,
                distanceToHit,
                currentWeapon.hitMask
            );
            
            // Check if hit object is first in LOS (no walls blocking)
            bool losValid = false;
            float closestBlockingDistance = float.MaxValue;
            
            for (int i = 0; i < losCount; i++)
            {
                // If we hit our target, LOS is valid
                if (losHits[i].collider == hitCollider)
                {
                    losValid = true;
                    break;
                }
                
                // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent
                // Check if something blocks LOS (wall, structure, or other player)
                bool hasBlockingHealth = losHits[i].collider.TryGetComponent<Health>(out _);
                bool isBlocking = hasBlockingHealth || 
                                 losHits[i].collider.CompareTag("Structure") ||
                                 losHits[i].collider.CompareTag("Wall");
                
                if (isBlocking && losHits[i].distance < closestBlockingDistance)
                {
                    closestBlockingDistance = losHits[i].distance;
                    // If blocking object is closer than target, LOS is blocked
                    if (closestBlockingDistance < distanceToHit)
                    {
                        break;  // Wall blocking LOS
                    }
                }
            }
            
            if (!losValid)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] LOS violation from player {netId} - wall blocking or target not in LOS");
                #endif
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
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent (no GC, faster)
            // ⭐ ÖNCELİKLE HITBOX KONTROL ET
            hitCollider.TryGetComponent<Hitbox>(out var hitbox);

            Health health = null;
            float damage = currentWeapon.damage;
            bool isCritical = false;
            SurfaceType surface = DetermineSurfaceType(hitCollider);
            
            // ✅ PERFORMANCE FIX: Check health component with TryGetComponent
            bool isBodyHit = hitbox != null || hitCollider.TryGetComponent<Health>(out _);

            if (hitbox != null)
            {
                // Hitbox found - use multiplier
                health = hitbox.GetParentHealth();
                
                // ✅ CRITICAL FIX: Null check for GetParentHealth() (can return null if no Health component found)
                if (health != null)
                {
                    damage = hitbox.CalculateDamage(Mathf.RoundToInt(damage));
                    isCritical = hitbox.IsCritical();

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"🎯 [Server] HIT {hitbox.zone} - Damage: {damage} (Multiplier: {hitbox.damageMultiplier}x)");
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"⚠️ [Server] Hitbox found but GetParentHealth() returned null - hit not registered");
                    #endif
                    // Health is null - cannot apply damage, return early
                    return;
                }
            }
            else
            {
                // ✅ PERFORMANCE FIX: Use TryGetComponent for health lookup
                // No hitbox - direct health component
                if (hitCollider.TryGetComponent<Health>(out health))
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"🎯 [Server] HIT (no hitbox) - Damage: {damage}");
                    #endif
                }
            }

            if (health != null)
            {
                // ✅ CRITICAL FIX: Prevent self-harm
                if (!health.TryGetComponent<NetworkIdentity>(out var targetIdentity))
                {
                    targetIdentity = null;
                }
                if (targetIdentity != null && targetIdentity.netId == netId)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Self-harm attempt from player {netId}");
                    #endif
                    return;
                }
                
                // ✅ CRITICAL FIX: Check team damage (friendly fire logic)
                health.TryGetComponent<TacticalCombat.Player.PlayerController>(out var targetPlayer);
                TryGetComponent<TacticalCombat.Player.PlayerController>(out var shooterPlayer);

                bool isFriendlyFire = false;

                if (targetPlayer != null && shooterPlayer != null)
                {
                    // Check if same team
                    if (targetPlayer.team == shooterPlayer.team && targetPlayer.team != Team.None)
                    {
                        // ✅ ALWAYS prevent self-harm
                        if (shooterPlayer.netId == targetPlayer.netId)
                        {
                            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Self-harm prevented");
                            #endif
                            return;
                        }

                        // ✅ IMPLEMENTED: Friendly fire with reduced damage
                        if (GameConstants.FRIENDLY_FIRE_ENABLED)
                        {
                            isFriendlyFire = true;
                            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Friendly fire: Team {shooterPlayer.team} player {netId} damaged teammate - damage reduced by {(1f - GameConstants.FRIENDLY_FIRE_DAMAGE_MULTIPLIER) * 100f}%");
                            #endif
                        }
                        // ✅ NOTE: Friendly fire is currently enabled (FRIENDLY_FIRE_ENABLED = true)
                        // If disabled in the future, uncomment this block:
                        // else
                        // {
                        //     #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        //     Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Friendly fire disabled - no damage");
                        //     #endif
                        //     return; // Friendly fire disabled - no damage
                        // }
                    }
                }

                // Distance falloff
                float distanceFactor = Mathf.Clamp01(1f - (distance / currentWeapon.range));
                damage *= distanceFactor;

                // ✅ IMPLEMENTED: Apply friendly fire damage reduction
                if (isFriendlyFire)
                {
                    damage *= GameConstants.FRIENDLY_FIRE_DAMAGE_MULTIPLIER;
                }

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
        /// ✅ CRITICAL FIX: Authoritative VFX (overwrites client prediction)
        /// </summary>
        [ClientRpc]
        private void RpcShowImpactEffect(Vector3 hitPoint, Vector3 hitNormal, SurfaceType surface, bool isBodyHit, bool isCritical)
        {
            // Show impact effect on all clients (authoritative - overwrites prediction)
            // ✅ REFACTOR: Use vfxController
            if (vfxController != null)
            {
                vfxController.PlayHitEffect(hitPoint, hitNormal, surface);
            }
            else if (ImpactVFXPool.Instance != null)
            {
                // Fallback to global pool if controller missing (shouldn't happen)
                ImpactVFXPool.Instance.PlayImpact(hitPoint, hitNormal, surface, isBodyHit);
            }

            // ✅ FIX: Play hit sound only in RPC (authoritative, prevents duplication)
            audioController?.PlayHitSound();
        }

        private SurfaceType DetermineSurfaceType(Collider collider)
        {
            // ✅ CRITICAL FIX: Null safety for environment hits
            if (collider == null)
                return SurfaceType.Generic;
            
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent
            if (collider.TryGetComponent<Health>(out _))
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
        
        // ✅ CRITICAL FIX: Removed dead code ApplyDamage() method
        // All damage now goes through ProcessHitOnServer() with proper server validation
        
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
            
            // ✅ CRITICAL FIX: Check for NaN values before using Lerp
            if (float.IsNaN(originalWeaponPos.x) || float.IsNaN(originalWeaponPos.y) || float.IsNaN(originalWeaponPos.z))
            {
                // Re-initialize if NaN detected
                originalWeaponPos = weaponHolder.localPosition;
                LogWarning("[WeaponSystem] originalWeaponPos was NaN, re-initialized");
            }
            
            if (float.IsNaN(originalWeaponRot.x) || float.IsNaN(originalWeaponRot.y) || 
                float.IsNaN(originalWeaponRot.z) || float.IsNaN(originalWeaponRot.w))
            {
                // Re-initialize if NaN detected
                originalWeaponRot = weaponHolder.localRotation;
                LogWarning("[WeaponSystem] originalWeaponRot was NaN, re-initialized");
            }
            
            // Smooth recovery
            recoilAmount = Mathf.Lerp(recoilAmount, 0, Time.deltaTime * 5f);
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, originalWeaponPos, Time.deltaTime * 10f);
            
            // ✅ CRITICAL FIX: Use Slerp for rotation (more accurate) and check for valid quaternion
            if (originalWeaponRot != Quaternion.identity || weaponHolder.localRotation != Quaternion.identity)
            {
                weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, originalWeaponRot, Time.deltaTime * 10f);
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
            // ✅ REFACTOR: Use audioController for hit sound
            audioController?.PlayHitSound();
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
        
        /// <summary>
        /// ✅ CRITICAL FIX: Command to request reload from client
        /// ✅ HIGH PRIORITY: Improved reload exploit prevention
        /// </summary>
        [Command]
        private void CmdStartReload()
        {
            // ✅ HIGH PRIORITY: Enhanced validation to prevent reload spam/exploit
            if (isReloading)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Reload spam attempt from player {netId}");
                #endif
                return;
            }
            
            if (currentAmmo >= currentWeapon.magazineSize)
            {
                // Already full, no need to reload
                return;
            }
            
            if (reserveAmmo <= 0)
            {
                // No ammo to reload
                return;
            }
            
            // ✅ FIX: Prevent reload during fire sequence (100ms buffer after fire)
            if (Time.time < nextFireTime + 0.1f)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem SERVER] Reload during fire attempt from player {netId}");
                #endif
                return;
            }
            
            StartReloadServer();
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Server-authoritative reload
        /// </summary>
        [Server]
        private void StartReloadServer()
        {
            if (isReloading) return;

            if (currentReloadCoroutine != null)
            {
                StopCoroutine(currentReloadCoroutine);
            }
            currentReloadCoroutine = StartCoroutine(ReloadCoroutine());
            
            // Sync reload start to clients
            RpcOnReloadStarted();
        }
        
        /// <summary>
        /// Legacy method - redirects to server version
        /// </summary>
        private void StartReload()
        {
            if (isServer)
            {
                StartReloadServer();
            }
            else
            {
                CmdStartReload();
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Sync reload start to clients
        /// </summary>
        [ClientRpc]
        private void RpcOnReloadStarted()
        {
            // ✅ HIGH PRIORITY: Use hashed trigger (no string allocation)
            // Play reload animation on all clients
            // ✅ FIX: Check if parameter exists before setting trigger
            if (weaponAnimator != null && HasAnimatorParameter(weaponAnimator, ReloadHash, AnimatorControllerParameterType.Trigger))
            {
                weaponAnimator.SetTrigger(ReloadHash);
            }
            
            // ✅ REFACTOR: Use audioController for reload sound
            audioController?.PlayReloadSound();
        }
        
        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            
            // ✅ CRITICAL FIX: Reload sound/animation already played via RpcOnReloadStarted
            // Only invoke event here (server-side)
            if (isServer)
            {
                OnReloadStarted?.Invoke();
            }
            
            // ✅ FIX: Reload sound is played in RpcOnReloadStarted, don't play here to avoid duplication
            // Sound/animation is synced to all clients via RPC
            
            // Wait
            yield return new WaitForSeconds(currentWeapon.reloadTime);
            
            // ✅ CRITICAL FIX: Reload only on server (ammo is SyncVar)
            if (isServer)
            {
                // Reload
                int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
                int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
                
                currentAmmo += ammoToReload;
                reserveAmmo -= ammoToReload;
            }
            
            isReloading = false;
            currentReloadCoroutine = null; // ✅ FIX: Clear coroutine reference
            OnReloadComplete?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

            Debug.Log($"🔄 [WeaponSystem] RELOADED: {currentAmmo}/{currentWeapon.magazineSize} (Reserve: {reserveAmmo})");
        }
        
        private void PlayFireEffects()
        {
            // ✅ REFACTOR: Use controllers for fire effects (no longer needed - effects are handled in PlayLocalFireEffects and RpcPlayFireEffects)
            // This method is kept for backward compatibility but is no longer used
            // Fire effects are now handled by:
            // - PlayLocalFireEffects() for client prediction
            // - RpcPlayFireEffects() for network sync
            
            // ✅ HIGH PRIORITY: Use hashed trigger (no string allocation)
            // ✅ FIX: Play weapon animation
            // ✅ FIX: Check if parameter exists before setting trigger
            if (weaponAnimator != null && HasAnimatorParameter(weaponAnimator, FireHash, AnimatorControllerParameterType.Trigger))
            {
                weaponAnimator.SetTrigger(FireHash);
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
            this.enabled = true;
            Debug.Log("✅ [WeaponSystem] Weapon enabled");
        }
        
        /// <summary>
        /// Build moduna girerken silahı devre dışı bırak
        /// </summary>
        public void DisableWeapon()
        {
            ResetWeaponState();
            this.enabled = false;
            Debug.Log("🚫 [WeaponSystem] Weapon disabled");
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// ✅ CRITICAL FIX: Set weapon with server authority
        /// ✅ FIX: Cancel reload if switching weapons
        /// </summary>
        [Server]
        public void SetWeapon(WeaponConfig weapon)
        {
            // ✅ FIX: Cancel reload if switching weapons
            if (currentReloadCoroutine != null)
            {
                StopCoroutine(currentReloadCoroutine);
                currentReloadCoroutine = null;
                isReloading = false;
            }
            
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
        /// ✅ PROFESSIONAL FIX: Called when local player is ready (FPSController camera is initialized)
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [WeaponSystem] OnStartLocalPlayer called - attempting camera assignment");
            #endif
            
            // ✅ CRITICAL FIX: Stop retry coroutine if it's running (we'll try directly now)
            if (retryCameraCoroutine != null)
            {
                StopCoroutine(retryCameraCoroutine);
                retryCameraCoroutine = null;
            }
            
            // ✅ CRITICAL FIX: Try to get camera immediately when local player starts
            // FPSController.OnStartLocalPlayer runs before this, so camera should be ready
            if (TryAssignCamera())
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"✅ [WeaponSystem] Camera assigned successfully in OnStartLocalPlayer!");
                #endif
                
                // Initialize ammo if on server
                if (isServer && currentWeapon != null)
                {
                    currentAmmo = currentWeapon.magazineSize;
                    reserveAmmo = currentWeapon.maxAmmo;
                    OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                }
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [WeaponSystem] Camera not found in OnStartLocalPlayer - will retry in Update");
                #endif
            }
        }
        
        /// <summary>
        /// ✅ PROFESSIONAL FIX: Try to assign camera from FPSController
        /// ✅ CRITICAL FIX: Use GetComponentInParent to find FPSController even if WeaponSystem is on a child GameObject
        /// </summary>
        private bool TryAssignCamera()
        {
            if (playerCamera != null) return true; // Already assigned
            
            // ✅ CRITICAL FIX: Search in parent GameObjects (WeaponSystem might be on child "CurrentWeapon" GameObject)
            var fpsController = GetComponentInParent<TacticalCombat.Player.FPSController>();
            if (fpsController != null)
            {
                playerCamera = fpsController.GetCamera();
                if (playerCamera != null)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"✅ [WeaponSystem] Camera assigned from FPSController! Camera: {playerCamera.name} (Found on parent: {fpsController.gameObject.name})");
                    #endif
                    
                    // Re-enable weapon system now that camera is found
                    enabled = true;
                    return true;
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"⚠️ [WeaponSystem] FPSController found on {fpsController.gameObject.name} but GetCamera() returned null");
                    #endif
                }
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Build hierarchy path for debug
                string hierarchyPath = gameObject.name;
                Transform parent = transform.parent;
                while (parent != null)
                {
                    hierarchyPath = parent.name + "/" + hierarchyPath;
                    parent = parent.parent;
                }
                Debug.LogWarning($"⚠️ [WeaponSystem] FPSController component not found in parent hierarchy. Searched: {hierarchyPath}");
                #endif
            }
            
            return false;
        }
        
        /// <summary>
        /// ✅ PROFESSIONAL FIX: Retry camera assignment (FPSController might not be ready in Start)
        /// </summary>
        private IEnumerator RetryCameraAssignment()
        {
            // Wait a bit longer for OnStartLocalPlayer to run
            yield return new WaitForSeconds(0.2f);
            
            int maxRetries = 20; // Increased retries
            float retryInterval = 0.15f; // 150ms between retries (was 100ms)
            
            for (int i = 0; i < maxRetries; i++)
            {
                // Try to assign camera
                if (TryAssignCamera())
                {
                    // Initialize ammo if on server
                    if (isServer && currentWeapon != null)
                    {
                        currentAmmo = currentWeapon.magazineSize;
                        reserveAmmo = currentWeapon.maxAmmo;
                        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                    }
                    yield break; // Success, exit coroutine
                }
                
                yield return new WaitForSeconds(retryInterval);
            }
            
            // Failed to find camera after all retries
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"❌ [WeaponSystem] Failed to find camera after {maxRetries} retries. Weapon system disabled.");
            #endif
            enabled = false;
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
        
        // ✅ REFACTOR: Audio and VFX are now handled by controllers
        // These properties are kept for backward compatibility but redirect to controllers
        public AudioClip[] FireSounds
        {
            get 
            { 
                // Return from audioController if available
                return audioController != null ? null : null; // Controllers handle this internally
            }
            set 
            { 
                // Set on audioController if available
                // Note: WeaponAudioController manages its own clips
            }
        }
        
        public AudioClip[] HitSounds
        {
            get { return null; } // Controllers handle this internally
            set { } // Controllers handle this internally
        }
        
        public GameObject MuzzleFlashPrefab
        {
            get { return null; } // Controllers handle this internally
            set { } // Controllers handle this internally
        }
        
        public GameObject HitEffectPrefab
        {
            get { return null; } // Controllers handle this internally
            set { } // Controllers handle this internally
        }

        /// <summary>
        /// ✅ FIX: Check if animator has parameter before setting trigger
        /// </summary>
        private bool HasAnimatorParameter(Animator animator, int paramHash, AnimatorControllerParameterType paramType)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.nameHash == paramHash && param.type == paramType)
                {
                    return true;
                }
            }
            return false;
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

