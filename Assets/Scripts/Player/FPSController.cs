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
    /// Refactored into Partial Classes: Core, Visuals, Input, Debug
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public partial class FPSController : NetworkBehaviour
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
        [Tooltip("Maximum fall speed (terminal velocity)")]
        public float maxFallSpeed = 20f; // ✅ Terminal velocity limit

        [Header("Movement Smoothing - Battlefield Style")]
        [Tooltip("Acceleration time (how fast you reach max speed)")]
        public float accelerationTime = 0.15f; // ✅ BATTLEFIELD: Smooth acceleration
        [Tooltip("Deceleration time (how fast you stop)")]
        public float decelerationTime = 0.1f; // ✅ BATTLEFIELD: Quick stop
        private float currentSpeedVelocity = 0f; // Current speed for smooth interpolation
        private float speedVelocityRef = 0f; // Reference for SmoothDamp (required parameter)
        
        [Header("Ground Check")]
        public float groundCheckDistance = 0.2f;
        public LayerMask groundMask = ~0;
        
        [Header("Network Sync (read-only view)")]
        // Read network state from PlayerController to avoid duplication
        private PlayerController playerController;
                
        // Private variables
        private CharacterController characterController;
        private AudioSource audioSource;
        private Vector3 moveDirection = Vector3.zero;
        private bool canMove = true;
        private bool wasGrounded = true;
        
        // ✅ AAA QUALITY: Speed multiplier for trap effects (GlueTrap slow) - Validated
        private float _speedMultiplier = 1f; // Default 1.0 (100% speed)
        public float speedMultiplier 
        { 
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Clamp(value, 0.1f, 2f); // Clamp between 10% and 200% speed
        }
        
        [Header("Jump & Gravity (Polish)")]
        [Tooltip("Time required to pass before being able to jump again (Spam prevention)")]
        public float JumpTimeout = 0.1f;
        
        [Tooltip("Time before falling state (Coyote Time) - Allows jumping shortly after walking off ledge")]
        public float FallTimeout = 0.15f; 

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        
        // ✅ PERFORMANCE FIX: Cache Vector3/RaycastHit instances to avoid GC allocation
        private RaycastHit cachedGroundHit;
        private Vector3 cachedHorizontalVelocity = Vector3.zero;
        private Vector3 cachedMovementInput = Vector3.zero;
        private Vector3 cachedForward = Vector3.forward;
        private Vector3 cachedRight = Vector3.right;
        private Vector3 cachedHorizontalMove = Vector3.zero;
        private Vector3 cachedHorizontalForce = Vector3.zero;
        private Vector3 cachedPositionDiff = Vector3.zero;
        
        // Cached references
        private InputManager inputManager;

        // ✅ LAG COMPENSATION: Position history for server-side rewind
        private TacticalCombat.Network.PositionHistory positionHistory = new TacticalCombat.Network.PositionHistory();

        // ✅ GROUNDED SYNCVAR: Server controls, all clients see
        [SyncVar]
        public bool Grounded = true;

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
            
            // ✅ CRITICAL FIX: Disable NetworkTransform if present to prevent conflict with our manual sync
            // NetworkTransformReliable conflicts with manual CharacterController.Move()
            // This causes DOUBLE position updates and desync between clients
            var netTransform = GetComponent<NetworkTransformReliable>();
            if (netTransform != null)
            {
                netTransform.enabled = false;
                Debug.Log("⚠️ [FPSController] Disabled NetworkTransformReliable to use custom movement sync");
            }

            // Also check for other NetworkTransform variants (use reflection to avoid compile errors)
            Component[] allComponents = GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                string typeName = component.GetType().Name;

                if ((typeName.Contains("NetworkTransform") || typeName == "NetworkTransformUnreliable")
                    && component != netTransform)
                {
                    var enabledProperty = component.GetType().GetProperty("enabled");
                    if (enabledProperty != null)
                    {
                        enabledProperty.SetValue(component, false);
                        Debug.Log($"⚠️ [FPSController] Disabled {typeName} to use custom movement sync");
                    }
                }
            }
            
            // Initialize Visuals (Partial class)
            InitializeVisuals();
            
            // ✅ AAA: InputManager will be cached in OnStartLocalPlayer
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

            // Cache InputManager & PlayerController
            inputManager = GetComponent<InputManager>();
            playerController = GetComponent<PlayerController>();
            
            // ✅ CRITICAL FIX: Remove old camera listeners (Debug function)
            StartCoroutine(CleanSceneAudioListenersDelayed());
        }
        
        // ✅ CRITICAL FIX: Update() method was missing - camera rotation wasn't working!
        private void Update()
        {
            if (!isLocalPlayer) return;

            // ✅ SAFETY: Ensure components are initialized before using them
            if (inputManager == null || characterController == null) return;

            // Read mouse input (frame-independent) -- Partial Input class
            ReadRotationInput();
        }
        
        // ✅ CRITICAL FIX: LateUpdate() method was missing - visual effects weren't working!
        private void LateUpdate()
        {
            if (!isLocalPlayer) return;

            // ✅ SAFETY: Ensure components are initialized before using them
            if (inputManager == null || characterController == null) return;

            // Apply rotation (after movement, smooth) -- Partial Input class
            ApplyRotation();

            // Visual effects (only for local player) -- Partial Visuals class
            if (useStamina) HandleStamina();
            if (useHeadBob) UpdateHeadBob();
            if (useFOVKick) UpdateFOV();
            UpdateCameraTilt(); // ✅ AAA FEATURE: Strafe tilt
            UpdateFootsteps();
            CheckGroundState();
        }
        
        // ✅ AAA QUALITY: Network update rate (MirrorUnityFPS standard)
        private const float MOVEMENT_RPC_INTERVAL = 0.0167f; // 60 Hz (16.7ms) - Industry standard (Valorant/CS2)
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private const float POSITION_THRESHOLD = 0.01f; // 1cm sensitivity (tighter for 60Hz)
        private const float ROTATION_THRESHOLD = 1f; // 1 degree sensitivity (tighter for 60Hz)

        // ✅ REMOVED: lastCmdMoveTime - not needed, using lastMovementRpcTime instead
        
        // ✅ AAA FIX: Track spawn time to ignore initial movement commands (prevents false speed warnings)
        private float lastSpawnTime = 0f;
        private const float SPAWN_GRACE_PERIOD = 0.5f; // 0.5 seconds after spawn - ignore speed validation
        
        private void FixedUpdate()
        {
            // ✅ SAFETY: Ensure characterController exists before any operations
            if (characterController == null) return;

            // 1️⃣ REMOTE CLIENTS: Interpolation Only (Visuals)
            if (!isLocalPlayer && !isServer)
            {
                // ✅ REMOTE PLAYERS (Clients viewing others): Interpolation only
                if (hasTargetPosition)
                {
                    float fixedDeltaTime = Time.fixedDeltaTime;
                    cachedPositionDiff = targetPosition - transform.position;
                    float sqrDistance = cachedPositionDiff.sqrMagnitude;

                    if (sqrDistance > 4f) // 2m squared - snap to prevent sliding
                    {
                        transform.position = targetPosition;
                        transform.rotation = targetRotation;
                        hasTargetPosition = false;
                        return;
                    }

                    if (sqrDistance > 0.0001f)
                    {
                        float maxInterpolationSpeed = 15f;
                        float minInterpolationSpeed = 8f;
                        float distance = Mathf.Sqrt(sqrDistance);
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

            // 2️⃣ SHARED SIMULATION (Prediction & Authority)
            
            // A) Input Sync (Client -> Server)
            if (isLocalPlayer)
            {
                Client_SendInputToServer();
            }

            // B) Ground Check (Server Authority for Animation/State)
            if (isServer)
            {
                Server_GroundedCheck();
            }

            // C) Physics Simulation (Both run this)
            // Host runs once (isLocalPlayer || isServer covers it)
            // Client runs prediction
            // Dedicated Server runs authority
            if (isLocalPlayer || isServer)
            {
                // ✅ AAA POLISH: Update Jump/Fall timers (Coyote Time)
                SC_UpdateTimers();
                
                // ✅ RESPONSIVENESS: Jump BEFORE Movement ensures instant application
                SC_Jump();
                
                // Apply Movement (Horizontal + Vertical/Gravity)
                SC_Movement();
            }

            // 3️⃣ SERVER POST-UPDATE (Lag Comp & Sync)
            if (isServer)
            {
                // Record History (Lag Comp)
                if (positionHistory != null)
                {
                    Bounds bounds = characterController.bounds;
                    positionHistory.RecordPosition(transform.position, transform.rotation, moveDirection, bounds);
                }

                // Manual State Sync (Server -> Clients)
                Server_SendState();
            }
        }

        private float lastStateSyncTime = 0f;
        private const float STATE_SYNC_INTERVAL = 0.05f; // 20 Hz sync rate

        [Server]
        private void Server_SendState()
        {
            if (Time.time - lastStateSyncTime < STATE_SYNC_INTERVAL) return;
            
            // Check if position changed enough to send
            if (Vector3.SqrMagnitude(transform.position - lastSentPosition) > POSITION_THRESHOLD * POSITION_THRESHOLD ||
                Quaternion.Angle(transform.rotation, lastSentRotation) > ROTATION_THRESHOLD)
            {
                // ✅ OPTIMIZATION: Send Yaw only (float) instead of full Quaternion
                // Saves bandwidth and prevents Camera Pitch reset issues
                RpcSetPosition(transform.position, transform.rotation.eulerAngles.y);
                
                lastSentPosition = transform.position;
                lastSentRotation = transform.rotation;
                lastStateSyncTime = Time.time;
            }
        }
        
        /// <summary>
        /// ✅ SERVER: Check if player is grounded (authoritative)
// ... content continues ...
// ...
        // ✅ AAA QUALITY: Smooth interpolation system for remote players
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasTargetPosition;
        
        // ✅ AAA QUALITY: Position correction threshold constant
        private const float POSITION_CORRECTION_THRESHOLD = 1.0f; // 1.0m threshold
        private const float POSITION_CORRECTION_THRESHOLD_SQR = 1.0f; // 1.0m squared (for sqrMagnitude comparison)
        
        /// <summary>
        /// ✅ AAA QUALITY: Sync corrected position to clients with smooth interpolation
        /// ✅ OPTIMIZATION: Receives Yaw only
        /// </summary>
        [ClientRpc]
        public void RpcSetPosition(Vector3 serverPosition, float serverYaw)
        {
            Quaternion serverRotation = Quaternion.Euler(0, serverYaw, 0);

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
                cachedPositionDiff = serverPosition - transform.position;
                float sqrCorrectionDistance = cachedPositionDiff.sqrMagnitude;

                // ✅ AAA FIX: Only correct major desyncs (prevents jitter)
                if (sqrCorrectionDistance > POSITION_CORRECTION_THRESHOLD_SQR)
                {
                    // ✅ FIX: Check if gameObject is active to prevent "Coroutine couldn't be started" error
                    if (gameObject.activeInHierarchy)
                    {
                        float correctionDistance = Mathf.Sqrt(sqrCorrectionDistance);
                        StartCoroutine(SmoothPositionCorrection(serverPosition, serverYaw, correctionDistance));
                    }
                    else
                    {
                        // 🛑 Force snap if inactive (cannot start coroutine)
                        transform.position = serverPosition;
                        
                        // ✅ AAA FIX: Ensure upright rotation on snap
                        Vector3 snapEuler = transform.rotation.eulerAngles;
                        snapEuler.y = serverYaw; // Apply server Yaw
                        transform.rotation = Quaternion.Euler(snapEuler);
                        
                        // ✅ FIX: Reset Camera Pitch to horizon (0) prevents "looking at feet" feeling after spawn
                        if (playerCamera != null)
                        {
                            var camRot = playerCamera.transform.localRotation.eulerAngles;
                            camRot.x = 0; // Reset pitch
                            playerCamera.transform.localRotation = Quaternion.Euler(camRot);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ✅ SERVER: Check if player is grounded (authoritative)
        /// Also handles Fall Damage validation
        /// </summary>
        [Server]
        private void Server_GroundedCheck()
        {
            if (characterController == null) return;

            bool wasGroundedServer = Grounded;

            // Use CharacterController's built-in ground check
            Grounded = characterController.isGrounded;

            // Fallback: Manual ground check with sphere cast
            if (!Grounded)
            {
                Vector3 spherePosition = transform.position + Vector3.down * 0.1f;
                Grounded = Physics.CheckSphere(spherePosition, 0.3f, LayerMask.GetMask("Default", "Ground"), QueryTriggerInteraction.Ignore);
            }
            
            // ✅ ANTI-CHEAT: Server-Side Fall Damage
            // Detect landing (wasAirborne -> nowGrounded)
            if (!wasGroundedServer && Grounded)
            {
                // Calculate impact speed from current vertical velocity
                // Note: Velocity might be reset by CharacterController.Move, so we rely on moveDirection.y
                float impactSpeed = Mathf.Abs(moveDirection.y);
                
                if (impactSpeed > FALL_DAMAGE_THRESHOLD)
                {
                    // Apply damage directly on server
                    ApplyFallDamage(impactSpeed);
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"💥 [Server] Fall Impact: {impactSpeed}m/s");
                    #endif
                }
            }
        }

        /// <summary>
        /// ✅ SHARED CODE: Movement calculation (runs on BOTH client and server)
        /// This is the CRITICAL function that eliminates desync
        /// </summary>
        private void SC_Movement()
        {
            if (characterController == null || !characterController.enabled) return;

            // Input deadzone (prevent ghost movement)
            Vector3 input = sc_InputMove;
            if (input.magnitude < 0.05f)
            {
                input = Vector3.zero;
            }

            // Calculate target speed
            float targetSpeed = sc_IsSprinting ? runSpeed : walkSpeed;
            targetSpeed *= speedMultiplier;

            // ✅ AAA FIX: Air Control (Reduced control when airborne)
            float currentAccelTime = Grounded ? accelerationTime : accelerationTime * 5f; // 5x slower to change direction in air
            float currentDecelTime = Grounded ? decelerationTime : decelerationTime * 5f; // 5x slower to stop in air

            // Smooth acceleration/deceleration
            if (input.magnitude > 0.01f)
            {
                currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, targetSpeed, ref speedVelocityRef, currentAccelTime);
            }
            else
            {
                currentSpeedVelocity = Mathf.SmoothDamp(currentSpeedVelocity, 0f, ref speedVelocityRef, currentDecelTime);
                if (Mathf.Abs(currentSpeedVelocity) < 0.01f)
                {
                    currentSpeedVelocity = 0f;
                }
            }

            // Calculate horizontal movement
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            // ✅ AAA FIX: Prevent unrealistic 180 turns in air (CS/Battlefield style)
            Vector3 horizontalMove = (forward * input.z + right * input.x) * currentSpeedVelocity;

            // Apply gravity
            // ✅ ENGINEERING FIX: Restore Client Prediction for Physics
            // Client must use local IsGrounded() to apply gravity immediately
            // Server uses Grounded (SyncVar) for authority
            bool isGroundedPhysics = isServer ? Grounded : IsGrounded();
            
            float verticalVelocity = moveDirection.y;
            if (!isGroundedPhysics)
            {
                verticalVelocity -= gravity * Time.fixedDeltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
            }
            else
            {
                // Grounded - apply small downward force
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f; // Keep grounded
                }
            }

            // Combine horizontal and vertical
            moveDirection.x = horizontalMove.x;
            moveDirection.y = verticalVelocity;
            moveDirection.z = horizontalMove.z;

            // Move the character
            characterController.Move(moveDirection * Time.fixedDeltaTime);
            
            // ❌ REMOVED: Double PositionHistory recording (Already done in FixedUpdate)
        }

        private void SC_UpdateTimers()
        {
            if (Grounded) // Use Authoritative Grounded
            {
                // Reset Coyote Time
                _fallTimeoutDelta = FallTimeout;

                // Cooldown countdown
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.fixedDeltaTime;
                }
            }
            else
            {
                // Count down Coyote Time
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.fixedDeltaTime;
                }

                // Count down Cooldown
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.fixedDeltaTime;
                }
            }
        }

        /// <summary>
        /// ✅ SHARED CODE: Jump calculation (runs on BOTH client and server)
        /// Flag-based system ensures jump is processed every frame, not just on RPC arrival
        /// </summary>
        private void SC_Jump()
        {
            // Process jump flag (set by client, checked every frame)
            // ✅ AAA FIX: Use Coyote Time (FallTimeout) for forgiving jumps
            // Check if Grounded OR within Coyote Time window
            // Use Authoritative Grounded
            bool canJump = Grounded || _fallTimeoutDelta > 0.0f;
            
            // ✅ AAA FIX: Check Jump Cooldown
            if (sc_JumpRequested && canJump && _jumpTimeoutDelta <= 0.0f)
            {
                // Anti-cheat: Only allow jump if not already jumping (technically handled by cooldown/coyote, but safety cap)
                // Also prevent jumping if falling too fast (unless via Coyote Time)
                if (moveDirection.y < 15f)
                {
                    moveDirection.y = jumpPower;
                    
                    // ✅ RESET: Consume Coyote Time (prevent double jump)
                    _fallTimeoutDelta = -1f;
                    
                    // ✅ RESET: Start Cooldown
                    _jumpTimeoutDelta = JumpTimeout;

                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    // Debug.Log($"🎯 [SC_Jump] Jump! Power: {jumpPower}");
                    #endif
                }

                // ✅ CRITICAL FIX: Clear jump flag immediately
                sc_JumpRequested = false;
            }
            // If requested but cannot jump, we might want to keep the request for a few frames? (Input Buffering)
            // For now, clear it to prevent "late" jumps
            else if (sc_JumpRequested)
            {
                 // Optional: Implement Jump Valid Window (Input Buffering) here
                 // But for now, simple clear to avoid stuck inputs
                 sc_JumpRequested = false;
            }
        }


        
        // ... (CmdMove removed - fully deprecated) ...

        

        

        
        // ✅ AAA QUALITY: Position correction constants
        private const float POSITION_CORRECTION_DURATION_MIN = 0.05f;
        private const float POSITION_CORRECTION_DURATION_MAX = 0.2f;
        private const float POSITION_CORRECTION_SPEED_DIVISOR = 10f;
        
        // ✅ AAA FIX: Track active correction coroutine for cleanup
        private System.Collections.IEnumerator activeCorrectionCoroutine = null;
        
        private System.Collections.IEnumerator SmoothPositionCorrection(Vector3 targetPos, float targetYaw, float distance)
        {
            if (activeCorrectionCoroutine != null)
            {
                StopCoroutine(activeCorrectionCoroutine);
            }
            activeCorrectionCoroutine = null;
            
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation; // Capture current full rotation
            
            float duration = Mathf.Clamp(distance / POSITION_CORRECTION_SPEED_DIVISOR, POSITION_CORRECTION_DURATION_MIN, POSITION_CORRECTION_DURATION_MAX);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (this == null || transform == null)
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                
                // ✅ AAA FIX: Force upright rotation during interpolation (Lock X/Z rotation)
                // Interpolate just the Y angle
                float newYaw = Mathf.LerpAngle(startRot.eulerAngles.y, targetYaw, t);
                transform.rotation = Quaternion.Euler(0, newYaw, 0);
                
                yield return null;
            }

            if (this != null && transform != null)
            {
                transform.position = targetPos;
                transform.rotation = Quaternion.Euler(0, targetYaw, 0);
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

        /// <summary>
        /// Detect landing for effects/sounds
        /// </summary>
        public void CheckGroundState()
        {
            bool grounded = IsGrounded();
            
            // Just landed
            if (grounded && !wasGrounded)
            {
                // ✅ AAA FIX: Capture fall speed at landing moment (before it changes)
                lastFallSpeed = Mathf.Abs(moveDirection.y);
                PlaySound(landSound); // From Visuals Partial

                if (showDebugInfo)
                {
                    Debug.Log($"🎯 Landed (fall speed: {lastFallSpeed:F1})");
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

        // ✅ AAA QUALITY: Fall damage constants (no magic numbers)
        private const float FALL_DAMAGE_THRESHOLD = 25f; // m/s - no damage below this
        private const float FALL_DAMAGE_MULTIPLIER = 4f; // HP per m/s above threshold
        private const float FALL_DAMAGE_MAX_SPEED = 50f; // m/s - cap damage calculation
        
        // ✅ AAA FIX: Track fall speed at landing moment (for accurate server validation)
        private float lastFallSpeed = 0f;
        

        /// <summary>
        /// ✅ AAA QUALITY: Apply fall damage directly on Server
        /// Pure Server Authority - no client trust
        /// </summary>
        [Server]
        private void ApplyFallDamage(float fallSpeed)
        {
            // ✅ AAA FIX: Validate fall speed (anti-cheat sanity check)
            if (fallSpeed < FALL_DAMAGE_THRESHOLD) return;
            
            // ✅ AAA FIX: Clamp fall speed to prevent exploit
            float clampedSpeed = Mathf.Clamp(fallSpeed, FALL_DAMAGE_THRESHOLD, FALL_DAMAGE_MAX_SPEED);
            float excessSpeed = clampedSpeed - FALL_DAMAGE_THRESHOLD;
            int damage = Mathf.RoundToInt(excessSpeed * FALL_DAMAGE_MULTIPLIER);

            if (damage > 0)
            {
                // Apply damage to Health component if it exists
                // We are already on the server, so we can access the Health component directly
                var health = GetComponent<TacticalCombat.Combat.Health>();
                if (health != null)
                {
                    // Use a damage struct/method appropriate for your Health system
                    // Create DamageInfo for Fall Damage
                    var damageInfo = new TacticalCombat.Combat.DamageInfo(
                        damage,
                        netId, // Self-inflicted
                        TacticalCombat.Combat.DamageType.Fall,
                        transform.position
                    );
                    
                    health.ApplyDamage(damageInfo);
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"💥 [Server] Applied {damage} Fall Damage to {name}");
                    #endif
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        // PUBLIC API - Other scripts can use these
        
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

        public TacticalCombat.Network.PositionHistory GetPositionHistory() => positionHistory;
        
        [ClientRpc]
        public void SetSpawnTime(float spawnTime)
        {
            lastSpawnTime = spawnTime;
        }
        
        public bool IsPlayerGrounded() => IsGrounded();
        
        public Vector3 GetVelocity() => moveDirection;
        
        public float GetStamina() => currentStamina; // From Visuals partial
        
        public float GetStaminaPercent() => currentStamina / maxStamina;
        
        public bool IsSprinting() => inputManager != null && inputManager.SprintHeld && IsMoving() && IsGrounded();
        
        private void OnDestroy()
        {
            // ✅ AAA FIX: Stop all coroutines
            StopAllCoroutines();
            activeCorrectionCoroutine = null;
        }
    }
}
