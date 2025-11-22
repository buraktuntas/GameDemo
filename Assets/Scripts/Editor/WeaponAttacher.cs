using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON ATTACHER: İndirdiğiniz silahı Player'ın eline (WeaponHolder'a) ekler
    /// </summary>
    public class WeaponAttacher : EditorWindow
    {
        private GameObject weaponPrefab;
        private bool replaceExistingWeapon = true;

        [MenuItem("Tools/Tactical Combat/Attach Weapon to Player")]
        public static void ShowWindow()
        {
            GetWindow<WeaponAttacher>("Weapon Attacher");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Attacher", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool indirdiğiniz silahı Player'ın eline (WeaponHolder'a) ekler:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Weapon Prefab
            GUILayout.Label("Weapon Prefab:", EditorStyles.boldLabel);
            weaponPrefab = (GameObject)EditorGUILayout.ObjectField(
                weaponPrefab,
                typeof(GameObject),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Options
            replaceExistingWeapon = EditorGUILayout.Toggle("Replace Existing Weapon", replaceExistingWeapon);
            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(weaponPrefab == null);
            if (GUILayout.Button("Attach Weapon to Player Prefab", GUILayout.Height(40)))
            {
                AttachWeaponToPlayer();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Find WeaponHolder", GUILayout.Height(30)))
            {
                FindWeaponHolder();
            }
        }

        private void AttachWeaponToPlayer()
        {
            if (weaponPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Weapon Prefab!", "OK");
                return;
            }

            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔧 ATTACHING WEAPON TO PLAYER");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Find WeaponHolder
            Transform weaponHolder = null;
            
            // 1. Check Camera children
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                weaponHolder = playerCamera.transform.Find("WeaponHolder");
            }

            // 2. Check Player root
            if (weaponHolder == null)
            {
                weaponHolder = playerInstance.transform.Find("WeaponHolder");
            }

            // 3. Check PlayerVisual
            if (weaponHolder == null)
            {
                Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    weaponHolder = playerVisual.Find("WeaponHolder");
                }
            }

            if (weaponHolder == null)
            {
                Debug.LogError("❌ WeaponHolder not found! Please run 'Setup WeaponHolder in Player Prefab' first.");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", 
                    "WeaponHolder not found!\n\n" +
                    "Please run:\n" +
                    "Tools > Tactical Combat > Setup WeaponHolder in Player Prefab",
                    "OK");
                return;
            }

            Debug.Log($"✅ Found WeaponHolder at: {GetPath(weaponHolder)}");

            // Remove existing weapon if requested
            if (replaceExistingWeapon)
            {
                Transform existingWeapon = weaponHolder.Find("CurrentWeapon");
                if (existingWeapon == null)
                {
                    // Try to find any weapon-like child
                    for (int i = 0; i < weaponHolder.childCount; i++)
                    {
                        Transform child = weaponHolder.GetChild(i);
                        if (child.name.Contains("Weapon") || child.name.Contains("Gun") || 
                            child.name.Contains("Pistol") || child.name.Contains("Rifle"))
                        {
                            existingWeapon = child;
                            break;
                        }
                    }
                }

                if (existingWeapon != null)
                {
                    string existingName = existingWeapon.name;
                    Object.DestroyImmediate(existingWeapon.gameObject);
                    Debug.Log($"✅ Removed existing weapon: {existingName}");
                    modified = true;
                }
            }

            // Instantiate weapon
            GameObject weaponInstance = null;
            string weaponPath = AssetDatabase.GetAssetPath(weaponPrefab);
            bool isPrefab = weaponPath.EndsWith(".prefab");

            if (isPrefab)
            {
                weaponInstance = PrefabUtility.InstantiatePrefab(weaponPrefab) as GameObject;
            }
            else
            {
                // It's an FBX/model file
                weaponInstance = Instantiate(weaponPrefab);
            }

            if (weaponInstance != null)
            {
                weaponInstance.name = "CurrentWeapon";
                weaponInstance.transform.SetParent(weaponHolder);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;
                weaponInstance.transform.localScale = Vector3.one;
                
                Debug.Log($"✅ Attached weapon: {weaponPrefab.name}");
                modified = true;

                // Remove NetworkIdentity from weapon (weapon is part of player hierarchy)
                var networkIdentity = weaponInstance.GetComponent<Mirror.NetworkIdentity>();
                if (networkIdentity != null)
                {
                    Object.DestroyImmediate(networkIdentity);
                    Debug.Log("✅ Removed NetworkIdentity from weapon");
                    modified = true;
                }

                // Remove NetworkTransform from weapon
                var networkTransform = weaponInstance.GetComponent("NetworkTransformReliable") ?? 
                                      weaponInstance.GetComponent("NetworkTransform") ??
                                      weaponInstance.GetComponent("NetworkTransformUnreliable");
                if (networkTransform != null)
                {
                    Object.DestroyImmediate(networkTransform as Component);
                    Debug.Log("✅ Removed NetworkTransform from weapon");
                    modified = true;
                }

                // Ensure WeaponSystem is on Player (not on weapon)
                var weaponSystemOnWeapon = weaponInstance.GetComponent<WeaponSystem>();
                if (weaponSystemOnWeapon != null)
                {
                    Debug.LogWarning("⚠️ WeaponSystem found on weapon - it should be on Player");
                    // Don't remove it automatically, just warn
                }

                // Find MuzzlePoint in weapon and position it correctly
                Transform weaponMuzzle = FindChildByName(weaponInstance.transform, new[] { "Muzzle", "MuzzleFlash", "FirePoint", "Barrel", "GunTip" });
                if (weaponMuzzle != null)
                {
                    // Update WeaponHolder's MuzzlePoint position
                    Transform muzzlePoint = weaponHolder.Find("MuzzlePoint");
                    if (muzzlePoint != null)
                    {
                        // Calculate local position relative to WeaponHolder
                        Vector3 localMuzzlePos = weaponHolder.InverseTransformPoint(weaponMuzzle.position);
                        muzzlePoint.localPosition = localMuzzlePos;
                        Debug.Log($"✅ Updated MuzzlePoint position: {localMuzzlePos}");
                        modified = true;
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Failed to instantiate weapon!");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ Weapon attached to Player prefab!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    $"Weapon attached successfully!\n\n" +
                    $"Weapon: {weaponPrefab.name}\n" +
                    $"Location: WeaponHolder/CurrentWeapon",
                    "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private void FindWeaponHolder()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            Transform weaponHolder = null;
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                weaponHolder = playerCamera.transform.Find("WeaponHolder");
            }

            if (weaponHolder == null)
            {
                weaponHolder = playerInstance.transform.Find("WeaponHolder");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (weaponHolder != null)
            {
                EditorUtility.DisplayDialog("WeaponHolder Found", 
                    $"WeaponHolder found at:\n{GetPath(weaponHolder)}\n\n" +
                    $"Position: {weaponHolder.localPosition}\n" +
                    $"Children: {weaponHolder.childCount}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("WeaponHolder Not Found", 
                    "WeaponHolder not found!\n\n" +
                    "Please run:\n" +
                    "Tools > Tactical Combat > Setup WeaponHolder in Player Prefab",
                    "OK");
            }
        }

        private Transform FindChildByName(Transform parent, string[] names)
        {
            foreach (Transform child in parent)
            {
                foreach (string name in names)
                {
                    if (child.name.Contains(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }

                Transform found = FindChildByName(child, names);
                if (found != null) return found;
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

