using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Traps;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Tuzak prefab'larını otomatik oluşturur
    /// </summary>
    public class TrapPrefabCreator
    {
        [MenuItem("Tools/Tactical Combat/Create All Trap Prefabs")]
        public static void CreateAllTrapPrefabs()
        {
            CreateSpikeTrapPrefab();
            CreateGlueTrapPrefab();
            CreateSpringboardPrefab();
            CreateDartTurretPrefab();
            
            Debug.Log("✅ Tüm tuzak prefab'ları oluşturuldu!");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Spike Trap Prefab")]
        public static void CreateSpikeTrapPrefab()
        {
            CreateTrapPrefab("SpikeTrap", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/SpikeTrap.prefab", typeof(SpikeTrap));
        }
        
        [MenuItem("Tools/Tactical Combat/Create Glue Trap Prefab")]
        public static void CreateGlueTrapPrefab()
        {
            CreateTrapPrefab("GlueTrap", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/GlueTrap.prefab", typeof(GlueTrap));
        }
        
        [MenuItem("Tools/Tactical Combat/Create Springboard Prefab")]
        public static void CreateSpringboardPrefab()
        {
            CreateTrapPrefab("Springboard", new Vector3(1f, 0.1f, 1f), "Assets/Prefabs/Traps/Springboard.prefab", typeof(Springboard));
        }
        
        [MenuItem("Tools/Tactical Combat/Create Dart Turret Prefab")]
        public static void CreateDartTurretPrefab()
        {
            CreateTrapPrefab("DartTurret", new Vector3(1f, 1f, 1f), "Assets/Prefabs/Traps/DartTurret.prefab", typeof(DartTurret));
        }
        
        private static void CreateTrapPrefab(string name, Vector3 scale, string prefabPath, System.Type trapType)
        {
            // Prefabs/Traps klasörünü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Traps"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Traps");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // Tuzak GameObject'i oluştur
            GameObject trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trap.name = name;
            
            // Boyutlandır
            trap.transform.localScale = scale;
            trap.transform.position = Vector3.zero;
            
            // Collider'ı ayarla
            BoxCollider collider = trap.GetComponent<BoxCollider>();
            collider.isTrigger = true; // Tuzaklar trigger olmalı
            
            // Material ayarla
            Renderer renderer = trap.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Tuzak türüne göre renk
            switch (name)
            {
                case "SpikeTrap":
                    material.color = new Color(0.3f, 0.3f, 0.3f); // Koyu gri
                    break;
                case "GlueTrap":
                    material.color = new Color(0.8f, 0.6f, 0.2f); // Sarı
                    break;
                case "Springboard":
                    material.color = new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                    break;
                case "DartTurret":
                    material.color = new Color(0.4f, 0.4f, 0.4f); // Gri
                    break;
                default:
                    material.color = Color.white;
                    break;
            }
            
            renderer.material = material;
            
            // Trap component ekle
            trap.AddComponent(trapType);
            
            // Network Identity ekle (multiplayer için)
            trap.AddComponent<NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(trap, prefabPath);
            
            // Scene'den temizle
            Object.DestroyImmediate(trap);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu: {prefabPath}");
        }
    }
}
