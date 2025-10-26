using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Quick fix for Build and Run black screen issue
    /// THE #1 CAUSE: Scene not added to Build Settings!
    /// </summary>
    public class BuildFixer : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Fix Build Black Screen ⚡")]
        public static void ShowWindow()
        {
            GetWindow<BuildFixer>("Build Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Build Black Screen Fixer", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "⚠️ BLACK SCREEN IN BUILD?\n\n" +
                "Most common cause: Scene not in Build Settings!\n\n" +
                "This tool fixes:\n" +
                "✓ Adds current scene to Build Settings\n" +
                "✓ Enables 'Run in Background' for multiplayer\n" +
                "✓ Checks NetworkManager configuration\n" +
                "✓ Verifies Player prefab has camera",
                MessageType.Warning
            );

            GUILayout.Space(10);

            if (GUILayout.Button("⚡ FIX BUILD ISSUES NOW", GUILayout.Height(50)))
            {
                FixBuildIssues();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📋 Show Build Settings", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }

        private void FixBuildIssues()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("⚡ BUILD FIXER STARTED");
            Debug.Log("═══════════════════════════════════════\n");

            int fixCount = 0;

            // FIX 1: Add scene to Build Settings
            fixCount += FixSceneInBuildSettings();

            // FIX 2: Enable Run in Background
            fixCount += FixRunInBackground();

            // FIX 3: Check NetworkManager
            fixCount += FixNetworkManager();

            // FIX 4: Check Player Camera
            fixCount += FixPlayerCamera();

            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log($"✅ BUILD FIXER COMPLETE - {fixCount} fixes applied");
            Debug.Log("═══════════════════════════════════════");

            EditorUtility.DisplayDialog(
                "Build Fixes Applied",
                $"✅ {fixCount} fixes applied!\n\n" +
                "Now you can:\n" +
                "1. File → Build and Run\n" +
                "2. Run the build as Server/Host\n" +
                "3. Run Unity Editor and join as client\n\n" +
                "Both should now work!",
                "OK"
            );
        }

        private int FixSceneInBuildSettings()
        {
            Debug.Log("📋 Checking Build Settings...");

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            if (string.IsNullOrEmpty(currentScenePath))
            {
                Debug.LogWarning("⚠️ Scene not saved! Save it first.");

                bool save = EditorUtility.DisplayDialog(
                    "Scene Not Saved",
                    "Scene must be saved before adding to Build Settings.\n\nSave now?",
                    "Save",
                    "Cancel"
                );

                if (save)
                {
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    currentScenePath = EditorSceneManager.GetActiveScene().path;
                }
                else
                {
                    return 0;
                }
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            bool sceneExists = scenes.Any(s => s.path == currentScenePath);
            bool sceneEnabled = scenes.Any(s => s.path == currentScenePath && s.enabled);

            if (!sceneExists || !sceneEnabled)
            {
                // Remove old entry if exists
                scenes.RemoveAll(s => s.path == currentScenePath);

                // Add at index 0 (main scene)
                scenes.Insert(0, new EditorBuildSettingsScene(currentScenePath, true));

                EditorBuildSettings.scenes = scenes.ToArray();

                Debug.Log($"✅ Scene added to Build Settings: {currentScenePath}");
                Debug.Log("   Scene will now be included in builds!");
                return 1;
            }
            else
            {
                Debug.Log("✓ Scene already in Build Settings");
                return 0;
            }
        }

        private int FixRunInBackground()
        {
            Debug.Log("\n🏃 Checking Run in Background...");

            if (!Application.runInBackground)
            {
                PlayerSettings.runInBackground = true;
                Debug.Log("✅ Run in Background enabled");
                Debug.Log("   Multiplayer testing will work better!");
                return 1;
            }
            else
            {
                Debug.Log("✓ Run in Background already enabled");
                return 0;
            }
        }

        private int FixNetworkManager()
        {
            Debug.Log("\n🌐 Checking NetworkManager...");

            var netMgr = FindFirstObjectByType<Mirror.NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ NO NETWORKMANAGER IN SCENE!");
                Debug.LogError("   Run: Tools → TacticalCombat → Ultimate Setup → BAŞLAT");
                return 0;
            }

            int fixes = 0;

            // Check Transport
            if (netMgr.transport == null)
            {
                var transport = netMgr.GetComponent<Mirror.Transport>();
                if (transport == null)
                {
                    transport = netMgr.gameObject.AddComponent<kcp2k.KcpTransport>();
                }
                netMgr.transport = transport;
                UnityEditor.EditorUtility.SetDirty(netMgr);
                Debug.Log("✅ Transport assigned to NetworkManager");
                fixes++;
            }
            else
            {
                Debug.Log("✓ Transport already assigned");
            }

            // Check Player Prefab
            if (netMgr.playerPrefab == null)
            {
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
                if (playerPrefab != null)
                {
                    netMgr.playerPrefab = playerPrefab;
                    UnityEditor.EditorUtility.SetDirty(netMgr);
                    Debug.Log("✅ Player Prefab assigned");
                    fixes++;
                }
                else
                {
                    Debug.LogError("❌ Player.prefab not found!");
                    Debug.LogError("   Run: Tools → TacticalCombat → Ultimate Setup → BAŞLAT");
                }
            }
            else
            {
                Debug.Log("✓ Player Prefab already assigned");
            }

            return fixes;
        }

        private int FixPlayerCamera()
        {
            Debug.Log("\n📷 Checking Player Camera...");

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player.prefab not found!");
                return 0;
            }

            Camera playerCam = playerPrefab.GetComponentInChildren<Camera>();

            if (playerCam == null)
            {
                Debug.LogError("❌ NO CAMERA IN PLAYER PREFAB!");
                Debug.LogError("   This is WHY you get black screen in builds!");
                Debug.LogError("   Run: Tools → TacticalCombat → Ultimate Setup → BAŞLAT");
                return 0;
            }

            int fixes = 0;

            if (!playerCam.enabled)
            {
                playerCam.enabled = true;
                UnityEditor.EditorUtility.SetDirty(playerCam.gameObject);
                Debug.Log("✅ Player camera enabled");
                fixes++;
            }
            else
            {
                Debug.Log("✓ Player camera is enabled");
            }

            // Check camera depth (should be higher than fallback cameras)
            if (playerCam.depth < 0)
            {
                playerCam.depth = 0;
                UnityEditor.EditorUtility.SetDirty(playerCam.gameObject);
                Debug.Log("✅ Player camera depth set to 0 (override fallback cameras)");
                fixes++;
            }
            else
            {
                Debug.Log($"✓ Player camera depth is {playerCam.depth}");
            }

            return fixes;
        }

        [MenuItem("Tools/TacticalCombat/Quick Diagnostic 🔍")]
        public static void QuickDiagnostic()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 QUICK DIAGNOSTIC");
            Debug.Log("═══════════════════════════════════════\n");

            // 1. Scene in Build Settings?
            string scenePath = EditorSceneManager.GetActiveScene().path;
            bool inBuild = EditorBuildSettings.scenes.Any(s => s.path == scenePath && s.enabled);
            Debug.Log($"Scene in Build Settings: {(inBuild ? "✅ YES" : "❌ NO (BLACK SCREEN CAUSE!)")}");

            // 2. Run in Background?
            Debug.Log($"Run in Background: {(Application.runInBackground ? "✅ YES" : "⚠️ NO")}");

            // 3. NetworkManager?
            var netMgr = FindFirstObjectByType<Mirror.NetworkManager>();
            Debug.Log($"NetworkManager: {(netMgr != null ? "✅ YES" : "❌ NO")}");
            if (netMgr != null)
            {
                Debug.Log($"  Transport: {(netMgr.transport != null ? "✅ YES" : "❌ NO")}");
                Debug.Log($"  Player Prefab: {(netMgr.playerPrefab != null ? "✅ YES" : "❌ NO")}");
            }

            // 4. Player Camera?
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                Camera cam = playerPrefab.GetComponentInChildren<Camera>();
                Debug.Log($"Player Camera: {(cam != null ? "✅ YES" : "❌ NO (BLACK SCREEN CAUSE!)")}");
                if (cam != null)
                {
                    Debug.Log($"  Enabled: {(cam.enabled ? "✅ YES" : "❌ NO (BLACK SCREEN CAUSE!)")}");
                    Debug.Log($"  Depth: {cam.depth}");
                }
            }
            else
            {
                Debug.Log("Player Prefab: ❌ NOT FOUND");
            }

            Debug.Log("\n═══════════════════════════════════════");

            if (!inBuild)
            {
                Debug.LogError("⚠️ MAIN ISSUE FOUND: Scene not in Build Settings!");
                Debug.LogError("   Run: Tools → TacticalCombat → Fix Build Black Screen");
            }
        }
    }
}
