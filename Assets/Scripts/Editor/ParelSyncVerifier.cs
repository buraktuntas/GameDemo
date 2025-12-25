using UnityEngine;
using UnityEditor;
using Mirror;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ PARELSYNC FIX: Verifies scene consistency between original and clone projects
    /// Helps identify desync issues caused by different spawn points or scene settings
    /// </summary>
    public class ParelSyncVerifier : EditorWindow
    {
        [MenuItem("Tools/ParelSync/Verify Scene Consistency")]
        public static void ShowWindow()
        {
            GetWindow<ParelSyncVerifier>("ParelSync Verifier");
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            GUILayout.Label("ParelSync Scene Consistency Checker", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool checks for common ParelSync issues that cause network desync:\n" +
                "- Different spawn point positions\n" +
                "- Mismatched NetworkTransform settings\n" +
                "- Scene asset reference differences",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Run Verification", GUILayout.Height(30)))
            {
                RunVerification();
            }

            GUILayout.Space(10);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            // Results will be logged to console
            EditorGUILayout.EndScrollView();
        }

        private void RunVerification()
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("🔍 PARELSYNC VERIFICATION START");
            Debug.Log("═══════════════════════════════════════════════════");

            int issuesFound = 0;

            // 1. Check NetworkStartPosition consistency
            issuesFound += CheckSpawnPoints();

            // 2. Check Player prefab NetworkTransform
            issuesFound += CheckPlayerPrefab();

            // 3. Check NetworkManager settings
            issuesFound += CheckNetworkManager();

            // 4. Check for duplicate NetworkIdentity
            issuesFound += CheckDuplicateNetworkIdentities();

            Debug.Log("═══════════════════════════════════════════════════");
            if (issuesFound == 0)
            {
                Debug.Log("✅ VERIFICATION PASSED - No issues found!");
            }
            else
            {
                Debug.LogWarning($"⚠️ VERIFICATION FOUND {issuesFound} ISSUE(S)");
            }
            Debug.Log("═══════════════════════════════════════════════════");
        }

        private int CheckSpawnPoints()
        {
            Debug.Log("\n📍 Checking NetworkStartPosition objects...");

            var spawnPoints = FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);

            if (spawnPoints.Length == 0)
            {
                Debug.LogError("❌ NO SPAWN POINTS FOUND! This will cause spawn issues.");
                return 1;
            }

            Debug.Log($"✅ Found {spawnPoints.Length} spawn points");

            // List all spawn positions (for comparison between projects)
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                Debug.Log($"  Spawn {i}: {sp.name} at {sp.transform.position} (rot: {sp.transform.rotation.eulerAngles})");
            }

            // Check for overlapping spawns
            int overlaps = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                for (int j = i + 1; j < spawnPoints.Length; j++)
                {
                    float distance = Vector3.Distance(
                        spawnPoints[i].transform.position,
                        spawnPoints[j].transform.position
                    );

                    if (distance < 0.1f)
                    {
                        Debug.LogWarning($"⚠️ Spawn points {i} and {j} are overlapping ({distance:F3}m apart)");
                        overlaps++;
                    }
                }
            }

            return overlaps;
        }

        private int CheckPlayerPrefab()
        {
            Debug.Log("\n🎮 Checking Player prefab...");

            // Find player prefab in Resources or Prefabs folder
            string[] guids = AssetDatabase.FindAssets("t:GameObject Player", new[] { "Assets/Prefabs", "Assets/Resources" });

            if (guids.Length == 0)
            {
                Debug.LogWarning("⚠️ Player prefab not found in Prefabs or Resources folder");
                return 1;
            }

            int issues = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                Debug.Log($"  Checking: {path}");

                // Check for NetworkTransform
                var netTransform = prefab.GetComponent<NetworkTransformReliable>();
                if (netTransform != null)
                {
                    if (netTransform.enabled)
                    {
                        Debug.LogWarning($"⚠️ NetworkTransformReliable is ENABLED on {prefab.name}");
                        Debug.LogWarning($"   This will conflict with FPSController manual sync!");
                        Debug.LogWarning($"   FPS Controller should disable it in Awake()");
                        issues++;
                    }
                    else
                    {
                        Debug.Log($"  ✅ NetworkTransformReliable is disabled (correct)");
                    }
                }

                // Check for FPSController
                var fpsController = prefab.GetComponent<TacticalCombat.Player.FPSController>();
                if (fpsController == null)
                {
                    Debug.LogError($"❌ FPSController missing on {prefab.name}!");
                    issues++;
                }
                else
                {
                    Debug.Log($"  ✅ FPSController present");
                }

                // Check for NetworkIdentity
                var netId = prefab.GetComponent<NetworkIdentity>();
                if (netId == null)
                {
                    Debug.LogError($"❌ NetworkIdentity missing on {prefab.name}!");
                    issues++;
                }
                else
                {
                    Debug.Log($"  ✅ NetworkIdentity present");
                }
            }

            return issues;
        }

        private int CheckNetworkManager()
        {
            Debug.Log("\n🌐 Checking NetworkManager...");

            var networkManager = FindFirstObjectByType<NetworkManager>();

            if (networkManager == null)
            {
                Debug.LogError("❌ NetworkManager not found in scene!");
                return 1;
            }

            Debug.Log($"  ✅ NetworkManager found: {networkManager.name}");

            // Check player prefab reference
            if (networkManager.playerPrefab == null)
            {
                Debug.LogError("❌ NetworkManager has no player prefab assigned!");
                return 1;
            }

            Debug.Log($"  ✅ Player prefab: {networkManager.playerPrefab.name}");

            // Check spawn method
            var spawnMethod = networkManager.playerSpawnMethod;
            Debug.Log($"  Spawn method: {spawnMethod}");

            if (spawnMethod == PlayerSpawnMethod.RoundRobin)
            {
                Debug.Log($"  ✅ Using RoundRobin spawn (recommended)");
            }

            return 0;
        }

        private int CheckDuplicateNetworkIdentities()
        {
            Debug.Log("\n🔢 Checking for duplicate NetworkIdentity SceneIDs...");

            var networkIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
            var sceneIds = networkIdentities
                .Where(ni => ni.sceneId != 0)
                .GroupBy(ni => ni.sceneId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (sceneIds.Count > 0)
            {
                Debug.LogError($"❌ Found {sceneIds.Count} duplicate NetworkIdentity SceneIDs!");
                foreach (var group in sceneIds)
                {
                    Debug.LogError($"  SceneID {group.Key} used by:");
                    foreach (var ni in group)
                    {
                        Debug.LogError($"    - {ni.name}", ni.gameObject);
                    }
                }
                return sceneIds.Count;
            }

            Debug.Log($"  ✅ No duplicate SceneIDs found ({networkIdentities.Length} NetworkIdentities checked)");
            return 0;
        }
    }
}
