using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ CLEANUP: Player prefab'daki duplicate CurrentWeapon'ları temizler ve düzeltir
    /// </summary>
    public class CurrentWeaponCleanup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Cleanup CurrentWeapon Duplicates")]
        public static void ShowWindow()
        {
            GetWindow<CurrentWeaponCleanup>("CurrentWeapon Cleanup");
        }

        private void OnGUI()
        {
            GUILayout.Label("CurrentWeapon Cleanup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.wordWrappedLabel);
            GUILayout.Label("1. Player prefab'daki duplicate CurrentWeapon'ları bulur", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Sadece bir tane bırakır (en üstteki)", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. WeaponSystem Player'da olduğundan emin olur", EditorStyles.wordWrappedLabel);
            GUILayout.Label("4. WeaponVFXController Player'da olduğundan emin olur", EditorStyles.wordWrappedLabel);
            GUILayout.Label("5. CurrentWeapon'ı aktif eder", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            if (GUILayout.Button("Cleanup Player Prefab", GUILayout.Height(30)))
            {
                CleanupPlayerPrefab();
            }
        }

        private static void CleanupPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at: " + prefabPath);
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            // 1. Find all WeaponHolders
            Transform[] weaponHolders = playerInstance.GetComponentsInChildren<Transform>();
            System.Collections.Generic.List<Transform> weaponHolderList = new System.Collections.Generic.List<Transform>();
            
            foreach (Transform t in weaponHolders)
            {
                if (t.name == "WeaponHolder")
                {
                    weaponHolderList.Add(t);
                }
            }

            Debug.Log($"🔍 Found {weaponHolderList.Count} WeaponHolder(s)");

            foreach (Transform weaponHolder in weaponHolderList)
            {
                // 2. Find all CurrentWeapon children
                System.Collections.Generic.List<Transform> currentWeapons = new System.Collections.Generic.List<Transform>();
                for (int i = 0; i < weaponHolder.childCount; i++)
                {
                    Transform child = weaponHolder.GetChild(i);
                    if (child.name == "CurrentWeapon" || child.name.Contains("CurrentWeapon"))
                    {
                        currentWeapons.Add(child);
                    }
                }

                Debug.Log($"🔍 Found {currentWeapons.Count} CurrentWeapon(s) in {weaponHolder.name}");

                if (currentWeapons.Count > 1)
                {
                    // 3. Keep only the first one, destroy others
                    for (int i = 1; i < currentWeapons.Count; i++)
                    {
                        Debug.Log($"🗑️ Destroying duplicate CurrentWeapon: {currentWeapons[i].name}");
                        Object.DestroyImmediate(currentWeapons[i].gameObject);
                        modified = true;
                    }
                }

                // 4. Activate the remaining CurrentWeapon
                if (currentWeapons.Count > 0)
                {
                    Transform currentWeapon = currentWeapons[0];
                    if (!currentWeapon.gameObject.activeSelf)
                    {
                        currentWeapon.gameObject.SetActive(true);
                        modified = true;
                        Debug.Log($"✅ Activated CurrentWeapon: {currentWeapon.name}");
                    }

                    // 5. Remove WeaponSystem from CurrentWeapon (should be on Player)
                    var weaponSystemOnWeapon = currentWeapon.GetComponent<WeaponSystem>();
                    if (weaponSystemOnWeapon != null)
                    {
                        Debug.LogWarning($"⚠️ Found WeaponSystem on CurrentWeapon! Removing it (should be on Player)");
                        Object.DestroyImmediate(weaponSystemOnWeapon);
                        modified = true;
                    }

                    // 6. Remove WeaponVFXController from CurrentWeapon (should be on Player)
                    var vfxOnWeapon = currentWeapon.GetComponent<WeaponVFXController>();
                    if (vfxOnWeapon != null)
                    {
                        Debug.LogWarning($"⚠️ Found WeaponVFXController on CurrentWeapon! Removing it (should be on Player)");
                        Object.DestroyImmediate(vfxOnWeapon);
                        modified = true;
                    }

                    // 7. Remove WeaponAudioController from CurrentWeapon (should be on Player)
                    var audioOnWeapon = currentWeapon.GetComponent<WeaponAudioController>();
                    if (audioOnWeapon != null)
                    {
                        Debug.LogWarning($"⚠️ Found WeaponAudioController on CurrentWeapon! Removing it (should be on Player)");
                        Object.DestroyImmediate(audioOnWeapon);
                        modified = true;
                    }
                    
                    // 8. Remove Network Identity from CurrentWeapon (weapon should not be networked separately)
                    var networkIdentity = currentWeapon.GetComponent<Mirror.NetworkIdentity>();
                    if (networkIdentity != null)
                    {
                        Debug.LogWarning($"⚠️ Found NetworkIdentity on CurrentWeapon! Removing it (weapon is part of player hierarchy)");
                        Object.DestroyImmediate(networkIdentity);
                        modified = true;
                    }
                    
                    // 9. Remove Network Transform from CurrentWeapon (weapon transform is synced via player)
                    // Check for any NetworkTransform component by name (Mirror has different variants)
                    var allComponents = currentWeapon.GetComponents<Component>();
                    foreach (var comp in allComponents)
                    {
                        if (comp != null)
                        {
                            string typeName = comp.GetType().Name;
                            if (typeName == "NetworkTransform" || 
                                typeName == "NetworkTransformReliable" || 
                                typeName == "NetworkTransformUnreliable" ||
                                typeName.Contains("NetworkTransform"))
                            {
                                Debug.LogWarning($"⚠️ Found {typeName} on CurrentWeapon! Removing it (weapon transform is synced via player)");
                                Object.DestroyImmediate(comp);
                                modified = true;
                                break;
                            }
                        }
                    }
                    
                    // 10. Remove Character Controller from CurrentWeapon (weapon doesn't need movement)
                    var characterController = currentWeapon.GetComponent<CharacterController>();
                    if (characterController != null)
                    {
                        Debug.LogWarning($"⚠️ Found CharacterController on CurrentWeapon! Removing it (weapon doesn't need movement)");
                        Object.DestroyImmediate(characterController);
                        modified = true;
                    }
                }
            }

            // 8. Ensure WeaponSystem is on Player (not on CurrentWeapon)
            var weaponSystemOnPlayer = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystemOnPlayer == null)
            {
                Debug.LogWarning("⚠️ WeaponSystem not found on Player! Adding it...");
                playerInstance.AddComponent<WeaponSystem>();
                modified = true;
            }

            // 9. Ensure WeaponVFXController is on Player (not on CurrentWeapon)
            var vfxOnPlayer = playerInstance.GetComponent<WeaponVFXController>();
            if (vfxOnPlayer == null)
            {
                Debug.Log("✅ Adding WeaponVFXController to Player...");
                playerInstance.AddComponent<WeaponVFXController>();
                modified = true;
            }

            // 10. Ensure WeaponAudioController is on Player (not on CurrentWeapon)
            var audioOnPlayer = playerInstance.GetComponent<WeaponAudioController>();
            if (audioOnPlayer == null)
            {
                Debug.Log("✅ Adding WeaponAudioController to Player...");
                playerInstance.AddComponent<WeaponAudioController>();
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                Debug.Log("✅ Player prefab cleaned up and saved!");
            }
            else
            {
                Debug.Log("ℹ️ Player prefab already clean");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

