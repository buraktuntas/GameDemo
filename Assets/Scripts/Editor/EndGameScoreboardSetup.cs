using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Editor tool to setup EndGameScoreboard with restart button
    /// </summary>
    public class EndGameScoreboardSetup : EditorWindow
    {
        [MenuItem("TacticalCombat/UI/Setup End Game Scoreboard")]
        public static void ShowWindow()
        {
            GetWindow<EndGameScoreboardSetup>("End Game Scoreboard Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("End Game Scoreboard Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create/Update End Game Scoreboard", GUILayout.Height(30)))
            {
                SetupEndGameScoreboard();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will:\n" +
                "1. Find or create EndGameScoreboard GameObject\n" +
                "2. Add restart button if missing\n" +
                "3. Setup all UI references",
                MessageType.Info
            );
        }

        private void SetupEndGameScoreboard()
        {
            // Find existing EndGameScoreboard
            EndGameScoreboard scoreboard = FindFirstObjectByType<EndGameScoreboard>();

            if (scoreboard == null)
            {
                // Create new GameObject
                GameObject scoreboardObj = new GameObject("EndGameScoreboard");
                scoreboard = scoreboardObj.AddComponent<EndGameScoreboard>();

                // Create Canvas if doesn't exist
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                }

                scoreboardObj.transform.SetParent(canvas.transform, false);
            }

            // Setup UI structure
            SetupScoreboardUI(scoreboard);
        }

        private void SetupScoreboardUI(EndGameScoreboard scoreboard)
        {
            GameObject panelObj = GetOrCreateChild(scoreboard.gameObject, "ScoreboardPanel");
            
            // Panel setup
            Image panelImage = panelObj.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.9f);
            }

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
            }

            // Winner Panel
            GameObject winnerPanelObj = GetOrCreateChild(panelObj, "WinnerPanel");
            SetupWinnerPanel(winnerPanelObj);

            // Player List Content
            GameObject playerListObj = GetOrCreateChild(panelObj, "PlayerListContent");
            SetupPlayerList(playerListObj);

            // Awards Content
            GameObject awardsObj = GetOrCreateChild(panelObj, "AwardsContent");
            SetupAwards(awardsObj);

            // ✅ NEW: Restart Button
            GameObject restartBtnObj = GetOrCreateChild(panelObj, "RestartButton");
            SetupRestartButton(restartBtnObj);

            // Use reflection to assign references
            AssignReferences(scoreboard, panelObj, winnerPanelObj, playerListObj, awardsObj, restartBtnObj);

            EditorUtility.SetDirty(scoreboard);
            Debug.Log("✅ EndGameScoreboard setup complete!");
        }

        private void SetupWinnerPanel(GameObject parent)
        {
            // Winner Text
            GameObject winnerTextObj = GetOrCreateChild(parent, "WinnerText");
            TextMeshProUGUI winnerText = winnerTextObj.GetComponent<TextMeshProUGUI>();
            if (winnerText == null)
            {
                winnerText = winnerTextObj.AddComponent<TextMeshProUGUI>();
                winnerText.text = "TEAM A WINS!";
                winnerText.fontSize = 48;
                winnerText.color = Color.yellow;
                winnerText.alignment = TextAlignmentOptions.Center;
            }

            RectTransform rect = winnerTextObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.9f);
                rect.anchorMax = new Vector2(0.5f, 0.9f);
                rect.sizeDelta = new Vector2(600, 80);
                rect.anchoredPosition = Vector2.zero;
            }
        }

        private void SetupPlayerList(GameObject parent)
        {
            RectTransform rect = parent.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = parent.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.1f, 0.2f);
            rect.anchorMax = new Vector2(0.9f, 0.7f);
            rect.sizeDelta = Vector2.zero;

            // Add ScrollRect
            ScrollRect scrollRect = parent.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = parent.AddComponent<ScrollRect>();
            }

            // Add Vertical Layout Group
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 5;
                layout.padding = new RectOffset(10, 10, 10, 10);
            }
        }

        private void SetupAwards(GameObject parent)
        {
            RectTransform rect = parent.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = parent.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.15f);
            rect.sizeDelta = Vector2.zero;

            // Add Horizontal Layout Group
            HorizontalLayoutGroup layout = parent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10;
                layout.padding = new RectOffset(10, 10, 10, 10);
            }
        }

        private void SetupRestartButton(GameObject buttonObj)
        {
            // Button component
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.9f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.6f, 0.1f, 1f);
            button.colors = colors;

            // RectTransform
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObj.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.05f);
            rect.anchorMax = new Vector2(0.5f, 0.05f);
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;

            // Background Image
            Image bgImage = buttonObj.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = buttonObj.AddComponent<Image>();
                bgImage.color = colors.normalColor;
            }

            // Button Text
            GameObject textObj = GetOrCreateChild(buttonObj, "Text");
            TextMeshProUGUI buttonText = textObj.GetComponent<TextMeshProUGUI>();
            if (buttonText == null)
            {
                buttonText = textObj.AddComponent<TextMeshProUGUI>();
            }

            buttonText.text = "YENİDEN OYNA";
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
            }
        }

        private GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child == null)
            {
                GameObject newChild = new GameObject(name);
                newChild.transform.SetParent(parent.transform, false);
                return newChild;
            }
            return child.gameObject;
        }

        private void AssignReferences(EndGameScoreboard scoreboard, GameObject panelObj, 
            GameObject winnerPanelObj, GameObject playerListObj, GameObject awardsObj, GameObject restartBtnObj)
        {
            // Use reflection to assign private SerializeField references
            var type = typeof(EndGameScoreboard);
            
            // scoreboardPanel
            var panelField = type.GetField("scoreboardPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (panelField != null)
            {
                panelField.SetValue(scoreboard, panelObj);
            }

            // winnerPanel
            var winnerPanelField = type.GetField("winnerPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (winnerPanelField != null)
            {
                winnerPanelField.SetValue(scoreboard, winnerPanelObj);
            }

            // winnerText
            var winnerTextObj = winnerPanelObj.transform.Find("WinnerText");
            if (winnerTextObj != null)
            {
                var winnerTextField = type.GetField("winnerText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (winnerTextField != null)
                {
                    winnerTextField.SetValue(scoreboard, winnerTextObj.GetComponent<TextMeshProUGUI>());
                }
            }

            // playerListContent
            var playerListField = type.GetField("playerListContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerListField != null)
            {
                playerListField.SetValue(scoreboard, playerListObj.transform);
            }

            // awardsContent
            var awardsField = type.GetField("awardsContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (awardsField != null)
            {
                awardsField.SetValue(scoreboard, awardsObj.transform);
            }

            // ✅ NEW: restartButton
            var restartButtonField = type.GetField("restartButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (restartButtonField != null)
            {
                restartButtonField.SetValue(scoreboard, restartBtnObj.GetComponent<Button>());
            }

            // restartButtonText
            var restartTextObj = restartBtnObj.transform.Find("Text");
            if (restartTextObj != null)
            {
                var restartTextField = type.GetField("restartButtonText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (restartTextField != null)
                {
                    restartTextField.SetValue(scoreboard, restartTextObj.GetComponent<TextMeshProUGUI>());
                }
            }
        }
    }
}













