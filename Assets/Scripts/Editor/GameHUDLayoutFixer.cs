using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Automatically fixes GameHUD panel positions to prevent crosshair blocking
    /// </summary>
    public class GameHUDLayoutFixer : EditorWindow
    {
        [MenuItem("Tools/Fix GameHUD Layout")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog(
                "Fix GameHUD Layout",
                "This will reposition all GameHUD panels to prevent crosshair blocking.\n\n" +
                "Changes:\n" +
                "• resourcePanel → Top-Left\n" +
                "• buildFeedbackPanel → Bottom-Center\n" +
                "• sabotagePanel → Top-Center\n" +
                "• controlPointPanel → Top-Center (below phase)\n" +
                "• coreCarryingPanel → Top-Right\n" +
                "• suddenDeathPanel → Center (but will auto-hide)\n" +
                "• abilityPanel → Bottom-Right\n" +
                "• hitMarkerPanel → Center (small, only shows briefly)\n\n" +
                "Continue?",
                "Yes, Fix Layout",
                "Cancel"))
            {
                FixLayout();
            }
        }

        private static void FixLayout()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Error", "GameHUD not found in scene!", "OK");
                return;
            }

            Undo.RecordObject(gameHUD.gameObject, "Fix GameHUD Layout");

            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            int fixedCount = 0;

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.Contains("Panel"))
                {
                    var panel = field.GetValue(gameHUD) as GameObject;
                    if (panel == null) continue;

                    var rectTransform = panel.GetComponent<RectTransform>();
                    if (rectTransform == null) continue;

                    Undo.RecordObject(rectTransform, "Fix Panel Position");

                    switch (field.Name)
                    {
                        case "resourcePanel":
                            // Top-Left corner
                            rectTransform.anchorMin = new Vector2(0, 1);
                            rectTransform.anchorMax = new Vector2(0, 1);
                            rectTransform.pivot = new Vector2(0, 1);
                            rectTransform.anchoredPosition = new Vector2(20, -20);
                            Debug.Log($"✅ Fixed: {field.Name} → Top-Left");
                            fixedCount++;
                            break;

                        case "buildFeedbackPanel":
                            // Bottom-Center
                            rectTransform.anchorMin = new Vector2(0.5f, 0);
                            rectTransform.anchorMax = new Vector2(0.5f, 0);
                            rectTransform.pivot = new Vector2(0.5f, 0);
                            rectTransform.anchoredPosition = new Vector2(0, 120); // Above ammo panel
                            Debug.Log($"✅ Fixed: {field.Name} → Bottom-Center");
                            fixedCount++;
                            break;

                        case "sabotagePanel":
                            // Top-Center (below timer)
                            rectTransform.anchorMin = new Vector2(0.5f, 1);
                            rectTransform.anchorMax = new Vector2(0.5f, 1);
                            rectTransform.pivot = new Vector2(0.5f, 1);
                            rectTransform.anchoredPosition = new Vector2(0, -120);
                            Debug.Log($"✅ Fixed: {field.Name} → Top-Center");
                            fixedCount++;
                            break;

                        case "controlPointPanel":
                            // Top-Center (below phase timer)
                            rectTransform.anchorMin = new Vector2(0.5f, 1);
                            rectTransform.anchorMax = new Vector2(0.5f, 1);
                            rectTransform.pivot = new Vector2(0.5f, 1);
                            rectTransform.anchoredPosition = new Vector2(0, -80);
                            Debug.Log($"✅ Fixed: {field.Name} → Top-Center");
                            fixedCount++;
                            break;

                        case "coreCarryingPanel":
                            // Top-Right
                            rectTransform.anchorMin = new Vector2(1, 1);
                            rectTransform.anchorMax = new Vector2(1, 1);
                            rectTransform.pivot = new Vector2(1, 1);
                            rectTransform.anchoredPosition = new Vector2(-20, -20);
                            Debug.Log($"✅ Fixed: {field.Name} → Top-Right");
                            fixedCount++;
                            break;

                        case "abilityPanel":
                            // Bottom-Right (above ammo)
                            rectTransform.anchorMin = new Vector2(1, 0);
                            rectTransform.anchorMax = new Vector2(1, 0);
                            rectTransform.pivot = new Vector2(1, 0);
                            rectTransform.anchoredPosition = new Vector2(-20, 120);
                            Debug.Log($"✅ Fixed: {field.Name} → Bottom-Right");
                            fixedCount++;
                            break;

                        case "suddenDeathPanel":
                            // Keep center but make it auto-hide (already done in code)
                            // Just ensure it starts hidden
                            panel.SetActive(false);
                            Debug.Log($"✅ Fixed: {field.Name} → Hidden by default");
                            fixedCount++;
                            break;

                        case "infoTowerHackPanel":
                            // Move slightly down from center (already at -162, good)
                            // Just ensure proper size
                            rectTransform.sizeDelta = new Vector2(300, 80); // Smaller height
                            Debug.Log($"✅ Fixed: {field.Name} → Adjusted size");
                            fixedCount++;
                            break;

                        case "hitMarkerPanel":
                            // Keep center but make much smaller
                            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                            rectTransform.pivot = new Vector2(0.5f, 0.5f);
                            rectTransform.anchoredPosition = Vector2.zero;
                            rectTransform.sizeDelta = new Vector2(64, 64); // Small, just for hit marker
                            Debug.Log($"✅ Fixed: {field.Name} → Center (small)");
                            fixedCount++;
                            break;

                        case "teamStatusPanel":
                        case "ammoPanel":
                            // Already positioned correctly (corners)
                            Debug.Log($"⏭️ Skipped: {field.Name} (already correct)");
                            break;
                    }

                    EditorUtility.SetDirty(rectTransform);
                }
            }

            EditorUtility.SetDirty(gameHUD.gameObject);
            EditorUtility.DisplayDialog(
                "Success",
                $"Fixed {fixedCount} panels!\n\nCheck the Scene view to verify positions.",
                "OK"
            );
        }
    }
}
