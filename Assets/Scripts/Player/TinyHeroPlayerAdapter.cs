using UnityEngine;
using Mirror;
using TacticalCombat.Combat;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Adapter script to bridge RPG Tiny Hero Duo characters with existing PlayerController
    /// This allows using Tiny Hero characters as player models while keeping existing gameplay
    /// </summary>
    [RequireComponent(typeof(CharacterIntegration))]
    public class TinyHeroPlayerAdapter : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterIntegration characterIntegration;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private WeaponSystem weaponSystem;
        [SerializeField] private Health healthSystem;

        [Header("Animation Settings")]
        [Tooltip("Minimum speed to trigger walk animation (reserved for future use)")]
#pragma warning disable 0414 // Field assigned but never used - reserved for future animation logic
        [SerializeField] private float walkSpeedThreshold = 0.1f;

        [Tooltip("Speed to trigger sprint animation (reserved for future use)")]
        [SerializeField] private float sprintSpeedThreshold = 4f;
#pragma warning restore 0414

        // Note: These thresholds are available for custom animation logic
        // Currently, CharacterIntegration handles basic movement animations

        [Header("Weapon Attachment")]
        [Tooltip("Attach weapon to right hand bone")]
        [SerializeField] private bool attachWeaponToHand = true;

        [Tooltip("Weapon position offset from hand bone")]
        [SerializeField] private Vector3 weaponPositionOffset = new Vector3(0.05f, 0.02f, -0.01f);

        [Tooltip("Weapon rotation offset from hand bone")]
        [SerializeField] private Vector3 weaponRotationOffset = new Vector3(-90f, 0f, 0f);

        private Animator animator;
        private Transform rightHandBone;
        private bool isInitialized = false;

        private void Awake()
        {
            // Get components
            if (characterIntegration == null)
                characterIntegration = GetComponent<CharacterIntegration>();

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (weaponSystem == null)
                weaponSystem = GetComponentInChildren<WeaponSystem>();

            if (healthSystem == null)
                healthSystem = GetComponent<Health>();

            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (!isServer) return;

            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Find right hand bone for weapon attachment
            if (animator != null && attachWeaponToHand)
            {
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);

                if (rightHandBone != null)
                {
                    AttachWeaponToHand();
                    Debug.Log($"✅ [TinyHeroPlayerAdapter] Weapon attached to right hand");
                }
                else
                {
                    Debug.LogWarning("⚠️ [TinyHeroPlayerAdapter] Right hand bone not found!");
                }
            }

            // Subscribe to events
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponFired += OnWeaponFired;
            }

            if (healthSystem != null)
            {
                healthSystem.OnDeathEvent += OnPlayerDeath;
                healthSystem.OnHealthChangedEvent += OnPlayerHealthChanged;
            }

            isInitialized = true;
            Debug.Log($"✅ [TinyHeroPlayerAdapter] Initialized for {gameObject.name}");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponFired -= OnWeaponFired;
            }

            if (healthSystem != null)
            {
                healthSystem.OnDeathEvent -= OnPlayerDeath;
                healthSystem.OnHealthChangedEvent -= OnPlayerHealthChanged;
            }
        }

        /// <summary>
        /// Attach weapon to character's right hand
        /// </summary>
        private void AttachWeaponToHand()
        {
            if (weaponSystem == null || rightHandBone == null) return;

            // Move weapon system to hand bone
            weaponSystem.transform.SetParent(rightHandBone, false);
            weaponSystem.transform.localPosition = weaponPositionOffset;
            weaponSystem.transform.localRotation = Quaternion.Euler(weaponRotationOffset);

            Debug.Log($"✅ [TinyHeroPlayerAdapter] Weapon attached at offset: {weaponPositionOffset}");
        }

        #region Event Handlers

        /// <summary>
        /// Called when weapon fires - trigger attack animation
        /// </summary>
        private void OnWeaponFired()
        {
            if (characterIntegration != null)
            {
                characterIntegration.PlayAttackAnimation();
            }
        }

        /// <summary>
        /// Called when player health changes
        /// </summary>
        private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
        {
            // Could trigger hit reaction animation here if health decreased
            // For now, we'll just log it
            float healthPercent = (float)currentHealth / maxHealth * 100f;
            Debug.Log($"[TinyHeroPlayerAdapter] Player health: {currentHealth}/{maxHealth} ({healthPercent:F1}%)");
        }

        /// <summary>
        /// Called when player dies - trigger death animation
        /// </summary>
        private void OnPlayerDeath()
        {
            if (characterIntegration != null)
            {
                characterIntegration.SetDeathState(true);
            }

            Debug.Log($"[TinyHeroPlayerAdapter] Player died - death animation triggered");
        }

        #endregion

        #region Network Callbacks

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (!isInitialized)
            {
                Initialize();
            }

            Debug.Log($"✅ [TinyHeroPlayerAdapter] Local player started");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (rightHandBone != null && attachWeaponToHand)
            {
                // Draw weapon attachment point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rightHandBone.position, 0.02f);

                // Draw weapon offset
                Vector3 weaponPos = rightHandBone.TransformPoint(weaponPositionOffset);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rightHandBone.position, weaponPos);
                Gizmos.DrawWireCube(weaponPos, Vector3.one * 0.05f);
            }
        }

        #endregion
    }
}
