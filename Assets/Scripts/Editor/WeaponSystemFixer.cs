using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class WeaponSystemFixer
    {
        [MenuItem("Tools/Tactical Combat/Fix WeaponSystem References")]
        public static void FixWeaponSystemReferences()
        {
            // Player prefab'ı yükle
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not found!");
                return;
            }
            
            GameObject playerInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // WeaponSystem'i bul
            var weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError("WeaponSystem not found!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }
            
            SerializedObject so = new SerializedObject(weaponSystem);
            
            // 1. Camera referansı
            Transform cameraTransform = playerInstance.transform.Find("PlayerCamera");
            if (cameraTransform != null)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                so.FindProperty("playerCamera").objectReferenceValue = cam;
                Debug.Log("✅ Camera assigned");
            }
            
            // 2. Weapon Holder referansı
            Transform weaponHolder = playerInstance.transform.Find("WeaponHolder");
            if (weaponHolder != null)
            {
                so.FindProperty("weaponHolder").objectReferenceValue = weaponHolder;
                Debug.Log("✅ WeaponHolder assigned");
            }
            
            // 3. AudioSource ekle
            AudioSource audioSource = playerInstance.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerInstance.AddComponent<AudioSource>();
                Debug.Log("✅ AudioSource added");
            }
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            
            // 4. Weapon Config ata
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                "Assets/Configs/Weapons/AssaultRifle.asset");
            if (config != null)
            {
                so.FindProperty("currentWeapon").objectReferenceValue = config;
                Debug.Log($"✅ Weapon config assigned: {config.weaponName} (Damage: {config.damage})");
            }
            else
            {
                Debug.LogError("❌ Weapon config not found! Creating default config...");
                CreateDefaultWeaponConfig();
                
                // Try again
                config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                    "Assets/Configs/Weapons/AssaultRifle.asset");
                if (config != null)
                {
                    so.FindProperty("currentWeapon").objectReferenceValue = config;
                    Debug.Log($"✅ Default weapon config assigned: {config.weaponName}");
                }
            }
            
            // 5. Effect Prefabs
            GameObject muzzleFlash = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Effects/MuzzleFlash.prefab");
            GameObject bulletHole = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Effects/BulletHole.prefab");
            GameObject bloodEffect = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Effects/BloodEffect.prefab");
            GameObject metalSparks = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Effects/MetalSparks.prefab");
            
            so.FindProperty("muzzleFlashPrefab").objectReferenceValue = muzzleFlash;
            so.FindProperty("bulletHolePrefab").objectReferenceValue = bulletHole;
            so.FindProperty("bloodEffectPrefab").objectReferenceValue = bloodEffect;
            so.FindProperty("metalSparksPrefab").objectReferenceValue = metalSparks;
            
            Debug.Log("✅ Effect prefabs assigned");
            
            // 6. Create and assign fire sounds
            AudioClip[] fireSounds = CreateFireSounds();
            var fireSoundsProp = so.FindProperty("fireSounds");
            if (fireSoundsProp != null && fireSoundsProp.isArray)
            {
                fireSoundsProp.ClearArray();
                fireSoundsProp.arraySize = fireSounds.Length;
                for (int i = 0; i < fireSounds.Length; i++)
                {
                    fireSoundsProp.GetArrayElementAtIndex(i).objectReferenceValue = fireSounds[i];
                }
                Debug.Log($"✅ Fire sounds assigned: {fireSounds.Length} clips");
            }
            
            so.ApplyModifiedProperties();
            
            // Kaydet
            PrefabUtility.SaveAsPrefabAsset(playerInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(playerInstance);
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("✅ WeaponSystem references fixed!");
            Debug.Log("═══════════════════════════════════════════");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static void CreateDefaultWeaponConfig()
        {
            // Create Configs folder
            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Configs/Weapons"))
            {
                AssetDatabase.CreateFolder("Assets/Configs", "Weapons");
            }
            
            // Create Assault Rifle config
            string path = "Assets/Configs/Weapons/AssaultRifle.asset";
            
            if (AssetDatabase.LoadAssetAtPath<WeaponConfig>(path) == null)
            {
                WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
                config.weaponName = "Assault Rifle";
                config.damage = 25f;
                config.range = 100f;
                config.fireRate = 10f;
                config.fireMode = FireMode.Auto;
                config.hipSpread = 0.05f;
                config.aimSpread = 0.01f;
                config.recoilAmount = 2f;
                config.headshotMultiplier = 2f;
                config.magazineSize = 30;
                config.maxAmmo = 120;
                config.reloadTime = 2f;
                
                AssetDatabase.CreateAsset(config, path);
                Debug.Log($"✅ Default weapon config created: {path}");
            }
        }
        
        private static AudioClip[] CreateFireSounds()
        {
            // Create simple fire sound clips
            AudioClip[] sounds = new AudioClip[3];
            
            for (int i = 0; i < sounds.Length; i++)
            {
                string name = $"FireSound_{i + 1}";
                float frequency = 200f + (i * 50f); // Different frequencies
                float duration = 0.1f;
                
                sounds[i] = CreateSimpleAudioClip(name, duration, frequency);
            }
            
            return sounds;
        }
        
        private static AudioClip CreateSimpleAudioClip(string name, float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                // Create a simple gunshot-like sound
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 20f); // Quick decay
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.3f;
            }
            
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            
            return clip;
        }
    }
}
