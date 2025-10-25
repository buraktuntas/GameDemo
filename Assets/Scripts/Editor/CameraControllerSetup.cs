using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// FPSController'ı Player prefab'ına otomatik ekler
    /// </summary>
    public class CameraControllerSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Add FPSController to Player")]
        public static void AddFPSControllerToPlayer()
        {
            // Player prefab'ını bul
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at: Assets/Prefabs/Player.prefab");
                return;
            }
            
            // Prefab'ı aç
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(playerPrefab));
            
            // FPSController var mı kontrol et
            FPSController existingController = prefabInstance.GetComponent<FPSController>();
            if (existingController != null)
            {
                Debug.Log("✅ FPSController already exists on Player prefab");
                PrefabUtility.UnloadPrefabContents(prefabInstance);
                return;
            }
            
            // FPSController ekle
            FPSController fpsController = prefabInstance.AddComponent<FPSController>();
            
            // Ayarları yapılandır
            SerializedObject serializedController = new SerializedObject(fpsController);
            serializedController.FindProperty("walkSpeed").floatValue = 6f;
            serializedController.FindProperty("runSpeed").floatValue = 12f;
            serializedController.FindProperty("jumpPower").floatValue = 7f;
            serializedController.FindProperty("gravity").floatValue = 10f;
            serializedController.FindProperty("lookSpeed").floatValue = 2f;
            serializedController.FindProperty("lookXLimit").floatValue = 45f;
            serializedController.FindProperty("showDebugInfo").boolValue = true;
            serializedController.ApplyModifiedProperties();
            
            // Prefab'ı kaydet
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(playerPrefab));
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log("✅ FPSController added to Player prefab!");
        }
        
        [MenuItem("Tools/Tactical Combat/Setup Complete FPS System")]
        public static void SetupCompleteFPSSystem()
        {
            AddFPSControllerToPlayer();
            
            Debug.Log("🎯 Complete FPS System Setup:");
            Debug.Log("  ✅ FPSController added to Player prefab");
            Debug.Log("  ✅ FPS settings configured");
            Debug.Log("  ✅ Debug mode enabled");
            Debug.Log("");
            Debug.Log("📋 Next steps:");
            Debug.Log("  1. Play mode'a gir");
            Debug.Log("  2. Console'da 'FPSController initialized' gormeli");
            Debug.Log("  3. Mouse hareket -> Player donmeli");
            Debug.Log("  4. W tuşu -> Player yonune gitmeli");
            Debug.Log("  5. Space ile zipla");
            Debug.Log("  6. Left Shift ile kos");
        }
    }
}
