using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;
using TacticalCombat.Core;
using TacticalCombat.Combat;
using TacticalCombat.Building;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Player prefab'ını sıfırdan oluşturur (FINAL - NO CONFLICTS!)
    /// </summary>
    public class PlayerPrefabRecreator
    {
        [MenuItem("Tools/Tactical Combat/Recreate Player Prefab (FINAL)", false, 1)]
        public static void RecreatePlayerPrefab()
        {
            // Mevcut prefab'ı sil
            string prefabPath = "Assets/Prefabs/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
                Debug.Log("🗑️ Old Player prefab deleted");
            }
            
            // Yeni Player GameObject oluştur
            GameObject player = new GameObject("Player");
            
            // ═══════════════════════════════════════════════════════════
            // NETWORK COMPONENTS (MUST BE FIRST!)
            // ═══════════════════════════════════════════════════════════
            
            player.AddComponent<Mirror.NetworkIdentity>();
            player.AddComponent<Mirror.NetworkTransformReliable>();
            
            // ═══════════════════════════════════════════════════════════
            // MOVEMENT & PHYSICS
            // ═══════════════════════════════════════════════════════════
            
            CharacterController charController = player.AddComponent<CharacterController>();
            charController.height = 2f;
            charController.radius = 0.5f;
            charController.center = new Vector3(0, 1, 0);
            charController.slopeLimit = 45f;
            charController.stepOffset = 0.3f;
            
            // ⭐ ONLY FPSController - NO PlayerController!
            FPSController fpsController = player.AddComponent<FPSController>();
            
            // ⭐ PlayerController sadece network/team yönetimi için (hareket yapmaz!)
            PlayerController playerController = player.AddComponent<PlayerController>();
            
            // ═══════════════════════════════════════════════════════════
            // VISUALS & UI
            // ═══════════════════════════════════════════════════════════
            
            PlayerVisuals playerVisuals = player.AddComponent<PlayerVisuals>();
            TacticalCombat.UI.SimpleCrosshair crosshair = player.AddComponent<TacticalCombat.UI.SimpleCrosshair>();
            
            // ═══════════════════════════════════════════════════════════
            // COMBAT
            // ═══════════════════════════════════════════════════════════
            
            TacticalCombat.Combat.Health health = player.AddComponent<TacticalCombat.Combat.Health>();
            TacticalCombat.Combat.SimpleGun simpleGun = player.AddComponent<TacticalCombat.Combat.SimpleGun>();
            
            // Configure gun
            SerializedObject serializedGun = new SerializedObject(simpleGun);
            serializedGun.FindProperty("damage").floatValue = 25f;
            serializedGun.FindProperty("range").floatValue = 100f;
            serializedGun.FindProperty("fireRate").floatValue = 0.5f;
            serializedGun.ApplyModifiedProperties();
            
            // ═══════════════════════════════════════════════════════════
            // BUILDING SYSTEM (Valheim style!)
            // ═══════════════════════════════════════════════════════════
            
            TacticalCombat.Building.SimpleBuildMode simpleBuildMode = player.AddComponent<TacticalCombat.Building.SimpleBuildMode>();
            
            // Try to find structure prefabs
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Wall.prefab");
            GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Floor.prefab");
            GameObject roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Roof.prefab");
            GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Door.prefab");
            GameObject windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Window.prefab");
            GameObject stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Stairs.prefab");
            
            // Fallback to old wall prefab
            if (wallPrefab == null)
            {
                wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wall.prefab");
            }
            
            if (wallPrefab != null)
            {
                SerializedObject serializedBuildMode = new SerializedObject(simpleBuildMode);
                
                // Assign all structure prefabs
                serializedBuildMode.FindProperty("wallPrefab").objectReferenceValue = wallPrefab;
                serializedBuildMode.FindProperty("floorPrefab").objectReferenceValue = floorPrefab;
                serializedBuildMode.FindProperty("roofPrefab").objectReferenceValue = roofPrefab;
                serializedBuildMode.FindProperty("doorPrefab").objectReferenceValue = doorPrefab;
                serializedBuildMode.FindProperty("windowPrefab").objectReferenceValue = windowPrefab;
                serializedBuildMode.FindProperty("stairsPrefab").objectReferenceValue = stairsPrefab;
                
                // ⭐ ONLY GROUND LAYER (not everything!)
                LayerMask groundMask = LayerMask.GetMask("Ground", "Terrain");
                if (groundMask == 0) groundMask = LayerMask.GetMask("Default"); // Fallback
                serializedBuildMode.FindProperty("groundLayer").intValue = groundMask;
                
                // Configure build settings
                serializedBuildMode.FindProperty("placementDistance").floatValue = 5f;
                serializedBuildMode.FindProperty("rotationSpeed").floatValue = 90f;
                serializedBuildMode.FindProperty("buildModeKey").intValue = (int)KeyCode.B;
                serializedBuildMode.FindProperty("rotateKey").intValue = (int)KeyCode.R;
                serializedBuildMode.FindProperty("cycleStructureKey").intValue = (int)KeyCode.Tab;
                
                serializedBuildMode.ApplyModifiedProperties();
                Debug.Log("✅ Structure prefabs assigned to SimpleBuildMode (Multi-structure support)");
            }
            else
            {
                Debug.LogWarning("⚠️ Wall prefab not found! Please create it first.");
            }
            
            // ═══════════════════════════════════════════════════════════
            // ABILITIES (Optional)
            // ═══════════════════════════════════════════════════════════
            
            AbilityController abilityController = player.AddComponent<AbilityController>();
            
            // ═══════════════════════════════════════════════════════════
            // CAMERA SETUP (FPS Style!)
            // ═══════════════════════════════════════════════════════════
            
            GameObject camera = new GameObject("PlayerCamera");
            camera.transform.SetParent(player.transform);
            
            // ⭐ FPS CAMERA POSITION (eye level, NOT behind!)
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            camera.transform.localRotation = Quaternion.identity;
            
            Camera cam = camera.AddComponent<Camera>();
            cam.enabled = false; // Will be enabled for local player only
            cam.fieldOfView = 60f;
            
            // URP Camera Data
            camera.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            
            // Audio Listener
            camera.AddComponent<AudioListener>();
            
            // Assign camera to FPSController
            SerializedObject serializedFPS = new SerializedObject(fpsController);
            serializedFPS.FindProperty("playerCamera").objectReferenceValue = cam;
            serializedFPS.FindProperty("walkSpeed").floatValue = 7f;
            serializedFPS.FindProperty("runSpeed").floatValue = 14f;
            serializedFPS.FindProperty("jumpPower").floatValue = 10f; // Düzeltilmiş
            serializedFPS.FindProperty("gravity").floatValue = 25f;   // Düzeltilmiş
            serializedFPS.FindProperty("lookSpeed").floatValue = 5f;
            serializedFPS.FindProperty("lookXLimit").floatValue = 60f;
            serializedFPS.FindProperty("useHeadBob").boolValue = true;
            serializedFPS.FindProperty("useFOVKick").boolValue = true;
            serializedFPS.ApplyModifiedProperties();
            
            // Prefab olarak kaydet
            string prefabDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
            
            // Scene'den temizle
            Object.DestroyImmediate(player);
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ PLAYER PREFAB CREATED SUCCESSFULLY!");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("📦 Components:");
            Debug.Log("  ✓ NetworkIdentity");
            Debug.Log("  ✓ NetworkTransformReliable");
            Debug.Log("  ✓ CharacterController");
            Debug.Log("  ✓ FPSController (ONLY controller - no conflicts!)");
            Debug.Log("  ✓ PlayerVisuals");
            Debug.Log("  ✓ Health");
            Debug.Log("  ✓ SimpleGun");
            Debug.Log("  ✓ SimpleBuildMode (Valheim style)");
            Debug.Log("  ✓ SimpleCrosshair");
            Debug.Log("  ✓ AbilityController");
            Debug.Log("  ✓ PlayerCamera (FPS position)");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("🎮 Controls:");
            Debug.Log("  • WASD = Move");
            Debug.Log("  • Mouse = Look");
            Debug.Log("  • Space = Jump");
            Debug.Log("  • Shift = Sprint");
            Debug.Log("  • B = Build Mode");
            Debug.Log("  • R = Rotate (in build mode)");
            Debug.Log("  • LMB = Shoot / Place");
            Debug.Log("  • ESC = Exit Build / Pause");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"📍 Location: {prefabPath}");
            Debug.Log("═══════════════════════════════════════════");
            
            // NetworkManager'a assign et
            var networkManager = Object.FindFirstObjectByType<Mirror.NetworkManager>();
            if (networkManager != null)
            {
                networkManager.playerPrefab = prefab;
                EditorUtility.SetDirty(networkManager);
                Debug.Log("✅ Player prefab assigned to NetworkManager");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
