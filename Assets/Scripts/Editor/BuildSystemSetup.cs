using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Build sistemi için otomatik prefab oluşturucu
    /// Tools > TacticalCombat > Setup Build System
    /// </summary>
    public class BuildSystemSetup : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Setup Build System")]
        public static void SetupBuildSystem()
        {
            // 1. Structures klasörü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Structures"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Structures");
            }

            // 2. Wall prefab oluştur
            CreateWallPrefab();

            // 3. Player prefab'a SimpleBuildMode ekle
            AddBuildModeToPlayer();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Build System Setup",
                "✅ Build sistemi kuruldu!\n\n" +
                "• Wall prefab oluşturuldu\n" +
                "• Player'a SimpleBuildMode eklendi\n\n" +
                "Play > HOST > B tuşuna bas!",
                "Tamam");

            Debug.Log("✅ Build system setup complete!");
        }

        private static void CreateWallPrefab()
        {
            // Wall GameObject oluştur
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            
            // Transform ayarla
            wall.transform.position = Vector3.zero;
            wall.transform.localScale = new Vector3(3f, 2f, 0.5f); // Duvar şekli

            // NetworkIdentity ekle
            wall.AddComponent<NetworkIdentity>();

            // Material oluştur (kahverengi duvar)
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                renderer.material = mat;
            }

            // Prefab olarak kaydet
            string prefabPath = "Assets/Prefabs/Structures/Wall.prefab";
            GameObject wallPrefab = PrefabUtility.SaveAsPrefabAsset(wall, prefabPath);

            // Scene'deki temp objeyi sil
            GameObject.DestroyImmediate(wall);

            Debug.Log($"✅ Wall prefab created: {prefabPath}");
        }

        private static void AddBuildModeToPlayer()
        {
            // Player prefab'ı yükle
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab bulunamadı!");
                return;
            }

            // Prefab'ı düzenleme modunda aç
            string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            // SimpleBuildMode zaten varsa ekleme
            var existingBuildMode = playerInstance.GetComponent<Building.SimpleBuildMode>();
            if (existingBuildMode != null)
            {
                Debug.Log("SimpleBuildMode zaten mevcut.");
            }
            else
            {
                var buildMode = playerInstance.AddComponent<Building.SimpleBuildMode>();
                Debug.Log("✅ SimpleBuildMode added to Player");
            }

            // Wall prefab'ı yükle ve ata
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Wall.prefab");
            
            var buildModeComponent = playerInstance.GetComponent<Building.SimpleBuildMode>();
            if (buildModeComponent != null && wallPrefab != null)
            {
                // SerializedObject kullanarak prefab field'ını ayarla
                SerializedObject so = new SerializedObject(buildModeComponent);
                SerializedProperty wallPrefabProp = so.FindProperty("wallPrefab");
                SerializedProperty groundLayerProp = so.FindProperty("groundLayer");
                
                if (wallPrefabProp != null)
                {
                    wallPrefabProp.objectReferenceValue = wallPrefab;
                }
                
                if (groundLayerProp != null)
                {
                    groundLayerProp.intValue = -1; // Everything layer
                }
                
                so.ApplyModifiedProperties();
                Debug.Log("✅ Wall prefab assigned to SimpleBuildMode");
            }

            // Değişiklikleri kaydet
            PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
            PrefabUtility.UnloadPrefabContents(playerInstance);

            Debug.Log("✅ Player prefab updated with SimpleBuildMode");
        }

        [MenuItem("Tools/TacticalCombat/Remove Build System")]
        public static void RemoveBuildSystem()
        {
            if (!EditorUtility.DisplayDialog("Remove Build System",
                "Build sistemini kaldırmak istediğine emin misin?",
                "Evet", "İptal"))
            {
                return;
            }

            // Player'dan SimpleBuildMode kaldır
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
                GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);

                var buildMode = playerInstance.GetComponent<Building.SimpleBuildMode>();
                if (buildMode != null)
                {
                    Object.DestroyImmediate(buildMode);
                    PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                    Debug.Log("SimpleBuildMode removed from Player");
                }

                PrefabUtility.UnloadPrefabContents(playerInstance);
            }

            EditorUtility.DisplayDialog("Build System Removed",
                "SimpleBuildMode Player'dan kaldırıldı.",
                "Tamam");
        }
    }
}



