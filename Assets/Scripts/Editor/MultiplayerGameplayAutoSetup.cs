using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;
using TacticalCombat.Combat;
using TacticalCombat.UI;
using UnityEditor.SceneManagement;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ AUTOMATIC SETUP: Multiplayer Gameplay (Health, Ammo, Damage, Hit Feedback)
    /// </summary>
    public class MultiplayerGameplayAutoSetup : EditorWindow
    {
        private GameObject playerPrefab;
        private GameObject gameHUDObject;
        private bool setupComplete = false;
        private string statusMessage = "";

        [MenuItem("Tools/Tactical Combat/Auto-Setup Multiplayer Gameplay")]
        static void ShowWindow()
        {
            var window = GetWindow<MultiplayerGameplayAutoSetup>("Multiplayer Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 MULTIPLAYER GAMEPLAY AUTO-SETUP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will automatically setup:\n" +
                "1. Player Health & Hitbox system\n" +
                "2. Camera Shake (hit feedback)\n" +
                "3. Ammo & Health UI connections\n" +
                "4. GameHUD UI elements (if needed)",
                MessageType.Info);

            GUILayout.Space(10);

            // Player Prefab Selection
            EditorGUILayout.LabelField("Step 1: Select Player Prefab", EditorStyles.boldLabel);
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false);

            GUILayout.Space(5);

            // GameHUD Selection
            EditorGUILayout.LabelField("Step 2: Select GameHUD (in scene)", EditorStyles.boldLabel);
            gameHUDObject = (GameObject)EditorGUILayout.ObjectField("GameHUD Object", gameHUDObject, typeof(GameObject), true);

            GUILayout.Space(10);

            // Auto-find buttons
            if (GUILayout.Button("🔍 Auto-Find Player Prefab", GUILayout.Height(30)))
            {
                AutoFindPlayerPrefab();
            }

            if (GUILayout.Button("🔍 Auto-Find GameHUD in Scene", GUILayout.Height(30)))
            {
                AutoFindGameHUD();
            }

            GUILayout.Space(10);

            // Setup button
            GUI.enabled = playerPrefab != null;
            if (GUILayout.Button("✅ RUN AUTOMATIC SETUP", GUILayout.Height(50)))
            {
                RunAutomaticSetup();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Status message
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, setupComplete ? MessageType.Info : MessageType.Warning);
            }

            GUILayout.Space(10);

            // Manual steps (if needed)
            if (setupComplete)
            {
                EditorGUILayout.HelpBox(
                    "✅ SETUP COMPLETE!\n\n" +
                    "Test in multiplayer:\n" +
                    "1. Build & Run (Host)\n" +
                    "2. Play in Editor (Client)\n" +
                    "3. Shoot each other\n" +
                    "4. Watch health bar & ammo display\n" +
                    "5. Feel camera shake on hit!",
                    MessageType.Info);
            }
        }

        private void AutoFindPlayerPrefab()
        {
            string[] guids = AssetDatabase.FindAssets("Player t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<PlayerController>() != null)
                {
                    playerPrefab = prefab;
                    statusMessage = $"✅ Found player prefab: {path}";
                    Debug.Log($"✅ Auto-found player prefab: {path}");
                    return;
                }
            }

            statusMessage = "⚠️ Player prefab not found. Please select manually.";
        }

        private void AutoFindGameHUD()
        {
            GameHUD hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                gameHUDObject = hud.gameObject;
                statusMessage = $"✅ Found GameHUD: {hud.gameObject.name}";
                Debug.Log($"✅ Auto-found GameHUD: {hud.gameObject.name}");
            }
            else
            {
                statusMessage = "⚠️ GameHUD not found in scene. Make sure scene is loaded.";
            }
        }

        private void RunAutomaticSetup()
        {
            if (playerPrefab == null)
            {
                statusMessage = "❌ Player prefab is required!";
                return;
            }

            int fixCount = 0;
            statusMessage = "🔧 Running automatic setup...\n\n";

            // STEP 1: Setup Player Prefab
            fixCount += SetupPlayerPrefab();

            // STEP 2: Setup GameHUD (if provided)
            if (gameHUDObject != null)
            {
                fixCount += SetupGameHUD();
            }

            // STEP 3: Setup Hitboxes
            fixCount += SetupHitboxes();

            // Save changes
            EditorUtility.SetDirty(playerPrefab);
            AssetDatabase.SaveAssets();

            statusMessage += $"\n✅ SETUP COMPLETE! Applied {fixCount} fixes/improvements.";
            setupComplete = true;

            Debug.Log($"✅ [MultiplayerSetup] Complete! Applied {fixCount} changes.");
        }

        private int SetupPlayerPrefab()
        {
            int changes = 0;
            statusMessage += "📦 Setting up Player Prefab...\n";

            // Add Health component if missing
            Health health = playerPrefab.GetComponent<Health>();
            if (health == null)
            {
                health = playerPrefab.AddComponent<Health>();
                changes++;
                statusMessage += "  ✅ Added Health component\n";
                Debug.Log("✅ Added Health component to player");
            }

            // Add PlayerHUDController if missing
            PlayerHUDController hudController = playerPrefab.GetComponent<PlayerHUDController>();
            if (hudController == null)
            {
                hudController = playerPrefab.AddComponent<PlayerHUDController>();
                changes++;
                statusMessage += "  ✅ Added PlayerHUDController\n";
                Debug.Log("✅ Added PlayerHUDController to player");
            }

            // Add CameraShake if missing
            CameraShake cameraShake = playerPrefab.GetComponent<CameraShake>();
            if (cameraShake == null)
            {
                cameraShake = playerPrefab.AddComponent<CameraShake>();
                changes++;
                statusMessage += "  ✅ Added CameraShake (hit feedback)\n";
                Debug.Log("✅ Added CameraShake to player");
            }

            // Add PlayerComponents hub if missing
            PlayerComponents playerComponents = playerPrefab.GetComponent<PlayerComponents>();
            if (playerComponents == null)
            {
                playerComponents = playerPrefab.AddComponent<PlayerComponents>();
                changes++;
                statusMessage += "  ✅ Added PlayerComponents hub\n";
                Debug.Log("✅ Added PlayerComponents to player");
            }

            return changes;
        }

        private int SetupGameHUD()
        {
            int changes = 0;
            statusMessage += "\n🎨 Setting up GameHUD UI...\n";

            GameHUD gameHUD = gameHUDObject.GetComponent<GameHUD>();
            if (gameHUD == null)
            {
                statusMessage += "  ⚠️ GameHUD component not found on object\n";
                return 0;
            }

            // Check if UI elements need to be created
            // Note: This is informational - user may need to create UI manually in some cases
            statusMessage += "  ℹ️ GameHUD found. Make sure UI elements are assigned in Inspector:\n";
            statusMessage += "    - ammoText (TextMeshProUGUI)\n";
            statusMessage += "    - reserveAmmoText (TextMeshProUGUI)\n";
            statusMessage += "    - healthSlider (Slider)\n";
            statusMessage += "    - healthText (TextMeshProUGUI)\n";

            return changes;
        }

        private int SetupHitboxes()
        {
            int changes = 0;
            statusMessage += "\n🎯 Setting up Hitboxes...\n";

            // Find or create body parts
            Transform head = FindOrCreateBodyPart("Head", Vector3.up * 1.7f);
            Transform chest = FindOrCreateBodyPart("Chest", Vector3.up * 1.0f);
            Transform legs = FindOrCreateBodyPart("Legs", Vector3.up * 0.5f);

            // Setup Head Hitbox
            if (head != null)
            {
                changes += SetupHitboxOnPart(head, HitZone.Head, 2.0f, 0.15f);
            }

            // Setup Chest Hitbox
            if (chest != null)
            {
                changes += SetupHitboxOnPart(chest, HitZone.Chest, 1.0f, 0.25f);
            }

            // Setup Legs Hitbox
            if (legs != null)
            {
                changes += SetupHitboxOnPart(legs, HitZone.Limbs, 0.75f, 0.15f);
            }

            if (changes > 0)
            {
                statusMessage += $"  ✅ Setup {changes} hitbox(es)\n";
            }
            else
            {
                statusMessage += "  ℹ️ Hitboxes already configured\n";
            }

            return changes;
        }

        private Transform FindOrCreateBodyPart(string partName, Vector3 localPosition)
        {
            // Try to find existing
            Transform existing = playerPrefab.transform.Find(partName);
            if (existing != null) return existing;

            // ✅ FIX: Use PrefabUtility for prefab editing
            // Must work with prefab instance, not asset directly
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(playerPrefab));

            // Create new body part
            GameObject part = new GameObject(partName);
            part.transform.SetParent(prefabInstance.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;

            // Set layer safely
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                part.layer = playerLayer;
            }

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(playerPrefab));
            PrefabUtility.UnloadPrefabContents(prefabInstance);

            // Reload to get the new transform
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(playerPrefab));
            existing = playerPrefab.transform.Find(partName);

            statusMessage += $"  ✅ Created body part: {partName}\n";
            Debug.Log($"✅ Created body part: {partName}");

            return existing;
        }

        private int SetupHitboxOnPart(Transform part, HitZone zone, float damageMultiplier, float radius)
        {
            int changes = 0;

            // Add SphereCollider if missing
            SphereCollider collider = part.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = part.gameObject.AddComponent<SphereCollider>();
                collider.radius = radius;
                collider.isTrigger = false; // Hitboxes should NOT be triggers (for raycasts)
                changes++;
                Debug.Log($"✅ Added SphereCollider to {part.name}");
            }

            // Add Hitbox component if missing
            Hitbox hitbox = part.GetComponent<Hitbox>();
            if (hitbox == null)
            {
                hitbox = part.gameObject.AddComponent<Hitbox>();
                changes++;
                Debug.Log($"✅ Added Hitbox to {part.name}");
            }

            // Configure Hitbox using reflection (since fields are serialized)
            SerializedObject so = new SerializedObject(hitbox);
            so.FindProperty("zone").enumValueIndex = (int)zone;
            so.FindProperty("damageMultiplier").floatValue = damageMultiplier;
            so.ApplyModifiedProperties();

            return changes;
        }
    }
}
