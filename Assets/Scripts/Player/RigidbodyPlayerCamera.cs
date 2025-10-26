using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalCombat.Player
{
    /// <summary>
    /// AAA-grade FPS Camera System - Rigidbody player compatible
    /// Inspired by Battlefield / Modern Warfare
    ///
    /// Features:
    /// - Smooth camera rotation with damping
    /// - Strafe tilt (2-4 degrees)
    /// - Realistic headbob that scales with velocity
    /// - Sprint FOV kick
    /// - Landing impact
    /// </summary>
    public class RigidbodyPlayerCamera : MonoBehaviour
    {
        [Header("Look Settings")]
        [Tooltip("Mouse sensitivity for horizontal rotation")]
        [SerializeField] private float mouseSensitivityX = 2f;

        [Tooltip("Mouse sensitivity for vertical rotation")]
        [SerializeField] private float mouseSensitivityY = 2f;

        [Tooltip("Maximum look up angle (degrees)")]
        [SerializeField] private float maxLookUpAngle = 80f;

        [Tooltip("Maximum look down angle (degrees)")]
        [SerializeField] private float maxLookDownAngle = 80f;

        [Header("Camera Damping")]
        [Tooltip("Camera rotation smoothing (0 = instant, higher = smoother)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float rotationDamping = 0.1f;

        [Header("Strafe Tilt")]
        [Tooltip("Enable camera tilt when strafing")]
        [SerializeField] private bool enableStrafeTilt = true;

        [Tooltip("Maximum tilt angle when strafing (degrees)")]
        [Range(0f, 10f)]
        [SerializeField] private float maxTiltAngle = 3f;

        [Tooltip("Speed of tilt transition")]
        [Range(1f, 20f)]
        [SerializeField] private float tiltSpeed = 8f;

        [Header("Head Bob")]
        [Tooltip("Enable head bob when moving")]
        [SerializeField] private bool enableHeadBob = true;

        [Tooltip("Vertical bobbing amount")]
        [Range(0f, 0.1f)]
        [SerializeField] private float bobVerticalAmount = 0.03f;

        [Tooltip("Horizontal bobbing amount")]
        [Range(0f, 0.1f)]
        [SerializeField] private float bobHorizontalAmount = 0.015f;

        [Tooltip("Bob frequency (cycles per second at full speed)")]
        [Range(1f, 20f)]
        [SerializeField] private float bobFrequency = 10f;

        [Tooltip("Minimum speed to trigger head bob")]
        [SerializeField] private float minBobSpeed = 0.5f;

        [Header("Sprint FOV")]
        [Tooltip("Enable FOV kick when sprinting")]
        [SerializeField] private bool enableSprintFOV = true;

        [Tooltip("FOV increase when sprinting")]
        [Range(0f, 20f)]
        [SerializeField] private float sprintFOVIncrease = 8f;

        [Tooltip("Speed of FOV transition")]
        [Range(1f, 20f)]
        [SerializeField] private float fovTransitionSpeed = 6f;

        [Header("Landing Impact")]
        [Tooltip("Enable camera shake on landing")]
        [SerializeField] private bool enableLandingImpact = true;

        [Tooltip("Landing shake intensity")]
        [Range(0f, 1f)]
        [SerializeField] private float landingShakeIntensity = 0.3f;

        [Tooltip("Landing shake duration")]
        [SerializeField] private float landingShakeDuration = 0.2f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform playerBody;

        // Input
        private Vector2 lookInput;

        // Camera rotation
        private float targetPitch = 0f;
        private float smoothPitch = 0f;

        // Camera tilt
        private float currentTilt = 0f;
        private float targetTilt = 0f;

        // Head bob
        private float bobTimer = 0f;
        private Vector3 originalCameraPosition;

        // FOV
        private float baseFOV;
        private float targetFOV;
        private float currentFOV;

        // Landing
        private float landingShakeTimer = 0f;

        // Movement state (updated by PlayerMovement)
        private float currentSpeed;
        private bool isSprinting;
        private bool isGrounded;
        private Vector2 moveInput;

        #region Unity Lifecycle

        private void Awake()
        {
            // Find camera if not assigned
            if (playerCamera == null)
            {
                playerCamera = GetComponent<Camera>();
                if (playerCamera == null)
                {
                    Debug.LogError("[RigidbodyPlayerCamera] No Camera component found!");
                }
            }

            // Find player body if not assigned
            if (playerBody == null)
            {
                playerBody = transform.parent;
                if (playerBody == null)
                {
                    Debug.LogError("[RigidbodyPlayerCamera] No parent transform (player body) found!");
                }
            }

            // Store original camera position
            originalCameraPosition = transform.localPosition;

            // Store base FOV
            if (playerCamera != null)
            {
                baseFOV = playerCamera.fieldOfView;
                currentFOV = baseFOV;
                targetFOV = baseFOV;
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Handle camera rotation
            HandleCameraRotation();

            // Handle strafe tilt
            if (enableStrafeTilt)
            {
                HandleStrafeTilt();
            }

            // Handle head bob
            if (enableHeadBob)
            {
                HandleHeadBob();
            }

            // Handle sprint FOV
            if (enableSprintFOV)
            {
                HandleSprintFOV();
            }

            // Handle landing shake
            if (enableLandingImpact && landingShakeTimer > 0f)
            {
                HandleLandingShake();
            }
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Called by New Input System - Look input
        /// </summary>
        public void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        #endregion

        #region Camera Rotation

        private void HandleCameraRotation()
        {
            if (playerBody == null || playerCamera == null) return;

            // Get mouse input
            float mouseX = lookInput.x * mouseSensitivityX;
            float mouseY = lookInput.y * mouseSensitivityY;

            // ═══════════════════════════════════════════════════════════
            // VERTICAL ROTATION (Pitch) - Camera only, with damping
            // ═══════════════════════════════════════════════════════════

            // Update target pitch
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, -maxLookDownAngle, maxLookUpAngle);

            // Smooth damp to target
            smoothPitch = Mathf.Lerp(smoothPitch, targetPitch, 1f - rotationDamping);

            // Apply to camera (X rotation)
            transform.localRotation = Quaternion.Euler(smoothPitch, 0f, currentTilt);

            // ═══════════════════════════════════════════════════════════
            // HORIZONTAL ROTATION (Yaw) - Player body, NOT locked to camera
            // ═══════════════════════════════════════════════════════════

            // Rotate player body (Y rotation) - instant, no damping for yaw
            playerBody.Rotate(Vector3.up * mouseX);
        }

        #endregion

        #region Strafe Tilt

        private void HandleStrafeTilt()
        {
            // Calculate target tilt based on strafe input
            float strafeInput = moveInput.x; // -1 (left) to 1 (right)

            // Tilt left when strafing right, right when strafing left
            targetTilt = -strafeInput * maxTiltAngle;

            // Smooth transition
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

            // Applied in HandleCameraRotation()
        }

        #endregion

        #region Head Bob

        private void HandleHeadBob()
        {
            if (currentSpeed < minBobSpeed || !isGrounded)
            {
                // Reset to original position when not moving or in air
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    originalCameraPosition,
                    Time.deltaTime * 8f
                );

                bobTimer = 0f;
                return;
            }

            // Speed-based bob frequency
            float speedMultiplier = currentSpeed / 7f; // Normalize to typical sprint speed
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            // Calculate bob offsets
            float bobVertical = Mathf.Sin(bobTimer) * bobVerticalAmount;
            float bobHorizontal = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmount;

            // Apply bob (additive to original position)
            Vector3 targetPosition = originalCameraPosition + new Vector3(bobHorizontal, bobVertical, 0f);
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.deltaTime * 10f
            );
        }

        #endregion

        #region Sprint FOV

        private void HandleSprintFOV()
        {
            if (playerCamera == null) return;

            // Determine target FOV
            if (isSprinting && isGrounded)
            {
                targetFOV = baseFOV + sprintFOVIncrease;
            }
            else
            {
                targetFOV = baseFOV;
            }

            // Smooth transition
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
            playerCamera.fieldOfView = currentFOV;
        }

        #endregion

        #region Landing Impact

        /// <summary>
        /// Called by PlayerMovement when player lands
        /// </summary>
        public void OnPlayerLanded()
        {
            if (enableLandingImpact)
            {
                landingShakeTimer = landingShakeDuration;
            }
        }

        private void HandleLandingShake()
        {
            landingShakeTimer -= Time.deltaTime;

            // Simple shake: random offset
            float shakeAmount = landingShakeIntensity * (landingShakeTimer / landingShakeDuration);
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );

            transform.localPosition = originalCameraPosition + shakeOffset;

            // Reset when done
            if (landingShakeTimer <= 0f)
            {
                landingShakeTimer = 0f;
                transform.localPosition = originalCameraPosition;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Called by PlayerMovement to update movement state
        /// </summary>
        public void UpdateMovementState(float speed, bool sprinting, bool grounded, Vector2 moveDirection)
        {
            currentSpeed = speed;
            isSprinting = sprinting;
            isGrounded = grounded;
            moveInput = moveDirection;
        }

        /// <summary>
        /// Reset camera to default position and rotation
        /// </summary>
        public void ResetCamera()
        {
            transform.localPosition = originalCameraPosition;
            transform.localRotation = Quaternion.identity;
            targetPitch = 0f;
            smoothPitch = 0f;
            currentTilt = 0f;
            targetTilt = 0f;
            bobTimer = 0f;
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            // Debug info (optional, can be disabled)
            if (Debug.isDebugBuild)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 12;
                style.normal.textColor = Color.white;

                float y = Screen.height - 120;
                GUI.Label(new Rect(10, y, 300, 20), $"Speed: {currentSpeed:F2} m/s", style);
                GUI.Label(new Rect(10, y + 20, 300, 20), $"Sprinting: {isSprinting}", style);
                GUI.Label(new Rect(10, y + 40, 300, 20), $"FOV: {currentFOV:F1}", style);
                GUI.Label(new Rect(10, y + 60, 300, 20), $"Tilt: {currentTilt:F1}°", style);
            }
        }

        #endregion
    }
}
