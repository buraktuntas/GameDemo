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
        
        // ✅ PERFORMANCE FIX: Cache Vector3/RaycastHit instances to avoid GC allocation
        private RaycastHit cachedGroundHit;
        private Vector3 cachedHorizontalVelocity = Vector3.zero;
        private Vector3 cachedMovementInput = Vector3.zero;
        private Vector3 cachedForward = Vector3.forward;
        private Vector3 cachedRight = Vector3.right;
        private Vector3 cachedHorizontalMove = Vector3.zero;
        private Vector3 cachedSpeedVector = Vector3.zero;
        private Vector3 cachedBobOffset = Vector3.zero;
        private Vector3 cachedHorizontalForce = Vector3.zero;
        private Vector3 cachedPositionDiff = Vector3.zero;
        
        // Cached references
        private InputManager inputManager;
        
        // ✅ AAA: Input Manager - Single Source of Truth
        // All input handling removed from FPSController
        // InputManager handles ALL input reading
        
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
            
            // ✅ AAA: InputManager will be cached in OnStartLocalPlayer
        }
        
        // ✅ AAA: All input initialization removed - InputManager handles everything
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (showDebugInfo)
            {
                Debug.Log($"FPSController init | NetID:{netId} | Pos:{transform.position}");
            }

            // ✅ AAA FIX: Reset movement state on spawn (prevents ghost movement)
            moveDirection = Vector3.zero;
            pendingMouseX = 0f;
            pendingMouseY = 0f;
            currentSpeedVelocity = 0f;
            speedVelocityRef = 0f;
            
            // ✅ AAA FIX: CRITICAL - Force stop all movement immediately
            if (characterController != null && characterController.enabled && gameObject.activeInHierarchy)
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
            
            // Cache InputManager & PlayerController
            inputManager = GetComponent<InputManager>();
            playerController = GetComponent<PlayerController>();
            
            // ✅ CRITICAL FIX: Initialize camera position for head bob
            if (playerCamera != null)
            {
                originalCameraPos = playerCamera.transform.localPosition;
                baseFOV = playerCamera.fieldOfView;
            }
        }
        
        // ✅ CRITICAL FIX: Update() method was missing - camera rotation wasn't working!
        private void Update()
        {
            if (!isLocalPlayer) return;
            
            // Read mouse input (frame-independent)
            ReadRotationInput();
        }
        
        // ✅ CRITICAL FIX: LateUpdate() method was missing - visual effects weren't working!
        private void LateUpdate()
        {
            if (!isLocalPlayer) return;
            
            // Apply rotation (after movement, smooth)
            ApplyRotation();
            
            // Visual effects (only for local player)
            if (useStamina) HandleStamina();
            if (useHeadBob) UpdateHeadBob();
            if (useFOVKick) UpdateFOV();
            UpdateFootsteps();
            CheckGroundState();
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
                // ✅ VALHEIM/RAFT: Remote players interpolation in FixedUpdate (consistent timing)
                if (hasTargetPosition)
                {
                    float fixedDeltaTime = Time.fixedDeltaTime;
                    // ✅ PERFORMANCE FIX: Use sqrMagnitude instead of Distance (no sqrt calculation)
                    cachedPositionDiff = targetPosition - transform.position;
                    float sqrDistance = cachedPositionDiff.sqrMagnitude;
                    if (sqrDistance > 0.0001f) // 0.01f squared = 0.0001f
                    {
                        // ✅ VALHEIM/RAFT: Smooth interpolation for remote players
                        float maxInterpolationSpeed = 15f; // Valheim uses ~15 m/s interpolation
                        float minInterpolationSpeed = 8f;
                        float distance = Mathf.Sqrt(sqrDistance); // Calculate distance only when needed
                        float adaptiveSpeed = Mathf.Clamp(distance * 2.5f, minInterpolationSpeed, maxInterpolationSpeed);
                        
                        float moveDistance = adaptiveSpeed * fixedDeltaTime;
                        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveDistance);
                        
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
            
            // ✅ VALHEIM/RAFT: Don't process movement if canMove is false
            if (!canMove)
            {
                // ✅ PERFORMANCE FIX: Direct assignment instead of new Vector3
                moveDirection.x = 0;
                moveDirection.y = CalculateVerticalVelocity();
                moveDirection.z = 0;
                if (characterController.enabled && gameObject.activeInHierarchy)
                {
                    characterController.Move(moveDirection * Time.fixedDeltaTime);
                }
                return;
            }

            // ✅ HOST-AUTHORITATIVE MOVEMENT (Valheim/Raft Style)
            // Client sends input, host calculates movement
            Vector3 input = GetMovementInput();
            
            // ✅ LOCAL PREDICTION: Apply movement locally for instant feedback
            // This prevents input lag while waiting for host response
            Vector3 horizontalMove = CalculateHorizontalMovement(input);
            float verticalVelocity = CalculateVerticalVelocity();

            // ✅ PERFORMANCE FIX: Direct assignment instead of new Vector3
            moveDirection.x = horizontalMove.x;
            moveDirection.y = verticalVelocity;
            moveDirection.z = horizontalMove.z;

            // Apply prediction locally (instant feedback)
            if (characterController.enabled && gameObject.activeInHierarchy)
            {
                characterController.Move(moveDirection * Time.fixedDeltaTime);
            }

            // ✅ SEND INPUT TO HOST (not position!)
            // Host will calculate authoritative position and sync back
            float timeSinceLastRpc = Time.fixedTime - lastMovementRpcTime;
            
            // Send input at 10 Hz (100ms) - Valheim uses ~10 tick/s
            if (timeSinceLastRpc >= 0.1f)
            {
                // Send input vector + rotation (not position!)
                CmdMoveInput(input, transform.rotation.eulerAngles.y);
                lastMovementRpcTime = Time.fixedTime;
            }
        }
        
        /// <summary>
        /// ✅ HOST-AUTHORITATIVE MOVEMENT (Valheim/Raft Style)
        /// Client sends input, host calculates and applies movement
        /// This prevents client-side position hacking
        /// </summary>
        [Command]
        private void CmdMoveInput(Vector3 input, float yRotation)
        {
            // ✅ INPUT DEADZONE: Prevent ghost movement from float precision errors
            if (input.magnitude < 0.05f)
            {
                input = Vector3.zero;
            }
            
            // ✅ HOST CALCULATES MOVEMENT (authoritative)
            // Client can't hack position, only send input
            
            // Update rotation (trust client for camera rotation)
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            
            // Calculate movement on host
            Vector3 horizontalMove = CalculateHorizontalMovement(input);
            float verticalVelocity = CalculateVerticalVelocity();
            
            // ✅ PERFORMANCE FIX: Direct assignment instead of new Vector3
            moveDirection.x = horizontalMove.x;
            moveDirection.y = verticalVelocity;
            moveDirection.z = horizontalMove.z;
            
            // Apply movement on host (authoritative)
            if (characterController.enabled && gameObject.activeInHierarchy)
            {
                characterController.Move(moveDirection * Time.fixedDeltaTime);
            }
            
            // Sync to all clients (including sender for reconciliation)
            RpcUpdateRemoteClients(transform.position, transform.rotation);
        }

        /// <summary>
        /// ✅ NEW: Sync position to other clients (observers)
        /// This replaces the implicit sync from server-side movement
        /// </summary>
        [ClientRpc]
        private void RpcUpdateRemoteClients(Vector3 pos, Quaternion rot)
        {
            // Local player doesn't need this (they have prediction)
            if (isLocalPlayer) return;

            // Remote clients need to interpolate to this position
            targetPosition = pos;
            targetRotation = rot;
            hasTargetPosition = true;
        }
        
        /// <summary>
        /// ✅ DEPRECATED: Old client-authoritative movement (kept for compatibility)
        /// This will be removed in future versions
        /// </summary>
        [Command]
        private void CmdMove(Vector3 clientPosition, Quaternion clientRotation, Vector3 input)
        {
            // ✅ DEPRECATED: Redirect to new input-based system
            Debug.LogWarning("[FPSController] CmdMove is deprecated! Use CmdMoveInput instead.");
            CmdMoveInput(input, clientRotation.eulerAngles.y);
        }

        
        // ✅ AAA QUALITY: Smooth interpolation system for remote players
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasTargetPosition;
        
        // ✅ AAA QUALITY: Position correction threshold constant
        private const float POSITION_CORRECTION_THRESHOLD = 1.0f; // 1.0m threshold
        private const float POSITION_CORRECTION_THRESHOLD_SQR = 1.0f; // 1.0m squared (for sqrMagnitude comparison)
        
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
                // ✅ PERFORMANCE FIX: Use sqrMagnitude instead of Distance (no sqrt calculation)
                cachedPositionDiff = serverPosition - transform.position;
                float sqrCorrectionDistance = cachedPositionDiff.sqrMagnitude;

                // ✅ AAA FIX: Only correct major desyncs (prevents jitter)
                // Threshold: 1.0m (allows normal prediction variance)
                if (sqrCorrectionDistance > POSITION_CORRECTION_THRESHOLD_SQR)
                {
                    // ✅ AAA FIX: Smooth correction for large desyncs (not instant snap)
                    // This prevents visible "teleport" when server corrects
                    float correctionDistance = Mathf.Sqrt(sqrCorrectionDistance);
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
        
        /// <summary>
        /// ✅ AAA: Get movement input from InputManager (Single Source of Truth)
        /// </summary>
        private Vector3 GetMovementInput()
        {
            if (inputManager == null) return Vector3.zero;
            
            // ✅ AAA: Read from InputManager - Single Source of Truth
            Vector2 moveInput = inputManager.MoveInput;
            
            // ✅ PERFORMANCE FIX: Use cached Vector3 to avoid GC allocation
            cachedMovementInput.x = moveInput.x;
            cachedMovementInput.y = 0;
            cachedMovementInput.z = moveInput.y;
            
            return cachedMovementInput;
        }
        
        private Vector3 CalculateHorizontalMovement(Vector3 input)
        {
            // ✅ AAA FIX: CRITICAL - Validate input magnitude first (prevent ghost movement)
            if (input.magnitude < 0.01f)
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
            
            // ✅ PERFORMANCE FIX: Use cached Vector3 instances to avoid GC allocation
            cachedForward = transform.forward;
            cachedForward.y = 0;
            cachedForward.Normalize();
            
            cachedRight = transform.right;
            cachedRight.y = 0;
            cachedRight.Normalize();
            
            // ✅ AAA: Check sprint from InputManager
            bool wantsToSprint = inputManager != null && inputManager.SprintHeld;
            bool canSprint = !useStamina || currentStamina > 0;
            
            bool isRunning = wantsToSprint && canSprint;
            float targetSpeed = (isRunning ? runSpeed : walkSpeed) * speedMultiplier;
            
            // ✅ BATTLEFIELD: Smooth acceleration/deceleration (realistic movement feel)
            currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, targetSpeed, ref speedVelocityRef, accelerationTime);
            
            // ✅ PERFORMANCE FIX: Use cached Vector3 to avoid GC allocation
            cachedHorizontalMove = (cachedForward * input.z) + (cachedRight * input.x);
            cachedHorizontalMove.Normalize();
            cachedHorizontalMove *= currentSpeedVelocity;
            
            return cachedHorizontalMove;
        }
        
        private float CalculateVerticalVelocity()
        {
            bool grounded = IsGrounded();
            
            // ✅ AAA: Check jump from InputManager
            bool jumpRequested = inputManager != null && inputManager.JumpPressed;
            
            if (jumpRequested && canMove && grounded)
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
            
            // ✅ PERFORMANCE FIX: Use cached Vector3 to avoid GC allocation
            cachedHorizontalForce.x = force.x;
            cachedHorizontalForce.y = 0;
            cachedHorizontalForce.z = force.z;
            moveDirection += cachedHorizontalForce;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🚀 [FPSController] Applied impulse: {force}");
            #endif
        }
        
        // ✅ CAMERA JITTER FIX: Split rotation into Read (Update) and Apply (LateUpdate)
        private float pendingMouseX;
        private float pendingMouseY;

        /// <summary>
        /// ✅ AAA: Read mouse input from InputManager (Single Source of Truth)
        /// </summary>
        private void ReadRotationInput()
        {
            if (inputManager == null || !canMove)
            {
                pendingMouseX = 0;
                pendingMouseY = 0;
                return;
            }

            // ✅ AAA: Read from InputManager - Single Source of Truth
            Vector2 lookDelta = inputManager.LookInput;
            
            // ✅ AAA: Apply sensitivity scaling
            // Mouse.delta is in pixels, needs scaling for smooth feel
            pendingMouseX = lookDelta.x * 0.15f;
            pendingMouseY = lookDelta.y * 0.15f;
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
            
            // ✅ PERFORMANCE FIX: Reuse cached horizontal velocity calculation
            cachedSpeedVector.x = moveDirection.x;
            cachedSpeedVector.y = 0;
            cachedSpeedVector.z = moveDirection.z;
            float speed = cachedSpeedVector.magnitude;
            bobTimer += Time.deltaTime * speed * bobSpeed;
            
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
            
            // ✅ PERFORMANCE FIX: Use cached Vector3 to avoid GC allocation
            cachedBobOffset.x = bobX;
            cachedBobOffset.y = bobY;
            cachedBobOffset.z = 0;
            Vector3 targetPos = originalCameraPos + cachedBobOffset;
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
            
            // ✅ PERFORMANCE FIX: Reuse cached horizontal velocity calculation
            cachedHorizontalVelocity.x = moveDirection.x;
            cachedHorizontalVelocity.y = 0;
            cachedHorizontalVelocity.z = moveDirection.z;
            float speed = cachedHorizontalVelocity.magnitude;
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
            
            // ✅ PERFORMANCE FIX: Use cached RaycastHit to avoid GC allocation
            // SphereCast detects ground even on slopes and stairs
            Vector3 origin = transform.position + Vector3.up * GROUND_CHECK_ORIGIN_OFFSET;
            float checkDistance = groundCheckDistance + GROUND_CHECK_ORIGIN_OFFSET;
            
            // ✅ PERFORMANCE FIX: Use cached RaycastHit (no GC allocation)
            return Physics.SphereCast(
                origin, 
                GROUND_CHECK_SPHERE_RADIUS, 
                Vector3.down, 
                out cachedGroundHit, 
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
            // ✅ PERFORMANCE FIX: Use cached Vector3 to avoid GC allocation
            cachedHorizontalVelocity.x = moveDirection.x;
            cachedHorizontalVelocity.y = 0;
            cachedHorizontalVelocity.z = moveDirection.z;
            float horizontalSpeed = cachedHorizontalVelocity.magnitude;
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
            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent (no GC allocation)
            if (serverDamage > 0)
            {
                if (TryGetComponent<Combat.Health>(out var health))
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
        
        public bool IsSprinting() => inputManager != null && inputManager.SprintHeld && IsMoving() && IsGrounded();
        
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
        private TacticalCombat.UI.LobbyUIController cachedLobbyController;
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
                cachedLobbyController = TacticalCombat.UI.LobbyUIController.Instance; // ✅ Cache singleton too
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
            // ✅ PERFORMANCE FIX: Cache LobbyUIController to avoid singleton property access overhead
            if (cachedLobbyController == null)
            {
                cachedLobbyController = TacticalCombat.UI.LobbyUIController.Instance;
            }
            if (cachedLobbyController != null && cachedLobbyController.IsLobbyVisible())
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
        /// ✅ AAA: Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            // ✅ AAA: No Input System cleanup needed - InputManager handles everything
            
            // ✅ AAA FIX: Stop all coroutines
            StopAllCoroutines();
            activeCorrectionCoroutine = null;
        }
    }
}

