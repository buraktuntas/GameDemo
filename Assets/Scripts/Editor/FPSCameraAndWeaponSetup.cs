using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ FPS CAMERA & WEAPON SETUP: Valorant tarzı FPS kamera ve silah konumlarını ayarlar
    /// </summary>
    public class FPSCameraAndWeaponSetup : EditorWindow
    {
        [Header("Camera Settings")]
        private Vector3 cameraPosition = new Vector3(0f, 1.5f, 0f); // Valorant tarzı: biraz daha alçak
        private float cameraFOV = 75f; // Valorant default FOV
        
        [Header("Weapon Holder Settings")]
        private Vector3 weaponHolderPosition = new Vector3(0.3f, -0.2f, 0.5f); // Valorant tarzı: sağ alt ön
        private Vector3 weaponHolderRotation = new Vector3(0f, 0f, 0f);
        private Vector3 weaponHolderScale = Vector3.one;

        [MenuItem("Tools/Tactical Combat/Setup FPS Camera & Weapon Position")]
        public static void ShowWindow()
        {
            GetWindow<FPSCameraAndWeaponSetup>("FPS Camera & Weapon Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("FPS Camera & Weapon Position Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Valorant tarzı FPS kamera ve silah konumlarını ayarlar:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Camera Settings
            GUILayout.Label("Camera Settings:", EditorStyles.boldLabel);
            cameraPosition = EditorGUILayout.Vector3Field("Camera Position (Local)", cameraPosition);
            cameraFOV = EditorGUILayout.FloatField("Camera FOV", cameraFOV);
            GUILayout.Space(10);

            // Weapon Holder Settings
            GUILayout.Label("Weapon Holder Settings:", EditorStyles.boldLabel);
            weaponHolderPosition = EditorGUILayout.Vector3Field("Weapon Position (Local)", weaponHolderPosition);
            weaponHolderRotation = EditorGUILayout.Vector3Field("Weapon Rotation (Local)", weaponHolderRotation);
            weaponHolderScale = EditorGUILayout.Vector3Field("Weapon Scale", weaponHolderScale);
            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(!HasPlayerPrefab());
            if (GUILayout.Button("Apply to Player Prefab", GUILayout.Height(30)))
            {
                ApplyFPSSettings();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Use Valorant Preset", GUILayout.Height(30)))
            {
                LoadValorantPreset();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Use CS:GO Preset", GUILayout.Height(30)))
            {
                LoadCSGOPreset();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Use Battlefield Preset", GUILayout.Height(30)))
            {
                LoadBattlefieldPreset();
            }
        }

        private static bool HasPlayerPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab") != null;
        }

        private void LoadValorantPreset()
        {
            cameraPosition = new Vector3(0f, 1.5f, 0f); // Biraz alçak (silahı görmek için)
            cameraFOV = 75f; // Valorant default
            weaponHolderPosition = new Vector3(0.3f, -0.2f, 0.5f); // Sağ alt ön
            weaponHolderRotation = Vector3.zero;
            weaponHolderScale = Vector3.one;
            Debug.Log("✅ Valorant preset loaded");
        }

        private void LoadCSGOPreset()
        {
            cameraPosition = new Vector3(0f, 1.6f, 0f); // Standart göz hizası
            cameraFOV = 90f; // CS:GO default
            weaponHolderPosition = new Vector3(0.35f, -0.15f, 0.6f); // Biraz daha önde
            weaponHolderRotation = Vector3.zero;
            weaponHolderScale = Vector3.one;
            Debug.Log("✅ CS:GO preset loaded");
        }

        private void LoadBattlefieldPreset()
        {
            cameraPosition = new Vector3(0f, 1.65f, 0f); // Biraz yüksek
            cameraFOV = 70f; // Battlefield default
            weaponHolderPosition = new Vector3(0.4f, -0.1f, 0.7f); // Daha önde ve yukarıda
            weaponHolderRotation = Vector3.zero;
            weaponHolderScale = Vector3.one;
            Debug.Log("✅ Battlefield preset loaded");
        }

        private void ApplyFPSSettings()
        {
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
            Debug.Log("🎮 FPS CAMERA & WEAPON SETUP");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. Setup Camera Position
            FPSController fpsController = playerInstance.GetComponent<FPSController>();
            if (fpsController != null)
            {
                SerializedObject fpsSo = new SerializedObject(fpsController);
                
                // Find or create camera
                Camera playerCamera = fpsController.playerCamera;
                if (playerCamera == null)
                {
                    // Try to find camera in children
                    playerCamera = playerInstance.GetComponentInChildren<Camera>();
                }

                if (playerCamera != null)
                {
                    // Set camera position
                    playerCamera.transform.localPosition = cameraPosition;
                    playerCamera.fieldOfView = cameraFOV;
                    EditorUtility.SetDirty(playerCamera);
                    Debug.Log($"✅ Camera position set to: {cameraPosition}, FOV: {cameraFOV}");
                    modified = true;

                    // Note: originalCameraPos is a private field, but it will be updated at runtime
                    // when SetupCamera() is called. The camera position is set correctly above.
                    fpsSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(fpsController);
                    Debug.Log($"✅ FPSController camera reference updated");
                }
                else
                {
                    Debug.LogWarning("⚠️ No camera found - camera will be created at runtime");
                }
            }

            // 2. Setup WeaponHolder Position
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                SerializedObject weaponSo = new SerializedObject(weaponSystem);
                var weaponHolderProp = weaponSo.FindProperty("weaponHolder");
                
                Transform weaponHolder = null;
                if (weaponHolderProp != null)
                {
                    weaponHolder = weaponHolderProp.objectReferenceValue as Transform;
                }

                // Try to find WeaponHolder if not assigned
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
                    GameObject weaponHolderGO = new GameObject("WeaponHolder");
                    weaponHolderGO.transform.SetParent(playerInstance.transform);
                    weaponHolder = weaponHolderGO.transform;
                    Debug.Log("✅ Created WeaponHolder GameObject");
                    modified = true;
                }

                if (weaponHolder != null)
                {
                    // Set WeaponHolder position relative to camera
                    // WeaponHolder should be a child of the camera or player
                    // For Valorant style, we'll make it a child of the camera
                    
                    // Find camera to attach weapon to
                    Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
                    if (playerCamera != null)
                    {
                        // Make WeaponHolder a child of camera
                        if (weaponHolder.parent != playerCamera.transform)
                        {
                            weaponHolder.SetParent(playerCamera.transform);
                            Debug.Log("✅ WeaponHolder attached to camera");
                            modified = true;
                        }

                        // Set local position relative to camera
                        weaponHolder.localPosition = weaponHolderPosition;
                        weaponHolder.localRotation = Quaternion.Euler(weaponHolderRotation);
                        weaponHolder.localScale = weaponHolderScale;
                        
                        EditorUtility.SetDirty(weaponHolder);
                        Debug.Log($"✅ WeaponHolder position set to: {weaponHolderPosition}");
                        Debug.Log($"✅ WeaponHolder rotation set to: {weaponHolderRotation}");
                        modified = true;

                        // Update WeaponSystem reference
                        if (weaponHolderProp != null)
                        {
                            weaponHolderProp.objectReferenceValue = weaponHolder;
                            weaponSo.ApplyModifiedProperties();
                            EditorUtility.SetDirty(weaponSystem);
                            Debug.Log("✅ WeaponSystem.weaponHolder reference updated");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No camera found - WeaponHolder will be positioned relative to player");
                        // Fallback: position relative to player
                        weaponHolder.localPosition = weaponHolderPosition;
                        weaponHolder.localRotation = Quaternion.Euler(weaponHolderRotation);
                        weaponHolder.localScale = weaponHolderScale;
                        EditorUtility.SetDirty(weaponHolder);
                        modified = true;
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ WeaponSystem not found on Player prefab");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ Player prefab updated with FPS camera and weapon positions!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    "FPS Camera & Weapon positions applied successfully!\n\n" +
                    $"Camera: {cameraPosition}\n" +
                    $"FOV: {cameraFOV}\n" +
                    $"Weapon: {weaponHolderPosition}", 
                    "OK");
            }
            else
            {
                Debug.Log("ℹ️ No changes made");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

