using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ BATTLE ROYALE ANIMATOR STATE SETUP: Animator Controller'a animasyon state'lerini ve transition'ları ekler
    /// </summary>
    public class BattleRoyaleAnimatorStateSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Setup Battle Royale Animator States")]
        public static void ShowWindow()
        {
            GetWindow<BattleRoyaleAnimatorStateSetup>("Battle Royale Animator State Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Battle Royale Animator State Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Animator Controller'a animasyon state'lerini ve transition'ları ekler:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            GUILayout.Label("⚠️ ÖNEMLİ: Bu tool Animator Controller'ı yeniden yapılandırır!", EditorStyles.wordWrappedLabel);
            GUILayout.Label("Mevcut state'ler ve transition'lar silinebilir!", EditorStyles.wordWrappedLabel);
            GUILayout.Space(20);

            if (GUILayout.Button("Setup AssaultRifle Animator States", GUILayout.Height(30)))
            {
                SetupAnimatorStates("Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller", "AssaultRifleStance");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Pistol Animator States", GUILayout.Height(30)))
            {
                SetupAnimatorStates("Assets/BattleRoyaleDuoPAPBR/Animator/PistolAnimator.controller", "HandGunStance");
            }
        }

        private static void SetupAnimatorStates(string controllerPath, string animationFolder)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", $"Animator Controller not found at: {controllerPath}", "OK");
                Debug.LogError($"❌ Animator Controller not found: {controllerPath}");
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
            Debug.Log($"🔧 SETTING UP STATES: {controller.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Get root state machine
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Clear existing states (optional - comment out if you want to keep existing states)
            // rootStateMachine.states = new ChildAnimatorState[0];
            // rootStateMachine.stateMachines = new ChildAnimatorStateMachine[0];

            // Find animation clips
            string animationPath = $"Assets/BattleRoyaleDuoPAPBR/Animation/{animationFolder}";
            Dictionary<string, AnimationClip> animations = FindAnimationClips(animationPath);

            // Create states
            AnimatorState idleState = CreateStateIfNotExists(rootStateMachine, "Idle", animations, "IdleNormal01");
            AnimatorState walkState = CreateStateIfNotExists(rootStateMachine, "Walk", animations, "RunFWD");
            AnimatorState runState = CreateStateIfNotExists(rootStateMachine, "Run", animations, "RunFWD_RM");
            AnimatorState shootState = CreateStateIfNotExists(rootStateMachine, "Shoot", animations, "ShootSingleshot");
            AnimatorState reloadState = CreateStateIfNotExists(rootStateMachine, "Reload", animations, "Reloading");
            AnimatorState jumpState = CreateStateIfNotExists(rootStateMachine, "Jump", animations, "Jump");

            // Set default state
            if (idleState != null)
            {
                rootStateMachine.defaultState = idleState;
                Debug.Log("✅ Default state set to: Idle");
            }

            // Create transitions
            CreateTransition(idleState, walkState, "Speed", 0.1f, true);
            CreateTransition(walkState, idleState, "Speed", 0.1f, false);
            CreateTransition(walkState, runState, "Speed", 4f, true);
            CreateTransition(runState, walkState, "Speed", 4f, false);
            CreateTransition(idleState, shootState, "TriggerFire", 0f, true);
            CreateTransition(walkState, shootState, "TriggerFire", 0f, true);
            CreateTransition(runState, shootState, "TriggerFire", 0f, true);
            CreateTransition(shootState, idleState, null, 0f, true, true); // Exit time transition
            CreateTransition(idleState, reloadState, "TriggerReload", 0f, true);
            CreateTransition(walkState, reloadState, "TriggerReload", 0f, true);
            CreateTransition(reloadState, idleState, "IsReloading", 0f, false, true); // Exit time transition
            CreateTransition(idleState, jumpState, null, 0f, true, true); // Exit time transition (will need jump trigger)
            CreateTransition(jumpState, idleState, "IsGrounded", 0f, true, true); // Exit time transition

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"\n✅ {controller.name} states and transitions setup complete!");
            Debug.Log($"═══════════════════════════════════════════════════════\n");
            EditorUtility.DisplayDialog("Success", "Animator states and transitions setup complete!", "OK");
        }

        private static Dictionary<string, AnimationClip> FindAnimationClips(string folderPath)
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

            Debug.Log($"✅ Found {clips.Count} animation clips in {folderPath}");
            return clips;
        }

        private static AnimatorState CreateStateIfNotExists(AnimatorStateMachine stateMachine, string stateName, Dictionary<string, AnimationClip> animations, string animationName)
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

            // Try to find matching animation
            AnimationClip clip = null;
            if (animations.ContainsKey(animationName))
            {
                clip = animations[animationName];
            }
            else
            {
                // Try to find by partial name match
                foreach (var kvp in animations)
                {
                    if (kvp.Key.Contains(animationName) || animationName.Contains(kvp.Key))
                    {
                        clip = kvp.Value;
                        break;
                    }
                }
            }

            if (clip != null)
            {
                newState.motion = clip;
                Debug.Log($"✅ Created state '{stateName}' with animation '{clip.name}'");
            }
            else
            {
                Debug.LogWarning($"⚠️ State '{stateName}' created but animation '{animationName}' not found");
            }

            return newState;
        }

        private static void CreateTransition(AnimatorState from, AnimatorState to, string parameterName, float threshold, bool greaterThan, bool useExitTime = false)
        {
            if (from == null || to == null) return;

            // Check if transition already exists
            foreach (var existingTransition in from.transitions)
            {
                if (existingTransition.destinationState == to)
                {
                    // Check if conditions match
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
                        Debug.Log($"ℹ️ Transition from '{from.name}' to '{to.name}' already exists");
                        return;
                    }
                }
            }

            // Create new transition
            AnimatorStateTransition newTransition = from.AddTransition(to);
            newTransition.hasExitTime = useExitTime || parameterName == null; // Use exit time if no parameter or explicitly requested
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

            Debug.Log($"✅ Created transition: {from.name} -> {to.name} (Condition: {parameterName})");
        }
    }
}

