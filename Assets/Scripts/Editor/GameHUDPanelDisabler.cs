using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Disables GameHUD panels that should start hidden
    /// </summary>
    public class GameHUDPanelDisabler : EditorWindow
    {
        [MenuItem("Tools/Disable GameHUD Panels")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog(
                "Disable GameHUD Panels",
                "This will disable all conditional panels that should start hidden:\n\n" +
                "• hitMarkerPanel\n" +
                "• sabotagePanel\n" +
                "• infoTowerHackPanel\n" +
                "• buildFeedbackPanel\n" +
                "• coreCarryingPanel\n" +
                "• suddenDeathPanel\n" +
                "• controlPointPanel\n\n" +
                "These panels will be enabled by code when needed.\n\n" +
                "Continue?",
                "Yes, Disable Panels",
                "Cancel"))
            {
                DisablePanels();
            }
        }

        private static void DisablePanels()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Error", "GameHUD not found in scene!", "OK");
                return;
            }

            Undo.RecordObject(gameHUD.gameObject, "Disable GameHUD Panels");

            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            int disabledCount = 0;

            // Panels that should start hidden
            string[] panelsToDisable = new string[]
            {
                "hitMarkerPanel",
                "sabotagePanel",
                "infoTowerHackPanel",
                "buildFeedbackPanel",
                "coreCarryingPanel",
                "suddenDeathPanel",
                "controlPointPanel",
                "killFeedPanel",
                "headshotPanel",
                "respawnPanel"
            };

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && System.Array.IndexOf(panelsToDisable, field.Name) >= 0)
                {
                    var panel = field.GetValue(gameHUD) as GameObject;
                    if (panel != null)
                    {
                        Undo.RecordObject(panel, "Disable Panel");

                        if (panel.activeSelf)
                        {
                            panel.SetActive(false);
                            Debug.Log($"✅ Disabled: {field.Name}");
                            disabledCount++;
                        }
                        else
                        {
                            Debug.Log($"⏭️ Already disabled: {field.Name}");
                        }

                        EditorUtility.SetDirty(panel);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {field.Name} is null!");
                    }
                }
            }

            EditorUtility.SetDirty(gameHUD.gameObject);

            // Mark scene as dirty to save changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog(
                "Success",
                $"Disabled {disabledCount} panels!\n\n" +
                "✅ Now save the scene (Ctrl+S)\n" +
                "✅ If GameHUD is a prefab, apply changes to prefab",
                "OK"
            );
        }
    }
}
