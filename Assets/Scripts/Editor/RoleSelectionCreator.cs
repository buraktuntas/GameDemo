using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// PROFESYONEL Role Selection UI Creator
    /// EventSystem, GraphicRaycaster, Canvas sorting - HEPSİ DAHIL
    /// </summary>
    public class RoleSelectionCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create Role Selection UI (PROFESSIONAL)")]
        static void ShowWindow()
        {
            CreateRoleSelectionUI();
        }

        static void CreateRoleSelectionUI()
        {
            // Clean up old version
            RoleSelectionUI existing = FindFirstObjectByType<RoleSelectionUI>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Role Selection Exists",
                    "Role Selection UI already exists. Delete and recreate?",
                    "Yes, Replace",
                    "Cancel");

                if (!replace) return;

                DestroyImmediate(existing.gameObject);
            }

            Debug.Log("🎨 Creating PROFESSIONAL Role Selection UI...");

            // STEP 1: Ensure EventSystem exists
            EnsureEventSystem();

            // STEP 2: Find or create Canvas
            Canvas mainCanvas = FindOrCreateCanvas();

            // STEP 3: Create Role Selection UI with HIGH sorting order
            GameObject roleSelectionObj = new GameObject("RoleSelectionUI");
            roleSelectionObj.transform.SetParent(mainCanvas.transform, false);

            // Add RectTransform
            RectTransform roleRect = roleSelectionObj.AddComponent<RectTransform>();
            roleRect.anchorMin = Vector2.zero;
            roleRect.anchorMax = Vector2.one;
            roleRect.sizeDelta = Vector2.zero;
            roleRect.anchoredPosition = Vector2.zero;

            // STEP 4: Create BLOCKING background
            GameObject blocker = CreateFullScreenBlocker(roleSelectionObj);

            // STEP 5: Create the actual UI panel
            GameObject panel = CreateMainPanel(roleSelectionObj);

            // STEP 6: Add UI elements
            CreateTitle(panel);
            var buttons = CreateRoleButtons(panel);
            var descText = CreateDescriptionArea(panel);
            var selectedText = CreateSelectedText(panel);
            var confirmBtn = CreateConfirmButton(panel);

            // STEP 7: Add RoleSelectionUI component
            RoleSelectionUI roleSelection = roleSelectionObj.AddComponent<RoleSelectionUI>();

            // STEP 8: Assign references
            SerializedObject so = new SerializedObject(roleSelection);
            so.FindProperty("selectionPanel").objectReferenceValue = panel;
            so.FindProperty("builderButton").objectReferenceValue = buttons[0];
            so.FindProperty("guardianButton").objectReferenceValue = buttons[1];
            so.FindProperty("rangerButton").objectReferenceValue = buttons[2];
            so.FindProperty("saboteurButton").objectReferenceValue = buttons[3];
            so.FindProperty("roleDescriptionText").objectReferenceValue = descText;
            so.FindProperty("selectedRoleText").objectReferenceValue = selectedText;
            so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
            so.ApplyModifiedProperties();

            // STEP 9: Set as last sibling (render on top)
            roleSelectionObj.transform.SetAsLastSibling();

            Debug.Log("✅ PROFESSIONAL Role Selection UI created!");
            Debug.Log("   - EventSystem: ✓");
            Debug.Log("   - GraphicRaycaster: ✓");
            Debug.Log("   - Fullscreen Blocker: ✓");
            Debug.Log("   - Sorting Order: TOP");

            EditorUtility.DisplayDialog("Success!",
                "Professional Role Selection UI created!\n\n" +
                "✓ EventSystem ready\n" +
                "✓ Buttons clickable\n" +
                "✓ Fullscreen blocker\n" +
                "✓ Sorted on top",
                "OK");

            Selection.activeGameObject = roleSelectionObj;
        }

        static void EnsureEventSystem()
        {
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ EventSystem created");
            }
            else
            {
                Debug.Log("✅ EventSystem already exists");
            }
        }

        static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("✅ Canvas created");
            }
            else
            {
                // Ensure GraphicRaycaster exists
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("✅ GraphicRaycaster added to existing Canvas");
                }
            }

            return canvas;
        }

        static GameObject CreateFullScreenBlocker(GameObject parent)
        {
            GameObject blocker = new GameObject("Blocker");
            blocker.transform.SetParent(parent.transform, false);

            RectTransform rect = blocker.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = blocker.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black

            // Add button to block clicks
            Button btn = blocker.AddComponent<Button>();
            btn.interactable = false; // Just blocks, doesn't click

            return blocker;
        }

        static GameObject CreateMainPanel(GameObject parent)
        {
            GameObject panel = new GameObject("SelectionPanel");
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1000, 700);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

            return panel;
        }

        static void CreateTitle(GameObject parent)
        {
            GameObject title = new GameObject("Title");
            title.transform.SetParent(parent.transform, false);

            RectTransform rect = title.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(900, 80);

            TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
            text.text = "SELECT YOUR ROLE";
            text.fontSize = 48;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
        }

        static Button[] CreateRoleButtons(GameObject parent)
        {
            Button[] buttons = new Button[4];

            // Grid layout
            Vector2[] positions = new Vector2[]
            {
                new Vector2(-250, 150),  // Builder
                new Vector2(250, 150),   // Guardian
                new Vector2(-250, 0),    // Ranger
                new Vector2(250, 0)      // Saboteur
            };

            string[] names = { "Builder", "Guardian", "Ranger", "Saboteur" };
            Color[] colors = {
                new Color(0.3f, 0.6f, 1f),    // Blue
                new Color(0.8f, 0.5f, 0.2f),  // Orange
                new Color(0.2f, 0.8f, 0.3f),  // Green
                new Color(0.7f, 0.2f, 0.7f)   // Purple
            };

            for (int i = 0; i < 4; i++)
            {
                buttons[i] = CreateRoleButton(parent, names[i] + "Button", names[i], positions[i], colors[i]);
            }

            return buttons;
        }

        static Button CreateRoleButton(GameObject parent, string objName, string text, Vector2 pos, Color color)
        {
            GameObject btn = new GameObject(objName);
            btn.transform.SetParent(parent.transform, false);

            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 120);

            Image img = btn.AddComponent<Image>();
            img.color = color;

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text.ToUpper();
            tmpText.fontSize = 28;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        static TextMeshProUGUI CreateDescriptionArea(GameObject parent)
        {
            GameObject descPanel = new GameObject("DescriptionPanel");
            descPanel.transform.SetParent(parent.transform, false);

            RectTransform rect = descPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -180);
            rect.sizeDelta = new Vector2(900, 200);

            Image img = descPanel.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            // Text
            GameObject textObj = new GameObject("DescriptionText");
            textObj.transform.SetParent(descPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = new Vector2(-40, -20);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Select a role to see description...";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;

            return text;
        }

        static TextMeshProUGUI CreateSelectedText(GameObject parent)
        {
            GameObject textObj = new GameObject("SelectedRoleText");
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 80);
            rect.sizeDelta = new Vector2(800, 40);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Selected: Builder";
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.8f, 0.2f); // Gold
            text.alignment = TextAlignmentOptions.Center;

            return text;
        }

        static Button CreateConfirmButton(GameObject parent)
        {
            GameObject btn = new GameObject("ConfirmButton");
            btn.transform.SetParent(parent.transform, false);

            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(300, 60);

            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.2f);

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CONFIRM";
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            return button;
        }
    }
}
