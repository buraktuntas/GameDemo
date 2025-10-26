using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Helper tool to setup RPG Tiny Hero Duo characters
    /// </summary>
    public class CharacterSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Character Setup")]
        public static void ShowWindow()
        {
            GetWindow<CharacterSetup>("Character Setup");
        }

        private enum CharacterGender
        {
            Male,
            Female
        }

        private enum CharacterStyle
        {
            Polyart,
            PBR
        }

        private CharacterGender selectedGender = CharacterGender.Male;
        private CharacterStyle selectedStyle = CharacterStyle.Polyart;
        private bool replaceExistingPlayer = false;
        private bool setupNetworking = true;
        private bool setupCollision = true;
        private bool upgradeToURP = true;

        private void OnGUI()
        {
            GUILayout.Label("⚔️ RPG Tiny Hero Duo - Character Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool RPG Tiny Hero Duo karakterlerini oyuna ekler.\n\n" +
                "Özellikler:\n" +
                "• Male/Female karakter seçimi\n" +
                "• Polyart (Low Poly) veya PBR (Realistic) stil\n" +
                "• Network component setup (Mirror)\n" +
                "• Collision ve physics setup\n" +
                "• URP material upgrade\n" +
                "• Player prefab replacement (opsiyonel)",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Character selection
            GUILayout.Label("Karakter Seçimi:", EditorStyles.boldLabel);
            selectedGender = (CharacterGender)EditorGUILayout.EnumPopup("Gender", selectedGender);
            selectedStyle = (CharacterStyle)EditorGUILayout.EnumPopup("Style", selectedStyle);

            GUILayout.Space(10);

            // Setup options
            GUILayout.Label("Setup Options:", EditorStyles.boldLabel);
            setupNetworking = EditorGUILayout.Toggle("Setup Mirror Networking", setupNetworking);
            setupCollision = EditorGUILayout.Toggle("Setup Collision/Physics", setupCollision);
            upgradeToURP = EditorGUILayout.Toggle("Upgrade Materials to URP", upgradeToURP);
            replaceExistingPlayer = EditorGUILayout.Toggle("Replace Player Prefab", replaceExistingPlayer);

            GUILayout.Space(10);

            if (GUILayout.Button("📦 Add Character to Scene", GUILayout.Height(40)))
            {
                AddCharacterToScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("🔧 Setup Existing Character in Scene", GUILayout.Height(30)))
            {
                SetupExistingCharacter();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("🔄 Upgrade All Character Materials to URP", GUILayout.Height(30)))
            {
                UpgradeAllCharacterMaterials();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🎮 Load Demo Scene", GUILayout.Height(30)))
            {
                LoadDemoScene();
            }
        }

        private void AddCharacterToScene()
        {
            // Get prefab path
            string prefabName = GetPrefabName();
            string prefabPath = $"Assets/RPG Tiny Hero Duo/Prefab/{prefabName}.prefab";

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (characterPrefab == null)
            {
                EditorUtility.DisplayDialog("Character Not Found",
                    $"Character prefab bulunamadı:\n{prefabPath}\n\n" +
                    "Lütfen RPG Tiny Hero Duo asset'ini doğru import ettiğinizden emin olun.",
                    "OK");
                return;
            }

            // Instantiate character
            GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
            character.name = $"TinyHero_{selectedGender}_{selectedStyle}";
            character.transform.position = Vector3.zero;

            // Setup components
            if (setupCollision)
            {
                SetupCharacterCollision(character);
            }

            if (setupNetworking)
            {
                SetupCharacterNetworking(character);
            }

            if (upgradeToURP)
            {
                UpgradeCharacterMaterials(character);
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            // Select character
            Selection.activeGameObject = character;

            EditorUtility.DisplayDialog("Character Added",
                $"{prefabName} scene'e eklendi!\n\n" +
                $"Position: (0, 0, 0)\n" +
                $"Networking: {(setupNetworking ? "✅" : "❌")}\n" +
                $"Collision: {(setupCollision ? "✅" : "❌")}\n" +
                $"URP Materials: {(upgradeToURP ? "✅" : "❌")}",
                "OK");

            Debug.Log($"✅ [CharacterSetup] Character added: {prefabName}");

            // Replace player prefab if requested
            if (replaceExistingPlayer)
            {
                ReplacePlayerPrefab(character);
            }
        }

        private void SetupExistingCharacter()
        {
            GameObject character = Selection.activeGameObject;

            if (character == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Lütfen Hierarchy'den bir karakter seçin!",
                    "OK");
                return;
            }

            // Setup components
            if (setupCollision)
            {
                SetupCharacterCollision(character);
            }

            if (setupNetworking)
            {
                SetupCharacterNetworking(character);
            }

            if (upgradeToURP)
            {
                UpgradeCharacterMaterials(character);
            }

            EditorUtility.DisplayDialog("Character Setup Complete",
                $"{character.name} başarıyla setup edildi!\n\n" +
                $"Networking: {(setupNetworking ? "✅" : "❌")}\n" +
                $"Collision: {(setupCollision ? "✅" : "❌")}\n" +
                $"URP Materials: {(upgradeToURP ? "✅" : "❌")}",
                "OK");

            Debug.Log($"✅ [CharacterSetup] Character setup complete: {character.name}");
        }

        private void SetupCharacterCollision(GameObject character)
        {
            // Add CharacterController or CapsuleCollider
            CharacterController controller = character.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = character.AddComponent<CharacterController>();
                controller.center = new Vector3(0, 0.9f, 0);
                controller.radius = 0.3f;
                controller.height = 1.8f;
                Debug.Log($"✅ [CharacterSetup] Added CharacterController");
            }

            // Add Rigidbody for physics
            Rigidbody rb = character.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = character.AddComponent<Rigidbody>();
                rb.mass = 70f; // 70kg
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Debug.Log($"✅ [CharacterSetup] Added Rigidbody");
            }

            // Add CapsuleCollider as backup
            CapsuleCollider capsule = character.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = character.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, 0.9f, 0);
                capsule.radius = 0.3f;
                capsule.height = 1.8f;
                Debug.Log($"✅ [CharacterSetup] Added CapsuleCollider");
            }
        }

        private void SetupCharacterNetworking(GameObject character)
        {
            // Add NetworkIdentity
            NetworkIdentity netIdentity = character.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                netIdentity = character.AddComponent<NetworkIdentity>();
                Debug.Log($"✅ [CharacterSetup] Added NetworkIdentity");
            }

            // Add NetworkTransform for position sync
            var netTransform = character.GetComponent<Mirror.NetworkTransformReliable>();
            if (netTransform == null)
            {
                netTransform = character.AddComponent<Mirror.NetworkTransformReliable>();
                Debug.Log($"✅ [CharacterSetup] Added NetworkTransformReliable");
            }

            // Add NetworkAnimator if Animator exists
            Animator animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                NetworkAnimator netAnimator = character.GetComponent<NetworkAnimator>();
                if (netAnimator == null)
                {
                    netAnimator = character.AddComponent<NetworkAnimator>();
                    netAnimator.animator = animator;
                    Debug.Log($"✅ [CharacterSetup] Added NetworkAnimator");
                }
            }

            // Try to add PlayerController script if exists
            var playerController = character.GetComponent<TacticalCombat.Player.PlayerController>();
            if (playerController == null)
            {
                // Check if script exists
                var scripts = AssetDatabase.FindAssets("t:Script PlayerController");
                if (scripts.Length > 0)
                {
                    playerController = character.AddComponent<TacticalCombat.Player.PlayerController>();
                    Debug.Log($"✅ [CharacterSetup] Added PlayerController");
                }
            }
        }

        private void UpgradeCharacterMaterials(GameObject character)
        {
            int upgraded = 0;
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                Debug.LogWarning("⚠️ [CharacterSetup] URP shader not found!");
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material mat = materials[i];
                        if (mat != null && !mat.shader.name.Contains("Universal Render Pipeline"))
                        {
                            // Store current properties
                            Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                            Texture mainTex = mat.HasProperty("_MainTex") ? mat.mainTexture : null;

                            // Upgrade shader
                            mat.shader = urpShader;

                            // Restore properties
                            if (mat.HasProperty("_BaseColor"))
                                mat.SetColor("_BaseColor", color);

                            if (mat.HasProperty("_BaseMap") && mainTex != null)
                                mat.SetTexture("_BaseMap", mainTex);

                            EditorUtility.SetDirty(mat);
                            changed = true;
                            upgraded++;
                        }
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }
            }

            if (upgraded > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"✅ [CharacterSetup] Upgraded {upgraded} materials to URP");
            }
        }

        private void UpgradeAllCharacterMaterials()
        {
            string[] materialPaths = new string[]
            {
                "Assets/RPG Tiny Hero Duo/Material/Polyart_Default.mat",
                "Assets/RPG Tiny Hero Duo/Material/PBR_Default.mat"
            };

            int upgraded = 0;
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpShader == null)
            {
                EditorUtility.DisplayDialog("URP Not Found",
                    "Universal Render Pipeline shader bulunamadı!\n\nProje URP mi kullanıyor?",
                    "OK");
                return;
            }

            foreach (string path in materialPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !mat.shader.name.Contains("Universal Render Pipeline"))
                {
                    // Store properties
                    Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.mainTexture : null;

                    // Upgrade
                    mat.shader = urpShader;

                    // Restore
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", color);

                    if (mat.HasProperty("_BaseMap") && mainTex != null)
                        mat.SetTexture("_BaseMap", mainTex);

                    EditorUtility.SetDirty(mat);
                    upgraded++;

                    Debug.Log($"✅ Upgraded: {mat.name}");
                }
            }

            if (upgraded > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Materials Upgraded",
                    $"{upgraded} character materials upgraded to URP!",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Upgrade Needed",
                    "Character materials zaten URP kullanıyor!",
                    "OK");
            }
        }

        private void ReplacePlayerPrefab(GameObject character)
        {
            string playerPrefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);

            if (playerPrefab == null)
            {
                Debug.LogWarning("⚠️ [CharacterSetup] Player prefab not found at: " + playerPrefabPath);
                return;
            }

            bool replace = EditorUtility.DisplayDialog("Replace Player Prefab",
                $"Player prefab'ı bu karakterle değiştirmek istiyor musun?\n\n" +
                $"Mevcut: {playerPrefab.name}\n" +
                $"Yeni: {character.name}\n\n" +
                "Not: Bu NetworkManager'daki player prefab referansını güncelleyecek.",
                "Evet, değiştir",
                "Hayır");

            if (!replace) return;

            // Find NetworkManager in scene
            var networkManager = FindFirstObjectByType<Mirror.NetworkManager>();
            if (networkManager != null)
            {
                // Save character as prefab
                string newPrefabPath = $"Assets/Prefabs/Player_{selectedGender}_{selectedStyle}.prefab";
                GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(character, newPrefabPath);

                // Update NetworkManager
                SerializedObject so = new SerializedObject(networkManager);
                SerializedProperty playerPrefabProp = so.FindProperty("playerPrefab");
                playerPrefabProp.objectReferenceValue = newPrefab;
                so.ApplyModifiedProperties();

                EditorUtility.DisplayDialog("Player Prefab Replaced",
                    $"Player prefab başarıyla değiştirildi!\n\n" +
                    $"Yeni prefab: {newPrefabPath}\n" +
                    $"NetworkManager güncellendi!",
                    "OK");

                Debug.Log($"✅ [CharacterSetup] Player prefab replaced: {newPrefabPath}");
            }
            else
            {
                Debug.LogWarning("⚠️ [CharacterSetup] NetworkManager not found in scene!");
            }
        }

        private void LoadDemoScene()
        {
            // Try different demo scene paths
            string[] scenePaths = new string[]
            {
                "Assets/RPG Tiny Hero Duo/Scene/PolyartScene.unity",
                "Assets/RPG Tiny Hero Duo/Scene/PBRScene.unity",
                "Assets/RPG Tiny Hero Duo/Scene/AnimationLayer.unity"
            };

            string sceneToLoad = null;
            foreach (string path in scenePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    sceneToLoad = path;
                    break;
                }
            }

            if (sceneToLoad == null)
            {
                EditorUtility.DisplayDialog("Demo Scene Not Found",
                    "RPG Tiny Hero Duo demo scene bulunamadı!",
                    "OK");
                return;
            }

            bool load = EditorUtility.DisplayDialog("Load Demo Scene",
                $"Demo scene'i yüklemek mevcut scene'i kapatacak.\n\n" +
                $"Scene: {System.IO.Path.GetFileName(sceneToLoad)}\n\n" +
                "Devam etmek istiyor musun?\n\n" +
                "Not: Mevcut scene'i kaydetmeyi unutma!",
                "Evet, yükle",
                "Hayır");

            if (load)
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sceneToLoad);
                    Debug.Log($"✅ [CharacterSetup] Demo scene loaded: {sceneToLoad}");
                }
            }
        }

        private string GetPrefabName()
        {
            string gender = selectedGender == CharacterGender.Male ? "Male" : "Female";
            string style = selectedStyle == CharacterStyle.Polyart ? "Polyart" : "PBR";
            return $"{gender}Character{style}";
        }
    }
}
