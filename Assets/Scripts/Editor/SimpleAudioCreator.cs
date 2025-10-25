using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    public class SimpleAudioCreator
    {
        [MenuItem("Tools/Create Simple Audio Clips")]
        public static void CreateSimpleAudioClips()
        {
            Debug.Log("🔊 Creating simple audio clips...");
            
            // Create directory if not exists
            if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }
            
            // Create fire sounds
            CreateAudioClip("FireSound_1", 0.1f, 200f);
            CreateAudioClip("FireSound_2", 0.1f, 250f);
            CreateAudioClip("FireSound_3", 0.1f, 300f);
            
            // Create hit sounds
            CreateAudioClip("HitSound_1", 0.05f, 150f);
            CreateAudioClip("HitSound_2", 0.05f, 180f);
            
            // Create other sounds
            CreateAudioClip("ReloadSound", 0.3f, 100f);
            CreateAudioClip("EmptyGunSound", 0.2f, 80f);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ Audio clips created in Assets/Audio/");
            Debug.Log("📋 Now manually assign them to WeaponSystem in Player prefab");
        }
        
        private static void CreateAudioClip(string name, float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * 0.3f;
                samples[i] += Random.Range(-0.1f, 0.1f); // Add noise
            }
            
            clip.SetData(samples, 0);
            
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
            
            Debug.Log($"✅ Created: {name}");
        }
    }
}
