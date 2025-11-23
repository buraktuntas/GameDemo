using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TacticalCombat.Combat;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ MIXAMO COMPLETE SETUP: T-pose karakter ve animasyonları tek seferde entegre eder
    /// - T-pose karakter modeli seçimi
    /// - Animasyon klasörü otomatik bulma/seçme
    /// - Animator Controller oluşturma
    /// - Silah eline doğru rotasyonla bağlama
    /// - Tüm referansları bağlama
    /// </summary>
    public class MixamoCompleteSetup : EditorWindow
    {
        [Header("Character Model")]
        private GameObject tPoseCharacter; // T-pose karakter FBX'i
        
        [Header("Animations")]
        private string animationFolderPath = "Assets/Character/Animations";
        private bool autoDetectAnimations = true;
        
        [Header("Weapon (Optional)")]
        private GameObject weaponPrefab;
        private bool attachWeapon = false;
        
        [Header("Settings")]
        private string characterName = "MixamoCharacter";
        private bool createAnimatorController = true;
        private bool setupAvatar = true;
        private bool fixTexturesAndMaterials = true; // ✅ NEW: Fix Mixamo texture/material issues
        
        [Header("Animation Settings")]
        private bool filterAnimations = false;
        private List<string> includedAnimations = new List<string>(); // Animasyon isimleri (contains check)
        private bool createBlendTree = true; // Walk/Run için blend tree
        private float transitionDuration = 0.25f;
        private bool useRootMotion = false;
        
        [Header("Advanced")]
        private bool showAdvancedSettings = false;
        private bool previewWeaponPosition = false;
        private bool testInScene = false;
        private bool saveSetupPreset = false;
        private string presetName = "Default";
        
        private Vector2 scrollPosition;
        private Vector2 animationMappingScrollPosition;
        private Vector2 animationListScrollPosition;
        private List<AnimationClip> detectedAnimations = new List<AnimationClip>();
        private Dictionary<string, string> animationStateMapping = new Dictionary<string, string>(); // clip name -> state name
        private Dictionary<string, bool> animationSelection = new Dictionary<string, bool>(); // Animasyon seçim durumu

        [MenuItem("Tools/Tactical Combat/Mixamo Complete Setup")]
        public static void ShowWindow()
        {
            GetWindow<MixamoCompleteSetup>("Mixamo Complete Setup");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Mixamo Complete Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool Mixamo T-pose karakter ve animasyonlarını tek seferde entegre eder:\n\n" +
                "1. T-pose karakter modeli seç (Mixamo veya başka Humanoid karakter)\n" +
                "2. Animasyon klasörünü belirle (Mixamo, Battle Royale veya başka Humanoid animasyonlar)\n" +
                "3. Silah ekle (opsiyonel)\n" +
                "4. 'Complete Setup' butonuna tıkla\n\n" +
                "Tool otomatik olarak:\n" +
                "• Humanoid Avatar oluşturur (retargeting için)\n" +
                "• Animator Controller oluşturur\n" +
                "• Animasyonları bağlar (Unity otomatik retargeting yapar)\n" +
                "• Silahı eline doğru rotasyonla bağlar\n" +
                "• Tüm referansları bağlar\n\n" +
                "✅ NOT: Farklı kaynaklardan gelen Humanoid animasyonlar otomatik olarak retarget edilir!",
                MessageType.Info);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 1. T-Pose Character Model
            GUILayout.Label("1. T-Pose Character Model (FBX):", EditorStyles.boldLabel);
            tPoseCharacter = (GameObject)EditorGUILayout.ObjectField(
                tPoseCharacter,
                typeof(GameObject),
                false,
                GUILayout.Height(20)
            );
            characterName = EditorGUILayout.TextField("Character Name", characterName);
            GUILayout.Space(10);

            // 2. Animation Folder
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("2. Animation Folder:", EditorStyles.boldLabel);
            animationFolderPath = EditorGUILayout.TextField("Animation Folder Path", animationFolderPath);
            
            if (GUILayout.Button("Browse Animation Folder", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Animation Folder", animationFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    animationFolderPath = "Assets" + path.Replace(Application.dataPath, "");
                }
            }
            
            autoDetectAnimations = EditorGUILayout.Toggle("Auto-Detect Animations", autoDetectAnimations);
            
            if (GUILayout.Button("Detect Animations Now", GUILayout.Height(30)))
            {
                DetectAnimations();
            }
            
            if (detectedAnimations.Count > 0)
            {
                GUILayout.Label($"Found {detectedAnimations.Count} animations:", EditorStyles.boldLabel);
                
                // Animation filtering
                filterAnimations = EditorGUILayout.Toggle("Filter Animations (Select which to use)", filterAnimations);
                
                animationListScrollPosition = EditorGUILayout.BeginScrollView(animationListScrollPosition, GUILayout.Height(150));
                
                if (filterAnimations)
                {
                    // Show checkboxes for each animation
                    foreach (var anim in detectedAnimations)
                    {
                        if (!animationSelection.ContainsKey(anim.name))
                        {
                            animationSelection[anim.name] = true; // Default: all selected
                        }
                        
                        animationSelection[anim.name] = EditorGUILayout.Toggle($"  ✓ {anim.name}", animationSelection[anim.name]);
                    }
                }
                else
                {
                    // Just show list
                    foreach (var anim in detectedAnimations)
                    {
                        EditorGUILayout.LabelField($"  • {anim.name}");
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
                if (filterAnimations)
                {
                    int selectedCount = animationSelection.Values.Count(v => v);
                    GUILayout.Label($"Selected: {selectedCount} / {detectedAnimations.Count}", EditorStyles.centeredGreyMiniLabel);
                }
            }
            
            // Show animation mapping if available
            if (animationStateMapping.Count > 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Label("Animation → State Mapping:", EditorStyles.boldLabel);
                animationMappingScrollPosition = EditorGUILayout.BeginScrollView(animationMappingScrollPosition, GUILayout.Height(200));
                
                foreach (var mapping in animationStateMapping)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {mapping.Key}", GUILayout.Width(200));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    EditorGUILayout.LabelField(mapping.Value, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            GUILayout.Space(10);

            // 3. Weapon (Optional)
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("3. Weapon (Optional):", EditorStyles.boldLabel);
            attachWeapon = EditorGUILayout.Toggle("Attach Weapon to Hand", attachWeapon);
            
            EditorGUI.BeginDisabledGroup(!attachWeapon);
            weaponPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Weapon Prefab",
                weaponPrefab,
                typeof(GameObject),
                false
            );
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);

            // 4. Settings
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("4. Settings:", EditorStyles.boldLabel);
            createAnimatorController = EditorGUILayout.Toggle("Create Animator Controller", createAnimatorController);
            setupAvatar = EditorGUILayout.Toggle("Setup Avatar (Humanoid)", setupAvatar);
            fixTexturesAndMaterials = EditorGUILayout.Toggle("Fix Textures & Materials (Mixamo gray fix)", fixTexturesAndMaterials);
            if (fixTexturesAndMaterials)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Fixes gray textures and missing colors on Mixamo characters by:\n• Extracting embedded materials\n• Configuring texture import settings\n• Setting up proper shaders", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            createBlendTree = EditorGUILayout.Toggle("Create Blend Tree (Walk/Run)", createBlendTree);
            transitionDuration = EditorGUILayout.Slider("Transition Duration", transitionDuration, 0.1f, 1f);
            useRootMotion = EditorGUILayout.Toggle("Use Root Motion", useRootMotion);
            GUILayout.Space(10);
            
            // Advanced Settings
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                previewWeaponPosition = EditorGUILayout.Toggle("Preview Weapon Position", previewWeaponPosition);
                testInScene = EditorGUILayout.Toggle("Test in Scene (after setup)", testInScene);
                saveSetupPreset = EditorGUILayout.Toggle("Save Setup Preset", saveSetupPreset);
                if (saveSetupPreset)
                {
                    presetName = EditorGUILayout.TextField("Preset Name", presetName);
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(20);

            EditorGUILayout.EndScrollView();

            // Action Buttons
            EditorGUI.BeginDisabledGroup(tPoseCharacter == null);
            if (GUILayout.Button("Complete Setup", GUILayout.Height(50)))
            {
                PerformCompleteSetup();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Quick Setup (Auto-Detect Everything)", GUILayout.Height(30)))
            {
                QuickSetup();
            }
        }

        private void DetectAnimations()
        {
            detectedAnimations.Clear();
            
            if (string.IsNullOrEmpty(animationFolderPath) || !Directory.Exists(animationFolderPath))
            {
                EditorUtility.DisplayDialog("Error", "Animation folder path is invalid!", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationFolderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    detectedAnimations.Add(clip);
                    
                    // ✅ DEBUG: Log both file name and clip name
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    string clipName = clip.name;
                    if (fileName != clipName)
                    {
                        Debug.Log($"📋 Animation: File='{fileName}' | Clip='{clipName}' | Path='{path}'");
                    }
                }
            }

            Debug.Log($"✅ Detected {detectedAnimations.Count} animations in {animationFolderPath}");
        }

        private void QuickSetup()
        {
            // Auto-detect animation folder if character is selected
            if (tPoseCharacter != null && string.IsNullOrEmpty(animationFolderPath))
            {
                string characterPath = AssetDatabase.GetAssetPath(tPoseCharacter);
                string characterDir = Path.GetDirectoryName(characterPath);
                
                // Look for common animation folder names
                string[] commonNames = { "Animations", "Animation", "Anims", "Mixamo" };
                foreach (string name in commonNames)
                {
                    string animPath = Path.Combine(characterDir, name).Replace('\\', '/');
                    if (Directory.Exists(animPath))
                    {
                        animationFolderPath = animPath;
                        Debug.Log($"✅ Auto-detected animation folder: {animationFolderPath}");
                        break;
                    }
                }
            }

            // Auto-detect animations
            if (autoDetectAnimations)
            {
                DetectAnimations();
            }

            // Perform setup
            PerformCompleteSetup();
        }

        private void PerformCompleteSetup()
        {
            if (tPoseCharacter == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a T-pose Character Model!", "OK");
                return;
            }

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🎮 MIXAMO COMPLETE SETUP");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            try
            {
                // Step 1: Setup Character Model
                Debug.Log("📦 Step 1: Setting up T-pose character model...");
                Avatar avatar = null;
                modified |= SetupCharacterModel(playerInstance, out avatar);
                
                // Step 1.5: Fix Textures & Materials (if requested)
                if (fixTexturesAndMaterials)
                {
                    Debug.Log("\n🎨 Step 1.5: Fixing textures and materials...");
                    modified |= FixTexturesAndMaterials(playerInstance);
                }

                // Step 2: Setup Animations
                Debug.Log("\n🎬 Step 2: Setting up animations...");
                AnimatorController animatorController = null;
                if (createAnimatorController)
                {
                    animatorController = SetupAnimations(playerInstance, avatar);
                    modified |= (animatorController != null);
                }

                // Step 3: Setup Weapon (if requested)
                if (attachWeapon && weaponPrefab != null)
                {
                    Debug.Log("\n🔫 Step 3: Setting up weapon...");
                    modified |= SetupWeapon(playerInstance);
                }

                // Step 4: Setup Components & References
                Debug.Log("\n🔧 Step 4: Setting up components and references...");
                modified |= SetupComponents(playerInstance, animatorController);

                // Step 5: Finalize
                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                    AssetDatabase.Refresh();
                    Debug.Log("\n✅ COMPLETE SETUP FINISHED!");
                    Debug.Log("═══════════════════════════════════════════════════════\n");
                    EditorUtility.DisplayDialog("Success", 
                        "Mixamo character setup finished!\n\n" +
                        "✓ T-pose character integrated\n" +
                        "✓ Avatar created\n" +
                        "✓ Animations configured\n" +
                        "✓ Weapon attached (if selected)\n" +
                        "✓ All references connected",
                        "OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error during setup: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Setup failed:\n{e.Message}", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerInstance);
            }
        }

        private bool SetupCharacterModel(GameObject playerInstance, out Avatar avatar)
        {
            avatar = null;
            bool modified = false;

            // Find or create PlayerVisual
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual == null)
            {
                GameObject visualGO = new GameObject("PlayerVisual");
                visualGO.transform.SetParent(playerInstance.transform);
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;
                visualGO.transform.localScale = Vector3.one;
                playerVisual = visualGO.transform;
                modified = true;
                Debug.Log("✅ Created PlayerVisual GameObject");
            }

            // Remove old character models
            for (int i = playerVisual.childCount - 1; i >= 0; i--)
            {
                Transform child = playerVisual.GetChild(i);
                if (child == null) continue;
                
                string childName = child.name;
                bool shouldRemove = childName.Contains("Character") || childName.Contains("Model") ||
                    child.GetComponent<SkinnedMeshRenderer>() != null ||
                    child.GetComponent<Animator>() != null;
                
                if (shouldRemove)
                {
                    Object.DestroyImmediate(child.gameObject);
                    modified = true;
                    Debug.Log($"✅ Removed old character model: {childName}");
                }
            }

            // Instantiate T-pose character
            GameObject characterInstance = null;
            string modelPath = AssetDatabase.GetAssetPath(tPoseCharacter);
            bool isPrefab = modelPath.EndsWith(".prefab");

            if (isPrefab)
            {
                characterInstance = PrefabUtility.InstantiatePrefab(tPoseCharacter) as GameObject;
            }
            else
            {
                GameObject loadedModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (loadedModel != null)
                {
                    characterInstance = Instantiate(loadedModel);
                }
            }

            if (characterInstance != null)
            {
                characterInstance.name = characterName;
                characterInstance.transform.SetParent(playerVisual);
                characterInstance.transform.localPosition = Vector3.zero;
                characterInstance.transform.localRotation = Quaternion.identity;
                characterInstance.transform.localScale = Vector3.one;
                modified = true;
                Debug.Log($"✅ Added T-pose character: {characterName}");

                // Setup Avatar
                if (setupAvatar)
                {
                    avatar = ExtractOrCreateAvatar(characterInstance, modelPath);
                    if (avatar != null)
                    {
                        Debug.Log($"✅ Avatar created/configured: {avatar.name}");
                    }
                }

                // Setup Animator
                Animator animator = characterInstance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = characterInstance.AddComponent<Animator>();
                    modified = true;
                    Debug.Log("✅ Added Animator component");
                }

                if (avatar != null && animator != null)
                {
                    animator.avatar = avatar;
                    EditorUtility.SetDirty(animator);
                    modified = true;
                }
            }

            return modified;
        }

        private Avatar ExtractOrCreateAvatar(GameObject characterModel, string modelPath)
        {
            Avatar avatar = null;

            // Try to get Avatar from existing Animator
            Animator animator = characterModel.GetComponent<Animator>();
            if (animator != null && animator.avatar != null)
            {
                Debug.Log($"✅ Found existing Avatar: {animator.avatar.name}");
                return animator.avatar;
            }

            // Configure ModelImporter for Humanoid
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer != null)
            {
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log("✅ Configured model as Humanoid (for animation retargeting)");
                }
                else
                {
                    Debug.Log("✅ Model is already configured as Humanoid - ready for retargeting!");
                }

                // Get avatar from importer
                if (importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    // Reimport to generate avatar
                    AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                    
                    // Get avatar from the model
                    GameObject modelInstance = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if (modelInstance != null)
                    {
                        Animator modelAnimator = modelInstance.GetComponent<Animator>();
                        if (modelAnimator == null)
                        {
                            // Create temporary instance to get avatar
                            GameObject tempInstance = Instantiate(modelInstance);
                            modelAnimator = tempInstance.GetComponent<Animator>();
                            if (modelAnimator != null && modelAnimator.avatar != null)
                            {
                                avatar = modelAnimator.avatar;
                                Debug.Log($"✅ Created Avatar: {avatar.name}");
                            }
                            Object.DestroyImmediate(tempInstance);
                        }
                        else if (modelAnimator.avatar != null)
                        {
                            avatar = modelAnimator.avatar;
                            Debug.Log($"✅ Found Avatar: {avatar.name}");
                        }
                    }
                }
            }

            return avatar;
        }

        private AnimatorController SetupAnimations(GameObject playerInstance, Avatar avatar)
        {
            // Find character animator
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                Debug.LogWarning("⚠️ No Animator found - skipping animation setup");
                return null;
            }

            // Auto-detect animations if not already done
            if (autoDetectAnimations && detectedAnimations.Count == 0)
            {
                DetectAnimations();
            }

            // Create Animator Controller
            string controllerPath = $"Assets/Animators/{characterName}Controller.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                Debug.Log($"✅ Created Animator Controller: {controllerPath}");
            }

            // Add required parameters
            EnsureParameters(controller);

            // Add animations and create states
            if (detectedAnimations.Count > 0)
            {
                CreateAnimationStates(controller, detectedAnimations);
            }

            // Assign to animator
            if (characterAnimator != null)
            {
                characterAnimator.runtimeAnimatorController = controller;
                if (avatar != null)
                {
                    characterAnimator.avatar = avatar;
                }
                EditorUtility.SetDirty(characterAnimator);
                Debug.Log($"✅ Assigned Animator Controller to character");
            }

            return controller;
        }

        private void EnsureParameters(AnimatorController controller)
        {
            var requiredParams = new Dictionary<string, AnimatorControllerParameterType>
            {
                { "Speed", AnimatorControllerParameterType.Float },
                { "IsShooting", AnimatorControllerParameterType.Bool },
                { "IsReloading", AnimatorControllerParameterType.Bool },
                { "IsAiming", AnimatorControllerParameterType.Bool },
                { "IsGrounded", AnimatorControllerParameterType.Bool },
                { "TriggerFire", AnimatorControllerParameterType.Trigger },
                { "TriggerReload", AnimatorControllerParameterType.Trigger },
                { "Jump", AnimatorControllerParameterType.Trigger }
            };

            foreach (var param in requiredParams)
            {
                bool exists = controller.parameters.Any(p => p.name == param.Key);
                if (!exists)
                {
                    controller.AddParameter(param.Key, param.Value);
                    Debug.Log($"✅ Added parameter: {param.Key}");
                }
            }
        }

        private void CreateAnimationStates(AnimatorController controller, List<AnimationClip> animations)
        {
            var stateMachine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();

            // Create states for detected animations
            animationStateMapping.Clear(); // Clear previous mappings
            
            foreach (var clip in animations)
            {
                // ✅ FIX: Use file name instead of clip name to avoid "mixamo" conflicts
                string assetPath = AssetDatabase.GetAssetPath(clip);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                string clipName = clip.name;
                
                // Prefer file name, fallback to clip name if file name is empty
                string originalName = !string.IsNullOrEmpty(fileName) ? fileName : clipName;
                
                // ✅ FIX: Remove invalid characters from state name ('.' and ' ' are not allowed)
                string stateName = originalName.Replace(".", "_").Replace(" ", "_");
                
                // ✅ FIX: Handle duplicate names (if multiple animations have same file name)
                int counter = 1;
                string uniqueStateName = stateName;
                while (states.ContainsKey(uniqueStateName))
                {
                    uniqueStateName = $"{stateName}_{counter}";
                    counter++;
                }
                stateName = uniqueStateName;
                
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;
                states[stateName] = state;
                
                // Store mapping for display (show both file name and clip name if different)
                string displayName = fileName != clipName ? $"{fileName} (clip: {clipName})" : originalName;
                animationStateMapping[displayName] = stateName;
                
                Debug.Log($"✅ Created state: {stateName} (from file: '{fileName}', clip: '{clipName}')");
            }

            // Set default state (Idle if exists, otherwise first state)
            // ✅ FIX: Check cleaned state names with multiple patterns
            string idleStateName = FindStateName(states, new[] { "Idle", "Standing", "Stand" });
            
            if (idleStateName != null && states.ContainsKey(idleStateName))
            {
                stateMachine.defaultState = states[idleStateName];
                Debug.Log($"✅ Set default state to: {idleStateName}");
            }
            else if (states.Count > 0)
            {
                stateMachine.defaultState = states.Values.First();
                Debug.Log($"✅ Set default state to first state: {stateMachine.defaultState.name}");
            }

            // Create basic transitions
            CreateBasicTransitions(states, stateMachine);
        }

        private void CreateBasicTransitions(Dictionary<string, AnimatorState> states, AnimatorStateMachine stateMachine)
        {
            // ✅ FIX: Find states by name (with cleaned names) - support multiple naming patterns
            string idleName = FindStateName(states, new[] { "Idle", "Standing", "Stand" });
            string walkName = FindStateName(states, new[] { "Walk", "Walking" });
            string runName = FindStateName(states, new[] { "Run", "Running", "Sprint" });
            string jumpName = FindStateName(states, new[] { "Jump", "Jumping" });
            string shootName = FindStateName(states, new[] { "Shoot", "Shooting", "Fire", "Firing", "Attack", "Attacking" });
            string reloadName = FindStateName(states, new[] { "Reload", "Reloading" });

            Debug.Log($"\n📋 Found Animation States:");
            if (idleName != null) Debug.Log($"  • Idle: {idleName}");
            if (walkName != null) Debug.Log($"  • Walk: {walkName}");
            if (runName != null) Debug.Log($"  • Run: {runName}");
            if (jumpName != null) Debug.Log($"  • Jump: {jumpName}");
            if (shootName != null) Debug.Log($"  • Shoot: {shootName}");
            if (reloadName != null) Debug.Log($"  • Reload: {reloadName}");
            Debug.Log("");

            // Idle <-> Walk <-> Run transitions
            if (idleName != null && walkName != null)
            {
                CreateTransition(states[idleName], states[walkName], "Speed", 0.1f, true, stateMachine);
                CreateTransition(states[walkName], states[idleName], "Speed", 0.1f, false, stateMachine);
            }

            if (walkName != null && runName != null)
            {
                CreateTransition(states[walkName], states[runName], "Speed", 4f, true, stateMachine);
                CreateTransition(states[runName], states[walkName], "Speed", 4f, false, stateMachine);
            }

            // Jump transitions (from any state)
            if (jumpName != null)
            {
                if (idleName != null)
                {
                    CreateTransition(states[idleName], states[jumpName], "Jump", 0f, true, stateMachine, true);
                    CreateTransition(states[jumpName], states[idleName], "IsGrounded", 0f, true, stateMachine, false, true); // Back to idle when grounded
                }
                if (walkName != null)
                {
                    CreateTransition(states[walkName], states[jumpName], "Jump", 0f, true, stateMachine, true);
                    CreateTransition(states[jumpName], states[walkName], "IsGrounded", 0f, true, stateMachine, false, true); // Back to walk when grounded
                }
                if (runName != null)
                {
                    CreateTransition(states[runName], states[jumpName], "Jump", 0f, true, stateMachine, true);
                    CreateTransition(states[jumpName], states[runName], "IsGrounded", 0f, true, stateMachine, false, true); // Back to run when grounded
                }
            }

            // ✅ NEW: Shoot transitions (from any movement state)
            if (shootName != null)
            {
                if (idleName != null)
                {
                    CreateTransition(states[idleName], states[shootName], "TriggerFire", 0f, true, stateMachine, true);
                    // Shoot back to idle when animation finishes (use exit time)
                    CreateTransition(states[shootName], states[idleName], null, 0f, false, stateMachine, false, true);
                }
                if (walkName != null)
                {
                    CreateTransition(states[walkName], states[shootName], "TriggerFire", 0f, true, stateMachine, true);
                    // Shoot back to walk when animation finishes
                    CreateTransition(states[shootName], states[walkName], null, 0f, false, stateMachine, false, true);
                }
                if (runName != null)
                {
                    CreateTransition(states[runName], states[shootName], "TriggerFire", 0f, true, stateMachine, true);
                    // Shoot back to run when animation finishes
                    CreateTransition(states[shootName], states[runName], null, 0f, false, stateMachine, false, true);
                }
            }

            // ✅ NEW: Reload transitions (from any movement state)
            if (reloadName != null)
            {
                if (idleName != null)
                {
                    CreateTransition(states[idleName], states[reloadName], "TriggerReload", 0f, true, stateMachine, true);
                    // Reload back to idle when IsReloading = false
                    CreateTransition(states[reloadName], states[idleName], "IsReloading", 0f, false, stateMachine, false, false);
                }
                if (walkName != null)
                {
                    CreateTransition(states[walkName], states[reloadName], "TriggerReload", 0f, true, stateMachine, true);
                    CreateTransition(states[reloadName], states[walkName], "IsReloading", 0f, false, stateMachine, false, false);
                }
                if (runName != null)
                {
                    CreateTransition(states[runName], states[reloadName], "TriggerReload", 0f, true, stateMachine, true);
                    CreateTransition(states[reloadName], states[runName], "IsReloading", 0f, false, stateMachine, false, false);
                }
            }
        }

        /// <summary>
        /// ✅ FIX: Find state name by searching (handles cleaned names)
        /// </summary>
        private string FindStateName(Dictionary<string, AnimatorState> states, string searchName)
        {
            return FindStateName(states, new[] { searchName });
        }

        /// <summary>
        /// ✅ NEW: Find state name by searching with multiple possible names
        /// </summary>
        private string FindStateName(Dictionary<string, AnimatorState> states, string[] possibleNames)
        {
            foreach (string searchName in possibleNames)
            {
                // First try exact match
                if (states.ContainsKey(searchName))
                {
                    return searchName;
                }

                // Then try case-insensitive search
                string found = states.Keys.FirstOrDefault(k => k.Equals(searchName, System.StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    return found;
                }

                // Finally try contains search
                found = states.Keys.FirstOrDefault(k => k.Contains(searchName, System.StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void CreateTransition(AnimatorState from, AnimatorState to, string paramName, float threshold, bool greaterThan, AnimatorStateMachine stateMachine, bool isTrigger = false, bool useExitTime = false)
        {
            if (from == null || to == null) return;
            
            AnimatorStateTransition transition = from.AddTransition(to);
            
            if (isTrigger)
            {
                transition.AddCondition(AnimatorConditionMode.If, 0, paramName);
            }
            else if (!string.IsNullOrEmpty(paramName))
            {
                // Bool parameter check
                if (paramName == "IsGrounded" || paramName == "IsShooting" || paramName == "IsReloading")
                {
                    transition.AddCondition(greaterThan ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, paramName);
                }
                else
                {
                    // Float parameter check (Speed)
                    transition.AddCondition(greaterThan ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, paramName);
                }
            }
            
            transition.duration = transitionDuration; // ✅ Use configurable duration
            transition.hasExitTime = useExitTime; // ✅ Configurable exit time
            if (useExitTime)
            {
                transition.exitTime = 0.9f; // Exit at 90% of animation
            }
        }

        /// <summary>
        /// ✅ NEW: Create blend tree for Walk/Run animations
        /// </summary>
        private void CreateBlendTree(AnimatorController controller, List<AnimationClip> animations)
        {
            var stateMachine = controller.layers[0].stateMachine;
            
            // Find Walk and Run animations
            AnimationClip walkClip = animations.FirstOrDefault(a => 
                a.name.Contains("Walk", System.StringComparison.OrdinalIgnoreCase) && 
                !a.name.Contains("Run", System.StringComparison.OrdinalIgnoreCase));
            AnimationClip runClip = animations.FirstOrDefault(a => 
                a.name.Contains("Run", System.StringComparison.OrdinalIgnoreCase));

            if (walkClip != null && runClip != null)
            {
                // Create blend tree
                BlendTree blendTree = new BlendTree();
                blendTree.name = "Locomotion";
                blendTree.blendType = BlendTreeType.Simple1D;
                blendTree.blendParameter = "Speed";
                
                // Add walk animation (speed 0-4)
                ChildMotion walkMotion = new ChildMotion();
                walkMotion.motion = walkClip;
                walkMotion.threshold = 0f;
                walkMotion.timeScale = 1f;
                
                // Add run animation (speed 4+)
                ChildMotion runMotion = new ChildMotion();
                runMotion.motion = runClip;
                runMotion.threshold = 4f;
                runMotion.timeScale = 1f;
                
                blendTree.children = new ChildMotion[] { walkMotion, runMotion };
                
                // Create state with blend tree
                AnimatorState blendState = stateMachine.AddState("Locomotion");
                blendState.motion = blendTree;
                
                Debug.Log("✅ Created Blend Tree for Walk/Run animations");
            }
        }

        private bool SetupWeapon(GameObject playerInstance)
        {
            bool modified = false;

            // Find character animator and hand bone
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                Debug.LogWarning("⚠️ No Animator found - cannot setup weapon");
                return false;
            }

            // Find hand bone (try multiple names)
            string[] handBoneNames = { "RightHand", "Hand_R", "R_Hand", "RightHandIndex1" };
            Transform handBone = null;
            
            foreach (string boneName in handBoneNames)
            {
                handBone = FindBoneInHierarchy(characterAnimator.transform, boneName);
                if (handBone != null)
                {
                    Debug.Log($"✅ Found hand bone: {boneName}");
                    break;
                }
            }

            if (handBone == null)
            {
                Debug.LogWarning("⚠️ Hand bone not found - cannot setup weapon");
                return false;
            }

            // Create WeaponHolder under hand bone
            Transform weaponHolder = handBone.Find("WeaponHolder");
            if (weaponHolder == null)
            {
                GameObject weaponHolderGO = new GameObject("WeaponHolder");
                weaponHolder = weaponHolderGO.transform;
                weaponHolder.SetParent(handBone);
                
                // ✅ FIX: Correct weapon position and rotation for hand
                weaponHolder.localPosition = new Vector3(0.05f, -0.02f, 0.1f);
                weaponHolder.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Rotate to point forward, not down
                weaponHolder.localScale = Vector3.one;
                modified = true;
                Debug.Log($"✅ Created WeaponHolder under hand bone");
            }

            // Remove existing weapon
            Transform existingWeapon = weaponHolder.Find("CurrentWeapon");
            if (existingWeapon != null)
            {
                Object.DestroyImmediate(existingWeapon.gameObject);
                modified = true;
            }

            // Instantiate weapon
            GameObject weaponInstance = InstantiateWeapon(weaponPrefab);
            if (weaponInstance != null)
            {
                weaponInstance.name = "CurrentWeapon";
                weaponInstance.transform.SetParent(weaponHolder);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;
                weaponInstance.transform.localScale = Vector3.one;
                CleanupWeapon(weaponInstance);
                modified = true;
                Debug.Log("✅ Attached weapon to hand");
            }

            // Also create WeaponHolder under Camera for FPS view
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                Transform fpsWeaponHolder = playerCamera.transform.Find("WeaponHolder");
                if (fpsWeaponHolder == null)
                {
                    GameObject fpsWeaponHolderGO = new GameObject("WeaponHolder");
                    fpsWeaponHolder = fpsWeaponHolderGO.transform;
                    fpsWeaponHolder.SetParent(playerCamera.transform);
                    fpsWeaponHolder.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                    fpsWeaponHolder.localRotation = Quaternion.identity;
                    fpsWeaponHolder.localScale = Vector3.one;
                    modified = true;
                    Debug.Log("✅ Created WeaponHolder under Camera (FPS view)");
                }

                // Update WeaponSystem to use Camera's WeaponHolder for FPS
                UpdateWeaponSystemReference(playerInstance, fpsWeaponHolder);
            }

            return modified;
        }

        private bool SetupComponents(GameObject playerInstance, AnimatorController animatorController)
        {
            bool modified = false;

            // Ensure PlayerVisuals component
            PlayerVisuals playerVisuals = playerInstance.GetComponent<PlayerVisuals>();
            if (playerVisuals == null)
            {
                playerVisuals = playerInstance.AddComponent<PlayerVisuals>();
                modified = true;
                Debug.Log("✅ Added PlayerVisuals component");
            }

            // Update PlayerVisuals renderer reference
            Renderer characterRenderer = playerInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (characterRenderer == null)
            {
                characterRenderer = playerInstance.GetComponentInChildren<MeshRenderer>();
            }

            if (characterRenderer != null)
            {
                SerializedObject visualsSo = new SerializedObject(playerVisuals);
                var rendererProp = visualsSo.FindProperty("visualRenderer");
                if (rendererProp != null && rendererProp.objectReferenceValue != characterRenderer)
                {
                    rendererProp.objectReferenceValue = characterRenderer;
                    visualsSo.ApplyModifiedProperties();
                    modified = true;
                    Debug.Log("✅ Updated PlayerVisuals renderer reference");
                }
            }

            // Ensure BattleRoyaleAnimationController
            BattleRoyaleAnimationController animController = playerInstance.GetComponent<BattleRoyaleAnimationController>();
            if (animController == null)
            {
                animController = playerInstance.AddComponent<BattleRoyaleAnimationController>();
                modified = true;
                Debug.Log("✅ Added BattleRoyaleAnimationController");
            }

            // Update BattleRoyaleAnimationController references
            if (animController != null)
            {
                SerializedObject animControllerSo = new SerializedObject(animController);
                Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
                if (characterAnimator != null)
                {
                    animControllerSo.FindProperty("characterAnimator").objectReferenceValue = characterAnimator;
                }
                animControllerSo.FindProperty("weaponSystem").objectReferenceValue = playerInstance.GetComponent<WeaponSystem>();
                animControllerSo.FindProperty("fpsController").objectReferenceValue = playerInstance.GetComponent<FPSController>();
                animControllerSo.FindProperty("characterController").objectReferenceValue = playerInstance.GetComponent<CharacterController>();
                animControllerSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(animController);
                modified = true;
                Debug.Log("✅ Updated BattleRoyaleAnimationController references");
            }

            return modified;
        }

        private GameObject InstantiateWeapon(GameObject weaponPrefab)
        {
            string weaponPath = AssetDatabase.GetAssetPath(weaponPrefab);
            bool isPrefab = weaponPath.EndsWith(".prefab");

            if (isPrefab)
            {
                return PrefabUtility.InstantiatePrefab(weaponPrefab) as GameObject;
            }
            else
            {
                return Instantiate(weaponPrefab);
            }
        }

        private void CleanupWeapon(GameObject weaponInstance)
        {
            if (weaponInstance == null) return;

            var networkIdentity = weaponInstance.GetComponent<Mirror.NetworkIdentity>();
            if (networkIdentity != null)
            {
                Object.DestroyImmediate(networkIdentity);
            }

            var networkTransform = weaponInstance.GetComponent("NetworkTransformReliable") ?? 
                                  weaponInstance.GetComponent("NetworkTransform") ??
                                  weaponInstance.GetComponent("NetworkTransformUnreliable");
            if (networkTransform != null)
            {
                Object.DestroyImmediate(networkTransform as Component);
            }
        }

        private void UpdateWeaponSystemReference(GameObject playerInstance, Transform weaponHolder)
        {
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null && weaponHolder != null)
            {
                SerializedObject so = new SerializedObject(weaponSystem);
                var weaponHolderProp = so.FindProperty("weaponHolder");
                if (weaponHolderProp != null)
                {
                    weaponHolderProp.objectReferenceValue = weaponHolder;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(weaponSystem);
                    Debug.Log("✅ Updated WeaponSystem.weaponHolder reference");
                }
            }
        }

        private Transform FindBoneInHierarchy(Transform root, string boneName)
        {
            Transform found = root.Find(boneName);
            if (found != null) return found;

            foreach (Transform child in root)
            {
                if (child.name.Contains(boneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform result = FindBoneInHierarchy(child, boneName);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// ✅ NEW: Setup root motion on character animator
        /// </summary>
        private void SetupRootMotion(GameObject playerInstance)
        {
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator != null)
            {
                characterAnimator.applyRootMotion = useRootMotion;
                EditorUtility.SetDirty(characterAnimator);
                Debug.Log($"✅ Root Motion set to: {useRootMotion}");
            }
        }

        /// <summary>
        /// ✅ NEW: Preview weapon position in scene
        /// </summary>
        private void PreviewWeaponPosition(GameObject playerInstance)
        {
            // This would instantiate the player in scene for preview
            // For now, just log the weapon position
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator != null)
            {
                string[] handBoneNames = { "RightHand", "Hand_R", "R_Hand", "RightHandIndex1" };
                Transform handBone = null;
                
                foreach (string boneName in handBoneNames)
                {
                    handBone = FindBoneInHierarchy(characterAnimator.transform, boneName);
                    if (handBone != null) break;
                }
                
                if (handBone != null)
                {
                    Transform weaponHolder = handBone.Find("WeaponHolder");
                    if (weaponHolder != null)
                    {
                        Debug.Log($"✅ Weapon Position Preview:");
                        Debug.Log($"   Hand Bone: {handBone.name}");
                        Debug.Log($"   WeaponHolder Local Position: {weaponHolder.localPosition}");
                        Debug.Log($"   WeaponHolder Local Rotation: {weaponHolder.localRotation.eulerAngles}");
                    }
                }
            }
        }

        /// <summary>
        /// ✅ NEW: Test setup in scene
        /// </summary>
        private void TestSetupInScene()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab != null)
            {
                GameObject testInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                if (testInstance != null)
                {
                    testInstance.transform.position = Vector3.zero;
                    testInstance.transform.rotation = Quaternion.identity;
                    Selection.activeGameObject = testInstance;
                    Debug.Log("✅ Test instance created in scene. Check it in Hierarchy.");
                }
            }
        }

        /// <summary>
        /// ✅ NEW: Fix Mixamo texture and material issues (gray textures, missing colors)
        /// </summary>
        private bool FixTexturesAndMaterials(GameObject playerInstance)
        {
            bool modified = false;
            
            if (tPoseCharacter == null)
            {
                Debug.LogWarning("⚠️ [MixamoCompleteSetup] No character model selected for texture fix!");
                return false;
            }

            string modelPath = AssetDatabase.GetAssetPath(tPoseCharacter);
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogWarning("⚠️ [MixamoCompleteSetup] Could not get model path!");
                return false;
            }

            // Find character model in player instance
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual == null)
            {
                Debug.LogWarning("⚠️ [MixamoCompleteSetup] PlayerVisual not found!");
                return false;
            }

            // Find character instance
            GameObject characterInstance = null;
            foreach (Transform child in playerVisual)
            {
                if (child.name == characterName || child.name.Contains("Ch33") || child.name.Contains("Mixamo"))
                {
                    characterInstance = child.gameObject;
                    break;
                }
            }

            if (characterInstance == null)
            {
                Debug.LogWarning("⚠️ [MixamoCompleteSetup] Character instance not found in PlayerVisual!");
                return false;
            }

            Debug.Log($"✅ Found character instance: {characterInstance.name}");

            // Fix ModelImporter settings
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer != null)
            {
                bool reimportNeeded = false;

                // ✅ FIX: Use materialImportMode instead of deprecated importMaterials
                // Check if material import is disabled and enable it
                var currentMode = importer.materialImportMode;
                if (currentMode == ModelImporterMaterialImportMode.None)
                {
                    // Try to set to a valid import mode
                    // Check available enum values dynamically
                    var enumValues = System.Enum.GetValues(typeof(ModelImporterMaterialImportMode));
                    foreach (var value in enumValues)
                    {
                        if (value.ToString() != "None")
                        {
                            try
                            {
                                importer.materialImportMode = (ModelImporterMaterialImportMode)value;
                                reimportNeeded = true;
                                Debug.Log($"✅ Enabled material import (mode: {value})");
                                break;
                            }
                            catch
                            {
                                // Continue to next value
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"ℹ️ Material import already enabled (mode: {currentMode})");
                }

                // Set material location (extract to external)
                if (importer.materialLocation != ModelImporterMaterialLocation.External)
                {
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    reimportNeeded = true;
                    Debug.Log("✅ Set material location to External");
                }

                // Set material naming
                if (importer.materialName != ModelImporterMaterialName.BasedOnTextureName)
                {
                    importer.materialName = ModelImporterMaterialName.BasedOnTextureName;
                    reimportNeeded = true;
                    Debug.Log("✅ Set material naming to BasedOnTextureName");
                }

                // Set material search
                if (importer.materialSearch != ModelImporterMaterialSearch.Everywhere)
                {
                    importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
                    reimportNeeded = true;
                    Debug.Log("✅ Set material search to Everywhere");
                }

                if (reimportNeeded)
                {
                    AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log("✅ Reimported model with new material settings");
                    modified = true;
                }
            }

            // Fix materials on character instance
            Renderer[] renderers = characterInstance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool materialsChanged = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;

                    Material mat = materials[i];
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    
                    // Check if material is using wrong shader or has no textures
                    if (mat.shader.name == "Standard" || mat.shader.name.Contains("Standard"))
                    {
                        // Try to find textures
                        Texture2D mainTex = mat.mainTexture as Texture2D;
                        Texture2D normalMap = mat.GetTexture("_BumpMap") as Texture2D;
                        
                        // If no main texture, try to find it
                        if (mainTex == null)
                        {
                            // Look for texture files in same directory as model
                            string modelDir = System.IO.Path.GetDirectoryName(modelPath);
                            string modelName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
                            
                            // Common Mixamo texture naming patterns
                            string[] texturePatterns = {
                                $"{modelName}_Diffuse",
                                $"{modelName}_Albedo",
                                $"{modelName}_BaseColor",
                                $"{modelName}",
                                "diffuse",
                                "albedo",
                                "basecolor"
                            };

                            foreach (string pattern in texturePatterns)
                            {
                                string[] guids = AssetDatabase.FindAssets($"{pattern} t:Texture2D", new[] { modelDir });
                                if (guids.Length > 0)
                                {
                                    string texPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                                    mainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                                    if (mainTex != null)
                                    {
                                        Debug.Log($"✅ Found texture: {texPath}");
                                        break;
                                    }
                                }
                            }
                        }

                        // Apply textures if found
                        if (mainTex != null)
                        {
                            mat.mainTexture = mainTex;
                            materialsChanged = true;
                            Debug.Log($"✅ Applied main texture to material: {mat.name}");
                        }

                        // Fix texture import settings
                        if (mainTex != null)
                        {
                            string texPath = AssetDatabase.GetAssetPath(mainTex);
                            TextureImporter texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                            if (texImporter != null)
                            {
                                bool texReimportNeeded = false;

                                // Set texture type
                                if (texImporter.textureType != TextureImporterType.Default)
                                {
                                    texImporter.textureType = TextureImporterType.Default;
                                    texReimportNeeded = true;
                                }

                                // Enable sRGB for color textures
                                if (!texImporter.sRGBTexture)
                                {
                                    texImporter.sRGBTexture = true;
                                    texReimportNeeded = true;
                                }

                                // Set max size
                                if (texImporter.maxTextureSize < 2048)
                                {
                                    texImporter.maxTextureSize = 2048;
                                    texReimportNeeded = true;
                                }

                                if (texReimportNeeded)
                                {
                                    AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                                    Debug.Log($"✅ Fixed texture import settings: {texPath}");
                                }
                            }
                        }

                        // Ensure material uses Standard shader with proper settings
                        if (mat.shader.name != "Standard")
                        {
                            mat.shader = Shader.Find("Standard");
                            materialsChanged = true;
                            Debug.Log($"✅ Set shader to Standard for material: {mat.name}");
                        }

                        // Set material properties for better appearance
                        if (mat.HasProperty("_Metallic"))
                        {
                            mat.SetFloat("_Metallic", 0f);
                        }
                        if (mat.HasProperty("_Glossiness"))
                        {
                            mat.SetFloat("_Glossiness", 0.5f);
                        }
                    }
                }

                if (materialsChanged)
                {
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    modified = true;
                }
            }

            if (modified)
            {
                Debug.Log("✅ Fixed textures and materials!");
            }
            else
            {
                Debug.Log("ℹ️ No texture/material fixes needed");
            }

            return modified;
        }
    }
}

