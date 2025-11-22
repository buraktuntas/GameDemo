using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ MIXAMO CHARACTER INTEGRATOR: Mixamo karakter ve animasyonlarını kusursuz entegre eder
    /// </summary>
    public class MixamoCharacterIntegrator : EditorWindow
    {
        [Header("Character Model")]
        private GameObject characterModel; // Mixamo karakter FBX'i
        
        [Header("Animations")]
        private List<AnimationClip> animationClips = new List<AnimationClip>();
        private string animationFolderPath = "Assets";
        
        [Header("Settings")]
        private string characterName = "MixamoCharacter";
        private bool createAnimatorController = true;
        private bool setupAvatar = true;
        private bool applyToPlayerPrefab = true;
        
        [Header("Animation Mapping")]
        private AnimationClip idleAnimation;
        private AnimationClip walkAnimation;
        private AnimationClip runAnimation;
        private AnimationClip shootAnimation;
        private AnimationClip reloadAnimation;
        private AnimationClip jumpAnimation;
        private AnimationClip dieAnimation;

        [MenuItem("Tools/Tactical Combat/Mixamo Character Integrator")]
        public static void ShowWindow()
        {
            GetWindow<MixamoCharacterIntegrator>("Mixamo Character Integrator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Mixamo Character Integrator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Mixamo karakter ve animasyonlarını otomatik entegre eder:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Character Model
            GUILayout.Label("Character Model (FBX):", EditorStyles.boldLabel);
            characterModel = (GameObject)EditorGUILayout.ObjectField(
                characterModel,
                typeof(GameObject),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Animation Folder
            GUILayout.Label("Animation Folder Path:", EditorStyles.boldLabel);
            animationFolderPath = EditorGUILayout.TextField(animationFolderPath);
            if (GUILayout.Button("Browse Folder", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Animation Folder", animationFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    animationFolderPath = "Assets" + path.Replace(Application.dataPath, "");
                }
            }
            GUILayout.Space(5);

            if (GUILayout.Button("Auto-Detect Animations", GUILayout.Height(30)))
            {
                AutoDetectAnimations();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Animation Mapping
            GUILayout.Label("Animation Mapping:", EditorStyles.boldLabel);
            idleAnimation = (AnimationClip)EditorGUILayout.ObjectField("Idle", idleAnimation, typeof(AnimationClip), false);
            walkAnimation = (AnimationClip)EditorGUILayout.ObjectField("Walk", walkAnimation, typeof(AnimationClip), false);
            runAnimation = (AnimationClip)EditorGUILayout.ObjectField("Run", runAnimation, typeof(AnimationClip), false);
            shootAnimation = (AnimationClip)EditorGUILayout.ObjectField("Shoot", shootAnimation, typeof(AnimationClip), false);
            reloadAnimation = (AnimationClip)EditorGUILayout.ObjectField("Reload", reloadAnimation, typeof(AnimationClip), false);
            jumpAnimation = (AnimationClip)EditorGUILayout.ObjectField("Jump", jumpAnimation, typeof(AnimationClip), false);
            dieAnimation = (AnimationClip)EditorGUILayout.ObjectField("Die", dieAnimation, typeof(AnimationClip), false);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Settings
            GUILayout.Label("Settings:", EditorStyles.boldLabel);
            characterName = EditorGUILayout.TextField("Character Name", characterName);
            createAnimatorController = EditorGUILayout.Toggle("Create Animator Controller", createAnimatorController);
            setupAvatar = EditorGUILayout.Toggle("Setup Avatar", setupAvatar);
            applyToPlayerPrefab = EditorGUILayout.Toggle("Apply to Player Prefab", applyToPlayerPrefab);

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(characterModel == null);
            if (GUILayout.Button("Integrate Mixamo Character", GUILayout.Height(40)))
            {
                IntegrateMixamoCharacter();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void AutoDetectAnimations()
        {
            if (string.IsNullOrEmpty(animationFolderPath))
            {
                EditorUtility.DisplayDialog("Error", "Please set Animation Folder Path!", "OK");
                return;
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 AUTO-DETECTING ANIMATIONS: {animationFolderPath}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Find all animation clips
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationFolderPath });
            animationClips.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    animationClips.Add(clip);
                }
            }

            Debug.Log($"✅ Found {animationClips.Count} animation clips");

            // Auto-map animations by name patterns
            AutoMapAnimations();

            Debug.Log($"\n✅ Auto-detection complete!");
            Debug.Log($"═══════════════════════════════════════════════════════\n");
        }

        private void AutoMapAnimations()
        {
            // Mixamo animation naming patterns
            var patterns = new Dictionary<string, string[]>
            {
                { "idle", new[] { "Idle", "idle", "IDLE", "Stand", "stand" } },
                { "walk", new[] { "Walk", "walk", "WALK", "Walking", "walking" } },
                { "run", new[] { "Run", "run", "RUN", "Running", "running", "Sprint", "sprint" } },
                { "shoot", new[] { "Shoot", "shoot", "SHOOT", "Fire", "fire", "Attack", "attack", "Shooting", "shooting" } },
                { "reload", new[] { "Reload", "reload", "RELOAD", "Reloading", "reloading" } },
                { "jump", new[] { "Jump", "jump", "JUMP", "Jumping", "jumping" } },
                { "die", new[] { "Die", "die", "DIE", "Death", "death", "Dead", "dead", "Dying", "dying" } }
            };

            foreach (var pattern in patterns)
            {
                string key = pattern.Key;
                string[] keywords = pattern.Value;

                AnimationClip found = null;
                foreach (var clip in animationClips)
                {
                    foreach (var keyword in keywords)
                    {
                        if (clip.name.Contains(keyword))
                        {
                            found = clip;
                            break;
                        }
                    }
                    if (found != null) break;
                }

                // Set the found animation
                switch (key)
                {
                    case "idle":
                        idleAnimation = found;
                        break;
                    case "walk":
                        walkAnimation = found;
                        break;
                    case "run":
                        runAnimation = found;
                        break;
                    case "shoot":
                        shootAnimation = found;
                        break;
                    case "reload":
                        reloadAnimation = found;
                        break;
                    case "jump":
                        jumpAnimation = found;
                        break;
                    case "die":
                        dieAnimation = found;
                        break;
                }

                if (found != null)
                {
                    Debug.Log($"✅ Mapped {key}: {found.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not find {key} animation");
                }
            }
        }

        private void IntegrateMixamoCharacter()
        {
            if (characterModel == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Character Model!", "OK");
                return;
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🎮 INTEGRATING MIXAMO CHARACTER: {characterModel.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // 1. Extract Avatar from character model
            Avatar avatar = null;
            if (setupAvatar)
            {
                avatar = ExtractAvatar(characterModel);
            }

            // 2. Create Animator Controller
            AnimatorController animatorController = null;
            if (createAnimatorController)
            {
                animatorController = CreateAnimatorController();
            }

            // 3. Apply to Player Prefab
            if (applyToPlayerPrefab)
            {
                ApplyToPlayerPrefab(characterModel, animatorController, avatar);
            }

            AssetDatabase.Refresh();

            Debug.Log($"\n✅ Mixamo character integration complete!");
            Debug.Log($"═══════════════════════════════════════════════════════\n");
            EditorUtility.DisplayDialog("Success", "Mixamo character integrated successfully!", "OK");
        }

        private Avatar ExtractAvatar(GameObject characterModel)
        {
            Avatar avatar = null;

            // Try to get Avatar from the model
            Animator animator = characterModel.GetComponent<Animator>();
            if (animator != null && animator.avatar != null)
            {
                Debug.Log($"✅ Found existing Avatar: {animator.avatar.name}");
                return animator.avatar;
            }

            // Try to get from model importer
            string modelPath = AssetDatabase.GetAssetPath(characterModel);
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer != null)
            {
                // Configure importer for humanoid
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log("✅ Configured model as Humanoid");
                }

                // Get avatar from importer
                if (importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    // Reimport to generate avatar
                    AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                    
                    // ✅ FIX: FBX files are not prefabs, use AssetDatabase.LoadAssetAtPath instead
                    // Check if it's a prefab or FBX
                    bool isPrefab = modelPath.EndsWith(".prefab");
                    
                    if (isPrefab)
                    {
                        // It's a prefab, use PrefabUtility
                        GameObject modelInstance = PrefabUtility.LoadPrefabContents(modelPath);
                        Animator modelAnimator = modelInstance.GetComponent<Animator>();
                        if (modelAnimator != null && modelAnimator.avatar != null)
                        {
                            avatar = modelAnimator.avatar;
                            Debug.Log($"✅ Created Avatar from prefab: {avatar.name}");
                        }
                        PrefabUtility.UnloadPrefabContents(modelInstance);
                    }
                    else
                    {
                        // It's an FBX/model file, use AssetDatabase
                        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                        if (modelAsset != null)
                        {
                            // Instantiate to get Avatar
                            GameObject modelInstance = Instantiate(modelAsset);
                            Animator modelAnimator = modelInstance.GetComponent<Animator>();
                            if (modelAnimator == null)
                            {
                                modelAnimator = modelInstance.GetComponentInChildren<Animator>();
                            }
                            
                            if (modelAnimator != null && modelAnimator.avatar != null)
                            {
                                avatar = modelAnimator.avatar;
                                Debug.Log($"✅ Created Avatar from FBX: {avatar.name}");
                            }
                            
                            // Clean up instantiated instance
                            DestroyImmediate(modelInstance);
                        }
                    }
                }
            }

            return avatar;
        }

        private AnimatorController CreateAnimatorController()
        {
            string controllerPath = $"Assets/Animators/{characterName}Controller.controller";
            
            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(controllerPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            // Create new Animator Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log($"✅ Created Animator Controller: {controllerPath}");

            // Note: Avatar is set on the Animator component, not the controller

            // Get root state machine
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Create states from mapped animations
            Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();

            if (idleAnimation != null)
            {
                AnimatorState idleState = rootStateMachine.AddState("Idle");
                idleState.motion = idleAnimation;
                states["Idle"] = idleState;
                rootStateMachine.defaultState = idleState;
                Debug.Log($"✅ Added Idle state: {idleAnimation.name}");
            }

            if (walkAnimation != null)
            {
                AnimatorState walkState = rootStateMachine.AddState("Walk");
                walkState.motion = walkAnimation;
                states["Walk"] = walkState;
                Debug.Log($"✅ Added Walk state: {walkAnimation.name}");
            }

            if (runAnimation != null)
            {
                AnimatorState runState = rootStateMachine.AddState("Run");
                runState.motion = runAnimation;
                states["Run"] = runState;
                Debug.Log($"✅ Added Run state: {runAnimation.name}");
            }

            if (shootAnimation != null)
            {
                AnimatorState shootState = rootStateMachine.AddState("Shoot");
                shootState.motion = shootAnimation;
                states["Shoot"] = shootState;
                Debug.Log($"✅ Added Shoot state: {shootAnimation.name}");
            }

            if (reloadAnimation != null)
            {
                AnimatorState reloadState = rootStateMachine.AddState("Reload");
                reloadState.motion = reloadAnimation;
                states["Reload"] = reloadState;
                Debug.Log($"✅ Added Reload state: {reloadAnimation.name}");
            }

            if (jumpAnimation != null)
            {
                AnimatorState jumpState = rootStateMachine.AddState("Jump");
                jumpState.motion = jumpAnimation;
                states["Jump"] = jumpState;
                Debug.Log($"✅ Added Jump state: {jumpAnimation.name}");
            }

            if (dieAnimation != null)
            {
                AnimatorState dieState = rootStateMachine.AddState("Die");
                dieState.motion = dieAnimation;
                states["Die"] = dieState;
                Debug.Log($"✅ Added Die state: {dieAnimation.name}");
            }

            // Add required parameters
            EnsureParameters(controller);

            // Create transitions
            CreateMixamoTransitions(states, rootStateMachine);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

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
                { "TriggerReload", AnimatorControllerParameterType.Trigger }
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

        private void CreateMixamoTransitions(Dictionary<string, AnimatorState> states, AnimatorStateMachine stateMachine)
        {
            // Idle <-> Walk <-> Run
            if (states.ContainsKey("Idle") && states.ContainsKey("Walk"))
            {
                CreateTransition(states["Idle"], states["Walk"], "Speed", 0.1f, true);
                CreateTransition(states["Walk"], states["Idle"], "Speed", 0.1f, false);
            }

            if (states.ContainsKey("Walk") && states.ContainsKey("Run"))
            {
                CreateTransition(states["Walk"], states["Run"], "Speed", 4f, true);
                CreateTransition(states["Run"], states["Walk"], "Speed", 4f, false);
            }

            // Shoot transitions
            if (states.ContainsKey("Shoot"))
            {
                if (states.ContainsKey("Idle"))
                    CreateTransition(states["Idle"], states["Shoot"], "TriggerFire", 0f, true);
                if (states.ContainsKey("Walk"))
                    CreateTransition(states["Walk"], states["Shoot"], "TriggerFire", 0f, true);
                if (states.ContainsKey("Run"))
                    CreateTransition(states["Run"], states["Shoot"], "TriggerFire", 0f, true);

                // Shoot back to previous state
                if (states.ContainsKey("Idle"))
                    CreateTransition(states["Shoot"], states["Idle"], null, 0f, true, true);
            }

            // Reload transitions
            if (states.ContainsKey("Reload"))
            {
                if (states.ContainsKey("Idle"))
                {
                    CreateTransition(states["Idle"], states["Reload"], "TriggerReload", 0f, true);
                    CreateTransition(states["Reload"], states["Idle"], "IsReloading", 0f, false, true);
                }
                if (states.ContainsKey("Walk"))
                    CreateTransition(states["Walk"], states["Reload"], "TriggerReload", 0f, true);
            }

            // Jump transitions
            if (states.ContainsKey("Jump") && states.ContainsKey("Idle"))
            {
                CreateTransition(states["Idle"], states["Jump"], null, 0f, true, true);
                CreateTransition(states["Jump"], states["Idle"], "IsGrounded", 0f, true, true);
            }
        }

        private void CreateTransition(AnimatorState from, AnimatorState to, string parameterName, float threshold, bool greaterThan, bool useExitTime = false)
        {
            if (from == null || to == null) return;

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = useExitTime || parameterName == null;
            transition.exitTime = 0.9f;
            transition.duration = 0.25f;

            if (parameterName != null)
            {
                if (parameterName == "Speed")
                {
                    transition.AddCondition(greaterThan ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, parameterName);
                }
                else if (parameterName == "TriggerFire" || parameterName == "TriggerReload")
                {
                    transition.AddCondition(AnimatorConditionMode.If, 0, parameterName);
                }
                else if (parameterName == "IsReloading" || parameterName == "IsGrounded")
                {
                    transition.AddCondition(greaterThan ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameterName);
                }
            }
        }

        private void ApplyToPlayerPrefab(GameObject characterModel, AnimatorController animatorController, Avatar avatar)
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            Debug.Log($"\n🔧 APPLYING TO PLAYER PREFAB");

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
                if (child == null) continue; // Safety check
                
                // ✅ FIX: Store name before destroying
                string childName = child.name;
                bool shouldRemove = childName.Contains("Character") || childName.Contains("Model") ||
                    child.GetComponent<SkinnedMeshRenderer>() != null ||
                    child.GetComponent<Animator>() != null;
                
                if (shouldRemove)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"✅ Removed old character model: {childName}");
                    modified = true;
                }
            }

            // Instantiate Mixamo character (handle both prefab and FBX)
            GameObject characterInstance = null;
            string modelPath = AssetDatabase.GetAssetPath(characterModel);
            bool isPrefab = modelPath.EndsWith(".prefab");
            
            if (isPrefab)
            {
                characterInstance = PrefabUtility.InstantiatePrefab(characterModel) as GameObject;
            }
            else
            {
                // It's an FBX/model file, instantiate directly
                characterInstance = Instantiate(characterModel);
            }
            if (characterInstance != null)
            {
                characterInstance.name = characterName;
                characterInstance.transform.SetParent(playerVisual);
                characterInstance.transform.localPosition = Vector3.zero;
                characterInstance.transform.localRotation = Quaternion.identity;
                characterInstance.transform.localScale = Vector3.one;
                modified = true;
                Debug.Log($"✅ Added Mixamo character: {characterModel.name}");

                // Setup Animator
                Animator animator = characterInstance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = characterInstance.AddComponent<Animator>();
                    Debug.Log("✅ Added Animator component");
                    modified = true;
                }

                // Assign Animator Controller
                if (animatorController != null)
                {
                    animator.runtimeAnimatorController = animatorController;
                    EditorUtility.SetDirty(animator);
                    Debug.Log($"✅ Assigned Animator Controller: {animatorController.name}");
                    modified = true;
                }

                // Assign Avatar
                if (avatar != null)
                {
                    animator.avatar = avatar;
                    EditorUtility.SetDirty(animator);
                    Debug.Log($"✅ Assigned Avatar: {avatar.name}");
                    modified = true;
                }

                // Update PlayerVisuals component
                PlayerVisuals playerVisuals = playerInstance.GetComponent<PlayerVisuals>();
                if (playerVisuals == null)
                {
                    playerVisuals = playerInstance.AddComponent<PlayerVisuals>();
                    modified = true;
                }

                Renderer newRenderer = characterInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (newRenderer == null)
                {
                    newRenderer = characterInstance.GetComponentInChildren<MeshRenderer>();
                }

                if (newRenderer != null)
                {
                    SerializedObject visualsSo = new SerializedObject(playerVisuals);
                    var rendererProp = visualsSo.FindProperty("visualRenderer");
                    if (rendererProp != null)
                    {
                        rendererProp.objectReferenceValue = newRenderer;
                        visualsSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(playerVisuals);
                        Debug.Log($"✅ Updated PlayerVisuals to use renderer: {newRenderer.name}");
                        modified = true;
                    }
                }

                // Add BattleRoyaleAnimationController
                var animationController = playerInstance.GetComponent<BattleRoyaleAnimationController>();
                if (animationController == null)
                {
                    animationController = playerInstance.AddComponent<BattleRoyaleAnimationController>();
                    Debug.Log("✅ Added BattleRoyaleAnimationController");
                    modified = true;
                }

                // Configure animation controller references
                if (animationController != null)
                {
                    SerializedObject animCtrlSo = new SerializedObject(animationController);
                    var animatorProp = animCtrlSo.FindProperty("characterAnimator");
                    if (animatorProp != null && animator != null)
                    {
                        animatorProp.objectReferenceValue = animator;
                        animCtrlSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(animationController);
                        Debug.Log("✅ Set characterAnimator reference");
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Player prefab updated!");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

