using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ CHARACTER SETUP TOOL
    /// Player prefab'a CharacterSelector ekler ve spawn point'ler oluşturur
    /// </summary>
    public class CharacterSetupTool : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🎭 Character Setup Tool")]
        public static void ShowWindow()
        {
            GetWindow<CharacterSetupTool>("Character Setup Tool");
        }
        
        [Header("Character Prefabs")]
        [SerializeField] private GameObject maleCharacterPrefab;
        [SerializeField] private GameObject femaleCharacterPrefab;
        
        [Header("Spawn Point Settings")]
        [SerializeField] private int spawnPointCount = 3;
        [SerializeField] private float spawnPointRadius = 10f;
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;
        
        private void OnGUI()
        {
            GUILayout.Label("🎭 Character Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("• Player prefab'a CharacterSelector ekler", EditorStyles.label);
            GUILayout.Label("• Character prefab'ları otomatik bulur ve atar", EditorStyles.label);
            GUILayout.Label("• Spawn point'ler oluşturur", EditorStyles.label);
            GUILayout.Label("• Tag'leri otomatik oluşturur", EditorStyles.label);
            GUILayout.Space(10);
            
            // Character Prefabs Section
            GUILayout.Label("Character Prefabs", EditorStyles.boldLabel);
            maleCharacterPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Male Character Prefab", maleCharacterPrefab, typeof(GameObject), false);
            femaleCharacterPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Female Character Prefab", femaleCharacterPrefab, typeof(GameObject), false);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Auto-Find Character Prefabs", GUILayout.Height(30)))
            {
                AutoFindCharacterPrefabs();
            }
            
            GUILayout.Space(10);
            
            // Spawn Point Settings
            GUILayout.Label("Spawn Point Settings", EditorStyles.boldLabel);
            spawnPointCount = EditorGUILayout.IntField("Spawn Point Count", spawnPointCount);
            spawnPointRadius = EditorGUILayout.FloatField("Spawn Radius", spawnPointRadius);
            spawnCenter = EditorGUILayout.Vector3Field("Spawn Center", spawnCenter);
            
            GUILayout.Space(10);
            
            // Main Actions
            if (GUILayout.Button("🎭 Setup Player Prefab", GUILayout.Height(30)))
            {
                SetupPlayerPrefab();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("📍 Create Spawn Points", GUILayout.Height(30)))
            {
                CreateSpawnPoints();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("🏷️ Create Required Tags", GUILayout.Height(30)))
            {
                CreateRequiredTags();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🚀 Complete Setup (All Steps)", GUILayout.Height(40)))
            {
                CompleteSetup();
            }
            
            GUILayout.Space(10);
            
            // Status
            GUILayout.Label("Status:", EditorStyles.boldLabel);
            CheckSetupStatus();
        }
        
        private void AutoFindCharacterPrefabs()
        {
            Debug.Log("🔍 Auto-finding character prefabs...");
            
            // Search for male character
            string[] maleGuids = AssetDatabase.FindAssets("MaleCharacterPBR t:GameObject");
            if (maleGuids.Length > 0)
            {
                string malePath = AssetDatabase.GUIDToAssetPath(maleGuids[0]);
                maleCharacterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(malePath);
                Debug.Log($"✅ Found Male Character: {malePath}");
            }
            else
            {
                Debug.LogWarning("⚠️ Male Character Prefab not found!");
            }
            
            // Search for female character
            string[] femaleGuids = AssetDatabase.FindAssets("FemaleCharacterPBR t:GameObject");
            if (femaleGuids.Length > 0)
            {
                string femalePath = AssetDatabase.GUIDToAssetPath(femaleGuids[0]);
                femaleCharacterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(femalePath);
                Debug.Log($"✅ Found Female Character: {femalePath}");
            }
            else
            {
                Debug.LogWarning("⚠️ Female Character Prefab not found!");
            }
            
            EditorUtility.SetDirty(this);
        }
        
        private void SetupPlayerPrefab()
        {
            Debug.Log("🎭 Setting up Player Prefab...");
            
            // Load Player prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found at Assets/Prefabs/Player.prefab");
                return;
            }
            
            // Open prefab for editing
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            
            // Check if CharacterSelector already exists
            CharacterSelector existingSelector = prefabInstance.GetComponent<CharacterSelector>();
            if (existingSelector != null)
            {
                Debug.Log("✅ CharacterSelector already exists, updating...");
            }
            else
            {
                // Add CharacterSelector component
                existingSelector = prefabInstance.AddComponent<CharacterSelector>();
                Debug.Log("✅ Added CharacterSelector component");
            }
            
            // Assign character prefabs using reflection
            if (maleCharacterPrefab != null)
            {
                SetPrivateField(existingSelector, "maleCharacterPrefab", maleCharacterPrefab);
                Debug.Log($"✅ Assigned Male Character: {maleCharacterPrefab.name}");
            }
            
            if (femaleCharacterPrefab != null)
            {
                SetPrivateField(existingSelector, "femaleCharacterPrefab", femaleCharacterPrefab);
                Debug.Log($"✅ Assigned Female Character: {femaleCharacterPrefab.name}");
            }
            
            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets/Prefabs/Player.prefab");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log("✅ Player Prefab setup complete!");
        }
        
        private void CreateSpawnPoints()
        {
            Debug.Log("📍 Creating spawn points...");
            
            // Create SpawnPoint tag if it doesn't exist
            CreateTag("SpawnPoint");
            
            // Remove existing spawn points (with safe tag check)
            try
            {
                GameObject[] existingSpawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
                foreach (GameObject spawn in existingSpawns)
                {
                    DestroyImmediate(spawn);
                }
            }
            catch (UnityException)
            {
                Debug.Log("ℹ️ No existing spawn points to remove (tag not created yet)");
            }
            
            // Create new spawn points
            for (int i = 0; i < spawnPointCount; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
                spawnPoint.tag = "SpawnPoint";
                
                // Position in circle around center
                float angle = (360f / spawnPointCount) * i;
                Vector3 position = spawnCenter + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * spawnPointRadius,
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * spawnPointRadius
                );
                
                spawnPoint.transform.position = position;
                
                // Add visual indicator
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual";
                visual.transform.SetParent(spawnPoint.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = new Vector3(2f, 0.1f, 2f);
                
                // Remove collider
                Collider collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyImmediate(collider);
                }
                
                // Set material color
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material spawnMaterial = new Material(Shader.Find("Standard"));
                    spawnMaterial.color = new Color(0, 1, 0, 0.5f); // Green
                    renderer.material = spawnMaterial;
                }
                
                Debug.Log($"✅ Created SpawnPoint_{i + 1} at {position}");
            }
            
            Debug.Log($"✅ Created {spawnPointCount} spawn points!");
        }
        
        private void CreateRequiredTags()
        {
            Debug.Log("🏷️ Creating required tags...");
            
            string[] requiredTags = { "SpawnPoint", "Player", "Enemy", "Structure", "Ground" };
            
            foreach (string tag in requiredTags)
            {
                CreateTag(tag);
            }
            
            Debug.Log("✅ Required tags created!");
        }
        
        private void CreateTag(string tagName)
        {
            // Check if tag already exists
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    tagExists = true;
                    break;
                }
            }
            
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
                newTagProp.stringValue = tagName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"✅ Created tag: {tagName}");
            }
            else
            {
                Debug.Log($"ℹ️ Tag already exists: {tagName}");
            }
        }
        
        private void CompleteSetup()
        {
            Debug.Log("🚀 Starting complete character setup...");
            
            try
            {
                // Step 1: Create tags
                CreateRequiredTags();
                
                // Step 2: Auto-find character prefabs
                AutoFindCharacterPrefabs();
                
                // Step 3: Setup player prefab
                SetupPlayerPrefab();
                
                // Step 4: Create spawn points
                CreateSpawnPoints();
                
                Debug.Log("🎉 Complete character setup finished successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Setup failed: {e.Message}");
            }
        }
        
        private void CheckSetupStatus()
        {
            // Check Player prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                CharacterSelector selector = playerPrefab.GetComponent<CharacterSelector>();
                if (selector != null)
                {
                    GUILayout.Label("✅ Player Prefab: CharacterSelector added", EditorStyles.label);
                }
                else
                {
                    GUILayout.Label("❌ Player Prefab: CharacterSelector missing", EditorStyles.label);
                }
            }
            else
            {
                GUILayout.Label("❌ Player Prefab: Not found", EditorStyles.label);
            }
            
            // Check spawn points (with safe tag check)
            try
            {
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
                GUILayout.Label($"📍 Spawn Points: {spawnPoints.Length} found", EditorStyles.label);
            }
            catch (UnityException)
            {
                GUILayout.Label("❌ Spawn Points: SpawnPoint tag not created", EditorStyles.label);
            }
            
            // Check character prefabs
            if (maleCharacterPrefab != null)
            {
                GUILayout.Label($"✅ Male Character: {maleCharacterPrefab.name}", EditorStyles.label);
            }
            else
            {
                GUILayout.Label("❌ Male Character: Not assigned", EditorStyles.label);
            }
            
            if (femaleCharacterPrefab != null)
            {
                GUILayout.Label($"✅ Female Character: {femaleCharacterPrefab.name}", EditorStyles.label);
            }
            else
            {
                GUILayout.Label("❌ Female Character: Not assigned", EditorStyles.label);
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
                Debug.LogWarning($"⚠️ Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }
    }
}
