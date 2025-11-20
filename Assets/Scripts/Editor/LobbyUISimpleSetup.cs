using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Simple Lobby UI Setup Tool
    /// LobbyUI'ya game mode selection panel'i ve tüm referansları ekler
    /// </summary>
    public class LobbyUISimpleSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🎮 Setup Lobby UI (Simple)")]
        public static void ShowWindow()
        {
            GetWindow<LobbyUISimpleSetup>("Lobby UI Simple Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 LOBBY UI SIMPLE SETUP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("1. LobbyUI'ya game mode selection panel ekler (üstte)");
            GUILayout.Label("2. Individual/Team butonları oluşturur");
            GUILayout.Label("3. Game mode text oluşturur");
            GUILayout.Label("4. Tüm referansları otomatik atar");
            GUILayout.Label("5. Full screen layout ayarlar");
            GUILayout.Space(10);
            
            GUILayout.Label("⚠️ ÖNEMLİ:", EditorStyles.helpBox);
            GUILayout.Label("• LobbyPanel'in full screen olması gerekiyor");
            GUILayout.Label("• Game mode selection panel üstte görünecek");
            GUILayout.Label("• Player list ve controls altta görünecek");
            GUILayout.Space(10);

            if (GUILayout.Button("🚀 SETUP LOBBY UI", GUILayout.Height(40)))
            {
                SetupLobbyUI();
            }

            GUILayout.Space(10);
            GUILayout.Label("✅ Mevcut scene'deki LobbyUI kullanılacak", EditorStyles.helpBox);
            GUILayout.Label("⚠️ Scene'i kaydetmeyi unutma: File > Save Scene (Ctrl+S)", EditorStyles.helpBox);
        }

        private void SetupLobbyUI()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮 LOBBY UI SIMPLE SETUP BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            // 1. Find LobbyUI
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI == null)
            {
                Debug.LogError("❌ LobbyUI bulunamadı! Scene'de LobbyPanel var mı kontrol edin.");
                return;
            }

            Debug.Log($"✅ LobbyUI bulundu: {lobbyUI.gameObject.name}");

            // 2. Get lobby panel
            GameObject lobbyPanel = null;
            SerializedObject serializedUI = new SerializedObject(lobbyUI);
            SerializedProperty lobbyPanelProp = serializedUI.FindProperty("lobbyPanel");
            if (lobbyPanelProp != null && lobbyPanelProp.objectReferenceValue != null)
            {
                lobbyPanel = lobbyPanelProp.objectReferenceValue as GameObject;
            }
            else
            {
                lobbyPanel = lobbyUI.gameObject;
            }

            // ✅ STEP 1: CLEAN - Remove existing game mode selection elements
            CleanGameModeSelectionElements(lobbyPanel.transform);

            // ✅ STEP 2: CREATE - Create Game Mode Selection Panel
            GameObject gameModePanel = CreateGameModeSelectionPanel(lobbyPanel.transform);

            // ✅ STEP 3: CREATE - Create Individual/Team buttons
            Button individualBtn = CreateModeButton(gameModePanel.transform, "IndividualModeButton", "BİREYSEL", true);
            Button teamBtn = CreateModeButton(gameModePanel.transform, "TeamModeButton", "TAKIM", false);

            // ✅ STEP 4: CREATE - Create Game Mode Text
            TextMeshProUGUI gameModeText = CreateGameModeText(lobbyPanel.transform);

            // ✅ STEP 5: ASSIGN - Assign all references
            AssignReferences(lobbyUI, gameModePanel, individualBtn, teamBtn, gameModeText);

            // ✅ STEP 6: SAVE - Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(lobbyUI.gameObject.scene);
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ LOBBY UI SETUP TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("2. Test et: Host > LobbyUI görünmeli");
            Debug.Log("3. Individual/Team butonları çalışmalı");
            Debug.Log("");
        }
        
        /// <summary>
        /// ✅ NEW: Clean existing game mode selection elements
        /// </summary>
        private void CleanGameModeSelectionElements(Transform parent)
        {
            Debug.Log("🧹 Cleaning existing game mode selection elements...");
            
            // Remove GameModeSelectionPanel
            Transform gameModePanel = parent.Find("GameModeSelectionPanel");
            if (gameModePanel != null)
            {
                DestroyImmediate(gameModePanel.gameObject);
                Debug.Log("✅ Removed existing GameModeSelectionPanel");
            }
            
            // Remove GameModeText
            Transform gameModeText = parent.Find("GameModeText");
            if (gameModeText != null)
            {
                DestroyImmediate(gameModeText.gameObject);
                Debug.Log("✅ Removed existing GameModeText");
            }
            
            // Remove Individual/Team buttons if they exist as direct children
            Transform individualBtn = parent.Find("IndividualModeButton");
            if (individualBtn != null)
            {
                DestroyImmediate(individualBtn.gameObject);
                Debug.Log("✅ Removed existing IndividualModeButton");
            }
            
            Transform teamBtn = parent.Find("TeamModeButton");
            if (teamBtn != null)
            {
                DestroyImmediate(teamBtn.gameObject);
                Debug.Log("✅ Removed existing TeamModeButton");
            }
            
            Debug.Log("✅ Cleaning completed");
        }

        private GameObject CreateGameModeSelectionPanel(Transform parent)
        {
            // Check if already exists
            Transform existing = parent.Find("GameModeSelectionPanel");
            if (existing != null)
            {
                Debug.Log("✅ GameModeSelectionPanel zaten var");
                return existing.gameObject;
            }

            GameObject panel = new GameObject("GameModeSelectionPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.85f); // Top 15% (game mode selection)
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // Add background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            // Add Horizontal Layout Group
            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Debug.Log("✅ GameModeSelectionPanel oluşturuldu");
            return panel;
        }

        private Button CreateModeButton(Transform parent, string name, string text, bool isIndividual)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 60);

            Image img = buttonObj.AddComponent<Image>();
            img.color = isIndividual ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.2f, 0.4f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = img;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 28;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;

            Debug.Log($"✅ {name} oluşturuldu");
            return button;
        }

        private TextMeshProUGUI CreateGameModeText(Transform parent)
        {
            Transform existing = parent.Find("GameModeText");
            if (existing != null)
            {
                Debug.Log("✅ GameModeText zaten var");
                return existing.GetComponent<TextMeshProUGUI>();
            }

            GameObject textObj = new GameObject("GameModeText");
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.8f); // Just below game mode buttons
            rect.anchorMax = new Vector2(1f, 0.85f);
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Mod: Bireysel (FFA)";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Right;

            Debug.Log("✅ GameModeText oluşturuldu");
            return text;
        }

        private void AssignReferences(LobbyUI lobbyUI, GameObject gameModePanel, Button individualBtn, Button teamBtn, TextMeshProUGUI gameModeText)
        {
            Debug.Log("🔗 Assigning references...");

            SerializedObject serializedUI = new SerializedObject(lobbyUI);

            AssignReference(serializedUI, "gameModeSelectionPanel", gameModePanel);
            AssignReference(serializedUI, "individualModeButton", individualBtn);
            AssignReference(serializedUI, "teamModeButton", teamBtn);
            AssignReference(serializedUI, "gameModeText", gameModeText);

            serializedUI.ApplyModifiedProperties();
            Debug.Log("✅ References assigned!");
        }

        private void AssignReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            if (value == null) return;
            
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"⚠️ Property '{propertyName}' not found in LobbyUI");
            }
        }
    }
}

