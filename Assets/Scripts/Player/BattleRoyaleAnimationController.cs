using UnityEngine;
using Mirror;
using TacticalCombat.Combat;

namespace TacticalCombat.Player
{
    /// <summary>
    /// ✅ BATTLE ROYALE ANIMATION CONTROLLER
    /// WeaponSystem ve FPSController'dan bilgi alarak animasyonları koşullu olarak kontrol eder
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BattleRoyaleAnimationController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private WeaponSystem weaponSystem;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private CharacterController characterController;

        [Header("Animation Parameter Names")]
        [Tooltip("Speed parameter (Float) - walking/running speed")]
        [SerializeField] private string speedParamName = "Speed";
        
        [Tooltip("IsShooting parameter (Bool) - true when firing")]
        [SerializeField] private string isShootingParamName = "IsShooting";
        
        [Tooltip("IsReloading parameter (Bool) - true when reloading")]
        [SerializeField] private string isReloadingParamName = "IsReloading";
        
        [Tooltip("IsAiming parameter (Bool) - true when aiming")]
        [SerializeField] private string isAimingParamName = "IsAiming";
        
        [Tooltip("IsGrounded parameter (Bool) - true when on ground")]
        [SerializeField] private string isGroundedParamName = "IsGrounded";
        
        [Tooltip("Fire trigger (Trigger) - fires weapon")]
        [SerializeField] private string fireTriggerName = "TriggerFire";
        
        [Tooltip("Reload trigger (Trigger) - starts reload")]
        [SerializeField] private string reloadTriggerName = "TriggerReload";
        
        [Tooltip("Jump trigger (Trigger) - jumps")]
        [SerializeField] private string jumpTriggerName = "Jump";

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true; // ✅ Default to true for troubleshooting

        // Cached parameter hashes (performance optimization)
        private int speedHash;
        private int isShootingHash;
        private int isReloadingHash;
        private int isAimingHash;
        private int isGroundedHash;
        private int fireTriggerHash;
        private int reloadTriggerHash;
        private int jumpTriggerHash;

        // State tracking
        private bool wasReloading = false;
        private bool wasGrounded = true;
        private float lastSpeed = 0f;

        private void Awake()
        {
            // Find components if not assigned
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<Animator>();
            }

            if (weaponSystem == null)
            {
                weaponSystem = GetComponentInParent<WeaponSystem>();
            }

