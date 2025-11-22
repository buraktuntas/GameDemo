using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ ANIMATION TRIGGER MAPPER: Hangi animasyonun ne zaman çalışacağını gösterir ve düzenler
    /// </summary>
    public class AnimationTriggerMapper : EditorWindow
    {
        private RuntimeAnimatorController animatorController;
        private Vector2 scrollPosition;
        private Dictionary<string, List<AnimationTriggerInfo>> triggerMap = new Dictionary<string, List<AnimationTriggerInfo>>();

        [MenuItem("Tools/Tactical Combat/Animation Trigger Mapper")]
        public static void ShowWindow()
        {
            GetWindow<AnimationTriggerMapper>("Animation Trigger Mapper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Trigger Mapper", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool hangi animasyonun ne zaman çalışacağını gösterir:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Animator Controller
            GUILayout.Label("Animator Controller:", EditorStyles.boldLabel);
            RuntimeAnimatorController newController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                animatorController,
                typeof(RuntimeAnimatorController),
                false,
                GUILayout.Height(20)
            );

            if (newController != animatorController)
            {
                animatorController = newController;
                AnalyzeAnimatorController();
            }

            GUILayout.Space(10);

            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("Please assign an Animator Controller to analyze.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Refresh Analysis", GUILayout.Height(30)))
            {
                AnalyzeAnimatorController();
            }

            GUILayout.Space(10);

            // Display trigger map
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Show game events and their animations
            GUILayout.Label("Game Events → Animations", EditorStyles.boldLabel);
            GUILayout.Space(5);

            ShowGameEventSection("WeaponSystem.OnWeaponFired", "TriggerFire", "ShootSingleshot", "ShootAutoshot", "ShootChargeshot");
            ShowGameEventSection("WeaponSystem.OnReloadStarted", "TriggerReload", "Reloading");
            ShowGameEventSection("FPSController Movement", "Speed", "IdleNormal01", "RunFWD", "RunFWD_RM");
            ShowGameEventSection("CharacterController.isGrounded", "IsGrounded", "Jump", "JumpAir", "JumpEnd");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Show animator states and transitions
            GUILayout.Label("Animator States & Transitions", EditorStyles.boldLabel);
            GUILayout.Space(5);

            ShowAnimatorStates();

            EditorGUILayout.EndScrollView();
        }

        private void AnalyzeAnimatorController()
        {
            triggerMap.Clear();

            if (animatorController == null) return;

            AnimatorController controller = animatorController as AnimatorController;
            if (controller == null) return;

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 ANALYZING: {controller.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Analyze all states and their transitions
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            AnalyzeStateMachine(rootStateMachine, "");

            Debug.Log($"\n✅ Analysis complete!");
            Debug.Log($"═══════════════════════════════════════════════════════\n");
        }

        private void AnalyzeStateMachine(AnimatorStateMachine stateMachine, string prefix)
        {
            // Analyze states
            foreach (var state in stateMachine.states)
            {
                string stateName = prefix + state.state.name;
                AnimationClip clip = state.state.motion as AnimationClip;
                string clipName = clip != null ? clip.name : "None";

                Debug.Log($"📋 State: {stateName} → Animation: {clipName}");

                // Analyze transitions
                foreach (var transition in state.state.transitions)
                {
                    string targetState = transition.destinationState != null ? transition.destinationState.name : "Exit";
                    string conditions = "";

                    foreach (var condition in transition.conditions)
                    {
                        if (!string.IsNullOrEmpty(conditions)) conditions += " AND ";
                        conditions += $"{condition.parameter} {GetConditionString(condition)}";
                    }

                    if (transition.hasExitTime)
                    {
                        if (!string.IsNullOrEmpty(conditions)) conditions += " OR ";
                        conditions += "ExitTime";
                    }

                    Debug.Log($"   → {targetState} (when: {conditions})");

                    // Map to game events
                    MapTransitionToGameEvent(stateName, targetState, transition);
                }
            }

            // Analyze sub-state machines
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                AnalyzeStateMachine(subStateMachine.stateMachine, prefix + subStateMachine.stateMachine.name + "/");
            }
        }

        private void MapTransitionToGameEvent(string fromState, string toState, AnimatorStateTransition transition)
        {
            foreach (var condition in transition.conditions)
            {
                string parameter = condition.parameter;
                string gameEvent = GetGameEventForParameter(parameter);

                if (!triggerMap.ContainsKey(gameEvent))
                {
                    triggerMap[gameEvent] = new List<AnimationTriggerInfo>();
                }

                triggerMap[gameEvent].Add(new AnimationTriggerInfo
                {
                    FromState = fromState,
                    ToState = toState,
                    Parameter = parameter,
                    Condition = GetConditionString(condition),
                    HasExitTime = transition.hasExitTime
                });
            }
        }

        private string GetGameEventForParameter(string parameter)
        {
            switch (parameter)
            {
                case "TriggerFire":
                    return "WeaponSystem.OnWeaponFired";
                case "TriggerReload":
                    return "WeaponSystem.OnReloadStarted";
                case "Speed":
                    return "FPSController Movement";
                case "IsGrounded":
                    return "CharacterController.isGrounded";
                case "IsReloading":
                    return "WeaponSystem.IsReloading";
                case "IsShooting":
                    return "WeaponSystem.IsShooting";
                case "IsAiming":
                    return "WeaponSystem.IsAiming";
                default:
                    return $"Parameter: {parameter}";
            }
        }

        private string GetConditionString(AnimatorCondition condition)
        {
            switch (condition.mode)
            {
                case AnimatorConditionMode.If:
                    return "== true";
                case AnimatorConditionMode.IfNot:
                    return "== false";
                case AnimatorConditionMode.Greater:
                    return $"> {condition.threshold}";
                case AnimatorConditionMode.Less:
                    return $"< {condition.threshold}";
                case AnimatorConditionMode.Equals:
                    return $"== {condition.threshold}";
                case AnimatorConditionMode.NotEqual:
                    return $"!= {condition.threshold}";
                default:
                    return condition.mode.ToString();
            }
        }

        private void ShowGameEventSection(string eventName, string parameter, params string[] animationNames)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label($"{eventName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.LabelField("Parameter:", parameter, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Triggers:", string.Join(", ", animationNames), EditorStyles.miniLabel);
            
            // Show code reference
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Code Location:", GetCodeLocation(eventName), EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private string GetCodeLocation(string eventName)
        {
            switch (eventName)
            {
                case "WeaponSystem.OnWeaponFired":
                    return "WeaponSystem.cs:581 → BattleRoyaleAnimationController.OnWeaponFired()";
                case "WeaponSystem.OnReloadStarted":
                    return "WeaponSystem.cs:1456 → BattleRoyaleAnimationController.OnReloadStarted()";
                case "FPSController Movement":
                    return "FPSController.cs → BattleRoyaleAnimationController.UpdateMovementAnimation()";
                case "CharacterController.isGrounded":
                    return "FPSController.cs → BattleRoyaleAnimationController.UpdateGroundedState()";
                default:
                    return "N/A";
            }
        }

        private void ShowAnimatorStates()
        {
            if (animatorController == null) return;

            AnimatorController controller = animatorController as AnimatorController;
            if (controller == null) return;

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (var state in rootStateMachine.states)
            {
                AnimationClip clip = state.state.motion as AnimationClip;
                string clipName = clip != null ? clip.name : "None";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.Label($"{state.state.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Animation:", clipName, EditorStyles.miniLabel);
                
                if (state.state.transitions.Length > 0)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Transitions:", EditorStyles.miniLabel);
                    
                    foreach (var transition in state.state.transitions)
                    {
                        string target = transition.destinationState != null ? transition.destinationState.name : "Exit";
                        string conditions = "";
                        
                        foreach (var condition in transition.conditions)
                        {
                            if (!string.IsNullOrEmpty(conditions)) conditions += " AND ";
                            conditions += $"{condition.parameter} {GetConditionString(condition)}";
                        }
                        
                        if (transition.hasExitTime)
                        {
                            if (!string.IsNullOrEmpty(conditions)) conditions += " OR ";
                            conditions += "ExitTime";
                        }
                        
                        EditorGUILayout.LabelField($"  → {target}", $"({conditions})", EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }
    }

    public class AnimationTriggerInfo
    {
        public string FromState;
        public string ToState;
        public string Parameter;
        public string Condition;
        public bool HasExitTime;
    }
}

