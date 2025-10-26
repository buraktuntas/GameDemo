using UnityEngine;
using UnityEditor;
using System.IO;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Helper tool to setup Low Poly Environment in scene
    /// </summary>
    public class EnvironmentSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Environment Setup")]
        public static void ShowWindow()
        {
            GetWindow<EnvironmentSetup>("Environment Setup");
        }

        private Vector3 environmentPosition = Vector3.zero;
        private float environmentScale = 1f;

        private void OnGUI()
        {
            GUILayout.Label("🌳 Low Poly Environment Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool Low Poly Environment asset'ini scene'e ekler.\n\n" +
                "Özellikler:\n" +
                "• Automatic prefab instantiation\n" +
                "• Collision setup\n" +
                "• URP material upgrade\n" +
                "• Optimized for multiplayer",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Position settings
            GUILayout.Label("Environment Position:", EditorStyles.boldLabel);
            environmentPosition = EditorGUILayout.Vector3Field("Position", environmentPosition);

            GUILayout.Space(5);

            // Scale settings
            GUILayout.Label("Environment Scale:", EditorStyles.boldLabel);
            environmentScale = EditorGUILayout.Slider("Scale", environmentScale, 0.1f, 10f);

            GUILayout.Space(10);

            if (GUILayout.Button("Add Low Poly Environment to Scene", GUILayout.Height(40)))
            {
                AddEnvironmentToScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Collision for Environment Objects", GUILayout.Height(30)))
            {
                SetupCollisions();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Upgrade Environment Materials to URP", GUILayout.Height(30)))
            {
                UpgradeEnvironmentMaterials();
            }

            GUILayout.Space(10);

            GUILayout.Label("Zemin (Ground/Terrain):", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Ground Plane (100x100)", GUILayout.Height(30)))
            {
                CreateGroundPlane(100f);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Add Terrain (500x500)", GUILayout.Height(30)))
            {
                CreateTerrain(500f);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Load Demo Scene (Level Design)", GUILayout.Height(30)))
            {
                LoadDemoScene();
            }
        }

        private void AddEnvironmentToScene()
        {
            // Find the Low Poly Environment FBX
            string[] assetPaths = new string[]
            {
                "Assets/Skyden_Games/Low Poly Environment/Low Poly Environment.fbx",
                "Assets/Low Poly Environment/Low Poly Environment.fbx",
                "Assets/Environment/Low Poly Environment.fbx"
            };

            GameObject environmentPrefab = null;

            foreach (string path in assetPaths)
            {
                environmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (environmentPrefab != null)
                {
                    Debug.Log($"✅ Found environment at: {path}");
                    break;
                }
            }

            if (environmentPrefab == null)
            {
                EditorUtility.DisplayDialog("Asset Not Found",
                    "Low Poly Environment asset bulunamadı!\n\n" +
                    "Lütfen asset'i Unity'ye import edin:\n" +
                    "Assets/Skyden_Games/Low Poly Environment/",
                    "OK");
                return;
            }

            // Check if already in scene
            if (GameObject.Find("Low Poly Environment") != null)
            {
                bool replace = EditorUtility.DisplayDialog("Environment Exists",
                    "Scene'de zaten Low Poly Environment var.\n\nYenisiyle değiştirmek ister misin?",
                    "Evet, değiştir",
                    "Hayır");

                if (replace)
                {
                    DestroyImmediate(GameObject.Find("Low Poly Environment"));
                }
                else
                {
                    return;
                }
            }

            // Instantiate environment
            GameObject environment = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);
            environment.name = "Low Poly Environment";
            environment.transform.position = environmentPosition;
            environment.transform.localScale = Vector3.one * environmentScale;

            // Setup layers and tags
            SetupLayersAndTags(environment);

            // Setup colliders
            SetupColliders(environment);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog("Environment Added",
                $"Low Poly Environment scene'e eklendi!\n\n" +
                $"Position: {environmentPosition}\n" +
                $"Scale: {environmentScale}x\n\n" +
                "Next steps:\n" +
                "1. Setup Collision butonuna bas\n" +
                "2. Upgrade Materials butonuna bas",
                "OK");

            Debug.Log("✅ [EnvironmentSetup] Low Poly Environment added to scene");

            // Select the environment
            Selection.activeGameObject = environment;
        }

        private void SetupLayersAndTags(GameObject environment)
        {
            // Set layer to "Default" or create "Environment" layer
            if (LayerMask.NameToLayer("Environment") != -1)
            {
                SetLayerRecursively(environment, LayerMask.NameToLayer("Environment"));
            }
            else
            {
                SetLayerRecursively(environment, LayerMask.NameToLayer("Default"));
            }

            // Add tag
            if (!TagExists("Environment"))
            {
                AddTag("Environment");
            }

            environment.tag = "Environment";
        }

        private void SetupColliders(GameObject environment)
        {
            int collidersAdded = 0;

            // Find all mesh renderers
            MeshRenderer[] renderers = environment.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                GameObject obj = renderer.gameObject;

                // Skip if already has collider
                if (obj.GetComponent<Collider>() != null)
                    continue;

                // Add MeshCollider for static objects
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshCollider collider = obj.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilter.sharedMesh;
                    collider.convex = false; // Static collision
                    collidersAdded++;
                }
            }

            Debug.Log($"✅ [EnvironmentSetup] Added {collidersAdded} colliders");
        }

        private void SetupCollisions()
        {
            GameObject environment = GameObject.Find("Low Poly Environment");
            if (environment == null)
            {
                EditorUtility.DisplayDialog("Environment Not Found",
                    "Scene'de 'Low Poly Environment' bulunamadı!\n\nÖnce environment'i ekle.",
                    "OK");
                return;
            }

            SetupColliders(environment);

            EditorUtility.DisplayDialog("Collisions Setup",
                "Environment collision'ları ayarlandı!",
                "OK");
        }

        private void UpgradeEnvironmentMaterials()
        {
            GameObject environment = GameObject.Find("Low Poly Environment");
            if (environment == null)
            {
                EditorUtility.DisplayDialog("Environment Not Found",
                    "Scene'de 'Low Poly Environment' bulunamadı!",
                    "OK");
                return;
            }

            int upgraded = 0;
            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat != null && UpgradeMaterialToURP(mat))
                        {
                            EditorUtility.SetDirty(mat);
                            upgraded++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Materials Upgraded",
                $"{upgraded} materials upgraded to URP!\n\nEnvironment artık düzgün görünecek.",
                "OK");

            Debug.Log($"✅ [EnvironmentSetup] Upgraded {upgraded} materials");
        }

        private bool UpgradeMaterialToURP(Material mat)
        {
            if (mat == null) return false;

            Shader currentShader = mat.shader;
            if (currentShader == null) return false;

            // Skip if already URP
            if (currentShader.name.Contains("Universal Render Pipeline") || currentShader.name.Contains("URP"))
                return false;

            // Get URP shader
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) return false;

            // Store properties
            Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.mainTexture : null;

            // Upgrade
            mat.shader = urpShader;

            // Restore properties
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_BaseMap") && mainTex != null)
                mat.SetTexture("_BaseMap", mainTex);

            return true;
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private bool TagExists(string tag)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag)) return true;
            }
            return false;
        }

        private void AddTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Add tag
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
            n.stringValue = tag;
            tagManager.ApplyModifiedProperties();

            Debug.Log($"✅ Added tag: {tag}");
        }

        private void CreateGroundPlane(float size)
        {
            // Check if ground already exists
            if (GameObject.Find("Ground") != null)
            {
                bool replace = EditorUtility.DisplayDialog("Ground Exists",
                    "Scene'de zaten 'Ground' var.\n\nYenisiyle değiştirmek ister misin?",
                    "Evet",
                    "Hayır");

                if (replace)
                {
                    DestroyImmediate(GameObject.Find("Ground"));
                }
                else
                {
                    return;
                }
            }

            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, 0, 0);
            ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f); // Plane default is 10x10

            // Setup material
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMat.color = new Color(0.3f, 0.5f, 0.3f); // Green grass color
                renderer.sharedMaterial = groundMat;
            }

            // Add tag
            if (!TagExists("Ground"))
            {
                AddTag("Ground");
            }
            ground.tag = "Ground";

            // Set layer
            ground.layer = LayerMask.NameToLayer("Default");

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = ground;

            EditorUtility.DisplayDialog("Ground Added",
                $"Ground plane ({size}x{size}) eklendi!\n\n" +
                "Position: (0, 0, 0)\n" +
                "Color: Green grass",
                "OK");

            Debug.Log($"✅ [EnvironmentSetup] Ground plane created ({size}x{size})");
        }

        private void CreateTerrain(float size)
        {
            // Check if terrain already exists
            if (Terrain.activeTerrain != null)
            {
                bool replace = EditorUtility.DisplayDialog("Terrain Exists",
                    "Scene'de zaten Terrain var.\n\nYenisiyle değiştirmek ister misin?",
                    "Evet",
                    "Hayır");

                if (replace)
                {
                    DestroyImmediate(Terrain.activeTerrain.gameObject);
                }
                else
                {
                    return;
                }
            }

            // Create terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(size, 50, size);
            terrainData.SetHeights(0, 0, new float[513, 513]); // Flat terrain

            // Create terrain GameObject
            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "Terrain";
            terrainObj.transform.position = new Vector3(-size / 2f, 0, -size / 2f); // Center at origin

            Terrain terrain = terrainObj.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.materialTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            }

            // Add tag
            if (!TagExists("Ground"))
            {
                AddTag("Ground");
            }
            terrainObj.tag = "Ground";

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = terrainObj;

            EditorUtility.DisplayDialog("Terrain Added",
                $"Terrain ({size}x{size}) eklendi!\n\n" +
                "Position: Centered at origin\n" +
                "Height: 50 units\n\n" +
                "Terrain tools ile şekillendirebilirsin!",
                "OK");

            Debug.Log($"✅ [EnvironmentSetup] Terrain created ({size}x{size})");
        }

        private void LoadDemoScene()
        {
            string scenePath = "Assets/Skyden_Games/Low Poly Environment/Scenes/Level Design.unity";

            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Demo Scene Not Found",
                    "Demo scene bulunamadı:\n" + scenePath,
                    "OK");
                return;
            }

            bool load = EditorUtility.DisplayDialog("Load Demo Scene",
                "Demo scene'i yüklemek mevcut scene'i kapatacak.\n\n" +
                "Devam etmek istiyor musun?\n\n" +
                "Not: Mevcut scene'i kaydetmeyi unutma!",
                "Evet, yükle",
                "Hayır");

            if (load)
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                    Debug.Log("✅ [EnvironmentSetup] Demo scene loaded");
                }
            }
        }
    }
}
