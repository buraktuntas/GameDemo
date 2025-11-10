using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Debug tool to find UI elements blocking the crosshair
    /// </summary>
    public class UIDebugger : EditorWindow
    {
        [MenuItem("Tools/Debug UI Overlaps")]
        public static void ShowWindow()
        {
            GetWindow<UIDebugger>("UI Debugger");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Elements in Scene", EditorStyles.boldLabel);

            if (GUILayout.Button("List All Active UI Panels"))
            {
                ListActiveUIPanels();
            }

            if (GUILayout.Button("Find GameHUD Panels"))
            {
                FindGameHUDPanels();
            }

            if (GUILayout.Button("Check Canvas Sorting Orders"))
            {
                CheckCanvasSortingOrders();
            }
        }

        private void ListActiveUIPanels()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"=== Found {canvases.Length} Canvases ===");

            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.activeInHierarchy)
                {
                    Debug.Log($"✅ ACTIVE Canvas: {canvas.name} | Sorting Order: {canvas.sortingOrder} | Render Mode: {canvas.renderMode}");

                    // List all active children
                    var transforms = canvas.GetComponentsInChildren<Transform>(false); // false = only active
                    foreach (var t in transforms)
                    {
                        if (t != canvas.transform && t.gameObject.activeInHierarchy)
                        {
                            var rectTransform = t.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                Vector2 pos = rectTransform.anchoredPosition;
                                Vector2 size = rectTransform.sizeDelta;
                                Debug.Log($"  → {t.name} | Pos: {pos} | Size: {size} | Active: {t.gameObject.activeSelf}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"❌ INACTIVE Canvas: {canvas.name}");
                }
            }
        }

        private void FindGameHUDPanels()
        {
            var gameHUD = FindFirstObjectByType<TacticalCombat.UI.GameHUD>();
            if (gameHUD == null)
            {
                Debug.LogWarning("⚠️ GameHUD not found in scene!");
                return;
            }

            Debug.Log("=== GameHUD Panels Status ===");

            // Use reflection to get all SerializeField GameObject panels
            var fields = typeof(TacticalCombat.UI.GameHUD).GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.Contains("Panel"))
                {
                    var panel = field.GetValue(gameHUD) as GameObject;
                    if (panel != null)
                    {
                        string status = panel.activeSelf ? "✅ ACTIVE" : "❌ Inactive";
                        Debug.Log($"{status}: {field.Name} ({panel.name})");

                        // If active, show position
                        if (panel.activeSelf)
                        {
                            var rectTransform = panel.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                Debug.Log($"    Position: {rectTransform.anchoredPosition}");
                                Debug.Log($"    Size: {rectTransform.sizeDelta}");
                                Debug.Log($"    Anchors: Min{rectTransform.anchorMin} Max{rectTransform.anchorMax}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {field.Name} is null!");
                    }
                }
            }
        }

        private void CheckCanvasSortingOrders()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log("=== Canvas Sorting Orders ===");

            System.Array.Sort(canvases, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

            foreach (var canvas in canvases)
            {
                string activeStatus = canvas.gameObject.activeInHierarchy ? "✅" : "❌";
                Debug.Log($"{activeStatus} {canvas.name} | Sorting Order: {canvas.sortingOrder} | Render Mode: {canvas.renderMode}");
            }
        }
    }
}
