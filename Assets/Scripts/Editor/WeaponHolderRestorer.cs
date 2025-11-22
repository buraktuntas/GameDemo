using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON HOLDER RESTORER: Silinmiş WeaponHolder'ı geri ekler
    /// </summary>
    public class WeaponHolderRestorer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Restore WeaponHolder")]
        public static void RestoreWeaponHolder()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                Debug.LogError("❌ Player prefab not found!");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔧 RESTORING WEAPONHOLDER");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Find WeaponSystem
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError("❌ WeaponSystem not found on Player prefab!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", "WeaponSystem not found on Player prefab!", "OK");
                return;
            }

            // Check if WeaponHolder already exists
            Transform weaponHolder = null;
            
            // Check in WeaponSystem reference
            SerializedObject weaponSo = new SerializedObject(weaponSystem);
            var weaponHolderProp = weaponSo.FindProperty("weaponHolder");
            if (weaponHolderProp != null)
            {
                weaponHolder = weaponHolderProp.objectReferenceValue as Transform;
            }

            // Try to find WeaponHolder in hierarchy
            if (weaponHolder == null)
            {
                weaponHolder = playerInstance.transform.Find("WeaponHolder");
                if (weaponHolder == null)
                {
                    var playerVisual = playerInstance.transform.Find("PlayerVisual");
                    if (playerVisual != null)
                    {
                        weaponHolder = playerVisual.Find("WeaponHolder");
                    }
                }
            }

            // Create WeaponHolder if it doesn't exist
            if (weaponHolder == null)
            {
                // Find camera to attach weapon to (Valorant style)
                Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
                
                if (playerCamera != null)
                {
                    // Create WeaponHolder as child of camera
                    GameObject weaponHolderGO = new GameObject("WeaponHolder");
                    weaponHolderGO.transform.SetParent(playerCamera.transform);
                    weaponHolderGO.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f); // Valorant style
                    weaponHolderGO.transform.localRotation = Quaternion.identity;
                    weaponHolderGO.transform.localScale = Vector3.one;
                    weaponHolder = weaponHolderGO.transform;
                    
                    Debug.Log("✅ Created WeaponHolder as child of camera");
                    modified = true;
                }
                else
                {
                    // Fallback: Create as child of player
                    GameObject weaponHolderGO = new GameObject("WeaponHolder");
                    weaponHolderGO.transform.SetParent(playerInstance.transform);
                    weaponHolderGO.transform.localPosition = new Vector3(0.3f, 1.4f, 0.5f); // Right hand position
                    weaponHolderGO.transform.localRotation = Quaternion.identity;
                    weaponHolderGO.transform.localScale = Vector3.one;
                    weaponHolder = weaponHolderGO.transform;
                    
                    Debug.Log("✅ Created WeaponHolder as child of player (camera not found)");
                    modified = true;
                }
            }
            else
            {
                Debug.Log("ℹ️ WeaponHolder already exists");
            }

            // Create MuzzlePoint child if it doesn't exist
            if (weaponHolder != null)
            {
                Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
                if (muzzlePoint == null)
                {
                    GameObject muzzlePointGO = new GameObject("MuzzlePoint");
                    muzzlePointGO.transform.SetParent(weaponHolder);
                    muzzlePointGO.transform.localPosition = new Vector3(0f, 0f, 0.8f); // Estimated muzzle position
                    muzzlePointGO.transform.localRotation = Quaternion.identity;
                    muzzlePointGO.transform.localScale = Vector3.one;
                    
                    Debug.Log("✅ Created MuzzlePoint child");
                    modified = true;
                }
            }

            // Update WeaponSystem reference
            if (weaponHolder != null && weaponHolderProp != null)
            {
                weaponHolderProp.objectReferenceValue = weaponHolder;
                weaponSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(weaponSystem);
                Debug.Log("✅ WeaponHolder assigned to WeaponSystem");
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ WeaponHolder restored successfully!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", "WeaponHolder restored successfully!", "OK");
            }
            else
            {
                Debug.Log("ℹ️ WeaponHolder already exists, no changes needed");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

