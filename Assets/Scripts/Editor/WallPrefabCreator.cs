using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Building;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class WallPrefabCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create Wall Prefab")]
        public static void CreateWallPrefab()
        {
            // Prefabs klasörünü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                Debug.Log("✅ Prefabs klasörü oluşturuldu");
            }
            
            // Wall GameObject oluştur
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            
            // Boyutlandır (2m genişlik, 1m yükseklik, 0.2m kalınlık)
            wall.transform.localScale = new Vector3(2f, 1f, 0.2f);
            wall.transform.position = Vector3.zero;
            
            // Collider'ı ayarla
            BoxCollider collider = wall.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = Vector3.one; // Scale zaten uygulanmış
            }
            
            // Mirror NetworkIdentity ekle
            wall.AddComponent<NetworkIdentity>();
            
            // Structure component
            Structure structure = wall.AddComponent<Structure>();
            
            // Health component
            Health health = wall.AddComponent<Health>();
            
            // StructuralIntegrity component
            StructuralIntegrity integrity = wall.AddComponent<StructuralIntegrity>();
            
            // Layer'ı ayarla (Default veya Structure)
            wall.layer = LayerMask.NameToLayer("Default");
            
            // Material - basit bir renk
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.8f, 0.8f, 0.8f); // Açık gri
                renderer.material = mat;
            }
            
            // Prefab olarak kaydet
            string prefabPath = "Assets/Prefabs/Wall.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(wall, prefabPath);
            
            // Scene'deki geçici objeyi sil
            DestroyImmediate(wall);
            
            // Prefab'ı seç
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Wall prefab oluşturuldu: {prefabPath}");
            
            EditorUtility.DisplayDialog(
                "Wall Prefab Oluşturuldu!",
                "✅ Wall prefab başarıyla oluşturuldu!\n\n" +
                "Konum: Assets/Prefabs/Wall.prefab\n\n" +
                "Özellikler:\n" +
                "• Boyut: 2m x 1m x 0.2m\n" +
                "• NetworkIdentity ✓\n" +
                "• Structure ✓\n" +
                "• Health ✓\n" +
                "• StructuralIntegrity ✓\n\n" +
                "Artık SimpleBuildMode'da kullanabilirsin!",
                "Tamam"
            );
        }
        
        [MenuItem("Tools/Tactical Combat/Setup Complete Build System")]
        public static void SetupCompleteBuildSystem()
        {
            // 1. Wall prefab oluştur
            CreateWallPrefab();
            
            // 2. Player prefab'ı bul ve SimpleBuildMode ekle
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab != null)
            {
                GameObject playerInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
                
                if (playerInstance.GetComponent<SimpleBuildMode>() == null)
                {
                    SimpleBuildMode buildMode = playerInstance.AddComponent<SimpleBuildMode>();
                    
                    // Wall prefab'ını ata
                    GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wall.prefab");
                    if (wallPrefab != null)
                    {
                        SerializedObject so = new SerializedObject(buildMode);
                        SerializedProperty wallProp = so.FindProperty("wallPrefab");
                        wallProp.objectReferenceValue = wallPrefab;
                        so.ApplyModifiedProperties();
                    }
                    
                    PrefabUtility.SaveAsPrefabAsset(playerInstance, "Assets/Prefabs/Player.prefab");
                    Debug.Log("✅ SimpleBuildMode Player prefab'ına eklendi!");
                }
                
                PrefabUtility.UnloadPrefabContents(playerInstance);
            }
            
            EditorUtility.DisplayDialog(
                "Build System Kurulumu Tamamlandı!",
                "✅ Build sistemi tamamen kuruldu!\n\n" +
                "Yapılanlar:\n" +
                "• Wall prefab oluşturuldu\n" +
                "• SimpleBuildMode Player'a eklendi\n" +
                "• Wall prefab SimpleBuildMode'a atandı\n\n" +
                "Şimdi:\n" +
                "1. Tools > Create InputManager\n" +
                "2. Play mode'a gir\n" +
                "3. B tuşuna bas ve test et!",
                "Harika!"
            );
        }
    }
}


