using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Tüm prefab'ları tek seferde oluşturan master creator
    /// </summary>
    public class MasterPrefabCreator
    {
        [MenuItem("Tools/Tactical Combat/Create ALL Prefabs (Master)", false, 0)]
        public static void CreateAllPrefabs()
        {
            Debug.Log("🚀 TÜM PREFAB'LAR OLUŞTURULUYOR...");
            
            // 1. Yapı prefab'ları
            Debug.Log("📦 Yapı prefab'ları oluşturuluyor...");
            StructurePrefabCreator.CreateAllStructurePrefabs();
            
            // 2. Tuzak prefab'ları
            Debug.Log("🪤 Tuzak prefab'ları oluşturuluyor...");
            TrapPrefabCreator.CreateAllTrapPrefabs();
            
            // 3. Silah prefab'ları
            Debug.Log("⚔️ Silah prefab'ları oluşturuluyor...");
            WeaponPrefabCreator.CreateAllWeaponPrefabs();
            
            // 4. Player prefab'ını yeniden oluştur
            Debug.Log("👤 Player prefab'ı güncelleniyor...");
            PlayerPrefabRecreator.RecreatePlayerPrefab();
            
            // 5. Asset database'i yenile
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ TÜM PREFAB'LAR BAŞARIYLA OLUŞTURULDU!");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("📦 Oluşturulan Prefab'lar:");
            Debug.Log("  • Wall, Floor, Roof, Door, Window, Stairs");
            Debug.Log("  • SpikeTrap, GlueTrap, Springboard, DartTurret");
            Debug.Log("  • Bow, Spear");
            Debug.Log("  • Player (güncellenmiş)");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("🎮 Artık oyunu test edebilirsin!");
            Debug.Log("═══════════════════════════════════════════");
            
            // Success dialog
            EditorUtility.DisplayDialog("Prefab'lar Oluşturuldu!",
                "Tüm prefab'lar başarıyla oluşturuldu!\n\n" +
                "✅ Yapı prefab'ları (6 adet)\n" +
                "✅ Tuzak prefab'ları (4 adet)\n" +
                "✅ Silah prefab'ları (2 adet)\n" +
                "✅ Player prefab'ı (güncellenmiş)\n\n" +
                "Artık oyunu test edebilirsin!",
                "Tamam");
        }
        
        [MenuItem("Tools/Tactical Combat/Quick Setup (Everything)", false, 1)]
        public static void QuickSetupEverything()
        {
            Debug.Log("🚀 HIZLI KURULUM BAŞLIYOR...");
            
            // 1. Tüm prefab'ları oluştur
            CreateAllPrefabs();
            
            // 2. Scene setup helper'ı aç
            Debug.Log("🎬 Scene setup helper açılıyor...");
            SceneSetupHelper.ShowWindow();
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ HIZLI KURULUM TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("📋 Sonraki adımlar:");
            Debug.Log("  1. Scene Setup Helper'da 'Setup Scene' butonuna bas");
            Debug.Log("  2. Play mode'a gir ve test et");
            Debug.Log("  3. Build & Run yap ve multiplayer test et");
            Debug.Log("═══════════════════════════════════════════");
        }
    }
}
