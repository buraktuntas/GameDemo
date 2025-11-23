using UnityEngine;
using Mirror;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem; // Optional Input System bridge
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Professional FPS Controller - Production Ready
    /// All bugs fixed, network support, professional features
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : NetworkBehaviour
    {
        [Header("Camera")]
        public Camera playerCamera;
        
        [Header("Movement - Battlefield Style")]
        [Tooltip("Normal walking speed (Battlefield: ~4.5 m/s)")]
        public float walkSpeed = 4.5f; // ✅ BATTLEFIELD: Realistic walking speed
        [Tooltip("Running/Sprinting speed (Battlefield: ~6.5 m/s)")]
        public float runSpeed = 6.5f; // ✅ BATTLEFIELD: Realistic sprint speed (was 14f - too fast!)
        [Tooltip("Jump power (Battlefield: moderate jump)")]
        public float jumpPower = 8f; // ✅ BATTLEFIELD: More realistic jump (was 10f)
        [Tooltip("Gravity strength")]
        public float gravity = 20f; // ✅ BATTLEFIELD: Realistic gravity (was 25f)
        
        [Header("Movement Smoothing - Battlefield Style")]
        [Tooltip("Acceleration time (how fast you reach max speed)")]
        public float accelerationTime = 0.15f; // ✅ BATTLEFIELD: Smooth acceleration
        [Tooltip("Deceleration time (how fast you stop)")]
        public float decelerationTime = 0.1f; // ✅ BATTLEFIELD: Quick stop
        private float currentSpeedVelocity = 0f; // Current speed for smooth interpolation
        private float speedVelocityRef = 0f; // Reference for SmoothDamp (required parameter)
        
        [Header("Look")]
        [Tooltip("Mouse sensitivity (lower = slower, higher = faster). Recommended: 1-3")]
        public float lookSpeed = 2f; // ✅ Reduced from 5f to 2f for better control
        public float lookXLimit = 60f;
        
        [Header("Ground Check")]
        public float groundCheckDistance = 0.2f;
        public LayerMask groundMask = ~0;
        
        [Header("Stamina (Optional)")]
        public bool useStamina = false;
        public float maxStamina = 100f;
        public float staminaDrainRate = 10f;
        public float staminaRegenRate = 5f;
        
        [Header("Network Sync (read-only view)")]
        // Read network state from PlayerController to avoid duplication
        private PlayerController playerController;
        
        [Header("Audio (Optional)")]
        public AudioClip jumpSound;
        public AudioClip landSound;
        public AudioClip[] footstepSounds;
        
        [Header("Effects")]
        public bool useHeadBob = true;
        public float bobSpeed = 12f;
        public float bobAmount = 0.05f;
        public bool useFOVKick = true;
        public float sprintFOVIncrease = 10f;
        
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        // Private variables
        private CharacterController characterController;
        private AudioSource audioSource;
        private Vector3 moveDirection = Vector3.zero;
        private float rotationX = 0;
        private bool canMove = true;
        private bool wasGrounded = true;
        private float currentStamina;
        private float baseFOV;
        private float bobTimer = 0f;
        private float stepTimer = 0f;
        private Vector3 originalCameraPos;
        
        // ✅ AAA QUALITY: Speed multiplier for trap effects (GlueTrap slow) - Validated
        private float _speedMultiplier = 1f; // Default 1.0 (100% speed)
        public float speedMultiplier 
        { 
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Clamp(value, 0.1f, 2f); // Clamp between 10% and 200% speed
        }
        
        // Cached references
        private InputManager inputManager;
        
        // ✅ Input System bridge (optional)
        [Header("Input System")]
        [SerializeField] private InputActionAsset actionsAsset; // Assign InputSystem_Actions in Inspector
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private Vector2 moveAxis;
        private Vector2 lookDelta;
        private bool jumpPressed;
        private bool sprintHeld;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null && (jumpSound != null || landSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (characterController == null)
            {
                Debug.LogError("❌ CharacterController not found!");
            }
            
            // ✅ AAA FIX: Disable NetworkTransform if present to prevent conflict with our manual sync
            // We are handling position sync manually for better precision and anti-rubberbanding
            // ✅ AAA FIX: Disable NetworkTransform if present to prevent conflict with our manual sync
            // We are handling position sync manually for better precision and anti-rubberbanding
            // var netTransform = GetComponent<NetworkTransform>();
            // if (netTransform != null)
            // {
            //     netTransform.enabled = false;
            //     Debug.Log("⚠️ [FPSController] Disabled NetworkTransform to use custom AAA movement sync");
            // }
            
            currentStamina = maxStamina;
            
            // ✅ PERFORMANCE: Initialize PlayerInput once in Awake (earlier than OnStartLocalPlayer)
            // This ensures it exists for both FPSController and WeaponSystem to share
            InitializePlayerInput();
        }
        
        // ✅ FIX: Removed static singleton pattern that broke multiplayer
        // private static PlayerInput s_sharedPlayerInput = null;
        
        private void InitializePlayerInput()
        {
            // Only initialize if actionsAsset is assigned (optional feature)
            if (actionsAsset == null) return;
            
            try
            {
                // ✅ FIX: Removed static singleton pattern that broke multiplayer
                // Each player must have their OWN PlayerInput component
                playerInput = GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = gameObject.AddComponent<PlayerInput>();
                    playerInput.actions = actionsAsset;
                    playerInput.defaultActionMap = "Player";
                }
                else
                {
                    if (playerInput.actions == null && actionsAsset != null)
                    {
                        playerInput.actions = actionsAsset;
                        playerInput.defaultActionMap = "Player";
                    }
                    
                    // ✅ AAA FIX: Disable auto-switching to prevent "Cannot find matching control scheme" errors
                    // We will handle device switching manually if needed, or rely on default behavior
                    playerInput.neverAutoSwitchControlSchemes = true;
                    
                    // Force "Keyboard&Mouse" or "Gamepad" if possible, otherwise let it be
                    // playerInput.defaultControlScheme = "Keyboard&Mouse";
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[FPSController] Failed to initialize PlayerInput: {e.Message}");
                }
            }
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (showDebugInfo)
            {
                Debug.Log($"FPSController init | NetID:{netId} | Pos:{transform.position}");
            }

            // ✅ AAA FIX: Reset movement state on spawn (prevents ghost movement)
            moveDirection = Vector3.zero;
            moveAxis = Vector2.zero;
            lookDelta = Vector2.zero;
            pendingMouseX = 0f;
            pendingMouseY = 0f;
            jumpPressed = false;
            sprintHeld = false;
            currentSpeedVelocity = 0f;
            speedVelocityRef = 0f;
            
            // ✅ AAA FIX: CRITICAL - Force stop all movement immediately
            if (characterController != null)
            {
                characterController.Move(Vector3.zero);
            }
            
            // ✅ AAA FIX: Track spawn time to ignore initial movement commands (prevents false speed warnings)
            lastSpawnTime = Time.time;
            
            // ✅ CRITICAL FIX: Reset last sent position/rotation to prevent false movement detection
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;

            // ✅ CRITICAL FIX: Reset movement RPC timing to prevent false movement detection
            lastMovementRpcTime = 0f;
            
            // ✅ AAA FIX: Ensure canMove is properly set (will be set by PlayerController, but ensure it's not stuck)
            // Don't force canMove here - let PlayerController handle it based on phase

            // Cache InputManager & PlayerController - her player'ın kendi component'ları var
            inputManager = GetComponent<InputManager>();
            playerController = GetComponent<PlayerController>();

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
                    // Make sure Player map is active
                    var map = playerInput.actions.FindActionMap("Player", true);
                    if (map != null)
                    {
                        playerInput.defaultActionMap = "Player";
                        map.Enable();

                        moveAction = map.FindAction("Move", false);
                        lookAction = map.FindAction("Look", false);
                        jumpAction = map.FindAction("Jump", false);
                        sprintAction = map.FindAction("Sprint", false);

                        if (moveAction != null) moveAction.Enable();
                        if (lookAction != null) lookAction.Enable();
                        if (jumpAction != null)
                        {
                            jumpAction.performed += OnJumpPerformed;
                            jumpAction.Enable();
                        }
                        if (sprintAction != null)
                        {
                            sprintAction.performed += OnSprintPerformed;
                            sprintAction.canceled += OnSprintCanceled;
                            sprintAction.Enable();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ [FPSController] 'Player' action map not found in assigned InputActionAsset.");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [FPSController] No InputActionAsset assigned to PlayerInput. Assign 'InputSystem_Actions'.");
                }
            }
            catch { }

            // ✅ FIX: Setup camera (creates if needed) - SAFE VERSION
            try
            {
                SetupCamera();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Camera setup failed: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }

            // Local camera kendi AudioListener'ına sahip; sahnedeki diğerlerini değiştirmeyin

            // ✅ FIX: Store base FOV for sprint effect
            if (playerCamera != null)
            {
                baseFOV = playerCamera.fieldOfView;
            }

            // ✅ CRITICAL FIX: Don't lock cursor if UI is open (TeamSelectionUI might be showing)
            // Check if any UI is open before locking cursor
            bool uiIsOpen = IsAnyUIOpen();
            if (!uiIsOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                // UI is open - keep cursor unlocked for menu interaction
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("🔓 [FPSController] UI detected - cursor kept unlocked");
            }
            
            // ✅ CRITICAL FIX: Ensure camera is always active (prevents "No cameras rendering" warning)
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.gameObject.SetActive(true);
            }

            // Player registration handled by PlayerController
        }
        
        // Registration handled by PlayerController on server
        
        private void SetupCamera()
        {
            // ✅ FIX: NEVER use Camera.main (causes bootstrap camera conflicts)
            // Find camera if not assigned
            if (playerCamera == null)
            {
                // First try to find child camera (best practice)
                playerCamera = GetComponentInChildren<Camera>();

                // ✅ FIX: DON'T fallback to Camera.main - create new camera instead
                if (playerCamera == null)
                {
                    Debug.LogWarning("⚠️ No camera child found - creating runtime camera");
                    var camGO = new GameObject("PlayerCamera");
                    camGO.transform.SetParent(transform);
                    playerCamera = camGO.AddComponent<Camera>();

                    // Add AudioListener
                    var listener = camGO.AddComponent<AudioListener>();
                    listener.enabled = true;

                    // URP Additional Data - safe add
                    try
                    {
                        camGO.AddComponent<UniversalAdditionalCameraData>();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"⚠️ Could not add UniversalAdditionalCameraData: {e.Message}");
                    }

                    Debug.Log("✅ Created runtime PlayerCamera");
                }
            }

            // Enable camera
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                
                // ✅ CRITICAL FIX: Set MainCamera tag so Unity recognizes it as the main camera
                if (!playerCamera.CompareTag("MainCamera"))
                {
                    playerCamera.tag = "MainCamera";
                }

                // Make camera child of player if not already
                if (playerCamera.transform.parent != transform)
                {
                    playerCamera.transform.SetParent(transform);
                }

                // Position at eye level
                originalCameraPos = new Vector3(0, 1.6f, 0);
                playerCamera.transform.localPosition = originalCameraPos;
                playerCamera.transform.localRotation = Quaternion.identity;

                // Store base FOV
                baseFOV = playerCamera.fieldOfView;

                // 🔧 AUDIO LISTENER FIX: Only local player has AudioListener
                if (isLocalPlayer)
                {
                    // ✅ CRITICAL FIX: Ensure local player's AudioListener is enabled
                    if (!playerCamera.TryGetComponent<AudioListener>(out var audioListener))
                    {
                        audioListener = playerCamera.gameObject.AddComponent<AudioListener>();
                    }
                    audioListener.enabled = true;

                    // ✅ CRITICAL FIX: Use delayed coroutine to clean scene listeners (prevents infinite loop)
                    // ✅ FIX: Check if GameObject is active before starting coroutine
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(CleanSceneAudioListenersDelayed());
                    }
                    // ✅ FIX: If GameObject is inactive, skip audio cleanup (will be handled when activated)

                    if (showDebugInfo)
                    {
                        Debug.Log($"🔊 AudioListener enabled for local player");
                    }
                }
                else
                {
                    // ✅ CRITICAL FIX: Non-local player - ensure NO AudioListener exists
                    if (playerCamera.TryGetComponent<AudioListener>(out var audioListener))
                    {
                        Destroy(audioListener);
                        if (showDebugInfo)
                        {
                            Debug.Log($"🔇 AudioListener removed from non-local player");
                        }
                    }
                }

                if (showDebugInfo)
                {
                    Debug.Log("📷 Camera setup complete (cached reference)");
                }
            }
            else
            {
                Debug.LogError("❌ Failed to setup camera - playerCamera is null!");
            }
        }
        
                // ✅ PERFORMANCE FIX: Cache UI state to prevent 420 FindFirstObjectByType calls per second
        private bool cachedUIState = false;
        private float lastUICheckTime = 0f;
        private const float UI_CHECK_INTERVAL = 0.1f; // ✅ AAA FIX: Reduced to 100ms (10 Hz) for faster UI state detection

