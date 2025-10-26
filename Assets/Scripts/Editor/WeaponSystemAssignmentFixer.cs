using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON SYSTEM ASSIGNMENT FIXER
    /// Inspector'da atanmamış field'ları otomatik assign eder
    /// </summary>
    public class WeaponSystemAssignmentFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Fix WeaponSystem Assignments")]
        public static void ShowWindow()
        {
            GetWindow<WeaponSystemAssignmentFixer>("WeaponSystem Assignment Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔧 WeaponSystem Assignment Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Find All WeaponSystem Components"))
            {
                FindAndFixWeaponSystems();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🎯 Fix Player Prefab WeaponSystem"))
            {
                FixPlayerPrefabWeaponSystem();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🎨 Create Missing Effect Prefabs"))
            {
                CreateMissingEffectPrefabs();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔊 Create Missing Audio Clips"))
            {
                CreateMissingAudioClips();
            }
        }
        
        private void FindAndFixWeaponSystems()
        {
            WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 Found {weaponSystems.Length} WeaponSystem components");
            
            foreach (WeaponSystem ws in weaponSystems)
            {
                FixWeaponSystemAssignments(ws);
            }
            
            Debug.Log("✅ WeaponSystem assignments fixed!");
        }
        
        private void FixPlayerPrefabWeaponSystem()
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
            
            FixWeaponSystemAssignments(weaponSystem);
            
            // Prefab'ı kaydet
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log("✅ Player prefab WeaponSystem assignments fixed!");
        }
        
        private void FixWeaponSystemAssignments(WeaponSystem weaponSystem)
        {
            if (weaponSystem == null) return;
            
            Debug.Log($"🔧 Fixing WeaponSystem on {weaponSystem.gameObject.name}");
            
            // Camera reference
            Camera camera = GetPrivateField<Camera>(weaponSystem, "playerCamera");
            if (camera == null)
            {
                camera = weaponSystem.GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    SetPrivateField(weaponSystem, "playerCamera", camera);
                    Debug.Log("✅ Camera assigned");
                }
            }
            
            // AudioSource reference
            AudioSource audioSource = GetPrivateField<AudioSource>(weaponSystem, "audioSource");
            if (audioSource == null)
            {
                audioSource = weaponSystem.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = weaponSystem.gameObject.AddComponent<AudioSource>();
                }
                SetPrivateField(weaponSystem, "audioSource", audioSource);
                Debug.Log("✅ AudioSource assigned");
            }
            
            // Weapon holder
            Transform weaponHolder = GetPrivateField<Transform>(weaponSystem, "weaponHolder");
            if (weaponHolder == null)
            {
                weaponHolder = weaponSystem.transform.Find("WeaponHolder");
                if (weaponHolder == null)
                {
                    GameObject holder = new GameObject("WeaponHolder");
                    holder.transform.SetParent(weaponSystem.transform);
                    holder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                    weaponHolder = holder.transform;
                }
                SetPrivateField(weaponSystem, "weaponHolder", weaponHolder);
                Debug.Log("✅ WeaponHolder assigned");
            }
            
            // Effect prefabs
            AssignEffectPrefab(weaponSystem, "hitEffectPrefab", "HitEffect");
            AssignEffectPrefab(weaponSystem, "muzzleFlashPrefab", "MuzzleFlash");
            AssignEffectPrefab(weaponSystem, "bulletHolePrefab", "BulletHole");
            AssignEffectPrefab(weaponSystem, "bloodEffectPrefab", "BloodEffect");
            AssignEffectPrefab(weaponSystem, "metalSparksPrefab", "MetalSparks");
            
            // Audio clips
            AssignAudioClips(weaponSystem);
            
            // Weapon config
            WeaponConfig config = GetPrivateField<WeaponConfig>(weaponSystem, "currentWeapon");
            if (config == null)
            {
                config = CreateDefaultWeaponConfig();
                SetPrivateField(weaponSystem, "currentWeapon", config);
                Debug.Log("✅ Default WeaponConfig assigned");
            }
        }
        
        private void AssignEffectPrefab(WeaponSystem weaponSystem, string fieldName, string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Effects/{prefabName}.prefab");
            
            if (prefab == null)
            {
                Debug.LogWarning($"⚠️ {prefabName} prefab not found, will create it");
                return;
            }
            
            SetPrivateField(weaponSystem, fieldName, prefab);
            Debug.Log($"✅ {prefabName} assigned to {fieldName}");
        }
        
        private void AssignAudioClips(WeaponSystem weaponSystem)
        {
            // Fire sounds
            AudioClip[] fireSounds = new AudioClip[3];
            for (int i = 0; i < 3; i++)
            {
                fireSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/FireSound_{i + 1}.asset");
                if (fireSounds[i] == null)
                {
                    fireSounds[i] = CreateSimpleAudioClip($"FireSound_{i + 1}");
                }
            }
            SetPrivateField(weaponSystem, "fireSounds", fireSounds);
            
            // Hit sounds
            AudioClip[] hitSounds = new AudioClip[2];
            for (int i = 0; i < 2; i++)
            {
                hitSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/HitSound_{i + 1}.asset");
                if (hitSounds[i] == null)
                {
                    hitSounds[i] = CreateSimpleAudioClip($"HitSound_{i + 1}");
                }
            }
            SetPrivateField(weaponSystem, "hitSounds", hitSounds);
            
            // Reload sound
            AudioClip reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ReloadSound.asset");
            if (reloadSound == null)
            {
                reloadSound = CreateSimpleAudioClip("ReloadSound");
            }
            SetPrivateField(weaponSystem, "reloadSound", reloadSound);
            
            // Empty sound
            AudioClip emptySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EmptySound.asset");
            if (emptySound == null)
            {
                emptySound = CreateSimpleAudioClip("EmptySound");
            }
            SetPrivateField(weaponSystem, "emptySound", emptySound);
            
            Debug.Log("✅ Audio clips assigned");
        }
        
        private WeaponConfig CreateDefaultWeaponConfig()
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            config.name = "Default Assault Rifle";
            
            // Set default values
            SetPrivateField(config, "damage", 25);
            SetPrivateField(config, "fireRate", 600f);
            SetPrivateField(config, "range", 100f);
            SetPrivateField(config, "magazineSize", 30);
            SetPrivateField(config, "maxAmmo", 120);
            SetPrivateField(config, "reloadTime", 2.5f);
            SetPrivateField(config, "recoilAmount", 0.1f);
            SetPrivateField(config, "spreadAmount", 0.05f);
            
            // Save asset
            AssetDatabase.CreateAsset(config, "Assets/Scripts/Combat/DefaultWeaponConfig.asset");
            AssetDatabase.SaveAssets();
            
            return config;
        }
        
        private AudioClip CreateSimpleAudioClip(string name)
        {
            // Create a simple sine wave audio clip
            int sampleRate = 44100;
            float duration = 0.5f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] samplesArray = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                samplesArray[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / sampleRate) * 0.1f;
            }
            
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(samplesArray, 0);
            
            return clip;
        }
        
        private void CreateMissingEffectPrefabs()
        {
            string[] effectNames = { "HitEffect", "MuzzleFlash", "BulletHole", "BloodEffect", "MetalSparks" };
            
            foreach (string effectName in effectNames)
            {
                CreateEffectPrefab(effectName);
            }
            
            Debug.Log("✅ Missing effect prefabs created!");
        }
        
        private void CreateEffectPrefab(string effectName)
        {
            GameObject effect = new GameObject(effectName);
            
            // Add basic components
            effect.AddComponent<MeshRenderer>();
            effect.AddComponent<MeshFilter>();
            effect.AddComponent<AutoDestroy>().lifetime = 2f;
            
            // Set mesh based on effect type
            MeshFilter meshFilter = effect.GetComponent<MeshFilter>();
            switch (effectName)
            {
                case "HitEffect":
                case "MuzzleFlash":
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    break;
                case "BulletHole":
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                    break;
                case "BloodEffect":
                case "MetalSparks":
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    break;
            }
            
            // Set material
            MeshRenderer renderer = effect.GetComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Standard"));
            switch (effectName)
            {
                case "HitEffect":
                    material.color = Color.yellow;
                    break;
                case "MuzzleFlash":
                    material.color = Color.red;
                    break;
                case "BulletHole":
                    material.color = Color.black;
                    break;
                case "BloodEffect":
                    material.color = Color.red;
                    break;
                case "MetalSparks":
                    material.color = Color.white;
                    break;
            }
            renderer.sharedMaterial = material;
            
            // Save as prefab
            string prefabPath = $"Assets/Prefabs/Effects/{effectName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(effect, prefabPath);
            
            DestroyImmediate(effect);
            
            Debug.Log($"✅ Created {effectName} prefab");
        }
        
        private void CreateMissingAudioClips()
        {
            // Create audio directory
            if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }
            
            string[] audioNames = { "FireSound_1", "FireSound_2", "FireSound_3", "HitSound_1", "HitSound_2", "ReloadSound", "EmptySound" };
            
            foreach (string audioName in audioNames)
            {
                AudioClip clip = CreateSimpleAudioClip(audioName);
                // ✅ FIX: Use .asset extension instead of .wav
                AssetDatabase.CreateAsset(clip, $"Assets/Audio/{audioName}.asset");
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("✅ Missing audio clips created!");
        }
        
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
        
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return (T)field.GetValue(obj);
            }
            return default(T);
        }
    }
}
