using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Comprehensive system scanner for Camera, Player, Combat, and Networking
    /// Detects critical errors, performance issues, and build problems
    /// </summary>
    public class SystemScanner : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/System Scanner & Fixer")]
        public static void ShowWindow()
        {
            GetWindow<SystemScanner>("System Scanner");
        }

        private Vector2 scrollPos;
        private List<Issue> issues = new List<Issue>();
        private bool hasScanned = false;

        private void OnGUI()
        {
            GUILayout.Label("System Scanner & Fixer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans Camera, Player, Combat, and Networking systems.\n" +
                "Detects critical errors, performance issues, and Build problems.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("🔍 SCAN ALL SYSTEMS", GUILayout.Height(40)))
            {
                ScanAllSystems();
            }

            if (hasScanned && issues.Count > 0)
            {
                GUILayout.Space(10);

                if (GUILayout.Button("✅ FIX ALL CRITICAL ISSUES", GUILayout.Height(35)))
                {
                    FixAllCriticalIssues();
                }
            }

            if (hasScanned)
            {
                GUILayout.Space(10);
                DisplayResults();
            }
        }

        private void ScanAllSystems()
        {
            issues.Clear();
            hasScanned = true;

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 SYSTEM SCAN STARTED");
            Debug.Log("═══════════════════════════════════════");

            ScanCameraSystem();
            ScanPlayerSystem();
            ScanCombatSystem();
            ScanNetworkingSystem();
            ScanBuildSettings();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"✅ SCAN COMPLETE - Found {issues.Count} issues");
            Debug.Log("═══════════════════════════════════════");

            Repaint();
        }

        // ═══════════════════════════════════════════════════════════
        // CAMERA SYSTEM
        // ═══════════════════════════════════════════════════════════

        private void ScanCameraSystem()
        {
            Debug.Log("\n📷 Scanning Camera System...");

            // 1. Check for Camera.main usage (build performance issue)
            CheckCameraMainUsage();

            // 2. Check for multiple AudioListeners
            CheckAudioListeners();

            // 3. Check camera setup in Player prefab
            CheckPlayerPrefabCamera();

            // 4. Check for disabled cameras in scene
            CheckSceneCameras();
        }

        private void CheckCameraMainUsage()
        {
            string[] scripts = new string[]
            {
                "Assets/Scripts/Combat/WeaponSystem.cs",
                "Assets/Scripts/Player/FPSController.cs",
                "Assets/Scripts/Building/SimpleBuildMode.cs",
                "Assets/Scripts/Combat/ADSSystem.cs"
            };

            foreach (var scriptPath in scripts)
            {
                if (System.IO.File.Exists(scriptPath))
                {
                    string content = System.IO.File.ReadAllText(scriptPath);
                    if (content.Contains("Camera.main"))
                    {
                        issues.Add(new Issue(
                            IssueSeverity.Performance,
                            "Camera System",
                            $"Camera.main usage in {System.IO.Path.GetFileName(scriptPath)}",
                            "Camera.main is slow and called every frame. Cache the reference in OnStartLocalPlayer.",
                            () => { Debug.Log($"⚠️ Manual fix needed: Cache camera in {scriptPath}"); }
                        ));
                    }
                }
            }
        }

        private void CheckAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length > 1)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Camera System",
                    $"Multiple AudioListeners ({listeners.Length})",
                    "Only 1 AudioListener allowed. Causes audio issues.",
                    () => FixDuplicateAudioListeners()
                ));
            }
            else if (listeners.Length == 0)
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Camera System",
                    "No AudioListener in scene",
                    "Audio won't play without an AudioListener.",
                    () => { Debug.Log("Add AudioListener to MainCamera manually"); }
                ));
            }
        }

        private void CheckPlayerPrefabCamera()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Camera System",
                    "Player prefab not found",
                    "Player.prefab must exist at Assets/Prefabs/Player.prefab",
                    null
                ));
                return;
            }

            Camera playerCam = playerPrefab.GetComponentInChildren<Camera>();
            if (playerCam == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Camera System",
                    "No camera in Player prefab",
                    "Player prefab must have a Camera child. This causes black screen in builds!",
                    () => { Debug.LogError("Run Ultimate Setup → BAŞLAT to fix"); }
                ));
            }
            else
            {
                // Check if camera is enabled
                if (!playerCam.enabled)
                {
                    issues.Add(new Issue(
                        IssueSeverity.Critical,
                        "Camera System",
                        "Player camera is disabled in prefab",
                        "Camera must be enabled. Causes black screen!",
                        () => EnablePlayerCamera(playerCam)
                    ));
                }
            }
        }

        private void CheckSceneCameras()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            if (cameras.Length == 0)
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Camera System",
                    "No cameras in scene",
                    "Scene needs at least 1 active camera for testing",
                    null
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PLAYER SYSTEM
        // ═══════════════════════════════════════════════════════════

        private void ScanPlayerSystem()
        {
            Debug.Log("\n🎮 Scanning Player System...");

            // 1. Check Player prefab exists
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Player System",
                    "Player prefab missing",
                    "Player.prefab not found at Assets/Prefabs/Player.prefab",
                    null
                ));
                return;
            }

            // 2. Check required components
            CheckPlayerComponents(playerPrefab);

            // 3. Check NetworkIdentity
            if (!playerPrefab.GetComponent<NetworkIdentity>())
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Player System",
                    "Player prefab missing NetworkIdentity",
                    "NetworkBehaviours require NetworkIdentity",
                    null
                ));
            }
        }

        private void CheckPlayerComponents(GameObject playerPrefab)
        {
            // Required components
            var requiredComponents = new Dictionary<System.Type, string>
            {
                { typeof(CharacterController), "CharacterController" },
                { typeof(TacticalCombat.Player.FPSController), "FPSController" },
                { typeof(TacticalCombat.Combat.WeaponSystem), "WeaponSystem" },
                { typeof(TacticalCombat.Combat.Health), "Health" }
            };

            foreach (var kvp in requiredComponents)
            {
                if (playerPrefab.GetComponent(kvp.Key) == null)
                {
                    issues.Add(new Issue(
                        IssueSeverity.Critical,
                        "Player System",
                        $"Missing {kvp.Value}",
                        $"Player prefab must have {kvp.Value} component",
                        null
                    ));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // COMBAT SYSTEM
        // ═══════════════════════════════════════════════════════════

        private void ScanCombatSystem()
        {
            Debug.Log("\n⚔️ Scanning Combat System...");

            // Check Player prefab weapon setup
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                var weaponSystem = playerPrefab.GetComponent<TacticalCombat.Combat.WeaponSystem>();
                if (weaponSystem != null)
                {
                    // Check serialized fields using reflection
                    CheckWeaponSystemReferences(weaponSystem);
                }
            }
        }

        private void CheckWeaponSystemReferences(TacticalCombat.Combat.WeaponSystem weaponSystem)
        {
            SerializedObject so = new SerializedObject(weaponSystem);

            // Check critical references
            var playerCamera = so.FindProperty("playerCamera");
            if (playerCamera.objectReferenceValue == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Combat System",
                    "WeaponSystem: playerCamera not assigned",
                    "Will use Camera.main fallback (slower)",
                    null
                ));
            }

            // Check audio clips
            var fireSounds = so.FindProperty("fireSounds");
            if (fireSounds.arraySize == 0)
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Combat System",
                    "WeaponSystem: No fire sounds assigned",
                    "Weapon will be silent",
                    null
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════
        // NETWORKING SYSTEM
        // ═══════════════════════════════════════════════════════════

        private void ScanNetworkingSystem()
        {
            Debug.Log("\n🌐 Scanning Networking System...");

            // 1. Check NetworkManager exists
            var netMgr = FindFirstObjectByType<NetworkManager>();
            if (netMgr == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Networking",
                    "No NetworkManager in scene",
                    "NetworkManager required for multiplayer",
                    null
                ));
                return;
            }

            // 2. Check Transport assigned
            if (netMgr.transport == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Networking",
                    "No Transport assigned to NetworkManager",
                    "Transport required for network communication",
                    () => FixNetworkTransport(netMgr)
                ));
            }

            // 3. Check Player Prefab assigned
            if (netMgr.playerPrefab == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Networking",
                    "No Player Prefab assigned to NetworkManager",
                    "Player won't spawn without prefab",
                    () => FixPlayerPrefabAssignment(netMgr)
                ));
            }

            // 4. Check MatchManager
            var matchMgr = FindFirstObjectByType<TacticalCombat.Core.MatchManager>();
            if (matchMgr == null)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Networking",
                    "No MatchManager in scene",
                    "MatchManager required for game flow",
                    null
                ));
            }
            else
            {
                // Check NetworkIdentity
                if (matchMgr.GetComponent<NetworkIdentity>() == null)
                {
                    issues.Add(new Issue(
                        IssueSeverity.Critical,
                        "Networking",
                        "MatchManager missing NetworkIdentity",
                        "NetworkBehaviour requires NetworkIdentity",
                        () => FixMatchManagerNetworkIdentity(matchMgr)
                    ));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // BUILD SETTINGS
        // ═══════════════════════════════════════════════════════════

        private void ScanBuildSettings()
        {
            Debug.Log("\n🔧 Scanning Build Settings...");

            // 1. Check if scene is in build settings
            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            if (string.IsNullOrEmpty(currentScenePath))
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Build Settings",
                    "Scene not saved",
                    "Save scene before building",
                    null
                ));
                return;
            }

            var scenesInBuild = EditorBuildSettings.scenes;
            bool sceneInBuild = scenesInBuild.Any(s => s.path == currentScenePath && s.enabled);

            if (!sceneInBuild)
            {
                issues.Add(new Issue(
                    IssueSeverity.Critical,
                    "Build Settings",
                    "Current scene not in Build Settings",
                    "Scene won't be included in build. THIS IS WHY BUILD SHOWS BLACK SCREEN!",
                    () => AddSceneToBuildSettings(currentScenePath)
                ));
            }

            // 2. Check Run in Background
            if (!Application.runInBackground)
            {
                issues.Add(new Issue(
                    IssueSeverity.Warning,
                    "Build Settings",
                    "Run in Background is disabled",
                    "Multiplayer games should run in background for testing",
                    () => { PlayerSettings.runInBackground = true; Debug.Log("✅ Run in Background enabled"); }
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════
        // FIXES
        // ═══════════════════════════════════════════════════════════

        private void FixDuplicateAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length <= 1) return;

            AudioListener keep = null;
            foreach (var listener in listeners)
            {
                if (listener.CompareTag("MainCamera"))
                {
                    keep = listener;
                    break;
                }
            }
            if (keep == null) keep = listeners[0];

            foreach (var listener in listeners)
            {
                if (listener != keep)
                {
                    DestroyImmediate(listener);
                }
            }

            Debug.Log("✅ Duplicate AudioListeners removed");
        }

        private void EnablePlayerCamera(Camera cam)
        {
            cam.enabled = true;
            EditorUtility.SetDirty(cam.gameObject);
            Debug.Log("✅ Player camera enabled");
        }

        private void FixNetworkTransport(NetworkManager netMgr)
        {
            var transport = netMgr.GetComponent<Mirror.Transport>();
            if (transport == null)
            {
                transport = netMgr.gameObject.AddComponent<kcp2k.KcpTransport>();
            }
            netMgr.transport = transport;
            EditorUtility.SetDirty(netMgr);
            Debug.Log("✅ NetworkManager Transport fixed");
        }

        private void FixPlayerPrefabAssignment(NetworkManager netMgr)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                netMgr.playerPrefab = playerPrefab;
                EditorUtility.SetDirty(netMgr);
                Debug.Log("✅ Player Prefab assigned to NetworkManager");
            }
        }

        private void FixMatchManagerNetworkIdentity(TacticalCombat.Core.MatchManager matchMgr)
        {
            if (matchMgr.GetComponent<NetworkIdentity>() == null)
            {
                matchMgr.gameObject.AddComponent<NetworkIdentity>();
                EditorUtility.SetDirty(matchMgr.gameObject);
                Debug.Log("✅ NetworkIdentity added to MatchManager");
            }
        }

        private void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            // Remove existing entry if disabled
            scenes.RemoveAll(s => s.path == scenePath);

            // Add at index 0 (first scene)
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"✅ Scene added to Build Settings: {scenePath}");
        }

        private void FixAllCriticalIssues()
        {
            int fixedCount = 0;

            foreach (var issue in issues)
            {
                if (issue.severity == IssueSeverity.Critical && issue.fix != null)
                {
                    issue.fix.Invoke();
                    fixedCount++;
                }
            }

            EditorUtility.DisplayDialog(
                "Fixes Applied",
                $"Applied {fixedCount} critical fixes.\n\nRe-scan to verify.",
                "OK"
            );

            ScanAllSystems();
        }

        // ═══════════════════════════════════════════════════════════
        // UI
        // ═══════════════════════════════════════════════════════════

        private void DisplayResults()
        {
            GUILayout.Space(10);
            GUILayout.Label($"Issues Found: {issues.Count}", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var grouped = issues.GroupBy(i => i.category);

            foreach (var group in grouped)
            {
                GUILayout.Space(10);
                GUILayout.Label($"═══ {group.Key} ═══", EditorStyles.boldLabel);

                foreach (var issue in group)
                {
                    DrawIssue(issue);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(Issue issue)
        {
            Color bgColor = issue.severity == IssueSeverity.Critical ? new Color(1, 0.3f, 0.3f, 0.3f) :
                           issue.severity == IssueSeverity.Warning ? new Color(1, 0.8f, 0, 0.3f) :
                           new Color(0.5f, 0.5f, 0.5f, 0.3f);

            GUI.backgroundColor = bgColor;
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUILayout.Label($"{GetSeverityIcon(issue.severity)} {issue.title}", EditorStyles.boldLabel);
            GUILayout.Label(issue.description, EditorStyles.wordWrappedLabel);

            if (issue.fix != null)
            {
                if (GUILayout.Button("Fix", GUILayout.Width(100)))
                {
                    issue.fix.Invoke();
                    ScanAllSystems();
                }
            }

            GUILayout.EndVertical();
        }

        private string GetSeverityIcon(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Critical: return "❌";
                case IssueSeverity.Warning: return "⚠️";
                case IssueSeverity.Performance: return "🐌";
                default: return "ℹ️";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DATA CLASSES
        // ═══════════════════════════════════════════════════════════

        private class Issue
        {
            public IssueSeverity severity;
            public string category;
            public string title;
            public string description;
            public System.Action fix;

            public Issue(IssueSeverity severity, string category, string title, string description, System.Action fix)
            {
                this.severity = severity;
                this.category = category;
                this.title = title;
                this.description = description;
                this.fix = fix;
            }
        }

        private enum IssueSeverity
        {
            Critical,
            Warning,
            Performance,
            Info
        }
    }
}
