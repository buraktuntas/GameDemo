using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ ANIMATION CHECKER: Animator Controller'da hangi animasyonların olduğunu kontrol eder
    /// </summary>
    public class AnimationChecker : EditorWindow
    {
        private AnimatorController animatorController;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Tactical Combat/Check Animations")]
        public static void ShowWindow()
        {
            GetWindow<AnimationChecker>("Animation Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Checker", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Animator Controller'daki animasyonları ve parametreleri gösterir:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Animator Controller
            GUILayout.Label("Animator Controller:", EditorStyles.boldLabel);
            animatorController = (AnimatorController)EditorGUILayout.ObjectField(
                animatorController,
                typeof(AnimatorController),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(animatorController == null);
            if (GUILayout.Button("Check Animations", GUILayout.Height(40)))
            {
                CheckAnimations();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Auto-find from Player prefab
            if (GUILayout.Button("Auto-Find from Player Prefab", GUILayout.Height(30)))
            {
                FindFromPlayerPrefab();
            }
        }

        private void CheckAnimations()
        {
            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animator Controller!", "OK");
                return;
            }

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 CHECKING ANIMATOR CONTROLLER: {animatorController.name}");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Check Parameters
            Debug.Log("📋 PARAMETERS:");
            if (animatorController.parameters.Length == 0)
            {
                Debug.LogWarning("  ⚠️ No parameters found!");
            }
            else
            {
                foreach (var param in animatorController.parameters)
                {
                    Debug.Log($"  ✅ {param.name} ({param.type})");
                }
            }

            Debug.Log("\n🎬 ANIMATION STATES:");
            
            // Get all states from all layers
            foreach (var layer in animatorController.layers)
            {
                Debug.Log($"\n  Layer: {layer.name}");
                
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

                // Check for default state
                if (layer.stateMachine.defaultState != null)
                {
                    Debug.Log($"  📌 Default State: {layer.stateMachine.defaultState.name}");
                }
            }

            // Check for common animations
            Debug.Log("\n🔍 CHECKING FOR COMMON ANIMATIONS:");
            CheckForAnimation(animatorController, "Idle", "Duran animasyon");
            CheckForAnimation(animatorController, "Walk", "Yürüme animasyonu");
            CheckForAnimation(animatorController, "Run", "Koşma animasyonu");
            CheckForAnimation(animatorController, "Jump", "Zıplama animasyonu");
            CheckForAnimation(animatorController, "Shoot", "Ateş etme animasyonu");
            CheckForAnimation(animatorController, "Reload", "Mermi doldurma animasyonu");

            Debug.Log("\n═══════════════════════════════════════════════════════\n");
        }

        private void CheckForAnimation(AnimatorController controller, string name, string description)
        {
            bool found = false;
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.motion != null && 
                        state.state.motion.name.Contains(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"  ✅ {description}: {state.state.motion.name}");
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                Debug.LogWarning($"  ⚠️ {description} bulunamadı!");
            }
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
            
            // Find Animator
            Animator animator = playerInstance.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController != null)
                {
                    Debug.Log($"✅ Found Animator Controller: {animatorController.name}");
                    EditorUtility.DisplayDialog("Found", 
                        $"Animator Controller found:\n{animatorController.name}",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", 
                        "Animator Controller is not an AnimatorController asset!\n" +
                        "It might be an override or runtime controller.",
                        "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", 
                    "No Animator or Animator Controller found on Player prefab!",
                    "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

