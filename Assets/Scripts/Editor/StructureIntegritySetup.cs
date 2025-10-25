using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Building;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class StructureIntegritySetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Add Structural Integrity to Wall")]
        public static void AddStructuralIntegrityToWall()
        {
            string wallPath = "Assets/Prefabs/Wall.prefab";
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wallPath);
            
            if (wallPrefab == null)
            {
                Debug.LogError("Wall prefab not found at: " + wallPath);
                return;
            }
            
            // Prefab'i düzenleme modunda aç
            GameObject wallInstance = PrefabUtility.LoadPrefabContents(wallPath);
            
            bool modified = false;
            
            // NetworkIdentity kontrolü
            if (wallInstance.GetComponent<NetworkIdentity>() == null)
            {
                wallInstance.AddComponent<NetworkIdentity>();
                Debug.Log("✅ NetworkIdentity eklendi");
                modified = true;
            }
            
            // Structure kontrolü
            if (wallInstance.GetComponent<Structure>() == null)
            {
                wallInstance.AddComponent<Structure>();
                Debug.Log("✅ Structure eklendi");
                modified = true;
            }
            
            // Health kontrolü
            if (wallInstance.GetComponent<Health>() == null)
            {
                wallInstance.AddComponent<Health>();
                Debug.Log("✅ Health eklendi");
                modified = true;
            }
            
            // StructuralIntegrity kontrolü
            if (wallInstance.GetComponent<StructuralIntegrity>() == null)
            {
                wallInstance.AddComponent<StructuralIntegrity>();
                Debug.Log("✅ StructuralIntegrity eklendi");
                modified = true;
            }
            
            // BoxCollider kontrolü
            if (wallInstance.GetComponent<BoxCollider>() == null)
            {
                BoxCollider collider = wallInstance.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 1f, 0.2f); // Wall boyutu
                Debug.Log("✅ BoxCollider eklendi");
                modified = true;
            }
            
            if (modified)
            {
                // Değişiklikleri kaydet
                PrefabUtility.SaveAsPrefabAsset(wallInstance, wallPath);
                Debug.Log($"✅ Wall prefab güncellendi: {wallPath}");
            }
            else
            {
                Debug.Log("ℹ️ Wall prefab zaten güncel!");
            }
            
            // Temizlik
            PrefabUtility.UnloadPrefabContents(wallInstance);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Tactical Combat/Test Structural Integrity")]
        public static void TestStructuralIntegrity()
        {
            EditorUtility.DisplayDialog(
                "Structural Integrity Test",
                "Oyunu çalıştır ve:\n\n" +
                "1. B tuşuna bas (Build mode)\n" +
                "2. Zemine duvar koy → MAVİ renk (çok sağlam)\n" +
                "3. Duvarın üzerine duvar koy → YEŞİL renk (sağlam)\n" +
                "4. Çok yukarı çık → SARI→TURUNCU→KIRMIZI\n" +
                "5. Kırmızı olunca yapı yıkılır!\n\n" +
                "Renk kodu:\n" +
                "🔵 Mavi = Zemine bağlı (100%)\n" +
                "🟢 Yeşil = Sağlam (80-60%)\n" +
                "🟡 Sarı = Orta (60-40%)\n" +
                "🟠 Turuncu = Zayıf (40-20%)\n" +
                "🔴 Kırmızı = Yıkılmak üzere (<20%)",
                "Anladım"
            );
        }
    }
}


