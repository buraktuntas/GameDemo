using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Analyzes ALL GameHUD UI elements for overlapping positions
    /// </summary>
    public class GameHUDCompleteLayoutAnalyzer : EditorWindow
    {
        [MenuItem("Tools/Analyze ALL GameHUD Layout")]
        public static void ShowWindow()
        {
            AnalyzeLayout();
        }

        private static void AnalyzeLayout()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Error", "GameHUD not found in scene!", "OK");
                return;
            }

            Debug.Log("═══════════════════════════════════════════════");
            Debug.Log("📊 COMPLETE GAMEHUD LAYOUT ANALYSIS");
            Debug.Log("═══════════════════════════════════════════════");

            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            var centerElements = new List<string>();
            var overlappingElements = new Dictionary<string, Rect>();

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject))
                {
                    var obj = field.GetValue(gameHUD) as GameObject;
                    if (obj != null)
                    {
                        var rectTransform = obj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            AnalyzeElement(field.Name, obj, rectTransform, centerElements, overlappingElements);
                        }
                    }
                }
                else if (typeof(UnityEngine.Component).IsAssignableFrom(field.FieldType))
                {
                    var component = field.GetValue(gameHUD) as UnityEngine.Component;
                    if (component != null)
                    {
                        var rectTransform = component.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            AnalyzeElement(field.Name, component.gameObject, rectTransform, centerElements, overlappingElements);
                        }
                    }
                }
            }

            // Summary
            Debug.Log("\n═══════════════════════════════════════════════");
            Debug.Log("🎯 ELEMENTS BLOCKING CENTER (Crosshair Area):");
            Debug.Log("═══════════════════════════════════════════════");
            if (centerElements.Count == 0)
            {
                Debug.Log("✅ No elements blocking center!");
            }
            else
            {
                foreach (var elem in centerElements)
                {
                    Debug.LogWarning($"❌ {elem}");
                }
            }

            Debug.Log("\n═══════════════════════════════════════════════");
            Debug.Log("⚠️ POTENTIALLY OVERLAPPING ELEMENTS:");
            Debug.Log("═══════════════════════════════════════════════");
            CheckOverlaps(overlappingElements);

            EditorUtility.DisplayDialog(
                "Analysis Complete",
                $"Found {centerElements.Count} elements blocking crosshair!\n\n" +
                "Check Console for detailed report.",
                "OK"
            );
        }

        private static void AnalyzeElement(string name, GameObject obj, RectTransform rect, List<string> centerElements, Dictionary<string, Rect> overlapping)
        {
            bool isActive = obj.activeInHierarchy;
            string status = isActive ? "✅ ACTIVE" : "❌ Inactive";

            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            Vector2 anchoredPos = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;

            // Check if element is in center (blocking crosshair)
            bool isCenter = IsCenterPosition(anchorMin, anchorMax, anchoredPos);

            string posDescription = GetPositionDescription(anchorMin, anchorMax, anchoredPos);

            Debug.Log($"{status}: {name}");
            Debug.Log($"    Position: {posDescription}");
            Debug.Log($"    Anchors: Min{anchorMin} Max{anchorMax}");
            Debug.Log($"    Offset: {anchoredPos}");
            Debug.Log($"    Size: {size}");
            Debug.Log($"    Parent: {(rect.parent != null ? rect.parent.name : "ROOT")}");

            if (isCenter && isActive)
            {
                centerElements.Add($"{name} - {posDescription}");
            }

            if (isActive)
            {
                // Calculate screen rect for overlap detection
                Rect screenRect = GetScreenRect(rect, anchorMin, anchorMax, anchoredPos, size);
                overlapping[name] = screenRect;
            }

            Debug.Log("───────────────────────────────────────────────");
        }

        private static bool IsCenterPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
        {
            // Check if anchored to center (0.5, 0.5)
            bool anchoredCenter = Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMin.y, 0.5f) &&
                                   Mathf.Approximately(anchorMax.x, 0.5f) && Mathf.Approximately(anchorMax.y, 0.5f);

            // Check if position is near center (within 200px)
            bool nearCenter = Mathf.Abs(anchoredPos.x) < 200 && Mathf.Abs(anchoredPos.y) < 200;

            return anchoredCenter && nearCenter;
        }

        private static string GetPositionDescription(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
        {
            // Determine position based on anchors
            if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMin.y, 0.5f))
            {
                return "CENTER";
            }
            else if (Mathf.Approximately(anchorMin.x, 0f) && Mathf.Approximately(anchorMin.y, 1f))
            {
                return "TOP-LEFT";
            }
            else if (Mathf.Approximately(anchorMin.x, 1f) && Mathf.Approximately(anchorMin.y, 1f))
            {
                return "TOP-RIGHT";
            }
            else if (Mathf.Approximately(anchorMin.x, 0f) && Mathf.Approximately(anchorMin.y, 0f))
            {
                return "BOTTOM-LEFT";
            }
            else if (Mathf.Approximately(anchorMin.x, 1f) && Mathf.Approximately(anchorMin.y, 0f))
            {
                return "BOTTOM-RIGHT";
            }
            else if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMin.y, 1f))
            {
                return "TOP-CENTER";
            }
            else if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMin.y, 0f))
            {
                return "BOTTOM-CENTER";
            }
            else
            {
                return $"CUSTOM ({anchoredPos.x}, {anchoredPos.y})";
            }
        }

        private static Rect GetScreenRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            // Simplified screen rect calculation (assumes 1920x1080)
            float screenWidth = 1920f;
            float screenHeight = 1080f;

            float centerX = screenWidth * anchorMin.x + anchoredPos.x;
            float centerY = screenHeight * anchorMin.y + anchoredPos.y;

            return new Rect(centerX - size.x / 2, centerY - size.y / 2, size.x, size.y);
        }

        private static void CheckOverlaps(Dictionary<string, Rect> elements)
        {
            var elementList = new List<KeyValuePair<string, Rect>>(elements);
            bool foundOverlap = false;

            for (int i = 0; i < elementList.Count; i++)
            {
                for (int j = i + 1; j < elementList.Count; j++)
                {
                    if (elementList[i].Value.Overlaps(elementList[j].Value))
                    {
                        Debug.LogWarning($"⚠️ OVERLAP: {elementList[i].Key} <-> {elementList[j].Key}");
                        foundOverlap = true;
                    }
                }
            }

            if (!foundOverlap)
            {
                Debug.Log("✅ No overlaps detected!");
            }
        }
    }
}
