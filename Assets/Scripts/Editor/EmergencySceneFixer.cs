using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Combat;
using TacticalCombat.UI;
using TacticalCombat.Network;
using TacticalCombat.Player;
using TacticalCombat.Building;

namespace TacticalCombat.Editor
{
    public class EmergencySceneFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🚨 Emergency Scene Fixer")]
        public static void ShowWindow()
        {
            GetWindow<EmergencySceneFixer>("Emergency Scene Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("🚨 Emergency Scene Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Problem: Siyah ekran ve missing script hataları", EditorStyles.helpBox);
            GUILayout.Label("Sebep: Scene corruption ve script referansları kaybolmuş", EditorStyles.helpBox);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Check Script Status", GUILayout.Height(30)))
            {
                CheckScriptStatus();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔄 Recompile All Scripts", GUILayout.Height(30)))
            {
                RecompileScripts();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🏗️ Rebuild Scene from Scratch", GUILayout.Height(30)))
            {
                RebuildScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🎯 Fix Missing Script References", GUILayout.Height(30)))
            {
                FixMissingScriptReferences();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📷 Fix Camera Issues", GUILayout.Height(30)))
            {
                FixCameraIssues();
            }
        }

        private void CheckScriptStatus()
        {
            Debug.Log("🔍 Checking Script Status...");
            
            // Check if scripts exist
            var scriptPaths = new string[]
            {
                "Assets/Scripts/Core/Unity6Optimizations.cs",
                "Assets/Scripts/UI/CombatUI.cs",
                "Assets/Scripts/Player/PlayerController.cs",
                "Assets/Scripts/Core/MatchManager.cs",
                "Assets/Scripts/Combat/Health.cs",
                "Assets/Scripts/Network/SimpleNetworkHUD.cs"
            };

            foreach (var path in scriptPaths)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    Debug.Log($"✅ Script exists: {path}");
                }
                else
                {
                    Debug.LogError($"❌ Script missing: {path}");
                }
            }

            // Check compilation status
            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("⚠️ Scripts are currently compiling...");
            }
            else
            {
                Debug.Log("✅ Scripts compilation complete");
            }
        }

        private void RecompileScripts()
        {
            Debug.Log("🔄 Recompiling All Scripts...");
            
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
            
            Debug.Log("✅ Script recompilation requested");
        }

        private void RebuildScene()
        {
            Debug.Log("🏗️ Rebuilding Scene from Scratch...");
            
            // Clear current scene
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            
            foreach (var obj in rootObjects)
            {
                if (obj.name != "Main Camera" && obj.name != "Directional Light")
                {
                    DestroyImmediate(obj);
                }
            }

            // Create basic scene structure
            CreateBasicScene();
            
            Debug.Log("✅ Scene rebuilt from scratch");
        }

        private void CreateBasicScene()
        {
            // Create GameManager
            var gameManager = new GameObject("GameManager");
            gameManager.AddComponent<MatchManager>();
            gameManager.AddComponent<NetworkIdentity>();

            // Create NetworkManager (use custom NetworkGameManager)
            var networkManager = new GameObject("NetworkManager");
            networkManager.AddComponent<NetworkGameManager>();
            networkManager.AddComponent<kcp2k.KcpTransport>();

            // Create UI Canvas
            var canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create Crosshair
            var crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvas.transform);
            crosshair.AddComponent<UnityEngine.UI.Image>();

            // Create CombatUI
            var combatUI = new GameObject("CombatUI");
            combatUI.transform.SetParent(canvas.transform);
            combatUI.AddComponent<CombatUI>();

            // Create Team Cores
            CreateTeamCore("TeamA_Core", Team.TeamA);
            CreateTeamCore("TeamB_Core", Team.TeamB);

            // Create Player Prefab
            CreatePlayerPrefab();

            Debug.Log("✅ Basic scene structure created");
        }

        private void CreateTeamCore(string name, Team team)
        {
            var core = new GameObject(name);
            core.AddComponent<Health>();
            // Remove invalid MonoBehaviour add; ensure NetworkIdentity exists
            core.AddComponent<NetworkIdentity>();
            
            Debug.Log($"✅ Created {name} for {team}");
        }

        private void CreatePlayerPrefab()
        {
            var player = new GameObject("Player");
            player.AddComponent<CharacterController>();
            player.AddComponent<NetworkIdentity>();
            // Use Mirror's reliable NetworkTransform variant present in this project
            player.AddComponent<Mirror.NetworkTransformReliable>();
            player.AddComponent<PlayerController>();
            player.AddComponent<FPSController>();
            player.AddComponent<WeaponSystem>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerVisuals>();

            // Create Camera
            var camera = new GameObject("PlayerCamera");
            camera.transform.SetParent(player.transform);
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();

            Debug.Log("✅ Player prefab created");
        }

        private void FixMissingScriptReferences()
        {
            Debug.Log("🎯 Fixing Missing Script References...");
            
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int fixedCount = 0;

            foreach (var obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        Debug.LogWarning($"⚠️ Missing component on {obj.name}");
                        fixedCount++;
                    }
                }
            }

            Debug.Log($"✅ Found {fixedCount} missing component references");
        }

        private void FixCameraIssues()
        {
            Debug.Log("📷 Fixing Camera Issues...");
            
            // Find main camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    mainCamera = cameras[0];
                    mainCamera.tag = "MainCamera";
                    Debug.Log("✅ Main camera tag assigned");
                }
                else
                {
                    // Create main camera
                    var cameraObj = new GameObject("Main Camera");
                    cameraObj.AddComponent<Camera>();
                    cameraObj.AddComponent<AudioListener>();
                    cameraObj.tag = "MainCamera";
                    Debug.Log("✅ Main camera created");
                }
            }

            // Set camera position
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 1.6f, 0);
                mainCamera.transform.rotation = Quaternion.identity;
                Debug.Log("✅ Camera position set");
            }
        }
    }
}
