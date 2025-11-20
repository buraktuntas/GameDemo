using UnityEngine;
using UnityEditor;
using TacticalCombat.Network;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Automatically fixes missing references in Lobby System
    /// Finds and assigns all necessary references for NetworkGameManager, LobbyUI, GameModeSelectionUI
    /// </summary>
    public class LobbyReferenceFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🔧 Fix Lobby References")]
        public static void ShowWindow()
        {
            GetWindow<LobbyReferenceFixer>("Fix Lobby References");
        }

        private void OnGUI()
        {
            GUILayout.Label("🔧 LOBBY REFERENCE FIXER", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Check & Fix All References", GUILayout.Height(40)))
            {
                FixAllReferences();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📋 List Missing References", GUILayout.Height(30)))
            {
                ListMissingReferences();
            }

            GUILayout.Space(20);
            GUILayout.Label("This will:", EditorStyles.label);
            GUILayout.Label("• Find NetworkGameManager and assign LobbyManager prefab", EditorStyles.helpBox);
            GUILayout.Label("• Find LobbyUI and assign all UI references", EditorStyles.helpBox);
            GUILayout.Label("• Find GameModeSelectionUI and verify references", EditorStyles.helpBox);
            GUILayout.Label("• Check all UI panels and buttons", EditorStyles.helpBox);
        }

        private void FixAllReferences()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔧 LOBBY REFERENCE FIXER STARTING...");
            Debug.Log("═══════════════════════════════════════");

            bool allFixed = true;

            // 1. Fix NetworkGameManager
            allFixed &= FixNetworkGameManager();

            // 2. Fix LobbyUI
            allFixed &= FixLobbyUI();

            // 3. Fix GameModeSelectionUI
            allFixed &= FixGameModeSelectionUI();

            Debug.Log("═══════════════════════════════════════");
            if (allFixed)
            {
                Debug.Log("✅ ALL REFERENCES FIXED SUCCESSFULLY!");
            }
            else
            {
                Debug.LogWarning("⚠️ SOME REFERENCES COULD NOT BE FIXED - Check logs above");
            }
            Debug.Log("═══════════════════════════════════════");

            EditorUtility.DisplayDialog("Reference Fixer", 
                allFixed ? "✅ All references fixed!" : "⚠️ Some references could not be fixed. Check Console for details.", 
                "OK");
        }

        private bool FixNetworkGameManager()
        {
            Debug.Log("\n📋 [1/3] Fixing NetworkGameManager...");

            // Find NetworkGameManager in scene
            NetworkGameManager networkManager = FindFirstObjectByType<NetworkGameManager>();
            if (networkManager == null)
            {
                Debug.LogError("❌ NetworkGameManager not found in scene!");
                return false;
            }

            Debug.Log($"✅ Found NetworkGameManager: {networkManager.name}");

            // Check if lobbyManagerPrefab is assigned
            SerializedObject serializedManager = new SerializedObject(networkManager);
            SerializedProperty lobbyPrefabProp = serializedManager.FindProperty("lobbyManagerPrefab");

            if (lobbyPrefabProp.objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ LobbyManager prefab not assigned! Searching for prefab...");

                // Try to find LobbyManager prefab
                string[] guids = AssetDatabase.FindAssets("t:Prefab LobbyManager");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    if (prefab != null && prefab.GetComponent<LobbyManager>() != null)
                    {
                        lobbyPrefabProp.objectReferenceValue = prefab;
                        serializedManager.ApplyModifiedProperties();
                        Debug.Log($"✅ Assigned LobbyManager prefab: {path}");
                    }
                    else
                    {
                        Debug.LogError("❌ LobbyManager prefab found but doesn't have LobbyManager component!");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("❌ LobbyManager prefab not found! Please create it first using 'Auto Setup Lobby System'");
                    return false;
                }
            }
            else
            {
                Debug.Log($"✅ LobbyManager prefab already assigned: {lobbyPrefabProp.objectReferenceValue.name}");
            }

            return true;
        }

        private bool FixLobbyUI()
        {
            Debug.Log("\n📋 [2/3] Fixing LobbyUI...");

            // Find LobbyUI in scene (including inactive)
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include);
            if (lobbyUI == null)
            {
                Debug.LogWarning("⚠️ LobbyUI not found in scene! It might not be created yet.");
                Debug.LogWarning("   Run 'Auto Setup Lobby System' first to create it.");
                return false;
            }

            Debug.Log($"✅ Found LobbyUI: {lobbyUI.name} (Active: {lobbyUI.gameObject.activeSelf})");

            SerializedObject serializedUI = new SerializedObject(lobbyUI);
            bool allFixed = true;

            // Check lobbyManager reference (this is set at runtime, so we'll just verify it exists)
            Debug.Log("ℹ️ LobbyManager reference is set at runtime via LobbyManager.Instance");

            // Find parent Canvas for searching
            Canvas canvas = lobbyUI.GetComponentInParent<Canvas>();
            Transform searchRoot = canvas != null ? canvas.transform : lobbyUI.transform;

            Debug.Log($"🔍 Searching in: {(canvas != null ? canvas.name : lobbyUI.transform.name)}");

            // Check UI panel references (try multiple possible names)
            allFixed &= CheckAndAssignReference(serializedUI, "lobbyPanel", new[] { "LobbyPanel", "lobbyPanel", "Lobby" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "gameStartingPanel", new[] { "GameStartingPanel", "GameStarting", "StartingPanel" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "errorPanel", new[] { "ErrorPanel", "Error", "ErrorMessage" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "playerListContainer", new[] { "PlayerListPanel", "PlayerList", "Content", "PlayerListContainer" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "teamSelectionPanel", new[] { "TeamSelectionPanel", "TeamSelection", "TeamPanel" }, searchRoot);

            // Check button references
            allFixed &= CheckAndAssignReference(serializedUI, "startGameButton", new[] { "StartGameButton", "StartButton", "Start" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "readyButton", new[] { "ReadyButton", "Ready", "ReadyBtn" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "teamAButton", new[] { "TeamAButton", "TeamA", "TeamABtn" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "teamBButton", new[] { "TeamBButton", "TeamB", "TeamBBtn" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "leaveButton", new[] { "LeaveButton", "Leave", "LeaveBtn" }, searchRoot);

            // Check text references
            allFixed &= CheckAndAssignReference(serializedUI, "lobbyTitleText", new[] { "LobbyTitleText", "TitleText", "Title", "LobbyTitle" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "playerCountText", new[] { "PlayerCountText", "PlayerCount", "CountText" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "errorText", new[] { "ErrorText", "Error", "ErrorMessageText" }, searchRoot);

            // Check prefab reference
            SerializedProperty prefabProp = serializedUI.FindProperty("playerListItemPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab PlayerListItemPrefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        prefabProp.objectReferenceValue = prefab;
                        Debug.Log($"✅ Assigned playerListItemPrefab: {path}");
                    }
                }
            }

            serializedUI.ApplyModifiedProperties();

            return allFixed;
        }

        private bool FixGameModeSelectionUI()
        {
            Debug.Log("\n📋 [3/3] Fixing GameModeSelectionUI...");

            // Find GameModeSelectionUI in scene (including inactive)
            GameModeSelectionUI gameModeUI = FindFirstObjectByType<GameModeSelectionUI>(FindObjectsInactive.Include);
            if (gameModeUI == null)
            {
                Debug.LogWarning("⚠️ GameModeSelectionUI not found in scene! It might not be created yet.");
                Debug.LogWarning("   Run 'Auto Setup Lobby System' first to create it.");
                return false;
            }

            Debug.Log($"✅ Found GameModeSelectionUI: {gameModeUI.name} (Active: {gameModeUI.gameObject.activeSelf})");

            SerializedObject serializedUI = new SerializedObject(gameModeUI);
            bool allFixed = true;

            // Find parent Canvas for searching
            Canvas canvas = gameModeUI.GetComponentInParent<Canvas>();
            Transform searchRoot = canvas != null ? canvas.transform : gameModeUI.transform;

            Debug.Log($"🔍 Searching in: {(canvas != null ? canvas.name : gameModeUI.transform.name)}");

            // Check panel references (try multiple possible names)
            allFixed &= CheckAndAssignReference(serializedUI, "selectionPanel", new[] { "GameModeSelectionPanel", "SelectionPanel", "GameModePanel" }, searchRoot);

            // Check button references
            allFixed &= CheckAndAssignReference(serializedUI, "individualModeButton", new[] { "IndividualModeButton", "IndividualButton", "Individual", "BireyselButton" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "teamModeButton", new[] { "TeamModeButton", "TeamButton", "Team", "TakimButton" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "confirmButton", new[] { "ConfirmButton", "Confirm", "OnaylaButton", "Onayla" }, searchRoot);

            // Check text references
            allFixed &= CheckAndAssignReference(serializedUI, "modeTitleText", new[] { "ModeTitleText", "TitleText", "Title", "ModeTitle" }, searchRoot);
            allFixed &= CheckAndAssignReference(serializedUI, "modeDescriptionText", new[] { "ModeDescriptionText", "DescriptionText", "Description", "ModeDescription" }, searchRoot);

            serializedUI.ApplyModifiedProperties();

            return allFixed;
        }

        private bool CheckAndAssignReference(SerializedObject serializedObject, string propertyName, string searchName, Transform parent)
        {
            return CheckAndAssignReference(serializedObject, propertyName, new[] { searchName }, parent);
        }

        private bool CheckAndAssignReference(SerializedObject serializedObject, string propertyName, string[] searchNames, Transform parent)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"⚠️ Property '{propertyName}' not found in {serializedObject.targetObject.GetType().Name}");
                return true; // Not critical, continue
            }

            if (prop.objectReferenceValue == null)
            {
                Transform found = null;
                
                // Try each possible name
                foreach (string searchName in searchNames)
                {
                    found = FindChildRecursive(parent, searchName);
                    if (found != null)
                    {
                        Debug.Log($"🔍 Found '{searchName}' for {propertyName}");
                        break;
                    }
                }

                if (found != null)
                {
                    // Determine the type and assign accordingly
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Component component = null;
                        
                        if (propertyName.Contains("Button"))
                        {
                            component = found.GetComponent<UnityEngine.UI.Button>();
                            if (component == null)
                            {
                                Debug.LogWarning($"⚠️ Found '{found.name}' but it doesn't have Button component");
                            }
                        }
                        else if (propertyName.Contains("Text") || propertyName.Contains("TextMeshProUGUI"))
                        {
                            component = found.GetComponent<TMPro.TextMeshProUGUI>();
                            if (component == null)
                            {
                                // Try regular Text component as fallback
                                component = found.GetComponent<UnityEngine.UI.Text>();
                            }
                        }
                        else if (propertyName.Contains("Panel") || propertyName.Contains("Container"))
                        {
                            // For panels/containers, assign the GameObject itself
                            prop.objectReferenceValue = found.gameObject;
                            Debug.Log($"✅ Assigned {propertyName}: {found.name}");
                            return true;
                        }

                        if (component != null)
                        {
                            prop.objectReferenceValue = component;
                            Debug.Log($"✅ Assigned {propertyName}: {found.name}");
                            return true;
                        }
                        else if (!propertyName.Contains("Panel") && !propertyName.Contains("Container"))
                        {
                            Debug.LogWarning($"⚠️ Found '{found.name}' but couldn't get appropriate component for {propertyName}");
                        }
                    }
                }

                Debug.LogWarning($"⚠️ Could not find {propertyName} (searched for: {string.Join(", ", searchNames)})");
                Debug.LogWarning($"   Searched in: {parent.name} and children");
                return false;
            }
            else
            {
                Debug.Log($"✅ {propertyName} already assigned: {prop.objectReferenceValue.name}");
                return true;
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == name || child.name.Contains(name))
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void ListMissingReferences()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📋 LISTING MISSING REFERENCES...");
            Debug.Log("═══════════════════════════════════════");

            // Check NetworkGameManager
            NetworkGameManager networkManager = FindFirstObjectByType<NetworkGameManager>();
            if (networkManager != null)
            {
                SerializedObject serialized = new SerializedObject(networkManager);
                SerializedProperty prefabProp = serialized.FindProperty("lobbyManagerPrefab");
                if (prefabProp == null || prefabProp.objectReferenceValue == null)
                {
                    Debug.LogWarning("❌ NetworkGameManager.lobbyManagerPrefab is MISSING");
                }
                else
                {
                    Debug.Log($"✅ NetworkGameManager.lobbyManagerPrefab: {prefabProp.objectReferenceValue.name}");
                }
            }

            // Check LobbyUI
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include);
            if (lobbyUI != null)
            {
                Debug.Log("\n📋 LobbyUI Missing References:");
                ListMissingReferencesForObject(lobbyUI, new[]
                {
                    "lobbyPanel", "gameStartingPanel", "errorPanel", "playerListContainer", "teamSelectionPanel",
                    "startGameButton", "readyButton", "teamAButton", "teamBButton", "leaveButton",
                    "lobbyTitleText", "playerCountText", "errorText", "playerListItemPrefab"
                });
            }
            else
            {
                Debug.LogWarning("❌ LobbyUI not found in scene!");
            }

            // Check GameModeSelectionUI
            GameModeSelectionUI gameModeUI = FindFirstObjectByType<GameModeSelectionUI>(FindObjectsInactive.Include);
            if (gameModeUI != null)
            {
                Debug.Log("\n📋 GameModeSelectionUI Missing References:");
                ListMissingReferencesForObject(gameModeUI, new[]
                {
                    "selectionPanel", "individualModeButton", "teamModeButton", "confirmButton",
                    "modeTitleText", "modeDescriptionText"
                });
            }
            else
            {
                Debug.LogWarning("❌ GameModeSelectionUI not found in scene!");
            }

            Debug.Log("═══════════════════════════════════════");
        }

        private void ListMissingReferencesForObject(UnityEngine.Object obj, string[] propertyNames)
        {
            SerializedObject serialized = new SerializedObject(obj);
            bool hasMissing = false;

            foreach (string propName in propertyNames)
            {
                SerializedProperty prop = serialized.FindProperty(propName);
                if (prop == null)
                {
                    Debug.LogWarning($"  ⚠️ Property '{propName}' not found in {obj.GetType().Name}");
                    hasMissing = true;
                }
                else if (prop.objectReferenceValue == null)
                {
                    Debug.LogWarning($"  ❌ {propName} is MISSING");
                    hasMissing = true;
                }
                else
                {
                    Debug.Log($"  ✅ {propName}: {prop.objectReferenceValue.name}");
                }
            }

            if (!hasMissing)
            {
                Debug.Log("  ✅ All references are assigned!");
            }
        }
    }
}

