using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Player Component Cleanup Tool
    /// Removes redundant components, fixes common issues
    ///
    /// Usage: Select player → Right-click → "Clean Up Player Components"
    /// </summary>
    public class PlayerComponentCleanup : EditorWindow
    {
        private GameObject playerPrefab;
        private bool removeCharacterSelector = true;
        private bool fixAudioSources = true;
        private bool validateCrosshair = true;
        

        [MenuItem("Tools/Tactical Combat/Clean Up Player Components")]
        static void ShowWindow()
        {
            PlayerComponentCleanup window = GetWindow<PlayerComponentCleanup>("Component Cleanup");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        [MenuItem("GameObject/Tactical Combat/Clean Up Player Components", false, 1)]
        static void CleanupContextMenu()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                CleanupPlayer(selected, true, true, true);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Component Cleanup Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool analyzes and cleans up redundant/problematic components:\n\n" +
                "• Removes CharacterSelector (should be on SpawnManager)\n" +
                "• Fixes AudioSource setup (need 3 for proper sound)\n" +
                "• Validates SimpleCrosshair (local player only)\n" +
                "• Reports any issues found",
                MessageType.Info);

            EditorGUILayout.Space(10);

            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Cleanup Options:", EditorStyles.boldLabel);

            removeCharacterSelector = EditorGUILayout.Toggle("Remove Character Selector", removeCharacterSelector);
            EditorGUILayout.HelpBox("CharacterSelector should be on NetworkManager, not on each player instance.", MessageType.Info);

            fixAudioSources = EditorGUILayout.Toggle("Fix Audio Sources", fixAudioSources);
            EditorGUILayout.HelpBox("Need 3 AudioSources: Footstep, Weapon, Voice (damage sounds).", MessageType.Info);

            validateCrosshair = EditorGUILayout.Toggle("Validate Crosshair", validateCrosshair);
            EditorGUILayout.HelpBox("SimpleCrosshair should only draw for local player.", MessageType.Info);

            EditorGUILayout.Space(10);

            GUI.enabled = playerPrefab != null;
            if (GUILayout.Button("🧹 Clean Up Player", GUILayout.Height(40)))
            {
                CleanupPlayer(playerPrefab, removeCharacterSelector, fixAudioSources, validateCrosshair);
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("📊 Analyze Only (No Changes)", GUILayout.Height(30)))
            {
                if (playerPrefab != null)
                {
                    AnalyzePlayer(playerPrefab);
                }
            }
        }

        static void CleanupPlayer(GameObject player, bool removeCharSel, bool fixAudio, bool validateCross)
        {
            if (player == null)
            {
                Debug.LogError("Player is null!");
                return;
            }

            Undo.SetCurrentGroupName("Cleanup Player Components");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("=== PLAYER CLEANUP STARTED ===");

            int issuesFixed = 0;

            // Step 1: Remove Character Selector
            if (removeCharSel)
            {
                CharacterSelector charSel = player.GetComponent<CharacterSelector>();
                if (charSel != null)
                {
                    Debug.Log("❌ Removing CharacterSelector (should be on SpawnManager, not player instance)");
                    Undo.DestroyObjectImmediate(charSel);
                    issuesFixed++;
                }
                else
                {
                    Debug.Log("✅ CharacterSelector not found (good)");
                }
            }

            // Step 2: Fix Audio Sources
            if (fixAudio)
            {
                issuesFixed += FixAudioSources(player);
            }

            // Step 3: Validate Crosshair
            if (validateCross)
            {
                ValidateCrosshair(player);
            }

            // Step 4: General analysis
            AnalyzePlayer(player);

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(player);

            Debug.Log($"=== CLEANUP COMPLETE: {issuesFixed} issues fixed ===");

            if (issuesFixed > 0)
            {
                EditorUtility.DisplayDialog("Cleanup Complete!",
                    $"Player cleanup complete!\n\n✅ Fixed {issuesFixed} issue(s)\n\nCheck Console for details.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Already Clean!",
                    "Player prefab is already clean - no issues found!\n\n✅ All good!",
                    "OK");
            }
        }

        static int FixAudioSources(GameObject player)
        {
            Debug.Log("--- Checking Audio Sources ---");

            AudioSource[] sources = player.GetComponents<AudioSource>();

            if (sources.Length >= 3)
            {
                Debug.Log($"✅ Found {sources.Length} AudioSources (good for multiple sounds)");
                return 0;
            }
            else if (sources.Length == 1)
            {
                Debug.LogWarning($"⚠️ Only 1 AudioSource found - need 3 for proper sound");
                Debug.Log("🔧 Adding 2 more AudioSources...");

                // Add footstep source
                AudioSource footstep = Undo.AddComponent<AudioSource>(player);
                footstep.playOnAwake = false;
                footstep.spatialBlend = 1f; // 3D sound
                footstep.minDistance = 1f;
                footstep.maxDistance = 15f;

                // Add voice source (damage sounds)
                AudioSource voice = Undo.AddComponent<AudioSource>(player);
                voice.playOnAwake = false;
                voice.spatialBlend = 1f;
                voice.minDistance = 1f;
                voice.maxDistance = 20f;
                voice.priority = 128; // Higher priority

                Debug.Log("✅ Added FootstepAudio and VoiceAudio sources");
                return 1;
            }
            else if (sources.Length == 2)
            {
                Debug.LogWarning("⚠️ Found 2 AudioSources - adding 1 more");
                AudioSource voice = Undo.AddComponent<AudioSource>(player);
                voice.playOnAwake = false;
                voice.spatialBlend = 1f;
                voice.priority = 128;
                Debug.Log("✅ Added VoiceAudio source");
                return 1;
            }
            else
            {
                Debug.LogWarning("⚠️ No AudioSources found - adding 3");
                for (int i = 0; i < 3; i++)
                {
                    AudioSource src = Undo.AddComponent<AudioSource>(player);
                    src.playOnAwake = false;
                    src.spatialBlend = 1f;
                }
                Debug.Log("✅ Added 3 AudioSources");
                return 1;
            }
        }

        static void ValidateCrosshair(GameObject player)
        {
            Debug.Log("--- Validating Crosshair ---");

            Component crosshair = player.GetComponent("SimpleCrosshair");
            if (crosshair != null)
            {
                Debug.LogWarning("⚠️ SimpleCrosshair found on player");
                Debug.Log("💡 RECOMMENDATION: SimpleCrosshair should only draw for local player");
                Debug.Log("   Check SimpleCrosshair script - it should check isLocalPlayer before drawing");
                Debug.Log("   If not, move crosshair to UI Canvas instead");
            }
            else
            {
                Debug.Log("ℹ️ No SimpleCrosshair component");
            }
        }

        static void AnalyzePlayer(GameObject player)
        {
            Debug.Log("--- COMPONENT ANALYSIS ---");

            Component[] allComponents = player.GetComponents<Component>();
            int componentCount = allComponents.Length;

            Debug.Log($"📊 Total Components: {componentCount}");

            // Categorize
            int networkComponents = 0;
            int movementComponents = 0;
            int combatComponents = 0;
            int visualComponents = 0;
            int audioComponents = 0;
            int utilityComponents = 0;

            foreach (Component comp in allComponents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;

                if (typeName.Contains("Network")) networkComponents++;
                else if (typeName.Contains("FPS") || typeName.Contains("Movement") || typeName.Contains("Character")) movementComponents++;
                else if (typeName.Contains("Weapon") || typeName.Contains("Health") || typeName.Contains("Hitbox")) combatComponents++;
                else if (typeName.Contains("Visual") || typeName.Contains("Renderer")) visualComponents++;
                else if (typeName.Contains("Audio")) audioComponents++;
                else utilityComponents++;
            }

            Debug.Log($"  Network: {networkComponents}");
            Debug.Log($"  Movement: {movementComponents}");
            Debug.Log($"  Combat: {combatComponents}");
            Debug.Log($"  Visual: {visualComponents}");
            Debug.Log($"  Audio: {audioComponents}");
            Debug.Log($"  Utility: {utilityComponents}");

            // Recommendations
            Debug.Log("--- RECOMMENDATIONS ---");

            if (componentCount > 20)
            {
                Debug.LogWarning($"⚠️ High component count ({componentCount}) - consider refactoring");
            }
            else if (componentCount > 15)
            {
                Debug.Log($"💡 Component count is acceptable ({componentCount}) but could be optimized");
            }
            else
            {
                Debug.Log($"✅ Component count is good ({componentCount})");
            }

            if (audioComponents < 3)
            {
                Debug.LogWarning("⚠️ Recommended: 3 AudioSources (Footstep, Weapon, Voice)");
            }

            // Check for redundancy
            CheckForRedundancy(player);
        }

        static void CheckForRedundancy(GameObject player)
        {
            Debug.Log("--- CHECKING FOR REDUNDANCY ---");

            // Check for duplicate functionality
            bool hasFPSController = player.GetComponent("FPSController") != null;
            bool hasRigidbodyMovement = player.GetComponent("RigidbodyPlayerMovement") != null;

            if (hasFPSController && hasRigidbodyMovement)
            {
                Debug.LogError("❌ REDUNDANCY: Both FPSController and RigidbodyPlayerMovement found!");
                Debug.LogError("   Remove one movement system - they conflict!");
            }

            // Check for multiple cameras
            Camera[] cameras = player.GetComponentsInChildren<Camera>();
            if (cameras.Length > 1)
            {
                Debug.LogWarning($"⚠️ Found {cameras.Length} cameras - usually only need 1");
            }

            // Check for missing required components
            if (player.GetComponent("NetworkIdentity") == null)
            {
                Debug.LogError("❌ MISSING: NetworkIdentity (required for multiplayer)");
            }

            if (player.GetComponent("Health") == null)
            {
                Debug.LogWarning("⚠️ MISSING: Health component (recommended)");
            }

            Debug.Log("✅ Redundancy check complete");
        }
    }
}
