using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Helper component for RPG Tiny Hero Duo character integration
    /// Bridges character animations with game systems
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterIntegration : NetworkBehaviour
    {
        [Header("Character Animation")]
        [SerializeField] private Animator characterAnimator;

        [Header("Animation Parameters")]
        [Tooltip("Speed parameter name in animator (default: Speed)")]
        [SerializeField] private string speedParamName = "Speed";

        [Tooltip("Attack trigger name in animator (default: Attack)")]
        [SerializeField] private string attackTriggerName = "Attack";

        [Tooltip("Jump trigger name in animator (default: Jump)")]
        [SerializeField] private string jumpTriggerName = "Jump";

        [Tooltip("IsDead bool parameter name (default: IsDead)")]
        [SerializeField] private string isDeadParamName = "IsDead";

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private FPSController playerMovement;  // Changed to FPSController (active system)

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void Awake()
        {
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<Animator>();
            }

            // Try to find components if not assigned
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<FPSController>();
            }
        }

        private void Start()
        {
            if (characterAnimator == null)
            {
                Debug.LogError("[CharacterIntegration] Animator not found!");
                enabled = false;
                return;
            }

            Debug.Log($"✅ [CharacterIntegration] Initialized on {gameObject.name}");
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            UpdateMovementAnimation();
        }

        /// <summary>
        /// Update movement animation based on player velocity
        /// </summary>
        private void UpdateMovementAnimation()
        {
            if (characterAnimator == null) return;

            float speed = 0f;

            // FPSController uses CharacterController
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                speed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            }

            // Set animator speed parameter
            if (HasParameter(speedParamName, AnimatorControllerParameterType.Float))
            {
                characterAnimator.SetFloat(speedParamName, speed);

                if (showDebugInfo)
                {
                    Debug.Log($"[CharacterIntegration] Speed: {speed:F2}");
                }
            }
        }

        /// <summary>
        /// Trigger attack animation
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (characterAnimator == null) return;

            if (HasParameter(attackTriggerName, AnimatorControllerParameterType.Trigger))
            {
                characterAnimator.SetTrigger(attackTriggerName);

                if (showDebugInfo)
                {
                    Debug.Log("[CharacterIntegration] Attack animation triggered");
                }
            }
        }

        /// <summary>
        /// Trigger jump animation
        /// </summary>
        public void PlayJumpAnimation()
        {
            if (characterAnimator == null) return;

            if (HasParameter(jumpTriggerName, AnimatorControllerParameterType.Trigger))
            {
                characterAnimator.SetTrigger(jumpTriggerName);

                if (showDebugInfo)
                {
                    Debug.Log("[CharacterIntegration] Jump animation triggered");
                }
            }
        }

        /// <summary>
        /// Set death state
        /// </summary>
        public void SetDeathState(bool isDead)
        {
            if (characterAnimator == null) return;

            if (HasParameter(isDeadParamName, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isDeadParamName, isDead);

                if (showDebugInfo)
                {
                    Debug.Log($"[CharacterIntegration] Death state: {isDead}");
                }
            }
        }

        /// <summary>
        /// Play custom animation by name
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (characterAnimator == null) return;

            characterAnimator.Play(animationName);

            if (showDebugInfo)
            {
                Debug.Log($"[CharacterIntegration] Playing animation: {animationName}");
            }
        }

        /// <summary>
        /// Check if animator has parameter
        /// </summary>
        private bool HasParameter(string paramName, AnimatorControllerParameterType paramType)
        {
            if (characterAnimator == null) return false;

            foreach (AnimatorControllerParameter param in characterAnimator.parameters)
            {
                if (param.name == paramName && param.type == paramType)
                {
                    return true;
                }
            }

            return false;
        }

        #region Network Callbacks

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log($"✅ [CharacterIntegration] Local player started: {gameObject.name}");
        }

        public override void OnStopLocalPlayer()
        {
            base.OnStopLocalPlayer();
            Debug.Log($"[CharacterIntegration] Local player stopped: {gameObject.name}");
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.cyan;

            float x = 10;
            float y = Screen.height - 150;

            GUI.Label(new Rect(x, y, 400, 25), "=== CHARACTER DEBUG ===", style);
            y += 30;

            if (characterAnimator != null)
            {
                GUI.Label(new Rect(x, y, 400, 25), $"Animator: {characterAnimator.name}", style);
                y += 25;

                if (HasParameter(speedParamName, AnimatorControllerParameterType.Float))
                {
                    float speed = characterAnimator.GetFloat(speedParamName);
                    GUI.Label(new Rect(x, y, 400, 25), $"Speed: {speed:F2}", style);
                    y += 25;
                }

                if (HasParameter(isDeadParamName, AnimatorControllerParameterType.Bool))
                {
                    bool isDead = characterAnimator.GetBool(isDeadParamName);
                    GUI.Label(new Rect(x, y, 400, 25), $"IsDead: {isDead}", style);
                    y += 25;
                }
            }
        }

        #endregion
    }
}
