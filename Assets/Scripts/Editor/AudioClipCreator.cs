using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    public class AudioClipCreator
    {
        [MenuItem("Tools/Tactical Combat/Create Audio Clips")]
        public static void CreateAudioClips()
        {
            // Create fire sounds
            CreateFireSound("FireSound_1", 200f, 0.1f);
            CreateFireSound("FireSound_2", 250f, 0.1f);
            CreateFireSound("FireSound_3", 300f, 0.1f);
            
            // Create hit sounds
            CreateHitSound("HitSound_1", 150f, 0.05f);
            CreateHitSound("HitSound_2", 180f, 0.05f);
            
            // Create reload sound
            CreateReloadSound("ReloadSound", 100f, 0.3f);
            
            // Create empty gun sound
            CreateEmptySound("EmptyGunSound", 80f, 0.2f);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ Audio clips created successfully!");
        }
        
        private static void CreateFireSound(string name, float frequency, float duration)
        {
            AudioClip clip = AudioClip.Create(name, 
                Mathf.RoundToInt(44100 * duration), 1, 44100, false);
            
            float[] samples = new float[clip.samples];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / 44100f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * 0.3f;
                
                // Add some noise for realism
                samples[i] += Random.Range(-0.1f, 0.1f);
            }
            
            clip.SetData(samples, 0);
            
            // Save to Assets/Audio folder
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
        }
        
        private static void CreateHitSound(string name, float frequency, float duration)
        {
            AudioClip clip = AudioClip.Create(name, 
                Mathf.RoundToInt(44100 * duration), 1, 44100, false);
            
            float[] samples = new float[clip.samples];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / 44100f;
                float envelope = 1f - (time / duration); // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * envelope * 0.2f;
            }
            
            clip.SetData(samples, 0);
            
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
        }
        
        private static void CreateReloadSound(string name, float frequency, float duration)
        {
            AudioClip clip = AudioClip.Create(name, 
                Mathf.RoundToInt(44100 * duration), 1, 44100, false);
            
            float[] samples = new float[clip.samples];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / 44100f;
                float envelope = Mathf.Sin(time / duration * Mathf.PI); // Bell curve
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * envelope * 0.15f;
            }
            
            clip.SetData(samples, 0);
            
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
        }
        
        private static void CreateEmptySound(string name, float frequency, float duration)
        {
            AudioClip clip = AudioClip.Create(name, 
                Mathf.RoundToInt(44100 * duration), 1, 44100, false);
            
            float[] samples = new float[clip.samples];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / 44100f;
                float envelope = 1f - (time / duration); // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * envelope * 0.1f;
            }
            
            clip.SetData(samples, 0);
            
            string path = $"Assets/Audio/{name}.asset";
            AssetDatabase.CreateAsset(clip, path);
        }
    }
}
