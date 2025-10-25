using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class HitboxSetup
    {
        [MenuItem("Tools/Tactical Combat/Add Hitboxes to Player")]
        public static void AddHitboxesToPlayer()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not found!");
                return;
            }
            
            GameObject playerInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // Clear existing hitboxes
            var existingHitboxes = playerInstance.GetComponentsInChildren<Hitbox>();
            foreach (var hitbox in existingHitboxes)
            {
                Object.DestroyImmediate(hitbox.gameObject);
            }
            
            // Create hitbox parent
            GameObject hitboxRoot = new GameObject("Hitboxes");
            hitboxRoot.transform.SetParent(playerInstance.transform);
            hitboxRoot.transform.localPosition = Vector3.zero;
            hitboxRoot.transform.localRotation = Quaternion.identity;
            
            // HEAD HITBOX (SphereCollider)
            GameObject head = new GameObject("Hitbox_Head");
            head.transform.SetParent(hitboxRoot.transform);
            head.transform.localPosition = new Vector3(0, 1.7f, 0); // Top of player
            head.layer = LayerMask.NameToLayer("Default");
            
            SphereCollider headCol = head.AddComponent<SphereCollider>();
            headCol.radius = 0.15f;
            headCol.isTrigger = false;
            
            Hitbox headHitbox = head.AddComponent<Hitbox>();
            headHitbox.zone = HitZone.Head;
            headHitbox.gizmoColor = Color.red;
            
            Debug.Log("✅ Head hitbox created");
            
            // CHEST HITBOX (BoxCollider)
            GameObject chest = new GameObject("Hitbox_Chest");
            chest.transform.SetParent(hitboxRoot.transform);
            chest.transform.localPosition = new Vector3(0, 1.2f, 0);
            chest.layer = LayerMask.NameToLayer("Default");
            
            BoxCollider chestCol = chest.AddComponent<BoxCollider>();
            chestCol.size = new Vector3(0.5f, 0.6f, 0.3f);
            chestCol.isTrigger = false;
            
            Hitbox chestHitbox = chest.AddComponent<Hitbox>();
            chestHitbox.zone = HitZone.Chest;
            chestHitbox.gizmoColor = Color.yellow;
            
            Debug.Log("✅ Chest hitbox created");
            
            // STOMACH HITBOX (BoxCollider)
            GameObject stomach = new GameObject("Hitbox_Stomach");
            stomach.transform.SetParent(hitboxRoot.transform);
            stomach.transform.localPosition = new Vector3(0, 0.8f, 0);
            stomach.layer = LayerMask.NameToLayer("Default");
            
            BoxCollider stomachCol = stomach.AddComponent<BoxCollider>();
            stomachCol.size = new Vector3(0.4f, 0.4f, 0.3f);
            stomachCol.isTrigger = false;
            
            Hitbox stomachHitbox = stomach.AddComponent<Hitbox>();
            stomachHitbox.zone = HitZone.Stomach;
            stomachHitbox.gizmoColor = Color.green;
            
            Debug.Log("✅ Stomach hitbox created");
            
            // LEGS HITBOX (CapsuleCollider)
            GameObject legs = new GameObject("Hitbox_Legs");
            legs.transform.SetParent(hitboxRoot.transform);
            legs.transform.localPosition = new Vector3(0, 0.4f, 0);
            legs.layer = LayerMask.NameToLayer("Default");
            
            CapsuleCollider legsCol = legs.AddComponent<CapsuleCollider>();
            legsCol.height = 0.8f;
            legsCol.radius = 0.2f;
            legsCol.direction = 1; // Y-axis
            legsCol.isTrigger = false;
            
            Hitbox legsHitbox = legs.AddComponent<Hitbox>();
            legsHitbox.zone = HitZone.Limbs;
            legsHitbox.gizmoColor = Color.blue;
            
            Debug.Log("✅ Legs hitbox created");
            
            // ARMS HITBOX (Optional - can be added later)
            
            // Remove main CharacterController collider (hitboxes replace it)
            var charController = playerInstance.GetComponent<CharacterController>();
            if (charController != null)
            {
                // Keep CharacterController for movement but reduce its collider
                charController.radius = 0.3f;
                charController.height = 2f;
                charController.center = new Vector3(0, 1, 0);
                Debug.Log("✅ CharacterController adjusted");
            }
            
            // Save
            PrefabUtility.SaveAsPrefabAsset(playerInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(playerInstance);
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ HITBOXES ADDED TO PLAYER!");
            Debug.Log("  • Head (2.5x damage)");
            Debug.Log("  • Chest (1.0x damage)");
            Debug.Log("  • Stomach (0.9x damage)");
            Debug.Log("  • Legs (0.75x damage)");
            Debug.Log("═══════════════════════════════════════════");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
