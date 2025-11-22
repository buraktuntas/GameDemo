using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ PLAYER PREFAB WEAPONHOLDER SETUP: WeaponHolder'ı Player prefab'ına kesinlikle ekler
    /// </summary>
    public class PlayerPrefabWeaponHolderSetup
    {
        [MenuItem("Tools/Tactical Combat/Setup WeaponHolder in Player Prefab")]
        public static void SetupWeaponHolderInPlayerPrefab()
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
            Debug.Log("🔧 SETTING UP WEAPONHOLDER IN PLAYER PREFAB");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. Find or create Camera
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                // Try to find via FPSController
                var fpsController = playerInstance.GetComponent<Player.FPSController>();
                if (fpsController != null)
                {
                    playerCamera = fpsController.GetCamera();
                }
            }

            if (playerCamera == null)
            {
                Debug.LogError("❌ Player camera not found! Cannot setup WeaponHolder.");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", "Player camera not found! Please ensure FPSController is set up.", "OK");
                return;
            }

            Debug.Log($"✅ Found camera: {playerCamera.name}");

            // 2. Check if WeaponHolder already exists under camera
            Transform weaponHolder = playerCamera.transform.Find("WeaponHolder");
            
            if (weaponHolder == null)
            {
                // Create WeaponHolder as child of camera
                GameObject weaponHolderGO = new GameObject("WeaponHolder");
                weaponHolderGO.transform.SetParent(playerCamera.transform);
                weaponHolderGO.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f); // Valorant style
                weaponHolderGO.transform.localRotation = Quaternion.identity;
                weaponHolderGO.transform.localScale = Vector3.one;
                weaponHolder = weaponHolderGO.transform;
                
                Debug.Log("✅ Created WeaponHolder under Camera");
                modified = true;
            }
            else
            {
                Debug.Log("ℹ️ WeaponHolder already exists under Camera");
                
                // Ensure it's positioned correctly
                if (weaponHolder.localPosition != new Vector3(0.3f, -0.2f, 0.5f))
                {
                    weaponHolder.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                    weaponHolder.localRotation = Quaternion.identity;
                    weaponHolder.localScale = Vector3.one;
                    EditorUtility.SetDirty(weaponHolder);
                    modified = true;
                    Debug.Log("✅ Updated WeaponHolder position");
                }
            }

            // 3. Create MuzzlePoint if it doesn't exist
            if (weaponHolder != null)
            {
                Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
                if (muzzlePoint == null)
                {
                    GameObject muzzlePointGO = new GameObject("MuzzlePoint");
                    muzzlePointGO.transform.SetParent(weaponHolder);
                    muzzlePointGO.transform.localPosition = new Vector3(0f, 0f, 0.8f);
                    muzzlePointGO.transform.localRotation = Quaternion.identity;
                    muzzlePointGO.transform.localScale = Vector3.one;
                    EditorUtility.SetDirty(muzzlePointGO);
                    Debug.Log("✅ Created MuzzlePoint");
                    modified = true;
                }
                else
                {
                    Debug.Log("ℹ️ MuzzlePoint already exists");
                }
            }

            // 4. Update WeaponSystem reference
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null && weaponHolder != null)
            {
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
                        Debug.Log("✅ WeaponHolder reference assigned to WeaponSystem");
                    }
                    else
                    {
                        Debug.Log("ℹ️ WeaponSystem already has correct WeaponHolder reference");
                    }
                }
            }
            else if (weaponSystem == null)
            {
                Debug.LogWarning("⚠️ WeaponSystem not found on Player prefab");
            }

            // 5. Save prefab
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ Player prefab updated with WeaponHolder!");
                Debug.Log($"   WeaponHolder path: {playerCamera.name}/WeaponHolder");
                Debug.Log($"   Position: {weaponHolder.localPosition}");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    "WeaponHolder setup complete!\n\n" +
                    $"Location: {playerCamera.name}/WeaponHolder\n" +
                    $"Position: {weaponHolder.localPosition}\n\n" +
                    "WeaponHolder is now saved in Player prefab.",
                    "OK");
            }
            else
            {
                Debug.Log("ℹ️ No changes needed - WeaponHolder already set up correctly");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

