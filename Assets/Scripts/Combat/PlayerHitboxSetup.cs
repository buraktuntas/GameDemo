using UnityEngine;
using Mirror;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Automatically sets up hitboxes for player at runtime
    /// Adds colliders for head, chest, stomach, and limbs with appropriate damage multipliers
    /// </summary>
    public class PlayerHitboxSetup : NetworkBehaviour
    {
        [Header("Hitbox Settings")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool showDebug = true;

        [Header("Collider Sizes")]
        [SerializeField] private float headRadius = 0.15f;
        [SerializeField] private Vector3 headOffset = new Vector3(0, 1.6f, 0);

        [SerializeField] private Vector3 chestSize = new Vector3(0.4f, 0.5f, 0.25f);
        [SerializeField] private Vector3 chestOffset = new Vector3(0, 1.2f, 0);

        [SerializeField] private Vector3 stomachSize = new Vector3(0.35f, 0.3f, 0.25f);
        [SerializeField] private Vector3 stomachOffset = new Vector3(0, 0.85f, 0);

        [SerializeField] private float limbRadius = 0.1f;
        [SerializeField] private float limbHeight = 0.5f;

        private GameObject hitboxRoot;

        private void Start()
        {
            if (setupOnStart)
            {
                SetupHitboxes();
            }
        }

        [ContextMenu("Setup Hitboxes")]
        public void SetupHitboxes()
        {
            // Only setup on server (clients will receive via network)
            if (!isServer) return;

            // Check if hitboxes already exist
            if (hitboxRoot != null)
            {
                if (showDebug)
                    Debug.LogWarning("⚠️ Hitboxes already setup, skipping");
                return;
            }

            // Create root for hitboxes
            hitboxRoot = new GameObject("Hitboxes");
            hitboxRoot.transform.SetParent(transform);
            hitboxRoot.transform.localPosition = Vector3.zero;
            hitboxRoot.transform.localRotation = Quaternion.identity;

            // Create hitboxes
            CreateHeadHitbox();
            CreateChestHitbox();
            CreateStomachHitbox();
            CreateLimbHitboxes();

            if (showDebug)
                Debug.Log($"✅ [PlayerHitboxSetup] Created hitboxes for {gameObject.name}");
        }

        private void CreateHeadHitbox()
        {
            GameObject head = new GameObject("Hitbox_Head");
            head.transform.SetParent(hitboxRoot.transform);
            head.transform.localPosition = headOffset;
            head.transform.localRotation = Quaternion.identity;
            head.layer = gameObject.layer; // Same layer as player

            SphereCollider col = head.AddComponent<SphereCollider>();
            col.radius = headRadius;
            col.isTrigger = false; // Must be non-trigger for raycasts

            Hitbox hitbox = head.AddComponent<Hitbox>();
            hitbox.zone = HitZone.Head; // 2.5x damage
            hitbox.gizmoColor = Color.red;
        }

        private void CreateChestHitbox()
        {
            GameObject chest = new GameObject("Hitbox_Chest");
            chest.transform.SetParent(hitboxRoot.transform);
            chest.transform.localPosition = chestOffset;
            chest.transform.localRotation = Quaternion.identity;
            chest.layer = gameObject.layer;

            BoxCollider col = chest.AddComponent<BoxCollider>();
            col.size = chestSize;
            col.isTrigger = false;

            Hitbox hitbox = chest.AddComponent<Hitbox>();
            hitbox.zone = HitZone.Chest; // 1.0x damage
            hitbox.gizmoColor = Color.yellow;
        }

        private void CreateStomachHitbox()
        {
            GameObject stomach = new GameObject("Hitbox_Stomach");
            stomach.transform.SetParent(hitboxRoot.transform);
            stomach.transform.localPosition = stomachOffset;
            stomach.transform.localRotation = Quaternion.identity;
            stomach.layer = gameObject.layer;

            BoxCollider col = stomach.AddComponent<BoxCollider>();
            col.size = stomachSize;
            col.isTrigger = false;

            Hitbox hitbox = stomach.AddComponent<Hitbox>();
            hitbox.zone = HitZone.Stomach; // 0.9x damage
            hitbox.gizmoColor = Color.green;
        }

        private void CreateLimbHitboxes()
        {
            // Left Arm
            CreateLimbHitbox("Hitbox_LeftArm", new Vector3(-0.3f, 1.2f, 0));

            // Right Arm
            CreateLimbHitbox("Hitbox_RightArm", new Vector3(0.3f, 1.2f, 0));

            // Left Leg
            CreateLimbHitbox("Hitbox_LeftLeg", new Vector3(-0.15f, 0.4f, 0));

            // Right Leg
            CreateLimbHitbox("Hitbox_RightLeg", new Vector3(0.15f, 0.4f, 0));
        }

        private void CreateLimbHitbox(string name, Vector3 offset)
        {
            GameObject limb = new GameObject(name);
            limb.transform.SetParent(hitboxRoot.transform);
            limb.transform.localPosition = offset;
            limb.transform.localRotation = Quaternion.identity;
            limb.layer = gameObject.layer;

            CapsuleCollider col = limb.AddComponent<CapsuleCollider>();
            col.radius = limbRadius;
            col.height = limbHeight;
            col.direction = 1; // Y-axis
            col.isTrigger = false;

            Hitbox hitbox = limb.AddComponent<Hitbox>();
            hitbox.zone = HitZone.Limbs; // 0.75x damage
            hitbox.gizmoColor = Color.cyan;
        }

        [ContextMenu("Remove Hitboxes")]
        public void RemoveHitboxes()
        {
            if (hitboxRoot != null)
            {
                DestroyImmediate(hitboxRoot);
                hitboxRoot = null;
                if (showDebug)
                    Debug.Log("🗑️ [PlayerHitboxSetup] Removed hitboxes");
            }
        }

        private void OnDestroy()
        {
            // Cleanup
            if (hitboxRoot != null)
            {
                Destroy(hitboxRoot);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Preview hitboxes in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + headOffset, headRadius);

            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(chestOffset, chestSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(stomachOffset, stomachSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + new Vector3(-0.3f, 1.2f, 0), limbRadius);
            Gizmos.DrawWireSphere(transform.position + new Vector3(0.3f, 1.2f, 0), limbRadius);
            Gizmos.DrawWireSphere(transform.position + new Vector3(-0.15f, 0.4f, 0), limbRadius);
            Gizmos.DrawWireSphere(transform.position + new Vector3(0.15f, 0.4f, 0), limbRadius);
        }
#endif
    }
}
