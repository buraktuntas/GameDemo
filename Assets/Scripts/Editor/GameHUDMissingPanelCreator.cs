using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Creates missing GameHUD panels and organizes UI hierarchy
    /// </summary>
    public class GameHUDMissingPanelCreator : EditorWindow
    {
        [MenuItem("Tools/Create Missing GameHUD Panels")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog(
                "Create Missing GameHUD Panels",
                "This will:\n\n" +
                "1. Create missing panel GameObjects (controlPointPanel, sabotagePanel)\n" +
                "2. Move existing UI elements under these panels\n" +
                "3. Set proper anchors and positions\n\n" +
                "This will fix the crosshair blocking issue!\n\n" +
                "Continue?",
                "Yes, Create Panels",
                "Cancel"))
            {
                CreateMissingPanels();
            }
        }

        private static void CreateMissingPanels()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Error", "GameHUD not found in scene!", "OK");
                return;
            }

            Undo.RecordObject(gameHUD.gameObject, "Create Missing Panels");

            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            int createdCount = 0;
            int fixedCount = 0;

            // ========== FIX CONTROL POINT PANEL ==========
            GameObject controlPointPanel = GetFieldValue(fields, gameHUD, "controlPointPanel");
            var controlPointText = GetComponentField<TMPro.TextMeshProUGUI>(fields, gameHUD, "controlPointText");
            var controlPointBar = GetComponentField<Slider>(fields, gameHUD, "controlPointBar");

            if (controlPointPanel == null && (controlPointText != null || controlPointBar != null))
            {
                // Create panel
                controlPointPanel = new GameObject("controlPointPanel");
                controlPointPanel.transform.SetParent(gameHUD.transform);

                var rectTransform = controlPointPanel.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(0, -80);
                rectTransform.sizeDelta = new Vector2(200, 50);

                // Assign to GameHUD
                SetFieldValue(fields, gameHUD, "controlPointPanel", controlPointPanel);

                Undo.RegisterCreatedObjectUndo(controlPointPanel, "Create controlPointPanel");
                Debug.Log("✅ Created controlPointPanel");
                createdCount++;

                // Move children
                if (controlPointText != null)
                {
                    Undo.SetTransformParent(controlPointText.transform, controlPointPanel.transform, "Move controlPointText");
                    Debug.Log("  ✅ Moved controlPointText under controlPointPanel");
                    fixedCount++;
                }
                if (controlPointBar != null)
                {
                    Undo.SetTransformParent(controlPointBar.transform, controlPointPanel.transform, "Move controlPointBar");
                    Debug.Log("  ✅ Moved controlPointBar under controlPointPanel");
                    fixedCount++;
                }
            }

            // ========== FIX SABOTAGE PANEL ==========
            GameObject sabotagePanel = GetFieldValue(fields, gameHUD, "sabotagePanel");
            var sabotageProgressBar = GetComponentField<Slider>(fields, gameHUD, "sabotageProgressBar");

            if (sabotagePanel == null && sabotageProgressBar != null)
            {
                // Create panel
                sabotagePanel = new GameObject("sabotagePanel");
                sabotagePanel.transform.SetParent(gameHUD.transform);

                var rectTransform = sabotagePanel.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(0, -120);
                rectTransform.sizeDelta = new Vector2(200, 20);

                // Assign to GameHUD
                SetFieldValue(fields, gameHUD, "sabotagePanel", sabotagePanel);

                Undo.RegisterCreatedObjectUndo(sabotagePanel, "Create sabotagePanel");
                Debug.Log("✅ Created sabotagePanel");
                createdCount++;

                // Move children
                if (sabotageProgressBar != null)
                {
                    Undo.SetTransformParent(sabotageProgressBar.transform, sabotagePanel.transform, "Move sabotageProgressBar");
                    Debug.Log("  ✅ Moved sabotageProgressBar under sabotagePanel");
                    fixedCount++;
                }
            }

            // ========== CHECK BUILD FEEDBACK PANEL ==========
            GameObject buildFeedbackPanel = GetFieldValue(fields, gameHUD, "buildFeedbackPanel");
            var buildFeedbackText = GetComponentField<TMPro.TextMeshProUGUI>(fields, gameHUD, "buildFeedbackText");

            if (buildFeedbackPanel == null && buildFeedbackText != null)
            {
                // Create panel
                buildFeedbackPanel = new GameObject("buildFeedbackPanel");
                buildFeedbackPanel.transform.SetParent(gameHUD.transform);

                var rectTransform = buildFeedbackPanel.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = new Vector2(0, 120);
                rectTransform.sizeDelta = new Vector2(200, 50);

                // Assign to GameHUD
                SetFieldValue(fields, gameHUD, "buildFeedbackPanel", buildFeedbackPanel);

                Undo.RegisterCreatedObjectUndo(buildFeedbackPanel, "Create buildFeedbackPanel");
                Debug.Log("✅ Created buildFeedbackPanel");
                createdCount++;

                // Move children
                if (buildFeedbackText != null)
                {
                    Undo.SetTransformParent(buildFeedbackText.transform, buildFeedbackPanel.transform, "Move buildFeedbackText");
                    Debug.Log("  ✅ Moved buildFeedbackText under buildFeedbackPanel");
                    fixedCount++;
                }
            }

            // ========== DISABLE ALL PANELS ==========
            if (controlPointPanel != null) controlPointPanel.SetActive(false);
            if (sabotagePanel != null) sabotagePanel.SetActive(false);
            if (buildFeedbackPanel != null) buildFeedbackPanel.SetActive(false);

            EditorUtility.SetDirty(gameHUD);
            EditorUtility.SetDirty(gameHUD.gameObject);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            if (createdCount == 0 && fixedCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Already Fixed",
                    "All panels already exist and are properly organized!",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Created {createdCount} panels!\n" +
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

        private static void SetFieldValue(System.Reflection.FieldInfo[] fields, object obj, string fieldName, GameObject value)
        {
            foreach (var field in fields)
            {
                if (field.Name == fieldName && field.FieldType == typeof(GameObject))
                {
                    field.SetValue(obj, value);
                    return;
                }
            }
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
