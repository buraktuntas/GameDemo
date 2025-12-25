using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    public partial class FPSController
    {
        [Header("Look")]
        [Tooltip("Mouse sensitivity (lower = slower, higher = faster). Recommended: 1-3")]
        public float lookSpeed = 2f; // ✅ Reduced from 5f to 2f for better control
        public float lookXLimit = 60f;
        
        // Private input variables
        private float rotationX = 0;
        private float pendingMouseX;
        private float pendingMouseY;
        
        // ✅ SHARED CODE: Variables synced between client and server (MirrorUnityFPS pattern)
        // "sc_" prefix = Shared Code (both client and server use these)
        private Vector3 sc_InputMove = Vector3.zero;        // Movement input (WASD)
        private bool sc_IsSprinting = false;                // Sprint state
        private bool sc_JumpRequested = false;              // Jump flag (processed every frame)
        private float sc_YawRotation = 0f;             // Y-axis rotation (Yaw)

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

            // ✅ SMOOTH FIX: Increased scaling factor for better responsiveness
            // Mouse.delta is in pixels, needs scaling for smooth feel
            // 0.15f was too low causing perceived "stutter"
            pendingMouseX = lookDelta.x * 0.5f;  // Increased from 0.15f
            pendingMouseY = lookDelta.y * 0.5f;  // Increased from 0.15f
        }

        /// <summary>
        /// Apply rotation in LateUpdate (after movement, smooth)
        /// ✅ CRITICAL FIX: Direct rotation without deltaTime scaling (mouse delta is already frame-independent)
        /// </summary>
        private void ApplyRotation()
        {
            // ✅ SMOOTH FIX: Apply mouse delta smoothly without deltaTime (already frame-independent)

            // Mouse Y -> Camera pitch (up/down)
            rotationX += -pendingMouseY * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            }

            // Mouse X -> Player rotation (left/right)
            // ✅ SMOOTH FIX: Use Euler instead of *= to avoid gimbal lock and micro-stutters
            float currentYaw = transform.eulerAngles.y;
            float newYaw = currentYaw + (pendingMouseX * lookSpeed);
            transform.rotation = Quaternion.Euler(0, newYaw, 0);

            // Reset pending input
            pendingMouseX = 0;
            pendingMouseY = 0;
        }

        /// <summary>
        /// ✅ CLIENT: Send input state to server
        /// Called only on local player to sync input variables
        /// </summary>
        [Client]
        private void Client_SendInputToServer()
        {
            if (!isLocalPlayer || inputManager == null) return;

            // Read input from InputManager
            Vector2 moveInput = inputManager.MoveInput;
            Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);
            bool sprint = inputManager.SprintHeld;
            bool jump = inputManager.JumpPressed;
            float yRot = transform.rotation.eulerAngles.y;

            // Send to server if anything changed
            // ✅ FIX: Use sc_YawRotation instead of sc_RotationVelocity
            if (input != sc_InputMove || sprint != sc_IsSprinting || jump != sc_JumpRequested || Mathf.Abs(yRot - sc_YawRotation) > 0.1f)
            {
                Cmd_SyncInput(input, sprint, jump, yRot);
            }

            // ✅ FIX: Apply input LOCALLY so Client Prediction works!
            // Without this, sc_InputMove stays (0,0,0) and client cannot move locally
            sc_InputMove = input;
            sc_IsSprinting = sprint;
            if (jump) sc_JumpRequested = true; // Only set true, reset handled in SC_Jump
            sc_YawRotation = yRot;
        }

        /// <summary>
        /// ✅ SERVER: Receive and store client input
        /// </summary>
        [Command]
        private void Cmd_SyncInput(Vector3 input, bool sprint, bool jump, float yRotation)
        {
            sc_InputMove = input;
            sc_IsSprinting = sprint;
            sc_JumpRequested = jump;
            sc_YawRotation = yRotation;

            // Update rotation immediately
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }
}
