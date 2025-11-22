using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ ANIMATION DEBUGGER: Animasyonların neden çalışmadığını debug eder
    /// </summary>
    public class AnimationDebugger : EditorWindow
    {
        private AnimatorController animatorController;
        private Vector2 scrollPosition;
        private bool showParameterValues = true;
        private bool showStateInfo = true;
        private bool showTransitions = true;

        [MenuItem("Tools/Tactical Combat/Debug Animations")]
        public static void ShowWindow()
        {
            GetWindow<AnimationDebugger>("Animation Debugger");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Animation Debugger", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool animasyonların neden çalışmadığını debug eder:\n\n" +
                "• Animator Controller'daki parametreleri kontrol eder\n" +
                "• State'leri ve transition'ları gösterir\n" +
                "• Runtime'da parametre değerlerini gösterir",
                MessageType.Info);
            GUILayout.Space(10);

            // Animator Controller
            GUILayout.Label("Animator Controller:", EditorStyles.boldLabel);
            animatorController = (AnimatorController)EditorGUILayout.ObjectField(
                animatorController,
                typeof(AnimatorController),
                false
            );
            GUILayout.Space(10);

            // Auto-find from Player prefab
            if (GUILayout.Button("Auto-Find from Player Prefab", GUILayout.Height(30)))
            {
                FindFromPlayerPrefab();
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(animatorController == null);
            
            // Options
            showParameterValues = EditorGUILayout.Toggle("Show Parameter Values", showParameterValues);
            showStateInfo = EditorGUILayout.Toggle("Show State Info", showStateInfo);
            showTransitions = EditorGUILayout.Toggle("Show Transitions", showTransitions);
            
            GUILayout.Space(10);

            if (GUILayout.Button("Debug Animator Controller", GUILayout.Height(40)))
            {
                DebugAnimatorController();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void FindFromPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            Animator animator = playerInstance.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController != null)
                {
                    Debug.Log($"✅ Found Animator Controller: {animatorController.name}");
                }
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private void DebugAnimatorController()
        {
            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animator Controller!", "OK");
                return;
            }

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 DEBUGGING ANIMATOR CONTROLLER: {animatorController.name}");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Check Parameters
            Debug.Log("📋 PARAMETERS:");
            if (animatorController.parameters.Length == 0)
            {
                Debug.LogError("  ❌ No parameters found! Animations won't work!");
            }
            else
            {
                foreach (var param in animatorController.parameters)
                {
                    Debug.Log($"  ✅ {param.name} ({param.type})");
                }
            }

            // Check Required Parameters
            Debug.Log("\n🔍 REQUIRED PARAMETERS CHECK:");
            var requiredParams = new Dictionary<string, AnimatorControllerParameterType>
            {
                { "Speed", AnimatorControllerParameterType.Float },
                { "IsShooting", AnimatorControllerParameterType.Bool },
                { "IsReloading", AnimatorControllerParameterType.Bool },
                { "IsGrounded", AnimatorControllerParameterType.Bool },
                { "TriggerFire", AnimatorControllerParameterType.Trigger },
                { "TriggerReload", AnimatorControllerParameterType.Trigger },
                { "Jump", AnimatorControllerParameterType.Trigger }
            };

            foreach (var reqParam in requiredParams)
            {
                bool exists = animatorController.parameters.Any(p => p.name == reqParam.Key && p.type == reqParam.Value);
                if (exists)
                {
                    Debug.Log($"  ✅ {reqParam.Key} ({reqParam.Value}) - Found");
                }
                else
                {
                    Debug.LogError($"  ❌ {reqParam.Key} ({reqParam.Value}) - MISSING!");
                }
            }

            // Check States
            if (showStateInfo)
            {
                Debug.Log("\n🎬 STATES:");
                foreach (var layer in animatorController.layers)
                {
                    Debug.Log($"\n  Layer: {layer.name}");
                    
                    if (layer.stateMachine.defaultState != null)
                    {
                        Debug.Log($"  📌 Default State: {layer.stateMachine.defaultState.name}");
                    }
                    
                    foreach (var state in layer.stateMachine.states)
                    {
                        string stateName = state.state.name;
                        Motion motion = state.state.motion;
                        
                        if (motion != null)
                        {
                            Debug.Log($"    ✅ {stateName} -> {motion.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"    ⚠️ {stateName} -> No motion assigned!");
                        }
                    }
                }
            }

            // Check Transitions
            if (showTransitions)
            {
                Debug.Log("\n🔄 TRANSITIONS:");
                foreach (var layer in animatorController.layers)
                {
                    Debug.Log($"\n  Layer: {layer.name}");
                    
                    foreach (var state in layer.stateMachine.states)
                    {
                        if (state.state.transitions.Length > 0)
                        {
                            Debug.Log($"\n    From: {state.state.name}");
                            foreach (var transition in state.state.transitions)
                            {
                                string conditions = "";
                                foreach (var condition in transition.conditions)
                                {
                                    conditions += $"{condition.parameter} {condition.mode} {condition.threshold}, ";
                                }
                                Debug.Log($"      → {transition.destinationState.name} [{conditions.TrimEnd(',', ' ')}]");
                            }
                        }
                    }
                }
            }

            Debug.Log("\n═══════════════════════════════════════════════════════\n");
            
            EditorUtility.DisplayDialog("Debug Complete", 
                "Animator Controller debugged!\n\n" +
                "Check Console for detailed information.",
                "OK");
        }
    }
}

