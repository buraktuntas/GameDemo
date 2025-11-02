using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// PROFESSIONAL Main Menu Creator
    /// EventSystem, GraphicRaycaster - ALL INCLUDED
    /// </summary>
    public class MainMenuCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create Main Menu (PROFESSIONAL)")]
        static void ShowWindow()
        {
            CreateMainMenu();
        }

        static void CreateMainMenu()
        {
            // Check if exists
            MainMenu existing = FindFirstObjectByType<MainMenu>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Main Menu Exists",
                    "Main Menu already exists. Delete and recreate?",
                    "Yes, Replace",
                    "Cancel");

                if (!replace) return;

                DestroyImmediate(existing.gameObject);
            }

            Debug.Log("🎨 Creating PROFESSIONAL Main Menu UI...");

            // STEP 1: Ensure EventSystem exists
            EnsureEventSystem();

            // STEP 2: Find or create Canvas
            Canvas canvas = FindOrCreateCanvas();

            // Create Main Menu root
            GameObject menuObj = new GameObject("MainMenu");
            menuObj.transform.SetParent(canvas.transform, false);

            RectTransform menuRect = menuObj.AddComponent<RectTransform>();
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.sizeDelta = Vector2.zero;

            MainMenu mainMenu = menuObj.AddComponent<MainMenu>();

            // Create background image
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(menuObj.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Create Main Menu Panel
            GameObject mainPanel = CreateMainMenuPanel(menuObj);

            // Create Join Panel
            GameObject joinPanel = CreateJoinPanel(menuObj);

            // Assign references
            SerializedObject so = new SerializedObject(mainMenu);
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
            so.FindProperty("joinPanel").objectReferenceValue = joinPanel;

            so.FindProperty("hostButton").objectReferenceValue = mainPanel.transform.Find("HostButton").GetComponent<Button>();
            so.FindProperty("joinButton").objectReferenceValue = mainPanel.transform.Find("JoinButton").GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = mainPanel.transform.Find("QuitButton").GetComponent<Button>();

            so.FindProperty("ipAddressInput").objectReferenceValue = joinPanel.transform.Find("IPInput").GetComponent<TMP_InputField>();
            so.FindProperty("connectButton").objectReferenceValue = joinPanel.transform.Find("ConnectButton").GetComponent<Button>();
            so.FindProperty("backButton").objectReferenceValue = joinPanel.transform.Find("BackButton").GetComponent<Button>();

            so.ApplyModifiedProperties();

            Debug.Log("✅ PROFESSIONAL Main Menu created!");
            Debug.Log("   - EventSystem: ✓");
            Debug.Log("   - GraphicRaycaster: ✓");
            Debug.Log("   - NetworkManagerHUD will be destroyed on Start");

            EditorUtility.DisplayDialog("Success!",
                "Professional Main Menu UI created!\n\n" +
                "✓ EventSystem ready\n" +
                "✓ Buttons clickable\n" +
                "✓ NetworkManagerHUD will be removed\n" +
                "✓ Proper UI hierarchy",
                "OK");

            Selection.activeGameObject = menuObj;
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

        static GameObject CreateMainMenuPanel(GameObject parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 500);

            // Title
            CreateTitle(panel, "TACTICAL COMBAT");

            // Host Button
            CreateButton(panel, "HostButton", "HOST GAME", new Vector2(0, 50), new Color(0.2f, 0.7f, 0.2f));

            // Join Button
            CreateButton(panel, "JoinButton", "JOIN GAME", new Vector2(0, -50), new Color(0.2f, 0.5f, 0.9f));

            // Quit Button
            CreateButton(panel, "QuitButton", "QUIT", new Vector2(0, -150), new Color(0.8f, 0.2f, 0.2f));

            return panel;
        }

        static GameObject CreateJoinPanel(GameObject parent)
        {
            GameObject panel = new GameObject("JoinPanel");
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 400);

            panel.SetActive(false); // Hidden by default

            // Title
            CreateSubtitle(panel, "JOIN GAME");

            // IP Input
            CreateIPInput(panel);

            // Connect Button
            CreateButton(panel, "ConnectButton", "CONNECT", new Vector2(0, -50), new Color(0.2f, 0.7f, 0.2f));

            // Back Button
            CreateButton(panel, "BackButton", "BACK", new Vector2(0, -150), new Color(0.5f, 0.5f, 0.5f));

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
            rect.anchoredPosition = new Vector2(0, 180);
            rect.sizeDelta = new Vector2(380, 80);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = text;
            title.fontSize = 48;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;
        }

        static void CreateSubtitle(GameObject parent, string text)
        {
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(parent.transform, false);

            RectTransform rect = subtitleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 150);
            rect.sizeDelta = new Vector2(380, 60);

            TextMeshProUGUI subtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitle.text = text;
            subtitle.fontSize = 36;
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.color = Color.white;
            subtitle.alignment = TextAlignmentOptions.Center;
        }

        static void CreateIPInput(GameObject parent)
        {
            GameObject inputObj = new GameObject("IPInput");
            inputObj.transform.SetParent(parent.transform, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 50);
            rect.sizeDelta = new Vector2(340, 50);

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = rect;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);

            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            phRect.offsetMin = new Vector2(10, 0);
            phRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter IP Address (e.g. 127.0.0.1)";
            placeholder.fontSize = 16;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholder.alignment = TextAlignmentOptions.Left;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "";
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Left;

            inputField.textComponent = tmpText;
            inputField.placeholder = placeholder;
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
            rect.sizeDelta = new Vector2(340, 60);

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
