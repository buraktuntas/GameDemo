using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class QuickAudioFix
    {
        [MenuItem("Tools/Tactical Combat/Quick Audio Fix")]
        public static void ExecuteQuickAudioFix()
        {
            Debug.Log("🔊 QUICK AUDIO FIX BAŞLIYOR...");
            
            // 1. Audio clips oluştur
            CreateAudioClips();
            
            // 2. WeaponSystem'e ata
            AssignToWeaponSystem();
            
            Debug.Log("✅ QUICK AUDIO FIX TAMAMLANDI!");
        }
        
        private static void CreateAudioClips()
        {
            // Create fire sounds
            for (int i = 1; i <= 3; i++)
            {
                string name = $"FireSound_{i}";
                CreateSimpleAudioClip(name, 0.1f, 200f + (i * 50f));
            }
            
            // Create hit sounds
            for (int i = 1; i <= 2; i++)
            {
                string name = $"HitSound_{i}";
                CreateSimpleAudioClip(name, 0.05f, 150f + (i * 30f));
            }
            
            // Create reload sound
            CreateSimpleAudioClip("ReloadSound", 0.3f, 100f);
            
            // Create empty gun sound
            CreateSimpleAudioClip("EmptyGunSound", 0.2f, 80f);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ Audio clips created!");
        }
        
        private static void CreateSimpleAudioClip(string name, float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * 0.3f;
                
                // Add some noise for realism
                samples[i] += Random.Range(-0.1f, 0.1f);
            }
            
            clip.SetData(samples, 0);
            
            // Save to Assets/Audio folder
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
            
            Debug.Log($"✅ Created: {name}");
        }
        
        private static void AssignToWeaponSystem()
        {
            // Player prefab'ı yükle
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found!");
                return;
            }
            
            GameObject playerInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // WeaponSystem component'ini bul
            var weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError("❌ WeaponSystem component not found!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }
            
            // SerializedObject oluştur
            SerializedObject so = new SerializedObject(weaponSystem);
            
            // Fire sounds ata
            var fireSoundsProp = so.FindProperty("fireSounds");
            if (fireSoundsProp != null && fireSoundsProp.isArray)
            {
                fireSoundsProp.ClearArray();
                fireSoundsProp.arraySize = 3;
                
                for (int i = 0; i < 3; i++)
                {
                    string clipName = $"FireSound_{i + 1}";
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{clipName}.asset");
                    
                    if (clip != null)
                    {
                        fireSoundsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                        Debug.Log($"✅ Assigned: {clipName}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Audio clip not found: {clipName}");
                    }
                }
            }
            
            // Hit sounds ata
            var hitSoundsProp = so.FindProperty("hitSounds");
            if (hitSoundsProp != null && hitSoundsProp.isArray)
            {
                hitSoundsProp.ClearArray();
                hitSoundsProp.arraySize = 2;
                
                for (int i = 0; i < 2; i++)
                {
                    string clipName = $"HitSound_{i + 1}";
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{clipName}.asset");
                    
                    if (clip != null)
                    {
                        hitSoundsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                        Debug.Log($"✅ Assigned: {clipName}");
                    }
                }
            }
            
            // Reload sound ata
            var reloadSoundProp = so.FindProperty("reloadSound");
            if (reloadSoundProp != null)
            {
                AudioClip reloadClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ReloadSound.asset");
                if (reloadClip != null)
                {
                    reloadSoundProp.objectReferenceValue = reloadClip;
                    Debug.Log("✅ Assigned: ReloadSound");
                }
            }
            
            // Empty sound ata
            var emptySoundProp = so.FindProperty("emptySound");
            if (emptySoundProp != null)
            {
                AudioClip emptyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EmptyGunSound.asset");
                if (emptyClip != null)
                {
                    emptySoundProp.objectReferenceValue = emptyClip;
                    Debug.Log("✅ Assigned: EmptyGunSound");
                }
            }
            
            // Değişiklikleri uygula
            so.ApplyModifiedProperties();
            
            // Prefab'ı kaydet
            PrefabUtility.SaveAsPrefabAsset(playerInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(playerInstance);
            
            Debug.Log("✅ WeaponSystem audio clips assigned!");
        }
    }
}
