using UnityEngine;
using Mirror;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// ✅ WEAPON HAND FOLLOWER: Silahı animasyonlu el bone'una bağlar
    /// FPS görünümünde Camera'ya, Third-Person görünümünde Hand Bone'a bağlanır
    /// </summary>
    public class WeaponHandFollower : NetworkBehaviour
    {
        [Header("Hand Bone Settings")]
        [Tooltip("Hand bone name (e.g., RightHand, Hand_R)")]
        [SerializeField] private string handBoneName = "RightHand";
        
        [Tooltip("Use FPS view (weapon follows camera) or Third-Person view (weapon follows hand)")]
        [SerializeField] private bool useFPSView = true;

        [Header("References")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Transform handBone;
        [SerializeField] private Camera playerCamera;

        [Header("Third-Person Settings")]
        [Tooltip("Weapon position relative to hand bone (Third-Person only)")]
        [SerializeField] private Vector3 weaponPositionOffset = new Vector3(0.05f, -0.02f, 0.1f);
        
        [Tooltip("Weapon rotation relative to hand bone (Third-Person only)")]
        [SerializeField] private Vector3 weaponRotationOffset = Vector3.zero;

        private Animator characterAnimator;
        private bool isInitialized = false;

        private void Awake()
        {
            // Find character animator
            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<Animator>();
            }

            // Find camera
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Find hand bone
            if (characterAnimator != null)
            {
                handBone = FindBoneInHierarchy(characterAnimator.transform, handBoneName);
            }
        }

        private void Start()
        {
            InitializeWeaponHolder();
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (useFPSView)
            {
                // FPS View: WeaponHolder should be child of Camera (set in editor)
                // No runtime updates needed - weapon follows camera transform
            }
            else
            {
                // Third-Person View: Update weapon position to follow hand bone
                if (handBone != null && weaponHolder != null)
                {
                    // Make weaponHolder follow hand bone
                    weaponHolder.position = handBone.position + handBone.TransformDirection(weaponPositionOffset);
                    weaponHolder.rotation = handBone.rotation * Quaternion.Euler(weaponRotationOffset);
                }
            }
        }

        private void InitializeWeaponHolder()
        {
            if (weaponHolder != null)
            {
                isInitialized = true;
                return;
            }

            // Try to find WeaponHolder
            if (useFPSView && playerCamera != null)
            {
                weaponHolder = playerCamera.transform.Find("WeaponHolder");
            }
            else if (!useFPSView && handBone != null)
            {
                weaponHolder = handBone.Find("WeaponHolder");
            }

            if (weaponHolder == null)
            {
                // Try to find from WeaponSystem
                WeaponSystem weaponSystem = GetComponent<WeaponSystem>();
                if (weaponSystem != null)
                {
                    // Use reflection to get weaponHolder (it's private)
                    var weaponHolderField = typeof(WeaponSystem).GetField("weaponHolder", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (weaponHolderField != null)
                    {
                        weaponHolder = weaponHolderField.GetValue(weaponSystem) as Transform;
                    }
                }
            }

            if (weaponHolder != null)
            {
                isInitialized = true;
                Debug.Log($"[WeaponHandFollower] Initialized - FPS View: {useFPSView}, WeaponHolder: {weaponHolder.name}");
            }
            else
            {
                Debug.LogWarning("[WeaponHandFollower] WeaponHolder not found! Weapon will not follow hand.");
            }
        }

        private Transform FindBoneInHierarchy(Transform root, string boneName)
        {
            // First try exact match
            Transform found = root.Find(boneName);
            if (found != null) return found;

            // Then search recursively
            foreach (Transform child in root)
            {
                if (child.name.Contains(boneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform result = FindBoneInHierarchy(child, boneName);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// Switch between FPS and Third-Person view
        /// </summary>
        public void SetFPSView(bool fpsView)
        {
            useFPSView = fpsView;
            isInitialized = false;
            InitializeWeaponHolder();
        }

        /// <summary>
        /// Get current view mode
        /// </summary>
        public bool IsFPSView()
        {
            return useFPSView;
        }
    }
}

