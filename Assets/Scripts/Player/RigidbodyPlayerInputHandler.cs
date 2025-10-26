using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Input handler bridge for Rigidbody FPS system
    /// Connects Unity Input System to RigidbodyPlayerMovement and RigidbodyPlayerCamera
    ///
    /// Alternative to PlayerInput component's Unity Events
    /// Use this if you prefer code-based input handling
    /// </summary>
    [RequireComponent(typeof(RigidbodyPlayerMovement))]
    [RequireComponent(typeof(RigidbodyPlayerCamera))]
    public class RigidbodyPlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions Asset")]
        [Tooltip("Assign your PlayerInputActions asset here")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Debug")]
        [SerializeField] private bool showInputDebug = false;

        // Components
        private RigidbodyPlayerMovement playerMovement;
        private RigidbodyPlayerCamera playerCamera;

        // Input Actions
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        private void Awake()
        {
            // Get components
            playerMovement = GetComponent<RigidbodyPlayerMovement>();
            playerCamera = GetComponentInChildren<RigidbodyPlayerCamera>();

            if (playerMovement == null)
            {
                Debug.LogError("[RigidbodyPlayerInputHandler] RigidbodyPlayerMovement not found!");
            }

            if (playerCamera == null)
            {
                Debug.LogError("[RigidbodyPlayerInputHandler] RigidbodyPlayerCamera not found!");
            }

            // Setup input actions
            if (inputActions != null)
            {
                SetupInputActions();
            }
            else
            {
                Debug.LogWarning("[RigidbodyPlayerInputHandler] No InputActionAsset assigned! Assign in Inspector or use PlayerInput component instead.");
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void SetupInputActions()
        {
            // Get the "Player" action map
            var playerActionMap = inputActions.FindActionMap("Player");

            if (playerActionMap == null)
            {
                Debug.LogError("[RigidbodyPlayerInputHandler] 'Player' action map not found in InputActions!");
                return;
            }

            // Get individual actions
            moveAction = playerActionMap.FindAction("Move");
            lookAction = playerActionMap.FindAction("Look");
            jumpAction = playerActionMap.FindAction("Jump");
            sprintAction = playerActionMap.FindAction("Sprint");

            // Subscribe to action callbacks
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }

            if (lookAction != null)
            {
                lookAction.performed += OnLook;
                lookAction.canceled += OnLook;
            }

            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
                jumpAction.canceled += OnJump;
            }

            if (sprintAction != null)
            {
                sprintAction.performed += OnSprint;
                sprintAction.canceled += OnSprint;
            }

            Debug.Log("✅ [RigidbodyPlayerInputHandler] Input actions setup complete");
        }

        private void EnableInputActions()
        {
            if (inputActions == null) return;

            inputActions.Enable();

            if (showInputDebug)
            {
                Debug.Log("🎮 [RigidbodyPlayerInputHandler] Input enabled");
            }
        }

        private void DisableInputActions()
        {
            if (inputActions == null) return;

            inputActions.Disable();

            if (showInputDebug)
            {
                Debug.Log("🎮 [RigidbodyPlayerInputHandler] Input disabled");
            }
        }

        #region Input Callbacks

        private void OnMove(InputAction.CallbackContext context)
        {
            if (playerMovement != null)
            {
                playerMovement.OnMove(context);

                if (showInputDebug)
                {
                    Vector2 value = context.ReadValue<Vector2>();
                    Debug.Log($"Move: {value}");
                }
            }
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            if (playerCamera != null)
            {
                playerCamera.OnLook(context);

                if (showInputDebug)
                {
                    Vector2 value = context.ReadValue<Vector2>();
                    Debug.Log($"Look: {value}");
                }
            }
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (playerMovement != null)
            {
                playerMovement.OnJump(context);

                if (showInputDebug)
                {
                    Debug.Log($"Jump: {context.phase}");
                }
            }
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            if (playerMovement != null)
            {
                playerMovement.OnSprint(context);

                if (showInputDebug)
                {
                    Debug.Log($"Sprint: {context.phase}");
                }
            }
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showInputDebug) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.yellow;

            float x = 10;
            float y = 10;

            GUI.Label(new Rect(x, y, 400, 25), "=== INPUT DEBUG ===", style);
            y += 30;

            if (moveAction != null)
            {
                Vector2 moveValue = moveAction.ReadValue<Vector2>();
                GUI.Label(new Rect(x, y, 400, 25), $"Move: ({moveValue.x:F2}, {moveValue.y:F2})", style);
                y += 25;
            }

            if (lookAction != null)
            {
                Vector2 lookValue = lookAction.ReadValue<Vector2>();
                GUI.Label(new Rect(x, y, 400, 25), $"Look: ({lookValue.x:F2}, {lookValue.y:F2})", style);
                y += 25;
            }

            if (jumpAction != null)
            {
                bool jumpPressed = jumpAction.ReadValue<float>() > 0.5f;
                GUI.Label(new Rect(x, y, 400, 25), $"Jump: {jumpPressed}", style);
                y += 25;
            }

            if (sprintAction != null)
            {
                bool sprintPressed = sprintAction.ReadValue<float>() > 0.5f;
                GUI.Label(new Rect(x, y, 400, 25), $"Sprint: {sprintPressed}", style);
                y += 25;
            }
        }

        #endregion
    }
}
