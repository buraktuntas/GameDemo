using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Creates Scoreboard UI automatically
    /// </summary>
    public class ScoreboardCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create Scoreboard")]
        static void ShowWindow()
        {
            CreateScoreboard();
        }

        static void CreateScoreboard()
        {
            // Check if exists
            Scoreboard existing = FindFirstObjectByType<Scoreboard>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Scoreboard Exists",
                    "Scoreboard already exists. Delete and recreate?",
                    "Yes, Replace",
                    "Cancel");

                if (!replace) return;

                DestroyImmediate(existing.gameObject);
            }

            Debug.Log("🎨 Creating Scoreboard UI...");

            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Scoreboard root
            GameObject scoreboardObj = new GameObject("Scoreboard");
            scoreboardObj.transform.SetParent(canvas.transform, false);

            RectTransform scoreboardRect = scoreboardObj.AddComponent<RectTransform>();
            scoreboardRect.anchorMin = Vector2.zero;
            scoreboardRect.anchorMax = Vector2.one;
            scoreboardRect.sizeDelta = Vector2.zero;

            Scoreboard scoreboard = scoreboardObj.AddComponent<Scoreboard>();

            // Create Panel (background)
            GameObject panel = CreatePanel(scoreboardObj, "ScoreboardPanel", new Vector2(800, 600));

            // Create header
            GameObject header = CreateText(panel, "Header", "SCOREBOARD", 36, new Vector2(0, 250),
                TextAlignmentOptions.Center);
            TextMeshProUGUI headerText = header.GetComponent<TextMeshProUGUI>();
            headerText.fontStyle = FontStyles.Bold;

            // Create Team A section
            GameObject teamASection = CreateTeamSection(panel, "TeamA", new Vector2(-200, 0), new Color(0.2f, 0.5f, 1f));
            GameObject teamAHeader = CreateText(teamASection, "TeamAHeader", "TEAM A - 0 Rounds", 24,
                new Vector2(0, 200), TextAlignmentOptions.Center);
            GameObject teamAContent = CreateScrollView(teamASection, "TeamAContent", new Vector2(350, 400),
                Vector2.zero);

            // Create Team B section
            GameObject teamBSection = CreateTeamSection(panel, "TeamB", new Vector2(200, 0), new Color(1f, 0.3f, 0.3f));
            GameObject teamBHeader = CreateText(teamBSection, "TeamBHeader", "TEAM B - 0 Rounds", 24,
                new Vector2(0, 200), TextAlignmentOptions.Center);
            GameObject teamBContent = CreateScrollView(teamBSection, "TeamBContent", new Vector2(350, 400),
                Vector2.zero);

            // Create player entry prefab
            GameObject entryPrefab = CreatePlayerEntryPrefab();

            // Assign references
            SerializedObject so = new SerializedObject(scoreboard);
            so.FindProperty("scoreboardPanel").objectReferenceValue = panel;
            so.FindProperty("teamAContent").objectReferenceValue = teamAContent.transform.Find("Viewport/Content");
            so.FindProperty("teamBContent").objectReferenceValue = teamBContent.transform.Find("Viewport/Content");
            so.FindProperty("teamAScoreText").objectReferenceValue = teamAHeader.GetComponent<TextMeshProUGUI>();
            so.FindProperty("teamBScoreText").objectReferenceValue = teamBHeader.GetComponent<TextMeshProUGUI>();
            so.FindProperty("playerEntryPrefab").objectReferenceValue = entryPrefab;
            so.ApplyModifiedProperties();

            // Save prefab
            string prefabPath = "Assets/Prefabs/UI/PlayerEntry.prefab";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(entryPrefab, prefabPath);
            DestroyImmediate(entryPrefab);

            Debug.Log("✅ Scoreboard created successfully!");
            EditorUtility.DisplayDialog("Success", "Scoreboard UI created!\n\nPress TAB to show/hide.", "OK");

            Selection.activeGameObject = scoreboardObj;
        }

        static GameObject CreatePanel(GameObject parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.9f);

            return panel;
        }

        static GameObject CreateTeamSection(GameObject parent, string name, Vector2 position, Color color)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(parent.transform, false);

            RectTransform rect = section.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(380, 500);
            rect.anchoredPosition = position;

            Image image = section.AddComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.2f);

            return section;
        }

        static GameObject CreateText(GameObject parent, string name, string text, int fontSize,
            Vector2 position, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360, fontSize + 20);
            rect.anchoredPosition = position;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.color = Color.white;
            tmpText.alignment = alignment;

            return textObj;
        }

        static GameObject CreateScrollView(GameObject parent, string name, Vector2 size, Vector2 position)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent.transform, false);

            RectTransform rect = scrollView.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = scrollView.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = Color.clear;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;

            return scrollView;
        }

        static GameObject CreatePlayerEntryPrefab()
        {
            GameObject prefab = new GameObject("PlayerEntry");

            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(340, 30);

            Image bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            HorizontalLayoutGroup layout = prefab.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);

            LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
            layoutElement.minHeight = 30;

            // Name
            CreateEntryText(prefab, "NameText", "Player", 160, TextAlignmentOptions.Left);

            // Kills
            CreateEntryText(prefab, "KillsText", "0", 40, TextAlignmentOptions.Center);

            // Deaths
            CreateEntryText(prefab, "DeathsText", "0", 40, TextAlignmentOptions.Center);

            // K/D
            CreateEntryText(prefab, "KDText", "0.00", 50, TextAlignmentOptions.Center);

            return prefab;
        }

        static void CreateEntryText(GameObject parent, string name, string text, float width,
            TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 16;
            tmpText.color = Color.white;
            tmpText.alignment = alignment;

            LayoutElement layout = textObj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }
    }
}
