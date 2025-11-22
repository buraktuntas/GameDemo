using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ DYNAMIC ANIMATION SETUP: Farklı karakter ve animasyonlar için otomatik animator state'leri oluşturur
    /// </summary>
    public class DynamicAnimationSetup : EditorWindow
    {
        private GameObject characterPrefab;
        private RuntimeAnimatorController animatorController;
        private string animationFolderPath = "Assets/BattleRoyaleDuoPAPBR/Animation";

        [MenuItem("Tools/Tactical Combat/Dynamic Animation Setup")]
        public static void ShowWindow()
        {
            GetWindow<DynamicAnimationSetup>("Dynamic Animation Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Dynamic Animation Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool farklı karakter ve animasyonlar için otomatik animator state'leri oluşturur:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Character Prefab
            GUILayout.Label("Character Prefab:", EditorStyles.boldLabel);
            characterPrefab = (GameObject)EditorGUILayout.ObjectField(
                characterPrefab,
                typeof(GameObject),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Animator Controller
            GUILayout.Label("Animator Controller:", EditorStyles.boldLabel);
            animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                animatorController,
                typeof(RuntimeAnimatorController),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Animation Folder Path
            GUILayout.Label("Animation Folder Path:", EditorStyles.boldLabel);
            animationFolderPath = EditorGUILayout.TextField(animationFolderPath);
            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(animatorController == null);
            if (GUILayout.Button("Auto Setup Animator States", GUILayout.Height(30)))
            {
                SetupAnimatorStates();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Find Animation Clips", GUILayout.Height(30)))
            {
                FindAndListAnimationClips();
            }
        }

        private void SetupAnimatorStates()
        {
            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animator Controller!", "OK");
                return;
            }

            AnimatorController controller = animatorController as AnimatorController;
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected object is not an AnimatorController!", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm",
                $"Bu işlem {controller.name} Animator Controller'ını yeniden yapılandıracak.\n\n" +
                "Mevcut state'ler ve transition'lar silinebilir.\n\n" +
                "Devam etmek istiyor musunuz?",
                "Yes", "No"))
            {
                return;
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔧 DYNAMIC ANIMATION SETUP: {controller.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Find animation clips
            Dictionary<string, AnimationClip> animations = FindAnimationClips(animationFolderPath);
            if (animations.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", $"No animation clips found in: {animationFolderPath}", "OK");
                return;
            }

            // Get root state machine
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Auto-detect animation patterns and create states
            Dictionary<string, AnimatorState> states = CreateStatesFromAnimations(rootStateMachine, animations);

            // Create intelligent transitions based on animation names
            CreateIntelligentTransitions(states, animations);

            // Ensure required parameters exist
            EnsureRequiredParameters(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"\n✅ Dynamic animation setup complete!");
            Debug.Log($"   Created {states.Count} states");
            Debug.Log($"═══════════════════════════════════════════════════════\n");
            EditorUtility.DisplayDialog("Success", "Dynamic animation setup complete!", "OK");
        }

        private Dictionary<string, AnimationClip> FindAnimationClips(string folderPath)
        {
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    clips[clip.name] = clip;
                }
            }

            Debug.Log($"✅ Found {clips.Count} animation clips");
            return clips;
        }

        private Dictionary<string, AnimatorState> CreateStatesFromAnimations(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> animations)
        {
            Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();

            // Pattern matching for common animation types
            var patterns = new Dictionary<string, string[]>
            {
                { "Idle", new[] { "Idle", "Stand" } },
                { "Walk", new[] { "Walk", "MoveFWD" } },
                { "Run", new[] { "Run", "Sprint" } },
                { "Shoot", new[] { "Shoot", "Fire", "Attack" } },
                { "Reload", new[] { "Reload", "Reloading" } },
                { "Jump", new[] { "Jump", "JumpStart" } },
                { "Fall", new[] { "Fall", "JumpAir" } },
                { "Land", new[] { "Land", "JumpEnd" } },
                { "Die", new[] { "Die", "Death" } },
                { "Hit", new[] { "Hit", "GetHit" } }
            };

            // Create states based on patterns
            foreach (var pattern in patterns)
            {
                string stateName = pattern.Key;
                string[] keywords = pattern.Value;

                AnimationClip clip = FindAnimationByKeywords(animations, keywords);
                if (clip != null)
                {
                    AnimatorState state = CreateStateIfNotExists(stateMachine, stateName, clip);
                    states[stateName] = state;
                }
            }

            // Set default state
            if (states.ContainsKey("Idle") && states["Idle"] != null)
            {
                stateMachine.defaultState = states["Idle"];
                Debug.Log("✅ Default state set to: Idle");
            }

            return states;
        }

        private AnimationClip FindAnimationByKeywords(Dictionary<string, AnimationClip> animations, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                // Exact match
                foreach (var kvp in animations)
                {
                    if (kvp.Key.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }
            return null;
        }

        private AnimatorState CreateStateIfNotExists(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
        {
            // Check if state already exists
            foreach (var state in stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    Debug.Log($"ℹ️ State '{stateName}' already exists");
                    return state.state;
                }
            }

            // Create new state
            AnimatorState newState = stateMachine.AddState(stateName);
            newState.motion = clip;
            Debug.Log($"✅ Created state '{stateName}' with animation '{clip.name}'");
            return newState;
        }

        private void CreateIntelligentTransitions(Dictionary<string, AnimatorState> states, Dictionary<string, AnimationClip> animations)
        {
            // Idle <-> Walk <-> Run transitions
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

            // Shoot transitions (from any movement state)
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
                if (states.ContainsKey("Land"))
                {
                    CreateTransition(states["Jump"], states["Land"], null, 0f, true, true);
                    CreateTransition(states["Land"], states["Idle"], "IsGrounded", 0f, true, true);
                }
                else
                {
                    CreateTransition(states["Jump"], states["Idle"], "IsGrounded", 0f, true, true);
                }
            }
        }

        private void CreateTransition(AnimatorState from, AnimatorState to, string parameterName, float threshold, bool greaterThan, bool useExitTime = false)
        {
            if (from == null || to == null) return;

            // Check if transition already exists
            foreach (var existingTransition in from.transitions)
            {
                if (existingTransition.destinationState == to)
                {
                    bool conditionsMatch = true;
                    if (parameterName != null)
                    {
                        conditionsMatch = false;
                        foreach (var condition in existingTransition.conditions)
                        {
                            if (condition.parameter == parameterName)
                            {
                                conditionsMatch = true;
                                break;
                            }
                        }
                    }

                    if (conditionsMatch)
                    {
                        return; // Transition already exists
                    }
                }
            }

            // Create new transition
            AnimatorStateTransition newTransition = from.AddTransition(to);
            newTransition.hasExitTime = useExitTime || parameterName == null;
            newTransition.exitTime = 0.9f;
            newTransition.duration = 0.25f;

            if (parameterName != null)
            {
                if (parameterName == "Speed")
                {
                    newTransition.AddCondition(greaterThan ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, parameterName);
                }
                else if (parameterName == "TriggerFire" || parameterName == "TriggerReload")
                {
                    newTransition.AddCondition(AnimatorConditionMode.If, 0, parameterName);
                }
                else if (parameterName == "IsReloading" || parameterName == "IsGrounded")
                {
                    newTransition.AddCondition(greaterThan ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameterName);
                }
            }
        }

        private void EnsureRequiredParameters(AnimatorController controller)
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
                bool exists = false;
                foreach (var existingParam in controller.parameters)
                {
                    if (existingParam.name == param.Key)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    controller.AddParameter(param.Key, param.Value);
                    Debug.Log($"✅ Added parameter: {param.Key}");
                }
            }
        }

        private void FindAndListAnimationClips()
        {
            Dictionary<string, AnimationClip> clips = FindAnimationClips(animationFolderPath);
            
            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"📋 FOUND {clips.Count} ANIMATION CLIPS:");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            foreach (var clip in clips.Values)
            {
                Debug.Log($"  • {clip.name}");
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════\n");
        }
    }
}

