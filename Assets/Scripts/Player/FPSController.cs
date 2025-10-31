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
        
        [Header("Movement")]
        public float walkSpeed = 7f;
        public float runSpeed = 14f;
        public float jumpPower = 10f; // Daha düşük zıplama
        public float gravity = 25f; // Daha güçlü gravity
        
        [Header("Look")]
        public float lookSpeed = 5f;
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

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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

                // 🔧 AUDIO LISTENER FIX: Sadece local player'da AudioListener aktif olsun
                AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    // Sadece local player'da AudioListener aktif
                    audioListener.enabled = isLocalPlayer;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"🔊 AudioListener enabled: {audioListener.enabled} (isLocalPlayer: {isLocalPlayer})");
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
        
        private void Update()
        {
            if (!isLocalPlayer || Time.timeScale == 0f) return;

            HandleRotation();
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer) return;

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
            
            // Uygula
            characterController.Move(moveDirection * Time.fixedDeltaTime);
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
            return new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        }
        
        private Vector3 CalculateHorizontalMovement(Vector3 input)
        {
            if (input.magnitude < 0.1f) return Vector3.zero;
            
            // Get direction vectors
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            // Check if can sprint
            bool wantsToSprint = sprintHeld || Input.GetKey(KeyCode.LeftShift);
            bool canSprint = !useStamina || currentStamina > 0;
            
            // ⭐ Check if sprint is blocked
            if (inputManager != null && inputManager.BlockSprintInput)
            {
                wantsToSprint = false;
            }
            
            bool isRunning = wantsToSprint && canSprint;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            
            Vector3 horizontalMove = (forward * input.z) + (right * input.x);
            return horizontalMove * currentSpeed;
        }
        
        private float CalculateVerticalVelocity()
        {
            bool grounded = IsGrounded();
            
            // ⭐ Check if jump is blocked
            if (inputManager != null && inputManager.BlockJumpInput)
            {
                // Skip jump check
            }
            else if ((jumpPressed || Input.GetButtonDown("Jump")) && canMove && grounded)
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
        
        private void HandleRotation()
        {
            // Check if camera input is blocked
            if (inputManager != null && inputManager.BlockCameraInput)
            {
                return;
            }
            
            // ✅ FIX: Build mode'da kamera çalışsın (Cursor.visible = false ama kamera dönsün)
            if (Cursor.visible && inputManager != null && !inputManager.IsInBuildMode)
            {
                return;
            }
            
            if (!canMove) return;
            
            float mouseX;
            float mouseY;
            if (lookAction != null)
            {
                lookDelta = lookAction.ReadValue<Vector2>();
                mouseX = lookDelta.x;
                mouseY = lookDelta.y;
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }
            
            // Mouse Y -> Camera pitch (up/down)
            rotationX += -mouseY * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            }
            
            // Mouse X -> Player rotation (left/right)
            transform.rotation *= Quaternion.Euler(0, mouseX * lookSpeed, 0);
        }
        
        private void HandleStamina()
        {
            bool sprinting = Input.GetKey(KeyCode.LeftShift) && IsMoving();
            
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
            
            bool sprinting = Input.GetKey(KeyCode.LeftShift) && IsMoving() && IsGrounded();
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
            
            bool sprinting = Input.GetKey(KeyCode.LeftShift);
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
                
                // TODO: Add landing damage for high falls
                // if (fallSpeed > 25f) TakeFallDamage(fallSpeed);
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
        
        public bool IsSprinting() => (sprintHeld || Input.GetKey(KeyCode.LeftShift)) && IsMoving() && IsGrounded();

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
    }
}

