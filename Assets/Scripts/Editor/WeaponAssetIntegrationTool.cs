using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON ASSET INTEGRATION TOOL
    /// Low Poly Pistol Weapon Pack'ini WeaponSystem'e entegre eder
    /// </summary>
    public class WeaponAssetIntegrationTool : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Integrate Weapon Assets")]
        public static void ShowWindow()
        {
            GetWindow<WeaponAssetIntegrationTool>("Weapon Asset Integration");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔫 Weapon Asset Integration Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔍 List Available Weapons"))
            {
                ListAvailableWeapons();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔫 Create Weapon Prefabs"))
            {
                CreateWeaponPrefabs();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🎯 Assign Weapons to Player Prefab"))
            {
                AssignWeaponsToPlayerPrefab();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🎨 Setup Weapon Holder"))
            {
                SetupWeaponHolder();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("⚙️ Configure WeaponSystem"))
            {
                ConfigureWeaponSystem();
            }
        }
        
        private void ListAvailableWeapons()
        {
            Debug.Log("🔍 Available Weapons:");
            
            string[] weaponNames = { "Pistol_A", "Pistol_B", "Pistol_C", "Pistol_D", "Pistol_E" };
            
            foreach (string weaponName in weaponNames)
            {
                GameObject weapon = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Low Poly Pistol Weapon Pack 1/Prefabs/Weapons/{weaponName}.prefab");
                if (weapon != null)
                {
                    Debug.Log($"✅ {weaponName} - Found");
                    
                    // Check components
                    MeshRenderer renderer = weapon.GetComponent<MeshRenderer>();
                    MeshFilter filter = weapon.GetComponent<MeshFilter>();
                    
                    if (renderer != null) Debug.Log($"   - MeshRenderer: ✅");
                    if (filter != null) Debug.Log($"   - MeshFilter: ✅");
                }
                else
                {
                    Debug.LogWarning($"❌ {weaponName} - Not found");
                }
            }
        }
        
        private void CreateWeaponPrefabs()
        {
            Debug.Log("🔫 Creating weapon prefabs...");
            
            string[] weaponNames = { "Pistol_A", "Pistol_B", "Pistol_C", "Pistol_D", "Pistol_E" };
            
            foreach (string weaponName in weaponNames)
            {
                CreateWeaponPrefab(weaponName);
            }
            
            Debug.Log("✅ Weapon prefabs created!");
        }
        
        private void CreateWeaponPrefab(string weaponName)
        {
            // Load original weapon
            GameObject originalWeapon = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Low Poly Pistol Weapon Pack 1/Prefabs/Weapons/{weaponName}.prefab");
            
            if (originalWeapon == null)
            {
                Debug.LogError($"❌ {weaponName} not found!");
                return;
            }
            
            // Create new weapon prefab
            GameObject weaponPrefab = Instantiate(originalWeapon);
            weaponPrefab.name = $"Tactical_{weaponName}";
            
            // Add WeaponSystem components
            AddWeaponComponents(weaponPrefab, weaponName);
            
            // Save as prefab
            string prefabPath = $"Assets/Prefabs/Weapons/Tactical_{weaponName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(weaponPrefab, prefabPath);
            
            DestroyImmediate(weaponPrefab);
            
            Debug.Log($"✅ Created Tactical_{weaponName}");
        }
        
        private void AddWeaponComponents(GameObject weapon, string weaponName)
        {
            // ❌ REMOVED: Don't add NetworkIdentity to weapon prefabs!
            // ✅ REASON: Player prefab already has NetworkIdentity, child objects shouldn't have it
            
            // Add WeaponSystem component (will work with parent's NetworkIdentity)
            WeaponSystem weaponSystem = weapon.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                weaponSystem = weapon.AddComponent<WeaponSystem>();
                Debug.Log($"✅ Added WeaponSystem to {weaponName}");
            }
            
            // Configure based on weapon type
            ConfigureWeaponSystem(weaponSystem, weaponName);
            
            // Add AudioSource
            AudioSource audioSource = weapon.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = weapon.AddComponent<AudioSource>();
            }
            
            // Configure audio
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            
            // ✅ FIX: Assign proper materials to weapon
            AssignWeaponMaterials(weapon, weaponName);
            
            Debug.Log($"✅ Added components to {weaponName} (NO NetworkIdentity - uses parent's)");
        }
        
        private void AssignWeaponMaterials(GameObject weapon, string weaponName)
        {
            // ✅ FIX: Use sharedMaterial to prevent material leak in Editor mode
            Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                // Create material only once
                Material weaponMaterial = new Material(Shader.Find("Standard"));
                weaponMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
                weaponMaterial.name = $"{weaponName}_Material";
                
                // ✅ FIX: Use sharedMaterial to prevent leak
                renderer.sharedMaterial = weaponMaterial;
                
                Debug.Log($"✅ Material assigned to {renderer.name} - Color: {weaponMaterial.color}");
            }
        }
        
        private void ConfigureWeaponSystem(WeaponSystem weaponSystem, string weaponName)
        {
            // Create weapon config
            WeaponConfig config = CreateWeaponConfig(weaponName);
            
            // Assign config using reflection
            SetPrivateField(weaponSystem, "currentWeapon", config);
            
            // Assign audio clips
            AssignAudioClips(weaponSystem);
            
            Debug.Log($"✅ Configured WeaponSystem for {weaponName}");
        }
        
        private WeaponConfig CreateWeaponConfig(string weaponName)
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            config.name = $"Config_{weaponName}";
            
            // Set weapon-specific values
            switch (weaponName)
            {
                case "Pistol_A":
                    SetWeaponConfigValues(config, 20, 300f, 50f, 12, 60, 1.5f, 0.05f, 0.02f);
                    break;
                case "Pistol_B":
                    SetWeaponConfigValues(config, 25, 250f, 45f, 15, 75, 2.0f, 0.08f, 0.03f);
                    break;
                case "Pistol_C":
                    SetWeaponConfigValues(config, 18, 350f, 55f, 10, 50, 1.2f, 0.04f, 0.015f);
                    break;
                case "Pistol_D":
                    SetWeaponConfigValues(config, 22, 280f, 48f, 14, 70, 1.8f, 0.06f, 0.025f);
                    break;
                case "Pistol_E":
                    SetWeaponConfigValues(config, 30, 200f, 40f, 8, 40, 2.5f, 0.1f, 0.04f);
                    break;
            }
            
            // Save config
            AssetDatabase.CreateAsset(config, $"Assets/Configs/Weapons/Config_{weaponName}.asset");
            AssetDatabase.SaveAssets();
            
            return config;
        }
        
        private void SetWeaponConfigValues(WeaponConfig config, int damage, float fireRate, float range, int magazineSize, int maxAmmo, float reloadTime, float recoilAmount, float spreadAmount)
        {
            SetPrivateField(config, "damage", damage);
            SetPrivateField(config, "fireRate", fireRate);
            SetPrivateField(config, "range", range);
            SetPrivateField(config, "magazineSize", magazineSize);
            SetPrivateField(config, "maxAmmo", maxAmmo);
            SetPrivateField(config, "reloadTime", reloadTime);
            SetPrivateField(config, "recoilAmount", recoilAmount);
            SetPrivateField(config, "spreadAmount", spreadAmount);
        }
        
        private void AssignAudioClips(WeaponSystem weaponSystem)
        {
            // Fire sounds
            AudioClip[] fireSounds = new AudioClip[3];
            for (int i = 0; i < 3; i++)
            {
                fireSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/FireSound_{i + 1}.mp3");
            }
            SetPrivateField(weaponSystem, "fireSounds", fireSounds);
            
            // Hit sounds
            AudioClip[] hitSounds = new AudioClip[2];
            for (int i = 0; i < 2; i++)
            {
                hitSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/HitSound_{i + 1}.mp3");
            }
            SetPrivateField(weaponSystem, "hitSounds", hitSounds);
            
            // Other sounds
            AudioClip reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ReloadSound.mp3");
            AudioClip emptySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EmptySound.mp3");
            
            SetPrivateField(weaponSystem, "reloadSound", reloadSound);
            SetPrivateField(weaponSystem, "emptySound", emptySound);
        }
        
        private void AssignWeaponsToPlayerPrefab()
        {
            Debug.Log("🎯 Assigning weapons to Player prefab...");
            
            // Load Player prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found!");
                return;
            }
            
            // Open prefab
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // Find or create weapon holder
            Transform weaponHolder = prefabInstance.transform.Find("WeaponHolder");
            if (weaponHolder == null)
            {
                GameObject holder = new GameObject("WeaponHolder");
                holder.transform.SetParent(prefabInstance.transform);
                holder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                weaponHolder = holder.transform;
            }
            
            // ✅ FIX: Remove any existing NetworkIdentity from CurrentWeapon
            Transform existingWeapon = weaponHolder.Find("CurrentWeapon");
            if (existingWeapon != null)
            {
                NetworkIdentity existingNetworkIdentity = existingWeapon.GetComponent<NetworkIdentity>();
                if (existingNetworkIdentity != null)
                {
                    DestroyImmediate(existingNetworkIdentity);
                    Debug.Log("✅ Removed NetworkIdentity from existing CurrentWeapon");
                }
                DestroyImmediate(existingWeapon.gameObject);
            }
            
            // Assign default weapon
            GameObject defaultWeapon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Tactical_Pistol_A.prefab");
            if (defaultWeapon != null)
            {
                // ✅ FIX: First remove NetworkIdentity from the source prefab
                GameObject prefabContents = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Weapons/Tactical_Pistol_A.prefab");
                NetworkIdentity prefabNetworkIdentity = prefabContents.GetComponent<NetworkIdentity>();
                if (prefabNetworkIdentity != null)
                {
                    DestroyImmediate(prefabNetworkIdentity);
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, "Assets/Prefabs/Weapons/Tactical_Pistol_A.prefab");
                    Debug.Log("✅ Removed NetworkIdentity from Tactical_Pistol_A prefab");
                }
                PrefabUtility.UnloadPrefabContents(prefabContents);
                
                // Now instantiate the cleaned prefab
                GameObject weaponInstance = Instantiate(defaultWeapon, weaponHolder);
                weaponInstance.name = "CurrentWeapon";
                
                // Configure weapon
                ConfigureWeaponForPlayer(weaponInstance);
            }
            
            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log("✅ Weapons assigned to Player prefab!");
        }
        
        private void ConfigureWeaponForPlayer(GameObject weapon)
        {
            // Get WeaponSystem component
            WeaponSystem weaponSystem = weapon.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError("❌ WeaponSystem component not found on weapon!");
                return;
            }
            
            // Assign camera reference
            Camera camera = weapon.GetComponentInParent<Camera>();
            if (camera != null)
            {
                SetPrivateField(weaponSystem, "playerCamera", camera);
            }
            
            // Assign weapon holder
            Transform weaponHolder = weapon.transform.parent;
            if (weaponHolder != null)
            {
                SetPrivateField(weaponSystem, "weaponHolder", weaponHolder);
            }
            
            // Assign audio source
            AudioSource audioSource = weapon.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                SetPrivateField(weaponSystem, "audioSource", audioSource);
            }
            
            Debug.Log("✅ Weapon configured for player!");
        }
        
        private void SetupWeaponHolder()
        {
            Debug.Log("🎨 Setting up weapon holder...");
            
            // Create weapon holder prefab
            GameObject weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.position = new Vector3(0.3f, -0.2f, 0.5f);
            
            // Add components
            weaponHolder.AddComponent<WeaponHolderController>();
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(weaponHolder, "Assets/Prefabs/Weapons/WeaponHolder.prefab");
            
            DestroyImmediate(weaponHolder);
            
            Debug.Log("✅ Weapon holder setup complete!");
        }
        
        private void ConfigureWeaponSystem()
        {
            Debug.Log("⚙️ Configuring WeaponSystem...");
            
            // Find all WeaponSystem components
            WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsSortMode.None);
            
            foreach (WeaponSystem ws in weaponSystems)
            {
                ConfigureWeaponSystem(ws, "Default");
            }
            
            Debug.Log("✅ WeaponSystem configuration complete!");
        }
        
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
    
    /// <summary>
    /// ✅ WEAPON HOLDER CONTROLLER
    /// Silah tutucu kontrolü için
    /// </summary>
    public class WeaponHolderController : MonoBehaviour
    {
        [Header("Weapon Settings")]
        public float weaponSwayAmount = 0.1f;
        public float weaponBobAmount = 0.05f;
        public float weaponSwaySpeed = 2f;
        public float weaponBobSpeed = 4f;
        
        private Vector3 originalPosition;
        private float bobTimer;
        
        void Start()
        {
            originalPosition = transform.localPosition;
        }
        
        void Update()
        {
            // Weapon sway
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            Vector3 swayOffset = new Vector3(
                mouseX * weaponSwayAmount,
                mouseY * weaponSwayAmount,
                0f
            );
            
            // Weapon bob
            bobTimer += Time.deltaTime * weaponBobSpeed;
            Vector3 bobOffset = new Vector3(
                0f,
                Mathf.Sin(bobTimer) * weaponBobAmount,
                0f
            );
            
            transform.localPosition = originalPosition + swayOffset + bobOffset;
        }
    }
}
