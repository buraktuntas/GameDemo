using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Hızlı prefab oluşturucu - menü sorunları için
    /// </summary>
    public class QuickPrefabCreator
    {
        [MenuItem("Tools/Create All Prefabs")]
        public static void CreateAllPrefabs()
        {
            Debug.Log("🚀 TÜM PREFAB'LAR OLUŞTURULUYOR...");
            
            try
            {
                // 1. Yapı prefab'ları
                Debug.Log("📦 Yapı prefab'ları oluşturuluyor...");
                CreateStructurePrefab("Wall", new Vector3(0.1f, 2f, 2f), "Assets/Prefabs/Structures/Wall.prefab");
                CreateStructurePrefab("Floor", new Vector3(2f, 0.1f, 2f), "Assets/Prefabs/Structures/Floor.prefab");
                CreateStructurePrefab("Roof", new Vector3(2f, 0.1f, 2f), "Assets/Prefabs/Structures/Roof.prefab");
                CreateStructurePrefab("Door", new Vector3(1f, 2f, 0.1f), "Assets/Prefabs/Structures/Door.prefab");
                CreateStructurePrefab("Window", new Vector3(1f, 1f, 0.1f), "Assets/Prefabs/Structures/Window.prefab");
                CreateStructurePrefab("Stairs", new Vector3(1f, 1f, 2f), "Assets/Prefabs/Structures/Stairs.prefab");
                
                // 2. Tuzak prefab'ları
                Debug.Log("🪤 Tuzak prefab'ları oluşturuluyor...");
                CreateTrapPrefab("SpikeTrap", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/SpikeTrap.prefab");
                CreateTrapPrefab("GlueTrap", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/GlueTrap.prefab");
                CreateTrapPrefab("Springboard", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/Springboard.prefab");
                CreateTrapPrefab("DartTurret", new Vector3(1f, 1f, 1f), "Assets/Prefabs/Traps/DartTurret.prefab");
                
                // 3. Silah prefab'ları
                Debug.Log("⚔️ Silah prefab'ları oluşturuluyor...");
                CreateWeaponPrefab("Bow", new Vector3(0.1f, 0.1f, 1f), "Assets/Prefabs/Weapons/Bow.prefab");
                CreateWeaponPrefab("Spear", new Vector3(0.1f, 0.1f, 2f), "Assets/Prefabs/Weapons/Spear.prefab");
                
                // 4. Asset database'i yenile
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("═══════════════════════════════════════════");
                Debug.Log("✅ TÜM PREFAB'LAR BAŞARIYLA OLUŞTURULDU!");
                Debug.Log("═══════════════════════════════════════════");
                
                // Player Prefab'ı güncelle
                Debug.Log("👤 Player Prefab güncelleniyor...");
                UpdatePlayerPrefab();
                
                EditorUtility.DisplayDialog("Başarılı!",
                    "Tüm prefab'lar oluşturuldu!\n\n" +
                    "✅ Yapı prefab'ları (6 adet)\n" +
                    "✅ Tuzak prefab'ları (4 adet)\n" +
                    "✅ Silah prefab'ları (2 adet)\n" +
                    "✅ Player Prefab güncellendi\n\n" +
                    "Artık oyunu test edebilirsin!",
                    "Tamam");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Hata: {e.Message}");
                EditorUtility.DisplayDialog("Hata!",
                    $"Prefab oluşturulurken hata oluştu:\n\n{e.Message}",
                    "Tamam");
            }
        }
        
        private static void CreateStructurePrefab(string name, Vector3 scale, string prefabPath)
        {
            // Klasör oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Structures"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Structures");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // GameObject oluştur
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.localScale = scale;
            
            // Material ayarla
            Renderer renderer = obj.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = GetStructureColor(name);
            renderer.material = material;
            
            // NetworkIdentity ekle (multiplayer için)
            obj.AddComponent<Mirror.NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu");
        }
        
        private static void CreateTrapPrefab(string name, Vector3 scale, string prefabPath)
        {
            // Klasör oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Traps"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Traps");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // GameObject oluştur
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.localScale = scale;
            
            // Collider'ı trigger yap
            obj.GetComponent<Collider>().isTrigger = true;
            
            // Material ayarla
            Renderer renderer = obj.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = GetTrapColor(name);
            renderer.material = material;
            
            // NetworkIdentity ekle (multiplayer için)
            obj.AddComponent<Mirror.NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu");
        }
        
        private static void CreateWeaponPrefab(string name, Vector3 scale, string prefabPath)
        {
            // Klasör oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // GameObject oluştur
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.localScale = scale;
            
            // Collider'ı trigger yap
            obj.GetComponent<Collider>().isTrigger = true;
            
            // Material ayarla
            Renderer renderer = obj.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = GetWeaponColor(name);
            renderer.material = material;
            
            // NetworkIdentity ekle (multiplayer için)
            obj.AddComponent<Mirror.NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu");
        }
        
        private static Color GetStructureColor(string name)
        {
            switch (name)
            {
                case "Floor": return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                case "Roof": return new Color(0.3f, 0.3f, 0.3f); // Gri
                case "Door": return new Color(0.4f, 0.2f, 0.1f); // Koyu kahverengi
                case "Window": return new Color(0.8f, 0.9f, 1f); // Açık mavi
                case "Stairs": return new Color(0.7f, 0.5f, 0.3f); // Açık kahverengi
                default: return Color.white;
            }
        }
        
        private static Color GetTrapColor(string name)
        {
            switch (name)
            {
                case "SpikeTrap": return new Color(0.3f, 0.3f, 0.3f); // Koyu gri
                case "GlueTrap": return new Color(0.8f, 0.6f, 0.2f); // Sarı
                case "Springboard": return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                case "DartTurret": return new Color(0.4f, 0.4f, 0.4f); // Gri
                default: return Color.white;
            }
        }
        
        private static Color GetWeaponColor(string name)
        {
            switch (name)
            {
                case "Bow": return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                case "Spear": return new Color(0.8f, 0.8f, 0.8f); // Gümüş
                default: return Color.white;
            }
        }
        
        private static void UpdatePlayerPrefab()
        {
            try
            {
                // Player prefab'ı bul
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
                if (playerPrefab == null)
                {
                    Debug.LogWarning("⚠️ Player.prefab bulunamadı!");
                    return;
                }
                
                // SimpleBuildMode component'ini bul
                var buildMode = playerPrefab.GetComponent<TacticalCombat.Building.SimpleBuildMode>();
                if (buildMode == null)
                {
                    Debug.LogWarning("⚠️ SimpleBuildMode component bulunamadı!");
                    return;
                }
                
                // Prefab'ları ata
                buildMode.wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Wall.prefab");
                buildMode.floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Floor.prefab");
                buildMode.roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Roof.prefab");
                buildMode.doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Door.prefab");
                buildMode.windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Window.prefab");
                buildMode.stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Stairs.prefab");
                
                // Ground layer'ı ayarla
                buildMode.groundLayer = LayerMask.GetMask("Ground", "Terrain");
                
                // Prefab'ı kaydet
                EditorUtility.SetDirty(playerPrefab);
                AssetDatabase.SaveAssets();
                
                Debug.Log("✅ Player Prefab güncellendi - Tüm yapı prefab'ları atandı!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Player Prefab güncellenirken hata: {e.Message}");
            }
        }
    }
}
