using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Building;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Çeşitli yapı prefab'larını otomatik oluşturur
    /// </summary>
    public class StructurePrefabCreator
    {
        [MenuItem("Tools/Tactical Combat/Create All Structure Prefabs")]
        public static void CreateAllStructurePrefabs()
        {
            CreateFloorPrefab();
            CreateRoofPrefab();
            CreateDoorPrefab();
            CreateWindowPrefab();
            CreateStairsPrefab();
            
            Debug.Log("✅ Tüm yapı prefab'ları oluşturuldu!");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Floor Prefab")]
        public static void CreateFloorPrefab()
        {
            CreateStructurePrefab("Floor", new Vector3(2f, 0.1f, 2f), "Assets/Prefabs/Structures/Floor.prefab");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Roof Prefab")]
        public static void CreateRoofPrefab()
        {
            CreateStructurePrefab("Roof", new Vector3(2f, 0.1f, 2f), "Assets/Prefabs/Structures/Roof.prefab");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Door Prefab")]
        public static void CreateDoorPrefab()
        {
            CreateStructurePrefab("Door", new Vector3(1f, 2f, 0.1f), "Assets/Prefabs/Structures/Door.prefab");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Window Prefab")]
        public static void CreateWindowPrefab()
        {
            CreateStructurePrefab("Window", new Vector3(1f, 1f, 0.1f), "Assets/Prefabs/Structures/Window.prefab");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Stairs Prefab")]
        public static void CreateStairsPrefab()
        {
            CreateStructurePrefab("Stairs", new Vector3(1f, 1f, 2f), "Assets/Prefabs/Structures/Stairs.prefab");
        }
        
        private static void CreateStructurePrefab(string name, Vector3 scale, string prefabPath)
        {
            // Prefabs/Structures klasörünü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Structures"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Structures");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // Yapı GameObject'i oluştur
            GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            structure.name = name;
            
            // Boyutlandır
            structure.transform.localScale = scale;
            structure.transform.position = Vector3.zero;
            
            // Collider'ı ayarla
            BoxCollider collider = structure.GetComponent<BoxCollider>();
            collider.isTrigger = false;
            
            // Material ayarla
            Renderer renderer = structure.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Yapı türüne göre renk
            switch (name)
            {
                case "Floor":
                    material.color = new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                    break;
                case "Roof":
                    material.color = new Color(0.3f, 0.3f, 0.3f); // Gri
                    break;
                case "Door":
                    material.color = new Color(0.4f, 0.2f, 0.1f); // Koyu kahverengi
                    break;
                case "Window":
                    material.color = new Color(0.8f, 0.9f, 1f); // Açık mavi
                    break;
                case "Stairs":
                    material.color = new Color(0.7f, 0.5f, 0.3f); // Açık kahverengi
                    break;
                default:
                    material.color = Color.white;
                    break;
            }
            
            renderer.material = material;
            
            // Structural Integrity ekle
            structure.AddComponent<StructuralIntegrity>();
            
            // Network Identity ekle (multiplayer için)
            structure.AddComponent<NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(structure, prefabPath);
            
            // Scene'den temizle
            Object.DestroyImmediate(structure);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu: {prefabPath}");
        }
    }
}
