using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ AUTOMATIC: Creates complete GameHUD with all UI elements
    /// </summary>
    public class GameHUDCreator : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create GameHUD (Auto-Complete UI)")]
        static void ShowWindow()
        {
            var window = GetWindow<GameHUDCreator>("GameHUD Creator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🎨 GAMEHUD CREATOR", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Check if GameHUD exists
            GameHUD existingHUD = FindFirstObjectByType<GameHUD>();

            if (existingHUD != null)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ GameHUD already exists: '{existingHUD.gameObject.name}'\n\n" +
                    "Choose an option below:",
                    MessageType.Warning);

                GUILayout.Space(10);

                // Option 1: Delete & Recreate (RECOMMENDED)
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("🗑️ DELETE OLD & CREATE NEW (Recommended)", GUILayout.Height(50)))
                {
                    DeleteAndRecreate(existingHUD);
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(5);

                // Option 2: Update existing
                if (GUILayout.Button("🔧 Update Existing GameHUD", GUILayout.Height(40)))
                {
                    SetupExistingGameHUD(existingHUD);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No GameHUD found in scene.\n\n" +
                    "Click below to create a complete GameHUD with all UI elements:",
                    MessageType.Info);

                GUILayout.Space(10);

                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button("✅ CREATE NEW GAMEHUD", GUILayout.Height(50)))
                {
                    CreateCompleteGameHUD();
                    Close();
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "GameHUD Layout:\n" +
                "• Top Right: Health Bar\n" +
                "• Bottom Right: Ammo Display\n" +
                "• Top Center: Timer & Phase\n" +
                "• Top Left: Team Score",
                MessageType.Info);
        }

        static void DeleteAndRecreate(GameHUD existingHUD)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Confirm Delete",
                "This will DELETE the old GameHUD and create a new one with correct positions.\n\n" +
                "Are you sure?",
                "Yes, Delete & Recreate",
                "Cancel");

            if (!confirm) return;

            // Delete old
            Debug.Log($"🗑️ Deleting old GameHUD: {existingHUD.gameObject.name}");
            DestroyImmediate(existingHUD.gameObject);

            // Create new
            CreateCompleteGameHUD();

            EditorUtility.DisplayDialog(
                "Success!",
                "✅ Old GameHUD deleted!\n" +
                "✅ New GameHUD created with correct positions!\n\n" +
                "All UI elements are now in their proper locations.",
                "Awesome!");
        }

        static void CreateCompleteGameHUD()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("✅ Created new Canvas");
            }

            // Create GameHUD GameObject
            GameObject hudObj = new GameObject("GameHUD");
            hudObj.transform.SetParent(canvas.transform, false);
            
            // ✅ FIX: Add RectTransform and make it fill the entire canvas
            RectTransform hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            hudRect.anchoredPosition = Vector2.zero;

            GameHUD gameHUD = hudObj.AddComponent<GameHUD>();

            // Create UI elements
            CreateHealthUI(hudObj, gameHUD);
            CreateAmmoUI(hudObj, gameHUD);
            CreateTimerUI(hudObj, gameHUD);
            CreateScoreUI(hudObj, gameHUD);
            CreateRoundWinUI(hudObj, gameHUD);
            CreateKillFeedUI(hudObj, gameHUD);
            CreateRespawnUI(hudObj, gameHUD);

            // Select the created object
            Selection.activeGameObject = hudObj;

            Debug.Log("✅✅✅ GameHUD Created Successfully! ✅✅✅");
            Debug.Log("All UI elements connected automatically.");

            EditorUtility.DisplayDialog(
                "GameHUD Created!",
                "✅ GameHUD with complete UI has been created!\n\n" +
                "Location: Canvas → GameHUD\n\n" +
                "All elements are connected and ready to use:\n" +
                "• Health Bar & Text\n" +
                "• Ammo Display\n" +
                "• Timer & Round Info\n" +
                "• Team Score\n\n" +
                "You can customize positions and styles in the Inspector.",
                "Awesome!");
        }

        static void SetupExistingGameHUD(GameHUD gameHUD)
        {
            GameObject hudObj = gameHUD.gameObject;

            // Setup/update UI elements
            CreateHealthUI(hudObj, gameHUD);
            CreateAmmoUI(hudObj, gameHUD);
            CreateTimerUI(hudObj, gameHUD);
            CreateScoreUI(hudObj, gameHUD);

            EditorUtility.SetDirty(gameHUD);

            Debug.Log("✅ GameHUD Updated Successfully!");

            EditorUtility.DisplayDialog(
                "GameHUD Updated!",
                "✅ GameHUD has been setup/updated!\n\n" +
                "All missing UI elements have been created and connected.",
                "Great!");
        }

        static void CreateHealthUI(GameObject parent, GameHUD gameHUD)
        {
            // Health Panel (BOTTOM LEFT - FPS Standard like Valorant/CS:GO)
            GameObject healthPanel = CreatePanel(parent, "HealthPanel", new Vector2(200, 60),
                new Vector2(20, 20), new Vector2(0, 0), new Vector2(0, 0));

            // Make background transparent (minimal HUD)
            Image panelBg = healthPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0, 0, 0, 0.2f);

            // Health Slider (horizontal bar)
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(healthPanel.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0);
            sliderRect.sizeDelta = new Vector2(-10, 8);
            sliderRect.anchoredPosition = new Vector2(0, 5);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;

            // Slider Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Slider Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-10, -10);

            // Slider Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0, 1, 0, 0.9f); // Green
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;

            slider.fillRect = fillRect;

            // Health Text (BIG number like Valorant)
            GameObject textObj = CreateText(healthPanel, "HealthText", "100", 28,
                new Vector2(0, 25), TextAlignmentOptions.Center, new Vector2(100, 40));
            TextMeshProUGUI healthText = textObj.GetComponent<TextMeshProUGUI>();
            healthText.fontStyle = FontStyles.Bold;
            healthText.color = Color.white;

            // Assign to GameHUD
            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("healthSlider").objectReferenceValue = slider;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Health UI created");
        }

        static void CreateAmmoUI(GameObject parent, GameHUD gameHUD)
        {
            // Ammo Panel (BOTTOM RIGHT - FPS Standard like CS:GO/Valorant)
            GameObject ammoPanel = CreatePanel(parent, "AmmoPanel", new Vector2(200, 80),
                new Vector2(-20, 20), new Vector2(1, 0), new Vector2(1, 0));

            // Make background transparent
            Image panelBg = ammoPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0, 0, 0, 0.2f);

            // Current Ammo (HUGE - primary focus)
            GameObject ammoObj = CreateTextRightAligned(ammoPanel, "AmmoText", "30", 56,
                new Vector2(-50, 20), new Vector2(120, 60));
            TextMeshProUGUI ammoText = ammoObj.GetComponent<TextMeshProUGUI>();
            ammoText.fontStyle = FontStyles.Bold;
            ammoText.color = Color.white;

            // Reserve Ammo (smaller, below current)
            GameObject reserveObj = CreateTextRightAligned(ammoPanel, "ReserveAmmoText", "/ 90", 28,
                new Vector2(-50, -10), new Vector2(120, 30));
            TextMeshProUGUI reserveText = reserveObj.GetComponent<TextMeshProUGUI>();
            reserveText.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);

            // Assign to GameHUD
            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("ammoText").objectReferenceValue = ammoText;
            so.FindProperty("reserveAmmoText").objectReferenceValue = reserveText;
            so.FindProperty("ammoPanel").objectReferenceValue = ammoPanel;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Ammo UI created");
        }

        static void CreateTimerUI(GameObject parent, GameHUD gameHUD)
        {
            // Timer Panel (TOP CENTER - Minimal like Valorant)
            GameObject timerPanel = CreatePanel(parent, "TimerPanel", new Vector2(300, 80),
                new Vector2(0, -15), new Vector2(0.5f, 1), new Vector2(0.5f, 1));

            // Make background very transparent
            Image panelBg = timerPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0, 0, 0, 0.15f);

            // Timer Text (prominent)
            GameObject timerObj = CreateText(timerPanel, "TimerText", "5:00", 32,
                new Vector2(0, 0), TextAlignmentOptions.Center, new Vector2(200, 40));
            TextMeshProUGUI timerText = timerObj.GetComponent<TextMeshProUGUI>();
            timerText.fontStyle = FontStyles.Bold;
            timerText.color = Color.white;

            // Phase Text (small, above timer)
            GameObject phaseObj = CreateText(timerPanel, "PhaseText", "BUILD", 16,
                new Vector2(0, 20), TextAlignmentOptions.Center, new Vector2(150, 25));
            TextMeshProUGUI phaseText = phaseObj.GetComponent<TextMeshProUGUI>();
            phaseText.color = new Color(1f, 0.9f, 0.3f, 1f);

            // Round Text (small, below timer)
            GameObject roundObj = CreateText(timerPanel, "RoundText", "Round 1", 14,
                new Vector2(0, -20), TextAlignmentOptions.Center, new Vector2(150, 25));
            TextMeshProUGUI roundText = roundObj.GetComponent<TextMeshProUGUI>();
            roundText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);

            // Assign to GameHUD
            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("phaseText").objectReferenceValue = phaseText;
            so.FindProperty("roundText").objectReferenceValue = roundText;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Timer UI created");
        }

        static void CreateScoreUI(GameObject parent, GameHUD gameHUD)
        {
            // Score Panel (TOP LEFT - Minimal)
            GameObject scorePanel = CreatePanel(parent, "ScorePanel", new Vector2(200, 50),
                new Vector2(20, -15), new Vector2(0, 1), new Vector2(0, 1));

            // Make background transparent
            Image panelBg = scorePanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0, 0, 0, 0.15f);

            // Team Score Text (clean format)
            GameObject scoreObj = CreateText(scorePanel, "TeamScoreText", "0 - 0", 22,
                Vector2.zero, TextAlignmentOptions.Center, new Vector2(180, 40));
            TextMeshProUGUI scoreText = scoreObj.GetComponent<TextMeshProUGUI>();
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.color = Color.white;

            // Assign to GameHUD
            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("teamScoreText").objectReferenceValue = scoreText;
            so.FindProperty("teamStatusPanel").objectReferenceValue = scorePanel;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Score UI created");
        }

        static void CreateRoundWinUI(GameObject parent, GameHUD gameHUD)
        {
            // Center screen - big victory message
            GameObject winPanel = CreatePanel(parent, "RoundWinPanel", new Vector2(600, 200),
                Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image winBg = winPanel.GetComponent<Image>();
            winBg.color = new Color(0, 0, 0, 0.8f);
            winPanel.SetActive(false);

            GameObject winTextObj = CreateText(winPanel, "RoundWinText", "TEAM A WINS!\n3 - 2", 48,
                Vector2.zero, TextAlignmentOptions.Center, new Vector2(580, 180));
            TextMeshProUGUI winText = winTextObj.GetComponent<TextMeshProUGUI>();
            winText.fontStyle = FontStyles.Bold;
            winText.color = Color.yellow;

            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("roundWinPanel").objectReferenceValue = winPanel;
            so.FindProperty("roundWinText").objectReferenceValue = winText;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Round Win UI created");
        }

        static void CreateKillFeedUI(GameObject parent, GameHUD gameHUD)
        {
            // Top right - kill notifications
            GameObject feedPanel = CreatePanel(parent, "KillFeedPanel", new Vector2(300, 50),
                new Vector2(-20, -80), new Vector2(1, 1), new Vector2(1, 1));

            Image feedBg = feedPanel.GetComponent<Image>();
            feedBg.color = new Color(0, 0, 0, 0.6f);
            feedPanel.SetActive(false);

            GameObject feedTextObj = CreateText(feedPanel, "KillFeedText", "Player1 → Player2", 18,
                Vector2.zero, TextAlignmentOptions.Center, new Vector2(280, 40));
            TextMeshProUGUI feedText = feedTextObj.GetComponent<TextMeshProUGUI>();
            feedText.color = Color.white;

            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("killFeedPanel").objectReferenceValue = feedPanel;
            so.FindProperty("killFeedText").objectReferenceValue = feedText;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Kill Feed UI created");
        }

        static void CreateRespawnUI(GameObject parent, GameHUD gameHUD)
        {
            // Center screen - respawn countdown
            GameObject respawnPanel = CreatePanel(parent, "RespawnPanel", new Vector2(400, 100),
                Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image respawnBg = respawnPanel.GetComponent<Image>();
            respawnBg.color = new Color(0, 0, 0, 0.7f);
            respawnPanel.SetActive(false);

            GameObject respawnTextObj = CreateText(respawnPanel, "RespawnText", "Respawning in 5...", 32,
                Vector2.zero, TextAlignmentOptions.Center, new Vector2(380, 80));
            TextMeshProUGUI respawnText = respawnTextObj.GetComponent<TextMeshProUGUI>();
            respawnText.color = Color.white;

            SerializedObject so = new SerializedObject(gameHUD);
            so.FindProperty("respawnPanel").objectReferenceValue = respawnPanel;
            so.FindProperty("respawnText").objectReferenceValue = respawnText;
            so.ApplyModifiedProperties();

            Debug.Log("  ✅ Respawn UI created");
        }

        static GameObject CreatePanel(GameObject parent, string name, Vector2 size, Vector2 position,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            
            // ✅ FIX: Set pivot based on anchor (critical for correct positioning)
            // If anchor is at bottom-left (0,0), pivot should be (0,0)
            // If anchor is at top-right (1,1), pivot should be (1,1)
            // If anchor is at center (0.5,0.5), pivot should be (0.5,0.5)
            rect.pivot = anchorMin; // Use anchorMin as pivot for consistent positioning
            
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            // Optional background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);

            return panel;
        }

        static GameObject CreateText(GameObject parent, string name, string content, int fontSize,
            Vector2 position, TextAlignmentOptions alignment, Vector2? size = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            // ✅ FIX: Use fixed anchor point (center) instead of stretch
            // This allows position to work correctly
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? new Vector2(200, 50); // Use provided size or default
            rect.anchoredPosition = position;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            return textObj;
        }

        static GameObject CreateTextRightAligned(GameObject parent, string name, string content, int fontSize,
            Vector2 position, Vector2? size = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            // ✅ FIX: Anchor to right side for right-aligned text
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f); // Pivot at right edge
            rect.sizeDelta = size ?? new Vector2(120, 50);
            rect.anchoredPosition = position;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Right;
            text.color = Color.white;

            return textObj;
        }
    }
}
