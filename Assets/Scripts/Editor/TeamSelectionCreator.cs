using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// PROFESSIONAL Team Selection Creator
    /// EventSystem, GraphicRaycaster - ALL INCLUDED
    /// </summary>
    public class TeamSelectionCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create Team Selection UI (PROFESSIONAL)")]
        static void ShowWindow()
        {
            CreateTeamSelectionUI();
        }

        static void CreateTeamSelectionUI()
        {
            Debug.Log("🎨 Creating PROFESSIONAL Team Selection UI...");

            // STEP 1: Ensure EventSystem exists
            EnsureEventSystem();

            // STEP 2: Find or create Canvas
            Canvas mainCanvas = FindOrCreateCanvas();

            // STEP 3: Create Team Selection UI
            GameObject teamSelectionObj = new GameObject("TeamSelectionUI");
            teamSelectionObj.transform.SetParent(mainCanvas.transform, false);

            RectTransform teamRect = teamSelectionObj.AddComponent<RectTransform>();
            teamRect.anchorMin = Vector2.zero;
            teamRect.anchorMax = Vector2.one;
            teamRect.sizeDelta = Vector2.zero;

            var teamSelectionScript = teamSelectionObj.AddComponent<TacticalCombat.UI.TeamSelectionUI>();

            // STEP 4: Create BLOCKING background
            GameObject blocker = CreateFullScreenBlocker(teamSelectionObj);

            // STEP 5: Create Selection Panel
            GameObject selectionPanel = CreateSelectionPanel(teamSelectionObj);

            // STEP 6: Assign references
            SerializedObject so = new SerializedObject(teamSelectionScript);
            so.FindProperty("selectionPanel").objectReferenceValue = selectionPanel;
            so.FindProperty("teamAButton").objectReferenceValue = selectionPanel.transform.Find("TeamAButton").GetComponent<Button>();
            so.FindProperty("teamBButton").objectReferenceValue = selectionPanel.transform.Find("TeamBButton").GetComponent<Button>();
            so.FindProperty("autoButton").objectReferenceValue = selectionPanel.transform.Find("AutoButton").GetComponent<Button>();
            so.FindProperty("teamACountText").objectReferenceValue = selectionPanel.transform.Find("TeamAButton/CountText").GetComponent<TextMeshProUGUI>();
            so.FindProperty("teamBCountText").objectReferenceValue = selectionPanel.transform.Find("TeamBButton/CountText").GetComponent<TextMeshProUGUI>();
            so.FindProperty("confirmButton").objectReferenceValue = selectionPanel.transform.Find("ConfirmButton").GetComponent<Button>();
            so.FindProperty("selectedTeamText").objectReferenceValue = selectionPanel.transform.Find("SelectedTeamText").GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            // Hide panel by default
            selectionPanel.SetActive(false);

            Debug.Log("✅ PROFESSIONAL Team Selection UI created!");
            Debug.Log("   - EventSystem: ✓");
            Debug.Log("   - GraphicRaycaster: ✓");
            Debug.Log("   - Fullscreen blocker: ✓");

            EditorUtility.DisplayDialog("Success!",
                "Professional Team Selection UI created!\n\n" +
                "✓ EventSystem ready\n" +
                "✓ Buttons clickable\n" +
                "✓ Fullscreen blocker\n" +
                "✓ TeamA (Blue) / TeamB (Red) / Auto Balance",
                "OK");

            Selection.activeGameObject = teamSelectionObj;
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
            img.color = new Color(0, 0, 0, 0.7f);

            Button btn = blocker.AddComponent<Button>();
            btn.interactable = false;

            return blocker;
        }

        static GameObject CreateSelectionPanel(GameObject parent)
        {
            GameObject panel = new GameObject("SelectionPanel");
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 500);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            CreateTitle(panel, "SELECT YOUR TEAM");

            // Team A Button (Blue)
            CreateTeamButton(panel, "TeamAButton", "TEAM A\n(BLUE)", new Vector2(0, 80), new Color(0.2f, 0.4f, 0.8f));

            // Team B Button (Red)
            CreateTeamButton(panel, "TeamBButton", "TEAM B\n(RED)", new Vector2(0, 0), new Color(0.8f, 0.2f, 0.2f));

            // Auto Balance Button
            CreateTeamButton(panel, "AutoButton", "AUTO BALANCE", new Vector2(0, -80), new Color(0.3f, 0.7f, 0.3f));

            // Selected Team Text
            CreateSelectedTeamText(panel);

            // Confirm Button
            CreateButton(panel, "ConfirmButton", "CONFIRM", new Vector2(0, -180), new Color(0.2f, 0.8f, 0.2f));

            return panel;
        }

        static void CreateTitle(GameObject parent, string text)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 200);
            rect.sizeDelta = new Vector2(560, 60);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = text;
            title.fontSize = 42;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;
        }

        static void CreateTeamButton(GameObject parent, string name, string text, Vector2 position, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500, 70);

            Image image = buttonObj.AddComponent<Image>();
            image.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-100, 0);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 24;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Left;

            // Count Text
            GameObject countObj = new GameObject("CountText");
            countObj.transform.SetParent(buttonObj.transform, false);

            RectTransform countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 0.5f);
            countRect.anchorMax = new Vector2(1f, 0.5f);
            countRect.pivot = new Vector2(1f, 0.5f);
            countRect.anchoredPosition = new Vector2(-10, 0);
            countRect.sizeDelta = new Vector2(80, 50);

            TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
            countText.text = "0 Players";
            countText.fontSize = 16;
            countText.color = Color.white;
            countText.alignment = TextAlignmentOptions.Right;
        }

        static void CreateSelectedTeamText(GameObject parent)
        {
            GameObject textObj = new GameObject("SelectedTeamText");
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -130);
            rect.sizeDelta = new Vector2(500, 40);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Selected: Auto Balance";
            text.fontSize = 20;
            text.color = new Color(1f, 1f, 0.5f);
            text.alignment = TextAlignmentOptions.Center;
        }

        static void CreateButton(GameObject parent, string name, string text, Vector2 position, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 60);

            Image image = buttonObj.AddComponent<Image>();
            image.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 24;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
        }
    }
}
