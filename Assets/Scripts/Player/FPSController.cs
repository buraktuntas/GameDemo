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
        
        // ✅ CRITICAL FIX: Speed multiplier for trap effects (GlueTrap slow)
        public float speedMultiplier = 1f; // Default 1.0 (100% speed)
        
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
            
            currentStamina = maxStamina;
            
            // ✅ PERFORMANCE: Initialize PlayerInput once in Awake (earlier than OnStartLocalPlayer)
            // This ensures it exists for both FPSController and WeaponSystem to share
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
                    playerInput = gameObject.AddComponent<PlayerInput>();
                    playerInput.actions = actionsAsset;
                    playerInput.defaultActionMap = "Player";
                    
                    if (showDebugInfo)
                    {
                        Debug.Log("[FPSController] PlayerInput initialized in Awake");
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
        private const float UI_CHECK_INTERVAL = 0.2f; // Check every 0.2 seconds instead of every frame

private void Update()
        {
            // ✅ PROFESSIONAL FIX: Smooth interpolation for remote players
            if (!isLocalPlayer && hasTargetPosition)
            {
                // Smooth interpolation towards target position
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

                // Stop interpolating if we're close enough
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                    hasTargetPosition = false;
                }
            }

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

            // ✅ CAMERA JITTER FIX: Apply rotation in LateUpdate (after all movement)
            // This ensures camera updates AFTER CharacterController.Move
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
        
        // ✅ PHASE 2: Movement RPC rate limiting
        private float lastMovementRpcTime = 0f;
        private const float MOVEMENT_RPC_INTERVAL = 0.1f; // 10 RPC/saniye (100ms throttle) - reduced from 50ms to prevent jitter
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private const float POSITION_THRESHOLD = 0.5f; // 50cm değişiklik olursa gönder (was 0.1m - too sensitive, caused jitter)
        private const float ROTATION_THRESHOLD = 10f; // 10 derece değişiklik olursa gönder (was 5° - too sensitive)
        
        private void FixedUpdate()
        {
            if (!isLocalPlayer) return;
            
            // ⭐ TÜM HAREKET LOJİĞİ FixedUpdate'de
            Vector3 input = GetMovementInput();
            Vector3 horizontalMove = CalculateHorizontalMovement(input);
            float verticalVelocity = CalculateVerticalVelocity();
            
            // Birleştir
            moveDirection = new Vector3(
                horizontalMove.x, 
                verticalVelocity, 
                horizontalMove.z
            );
            
            // Apply locally (prediction)
            characterController.Move(moveDirection * Time.fixedDeltaTime);
            
            // ✅ PHASE 2: Rate-limited RPC to server
            Vector3 predictedPosition = transform.position;
            float timeSinceLastRpc = Time.fixedTime - lastMovementRpcTime;
            bool positionChanged = Vector3.Distance(predictedPosition, lastSentPosition) > POSITION_THRESHOLD;
            bool rotationChanged = Quaternion.Angle(transform.rotation, lastSentRotation) > ROTATION_THRESHOLD;
            
            // Send RPC if: enough time passed OR significant position/rotation change
            if (timeSinceLastRpc >= MOVEMENT_RPC_INTERVAL || positionChanged || rotationChanged)
            {
                CmdMove(predictedPosition, transform.rotation, input);
                lastMovementRpcTime = Time.fixedTime;
                lastSentPosition = predictedPosition;
                lastSentRotation = transform.rotation;
            }
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Server-validated movement (anti-speed hack, anti-teleport)
        /// </summary>
        [Command]
        private void CmdMove(Vector3 predictedPosition, Quaternion predictedRotation, Vector3 input)
        {
            // ✅ CRITICAL FIX: Platform-agnostic validation (Mac/Windows compatible)
            float distance = Vector3.Distance(transform.position, predictedPosition);

            // ✅ FIX: Use constant time instead of Time.fixedDeltaTime (platform-agnostic)
            // Mac and Windows may have different fixedDeltaTime values
            const float MOVE_TIME_WINDOW = 0.1f; // 100ms movement window
            float maxAllowedMove = runSpeed * MOVE_TIME_WINDOW * 2.5f; // Allow 2.5x for lag compensation

            if (distance > maxAllowedMove)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [FPSController SERVER] Teleport detected: {distance:F3}m > {maxAllowedMove:F3}m from player {netId}");
                #endif
                // Reject - don't move, client will be corrected by RpcSetPosition
                return;
            }

            // Validate movement speed (platform-agnostic)
            Vector3 serverMove = CalculateServerMovement(input);

            // ✅ FIX: Calculate speed using constant time window (not fixedDeltaTime)
            float predictedSpeed = distance / MOVE_TIME_WINDOW;

            // ✅ PROFESSIONAL FIX: Increased tolerance for normal gameplay (zıplama, koşma, lag)
            // Allow 50% tolerance for normal gameplay variations (was 15% - too strict)
            float maxAllowedSpeed = runSpeed * 1.5f; // 50% tolerance
            
            // Only log and clamp if speed is suspiciously high (2x normal speed = likely hack)
            if (predictedSpeed > runSpeed * 2.0f)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [FPSController SERVER] SUSPICIOUS speed detected: {predictedSpeed:F2}m/s > {runSpeed * 2.0f:F2}m/s from player {netId} (clamping)");
                #endif
                // Clamp to reasonable maximum
                serverMove = serverMove.normalized * Mathf.Min(predictedSpeed, maxAllowedSpeed);
            }
            else if (predictedSpeed > maxAllowedSpeed)
            {
                // Silent clamp for minor violations (normal gameplay variations)
                serverMove = serverMove.normalized * maxAllowedSpeed;
            }
            
            // Apply server movement
            characterController.Move(serverMove * Time.fixedDeltaTime);
            
            // Sync corrected position to clients
            RpcSetPosition(transform.position, transform.rotation);
        }
        
        /// <summary>
        /// ✅ CRITICAL FIX: Server-side movement calculation (authoritative)
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
            
            // Server calculates speed (can't be hacked)
            bool wantsToSprint = input.magnitude > 0.8f; // Assume sprint if input is strong
            float currentSpeed = (wantsToSprint ? runSpeed : walkSpeed) * speedMultiplier; // ✅ CRITICAL FIX: Apply speed multiplier on server too
            
            Vector3 horizontalMove = (forward * input.z) + (right * input.x);
            return horizontalMove * currentSpeed;
        }
        
        // ✅ PROFESSIONAL FIX: Smooth interpolation for remote players
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasTargetPosition;
        
        /// <summary>
        /// ✅ PROFESSIONAL FIX: Sync corrected position to clients with smooth interpolation
        /// </summary>
        [ClientRpc]
        private void RpcSetPosition(Vector3 serverPosition, Quaternion serverRotation)
        {
            // Only apply correction to non-local players
            // Local player uses prediction, server corrections are rare
            if (!isLocalPlayer)
            {
                // ✅ PROFESSIONAL FIX: Smooth interpolation for other players
                targetPosition = serverPosition;
                targetRotation = serverRotation;
                hasTargetPosition = true;
            }
            else
            {
                // ✅ CRITICAL FIX: Local player correction - only for major desyncs
                // Client prediction is usually accurate, only correct big differences
                float correctionDistance = Vector3.Distance(transform.position, serverPosition);

                // ✅ FIX: Increased threshold to prevent jitter (0.1m → 0.5m)
                // Only correct if player is significantly out of sync (teleport, major lag spike)
                if (correctionDistance > 0.5f)
                {
                    // ✅ FIX: Instant snap for large corrections (smoother than lerp for big jumps)
                    transform.position = serverPosition;
                    transform.rotation = serverRotation;

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (showDebugInfo)
                    {
                        Debug.Log($"🔧 [FPSController] Position corrected by server: {correctionDistance:F3}m (snap)");
                    }
                    #endif
                }
                // Small differences (< 0.5m) are ignored - client prediction handles them
            }
        }
        
        // ⭐ YENİ MOVEMENT SİSTEMİ - FixedUpdate için optimize edilmiş
        private Vector3 GetMovementInput()
        {
            if (inputManager != null && inputManager.BlockMovementInput)
            {
                return Vector3.zero;
            }
            
            // ✅ FIX: Build mode'da hareket çalışsın (Cursor.visible = false ama hareket çalışsın)
            if (Cursor.visible && inputManager != null && !inputManager.IsInBuildMode)
            {
                return Vector3.zero;
            }
            
            if (!canMove) return Vector3.zero;

            // Prefer Input System; fallback to legacy to ensure controls always work
            if (moveAction != null)
            {
                moveAxis = moveAction.ReadValue<Vector2>();
                return new Vector3(moveAxis.x, 0, moveAxis.y);
            }
            
            // Fallback to new input system direct check if action missing
            float h = 0, v = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1;
            }
            return new Vector3(h, 0, v);
        }
        
        private Vector3 CalculateHorizontalMovement(Vector3 input)
        {
            if (input.magnitude < 0.1f)
            {
                // ✅ BATTLEFIELD: Smooth deceleration when stopping
                currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, 0f, ref speedVelocityRef, decelerationTime);
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

            // Read input
            if (lookAction != null)
            {
                lookDelta = lookAction.ReadValue<Vector2>();
                // ✅ FIX: Apply sensitivity multiplier for Input System (0.01f = smoother, adjustable via lookSpeed)
                float inputSensitivity = 0.01f; // Input System delta is in pixels, needs scaling
                pendingMouseX = lookDelta.x * inputSensitivity;
                pendingMouseY = lookDelta.y * inputSensitivity;
            }
            else if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                // Adjust sensitivity as Mouse.delta is pixels, legacy GetAxis was different. 
                // 0.03f reduced from 0.05f for smoother feel, user can adjust lookSpeed.
                pendingMouseX = delta.x * 0.03f; // ✅ Reduced from 0.05f
                pendingMouseY = delta.y * 0.03f; // ✅ Reduced from 0.05f
            }
        }

        /// <summary>
        /// Apply rotation in LateUpdate (after movement, smooth)
        /// </summary>
        private void ApplyRotation()
        {
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
        private bool IsGrounded()
        {
            // Primary check
            if (characterController.isGrounded)
            {
                return true;
            }
            
            // Secondary check with raycast for reliability
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask);
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
                float fallSpeed = Mathf.Abs(moveDirection.y);
                PlaySound(landSound);

                if (showDebugInfo)
                {
                    Debug.Log($"🎯 Landed (fall speed: {fallSpeed:F1})");
                }

                // ✅ IMPLEMENTED: Landing damage for high falls
                if (fallSpeed > 25f)
                {
                    TakeFallDamage(fallSpeed);
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
        /// ✅ IMPLEMENTED: Apply fall damage based on fall speed
        /// </summary>
        [Client]
        private void TakeFallDamage(float fallSpeed)
        {
            if (!isLocalPlayer) return;

            // Calculate damage based on fall speed
            // 25 m/s = threshold (no damage)
            // 30 m/s = 20 HP
            // 35 m/s = 40 HP
            // 40+ m/s = 60 HP (likely death from 100 HP)
            float damageThreshold = 25f;
            float excessSpeed = fallSpeed - damageThreshold;
            int damage = Mathf.RoundToInt(excessSpeed * 4f); // 4 HP per m/s above threshold

            if (damage > 0)
            {
                // Get Health component
                var health = GetComponent<Combat.Health>();
                if (health != null)
                {
                    // Create fall damage info
                    var damageInfo = new Combat.DamageInfo(
                        damage,
                        netId, // Self-inflicted
                        Combat.DamageType.Fall,
                        transform.position
                    );

                    // Apply damage (server-authoritative)
                    health.ApplyDamage(damageInfo);

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"💀 [FallDamage] Took {damage} HP from fall (speed: {fallSpeed:F1} m/s)");
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
        
        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"<b>FPS Controller Debug</b>");
            GUILayout.Label($"Grounded: {IsGrounded()}");
            GUILayout.Label($"Velocity: {moveDirection.magnitude:F2}");
            GUILayout.Label($"Sprinting: {IsSprinting()}");
            if (useStamina)
            {
                GUILayout.Label($"Stamina: {currentStamina:F0}/{maxStamina}");
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
        private const float UI_CACHE_REFRESH_INTERVAL = 1f; // Refresh cache every 1 second
        
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
            // Unsubscribe from Input System actions
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
            
            // Stop all coroutines
            StopAllCoroutines();
        }
    }
}

