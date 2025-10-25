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
            
            // ⭐ PROFESSIONAL COMBAT SYSTEM
            TacticalCombat.Combat.WeaponSystem weaponSystem = player.AddComponent<TacticalCombat.Combat.WeaponSystem>();
            
            // Legacy SimpleGun (backup)
            TacticalCombat.Combat.SimpleGun simpleGun = player.AddComponent<TacticalCombat.Combat.SimpleGun>();
            
            // Configure gun
            SerializedObject serializedGun = new SerializedObject(simpleGun);
            var damageProp = serializedGun.FindProperty("damage");
            var rangeProp = serializedGun.FindProperty("range");
            var fireRateProp = serializedGun.FindProperty("fireRate");
            
            if (damageProp != null && damageProp.propertyType == SerializedPropertyType.Float) 
                damageProp.floatValue = 25f;
            if (rangeProp != null && rangeProp.propertyType == SerializedPropertyType.Float) 
                rangeProp.floatValue = 100f;
            if (fireRateProp != null && fireRateProp.propertyType == SerializedPropertyType.Float) 
                fireRateProp.floatValue = 0.5f;
            
            // ⭐ Create Weapon Holder (Professional Combat System)
            GameObject weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(player.transform);
            weaponHolder.transform.localPosition = new Vector3(0.3f, 1.4f, 0.5f); // Sağ el pozisyonu
            weaponHolder.transform.localRotation = Quaternion.identity;
            
            // ⭐ Create Muzzle Transform (Legacy)
            GameObject muzzleTransform = new GameObject("MuzzleTransform");
            muzzleTransform.transform.SetParent(player.transform);
            muzzleTransform.transform.localPosition = new Vector3(0.3f, 1.4f, 0.5f); // Sağ el pozisyonu
            muzzleTransform.transform.localRotation = Quaternion.identity;
            
            // ⭐ Assign effect prefabs, muzzle transform, and sounds
            var hitEffectProp = serializedGun.FindProperty("hitEffectPrefab");
            var muzzleFlashProp = serializedGun.FindProperty("muzzleFlashPrefab");
            var muzzleTransformProp = serializedGun.FindProperty("muzzleTransform");
            var fireSoundProp = serializedGun.FindProperty("fireSound");
            var hitSoundProp = serializedGun.FindProperty("hitSound");
            
            GameObject hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/HitEffect.prefab");
            GameObject muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MuzzleFlash.prefab");
            
            // Create simple audio clips (if they don't exist)
            AudioClip fireSound = CreateSimpleAudioClip("FireSound", 0.1f, 440f); // 440Hz, 0.1s
            AudioClip hitSound = CreateSimpleAudioClip("HitSound", 0.2f, 220f); // 220Hz, 0.2s
            
            if (hitEffectProp != null && hitEffectPrefab != null) 
                hitEffectProp.objectReferenceValue = hitEffectPrefab;
            if (muzzleFlashProp != null && muzzleFlashPrefab != null) 
                muzzleFlashProp.objectReferenceValue = muzzleFlashPrefab;
            if (muzzleTransformProp != null) 
                muzzleTransformProp.objectReferenceValue = muzzleTransform.transform;
            if (fireSoundProp != null && fireSound != null) 
                fireSoundProp.objectReferenceValue = fireSound;
            if (hitSoundProp != null && hitSound != null) 
                hitSoundProp.objectReferenceValue = hitSound;
            
            serializedGun.ApplyModifiedProperties();
            Debug.Log("✅ Muzzle transform created and assigned to SimpleGun");
            
            // ⭐ Configure WeaponSystem (Professional Combat System)
            SerializedObject serializedWeaponSystem = new SerializedObject(weaponSystem);
            var weaponHolderProp = serializedWeaponSystem.FindProperty("weaponHolder");
            if (weaponHolderProp != null)
            {
                weaponHolderProp.objectReferenceValue = weaponHolder.transform;
                serializedWeaponSystem.ApplyModifiedProperties();
                Debug.Log("✅ WeaponHolder assigned to WeaponSystem");
            }
            
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
            
            // ⭐ Assign camera to SimpleGun (for muzzle flash positioning)
            // Re-create serializedGun to assign camera
            SerializedObject serializedGunForCamera = new SerializedObject(simpleGun);
            var playerCameraProp = serializedGunForCamera.FindProperty("playerCamera");
            if (playerCameraProp != null && playerCameraProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                playerCameraProp.objectReferenceValue = cam;
                serializedGunForCamera.ApplyModifiedProperties();
                Debug.Log("✅ Camera assigned to SimpleGun for proper muzzle flash positioning");
            }
            else
            {
                Debug.LogWarning("⚠️ playerCamera property not found in SimpleGun! Camera assignment skipped.");
            }
            
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

        private static AudioClip CreateSimpleAudioClip(string name, float duration, float frequency)
        {
            // Create a simple sine wave audio clip
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * 0.1f; // Low volume
            }
            
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            
            return clip;
        }
    }
}
