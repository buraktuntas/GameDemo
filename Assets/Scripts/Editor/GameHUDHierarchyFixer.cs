using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Fixes GameHUD hierarchy by making UI elements children of their parent panels
    /// </summary>
    public class GameHUDHierarchyFixer : EditorWindow
    {
        [MenuItem("Tools/Fix GameHUD Hierarchy")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog(
                "Fix GameHUD Hierarchy",
                "This will reorganize GameHUD UI elements:\n\n" +
                "• Make Slider/Text children of their parent panels\n" +
                "• infoTowerHackSlider → child of infoTowerHackPanel\n" +
                "• infoTowerHackText → child of infoTowerHackPanel\n" +
                "• sabotageProgressBar → child of sabotagePanel\n" +
                "• controlPointText → child of controlPointPanel\n" +
                "• controlPointBar → child of controlPointPanel\n" +
                "• buildFeedbackText → child of buildFeedbackPanel\n" +
                "• hitMarkerImage → child of hitMarkerPanel\n" +
                "• headshotMarkerImage → child of hitMarkerPanel\n\n" +
                "This will fix the crosshair blocking issue!\n\n" +
                "Continue?",
                "Yes, Fix Hierarchy",
                "Cancel"))
            {
                FixHierarchy();
            }
        }

        private static void FixHierarchy()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Error", "GameHUD not found in scene!", "OK");
                return;
            }

            Undo.RecordObject(gameHUD.gameObject, "Fix GameHUD Hierarchy");

            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            int fixedCount = 0;

            // Get all panel references
            GameObject infoTowerHackPanel = GetFieldValue(fields, gameHUD, "infoTowerHackPanel");
            GameObject sabotagePanel = GetFieldValue(fields, gameHUD, "sabotagePanel");
            GameObject controlPointPanel = GetFieldValue(fields, gameHUD, "controlPointPanel");
            GameObject buildFeedbackPanel = GetFieldValue(fields, gameHUD, "buildFeedbackPanel");
            GameObject hitMarkerPanel = GetFieldValue(fields, gameHUD, "hitMarkerPanel");

            // Get all child element references
            var infoTowerHackSlider = GetComponentField<UnityEngine.UI.Slider>(fields, gameHUD, "infoTowerHackSlider");
            var infoTowerHackText = GetComponentField<TMPro.TextMeshProUGUI>(fields, gameHUD, "infoTowerHackText");
            var sabotageProgressBar = GetComponentField<UnityEngine.UI.Slider>(fields, gameHUD, "sabotageProgressBar");
            var controlPointText = GetComponentField<TMPro.TextMeshProUGUI>(fields, gameHUD, "controlPointText");
            var controlPointBar = GetComponentField<UnityEngine.UI.Slider>(fields, gameHUD, "controlPointBar");
            var buildFeedbackText = GetComponentField<TMPro.TextMeshProUGUI>(fields, gameHUD, "buildFeedbackText");
            var hitMarkerImage = GetComponentField<UnityEngine.UI.Image>(fields, gameHUD, "hitMarkerImage");
            var headshotMarkerImage = GetComponentField<UnityEngine.UI.Image>(fields, gameHUD, "headshotMarkerImage");

            // Fix InfoTower hierarchy
            if (infoTowerHackPanel != null)
            {
                if (infoTowerHackSlider != null && infoTowerHackSlider.transform.parent != infoTowerHackPanel.transform)
                {
                    Undo.SetTransformParent(infoTowerHackSlider.transform, infoTowerHackPanel.transform, "Set InfoTowerSlider parent");
                    Debug.Log("✅ infoTowerHackSlider → child of infoTowerHackPanel");
                    fixedCount++;
                }
                if (infoTowerHackText != null && infoTowerHackText.transform.parent != infoTowerHackPanel.transform)
                {
                    Undo.SetTransformParent(infoTowerHackText.transform, infoTowerHackPanel.transform, "Set InfoTowerText parent");
                    Debug.Log("✅ infoTowerHackText → child of infoTowerHackPanel");
                    fixedCount++;
                }
            }

            // Fix Sabotage hierarchy
            if (sabotagePanel != null && sabotageProgressBar != null)
            {
                if (sabotageProgressBar.transform.parent != sabotagePanel.transform)
                {
                    Undo.SetTransformParent(sabotageProgressBar.transform, sabotagePanel.transform, "Set SabotageBar parent");
                    Debug.Log("✅ sabotageProgressBar → child of sabotagePanel");
                    fixedCount++;
                }
            }

            // Fix ControlPoint hierarchy
            if (controlPointPanel != null)
            {
                if (controlPointText != null && controlPointText.transform.parent != controlPointPanel.transform)
                {
                    Undo.SetTransformParent(controlPointText.transform, controlPointPanel.transform, "Set ControlPointText parent");
                    Debug.Log("✅ controlPointText → child of controlPointPanel");
                    fixedCount++;
                }
                if (controlPointBar != null && controlPointBar.transform.parent != controlPointPanel.transform)
                {
                    Undo.SetTransformParent(controlPointBar.transform, controlPointPanel.transform, "Set ControlPointBar parent");
                    Debug.Log("✅ controlPointBar → child of controlPointPanel");
                    fixedCount++;
                }
            }

            // Fix BuildFeedback hierarchy
            if (buildFeedbackPanel != null && buildFeedbackText != null)
            {
                if (buildFeedbackText.transform.parent != buildFeedbackPanel.transform)
                {
                    Undo.SetTransformParent(buildFeedbackText.transform, buildFeedbackPanel.transform, "Set BuildFeedbackText parent");
                    Debug.Log("✅ buildFeedbackText → child of buildFeedbackPanel");
                    fixedCount++;
                }
            }

            // Fix HitMarker hierarchy
            if (hitMarkerPanel != null)
            {
                if (hitMarkerImage != null && hitMarkerImage.transform.parent != hitMarkerPanel.transform)
                {
                    Undo.SetTransformParent(hitMarkerImage.transform, hitMarkerPanel.transform, "Set HitMarkerImage parent");
                    Debug.Log("✅ hitMarkerImage → child of hitMarkerPanel");
                    fixedCount++;
                }
                if (headshotMarkerImage != null && headshotMarkerImage.transform.parent != hitMarkerPanel.transform)
                {
                    Undo.SetTransformParent(headshotMarkerImage.transform, hitMarkerPanel.transform, "Set HeadshotMarkerImage parent");
                    Debug.Log("✅ headshotMarkerImage → child of hitMarkerPanel");
                    fixedCount++;
                }
            }

            EditorUtility.SetDirty(gameHUD.gameObject);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            if (fixedCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Already Fixed",
                    "Hierarchy is already correct!\n\nAll UI elements are properly organized.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Fixed {fixedCount} UI elements!\n\n" +
                    "✅ Now save the scene (Ctrl+S)\n" +
                    "✅ Test in Play mode - crosshair should be clean!",
                    "OK"
                );
            }
        }

        private static GameObject GetFieldValue(System.Reflection.FieldInfo[] fields, object obj, string fieldName)
        {
            foreach (var field in fields)
            {
                if (field.Name == fieldName && field.FieldType == typeof(GameObject))
                {
                    return field.GetValue(obj) as GameObject;
                }
            }
            return null;
        }

        private static T GetComponentField<T>(System.Reflection.FieldInfo[] fields, object obj, string fieldName) where T : Component
        {
            foreach (var field in fields)
            {
                if (field.Name == fieldName && field.FieldType == typeof(T))
                {
                    return field.GetValue(obj) as T;
                }
            }
            return null;
        }
    }
}