            if (fpsController == null)
            {
                fpsController = GetComponentInParent<FPSController>();
            }

            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }
        }

        private void Start()
        {
            if (characterAnimator == null)
            {
                Debug.LogError("❌ [BattleRoyaleAnimationController] Animator not found!");
                enabled = false;
                return;
            }

            // Cache parameter hashes for performance
            CacheParameterHashes();

            // Subscribe to WeaponSystem events
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponFired += OnWeaponFired;
                weaponSystem.OnReloadStarted += OnReloadStarted;
                weaponSystem.OnReloadComplete += OnReloadComplete;
                Debug.Log("✅ [BattleRoyaleAnimationController] Subscribed to WeaponSystem events");
            }
            else
            {
                Debug.LogWarning("⚠️ [BattleRoyaleAnimationController] WeaponSystem not found! Retrying in coroutine...");
                StartCoroutine(RetryWeaponSystemSubscription());
            }

            // Subscribe to FPSController jump event if available
            if (fpsController != null)
            {
                // We'll check for jump in Update() by monitoring grounded state changes
            }

            Debug.Log("✅ [BattleRoyaleAnimationController] Initialized");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponFired -= OnWeaponFired;
                weaponSystem.OnReloadStarted -= OnReloadStarted;
                weaponSystem.OnReloadComplete -= OnReloadComplete;
            }
        }

        private void Update()
        {
            if (characterAnimator == null) return;

            if (isLocalPlayer)
            {
                UpdateMovementAnimation();
                UpdateWeaponAnimations();
                UpdateGroundedState();
                UpdateJumpAnimation();
            }
            else
            {
                // ✅ AAA FIX: For remote players, update movement animation based on actual position delta
                UpdateRemoteMovementAnimation();
            }
        }

        private Vector3 lastPosition;
        private void UpdateRemoteMovementAnimation()
        {
            if (characterAnimator == null) return;

            // Calculate speed from position change since last frame
            float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
            lastPosition = transform.position;

            // Smooth the speed to avoid jitter in legacy animations
            float smoothSpeed = Mathf.Lerp(lastSpeed, speed, Time.deltaTime * 10f);
            lastSpeed = smoothSpeed;

            if (HasParameter(speedHash, AnimatorControllerParameterType.Float))
            {
                characterAnimator.SetFloat(speedHash, smoothSpeed);
            }
        }

        /// <summary>
        /// Cache animator parameter hashes for performance
        /// </summary>
        private void CacheParameterHashes()
        {
            speedHash = Animator.StringToHash(speedParamName);
            isShootingHash = Animator.StringToHash(isShootingParamName);
            isReloadingHash = Animator.StringToHash(isReloadingParamName);
            isAimingHash = Animator.StringToHash(isAimingParamName);
            isGroundedHash = Animator.StringToHash(isGroundedParamName);
            fireTriggerHash = Animator.StringToHash(fireTriggerName);
            reloadTriggerHash = Animator.StringToHash(reloadTriggerName);
            jumpTriggerHash = Animator.StringToHash(jumpTriggerName);
        }

        /// <summary>
        /// Update movement animation based on player velocity
        /// </summary>
        private void UpdateMovementAnimation()
        {
            float speed = 0f;

            if (characterController != null)
            {
                // Calculate horizontal speed (ignore vertical velocity)
                Vector3 velocity = characterController.velocity;
                speed = new Vector2(velocity.x, velocity.z).magnitude;
            }
            else if (fpsController != null)
            {
                // Fallback: try to get speed from FPSController
                CharacterController cc = fpsController.GetComponent<CharacterController>();
                if (cc != null)
                {
                    Vector3 velocity = cc.velocity;
                    speed = new Vector2(velocity.x, velocity.z).magnitude;
                }
            }

            // Update speed parameter if it exists
            if (HasParameter(speedHash, AnimatorControllerParameterType.Float))
            {
                characterAnimator.SetFloat(speedHash, speed);
                
                if (showDebugInfo && Mathf.Abs(speed - lastSpeed) > 0.1f)
                {
                    Debug.Log($"[BattleRoyaleAnimationController] Speed: {speed:F2} m/s");
                    lastSpeed = speed;
                }
            }
        }

        /// <summary>
        /// Update weapon-related animations (shooting, reloading, aiming)
        /// </summary>
        private void UpdateWeaponAnimations()
        {
            if (weaponSystem == null) return;

            // Check if reloading
            bool isReloading = weaponSystem.IsReloading();

            // Update IsShooting parameter (will be set by OnWeaponFired event)
            // Don't reset here - let the event handle it

            // Update IsReloading parameter
            if (HasParameter(isReloadingHash, AnimatorControllerParameterType.Bool))
            {
                if (isReloading != wasReloading)
                {
                    characterAnimator.SetBool(isReloadingHash, isReloading);
                    wasReloading = isReloading;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[BattleRoyaleAnimationController] IsReloading: {isReloading}");
                    }
                }
            }

            // Update IsAiming parameter (if WeaponSystem exposes aiming state)
            // This would need to be added to WeaponSystem if not already present
            bool isAiming = false; // Placeholder - implement based on your WeaponSystem
            if (HasParameter(isAimingHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isAimingHash, isAiming);
            }
        }

        /// <summary>
        /// Update grounded state
        /// </summary>
        private void UpdateGroundedState()
        {
            bool isGrounded = false;

            if (characterController != null)
            {
                isGrounded = characterController.isGrounded;
            }
            else if (fpsController != null)
            {
                // Try to get grounded state from FPSController
                CharacterController cc = fpsController.GetComponent<CharacterController>();
                if (cc != null)
                {
                    isGrounded = cc.isGrounded;
                }
            }

            if (HasParameter(isGroundedHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isGroundedHash, isGrounded);
            }
            
            // ✅ AAA FIX: Notify server of grounded state if it changed
            if (isGrounded != wasGrounded)
            {
                CmdNotifyGrounded(isGrounded);
            }

            // Track grounded state for jump detection
            wasGrounded = isGrounded;
        }

        [Command]
        private void CmdNotifyGrounded(bool grounded)
        {
            RpcSetGrounded(grounded);
        }

        [ClientRpc]
        private void RpcSetGrounded(bool grounded)
        {
            if (!isLocalPlayer && characterAnimator != null && HasParameter(isGroundedHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isGroundedHash, grounded);
            }
        }

        /// <summary>
        /// Update jump animation - trigger when player leaves ground
        /// </summary>
        private void UpdateJumpAnimation()
        {
            bool isGrounded = false;

            if (characterController != null)
            {
                isGrounded = characterController.isGrounded;
            }
            else if (fpsController != null)
            {
                CharacterController cc = fpsController.GetComponent<CharacterController>();
                if (cc != null)
                {
                    isGrounded = cc.isGrounded;
                }
            }

            // Trigger jump animation when player leaves ground (was grounded, now not grounded)
            if (wasGrounded && !isGrounded)
            {
                if (isLocalPlayer) CmdNotifyJump();
                PlayJumpAnimation();
            }
        }

        [Command]
        private void CmdNotifyJump()
        {
            RpcOnJump();
        }

        [ClientRpc]
        private void RpcOnJump()
        {
            if (!isLocalPlayer) PlayJumpAnimation();
        }

        private void PlayJumpAnimation()
        {
            if (characterAnimator == null) return;
            if (HasParameter(jumpTriggerHash, AnimatorControllerParameterType.Trigger))
            {
                characterAnimator.SetTrigger(jumpTriggerHash);
            }
        }

        /// <summary>
        /// Called when weapon fires
        /// </summary>
        private void OnWeaponFired()
        {
            if (isLocalPlayer)
            {
                // Send to server to broadcast to all clients
                CmdNotifyWeaponFired();
            }
            
            PlayFireAnimation();
        }

        [Command]
        private void CmdNotifyWeaponFired()
        {
            RpcOnWeaponFired();
        }

        [ClientRpc]
        private void RpcOnWeaponFired()
        {
            if (!isLocalPlayer) PlayFireAnimation();
        }

        private void PlayFireAnimation()
        {
            if (characterAnimator == null) return;

            if (HasParameter(fireTriggerHash, AnimatorControllerParameterType.Trigger))
            {
                characterAnimator.SetTrigger(fireTriggerHash);
            }

            if (HasParameter(isShootingHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isShootingHash, true);
                StartCoroutine(ResetShootingState());
            }
        }

        /// <summary>
        /// ✅ NEW: Reset IsShooting after animation
        /// </summary>
        private System.Collections.IEnumerator ResetShootingState()
        {
            yield return new WaitForSeconds(0.1f); // Short delay
            
            if (HasParameter(isShootingHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isShootingHash, false);
            }
        }

        /// <summary>
        /// Called when reload starts
        /// </summary>
        private void OnReloadStarted()
        {
            if (isLocalPlayer)
            {
                CmdNotifyReloadStarted();
            }
            
            PlayReloadAnimation();
        }

        [Command]
        private void CmdNotifyReloadStarted()
        {
            RpcOnReloadStarted();
        }

        [ClientRpc]
        private void RpcOnReloadStarted()
        {
            if (!isLocalPlayer) PlayReloadAnimation();
        }

        private void PlayReloadAnimation()
        {
            if (characterAnimator == null) return;

            if (HasParameter(reloadTriggerHash, AnimatorControllerParameterType.Trigger))
            {
                characterAnimator.SetTrigger(reloadTriggerHash);
            }

            if (HasParameter(isReloadingHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isReloadingHash, true);
            }
        }

        /// <summary>
        /// Called when reload completes
        /// </summary>
        private void OnReloadComplete()
        {
            if (isLocalPlayer)
            {
                CmdNotifyReloadComplete();
            }

            StopReloadAnimation();
        }

        [Command]
        private void CmdNotifyReloadComplete()
        {
            RpcOnReloadComplete();
        }

        [ClientRpc]
        private void RpcOnReloadComplete()
        {
            if (!isLocalPlayer) StopReloadAnimation();
        }

        private void StopReloadAnimation()
        {
            if (characterAnimator == null) return;

            if (HasParameter(isReloadingHash, AnimatorControllerParameterType.Bool))
            {
                characterAnimator.SetBool(isReloadingHash, false);
            }
        }

        /// <summary>
        /// Check if animator has parameter
        /// </summary>
        private bool HasParameter(int paramHash, AnimatorControllerParameterType paramType)
        {
            if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null) return false;

            foreach (AnimatorControllerParameter param in characterAnimator.parameters)
            {
                if (param.nameHash == paramHash && param.type == paramType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Public method to manually trigger fire animation (if needed)
        /// </summary>
        public void TriggerFireAnimation()
        {
            OnWeaponFired();
        }

        /// <summary>
        /// Public method to manually trigger reload animation (if needed)
        /// </summary>
        public void TriggerReloadAnimation()
        {
            OnReloadStarted();
        }

        /// <summary>
        /// Retry WeaponSystem subscription if it wasn't found in Start()
        /// </summary>
        private System.Collections.IEnumerator RetryWeaponSystemSubscription()
        {
            int retries = 0;
            while (weaponSystem == null && retries < 10)
            {
                yield return new WaitForSeconds(0.5f);
                weaponSystem = GetComponentInParent<WeaponSystem>();
                retries++;
            }

            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponFired += OnWeaponFired;
                weaponSystem.OnReloadStarted += OnReloadStarted;
                weaponSystem.OnReloadComplete += OnReloadComplete;
                Debug.Log("✅ [BattleRoyaleAnimationController] Successfully subscribed to WeaponSystem events (retry)");
            }
            else
            {
                Debug.LogError("❌ [BattleRoyaleAnimationController] Failed to find WeaponSystem after retries!");
            }
        }

        /// <summary>
        /// Debug: Log current animator state
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.normal.textColor = Color.cyan;

            float x = 10;
            float y = Screen.height - 200;

            GUI.Label(new Rect(x, y, 400, 25), "=== ANIMATION DEBUG ===", style);
            y += 25;

            if (characterAnimator != null)
            {
                AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
                GUI.Label(new Rect(x, y, 400, 25), $"State: {stateInfo.fullPathHash}", style);
                y += 25;

                if (HasParameter(speedHash, AnimatorControllerParameterType.Float))
                {
                    float speed = characterAnimator.GetFloat(speedHash);
                    GUI.Label(new Rect(x, y, 400, 25), $"Speed: {speed:F2}", style);
                    y += 25;
                }

                if (HasParameter(isShootingHash, AnimatorControllerParameterType.Bool))
                {
                    bool isShooting = characterAnimator.GetBool(isShootingHash);
                    GUI.Label(new Rect(x, y, 400, 25), $"IsShooting: {isShooting}", style);
                    y += 25;
                }

                if (HasParameter(isReloadingHash, AnimatorControllerParameterType.Bool))
                {
                    bool isReloading = characterAnimator.GetBool(isReloadingHash);
                    GUI.Label(new Rect(x, y, 400, 25), $"IsReloading: {isReloading}", style);
                    y += 25;
                }
            }
        }
    }
}

