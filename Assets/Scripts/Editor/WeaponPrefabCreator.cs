using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Silah prefab'larını otomatik oluşturur
    /// </summary>
    public class WeaponPrefabCreator
    {
        [MenuItem("Tools/Tactical Combat/Create All Weapon Prefabs")]
        public static void CreateAllWeaponPrefabs()
        {
            CreateBowPrefab();
            CreateSpearPrefab();
            
            Debug.Log("✅ Tüm silah prefab'ları oluşturuldu!");
        }
        
        [MenuItem("Tools/Tactical Combat/Create Bow Prefab")]
        public static void CreateBowPrefab()
        {
            CreateWeaponPrefab("Bow", new Vector3(0.1f, 0.1f, 1f), "Assets/Prefabs/Weapons/Bow.prefab", typeof(WeaponBow));
        }
        
        [MenuItem("Tools/Tactical Combat/Create Spear Prefab")]
        public static void CreateSpearPrefab()
        {
            CreateWeaponPrefab("Spear", new Vector3(0.1f, 0.1f, 2f), "Assets/Prefabs/Weapons/Spear.prefab", typeof(WeaponSpear));
        }
        
        private static void CreateWeaponPrefab(string name, Vector3 scale, string prefabPath, System.Type weaponType)
        {
            // Prefabs/Weapons klasörünü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
            }
            
            // Mevcut prefab'ı sil
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // Silah GameObject'i oluştur
            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            weapon.name = name;
            
            // Boyutlandır
            weapon.transform.localScale = scale;
            weapon.transform.position = Vector3.zero;
            
            // Collider'ı ayarla
            CapsuleCollider collider = weapon.GetComponent<CapsuleCollider>();
            collider.isTrigger = true; // Silahlar trigger olmalı
            
            // Material ayarla
            Renderer renderer = weapon.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Silah türüne göre renk
            switch (name)
            {
                case "Bow":
                    material.color = new Color(0.6f, 0.4f, 0.2f); // Kahverengi
                    break;
                case "Spear":
                    material.color = new Color(0.8f, 0.8f, 0.8f); // Gümüş
                    break;
                default:
                    material.color = Color.white;
                    break;
            }
            
            renderer.material = material;
            
            // Weapon component ekle
            weapon.AddComponent(weaponType);
            
            // Network Identity ekle (multiplayer için)
            weapon.AddComponent<NetworkIdentity>();
            
            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(weapon, prefabPath);
            
            // Scene'den temizle
            Object.DestroyImmediate(weapon);
            
            Debug.Log($"✅ {name} prefab'ı oluşturuldu: {prefabPath}");
        }
    }
}
