using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ Auto-setup UIFlowManager in scene
    /// </summary>
    [InitializeOnLoad]
    public static class UIFlowManagerAutoSetup
    {
        static UIFlowManagerAutoSetup()
        {
            EditorApplication.delayCall += SetupUIFlowManager;
        }

        [MenuItem("Tools/Tactical Combat/🔧 Setup UIFlowManager")]
        public static void SetupUIFlowManager()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene == null || !activeScene.isLoaded)
            {
                Debug.LogWarning("⚠️ No active scene loaded. Please open a scene first.");
                return;
            }

            Debug.Log("🔧 [UIFlowManagerAutoSetup] Setting up UIFlowManager...");

            // Check if UIFlowManager already exists
            UIFlowManager existingManager = Object.FindFirstObjectByType<UIFlowManager>();
            if (existingManager != null)
            {
                Debug.Log($"✅ UIFlowManager already exists: {existingManager.name}");
                
                // Auto-assign references if missing
                AutoAssignReferences(existingManager);
                return;
            }

            // Create UIFlowManager GameObject
            GameObject managerObj = new GameObject("UIFlowManager");
            UIFlowManager manager = managerObj.AddComponent<UIFlowManager>();

            Debug.Log("✅ UIFlowManager GameObject created");

            // Auto-assign references
            AutoAssignReferences(manager);

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("✅ Scene marked dirty. Remember to save (Ctrl+S)!");
        }

        private static void AutoAssignReferences(UIFlowManager manager)
        {
            bool needsSave = false;

            // Find MainMenu
            var mainMenu = Object.FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                var serializedObject = new SerializedObject(manager);
                var mainMenuProp = serializedObject.FindProperty("mainMenu");
                if (mainMenuProp != null && mainMenuProp.objectReferenceValue == null)
                {
                    mainMenuProp.objectReferenceValue = mainMenu;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("✅ Assigned MainMenu reference");
                    needsSave = true;
                }
            }

            // Find GameModeSelectionUI
            var gameModeSelection = Object.FindFirstObjectByType<GameModeSelectionUI>();
            if (gameModeSelection != null)
            {
                var serializedObject = new SerializedObject(manager);
                var gameModeProp = serializedObject.FindProperty("gameModeSelection");
                if (gameModeProp != null && gameModeProp.objectReferenceValue == null)
                {
                    gameModeProp.objectReferenceValue = gameModeSelection;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("✅ Assigned GameModeSelectionUI reference");
                    needsSave = true;
                }
            }

            // Find LobbyUI
            var lobbyUI = Object.FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                var serializedObject = new SerializedObject(manager);
                var lobbyUIProp = serializedObject.FindProperty("lobbyUI");
                if (lobbyUIProp != null && lobbyUIProp.objectReferenceValue == null)
                {
                    lobbyUIProp.objectReferenceValue = lobbyUI;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("✅ Assigned LobbyUI reference");
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("✅ References assigned. Scene marked dirty.");
            }
            else
            {
                Debug.Log("ℹ️ All references already assigned or components not found.");
            }
        }
    }
}







