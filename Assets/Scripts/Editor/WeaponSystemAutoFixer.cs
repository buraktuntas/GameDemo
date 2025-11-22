using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ AUTO FIX: WeaponSystem ve WeaponHolder referanslarını otomatik düzeltir
    /// </summary>
    public class WeaponSystemAutoFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Auto Fix WeaponSystem References")]
        public static void ShowWindow()
        {
            GetWindow<WeaponSystemAutoFixer>("WeaponSystem Auto Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("WeaponSystem Auto Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Fix Player Prefab", GUILayout.Height(30)))
            {
                FixPlayerPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Fix All Players in Scene", GUILayout.Height(30)))
            {
                FixAllPlayersInScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Fix Selected GameObject", GUILayout.Height(30)))
            {
                FixSelectedGameObject();
            }
        }

        private static void FixPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at: " + prefabPath);
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = FixWeaponSystemOnPlayer(playerInstance);

            if (modified)
            {
                // ✅ CRITICAL FIX: Mark all modified components as dirty before saving
                EditorUtility.SetDirty(playerInstance);
                
                // ✅ CRITICAL FIX: Save prefab with all modifications
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                
                // ✅ CRITICAL FIX: Refresh asset database to see changes in Inspector
                AssetDatabase.Refresh();
                
                Debug.Log("✅ Player prefab fixed and saved! Please check Inspector to verify prefab references.");
            }
            else
            {
                Debug.Log("ℹ️ Player prefab already configured correctly");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static void FixAllPlayersInScene()
        {
            var players = FindObjectsByType<WeaponSystem>(FindObjectsSortMode.None);
            int fixedCount = 0;

            foreach (var weaponSystem in players)
            {
                if (FixWeaponSystemOnPlayer(weaponSystem.gameObject))
                {
                    fixedCount++;
                }
            }

            Debug.Log($"✅ Fixed {fixedCount} WeaponSystem(s) in scene");
        }

        private static void FixSelectedGameObject()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("⚠️ No GameObject selected!");
                return;
            }

            var weaponSystem = selected.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogWarning("⚠️ Selected GameObject doesn't have WeaponSystem component!");
                return;
            }

            if (FixWeaponSystemOnPlayer(selected))
            {
                Debug.Log("✅ Selected GameObject fixed!");
            }
        }

        private static bool FixWeaponSystemOnPlayer(GameObject player)
        {
            bool modified = false;

            var weaponSystem = player.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogWarning($"⚠️ WeaponSystem not found on {player.name}");
                return false;
            }

            SerializedObject so = new SerializedObject(weaponSystem);

            // Get WeaponHolder reference (will be used later for CurrentWeapon fix)
            Transform weaponHolderTransform = null;

            // 1. Fix WeaponHolder
            var weaponHolderProp = so.FindProperty("weaponHolder");
            if (weaponHolderProp != null && weaponHolderProp.objectReferenceValue == null)
            {
                weaponHolderTransform = player.transform.Find("WeaponHolder");
                if (weaponHolderTransform == null)
                {
                    // Try to find in PlayerVisuals
                    var playerVisuals = player.transform.Find("PlayerVisual");
                    if (playerVisuals != null)
                    {
                        weaponHolderTransform = playerVisuals.Find("WeaponHolder");
                    }
                }

                if (weaponHolderTransform == null)
                {
                    // Create WeaponHolder
                    GameObject holderGO = new GameObject("WeaponHolder");
                    holderGO.transform.SetParent(player.transform);
                    holderGO.transform.localPosition = new Vector3(0.3f, 1.4f, 0.5f); // Right hand position
                    holderGO.transform.localRotation = Quaternion.identity;
                    weaponHolderTransform = holderGO.transform;
                    Debug.Log($"✅ Created WeaponHolder for {player.name}");
                }

                weaponHolderProp.objectReferenceValue = weaponHolderTransform;
                modified = true;
                Debug.Log($"✅ WeaponHolder assigned to {player.name}");
            }

            // 2. Fix Player Camera
            var cameraProp = so.FindProperty("playerCamera");
            if (cameraProp != null && cameraProp.objectReferenceValue == null)
            {
                Camera playerCamera = null;

                // Try to find camera in FPSController
                var fpsController = player.GetComponent<Player.FPSController>();
                if (fpsController != null)
                {
                    playerCamera = fpsController.GetCamera();
                }

                // Fallback: Find camera in children
                if (playerCamera == null)
                {
                    playerCamera = player.GetComponentInChildren<Camera>();
                }

                // Fallback: Find by name
                if (playerCamera == null)
                {
                    Transform cameraTransform = player.transform.Find("PlayerCamera");
                    if (cameraTransform != null)
                    {
                        playerCamera = cameraTransform.GetComponent<Camera>();
                    }
                }

                if (playerCamera != null)
                {
                    cameraProp.objectReferenceValue = playerCamera;
                    modified = true;
                    Debug.Log($"✅ Player Camera assigned to {player.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Camera not found for {player.name}");
                }
            }

            // 3. Fix WeaponConfig (Current Weapon)
            var weaponConfigProp = so.FindProperty("currentWeapon");
            if (weaponConfigProp != null && weaponConfigProp.objectReferenceValue == null)
            {
                // Try to find default weapon config
                WeaponConfig defaultConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/ScriptableObjects/Weapons/Config_Pistol_.asset");
                
                if (defaultConfig == null)
                {
                    // Try to find any weapon config
                    string[] guids = AssetDatabase.FindAssets("t:WeaponConfig");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        defaultConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
                    }
                }

                if (defaultConfig != null)
                {
                    weaponConfigProp.objectReferenceValue = defaultConfig;
                    modified = true;
                    Debug.Log($"✅ WeaponConfig assigned to {player.name}: {defaultConfig.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ No WeaponConfig found for {player.name}");
                }
            }

            // 4. Ensure WeaponAudioController exists and fix references
            var audioController = weaponSystem.GetComponent<WeaponAudioController>();
            if (audioController == null)
            {
                audioController = weaponSystem.gameObject.AddComponent<WeaponAudioController>();
                modified = true;
                Debug.Log($"✅ WeaponAudioController added to {player.name}");
            }
            
            // Fix WeaponAudioController references
            if (audioController != null)
            {
                SerializedObject audioSo = new SerializedObject(audioController);
                bool audioModified = false;
                
                // Try to find fire sounds
                var fireSoundsProp = audioSo.FindProperty("fireSounds");
                if (fireSoundsProp != null && (fireSoundsProp.arraySize == 0 || fireSoundsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
                {
                    // Try to find fire sound clips
                    string[] fireSoundGuids = AssetDatabase.FindAssets("fire t:AudioClip");
                    if (fireSoundGuids.Length > 0)
                    {
                        fireSoundsProp.arraySize = Mathf.Min(fireSoundGuids.Length, 3); // Max 3 sounds
                        for (int i = 0; i < fireSoundsProp.arraySize; i++)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(fireSoundGuids[i]);
                            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                            fireSoundsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                        }
                        audioModified = true;
                        Debug.Log($"✅ Fire sounds assigned to WeaponAudioController on {player.name}");
                    }
                }
                
                if (audioModified)
                {
                    audioSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(audioController);
                    modified = true; // Mark player as modified so prefab gets saved
                }
            }

            // 5. Ensure WeaponVFXController exists and fix references
            var vfxController = weaponSystem.GetComponent<WeaponVFXController>();
            if (vfxController == null)
            {
                vfxController = weaponSystem.gameObject.AddComponent<WeaponVFXController>();
                modified = true;
                Debug.Log($"✅ WeaponVFXController added to {player.name}");
            }
            
            // Fix WeaponVFXController prefab references
            if (vfxController != null)
            {
                SerializedObject vfxSo = new SerializedObject(vfxController);
                bool vfxModified = false;
                
                // Muzzle Flash Prefab
                var muzzleFlashProp = vfxSo.FindProperty("muzzleFlashPrefab");
                if (muzzleFlashProp != null && muzzleFlashProp.objectReferenceValue == null)
                {
                    GameObject muzzleFlash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MuzzleFlash.prefab");
                    if (muzzleFlash == null)
                    {
                        // Try to find any muzzle flash prefab
                        string[] guids = AssetDatabase.FindAssets("MuzzleFlash t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            muzzleFlash = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        }
                    }
                    if (muzzleFlash != null)
                    {
                        muzzleFlashProp.objectReferenceValue = muzzleFlash;
                        vfxModified = true;
                        Debug.Log($"✅ MuzzleFlash prefab assigned to WeaponVFXController on {player.name}");
                    }
                }
                
                // Hit Effect Prefab
                var hitEffectProp = vfxSo.FindProperty("hitEffectPrefab");
                if (hitEffectProp != null && hitEffectProp.objectReferenceValue == null)
                {
                    GameObject hitEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/HitEffect.prefab");
                    if (hitEffect == null)
                    {
                        string[] guids = AssetDatabase.FindAssets("HitEffect t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            hitEffect = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        }
                    }
                    if (hitEffect != null)
                    {
                        hitEffectProp.objectReferenceValue = hitEffect;
                        vfxModified = true;
                        Debug.Log($"✅ HitEffect prefab assigned to WeaponVFXController on {player.name}");
                    }
                }
                
                // Bullet Hole Prefab
                var bulletHoleProp = vfxSo.FindProperty("bulletHolePrefab");
                if (bulletHoleProp != null && bulletHoleProp.objectReferenceValue == null)
                {
                    GameObject bulletHole = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/BulletHole.prefab");
                    if (bulletHole == null)
                    {
                        string[] guids = AssetDatabase.FindAssets("BulletHole t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            bulletHole = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        }
                    }
                    if (bulletHole != null)
                    {
                        bulletHoleProp.objectReferenceValue = bulletHole;
                        vfxModified = true;
                        Debug.Log($"✅ BulletHole prefab assigned to WeaponVFXController on {player.name}");
                    }
                }
                
                // Blood Effect Prefab
                var bloodEffectProp = vfxSo.FindProperty("bloodEffectPrefab");
                if (bloodEffectProp != null && bloodEffectProp.objectReferenceValue == null)
                {
                    GameObject bloodEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/BloodEffect.prefab");
                    if (bloodEffect == null)
                    {
                        string[] guids = AssetDatabase.FindAssets("BloodEffect t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            bloodEffect = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        }
                    }
                    if (bloodEffect != null)
                    {
                        bloodEffectProp.objectReferenceValue = bloodEffect;
                        vfxModified = true;
                        Debug.Log($"✅ BloodEffect prefab assigned to WeaponVFXController on {player.name}");
                    }
                }
                
                // Metal Sparks Prefab
                var metalSparksProp = vfxSo.FindProperty("metalSparksPrefab");
                if (metalSparksProp != null && metalSparksProp.objectReferenceValue == null)
                {
                    GameObject metalSparks = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MetalSparks.prefab");
                    if (metalSparks == null)
                    {
                        string[] guids = AssetDatabase.FindAssets("MetalSparks t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            metalSparks = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        }
                    }
                    if (metalSparks != null)
                    {
                        metalSparksProp.objectReferenceValue = metalSparks;
                        vfxModified = true;
                        Debug.Log($"✅ MetalSparks prefab assigned to WeaponVFXController on {player.name}");
                    }
                }
                
                if (vfxModified)
                {
                    vfxSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(vfxController);
                    // ✅ CRITICAL FIX: Force update serialized object
                    vfxSo.Update();
                    modified = true; // Mark player as modified so prefab gets saved
                    Debug.Log($"✅ [WeaponSystemAutoFixer] WeaponVFXController prefabs assigned and marked as dirty");
                }
            }

            // 6. Ensure AudioSource exists
            var audioSource = weaponSystem.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                weaponSystem.gameObject.AddComponent<AudioSource>();
                modified = true;
                Debug.Log($"✅ AudioSource added to {player.name}");
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(weaponSystem);
            }

            // ✅ CRITICAL FIX: Also fix WeaponVFXController and WeaponAudioController on CurrentWeapon GameObject
            // CurrentWeapon is a child of WeaponHolder, and it might have its own controllers
            // Get WeaponHolder if not already found
            if (weaponHolderTransform == null)
            {
                var weaponHolderProp2 = so.FindProperty("weaponHolder");
                if (weaponHolderProp2 != null)
                {
                    weaponHolderTransform = weaponHolderProp2.objectReferenceValue as Transform;
                }
                
                if (weaponHolderTransform == null)
                {
                    // Try to find WeaponHolder in player
                    weaponHolderTransform = player.transform.Find("WeaponHolder");
                    if (weaponHolderTransform == null)
                    {
                        var playerVisuals = player.transform.Find("PlayerVisual");
                        if (playerVisuals != null)
                        {
                            weaponHolderTransform = playerVisuals.Find("WeaponHolder");
                        }
                    }
                }
            }
            
            if (weaponHolderTransform != null)
            {
                // Find CurrentWeapon GameObject
                Transform currentWeapon = weaponHolderTransform.Find("CurrentWeapon");
                if (currentWeapon == null)
                {
                    // Try to find any weapon child
                    for (int i = 0; i < weaponHolderTransform.childCount; i++)
                    {
                        Transform child = weaponHolderTransform.GetChild(i);
                        if (child.name.Contains("Weapon") || child.name.Contains("Pistol") || child.name.Contains("Gun"))
                        {
                            currentWeapon = child;
                            break;
                        }
                    }
                }

                if (currentWeapon != null)
                {
                    // Fix WeaponVFXController on CurrentWeapon
                    var weaponVFX = currentWeapon.GetComponent<WeaponVFXController>();
                    if (weaponVFX != null)
                    {
                        SerializedObject weaponVFXSo = new SerializedObject(weaponVFX);
                        bool weaponVFXModified = false;

                        // Copy prefab references from Player's WeaponVFXController
                        if (vfxController != null)
                        {
                            SerializedObject playerVFXSo = new SerializedObject(vfxController);
                            
                            var playerMuzzleFlash = playerVFXSo.FindProperty("muzzleFlashPrefab");
                            var weaponMuzzleFlash = weaponVFXSo.FindProperty("muzzleFlashPrefab");
                            if (playerMuzzleFlash != null && weaponMuzzleFlash != null && 
                                playerMuzzleFlash.objectReferenceValue != null && weaponMuzzleFlash.objectReferenceValue == null)
                            {
                                weaponMuzzleFlash.objectReferenceValue = playerMuzzleFlash.objectReferenceValue;
                                weaponVFXModified = true;
                            }

                            var playerHitEffect = playerVFXSo.FindProperty("hitEffectPrefab");
                            var weaponHitEffect = weaponVFXSo.FindProperty("hitEffectPrefab");
                            if (playerHitEffect != null && weaponHitEffect != null && 
                                playerHitEffect.objectReferenceValue != null && weaponHitEffect.objectReferenceValue == null)
                            {
                                weaponHitEffect.objectReferenceValue = playerHitEffect.objectReferenceValue;
                                weaponVFXModified = true;
                            }

                            var playerBulletHole = playerVFXSo.FindProperty("bulletHolePrefab");
                            var weaponBulletHole = weaponVFXSo.FindProperty("bulletHolePrefab");
                            if (playerBulletHole != null && weaponBulletHole != null && 
                                playerBulletHole.objectReferenceValue != null && weaponBulletHole.objectReferenceValue == null)
                            {
                                weaponBulletHole.objectReferenceValue = playerBulletHole.objectReferenceValue;
                                weaponVFXModified = true;
                            }

                            var playerBloodEffect = playerVFXSo.FindProperty("bloodEffectPrefab");
                            var weaponBloodEffect = weaponVFXSo.FindProperty("bloodEffectPrefab");
                            if (playerBloodEffect != null && weaponBloodEffect != null && 
                                playerBloodEffect.objectReferenceValue != null && weaponBloodEffect.objectReferenceValue == null)
                            {
                                weaponBloodEffect.objectReferenceValue = playerBloodEffect.objectReferenceValue;
                                weaponVFXModified = true;
                            }

                            var playerMetalSparks = playerVFXSo.FindProperty("metalSparksPrefab");
                            var weaponMetalSparks = weaponVFXSo.FindProperty("metalSparksPrefab");
                            if (playerMetalSparks != null && weaponMetalSparks != null && 
                                playerMetalSparks.objectReferenceValue != null && weaponMetalSparks.objectReferenceValue == null)
                            {
                                weaponMetalSparks.objectReferenceValue = playerMetalSparks.objectReferenceValue;
                                weaponVFXModified = true;
                            }

                            if (weaponVFXModified)
                            {
                                weaponVFXSo.ApplyModifiedProperties();
                                EditorUtility.SetDirty(weaponVFX);
                                Debug.Log($"✅ WeaponVFXController prefabs copied to CurrentWeapon on {player.name}");
                            }
                        }
                    }

                    // Fix WeaponAudioController on CurrentWeapon
                    var weaponAudio = currentWeapon.GetComponent<WeaponAudioController>();
                    if (weaponAudio != null && audioController != null)
                    {
                        SerializedObject weaponAudioSo = new SerializedObject(weaponAudio);
                        SerializedObject playerAudioSo = new SerializedObject(audioController);
                        bool weaponAudioModified = false;

                        var playerFireSounds = playerAudioSo.FindProperty("fireSounds");
                        var weaponFireSounds = weaponAudioSo.FindProperty("fireSounds");
                        if (playerFireSounds != null && weaponFireSounds != null && 
                            playerFireSounds.arraySize > 0 && weaponFireSounds.arraySize == 0)
                        {
                            weaponFireSounds.arraySize = playerFireSounds.arraySize;
                            for (int i = 0; i < playerFireSounds.arraySize; i++)
                            {
                                var playerClip = playerFireSounds.GetArrayElementAtIndex(i);
                                var weaponClip = weaponFireSounds.GetArrayElementAtIndex(i);
                                weaponClip.objectReferenceValue = playerClip.objectReferenceValue;
                            }
                            weaponAudioModified = true;
                        }

                        var playerReloadSound = playerAudioSo.FindProperty("reloadSound");
                        var weaponReloadSound = weaponAudioSo.FindProperty("reloadSound");
                        if (playerReloadSound != null && weaponReloadSound != null && 
                            playerReloadSound.objectReferenceValue != null && weaponReloadSound.objectReferenceValue == null)
                        {
                            weaponReloadSound.objectReferenceValue = playerReloadSound.objectReferenceValue;
                            weaponAudioModified = true;
                        }

                        if (weaponAudioModified)
                        {
                            weaponAudioSo.ApplyModifiedProperties();
                            EditorUtility.SetDirty(weaponAudio);
                            Debug.Log($"✅ WeaponAudioController sounds copied to CurrentWeapon on {player.name}");
                        }
                    }
                }
            }

            return modified;
        }
    }
}

