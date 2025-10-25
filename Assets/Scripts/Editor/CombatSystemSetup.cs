using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Combat System Setup Tool
    /// Tools > Tactical Combat > Setup Combat System
    /// </summary>
    public class CombatSystemSetup
    {
        [MenuItem("Tools/Tactical Combat/Setup Combat System (PROFESSIONAL)", priority = 100)]
        public static void SetupCombatSystem()
        {
            Debug.Log("🎯 Setting up Professional Combat System...");
            
            // 1. Create managers
            CreateCombatManager();
            CreateCombatUI();
            
            // 2. Create effect prefabs
            CreateEffectPrefabs();
            
            // 3. Create default weapon config
            CreateDefaultWeaponConfig();
            
            // 4. Update Player prefab
            UpdatePlayerPrefab();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Combat System Setup Complete!",
                "✅ Professional Combat System kuruldu!\n\n" +
                "Oluşturulanlar:\n" +
                "• CombatManager (Scene)\n" +
                "• CombatUI (Scene)\n" +
                "• Effect Prefabs\n" +
                "• Default Weapon Config\n" +
                "• Player WeaponSystem\n\n" +
                "ŞİMDİ YAP:\n" +
                "1. Play mode'a gir\n" +
                "2. Sol tık = Ateş et\n" +
                "3. R = Reload\n" +
                "4. Hit marker ve damage görünecek!\n\n" +
                "PROFESYONEL ÖZELLÜKLER:\n" +
                "• Recoil ✓\n" +
                "• Spread ✓\n" +
                "• Ammo System ✓\n" +
                "• Reload ✓\n" +
                "• Hit Effects ✓\n" +
                "• Damage Numbers ✓\n" +
                "• Camera Shake ✓\n" +
                "• Hit Markers ✓\n" +
                "• Audio Feedback ✓",
                "Harika!"
            );
        }
        
        private static void CreateCombatManager()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<Combat.CombatManager>();
            if (existing != null)
            {
                Debug.Log("⚠️ CombatManager already exists");
                return;
            }
            
            // Create
            GameObject managerGO = new GameObject("[CombatManager]");
            managerGO.AddComponent<Combat.CombatManager>();
            
            Debug.Log("✅ CombatManager created in scene");
        }
        
        private static void CreateCombatUI()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<TacticalCombat.UI.CombatUI>();
            if (existing != null)
            {
                Debug.Log("⚠️ CombatUI already exists");
                return;
            }
            
            // Create Canvas
            GameObject canvasGO = new GameObject("[CombatUI]");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add CombatUI component
            var combatUI = canvasGO.AddComponent<TacticalCombat.UI.CombatUI>();
            
            // Create Crosshair
            GameObject crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(canvasGO.transform);
            
            UnityEngine.UI.Image crosshair = crosshairGO.AddComponent<UnityEngine.UI.Image>();
            crosshair.color = Color.white;
            
            RectTransform crosshairRT = crosshair.rectTransform;
            crosshairRT.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRT.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRT.pivot = new Vector2(0.5f, 0.5f);
            crosshairRT.anchoredPosition = Vector2.zero;
            crosshairRT.sizeDelta = new Vector2(20, 20);
            
            // Create Hit Marker
            GameObject hitMarkerGO = new GameObject("HitMarker");
            hitMarkerGO.transform.SetParent(canvasGO.transform);
            
            UnityEngine.UI.Image hitMarker = hitMarkerGO.AddComponent<UnityEngine.UI.Image>();
            hitMarker.color = new Color(1, 1, 1, 0);
            
            RectTransform hitMarkerRT = hitMarker.rectTransform;
            hitMarkerRT.anchorMin = new Vector2(0.5f, 0.5f);
            hitMarkerRT.anchorMax = new Vector2(0.5f, 0.5f);
            hitMarkerRT.pivot = new Vector2(0.5f, 0.5f);
            hitMarkerRT.anchoredPosition = Vector2.zero;
            hitMarkerRT.sizeDelta = new Vector2(40, 40);
            
            hitMarkerGO.SetActive(false);
            
            // Create Ammo Text
            GameObject ammoGO = new GameObject("AmmoText");
            ammoGO.transform.SetParent(canvasGO.transform);
            
            TMPro.TextMeshProUGUI ammoText = ammoGO.AddComponent<TMPro.TextMeshProUGUI>();
            ammoText.text = "30";
            ammoText.fontSize = 48;
            ammoText.color = Color.white;
            ammoText.alignment = TMPro.TextAlignmentOptions.BottomRight;
            
            RectTransform ammoRT = ammoText.rectTransform;
            ammoRT.anchorMin = new Vector2(1, 0);
            ammoRT.anchorMax = new Vector2(1, 0);
            ammoRT.pivot = new Vector2(1, 0);
            ammoRT.anchoredPosition = new Vector2(-50, 50);
            ammoRT.sizeDelta = new Vector2(200, 100);
            
            Debug.Log("✅ CombatUI created in scene");
        }
        
        private static void CreateEffectPrefabs()
        {
            // Create Effects folder
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
            }
            
            // Muzzle Flash
            CreateMuzzleFlashPrefab();
            
            // Blood Effect
            CreateBloodEffectPrefab();
            
            // Metal Sparks
            CreateMetalSparksPrefab();
            
            Debug.Log("✅ Effect prefabs created");
        }
        
        private static void CreateMuzzleFlashPrefab()
        {
            GameObject muzzleFlash = new GameObject("MuzzleFlash");
            
            ParticleSystem ps = muzzleFlash.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 5f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.8f, 0.3f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 20)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;
            
            // Light
            GameObject lightGO = new GameObject("Light");
            lightGO.transform.SetParent(muzzleFlash.transform);
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.3f);
            light.intensity = 2f;
            light.range = 3f;
            
            // Auto destroy
            var autoDestroy = muzzleFlash.AddComponent<AutoDestroy>();
            autoDestroy.lifetime = 0.2f;
            
            // Save
            string path = "Assets/Prefabs/Effects/MuzzleFlash.prefab";
            PrefabUtility.SaveAsPrefabAsset(muzzleFlash, path);
            Object.DestroyImmediate(muzzleFlash);
        }
        
        private static void CreateBloodEffectPrefab()
        {
            GameObject blood = new GameObject("BloodEffect");
            
            ParticleSystem ps = blood.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.8f, 0f, 0f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 25)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;
            
            // Auto destroy
            var autoDestroy = blood.AddComponent<AutoDestroy>();
            autoDestroy.lifetime = 1f;
            
            // Save
            string path = "Assets/Prefabs/Effects/BloodEffect.prefab";
            PrefabUtility.SaveAsPrefabAsset(blood, path);
            Object.DestroyImmediate(blood);
        }
        
        private static void CreateMetalSparksPrefab()
        {
            GameObject sparks = new GameObject("MetalSparks");
            
            ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 5f;
            main.startSize = 0.02f;
            main.startColor = new Color(1f, 0.8f, 0.3f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 40)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;
            
            // Auto destroy
            var autoDestroy = sparks.AddComponent<AutoDestroy>();
            autoDestroy.lifetime = 0.5f;
            
            // Save
            string path = "Assets/Prefabs/Effects/MetalSparks.prefab";
            PrefabUtility.SaveAsPrefabAsset(sparks, path);
            Object.DestroyImmediate(sparks);
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
            
            if (AssetDatabase.LoadAssetAtPath<Combat.WeaponConfig>(path) == null)
            {
                Combat.WeaponConfig config = ScriptableObject.CreateInstance<Combat.WeaponConfig>();
                config.weaponName = "Assault Rifle";
                config.damage = 25f;
                config.range = 100f;
                config.fireRate = 10f;
                config.fireMode = Combat.FireMode.Auto;
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
        
        private static void UpdatePlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found!");
                return;
            }
            
            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            // Remove old SimpleGun
            var oldGun = playerInstance.GetComponent<Combat.SimpleGun>();
            if (oldGun != null)
            {
                Object.DestroyImmediate(oldGun);
                Debug.Log("🗑️ Removed old SimpleGun");
            }
            
            // Add WeaponSystem
            var weaponSystem = playerInstance.GetComponent<Combat.WeaponSystem>();
            if (weaponSystem == null)
            {
                weaponSystem = playerInstance.AddComponent<Combat.WeaponSystem>();
                Debug.Log("✅ Added WeaponSystem");
            }
            
            // Create weapon holder
            Transform weaponHolder = playerInstance.transform.Find("WeaponHolder");
            if (weaponHolder == null)
            {
                GameObject holderGO = new GameObject("WeaponHolder");
                holderGO.transform.SetParent(playerInstance.transform);
                holderGO.transform.localPosition = new Vector3(0.3f, 1.4f, 0.5f); // Right hand
                holderGO.transform.localRotation = Quaternion.identity;
                weaponHolder = holderGO.transform;
                Debug.Log("✅ Created WeaponHolder");
            }
            
            // Assign references
            SerializedObject so = new SerializedObject(weaponSystem);
            
            // Find camera
            Transform cameraTransform = playerInstance.transform.Find("PlayerCamera");
            if (cameraTransform != null)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                so.FindProperty("playerCamera").objectReferenceValue = cam;
            }
            
            so.FindProperty("weaponHolder").objectReferenceValue = weaponHolder;
            
            // Load default weapon config
            Combat.WeaponConfig config = AssetDatabase.LoadAssetAtPath<Combat.WeaponConfig>(
                "Assets/Configs/Weapons/AssaultRifle.asset");
            if (config != null)
            {
                so.FindProperty("currentWeapon").objectReferenceValue = config;
            }
            
            so.ApplyModifiedProperties();
            
            // Save
            PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
            PrefabUtility.UnloadPrefabContents(playerInstance);
            
            Debug.Log("✅ Player prefab updated with WeaponSystem");
        }
    }
    
    // Helper component for auto-destroying effects
    public class AutoDestroy : MonoBehaviour
    {
        public float lifetime = 1f;
        
        private void OnEnable()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
