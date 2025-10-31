using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Central hub for accessing all player components
    /// Use this instead of GetComponent calls everywhere
    ///
    /// Usage:
    ///   PlayerComponents pc = player.GetComponent<PlayerComponents>();
    ///   pc.health.TakeDamage(10);
    ///   pc.weaponSystem.Reload();
    /// </summary>
    public class PlayerComponents : NetworkBehaviour
    {
        [Header("Core Components")]
        public PlayerController playerController;
        public FPSController fpsController;
        public InputManager inputManager;

        [Header("Combat Components")]
        public Combat.Health health;
        public Combat.WeaponSystem weaponSystem;
        public AbilityController abilityController;

        [Header("Visual Components")]
        public PlayerVisuals visuals;
        public Transform weaponAttachPoint;  // ← NEW: Prefab attach point
        public Transform headAttachPoint;     // ← NEW: For helmets/hats
        public Transform backAttachPoint;     // ← NEW: For backpacks

        [Header("Audio Components")]
        public AudioSource footstepAudio;
        public AudioSource weaponAudio;
        public AudioSource voiceAudio;

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            // Auto-find components if not assigned
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (fpsController == null) fpsController = GetComponent<FPSController>();
            if (inputManager == null) inputManager = GetComponent<InputManager>();
            if (health == null) health = GetComponent<Combat.Health>();
            if (weaponSystem == null) weaponSystem = GetComponent<Combat.WeaponSystem>();
            if (abilityController == null) abilityController = GetComponent<AbilityController>();
            if (visuals == null) visuals = GetComponent<PlayerVisuals>();

            // Auto-find audio sources
            AudioSource[] sources = GetComponentsInChildren<AudioSource>();
            if (sources.Length > 0) footstepAudio = sources[0];
            if (sources.Length > 1) weaponAudio = sources[1];
            if (sources.Length > 2) voiceAudio = sources[2];

            // Try to find attach points by name
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform t in children)
            {
                if (t.name.Contains("WeaponAttach")) weaponAttachPoint = t;
                else if (t.name.Contains("HeadAttach")) headAttachPoint = t;
                else if (t.name.Contains("BackAttach")) backAttachPoint = t;
            }
        }

        // Quick access helpers
        public bool IsLocalPlayer => isLocalPlayer;
        public bool IsAlive => health != null && !health.IsDead();
        public Team Team => playerController != null ? playerController.team : Team.None;
        public RoleId Role => playerController != null ? playerController.role : RoleId.Builder;

        // Prefab attachment helper
        public void AttachPrefabToWeapon(GameObject prefab)
        {
            if (weaponAttachPoint == null)
            {
                Debug.LogWarning("WeaponAttachPoint not found!");
                return;
            }

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, weaponAttachPoint);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
        }

        public void AttachPrefabToHead(GameObject prefab)
        {
            if (headAttachPoint == null)
            {
                Debug.LogWarning("HeadAttachPoint not found!");
                return;
            }

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, headAttachPoint);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
        }

        public void AttachPrefabToBack(GameObject prefab)
        {
            if (backAttachPoint == null)
            {
                Debug.LogWarning("BackAttachPoint not found!");
                return;
            }

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, backAttachPoint);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
        }

        // Debug helper
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            CacheComponents();
        }
    }
}
