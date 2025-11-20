using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: GameModeSelection + Lobby UI Auto Setup Tool
    /// GameModeSelectionUI'ya lobby özelliklerini otomatik ekler ve referansları atar
    /// </summary>
    public class GameModeSelectionLobbySetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🎮 Setup GameModeSelection + Lobby UI")]
        public static void ShowWindow()
        {
            GetWindow<GameModeSelectionLobbySetup>("GameModeSelection + Lobby Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 GAMEMODESELECTION + LOBBY UI SETUP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("1. GameModeSelectionUI'ya lobby section ekler");
            GUILayout.Label("2. Player list container oluşturur");
            GUILayout.Label("3. Host controls (Start Game button) ekler");
            GUILayout.Label("4. Player controls (Ready button) ekler");
            GUILayout.Label("5. Tüm referansları otomatik atar");
            GUILayout.Space(10);

            if (GUILayout.Button("🚀 SETUP GAMEMODESELECTION + LOBBY UI", GUILayout.Height(40)))
            {
                SetupGameModeSelectionLobby();
            }

            GUILayout.Space(10);
            GUILayout.Label("✅ Mevcut scene'deki GameModeSelectionPanel kullanılacak", EditorStyles.helpBox);
            GUILayout.Label("⚠️ Scene'i kaydetmeyi unutma: File > Save Scene (Ctrl+S)", EditorStyles.helpBox);
        }

        private void SetupGameModeSelectionLobby()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮 GAMEMODESELECTION + LOBBY SETUP BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            // 1. Find GameModeSelectionUI
            GameModeSelectionUI gameModeUI = FindFirstObjectByType<GameModeSelectionUI>();
            if (gameModeUI == null)
            {
                Debug.LogError("❌ GameModeSelectionUI bulunamadı! Scene'de GameModeSelectionPanel var mı kontrol edin.");
                return;
            }

            Debug.Log($"✅ GameModeSelectionUI bulundu: {gameModeUI.gameObject.name}");

            // 2. Get selection panel
            GameObject selectionPanel = null;
            SerializedObject serializedUI = new SerializedObject(gameModeUI);
            SerializedProperty selectionPanelProp = serializedUI.FindProperty("selectionPanel");
            if (selectionPanelProp != null && selectionPanelProp.objectReferenceValue != null)
            {
                selectionPanel = selectionPanelProp.objectReferenceValue as GameObject;
            }
            else
            {
                // Try to find by name
                selectionPanel = gameModeUI.transform.Find("SelectionPanel")?.gameObject;
                if (selectionPanel == null)
                {
                    selectionPanel = gameModeUI.gameObject;
                }
            }

            // 3. Create Lobby Section (at top)
            GameObject lobbySection = CreateLobbySection(selectionPanel != null ? selectionPanel.transform : gameModeUI.transform);
            
            // 4. Create Player List Container
            Transform playerListContainer = CreatePlayerListContainer(lobbySection.transform);

            // 5. Create Host Controls
            GameObject hostControlsPanel = CreateHostControls(lobbySection.transform);

            // 6. Create Player Controls
            GameObject playerControlsPanel = CreatePlayerControls(lobbySection.transform);

            // 7. Create Info Texts
            CreateInfoTexts(lobbySection.transform);

            // 8. ✅ NEW: Reposition mode selection buttons to bottom
            RepositionModeButtons(selectionPanel != null ? selectionPanel : gameModeUI.gameObject);

            // 9. Assign all references
            AssignReferences(gameModeUI, lobbySection, playerListContainer, hostControlsPanel, playerControlsPanel);

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ GAMEMODESELECTION + LOBBY SETUP TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("2. Test et: Host > Individual > Confirm");
            Debug.Log("3. Lobby section görünmeli!");
            Debug.Log("");
        }

        private GameObject CreateLobbySection(Transform parent)
        {
            // Check if already exists
            Transform existing = parent.Find("LobbySection");
            if (existing != null)
            {
                Debug.Log("✅ LobbySection zaten var, kullanılıyor");
                return existing.gameObject;
            }

            GameObject lobbySection = new GameObject("LobbySection");
            lobbySection.transform.SetParent(parent, false);

            // Add RectTransform - Position at top (above mode buttons)
            RectTransform rect = lobbySection.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.3f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            // Add background
            Image bg = lobbySection.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            Debug.Log("✅ LobbySection oluşturuldu");
            return lobbySection;
        }

        private Transform CreatePlayerListContainer(Transform parent)
        {
            Transform existing = parent.Find("PlayerListContainer");
            if (existing != null)
            {
                Debug.Log("✅ PlayerListContainer zaten var");
                return existing;
            }

            GameObject container = new GameObject("PlayerListContainer");
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.3f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            // Add Vertical Layout Group
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            // Add Content Size Fitter
            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add ScrollRect
            ScrollRect scrollRect = container.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // Create Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(container.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5f;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            Debug.Log("✅ PlayerListContainer oluşturuldu");
            return content.transform; // Return content transform for player items
        }

        private GameObject CreateHostControls(Transform parent)
        {
            Transform existing = parent.Find("HostControlsPanel");
            if (existing != null)
            {
                Debug.Log("✅ HostControlsPanel zaten var");
                return existing.gameObject;
            }

            GameObject panel = new GameObject("HostControlsPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.15f);
            rect.anchorMax = new Vector2(1f, 0.3f);
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);

            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // Create Start Game Button
            GameObject startButton = new GameObject("StartGameButton");
            startButton.transform.SetParent(panel.transform, false);

            RectTransform buttonRect = startButton.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);

            Image buttonImage = startButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.8f, 0.2f);

            Button button = startButton.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button Text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(startButton.transform, false);
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = buttonText.AddComponent<TextMeshProUGUI>();
            text.text = "OYUNU BAŞLAT";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            Debug.Log("✅ HostControlsPanel oluşturuldu");
            return panel;
        }

        private GameObject CreatePlayerControls(Transform parent)
        {
            Transform existing = parent.Find("PlayerControlsPanel");
            if (existing != null)
            {
                Debug.Log("✅ PlayerControlsPanel zaten var");
                return existing.gameObject;
            }

            GameObject panel = new GameObject("PlayerControlsPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.15f);
            rect.anchorMax = new Vector2(1f, 0.3f);
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);

            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // Create Ready Button
            GameObject readyButton = new GameObject("ReadyButton");
            readyButton.transform.SetParent(panel.transform, false);

            RectTransform buttonRect = readyButton.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);

            Image buttonImage = readyButton.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.2f, 0.2f);

            Button button = readyButton.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button Text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(readyButton.transform, false);
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = buttonText.AddComponent<TextMeshProUGUI>();
            text.text = "HAZIR DEĞİL";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            Debug.Log("✅ PlayerControlsPanel oluşturuldu");
            return panel;
        }

        private void CreateInfoTexts(Transform parent)
        {
            // Player Count Text
            if (parent.Find("PlayerCountText") == null)
            {
                GameObject playerCountText = new GameObject("PlayerCountText");
                playerCountText.transform.SetParent(parent, false);

                RectTransform rect = playerCountText.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.05f);
                rect.anchorMax = new Vector2(0.5f, 0.15f);
                rect.offsetMin = new Vector2(10, 0);
                rect.offsetMax = new Vector2(-5, 0);

                TextMeshProUGUI text = playerCountText.AddComponent<TextMeshProUGUI>();
                text.text = "Oyuncular: 0/8";
                text.fontSize = 20;
                text.color = Color.white;
            }

            // Game Mode Text
            if (parent.Find("GameModeText") == null)
            {
                GameObject gameModeText = new GameObject("GameModeText");
                gameModeText.transform.SetParent(parent, false);

                RectTransform rect = gameModeText.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.05f);
                rect.anchorMax = new Vector2(1f, 0.15f);
                rect.offsetMin = new Vector2(5, 0);
                rect.offsetMax = new Vector2(-10, 0);

                TextMeshProUGUI text = gameModeText.AddComponent<TextMeshProUGUI>();
                text.text = "Mod: Bireysel (FFA)";
                text.fontSize = 20;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Right;
            }

            Debug.Log("✅ Info texts oluşturuldu");
        }

        private void AssignReferences(GameModeSelectionUI gameModeUI, GameObject lobbySection, Transform playerListContainer, GameObject hostControlsPanel, GameObject playerControlsPanel)
        {
            Debug.Log("🔗 Assigning references...");

            SerializedObject serializedUI = new SerializedObject(gameModeUI);

            // Lobby Section
            AssignReference(serializedUI, "lobbySection", lobbySection);

            // Player List Container
            AssignReference(serializedUI, "playerListContainer", playerListContainer);

            // Host Controls
            if (hostControlsPanel != null)
            {
                Transform startButton = hostControlsPanel.transform.Find("StartGameButton");
                if (startButton != null)
                {
                    AssignReference(serializedUI, "startGameButton", startButton.GetComponent<Button>());
                    Transform buttonText = startButton.Find("Text");
                    if (buttonText != null)
                    {
                        AssignReference(serializedUI, "startGameButtonText", buttonText.GetComponent<TextMeshProUGUI>());
                    }
                }
                AssignReference(serializedUI, "hostControlsPanel", hostControlsPanel);
            }

            // Player Controls
            if (playerControlsPanel != null)
            {
                Transform readyButton = playerControlsPanel.transform.Find("ReadyButton");
                if (readyButton != null)
                {
                    AssignReference(serializedUI, "readyButton", readyButton.GetComponent<Button>());
                    Transform buttonText = readyButton.Find("Text");
                    if (buttonText != null)
                    {
                        AssignReference(serializedUI, "readyButtonText", buttonText.GetComponent<TextMeshProUGUI>());
                    }
                }
                AssignReference(serializedUI, "playerControlsPanel", playerControlsPanel);
            }

            // Info Texts
            Transform playerCountText = lobbySection.transform.Find("PlayerCountText");
            if (playerCountText != null)
            {
                AssignReference(serializedUI, "playerCountText", playerCountText.GetComponent<TextMeshProUGUI>());
            }

            Transform gameModeText = lobbySection.transform.Find("GameModeText");
            if (gameModeText != null)
            {
                AssignReference(serializedUI, "gameModeText", gameModeText.GetComponent<TextMeshProUGUI>());
            }

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
                Debug.LogWarning($"⚠️ Property '{propertyName}' not found in GameModeSelectionUI");
            }
        }

        private void RepositionModeButtons(GameObject selectionPanel)
        {
            if (selectionPanel == null) return;

            // Find mode buttons (try different possible names)
            Transform individualButton = selectionPanel.transform.Find("IndividualModeButton");
            if (individualButton == null)
                individualButton = selectionPanel.transform.Find("BireyselButton");
            if (individualButton == null)
                individualButton = selectionPanel.transform.Find("IndividualButton");

            Transform teamButton = selectionPanel.transform.Find("TeamModeButton");
            if (teamButton == null)
                teamButton = selectionPanel.transform.Find("TakimButton");
            if (teamButton == null)
                teamButton = selectionPanel.transform.Find("TeamButton");

            Transform confirmButton = selectionPanel.transform.Find("ConfirmButton");
            if (confirmButton == null)
                confirmButton = selectionPanel.transform.Find("OnaylaButton");
            if (confirmButton == null)
                confirmButton = selectionPanel.transform.Find("Confirm");

            // Hide confirm button
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
                Debug.Log("✅ Confirm button hidden");
            }

            // Make buttons smaller and position at bottom
            if (individualButton != null)
            {
                RectTransform rect = individualButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(150, 40);
                    rect.anchorMin = new Vector2(0.4f, 0.05f);
                    rect.anchorMax = new Vector2(0.5f, 0.15f);
                    rect.anchoredPosition = new Vector2(0, 0);
                    Debug.Log("✅ Individual button repositioned");
                }
            }

            if (teamButton != null)
            {
                RectTransform rect = teamButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(150, 40);
                    rect.anchorMin = new Vector2(0.5f, 0.05f);
                    rect.anchorMax = new Vector2(0.6f, 0.15f);
                    rect.anchoredPosition = new Vector2(0, 0);
                    Debug.Log("✅ Team button repositioned");
                }
            }

            Debug.Log("✅ Mode buttons repositioned to bottom");
        }
    }
}

