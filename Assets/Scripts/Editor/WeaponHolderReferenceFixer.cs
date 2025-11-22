using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON HOLDER REFERENCE FIXER: WeaponHolder GameObject'i bulur ve WeaponSystem'e referans olarak atar
    /// </summary>
    public class WeaponHolderReferenceFixer
    {
        [MenuItem("Tools/Tactical Combat/Fix WeaponHolder Reference")]
        public static void FixWeaponHolderReference()
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
            Debug.Log("🔧 FIXING WEAPONHOLDER REFERENCE");
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

            // Find WeaponHolder in hierarchy (check multiple locations)
            Transform weaponHolder = null;

            // 1. Check Camera children
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                weaponHolder = playerCamera.transform.Find("WeaponHolder");
                if (weaponHolder != null)
                {
                    Debug.Log("✅ Found WeaponHolder under Camera");
                }
            }

            // 2. Check Player root
            if (weaponHolder == null)
            {
                weaponHolder = playerInstance.transform.Find("WeaponHolder");
                if (weaponHolder != null)
                {
                    Debug.Log("✅ Found WeaponHolder under Player root");
                }
            }

            // 3. Check PlayerVisual
            if (weaponHolder == null)
            {
                Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    weaponHolder = playerVisual.Find("WeaponHolder");
                    if (weaponHolder != null)
                    {
                        Debug.Log("✅ Found WeaponHolder under PlayerVisual");
                    }
                }
            }

            // 4. Check all children recursively
            if (weaponHolder == null)
            {
                weaponHolder = FindInChildren(playerInstance.transform, "WeaponHolder");
                if (weaponHolder != null)
                {
                    Debug.Log($"✅ Found WeaponHolder at: {GetPath(weaponHolder)}");
                }
            }

            if (weaponHolder == null)
            {
                Debug.LogError("❌ WeaponHolder not found in Player prefab hierarchy!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", 
                    "WeaponHolder not found in Player prefab!\n\n" +
                    "Please make sure WeaponHolder exists under Camera or Player root.\n\n" +
                    "Use 'Tools > Tactical Combat > Restore WeaponHolder' to create it.",
                    "OK");
                return;
            }

            // Update WeaponSystem reference
            SerializedObject weaponSo = new SerializedObject(weaponSystem);
            var weaponHolderProp = weaponSo.FindProperty("weaponHolder");
            
            if (weaponHolderProp != null)
            {
                Transform currentReference = weaponHolderProp.objectReferenceValue as Transform;
                
                if (currentReference != weaponHolder)
                {
                    weaponHolderProp.objectReferenceValue = weaponHolder;
                    weaponSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(weaponSystem);
                    modified = true;
                    Debug.Log($"✅ WeaponHolder reference updated in WeaponSystem");
                    Debug.Log($"   Path: {GetPath(weaponHolder)}");
                }
                else
                {
                    Debug.Log("ℹ️ WeaponHolder reference already correct");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find 'weaponHolder' property in WeaponSystem");
            }

            // Also check if WeaponHolder has MuzzlePoint
            Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
            if (muzzlePoint == null)
            {
                GameObject muzzlePointGO = new GameObject("MuzzlePoint");
                muzzlePointGO.transform.SetParent(weaponHolder);
                muzzlePointGO.transform.localPosition = new Vector3(0f, 0f, 0.8f);
                muzzlePointGO.transform.localRotation = Quaternion.identity;
                muzzlePointGO.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(muzzlePointGO);
                Debug.Log("✅ Created MuzzlePoint child");
                modified = true;
            }
            else
            {
                Debug.Log("ℹ️ MuzzlePoint already exists");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ WeaponHolder reference fixed successfully!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    "WeaponHolder reference fixed!\n\n" +
                    $"Found at: {GetPath(weaponHolder)}\n\n" +
                    "WeaponSystem now has the correct reference.",
                    "OK");
            }
            else
            {
                Debug.Log("ℹ️ No changes needed - reference already correct");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
                
                Transform found = FindInChildren(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}

