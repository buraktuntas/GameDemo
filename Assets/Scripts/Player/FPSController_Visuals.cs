using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

namespace TacticalCombat.Player
{
    public partial class FPSController
    {
        [Header("Effects")]
        public bool useHeadBob = true;
        public float bobSpeed = 12f;
        public float bobAmount = 0.05f;
        public bool useFOVKick = true;
        public float sprintFOVIncrease = 10f;
        // ✅ AAA FEATURE: Procedural Camera Tilt when strafing
        public float tiltAmount = 2.5f; 
        public float tiltSpeed = 5f;
        
        [Header("Audio (Optional)")]
        public AudioClip jumpSound;
        public AudioClip landSound;
        public AudioClip[] footstepSounds;
        
        [Header("Stamina (Optional)")]
        public bool useStamina = false;
        public float maxStamina = 100f;
        public float staminaDrainRate = 10f;
        public float staminaRegenRate = 5f;
        
        // Private visual variables
        private float currentStamina;
        private float baseFOV;
        private float bobTimer = 0f;
        private float stepTimer = 0f;
        private Vector3 originalCameraPos;
        
        // Cached variables to avoid GC
        private Vector3 cachedSpeedVector = Vector3.zero;
        private Vector3 cachedBobOffset = Vector3.zero;

        private void InitializeVisuals()
        {
            currentStamina = maxStamina;
            if (playerCamera != null)
            {
                originalCameraPos = playerCamera.transform.localPosition;
                baseFOV = playerCamera.fieldOfView;
            }
        }

        private void HandleStamina()
        {
            if (!useStamina) return;

            // ✅ ARCHITECTURE FIX: Use InputManager as Single Source of Truth
            bool isSprintInput = inputManager != null && inputManager.SprintHeld && inputManager.IsGameplayInputAllowed();
            bool sprinting = isSprintInput && IsMoving();
            
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
            if (!useHeadBob) return;

            // ✅ SAFETY: Ensure camera exists before using it
            if (playerCamera == null) return;

            // ✅ AAA FIX: Check if we are really moving (not just trying to move into a wall)
            // But for visuals, sc_InputMove check is safer than velocity
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
            if (!useFOVKick || playerCamera == null) return;
            
            // ✅ ARCHITECTURE FIX: Use InputManager as Single Source of Truth
            bool isSprintInput = inputManager != null && inputManager.SprintHeld && inputManager.IsGameplayInputAllowed();
            bool sprinting = isSprintInput && IsMoving() && IsGrounded();
            float targetFOV = sprinting ? baseFOV + sprintFOVIncrease : baseFOV;
            
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * 8f
            );
        }

        private void UpdateCameraTilt()
        {
            if (playerCamera == null) return;

            // Determine target tilt based on strafe input (sc_InputMove.x)
            // -1 (Left) -> Positive Tilt (Rotate Z right) OR Negative Tilt? 
            // Usually Strafe Left -> Tilt Left (Positive Z)
            
            float targetTilt = 0f;
            if (sc_InputMove.x > 0.1f) targetTilt = -tiltAmount; // Right strafe -> Tilt Right
            else if (sc_InputMove.x < -0.1f) targetTilt = tiltAmount; // Left strafe -> Tilt Left

            // Apply smoothing
            float currentZ = playerCamera.transform.localRotation.eulerAngles.z;
            // Handle angle wrapping (0-360) for smooth lerp
            if (currentZ > 180) currentZ -= 360;

            float newZ = Mathf.Lerp(currentZ, targetTilt, Time.deltaTime * tiltSpeed);
            
            // Apply rotation (preserving Pitch X)
            Vector3 currentRot = playerCamera.transform.localRotation.eulerAngles.z == newZ ? playerCamera.transform.localRotation.eulerAngles : new Vector3(playerCamera.transform.localRotation.eulerAngles.x, playerCamera.transform.localRotation.eulerAngles.y, newZ);
            playerCamera.transform.localRotation = Quaternion.Euler(currentRot.x, currentRot.y, newZ);
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
            
            // ✅ ARCHITECTURE FIX: Use InputManager as Single Source of Truth
            bool isSprintInput = inputManager != null && inputManager.SprintHeld;
            bool sprinting = isSprintInput;
            float stepInterval = sprinting ? 0.3f : 0.5f;
            
            if (stepTimer >= stepInterval)
            {
                PlayRandomFootstep();
                stepTimer = 0f;
            }
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
    }
}
