using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TacticalCombat.Network;
using TacticalCombat.UI;
using UnityEngine.SceneManagement;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Automatic Lobby Setup Tool
    /// Tek tıkla tüm lobby sistemini kurar: Scene, UI, Prefabs
    /// </summary>
    public class LobbyAutoSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🎮 Auto Setup Lobby System")]
        public static void ShowWindow()
        {
            GetWindow<LobbyAutoSetup>("Lobby Auto Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 LOBBY SYSTEM AUTO SETUP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("1. test2.unity scene'ini kullanır (mevcut scene)");
            GUILayout.Label("2. Canvas ve Lobby UI hierarchy kurar");
            GUILayout.Label("3. LobbyManager prefab oluşturur");
            GUILayout.Label("4. PlayerListItem prefab oluşturur");
            GUILayout.Label("5. NetworkManager'ı yapılandırır");
            GUILayout.Space(10);

            if (GUILayout.Button("🚀 SETUP LOBBY SYSTEM", GUILayout.Height(40)))
            {
                SetupLobbySystem();
            }

            GUILayout.Space(10);
            GUILayout.Label("✅ Mevcut test2.unity scene'i kullanılacak", EditorStyles.helpBox);
            GUILayout.Label("⚠️ Scene'i kaydetmeyi unutma: File > Save Scene (Ctrl+S)", EditorStyles.helpBox);
        }

        private void SetupLobbySystem()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮 LOBBY SYSTEM AUTO SETUP BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            // 1. LobbyManager Prefab oluştur
            CreateLobbyManagerPrefab();

            // 2. PlayerListItem Prefab oluştur
            CreatePlayerListItemPrefab();

            // 3. NetworkManager'ı bul ve yapılandır
            ConfigureNetworkManager();

            // 4. ✅ NEW: LobbyScene ve UI'yı otomatik oluştur
            CreateLobbySceneAndUI();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ LOBBY SYSTEM SETUP TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. test2.unity scene'inde LobbyPanel'i kontrol et");
            Debug.Log("2. LobbyPanel'deki LobbyUI component'inde referansları kontrol et");
            Debug.Log("3. NetworkManager'da LobbyManager prefab referansını kontrol et");
            Debug.Log("4. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("5. Test et!");
            Debug.Log("");
        }
        
        private void CreateLobbySceneAndUI()
        {
            Debug.Log("🎨 Adding Lobby UI to test2.unity scene...");

            // ✅ FIX: test2.unity scene'ini kullan
            string scenePath = "Assets/test2.unity";
            UnityEngine.SceneManagement.Scene scene;
            
            // Mevcut scene'i kontrol et
            bool sceneExists = System.IO.File.Exists(scenePath);
            
            if (sceneExists)
            {
                // test2.unity'i aç
                try
                {
                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                    Debug.Log($"✅ test2.unity scene opened: {scenePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Could not open test2.unity scene: {e.Message}");
                    Debug.LogWarning("⚠️ Please make sure test2.unity exists and is not locked.");
                    return;
                }
            }
            else
            {
                Debug.LogError($"❌ test2.unity scene not found at: {scenePath}");
                Debug.LogError("❌ Please create test2.unity scene first or update the path.");
                return;
            }

            // Canvas kontrolü ve oluşturma
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("LobbyCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("✅ LobbyCanvas created");
            }
            else
            {
                Debug.Log($"✅ Canvas already exists: {canvas.name}");
            }
            
            // EventSystem kontrolü
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("✅ EventSystem created");
            }
            else
            {
                Debug.Log("✅ EventSystem already exists");
            }

            // LobbyPanel kontrolü ve oluşturma
            GameObject lobbyPanel = GameObject.Find("LobbyPanel");
            if (lobbyPanel == null)
            {
                lobbyPanel = new GameObject("LobbyPanel");
                lobbyPanel.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = lobbyPanel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                Image bg = lobbyPanel.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                
                Debug.Log("✅ LobbyPanel created");
            }
            else
            {
                Debug.Log("✅ LobbyPanel already exists");
            }

            // LobbyUI component ekle
            TacticalCombat.UI.LobbyUI lobbyUI = lobbyPanel.GetComponent<TacticalCombat.UI.LobbyUI>();
            if (lobbyUI == null)
            {
                lobbyUI = lobbyPanel.AddComponent<TacticalCombat.UI.LobbyUI>();
                Debug.Log("✅ LobbyUI component added");
            }
            else
            {
                Debug.Log("✅ LobbyUI component already exists");
            }

            // UI Hierarchy oluştur (temel yapı)
            CreateUIHierarchy(lobbyPanel, canvas.transform);

            // ✅ NEW: LobbyUI referanslarını otomatik ata
            AssignLobbyUIReferences(lobbyUI, lobbyPanel, canvas.transform);

            // ✅ FIX: Scene'i dirty olarak işaretle (kullanıcı manuel kaydedecek)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("✅ Lobby UI added to test2.unity");
            Debug.Log("⚠️ IMPORTANT: Please save the scene manually: File > Save Scene (Ctrl+S)");
        }

        private void AssignLobbyUIReferences(TacticalCombat.UI.LobbyUI lobbyUI, GameObject lobbyPanel, Transform canvasTransform)
        {
            Debug.Log("🔗 Assigning LobbyUI references...");

            SerializedObject serializedUI = new SerializedObject(lobbyUI);

            // Panels
            AssignReference(serializedUI, "lobbyPanel", lobbyPanel);
            AssignReference(serializedUI, "gameStartingPanel", FindChild(canvasTransform, "GameStartingPanel"));
            AssignReference(serializedUI, "errorPanel", FindChild(lobbyPanel.transform, "ErrorPanel"));

            // Player List
            GameObject content = FindChild(lobbyPanel.transform, "Content");
            AssignReference(serializedUI, "playerListContainer", content);
            
            GameObject playerListItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PlayerListItemPrefab.prefab");
            AssignReference(serializedUI, "playerListItemPrefab", playerListItemPrefab);

            // Lobby Info
            AssignReference(serializedUI, "lobbyTitleText", FindChild(lobbyPanel.transform, "LobbyTitleText"));
            AssignReference(serializedUI, "playerCountText", FindChild(lobbyPanel.transform, "PlayerCountText"));
            AssignReference(serializedUI, "roomCodeText", FindChild(lobbyPanel.transform, "RoomCodeText"));

            // Host Controls
            GameObject hostControls = FindChild(lobbyPanel.transform, "HostControls");
            AssignReference(serializedUI, "startGameButton", FindChild(hostControls.transform, "StartGameButton"));
            AssignReference(serializedUI, "autoBalanceButton", FindChild(hostControls.transform, "AutoBalanceButton"));
            GameObject startGameBtn = FindChild(hostControls.transform, "StartGameButton");
            if (startGameBtn != null)
                AssignReference(serializedUI, "startGameButtonText", FindChild(startGameBtn.transform, "Text"));

            // Player Controls
            GameObject playerControls = FindChild(lobbyPanel.transform, "PlayerControls");
            AssignReference(serializedUI, "readyButton", FindChild(playerControls.transform, "ReadyButton"));
            AssignReference(serializedUI, "readyButtonText", FindChild(playerControls.transform, "ReadyButtonText"));
            AssignReference(serializedUI, "teamAButton", FindChild(playerControls.transform, "TeamAButton"));
            AssignReference(serializedUI, "teamBButton", FindChild(playerControls.transform, "TeamBButton"));
            AssignReference(serializedUI, "leaveButton", FindChild(playerControls.transform, "LeaveButton"));

            // Waiting State
            GameObject waitingPanel = FindChild(lobbyPanel.transform, "WaitingForHostPanel");
            AssignReference(serializedUI, "waitingForHostPanel", waitingPanel);
            if (waitingPanel != null)
                AssignReference(serializedUI, "waitingText", FindChild(waitingPanel.transform, "WaitingText"));

            // Error Display
            AssignReference(serializedUI, "errorText", FindChild(lobbyPanel.transform, "ErrorText"));
            SerializedProperty errorDuration = serializedUI.FindProperty("errorDisplayDuration");
            if (errorDuration != null)
                errorDuration.floatValue = 3f;

            serializedUI.ApplyModifiedProperties();
            Debug.Log("✅ LobbyUI references assigned");
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

        private GameObject FindChild(Transform parent, string name, string parentName = "")
        {
            if (parent == null) return null;
            
            // Önce direkt child'ı ara
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;

            // Eğer parentName belirtilmişse, önce parent'ı bul
            if (!string.IsNullOrEmpty(parentName))
            {
                Transform parentObj = parent.Find(parentName);
                if (parentObj != null)
                {
                    child = parentObj.Find(name);
                    if (child != null) return child.gameObject;
                }
            }

            // Recursive search
            foreach (Transform t in parent)
            {
                if (t.name == name)
                    return t.gameObject;
                
                GameObject found = FindChild(t, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void CreateUIHierarchy(GameObject lobbyPanel, Transform canvasTransform)
        {
            if (lobbyPanel == null)
            {
                Debug.LogError("❌ CreateUIHierarchy: lobbyPanel is null!");
                return;
            }

            if (canvasTransform == null)
            {
                Debug.LogError("❌ CreateUIHierarchy: canvasTransform is null!");
                return;
            }

            // ✅ NEW: Create GameModeSelection UI first (before lobby)
            CreateGameModeSelectionUI(canvasTransform);

            // Header
            GameObject header = GetOrCreateChild(lobbyPanel.transform, "Header");
            if (header == null)
            {
                Debug.LogError("❌ Failed to create Header!");
                return;
            }

            GetOrCreateTextChild(header.transform, "LobbyTitleText", "LOBBY");
            GetOrCreateTextChild(header.transform, "PlayerCountText", "Players: 0/8");
            GetOrCreateTextChild(header.transform, "RoomCodeText", "");
            
            // Header layout
            HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout == null)
            {
                headerLayout = header.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 20f;
                headerLayout.padding = new RectOffset(20, 20, 20, 20);
            }

            // PlayerListPanel
            GameObject playerListPanel = GetOrCreateChild(lobbyPanel.transform, "PlayerListPanel");
            if (playerListPanel == null)
            {
                Debug.LogError("❌ Failed to create PlayerListPanel!");
                return;
            }

            GameObject scrollView = GetOrCreateChild(playerListPanel.transform, "ScrollView");
            if (scrollView == null)
            {
                Debug.LogError("❌ Failed to create ScrollView!");
                return;
            }

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollView.AddComponent<ScrollRect>();
            }

            Image scrollBg = scrollView.GetComponent<Image>();
            if (scrollBg == null)
            {
                scrollBg = scrollView.AddComponent<Image>();
            }
            scrollBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            GameObject viewport = GetOrCreateChild(scrollView.transform, "Viewport");
            if (viewport == null)
            {
                Debug.LogError("❌ Failed to create Viewport!");
                return;
            }

            Mask viewportMask = viewport.GetComponent<Mask>();
            if (viewportMask == null)
            {
                viewportMask = viewport.AddComponent<Mask>();
            }

            Image viewportBg = viewport.GetComponent<Image>();
            if (viewportBg == null)
            {
                viewportBg = viewport.AddComponent<Image>();
            }
            viewportBg.color = Color.clear;
            
            GameObject content = GetOrCreateChild(viewport.transform, "Content");
            if (content == null)
            {
                Debug.LogError("❌ Failed to create Content!");
                return;
            }

            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.AddComponent<ContentSizeFitter>();
            }
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // ✅ ScrollRect referanslarını ata
            RectTransform contentRect = content.GetComponent<RectTransform>();
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            
            if (contentRect != null && viewportRect != null && scrollRect != null)
            {
                scrollRect.content = contentRect;
                scrollRect.viewport = viewportRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            else
            {
                Debug.LogError("❌ Failed to assign ScrollRect references!");
            }

            // ControlsPanel
            GameObject controlsPanel = GetOrCreateChild(lobbyPanel.transform, "ControlsPanel");
            if (controlsPanel == null)
            {
                Debug.LogError("❌ Failed to create ControlsPanel!");
                return;
            }

            VerticalLayoutGroup controlsLayout = controlsPanel.GetComponent<VerticalLayoutGroup>();
            if (controlsLayout == null)
            {
                controlsLayout = controlsPanel.AddComponent<VerticalLayoutGroup>();
                controlsLayout.spacing = 10f;
                controlsLayout.padding = new RectOffset(20, 20, 20, 20);
            }
            
            // HostControls
            GameObject hostControls = GetOrCreateChild(controlsPanel.transform, "HostControls");
            if (hostControls == null)
            {
                Debug.LogError("❌ Failed to create HostControls!");
                return;
            }

            HorizontalLayoutGroup hostLayout = hostControls.GetComponent<HorizontalLayoutGroup>();
            if (hostLayout == null)
            {
                hostLayout = hostControls.AddComponent<HorizontalLayoutGroup>();
                hostLayout.spacing = 10f;
            }
            GameObject startGameButton = GetOrCreateButton(hostControls.transform, "StartGameButton", "START GAME");
            GetOrCreateButton(hostControls.transform, "AutoBalanceButton", "AUTO BALANCE");

            // PlayerControls
            GameObject playerControls = GetOrCreateChild(controlsPanel.transform, "PlayerControls");
            if (playerControls == null)
            {
                Debug.LogError("❌ Failed to create PlayerControls!");
                return;
            }

            HorizontalLayoutGroup playerLayout = playerControls.GetComponent<HorizontalLayoutGroup>();
            if (playerLayout == null)
            {
                playerLayout = playerControls.AddComponent<HorizontalLayoutGroup>();
                playerLayout.spacing = 10f;
            }
            GameObject readyButton = GetOrCreateButton(playerControls.transform, "ReadyButton", "READY");
            if (readyButton != null)
            {
                GetOrCreateTextChild(readyButton.transform, "ReadyButtonText", "READY");
            }
            GetOrCreateButton(playerControls.transform, "TeamAButton", "TEAM A");
            GetOrCreateButton(playerControls.transform, "TeamBButton", "TEAM B");
            GetOrCreateButton(playerControls.transform, "LeaveButton", "LEAVE");

            // WaitingForHostPanel
            GameObject waitingPanel = GetOrCreateChild(controlsPanel.transform, "WaitingForHostPanel");
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(false);
                GetOrCreateTextChild(waitingPanel.transform, "WaitingText", "Waiting for host to start...");
            }

            // ErrorPanel
            GameObject errorPanel = GetOrCreateChild(lobbyPanel.transform, "ErrorPanel");
            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
                GetOrCreateTextChild(errorPanel.transform, "ErrorText", "");
            }

            // GameStartingPanel
            GameObject gameStartingPanel = GetOrCreateChild(canvasTransform, "GameStartingPanel");
            if (gameStartingPanel != null)
            {
                gameStartingPanel.SetActive(false);
                GetOrCreateTextChild(gameStartingPanel.transform, "Text", "GAME STARTING...");
            }

            Debug.Log("✅ UI Hierarchy created");
        }

        private GameObject GetOrCreateChild(Transform parent, string name)
        {
            if (parent == null)
            {
                Debug.LogError($"❌ GetOrCreateChild: Parent is null for '{name}'!");
                return null;
            }

            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            GameObject go = new GameObject(name);
            if (go == null)
            {
                Debug.LogError($"❌ Failed to create GameObject '{name}'!");
                return null;
            }

            go.transform.SetParent(parent, false);
            
            RectTransform rect = go.AddComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }
            
            return go;
        }

        private GameObject GetOrCreateTextChild(Transform parent, string name, string text)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                var tmp = child.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = text;
                return child.gameObject;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            TMPro.TextMeshProUGUI tmpComponent = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmpComponent.text = text;
            tmpComponent.fontSize = 24;
            tmpComponent.color = Color.white;
            tmpComponent.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            return go;
        }

        private GameObject GetOrCreateButton(Transform parent, string name, string text)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f);
            
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f);
            button.colors = colors;
            
            GetOrCreateTextChild(go.transform, "Text", text);
            
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            
            return go;
        }

        private void CreateGameModeSelectionUI(Transform canvasTransform)
        {
            Debug.Log("🎮 Creating GameModeSelection UI...");

            // Check if already exists
            GameObject existing = GameObject.Find("GameModeSelectionPanel");
            if (existing != null)
            {
                Debug.Log("✅ GameModeSelectionPanel already exists");
                return;
            }

            // Main panel
            GameObject panel = new GameObject("GameModeSelectionPanel");
            panel.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            // Content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(800, 600);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 30f;
            contentLayout.padding = new RectOffset(40, 40, 40, 40);
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;

            // Title
            GameObject title = GetOrCreateTextChild(content.transform, "TitleText", "OYUN MODU SEÇİMİ");
            var titleTMP = title.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleTMP != null)
            {
                titleTMP.fontSize = 48;
                titleTMP.fontStyle = TMPro.FontStyles.Bold;
            }

            // Description
            GameObject desc = GetOrCreateTextChild(content.transform, "DescriptionText", "Oyun modunu seçin");
            var descTMP = desc.GetComponent<TMPro.TextMeshProUGUI>();
            if (descTMP != null)
            {
                descTMP.fontSize = 24;
                descTMP.color = new Color(0.8f, 0.8f, 0.8f);
            }

            // Mode buttons container
            GameObject buttonsContainer = new GameObject("ModeButtons");
            buttonsContainer.transform.SetParent(content.transform, false);
            RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(700, 300);

            HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 30f;
            buttonsLayout.padding = new RectOffset(0, 0, 0, 0);
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childForceExpandWidth = true;
            buttonsLayout.childForceExpandHeight = true;

            // Individual Mode Button
            GameObject individualBtn = GetOrCreateButton(buttonsContainer.transform, "IndividualModeButton", "BİREYSEL");
            var individualTMP = individualBtn.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (individualTMP != null)
            {
                individualTMP.fontSize = 32;
                individualTMP.fontStyle = TMPro.FontStyles.Bold;
            }

            // Team Mode Button
            GameObject teamBtn = GetOrCreateButton(buttonsContainer.transform, "TeamModeButton", "TAKIM");
            var teamTMP = teamBtn.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (teamTMP != null)
            {
                teamTMP.fontSize = 32;
                teamTMP.fontStyle = TMPro.FontStyles.Bold;
            }

            // Mode description panel
            GameObject descPanel = new GameObject("ModeDescriptionPanel");
            descPanel.transform.SetParent(content.transform, false);
            RectTransform descRect = descPanel.AddComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(700, 150);

            Image descBg = descPanel.AddComponent<Image>();
            descBg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

            GameObject descText = GetOrCreateTextChild(descPanel.transform, "ModeDescriptionText", "");
            var descTextTMP = descText.GetComponent<TMPro.TextMeshProUGUI>();
            if (descTextTMP != null)
            {
                descTextTMP.fontSize = 20;
                descTextTMP.alignment = TMPro.TextAlignmentOptions.Center;
            }

            // Confirm button
            GameObject confirmBtn = GetOrCreateButton(content.transform, "ConfirmButton", "ONAYLA");
            var confirmTMP = confirmBtn.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (confirmTMP != null)
            {
                confirmTMP.fontSize = 28;
                confirmTMP.fontStyle = TMPro.FontStyles.Bold;
            }
            RectTransform confirmRect = confirmBtn.GetComponent<RectTransform>();
            confirmRect.sizeDelta = new Vector2(300, 60);

            // Add GameModeSelectionUI component
            TacticalCombat.UI.GameModeSelectionUI gameModeUI = panel.AddComponent<TacticalCombat.UI.GameModeSelectionUI>();

            // Assign references using SerializedObject
            SerializedObject serializedUI = new SerializedObject(gameModeUI);
            AssignReference(serializedUI, "selectionPanel", panel);
            AssignReference(serializedUI, "individualModeButton", individualBtn);
            AssignReference(serializedUI, "teamModeButton", teamBtn);
            AssignReference(serializedUI, "confirmButton", confirmBtn);
            AssignReference(serializedUI, "modeDescriptionText", descText);
            AssignReference(serializedUI, "individualDescriptionPanel", descPanel);
            AssignReference(serializedUI, "teamDescriptionPanel", descPanel); // Same panel for both
            serializedUI.ApplyModifiedProperties();

            Debug.Log("✅ GameModeSelection UI created");
        }

        private void CreateLobbyManagerPrefab()
        {
            Debug.Log("📦 Creating LobbyManager Prefab...");

            // Prefab zaten var mı kontrol et
            string prefabPath = "Assets/Prefabs/LobbyManager.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (existingPrefab != null)
            {
                Debug.Log($"✅ LobbyManager prefab already exists at {prefabPath}");
                return;
            }

            // Yeni GameObject oluştur
            GameObject lobbyManagerGO = new GameObject("LobbyManager");
            
            // Component'leri ekle
            NetworkIdentity netIdentity = lobbyManagerGO.AddComponent<NetworkIdentity>();
            LobbyManager lobbyManager = lobbyManagerGO.AddComponent<LobbyManager>();

            // NetworkIdentity ayarları
            netIdentity.serverOnly = false; // Client'lar da görebilmeli

            // LobbyManager ayarları (Inspector'da ayarlanacak ama default değerler)
            // SerializedField'lar Inspector'da görünecek

            // Prefab klasörü yoksa oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(lobbyManagerGO, prefabPath);
            DestroyImmediate(lobbyManagerGO);

            Debug.Log($"✅ LobbyManager prefab created at {prefabPath}");
        }

        private void CreatePlayerListItemPrefab()
        {
            Debug.Log("📦 Creating PlayerListItem Prefab...");

            string prefabPath = "Assets/Prefabs/UI/PlayerListItemPrefab.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (existingPrefab != null)
            {
                Debug.Log($"✅ PlayerListItem prefab already exists at {prefabPath}");
                return;
            }

            // Yeni GameObject oluştur
            GameObject itemGO = new GameObject("PlayerListItem");

            // Image component (background)
            Image background = itemGO.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // RectTransform ayarları
            RectTransform rect = itemGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 60);

            // NameText
            GameObject nameTextGO = new GameObject("NameText");
            nameTextGO.transform.SetParent(itemGO.transform);
            TextMeshProUGUI nameText = nameTextGO.AddComponent<TextMeshProUGUI>();
            nameText.text = "Player Name";
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;
            RectTransform nameRect = nameTextGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.4f, 1);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-10, 0);

            // TeamText
            GameObject teamTextGO = new GameObject("TeamText");
            teamTextGO.transform.SetParent(itemGO.transform);
            TextMeshProUGUI teamText = teamTextGO.AddComponent<TextMeshProUGUI>();
            teamText.text = "NO TEAM";
            teamText.fontSize = 16;
            teamText.color = Color.gray;
            teamText.alignment = TextAlignmentOptions.Center;
            RectTransform teamRect = teamTextGO.GetComponent<RectTransform>();
            teamRect.anchorMin = new Vector2(0.4f, 0);
            teamRect.anchorMax = new Vector2(0.7f, 1);
            teamRect.offsetMin = Vector2.zero;
            teamRect.offsetMax = Vector2.zero;

            // ReadyText
            GameObject readyTextGO = new GameObject("ReadyText");
            readyTextGO.transform.SetParent(itemGO.transform);
            TextMeshProUGUI readyText = readyTextGO.AddComponent<TextMeshProUGUI>();
            readyText.text = "○ NOT READY";
            readyText.fontSize = 16;
            readyText.color = Color.red;
            readyText.alignment = TextAlignmentOptions.Right;
            RectTransform readyRect = readyTextGO.GetComponent<RectTransform>();
            readyRect.anchorMin = new Vector2(0.7f, 0);
            readyRect.anchorMax = new Vector2(1, 1);
            readyRect.offsetMin = new Vector2(10, 0);
            readyRect.offsetMax = new Vector2(-10, 0);

            // HostIcon (başlangıçta gizli)
            GameObject hostIconGO = new GameObject("HostIcon");
            hostIconGO.transform.SetParent(itemGO.transform);
            Image hostIcon = hostIconGO.AddComponent<Image>();
            hostIcon.color = Color.yellow;
            hostIconGO.SetActive(false);
            RectTransform hostRect = hostIconGO.GetComponent<RectTransform>();
            hostRect.anchorMin = new Vector2(0.95f, 0.5f);
            hostRect.anchorMax = new Vector2(1, 1);
            hostRect.sizeDelta = new Vector2(30, 30);
            hostRect.anchoredPosition = Vector2.zero;

            // Prefab klasörü yoksa oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // Prefab olarak kaydet
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(itemGO, prefabPath);
            DestroyImmediate(itemGO);

            Debug.Log($"✅ PlayerListItem prefab created at {prefabPath}");
        }

        private void ConfigureNetworkManager()
        {
            Debug.Log("🔧 Configuring NetworkManager...");

            // NetworkManager'ı bul
            NetworkGameManager networkManager = FindFirstObjectByType<NetworkGameManager>();
            
            if (networkManager == null)
            {
                Debug.LogWarning("⚠️ NetworkGameManager not found in scene! Please add it manually.");
                return;
            }

            // LobbyManager prefab'ı yükle
            GameObject lobbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyManager.prefab");
            
            if (lobbyPrefab == null)
            {
                Debug.LogWarning("⚠️ LobbyManager prefab not found! Run 'Create LobbyManager Prefab' first.");
                return;
            }

            // SerializedObject kullanarak prefab referansını ata
            SerializedObject serializedManager = new SerializedObject(networkManager);
            SerializedProperty lobbyPrefabProperty = serializedManager.FindProperty("lobbyManagerPrefab");
            
            if (lobbyPrefabProperty != null)
            {
                lobbyPrefabProperty.objectReferenceValue = lobbyPrefab;
                serializedManager.ApplyModifiedProperties();
                Debug.Log("✅ LobbyManager prefab assigned to NetworkManager");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find 'lobbyManagerPrefab' property. Please assign manually in Inspector.");
            }

            // Spawnable prefabs listesine ekle
            SerializedProperty spawnListProperty = serializedManager.FindProperty("spawnPrefabs");
            if (spawnListProperty != null)
            {
                bool alreadyAdded = false;
                for (int i = 0; i < spawnListProperty.arraySize; i++)
                {
                    if (spawnListProperty.GetArrayElementAtIndex(i).objectReferenceValue == lobbyPrefab)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    spawnListProperty.arraySize++;
                    spawnListProperty.GetArrayElementAtIndex(spawnListProperty.arraySize - 1).objectReferenceValue = lobbyPrefab;
                    serializedManager.ApplyModifiedProperties();
                    Debug.Log("✅ LobbyManager added to spawnable prefabs list");
                }
            }
        }
    }
}

