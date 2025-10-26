using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalCombat.Player
{
    /// <summary>
    /// AAA-grade FPS Movement System - Rigidbody-based
    /// Inspired by Battlefield / Modern Warfare
    ///
    /// Features:
    /// - Smooth acceleration/deceleration (no instant speed)
    /// - Slope handling with friction
    /// - Sprint system
    /// - "Heavy" physics feel
    /// - Network-ready (can be extended with Mirror)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyPlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Maximum walk speed in m/s")]
        [SerializeField] private float walkSpeed = 4.5f;

        [Tooltip("Maximum sprint speed in m/s")]
        [SerializeField] private float sprintSpeed = 7.0f;

        [Tooltip("How fast the player accelerates (0-1)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float acceleration = 0.15f;

        [Tooltip("How fast the player decelerates when stopping (0-1)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float deceleration = 0.25f;

        [Tooltip("Air control multiplier (lower = less air control)")]
        [Range(0f, 1f)]
        [SerializeField] private float airControlMultiplier = 0.3f;

        [Header("Jump Settings")]
        [Tooltip("Jump force applied upward")]
        [SerializeField] private float jumpForce = 7f;

        [Tooltip("Gravity multiplier when falling (for better jump feel)")]
        [SerializeField] private float fallGravityMultiplier = 2.5f;

        [Tooltip("Maximum jumps before needing to touch ground")]
        [SerializeField] private int maxJumps = 1;

        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground detection")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("Distance to check for ground (slightly more than capsule radius)")]
        [SerializeField] private float groundCheckDistance = 0.3f;

        [Tooltip("Maximum slope angle player can walk on (degrees)")]
        [SerializeField] private float maxSlopeAngle = 45f;

        [Header("Slope Physics")]
        [Tooltip("Friction force applied when on slopes")]
        [SerializeField] private float slopeFriction = 8f;

        [Tooltip("Minimum slope angle to start applying friction (degrees)")]
        [SerializeField] private float minSlopeFrictionAngle = 15f;

        [Tooltip("Force pushing player down steep slopes")]
        [SerializeField] private float slopeSlideForce = 10f;

        [Header("Physics Feel")]
        [Tooltip("Player mass (affects how 'heavy' movement feels)")]
        [SerializeField] private float playerMass = 80f;

        [Tooltip("Linear drag (air resistance)")]
        [SerializeField] private float linearDrag = 2f;

        [Tooltip("Angular drag (rotation resistance)")]
        [SerializeField] private float angularDrag = 0.05f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        // Components
        private Rigidbody rb;
        private CapsuleCollider capsuleCollider;
        private RigidbodyPlayerCamera playerCamera;

        // Input
        private Vector2 moveInput;
        private bool sprintInput;
        private bool jumpInput;

        // State
        private bool isGrounded;
        private bool wasGrounded;
        private Vector3 groundNormal;
        private float groundAngle;
        private int jumpsRemaining;
        private float currentSpeed;
        private Vector3 currentVelocity;
        private bool isOnSlope;
        private bool isSprinting;

        // Cached values
        private float capsuleRadius;
        private float capsuleHeight;
        private Vector3 capsuleCenter;

        #region Unity Lifecycle

        private void Awake()
        {
            // Get components
            rb = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            playerCamera = GetComponentInChildren<RigidbodyPlayerCamera>();

            // Find camera if not assigned
            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cameraTransform = cam.transform;
                }
                else
                {
                    Debug.LogError("[RigidbodyPlayerMovement] No camera found! Assign cameraTransform.");
                }
            }

            // Cache capsule values
            capsuleRadius = capsuleCollider.radius;
            capsuleHeight = capsuleCollider.height;
            capsuleCenter = capsuleCollider.center;

            // Configure rigidbody
            SetupRigidbody();
        }

        private void FixedUpdate()
        {
            // Ground check FIRST
            CheckGroundStatus();

            // Handle movement physics
            HandleMovement();

            // Handle slope physics
            HandleSlopePhysics();

            // Handle jump
            HandleJump();

            // Update state for camera
            UpdateMovementState();
        }

        #endregion

        #region Setup

        private void SetupRigidbody()
        {
            // Rigidbody configuration for FPS character
            rb.mass = playerMass;
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            rb.freezeRotation = true; // Prevent physics rotation
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Prevent tunneling
            rb.useGravity = true;

            Debug.Log($"✅ [RigidbodyPlayerMovement] Rigidbody configured: Mass={playerMass}kg, Drag={linearDrag}");
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Called by New Input System - Movement input
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Called by New Input System - Sprint input
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
                sprintInput = true;
            else if (context.canceled)
                sprintInput = false;
        }

        /// <summary>
        /// Called by New Input System - Jump input
        /// </summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                jumpInput = true;
        }

        #endregion

        #region Ground Detection

        /// <summary>
        /// Comprehensive ground detection using spherecast
        /// </summary>
        private void CheckGroundStatus()
        {
            wasGrounded = isGrounded;

            // Spherecast from bottom of capsule
            Vector3 origin = transform.position + capsuleCenter + Vector3.up * (capsuleRadius - groundCheckDistance);
            float checkRadius = capsuleRadius * 0.9f; // Slightly smaller to avoid edge cases

            if (Physics.SphereCast(origin, checkRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask))
            {
                isGrounded = true;
                groundNormal = hit.normal;
                groundAngle = Vector3.Angle(Vector3.up, groundNormal);

                // Check if on slope
                isOnSlope = groundAngle > minSlopeFrictionAngle && groundAngle < maxSlopeAngle;

                // Reset jumps when grounded
                if (!wasGrounded)
                {
                    jumpsRemaining = maxJumps;
                    OnLanded();
                }
            }
            else
            {
                isGrounded = false;
                groundNormal = Vector3.up;
                groundAngle = 0f;
                isOnSlope = false;
            }
        }

        private void OnLanded()
        {
            // Called when player lands
            // Can be used for landing sound, animation, etc.
            if (playerCamera != null)
            {
                playerCamera.OnPlayerLanded();
            }
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            // Determine target speed
            bool wantsToSprint = sprintInput && moveInput.y > 0; // Only sprint forward
            float targetSpeed = wantsToSprint ? sprintSpeed : walkSpeed;
            isSprinting = wantsToSprint && isGrounded;

            // Get movement direction relative to camera
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Flatten to horizontal plane
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // Calculate desired movement direction
            Vector3 desiredMoveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            // If on slope, project movement onto slope
            if (isOnSlope && isGrounded)
            {
                desiredMoveDirection = Vector3.ProjectOnPlane(desiredMoveDirection, groundNormal).normalized;
            }

            // Calculate target velocity
            Vector3 targetVelocity = desiredMoveDirection * targetSpeed;

            // Current horizontal velocity
            Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            // Smooth acceleration/deceleration
            float acceleration = moveInput.magnitude > 0.1f ? this.acceleration : this.deceleration;

            // Reduce control in air
            if (!isGrounded)
            {
                acceleration *= airControlMultiplier;
            }

            // Lerp to target velocity (creates smooth acceleration)
            Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, acceleration);

            // Apply horizontal velocity (preserve vertical velocity)
            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);

            // Store current speed for camera effects
            currentSpeed = newHorizontalVelocity.magnitude;
            currentVelocity = rb.linearVelocity;
        }

        private void HandleJump()
        {
            // Check if jump input is pressed and jumps available
            if (jumpInput && jumpsRemaining > 0)
            {
                // Apply jump force
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Reset Y velocity
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                jumpsRemaining--;
                jumpInput = false; // Consume jump input

                Debug.Log($"🦘 Jump! Remaining: {jumpsRemaining}");
            }

            // Better jump feel: increase gravity when falling
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
            }

            // Reset jump input if not pressed (for single jump)
            if (!Input.GetButton("Jump"))
            {
                jumpInput = false;
            }
        }

        #endregion

        #region Slope Physics

        /// <summary>
        /// Apply friction and sliding on slopes for realistic physics
        /// </summary>
        private void HandleSlopePhysics()
        {
            if (!isGrounded || !isOnSlope) return;

            // Apply friction when moving on slopes
            if (moveInput.magnitude > 0.1f)
            {
                // Moving on slope - apply friction proportional to slope angle
                float frictionMultiplier = Mathf.InverseLerp(minSlopeFrictionAngle, maxSlopeAngle, groundAngle);
                Vector3 frictionForce = -rb.linearVelocity.normalized * slopeFriction * frictionMultiplier;
                rb.AddForce(frictionForce, ForceMode.Force);
            }
            else
            {
                // Standing still on slope - prevent sliding (up to a point)
                if (groundAngle < maxSlopeAngle * 0.7f)
                {
                    // Apply strong friction to prevent sliding
                    rb.linearVelocity = new Vector3(
                        rb.linearVelocity.x * 0.9f,
                        rb.linearVelocity.y,
                        rb.linearVelocity.z * 0.9f
                    );
                }
                else
                {
                    // Too steep - player slides down
                    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
                    rb.AddForce(slideDirection * slopeSlideForce, ForceMode.Force);
                }
            }
        }

        #endregion

        #region State Updates

        private void UpdateMovementState()
        {
            // Update camera with movement state
            if (playerCamera != null)
            {
                playerCamera.UpdateMovementState(
                    currentSpeed,
                    isSprinting,
                    isGrounded,
                    moveInput
                );
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current movement speed
        /// </summary>
        public float GetSpeed() => currentSpeed;

        /// <summary>
        /// Get current velocity
        /// </summary>
        public Vector3 GetVelocity() => currentVelocity;

        /// <summary>
        /// Is player grounded?
        /// </summary>
        public bool IsGrounded() => isGrounded;

        /// <summary>
        /// Is player sprinting?
        /// </summary>
        public bool IsSprinting() => isSprinting;

        /// <summary>
        /// Get slope angle player is standing on
        /// </summary>
        public float GetSlopeAngle() => groundAngle;

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Draw ground check sphere
            Vector3 origin = transform.position + capsuleCenter + Vector3.up * (capsuleRadius - groundCheckDistance);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin - Vector3.up * groundCheckDistance, capsuleRadius * 0.9f);

            // Draw ground normal
            if (isGrounded)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, groundNormal * 2f);
            }

            // Draw velocity
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, rb.linearVelocity);
        }

        #endregion
    }
}
