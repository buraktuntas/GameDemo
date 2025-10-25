using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    public class WeaponSystemDebugger
    {
        [MenuItem("Tools/Tactical Combat/Debug WeaponSystem")]
        public static void DebugWeaponSystem()
        {
            Debug.Log("🔍 WEAPONSYSTEM DEBUG BAŞLIYOR...");
            
            // 1. Player prefab kontrolü
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at Assets/Prefabs/Player.prefab");
                return;
            }
            Debug.Log("✅ Player prefab found");
            
            // 2. Player prefab içeriğini yükle
            GameObject playerInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // 3. WeaponSystem kontrolü
            var weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError("❌ WeaponSystem component not found on Player prefab!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }
            Debug.Log("✅ WeaponSystem component found");
            
            // 4. SerializedObject ile detaylı kontrol
            SerializedObject so = new SerializedObject(weaponSystem);
            
            // Camera kontrolü
            var cameraProp = so.FindProperty("playerCamera");
            if (cameraProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ playerCamera is null!");
            }
            else
            {
                Debug.Log($"✅ playerCamera: {cameraProp.objectReferenceValue.name}");
            }
            
            // WeaponHolder kontrolü
            var weaponHolderProp = so.FindProperty("weaponHolder");
            if (weaponHolderProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ weaponHolder is null!");
            }
            else
            {
                Debug.Log($"✅ weaponHolder: {weaponHolderProp.objectReferenceValue.name}");
            }
            
            // AudioSource kontrolü
            var audioSourceProp = so.FindProperty("audioSource");
            if (audioSourceProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ audioSource is null!");
            }
            else
            {
                Debug.Log($"✅ audioSource: {audioSourceProp.objectReferenceValue.name}");
            }
            
            // CurrentWeapon kontrolü
            var currentWeaponProp = so.FindProperty("currentWeapon");
            if (currentWeaponProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ currentWeapon is null!");
            }
            else
            {
                var weaponConfig = currentWeaponProp.objectReferenceValue as WeaponConfig;
                Debug.Log($"✅ currentWeapon: {weaponConfig.weaponName} (Damage: {weaponConfig.damage})");
            }
            
            // Effect Prefabs kontrolü
            var muzzleFlashProp = so.FindProperty("muzzleFlashPrefab");
            if (muzzleFlashProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ muzzleFlashPrefab is null!");
            }
            else
            {
                Debug.Log($"✅ muzzleFlashPrefab: {muzzleFlashProp.objectReferenceValue.name}");
            }
            
            var bulletHoleProp = so.FindProperty("bulletHolePrefab");
            if (bulletHoleProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ bulletHolePrefab is null!");
            }
            else
            {
                Debug.Log($"✅ bulletHolePrefab: {bulletHoleProp.objectReferenceValue.name}");
            }
            
            var bloodEffectProp = so.FindProperty("bloodEffectPrefab");
            if (bloodEffectProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ bloodEffectPrefab is null!");
            }
            else
            {
                Debug.Log($"✅ bloodEffectPrefab: {bloodEffectProp.objectReferenceValue.name}");
            }
            
            var metalSparksProp = so.FindProperty("metalSparksPrefab");
            if (metalSparksProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ metalSparksPrefab is null!");
            }
            else
            {
                Debug.Log($"✅ metalSparksPrefab: {metalSparksProp.objectReferenceValue.name}");
            }
            
            // 5. Scene'de CombatManager kontrolü
            var combatManager = Object.FindFirstObjectByType<CombatManager>();
            if (combatManager == null)
            {
                Debug.LogWarning("⚠️ CombatManager not found in scene!");
            }
            else
            {
                Debug.Log("✅ CombatManager found in scene");
            }
            
            // 6. Scene'de CombatUI kontrolü
            var combatUI = Object.FindFirstObjectByType<CombatUI>();
            if (combatUI == null)
            {
                Debug.LogWarning("⚠️ CombatUI not found in scene!");
            }
            else
            {
                Debug.Log("✅ CombatUI found in scene");
            }
            
            PrefabUtility.UnloadPrefabContents(playerInstance);
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("🔍 WEAPONSYSTEM DEBUG TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════════");
        }
        
        [MenuItem("Tools/Tactical Combat/Quick Fix All")]
        public static void QuickFixAll()
        {
            Debug.Log("🔧 QUICK FIX ALL BAŞLIYOR...");
            
            // 1. Combat System Setup
            CombatSystemSetup.SetupCombatSystem();
            
            // 2. Player Prefab Recreate
            PlayerPrefabRecreator.RecreatePlayerPrefab();
            
            // 3. WeaponSystem Fix
            WeaponSystemFixer.FixWeaponSystemReferences();
            
            // 4. Hitbox Setup
            HitboxSetup.AddHitboxesToPlayer();
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ QUICK FIX ALL TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════════");
        }
    }
}
