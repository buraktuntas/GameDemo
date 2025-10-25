using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    public class ManualAudioFix
    {
        [MenuItem("Tools/Tactical Combat/Manual Audio Fix")]
        public static void ExecuteManualAudioFix()
        {
            Debug.Log("🔊 MANUAL AUDIO FIX BAŞLIYOR...");
            
            // Create audio clips
            CreateFireSound1();
            CreateFireSound2();
            CreateFireSound3();
            CreateHitSound1();
            CreateHitSound2();
            CreateReloadSound();
            CreateEmptySound();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ MANUAL AUDIO FIX TAMAMLANDI!");
            Debug.Log("📋 Şimdi Player prefab'ını aç ve WeaponSystem'e audio clip'leri manuel olarak ata:");
            Debug.Log("  1. Assets/Prefabs/Player.prefab aç");
            Debug.Log("  2. WeaponSystem component'ini bul");
            Debug.Log("  3. Fire Sounds array'ini genişlet (Size: 3)");
            Debug.Log("  4. Her element'e oluşturulan audio clip'leri sürükle");
        }
        
        private static void CreateFireSound1()
        {
            CreateSimpleAudioClip("FireSound_1", 0.1f, 200f);
        }
        
        private static void CreateFireSound2()
        {
            CreateSimpleAudioClip("FireSound_2", 0.1f, 250f);
        }
        
        private static void CreateFireSound3()
        {
            CreateSimpleAudioClip("FireSound_3", 0.1f, 300f);
        }
        
        private static void CreateHitSound1()
        {
            CreateSimpleAudioClip("HitSound_1", 0.05f, 150f);
        }
        
        private static void CreateHitSound2()
        {
            CreateSimpleAudioClip("HitSound_2", 0.05f, 180f);
        }
        
        private static void CreateReloadSound()
        {
            CreateSimpleAudioClip("ReloadSound", 0.3f, 100f);
        }
        
        private static void CreateEmptySound()
        {
            CreateSimpleAudioClip("EmptyGunSound", 0.2f, 80f);
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
            
            Debug.Log($"✅ Created: {name} at {path}");
        }
    }
}
