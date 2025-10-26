using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ MANUAL AUDIO ASSIGNMENT FIXER
    /// WeaponSystem'e audio clip'leri manuel olarak assign eder
    /// </summary>
    public class ManualAudioAssignmentFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Manual Audio Assignment Fix")]
        public static void ShowWindow()
        {
            GetWindow<ManualAudioAssignmentFixer>("Manual Audio Assignment Fix");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔊 Manual Audio Assignment Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("🎯 Assign Audio to Player Prefab"))
            {
                AssignAudioToPlayerPrefab();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Assign Audio to Scene WeaponSystems"))
            {
                AssignAudioToSceneWeaponSystems();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("📋 List Available Audio Clips"))
            {
                ListAvailableAudioClips();
            }
        }
        
        private void AssignAudioToPlayerPrefab()
        {
            // Player prefab'ı bul
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at Assets/Prefabs/Player.prefab");
                return;
            }
            
            // Prefab'ı aç
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            WeaponSystem weaponSystem = prefabInstance.GetComponent<WeaponSystem>();
            
            if (weaponSystem == null)
            {
                Debug.LogError("❌ WeaponSystem component not found on Player prefab");
                PrefabUtility.UnloadPrefabContents(prefabInstance);
                return;
            }
            
            // Audio clip'leri assign et
            AssignAudioClipsToWeaponSystem(weaponSystem);
            
            // Prefab'ı kaydet
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log("✅ Audio clips assigned to Player prefab!");
        }
        
        private void AssignAudioToSceneWeaponSystems()
        {
            WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 Found {weaponSystems.Length} WeaponSystem components in scene");
            
            foreach (WeaponSystem ws in weaponSystems)
            {
                AssignAudioClipsToWeaponSystem(ws);
            }
            
            Debug.Log("✅ Audio clips assigned to all scene WeaponSystems!");
        }
        
        private void AssignAudioClipsToWeaponSystem(WeaponSystem weaponSystem)
        {
            if (weaponSystem == null) return;
            
            Debug.Log($"🔊 Assigning audio clips to {weaponSystem.gameObject.name}");
            
            // Fire sounds
            AudioClip[] fireSounds = new AudioClip[3];
            for (int i = 0; i < 3; i++)
            {
                fireSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/FireSound_{i + 1}.mp3");
                if (fireSounds[i] == null)
                {
                    Debug.LogWarning($"⚠️ FireSound_{i + 1}.mp3 not found");
                }
            }
            SetPrivateField(weaponSystem, "fireSounds", fireSounds);
            
            // Hit sounds
            AudioClip[] hitSounds = new AudioClip[2];
            for (int i = 0; i < 2; i++)
            {
                hitSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/HitSound_{i + 1}.mp3");
                if (hitSounds[i] == null)
                {
                    Debug.LogWarning($"⚠️ HitSound_{i + 1}.mp3 not found");
                }
            }
            SetPrivateField(weaponSystem, "hitSounds", hitSounds);
            
            // Reload sound
            AudioClip reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ReloadSound.mp3");
            if (reloadSound == null)
            {
                Debug.LogWarning("⚠️ ReloadSound.mp3 not found");
            }
            SetPrivateField(weaponSystem, "reloadSound", reloadSound);
            
            // Empty sound
            AudioClip emptySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EmptySound.mp3");
            if (emptySound == null)
            {
                Debug.LogWarning("⚠️ EmptySound.mp3 not found");
            }
            SetPrivateField(weaponSystem, "emptySound", emptySound);
            
            Debug.Log($"✅ Audio clips assigned to {weaponSystem.gameObject.name}");
        }
        
        private void ListAvailableAudioClips()
        {
            Debug.Log("📋 Available Audio Clips:");
            
            string[] audioNames = { "FireSound_1", "FireSound_2", "FireSound_3", "HitSound_1", "HitSound_2", "ReloadSound", "EmptySound" };
            
            foreach (string audioName in audioNames)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{audioName}.mp3");
                if (clip != null)
                {
                    Debug.Log($"✅ {audioName}.mp3 - Found");
                }
                else
                {
                    Debug.LogWarning($"❌ {audioName}.mp3 - Not found");
                }
            }
        }
        
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"❌ Field {fieldName} not found on {obj.GetType().Name}");
            }
        }
    }
}
