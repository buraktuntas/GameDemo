using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ BATTLE ROYALE ANIMATOR SETUP: Animator Controller'a gerekli parametreleri otomatik ekler
    /// </summary>
    public class BattleRoyaleAnimatorSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Setup Battle Royale Animator Parameters")]
        public static void ShowWindow()
        {
            GetWindow<BattleRoyaleAnimatorSetup>("Battle Royale Animator Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Battle Royale Animator Parameter Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Battle Royale Animator Controller'larına gerekli parametreleri ekler:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            GUILayout.Label("Eklenecek Parametreler:", EditorStyles.boldLabel);
            GUILayout.Label("• Speed (Float) - Yürüme/koşma hızı", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• IsShooting (Bool) - Ateş etme durumu", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• IsReloading (Bool) - Mermi doldurma durumu", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• IsAiming (Bool) - Nişan alma durumu", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• IsGrounded (Bool) - Yerde olma durumu", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• TriggerFire (Trigger) - Ateş trigger'ı", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• TriggerReload (Trigger) - Reload trigger'ı", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            if (GUILayout.Button("Setup AssaultRifle Animator", GUILayout.Height(30)))
            {
                SetupAnimator("Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Pistol Animator", GUILayout.Height(30)))
            {
                SetupAnimator("Assets/BattleRoyaleDuoPAPBR/Animator/PistolAnimator.controller");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Both Animators", GUILayout.Height(30)))
            {
                SetupAnimator("Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller");
                SetupAnimator("Assets/BattleRoyaleDuoPAPBR/Animator/PistolAnimator.controller");
            }
        }

        private static void SetupAnimator(string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", $"Animator Controller not found at: {controllerPath}", "OK");
                Debug.LogError($"❌ Animator Controller not found: {controllerPath}");
                return;
            }

            bool modified = false;

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔧 SETTING UP: {controller.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Add parameters
            modified |= AddParameterIfNotExists(controller, "Speed", AnimatorControllerParameterType.Float);
            modified |= AddParameterIfNotExists(controller, "IsShooting", AnimatorControllerParameterType.Bool);
            modified |= AddParameterIfNotExists(controller, "IsReloading", AnimatorControllerParameterType.Bool);
            modified |= AddParameterIfNotExists(controller, "IsAiming", AnimatorControllerParameterType.Bool);
            modified |= AddParameterIfNotExists(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            modified |= AddParameterIfNotExists(controller, "TriggerFire", AnimatorControllerParameterType.Trigger);
            modified |= AddParameterIfNotExists(controller, "TriggerReload", AnimatorControllerParameterType.Trigger);

            if (modified)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"\n✅ {controller.name} updated with Battle Royale parameters!");
                Debug.Log($"═══════════════════════════════════════════════════════\n");
            }
            else
            {
                Debug.Log($"\nℹ️ {controller.name} already has all required parameters.");
            }
        }

        private static bool AddParameterIfNotExists(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType)
        {
            // Check if parameter already exists
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                {
                    Debug.Log($"ℹ️ Parameter '{paramName}' already exists");
                    return false;
                }
            }

            // Add new parameter
            controller.AddParameter(paramName, paramType);
            Debug.Log($"✅ Added parameter: {paramName} ({paramType})");
            return true;
        }
    }
}