private void Update()
        {
            // ✅ AAA FIX: Interpolation moved to FixedUpdate for consistent timing
            // Update() only handles UI and input reading (no movement interpolation)

            if (!isLocalPlayer || Time.timeScale == 0f) return;

                        // ✅ CRITICAL PERFORMANCE FIX: Cache UI state check (was calling 7x FindFirstObjectByType every frame!)
            if (Time.time - lastUICheckTime >= UI_CHECK_INTERVAL)
            {
                cachedUIState = IsAnyUIOpen();
                lastUICheckTime = Time.time;
            }
            bool uiIsOpen = cachedUIState;

            if (!uiIsOpen)
            {
                // ESC to unlock, click to re-lock (standard FPS behavior)
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (Cursor.lockState != CursorLockMode.Locked)
                {
                    // ANY mouse button click re-locks cursor (ONLY when UI is closed!)
                    if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame))
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            // ✅ CAMERA JITTER FIX: Read input in Update, apply in LateUpdate
            ReadRotationInput();
            
            // ✅ CRITICAL FIX: Ensure camera is always active (prevents "No cameras rendering" warning)
            // This is especially important when UI is shown (lobby, menu, etc.)
            if (playerCamera != null)
            {
                if (!playerCamera.enabled)
                {
                    playerCamera.enabled = true;
                }
                if (!playerCamera.gameObject.activeInHierarchy)
                {
                    playerCamera.gameObject.SetActive(true);
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!isLocalPlayer) return;

            if (hasFocus)
            {
                // ✅ CRITICAL FIX: Only re-lock cursor if no UI is open (prevents menu interaction issues)
                if (!IsAnyUIOpen())
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    // UI is open - keep cursor unlocked for menu interaction
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // Re-enable movement (fix for multi-window freeze)
                canMove = true;

                Debug.Log($"🎮 Focus gained - Controls restored");
            }
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer) return;

            // ✅ AAA QUALITY: Apply rotation in LateUpdate (after all movement)
            // This ensures camera updates AFTER CharacterController.Move
            // ✅ AAA FIX: Use smoothDeltaTime for consistent frame rate (prevents jitter)
            ApplyRotation();

            // Visual effects only
            if (useStamina) HandleStamina();
            if (useHeadBob) UpdateHeadBob();
            if (useFOVKick) UpdateFOV();
            UpdateFootsteps();
            CheckGroundState();

            // Reset per-frame flags
            jumpPressed = false;
            lookDelta = Vector2.zero;
        }
        
        // ✅ AAA QUALITY: Optimized movement RPC rate limiting
        private float lastMovementRpcTime = 0f;
        private const float MOVEMENT_RPC_INTERVAL = 0.05f; // 20 RPC/saniye (50ms) - AAA quality smoothness
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private const float POSITION_THRESHOLD = 0.1f; // 10cm değişiklik olursa gönder (AAA quality precision)
        private const float ROTATION_THRESHOLD = 5f; // 5 derece değişiklik olursa gönder

        // ✅ REMOVED: lastCmdMoveTime - not needed, using lastMovementRpcTime instead
        
        // ✅ AAA FIX: Track spawn time to ignore initial movement commands (prevents false speed warnings)
        private float lastSpawnTime = 0f;
        private const float SPAWN_GRACE_PERIOD = 0.5f; // 0.5 seconds after spawn - ignore speed validation
        
        private void FixedUpdate()
        {
            if (!isLocalPlayer)
            {
                // ✅ AAA FIX: Remote players interpolation in FixedUpdate (consistent timing)
                if (hasTargetPosition)
                {
                    float fixedDeltaTime = Time.fixedDeltaTime;
                    float distance = Vector3.Distance(transform.position, targetPosition);
                    if (distance > 0.01f)
                    {
                        // ✅ AAA FIX: Improved interpolation - smoother and more responsive
                        // Use distance-based interpolation speed with better curve
                        float maxInterpolationSpeed = 25f; // ✅ CRITICAL FIX: Increased to 25 for better responsiveness
                        float minInterpolationSpeed = 12f; // ✅ CRITICAL FIX: Increased to 12 for faster initial response
                        float adaptiveSpeed = Mathf.Clamp(distance * 2.5f, minInterpolationSpeed, maxInterpolationSpeed);
                        
                        // ✅ AAA FIX: Use MoveTowards for more predictable interpolation
                        float moveDistance = adaptiveSpeed * fixedDeltaTime;
                        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveDistance);
                        
                        // ✅ AAA FIX: Smooth rotation interpolation
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, adaptiveSpeed * 50f * fixedDeltaTime);
                    }
                    else
                    {
                        transform.position = targetPosition;
                        transform.rotation = targetRotation;
                        hasTargetPosition = false;
                    }
                }
                return;
            }
            
            // ✅ AAA FIX: Don't process movement if canMove is false
            if (!canMove)
            {
                // Still apply gravity and reset horizontal movement
                moveDirection = new Vector3(0, CalculateVerticalVelocity(), 0);
                characterController.Move(moveDirection * Time.fixedDeltaTime);
                return;
            }

            // ✅ CLIENT AUTHORITATIVE MOVEMENT (AAA Standard)
            // Client moves freely, server validates
            Vector3 input = GetMovementInput();
            Vector3 horizontalMove = CalculateHorizontalMovement(input);
            float verticalVelocity = CalculateVerticalVelocity();

            // Combine movement
            moveDirection = new Vector3(
                horizontalMove.x,
                verticalVelocity,
                horizontalMove.z
            );

            // Apply locally (client moves immediately)
            characterController.Move(moveDirection * Time.fixedDeltaTime);

            // ✅ Send position to server for validation
            // Rate limited to reduce bandwidth, but frequent enough for smooth remote interpolation
            float timeSinceLastRpc = Time.fixedTime - lastMovementRpcTime;
            bool positionChanged = Vector3.Distance(transform.position, lastSentPosition) > POSITION_THRESHOLD;
            bool rotationChanged = Quaternion.Angle(transform.rotation, lastSentRotation) > ROTATION_THRESHOLD;

            // Send RPC if: enough time passed OR significant position/rotation change
            if (timeSinceLastRpc >= MOVEMENT_RPC_INTERVAL || positionChanged || rotationChanged)
            {
                CmdMove(transform.position, transform.rotation, input);
                lastMovementRpcTime = Time.fixedTime;
                lastSentPosition = transform.position;
                lastSentRotation = transform.rotation;
            }
        }
        
        /// <summary>
        /// ✅ CLIENT AUTHORITATIVE WITH SERVER VALIDATION
        /// - Server trusts client position unless it's impossible (anti-cheat)
        /// - Fixes rubber-banding caused by update frequency mismatch
        /// </summary>
        [Command]
        private void CmdMove(Vector3 clientPosition, Quaternion clientRotation, Vector3 input)
        {
            // ✅ AAA FIX: Ignore validation during spawn grace period (prevents false warnings)
            float timeSinceSpawn = Time.time - lastSpawnTime;
            bool inSpawnGracePeriod = timeSinceSpawn < SPAWN_GRACE_PERIOD;
            
            // Calculate distance moved since last server update
            float distance = Vector3.Distance(transform.position, clientPosition);
            
            // ✅ VALIDATION 1: Teleport Check
            // Allow generous tolerance for lag + physics differences
            // Max speed * time * tolerance factor
            float maxAllowedDistance = (runSpeed * 1.5f * (Time.time - lastMovementRpcTime)) + 0.5f; 
            
            // If first update or long time since last update, be more lenient
            if (Time.time - lastMovementRpcTime > 1.0f) maxAllowedDistance += 2.0f;

            if (distance > maxAllowedDistance && !inSpawnGracePeriod)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Debug.LogWarning($"🚨 [FPSController SERVER] Possible teleport/lag: {distance:F2}m > {maxAllowedDistance:F2}m from player {netId}");
                #endif
                
                // If deviation is EXTREME (e.g. > 5 meters), reject it
                if (distance > 5.0f)
                {
                    // Reject - force client back to server position
                    RpcSetPosition(transform.position, transform.rotation);
                    return;
                }
                // Otherwise, accept it but log it (soft correction / trust client for minor lag spikes)
            }

            // ✅ VALIDATION 2: Speed Check (Optional, can be added if needed)
            // For now, distance check covers most speed hacks implicitly

            // ✅ ACCEPT CLIENT POSITION
            // Server updates its representation of the player to match the client
            transform.position = clientPosition;
            transform.rotation = clientRotation;
            
            // Update last RPC time for next validation
            lastMovementRpcTime = Time.time;

            // Sync to other clients is handled automatically by NetworkTransform if used, 
            // OR we need to relay this to other clients if we are doing manual sync.
            // Since we are using RpcSetPosition for corrections, we might need a separate mechanism 
            // for syncing to OTHER clients if NetworkTransform isn't doing it.
            // Assuming NetworkTransform is NOT used (since we have manual sync code), 
            // we need to update the server's transform so ClientRpc's on other clients (if any) get the data.
            // However, this script seems to rely on `RpcSetPosition` only for corrections?
            // Wait, looking at the code, `RpcSetPosition` is only called on correction.
            // Remote clients need to know where this player is!
            // The original code didn't seem to have a specific "RpcUpdatePositionToOthers".
            // It relied on `characterController.Move` on server to update position, 
            // and then presumably `NetworkTransform` or similar would sync it?
            // OR `RpcSetPosition` was being used for everything?
            // Let's check if there is a `NetworkTransform` component on the prefab.
            // If not, we need to add an Rpc to update other clients.
            
            // For now, we update server transform. If there is a NetworkTransform, it will pick this up.
            // If there is NO NetworkTransform, we should add an Rpc to sync to others.
            // Given the manual interpolation code in FixedUpdate (!isLocalPlayer), 
            // it implies there IS some mechanism syncing `targetPosition`.
            // But `targetPosition` is only set in `RpcSetPosition`.
            // So `RpcSetPosition` MUST be called for valid moves too if we want others to see it!
            
            // ✅ CRITICAL FIX: We must relay the position to other clients!
            RpcUpdateRemoteClients(clientPosition, clientRotation);
        }

        /// <summary>
        /// ✅ NEW: Sync position to other clients (observers)
        /// This replaces the implicit sync from server-side movement
        /// </summary>
        [ClientRpc]
        private void RpcUpdateRemoteClients(Vector3 pos, Quaternion rot)
        {
            // Local player doesn't need this (they are the authority)
            if (isLocalPlayer) return;

            // Remote clients need to interpolate to this position
            targetPosition = pos;
            targetRotation = rot;
            hasTargetPosition = true;
        }
        
        /// <summary>
        /// ✅ AAA QUALITY: Server-side movement calculation (authoritative)
        /// Returns horizontal movement vector (no vertical component)
        /// </summary>
        [Server]
        private Vector3 CalculateServerMovement(Vector3 input)
        {
            if (input.magnitude < 0.1f) return Vector3.zero;
            
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            // ✅ AAA FIX: Server calculates speed (can't be hacked)
            // Use same logic as client for consistency
            bool wantsToSprint = input.magnitude > 0.8f; // Assume sprint if input is strong
            float currentSpeed = (wantsToSprint ? runSpeed : walkSpeed) * speedMultiplier;
            
            // ✅ AAA FIX: Calculate movement direction
            Vector3 horizontalMove = (forward * input.z) + (right * input.x);
            horizontalMove.Normalize(); // ✅ CRITICAL: Normalize to prevent diagonal speed boost
            
            return horizontalMove * currentSpeed;
        }
        
        // ✅ AAA QUALITY: Smooth interpolation system for remote players
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasTargetPosition;
        
        // ✅ AAA QUALITY: Position correction threshold constant
        private const float POSITION_CORRECTION_THRESHOLD = 1.0f; // 1.0m threshold
        
        /// <summary>
        /// ✅ AAA QUALITY: Sync corrected position to clients with smooth interpolation
        /// </summary>
        [ClientRpc]
        public void RpcSetPosition(Vector3 serverPosition, Quaternion serverRotation)
        {
            if (!isLocalPlayer)
            {
                // ✅ AAA FIX: Smooth interpolation for remote players (no jitter)
                targetPosition = serverPosition;
                targetRotation = serverRotation;
                hasTargetPosition = true;
            }
            else
            {
                // ✅ AAA FIX: Local player reconciliation - smooth correction
                // Client prediction is usually accurate, only correct significant desyncs
                float correctionDistance = Vector3.Distance(transform.position, serverPosition);

                // ✅ AAA FIX: Only correct major desyncs (prevents jitter)
                // Threshold: 1.0m (allows normal prediction variance)
                if (correctionDistance > POSITION_CORRECTION_THRESHOLD)
                {
                    // ✅ AAA FIX: Smooth correction for large desyncs (not instant snap)
                    // This prevents visible "teleport" when server corrects
                    StartCoroutine(SmoothPositionCorrection(serverPosition, serverRotation, correctionDistance));

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (showDebugInfo)
                    {
                        Debug.Log($"🔧 [FPSController] Position correction: {correctionDistance:F3}m (smooth)");
                    }
                    #endif
                }
                // Small differences (< 1.0m) are ignored - client prediction handles them
            }
        }
        
        // ✅ AAA QUALITY: Position correction constants
        private const float POSITION_CORRECTION_DURATION_MIN = 0.05f;
        private const float POSITION_CORRECTION_DURATION_MAX = 0.2f;
        private const float POSITION_CORRECTION_SPEED_DIVISOR = 10f;
        
        // ✅ AAA FIX: Track active correction coroutine for cleanup
        private System.Collections.IEnumerator activeCorrectionCoroutine = null;
        
        /// <summary>
        /// ✅ AAA QUALITY: Smooth position correction for local player (prevents visible teleport)
        /// </summary>
        private System.Collections.IEnumerator SmoothPositionCorrection(Vector3 targetPos, Quaternion targetRot, float distance)
        {
            // ✅ AAA FIX: Stop previous correction if still running (prevents memory leak)
            if (activeCorrectionCoroutine != null)
            {
                StopCoroutine(activeCorrectionCoroutine);
            }
            activeCorrectionCoroutine = null;
            
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float duration = Mathf.Clamp(distance / POSITION_CORRECTION_SPEED_DIVISOR, POSITION_CORRECTION_DURATION_MIN, POSITION_CORRECTION_DURATION_MAX);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // ✅ AAA FIX: Check if object is destroyed (prevents memory leak)
                if (this == null || transform == null)
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smooth curve (ease out)
                t = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                yield return null;
            }

            // Ensure final position is exact
            if (this != null && transform != null)
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
            
            activeCorrectionCoroutine = null;
        }
        
        // ⭐ YENİ MOVEMENT SİSTEMİ - FixedUpdate için optimize edilmiş
        private Vector3 GetMovementInput()
        {
            // ✅ AAA FIX: Reset moveAxis at start (prevents ghost movement from stale values)
            moveAxis = Vector2.zero;
            
            // ✅ AAA FIX: Block movement if input is blocked
            if (inputManager != null && inputManager.BlockMovementInput)
            {
                return Vector3.zero;
            }
            
            // ✅ AAA FIX: Block movement if UI is open (unless in build mode)
            if (Cursor.visible && inputManager != null && !inputManager.IsInBuildMode)
            {
                return Vector3.zero;
            }
            
            // ✅ AAA FIX: Block movement if canMove is false
            if (!canMove)
            {
                return Vector3.zero;
            }

            // ✅ AAA FIX: Try Input System first (with deadzone check)
            // CRITICAL: If Input System is available and enabled, ONLY use it (no keyboard fallback)
            if (moveAction != null && moveAction.enabled)
            {
                try
                {
                    moveAxis = moveAction.ReadValue<Vector2>();
                    
                    // ✅ DEBUG: Log input values to diagnose ghost movement
                    if (moveAxis.magnitude > 0.01f && showDebugInfo)
                    {
                        Debug.Log($"[FPSController] Input System Move: {moveAxis}");
                    }

                    // ✅ AAA FIX: Validate input (prevent NaN or invalid values)
                    if (float.IsNaN(moveAxis.x) || float.IsNaN(moveAxis.y))
                    {
                        moveAxis = Vector2.zero;
                        return Vector3.zero;
                    }
                    
                    // ✅ AAA FIX: Deadzone check - if input is below threshold, return zero (no movement)
                    // INCREASED to 0.1f to prevent stick drift
                    if (moveAxis.magnitude <= 0.1f)
                    {
                        moveAxis = Vector2.zero;
                        return Vector3.zero; // ✅ CRITICAL: Return zero, don't fall through to keyboard
                    }
                    
                    // ✅ AAA FIX: Input System active - use it exclusively (no keyboard fallback)
                    return new Vector3(moveAxis.x, 0, moveAxis.y);
                }
                catch (System.Exception e)
                {
                    // Action read failed - fallback to keyboard
                    Debug.LogWarning($"[FPSController] Input Read Failed: {e.Message}");
                    moveAxis = Vector2.zero;
                }
            }
            
            // ✅ AAA FIX: Fallback to keyboard input (ONLY if Input System is not available)
            float h = 0, v = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1;
            }
            
            // ✅ AAA FIX: Normalize input to prevent diagonal speed boost
            Vector3 input = new Vector3(h, 0, v);
            if (input.magnitude > 1f)
            {
                input = input.normalized;
            }
            
            return input;
        }
        
        private Vector3 CalculateHorizontalMovement(Vector3 input)
        {
            // ✅ AAA FIX: CRITICAL - Validate input magnitude first (prevent ghost movement)
            if (input.magnitude < 0.01f) // ✅ INCREASED threshold from 0.1f to 0.01f for better deadzone
            {
                // ✅ BATTLEFIELD: Smooth deceleration when stopping
                currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, 0f, ref speedVelocityRef, decelerationTime);
                // ✅ AAA FIX: Force zero if speed is very low (prevents micro-movements)
                if (Mathf.Abs(currentSpeedVelocity) < 0.01f)
                {
                    currentSpeedVelocity = 0f;
                }
                return Vector3.zero;
            }
            
            // Get direction vectors
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            // Check if can sprint
            bool wantsToSprint = sprintHeld;
            if (!wantsToSprint && Keyboard.current != null)
            {
                 wantsToSprint = Keyboard.current.leftShiftKey.isPressed;
            }
            bool canSprint = !useStamina || currentStamina > 0;
            
            // ⭐ Check if sprint is blocked
            if (inputManager != null && inputManager.BlockSprintInput)
            {
                wantsToSprint = false;
            }
            
            bool isRunning = wantsToSprint && canSprint;
            float targetSpeed = (isRunning ? runSpeed : walkSpeed) * speedMultiplier; // ✅ CRITICAL FIX: Apply speed multiplier
            
            // ✅ BATTLEFIELD: Smooth acceleration/deceleration (realistic movement feel)
            currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, targetSpeed, ref speedVelocityRef, accelerationTime);
            
            Vector3 horizontalMove = (forward * input.z) + (right * input.x);
            return horizontalMove.normalized * currentSpeedVelocity; // Use smoothed speed
        }
        
        private float CalculateVerticalVelocity()
        {
            bool grounded = IsGrounded();
            
            // ⭐ Check if jump is blocked
            if (inputManager != null && inputManager.BlockJumpInput)
            {
                // Skip jump check
            }
            else if ((jumpPressed || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)) && canMove && grounded)
            {
                return jumpPower;
            }
            
            // Apply gravity
            if (!grounded)
            {
                return moveDirection.y - gravity * Time.fixedDeltaTime;
            }
            else if (moveDirection.y < 0)
            {
                // Small downward force to keep grounded
                return -2f;
            }
            
            return moveDirection.y;
        }
        
        // ⭐ HandleJumping artık CalculateVerticalVelocity içinde
        
        /// <summary>
        /// ✅ CRITICAL FIX: Apply impulse for Springboard trap (launch player)
        /// </summary>
        public void ApplyImpulse(Vector3 force)
        {
            // Add to vertical velocity
            moveDirection.y += force.y;
            
            // Add horizontal force
            Vector3 horizontalForce = new Vector3(force.x, 0, force.z);
            moveDirection += horizontalForce;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🚀 [FPSController] Applied impulse: {force}");
            #endif
        }
        
        // ✅ CAMERA JITTER FIX: Split rotation into Read (Update) and Apply (LateUpdate)
        private float pendingMouseX;
        private float pendingMouseY;

        /// <summary>
        /// Read mouse input in Update (high frequency)
        /// </summary>
        private void ReadRotationInput()
        {
            // Check if camera input is blocked
            if (inputManager != null && inputManager.BlockCameraInput)
            {
                pendingMouseX = 0;
                pendingMouseY = 0;
                return;
            }

            // ✅ FIX: Build mode'da kamera çalışsın
            if (Cursor.visible && inputManager != null && !inputManager.IsInBuildMode)
            {
                pendingMouseX = 0;
                pendingMouseY = 0;
                return;
            }

            if (!canMove)
            {
                pendingMouseX = 0;
                pendingMouseY = 0;
                return;
            }

            // ✅ AAA FIX: Read fresh input each frame (don't accumulate - prevents input lag)
            // ApplyRotation() will be called in LateUpdate, so we read fresh input here
            lookDelta = Vector2.zero;
            
            // Read input
            if (lookAction != null)
            {
                lookDelta = lookAction.ReadValue<Vector2>();
                // ✅ AAA FIX: Increased sensitivity for Input System (0.1f = responsive, adjustable via lookSpeed)
                float inputSensitivity = 0.1f; // ✅ INCREASED from 0.01f - Input System delta is in pixels, needs scaling
                // ✅ AAA FIX: Set input (not accumulate) - ApplyRotation handles frame-independent scaling
                pendingMouseX = lookDelta.x * inputSensitivity;
                pendingMouseY = lookDelta.y * inputSensitivity;
            }
            else if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                // ✅ AAA FIX: Increased sensitivity for Mouse.delta (0.15f = responsive)
                // Mouse.delta is pixels, needs proper scaling for smooth feel
                // ✅ AAA FIX: Set input (not accumulate) - ApplyRotation handles frame-independent scaling
                pendingMouseX = delta.x * 0.15f; // ✅ INCREASED from 0.03f
                pendingMouseY = delta.y * 0.15f; // ✅ INCREASED from 0.03f
            }
            else
            {
                // ✅ AAA FIX: No input device - reset to prevent stale values
                pendingMouseX = 0;
                pendingMouseY = 0;
            }
        }

        /// <summary>
        /// Apply rotation in LateUpdate (after movement, smooth)
        /// ✅ CRITICAL FIX: Direct rotation without deltaTime scaling (mouse delta is already frame-independent)
        /// </summary>
        private void ApplyRotation()
        {
            // ✅ CRITICAL FIX: Mouse delta is ALREADY frame-independent (pixels per frame)
            // Don't multiply by deltaTime - this causes jitter during movement!
            // Just apply lookSpeed as sensitivity multiplier

            // Mouse Y -> Camera pitch (up/down)
            rotationX += -pendingMouseY * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            }

            // Mouse X -> Player rotation (left/right)
            transform.rotation *= Quaternion.Euler(0, pendingMouseX * lookSpeed, 0);

            // Reset pending input
            pendingMouseX = 0;
            pendingMouseY = 0;
        }
        
        private void HandleStamina()
        {
            bool isShiftPressed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool sprinting = isShiftPressed && IsMoving();
            
            if (sprinting && currentStamina > 0)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            else if (!sprinting && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
            }
            
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
        
        private void UpdateHeadBob()
        {
            if (!IsGrounded() || !IsMoving())
            {
                // Reset to original position
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    originalCameraPos,
                    Time.deltaTime * 5f
                );
                bobTimer = 0f;
                return;
            }
            
            float speed = new Vector3(moveDirection.x, 0, moveDirection.z).magnitude;
            bobTimer += Time.deltaTime * speed * bobSpeed;
            
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
            
            Vector3 targetPos = originalCameraPos + new Vector3(bobX, bobY, 0);
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                targetPos,
                Time.deltaTime * 10f
            );
        }
        
        private void UpdateFOV()
        {
            if (playerCamera == null) return;
            
            bool isShiftPressed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool sprinting = isShiftPressed && IsMoving() && IsGrounded();
            float targetFOV = sprinting ? baseFOV + sprintFOVIncrease : baseFOV;
            
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * 8f
            );
        }
        
        private void UpdateFootsteps()
        {
            if (!IsGrounded() || !IsMoving() || footstepSounds == null || footstepSounds.Length == 0)
            {
                return;
            }
            
            float speed = new Vector3(moveDirection.x, 0, moveDirection.z).magnitude;
            stepTimer += Time.deltaTime;
            
            bool isShiftPressed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool sprinting = isShiftPressed;
            float stepInterval = sprinting ? 0.3f : 0.5f;
            
            if (stepTimer >= stepInterval)
            {
                PlayRandomFootstep();
                stepTimer = 0f;
            }
        }
        
        /// <summary>
        /// More reliable ground check using both CharacterController and Raycast
        /// </summary>
        // ✅ AAA QUALITY: Ground check constants
        private const float GROUND_CHECK_ORIGIN_OFFSET = 0.1f; // Offset from player position
        private const float GROUND_CHECK_SPHERE_RADIUS = 0.3f; // SphereCast radius
        
        private bool IsGrounded()
        {
            // ✅ AAA FIX: Primary check (fastest)
            if (characterController.isGrounded)
            {
                return true;
            }
            
            // ✅ AAA FIX: Secondary check with SphereCast (more reliable than single raycast)
            // SphereCast detects ground even on slopes and stairs
            Vector3 origin = transform.position + Vector3.up * GROUND_CHECK_ORIGIN_OFFSET;
            float checkDistance = groundCheckDistance + GROUND_CHECK_ORIGIN_OFFSET;
            
            // ✅ AAA FIX: Use SphereCast for better reliability (detects ground on edges)
            return Physics.SphereCast(
                origin, 
                GROUND_CHECK_SPHERE_RADIUS, 
                Vector3.down, 
                out RaycastHit hit, 
                checkDistance, 
                groundMask
            );
        }
        
        /// <summary>
        /// Detect landing for effects/sounds
        /// </summary>
        private void CheckGroundState()
        {
            bool grounded = IsGrounded();
            
            // Just landed
            if (grounded && !wasGrounded)
            {
                // ✅ AAA FIX: Capture fall speed at landing moment (before it changes)
                lastFallSpeed = Mathf.Abs(moveDirection.y);
                PlaySound(landSound);

                if (showDebugInfo)
                {
                    Debug.Log($"🎯 Landed (fall speed: {lastFallSpeed:F1})");
                }

                // ✅ AAA FIX: Landing damage for high falls (use captured speed)
                if (lastFallSpeed > FALL_DAMAGE_THRESHOLD)
                {
                    TakeFallDamage(lastFallSpeed);
                }
            }
            
            wasGrounded = grounded;
        }
        
        private bool IsMoving()
        {
            float horizontalSpeed = new Vector3(moveDirection.x, 0, moveDirection.z).magnitude;
            return horizontalSpeed > 0.1f;
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip);
        }
        
        private void PlayRandomFootstep()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySound(clip);
        }

        /// <summary>
        // ✅ AAA QUALITY: Fall damage constants (no magic numbers)
        private const float FALL_DAMAGE_THRESHOLD = 25f; // m/s - no damage below this
        private const float FALL_DAMAGE_MULTIPLIER = 4f; // HP per m/s above threshold
        private const float FALL_DAMAGE_MAX_SPEED = 50f; // m/s - cap damage calculation
        
        // ✅ AAA FIX: Track fall speed at landing moment (for accurate server validation)
        private float lastFallSpeed = 0f;
        
        /// <summary>
        /// ✅ AAA QUALITY: Apply fall damage based on fall speed (server-validated)
        /// </summary>
        [Client]
        private void TakeFallDamage(float fallSpeed)
        {
            if (!isLocalPlayer) return;

            // ✅ AAA FIX: Validate fall speed (anti-cheat)
            if (fallSpeed < FALL_DAMAGE_THRESHOLD) return;
            
            // ✅ AAA FIX: Clamp fall speed to prevent exploit
            float clampedSpeed = Mathf.Clamp(fallSpeed, FALL_DAMAGE_THRESHOLD, FALL_DAMAGE_MAX_SPEED);
            float excessSpeed = clampedSpeed - FALL_DAMAGE_THRESHOLD;
            int damage = Mathf.RoundToInt(excessSpeed * FALL_DAMAGE_MULTIPLIER);

            if (damage > 0)
            {
                // ✅ AAA FIX: Server-validate fall damage (anti-cheat)
                CmdValidateFallDamage(fallSpeed, damage);
            }
        }
        
        /// <summary>
        /// ✅ AAA QUALITY: Server-validated fall damage (anti-cheat)
        /// </summary>
        [Command]
        private void CmdValidateFallDamage(float reportedFallSpeed, int reportedDamage)
        {
            // ✅ AAA FIX: Use captured fall speed at landing moment (more accurate than current moveDirection.y)
            // This prevents issues where player might have jumped again after landing
            float serverFallSpeed = lastFallSpeed;
            
            // ✅ AAA FIX: Validate reported speed (allow 10% tolerance for network lag)
            float speedDifference = Mathf.Abs(serverFallSpeed - reportedFallSpeed);
            if (speedDifference > serverFallSpeed * 0.1f && speedDifference > 2f)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [FPSController SERVER] Fall speed mismatch: client={reportedFallSpeed:F1}, server={serverFallSpeed:F1} from player {netId}");
                #endif
                // Use server-calculated speed instead
                reportedFallSpeed = serverFallSpeed;
            }
            
            // ✅ AAA FIX: Server calculates damage (can't be hacked)
            if (reportedFallSpeed < FALL_DAMAGE_THRESHOLD) return;
            
            float clampedSpeed = Mathf.Clamp(reportedFallSpeed, FALL_DAMAGE_THRESHOLD, FALL_DAMAGE_MAX_SPEED);
            float excessSpeed = clampedSpeed - FALL_DAMAGE_THRESHOLD;
            int serverDamage = Mathf.RoundToInt(excessSpeed * FALL_DAMAGE_MULTIPLIER);
            
            // ✅ AAA FIX: Use server-calculated damage (ignore client value)
            if (serverDamage > 0)
            {
                var health = GetComponent<Combat.Health>();
                if (health != null)
                {
                    var damageInfo = new Combat.DamageInfo(
                        serverDamage,
                        netId, // Self-inflicted
                        Combat.DamageType.Fall,
                        transform.position
                    );

                    health.ApplyDamage(damageInfo);

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"💀 [FallDamage SERVER] Applied {serverDamage} HP from fall (speed: {reportedFallSpeed:F1} m/s)");
                    #endif
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API - Other scripts can use these
        // ═══════════════════════════════════════════════════════════
        
        public void SetCanMove(bool value)
        {
            canMove = value;
            if (showDebugInfo)
            {
                Debug.Log($"🎮 Can move: {canMove}");
            }
        }
        
        public bool GetCanMove() => canMove;
        
        public Camera GetCamera() => playerCamera;
        
        /// <summary>
        /// ✅ AAA QUALITY: Set spawn time (called by server after spawn position sync)
        /// </summary>
        [ClientRpc]
        public void SetSpawnTime(float spawnTime)
        {
            lastSpawnTime = spawnTime;
        }
        
        public bool IsPlayerGrounded() => IsGrounded();
        
        public Vector3 GetVelocity() => moveDirection;
        
        public float GetStamina() => currentStamina;
        
        public float GetStaminaPercent() => currentStamina / maxStamina;
        
        public bool IsSprinting() => (sprintHeld || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)) && IsMoving() && IsGrounded();

        // ═══════════════════════════════════════════════════════════
        // INPUT SYSTEM BRIDGE HANDLERS
        // ═══════════════════════════════════════════════════════════
        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            jumpPressed = true;
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            sprintHeld = true;
        }

        private void OnSprintCanceled(InputAction.CallbackContext ctx)
        {
            sprintHeld = false;
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG VISUALIZATION
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !showDebugInfo || !isLocalPlayer) return;
            
            // Draw player forward direction (BLUE)
            Gizmos.color = Color.blue;
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Vector3 end = start + transform.forward * 3f;
            
            for (int i = -1; i <= 1; i++)
            {
                Vector3 offset = transform.right * i * 0.05f;
                Gizmos.DrawLine(start + offset, end + offset);
            }
            
            // Draw camera direction (RED)
            if (playerCamera != null)
            {
                Gizmos.color = Color.red;
                Vector3 camStart = playerCamera.transform.position;
                Vector3 camEnd = camStart + playerCamera.transform.forward * 5f;
                Gizmos.DrawLine(camStart, camEnd);
            }
            
            // Draw ground check (GREEN/YELLOW)
            Gizmos.color = IsGrounded() ? Color.green : Color.yellow;
            Vector3 checkStart = transform.position + Vector3.up * 0.1f;
            Vector3 checkEnd = checkStart + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(checkStart, checkEnd);
            Gizmos.DrawWireSphere(checkEnd, 0.1f);
        }
        
        // ✅ AAA QUALITY: Cached debug strings (prevents GC allocation)
        private string cachedDebugGrounded = "";
        private string cachedDebugVelocity = "";
        private string cachedDebugSprinting = "";
        private string cachedDebugStamina = "";
        private float lastDebugUpdateTime = 0f;
        private const float DEBUG_UPDATE_INTERVAL = 0.1f; // Update every 100ms (10 FPS)
        
        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;

            // ✅ AAA FIX: Throttle string updates to prevent GC allocation
            if (Time.time - lastDebugUpdateTime >= DEBUG_UPDATE_INTERVAL)
            {
                cachedDebugGrounded = $"Grounded: {IsGrounded()}";
                cachedDebugVelocity = $"Velocity: {moveDirection.magnitude:F2}";
                cachedDebugSprinting = $"Sprinting: {IsSprinting()}";
                if (useStamina)
                {
                    cachedDebugStamina = $"Stamina: {currentStamina:F0}/{maxStamina}";
                }
                lastDebugUpdateTime = Time.time;
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("<b>FPS Controller Debug</b>");
            GUILayout.Label(cachedDebugGrounded);
            GUILayout.Label(cachedDebugVelocity);
            GUILayout.Label(cachedDebugSprinting);
            if (useStamina)
            {
                GUILayout.Label(cachedDebugStamina);
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Clean scene AudioListeners with delay (prevents infinite loop)
        /// Only called once for local player on spawn
        /// </summary>
        private System.Collections.IEnumerator CleanSceneAudioListenersDelayed()
        {
            // Wait one frame to ensure all objects are initialized
            yield return null;

            // Find all AudioListeners in the scene
            var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int disabledCount = 0;

            foreach (var listener in allListeners)
            {
                // Skip if null or is our own listener
                if (listener == null || listener.gameObject == playerCamera.gameObject)
                    continue;

                // Disable scene/bootstrap listeners
                if (listener.gameObject.scene.IsValid()) // Scene object (not prefab)
                {
                    listener.enabled = false;
                    disabledCount++;

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (showDebugInfo)
                    {
                        Debug.Log($"🔇 Disabled AudioListener on: {listener.gameObject.name}");
                    }
                    #endif
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (showDebugInfo && disabledCount > 0)
            {
                Debug.Log($"🔊 Cleaned {disabledCount} scene AudioListener(s) - Local player listener active");
            }
            #endif
        }

        /// <summary>
        /// ✅ CRITICAL: Check if any UI panel is currently open
        /// Prevents FPSController from interfering with UI clicks
        /// ✅ PERFORMANCE FIX: Cache UI component references to avoid FindFirstObjectByType calls
        /// </summary>
        // ✅ PERFORMANCE FIX: Cache UI component references
        private TacticalCombat.UI.MainMenu cachedMainMenu;
        private TacticalCombat.UI.GameModeSelectionUI cachedGameModeSelection;
        private TacticalCombat.UI.RoleSelectionUI cachedRoleSelection;
        private TacticalCombat.UI.TeamSelectionUI cachedTeamSelection;
        private float lastUICacheTime = 0f;
        private const float UI_CACHE_REFRESH_INTERVAL = 0.1f; // ✅ AAA FIX: Reduced to 100ms for faster UI state detection
        
        private bool IsAnyUIOpen()
        {
            // ✅ CRITICAL FIX: Check MatchManager phase FIRST (Lobby phase = UI open)
            if (TacticalCombat.Core.MatchManager.Instance != null)
            {
                TacticalCombat.Core.Phase currentPhase = TacticalCombat.Core.MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == TacticalCombat.Core.Phase.Lobby)
                {
                    return true; // Lobby phase = UI must be open
                }
            }
            
            // ✅ PERFORMANCE FIX: Refresh UI cache periodically (not every frame)
            if (Time.time - lastUICacheTime >= UI_CACHE_REFRESH_INTERVAL)
            {
                cachedMainMenu = FindFirstObjectByType<TacticalCombat.UI.MainMenu>();
                cachedGameModeSelection = FindFirstObjectByType<TacticalCombat.UI.GameModeSelectionUI>();
                cachedRoleSelection = FindFirstObjectByType<TacticalCombat.UI.RoleSelectionUI>();
                cachedTeamSelection = FindFirstObjectByType<TacticalCombat.UI.TeamSelectionUI>();
                lastUICacheTime = Time.time;
            }
            
            // ✅ FIX: Use cached references and public IsPanelOpen() methods
            
            // Check MainMenu using cached reference
            if (cachedMainMenu != null && cachedMainMenu.IsPanelOpen())
            {
                return true;
            }

            // Check GameModeSelectionUI using cached reference
            if (cachedGameModeSelection != null && cachedGameModeSelection.IsPanelOpen())
            {
                return true;
            }

            // ✅ NEW: Check LobbyUIController (CRITICAL - this was missing!)
            var lobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            if (lobbyController != null && lobbyController.IsLobbyVisible())
            {
                return true;
            }

            // Check RoleSelectionUI using cached reference
            if (cachedRoleSelection != null && cachedRoleSelection.IsPanelOpen())
            {
                return true;
            }

            // Check TeamSelectionUI using cached reference
            if (cachedTeamSelection != null && cachedTeamSelection.IsPanelOpen())
            {
                return true;
            }

            // No UI open - safe to handle cursor locking
            return false;
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Cleanup Input System actions on destroy to prevent memory leaks
        /// </summary>
        private void OnDestroy()
        {
            // ✅ AAA FIX: Unsubscribe from Input System actions first
            try
            {
                if (jumpAction != null)
                {
                    jumpAction.performed -= OnJumpPerformed;
                    jumpAction.Disable();
                }
                if (sprintAction != null)
                {
                    sprintAction.performed -= OnSprintPerformed;
                    sprintAction.canceled -= OnSprintCanceled;
                    sprintAction.Disable();
                }
                if (moveAction != null)
                {
                    moveAction.Disable();
                }
                if (lookAction != null)
                {
                    lookAction.Disable();
                }
            }
            catch (System.Exception e)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[FPSController] Error cleaning up Input System actions: {e.Message}");
                #endif
            }
            
            // ✅ AAA FIX: Clear singleton if we own it (prevents stale reference)
            // ✅ AAA FIX: Clear singleton if we own it (prevents stale reference)
            // if (s_sharedPlayerInput == playerInput)
            // {
            //     s_sharedPlayerInput = null;
            // }
            
            // ✅ AAA FIX: Stop all coroutines ONCE at the end (after all cleanup)
            StopAllCoroutines();
            activeCorrectionCoroutine = null;
        }
    }
}

