using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Player Health Check - Final Validation
    /// Detects all potential issues in player prefab
    ///
    /// Usage: Tools → Tactical Combat → Player Health Check
    /// </summary>
    public class PlayerHealthCheck : EditorWindow
    {
        private GameObject playerPrefab;
        private Vector2 scrollPos;

        [MenuItem("Tools/Tactical Combat/Player Health Check")]
        static void ShowWindow()
        {
            PlayerHealthCheck window = GetWindow<PlayerHealthCheck>("Player Health Check");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Health Check - Final Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Comprehensive player prefab validation - checks all common multiplayer issues", MessageType.Info);

            EditorGUILayout.Space(10);
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), true);

            EditorGUILayout.Space(10);

            GUI.enabled = playerPrefab != null;
            if (GUILayout.Button("🏥 Run Health Check", GUILayout.Height(40)))
            {
                RunHealthCheck(playerPrefab);
            }
            GUI.enabled = true;
        }

        static void RunHealthCheck(GameObject player)
        {
            if (player == null)
            {
                Debug.LogError("Player is null!");
                return;
            }

            Debug.Log("=================================================================");
            Debug.Log("🏥 PLAYER HEALTH CHECK - COMPREHENSIVE VALIDATION");
            Debug.Log("=================================================================");
            Debug.Log($"Player: {player.name}");
            Debug.Log("");

            int criticalIssues = 0;
            int warnings = 0;
            int infos = 0;

            // Category 1: Network Setup
            Debug.Log("--- 1. NETWORK SETUP ---");
            criticalIssues += CheckNetworkSetup(player, ref warnings);

            // Category 2: Movement System
            Debug.Log("\n--- 2. MOVEMENT SYSTEM ---");
            criticalIssues += CheckMovementSystem(player, ref warnings);

            // Category 3: Input System
            Debug.Log("\n--- 3. INPUT SYSTEM ---");
            criticalIssues += CheckInputSystem(player, ref warnings);

            // Category 4: Combat System
            Debug.Log("\n--- 4. COMBAT SYSTEM ---");
            criticalIssues += CheckCombatSystem(player, ref warnings);

            // Category 5: Audio System
            Debug.Log("\n--- 5. AUDIO SYSTEM ---");
            warnings += CheckAudioSystem(player);

            // Category 6: Performance Issues
            Debug.Log("\n--- 6. PERFORMANCE CHECKS ---");
            warnings += CheckPerformance(player);

            // Category 7: Multiplayer Best Practices
            Debug.Log("\n--- 7. MULTIPLAYER BEST PRACTICES ---");
            infos += CheckMultiplayerBestPractices(player);

            // Summary
            Debug.Log("\n=================================================================");
            Debug.Log("📊 HEALTH CHECK SUMMARY");
            Debug.Log("=================================================================");
            Debug.Log($"❌ Critical Issues: {criticalIssues}");
            Debug.Log($"⚠️  Warnings: {warnings}");
            Debug.Log($"ℹ️  Info: {infos}");

            if (criticalIssues == 0 && warnings == 0)
            {
                Debug.Log("\n✅✅✅ PLAYER PREFAB IS PERFECT! ✅✅✅");
                Debug.Log("No critical issues or warnings found!");
            }
            else if (criticalIssues == 0)
            {
                Debug.Log("\n✅ PLAYER PREFAB IS GOOD!");
                Debug.Log($"No critical issues, but {warnings} warning(s) to review.");
            }
            else
            {
                Debug.LogError($"\n❌ PLAYER PREFAB HAS ISSUES!");
                Debug.LogError($"Found {criticalIssues} critical issue(s) that must be fixed!");
            }

            Debug.Log("=================================================================\n");

            // Show dialog
            string message = $"Health Check Complete!\n\n" +
                           $"❌ Critical Issues: {criticalIssues}\n" +
                           $"⚠️  Warnings: {warnings}\n" +
                           $"ℹ️  Info: {infos}\n\n" +
                           $"Check Console for details.";

            EditorUtility.DisplayDialog("Health Check Complete", message, "OK");
        }

        static int CheckNetworkSetup(GameObject player, ref int warnings)
        {
            int issues = 0;

            // NetworkIdentity required
            NetworkIdentity netId = player.GetComponent<NetworkIdentity>();
            if (netId == null)
            {
                Debug.LogError("❌ CRITICAL: NetworkIdentity missing!");
                issues++;
            }
            else
            {
                Debug.Log("✅ NetworkIdentity found");
            }

            // NetworkTransform recommended (check by component name to avoid namespace issues)
            Component networkTransform = player.GetComponent("NetworkTransformReliable") ?? player.GetComponent("NetworkTransform");
            if (networkTransform == null)
            {
                Debug.LogWarning("⚠️  NetworkTransform missing (recommended for smooth movement)");
                warnings++;
            }
            else
            {
                Debug.Log($"✅ NetworkTransform found ({networkTransform.GetType().Name})");
            }

            // Check for NetworkBehaviour components
            NetworkBehaviour[] netBehaviours = player.GetComponents<NetworkBehaviour>();
            Debug.Log($"ℹ️  Found {netBehaviours.Length} NetworkBehaviour component(s)");

            return issues;
        }

        static int CheckMovementSystem(GameObject player, ref int warnings)
        {
            int issues = 0;

            // Need either CharacterController or Rigidbody
            bool hasCharController = player.GetComponent<CharacterController>() != null;
            bool hasRigidbody = player.GetComponent<Rigidbody>() != null;

            if (!hasCharController && !hasRigidbody)
            {
                Debug.LogError("❌ CRITICAL: No movement physics component (CharacterController or Rigidbody)");
                issues++;
            }
            else
            {
                if (hasCharController)
                {
                    Debug.Log("✅ CharacterController found");
                }
                if (hasRigidbody)
                {
                    Debug.Log("✅ Rigidbody found");
                }
            }

            // Check for movement script
            Component fpsController = player.GetComponent("FPSController");
            Component rigidBodyMovement = player.GetComponent("RigidbodyPlayerMovement");

            if (fpsController == null && rigidBodyMovement == null)
            {
                Debug.LogError("❌ CRITICAL: No movement controller script found");
                issues++;
            }
            else if (fpsController != null && rigidBodyMovement != null)
            {
                Debug.LogError("❌ CRITICAL: Multiple movement systems detected - conflicts!");
                issues++;
            }
            else
            {
                Debug.Log("✅ Movement controller found");
            }

            return issues;
        }

        static int CheckInputSystem(GameObject player, ref int warnings)
        {
            int issues = 0;

            // InputManager required
            Component inputManager = player.GetComponent("InputManager");
            if (inputManager == null)
            {
                Debug.LogError("❌ CRITICAL: InputManager missing");
                issues++;
            }
            else
            {
                Debug.Log("✅ InputManager found");
            }

            // PlayerInput optional
            Component playerInput = player.GetComponent("PlayerInput");
            if (playerInput != null)
            {
                Debug.Log("ℹ️  PlayerInput found (optional)");
            }

            return issues;
        }

        static int CheckCombatSystem(GameObject player, ref int warnings)
        {
            int issues = 0;

            // Health component
            Component health = player.GetComponent("Health");
            if (health == null)
            {
                Debug.LogWarning("⚠️  Health component missing (recommended)");
                warnings++;
            }
            else
            {
                Debug.Log("✅ Health component found");
            }

            // Weapon system
            Component weaponSystem = player.GetComponent("WeaponSystem");
            if (weaponSystem == null)
            {
                Debug.Log("ℹ️  WeaponSystem not found (optional for non-combat players)");
            }
            else
            {
                Debug.Log("✅ WeaponSystem found");
            }

            return issues;
        }

        static int CheckAudioSystem(GameObject player)
        {
            int warnings = 0;

            AudioSource[] sources = player.GetComponents<AudioSource>();

            if (sources.Length == 0)
            {
                Debug.LogWarning("⚠️  No AudioSource found (sounds won't work)");
                warnings++;
            }
            else if (sources.Length == 1)
            {
                Debug.LogWarning("⚠️  Only 1 AudioSource - recommend 3 (footstep, weapon, voice)");
                Debug.Log("   Use 'Clean Up Player Components' tool to fix");
                warnings++;
            }
            else if (sources.Length >= 3)
            {
                Debug.Log($"✅ Found {sources.Length} AudioSources (good for multiple sounds)");
            }
            else
            {
                Debug.Log($"ℹ️  Found {sources.Length} AudioSources");
            }

            return warnings;
        }

        static int CheckPerformance(GameObject player)
        {
            int warnings = 0;

            // Component count check
            Component[] allComponents = player.GetComponents<Component>();
            int componentCount = allComponents.Length;

            if (componentCount > 20)
            {
                Debug.LogWarning($"⚠️  High component count ({componentCount}) - consider refactoring");
                warnings++;
            }
            else if (componentCount > 15)
            {
                Debug.Log($"ℹ️  Component count: {componentCount} (acceptable but could be optimized)");
            }
            else
            {
                Debug.Log($"✅ Component count: {componentCount} (good)");
            }

            // Check for OnGUI usage
            MonoBehaviour[] monoBehaviours = player.GetComponents<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
            {
                if (mb == null) continue;
                var type = mb.GetType();
                var method = type.GetMethod("OnGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (method != null && method.DeclaringType == type)
                {
                    Debug.LogWarning($"⚠️  {type.Name} uses OnGUI (old IMGUI - performance hit)");
                    warnings++;
                }
            }

            return warnings;
        }

        static int CheckMultiplayerBestPractices(GameObject player)
        {
            int infos = 0;

            Debug.Log("ℹ️  Multiplayer Best Practices:");

            // Check for PlayerComponents hub
            Component playerComponents = player.GetComponent("PlayerComponents");
            if (playerComponents != null)
            {
                Debug.Log("  ✅ PlayerComponents hub found (good practice)");
            }
            else
            {
                Debug.Log("  ℹ️  PlayerComponents not found - consider adding for easier component access");
                infos++;
            }

            // Check for Camera
            Camera[] cameras = player.GetComponentsInChildren<Camera>();
            if (cameras.Length == 0)
            {
                Debug.Log("  ℹ️  No camera found - should be added at runtime for local player");
            }
            else if (cameras.Length == 1)
            {
                Debug.Log("  ✅ 1 camera found (should be disabled for remote players)");
            }
            else
            {
                Debug.LogWarning($"  ⚠️  {cameras.Length} cameras found - usually only need 1");
            }

            // Check for AudioListener
            AudioListener[] listeners = player.GetComponentsInChildren<AudioListener>();
            if (listeners.Length > 1)
            {
                Debug.LogWarning($"  ⚠️  {listeners.Length} AudioListeners - only 1 should be active!");
            }

            return infos;
        }
    }
}
