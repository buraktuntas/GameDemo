using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Automatic Player Prefab Setup Tool
    /// Adds PlayerComponents, creates attach points, validates setup
    ///
    /// Usage: Select player prefab → Right-click → "Fix Player Prefab Setup"
    /// </summary>
    public class PlayerPrefabAutoSetup : EditorWindow
    {
        private GameObject playerPrefab;
        private bool createAttachPoints = true;
        private bool addPlayerComponents = true;
        private bool validateSetup = true;
        private bool removeUnusedComponents = false;

        [MenuItem("Tools/Tactical Combat/Auto-Setup Player Prefab")]
        static void ShowWindow()
        {
            PlayerPrefabAutoSetup window = GetWindow<PlayerPrefabAutoSetup>("Player Auto-Setup");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        // Context menu - right click on GameObject
        [MenuItem("GameObject/Tactical Combat/Fix Player Prefab Setup", false, 0)]
        static void FixPlayerPrefabContextMenu()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                AutoSetupPlayer(selected);
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a player prefab in the hierarchy or project.", "OK");
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Prefab Auto-Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool will automatically:\n• Add PlayerComponents script\n• Create attach points (Weapon/Head/Back)\n• Validate all required components\n• Remove unused/dead code references", MessageType.Info);

            EditorGUILayout.Space(10);

            // Player prefab selection
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), true);

            EditorGUILayout.Space(10);

            // Options
            EditorGUILayout.LabelField("Options:", EditorStyles.boldLabel);
            addPlayerComponents = EditorGUILayout.Toggle("Add PlayerComponents", addPlayerComponents);
            createAttachPoints = EditorGUILayout.Toggle("Create Attach Points", createAttachPoints);
            validateSetup = EditorGUILayout.Toggle("Validate Setup", validateSetup);
            removeUnusedComponents = EditorGUILayout.Toggle("Remove Unused Scripts", removeUnusedComponents);

            EditorGUILayout.Space(10);

            GUI.enabled = playerPrefab != null;
            if (GUILayout.Button("🚀 Auto-Setup Player", GUILayout.Height(40)))
            {
                AutoSetupPlayer(playerPrefab);
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Validate Current Setup (No Changes)", GUILayout.Height(30)))
            {
                if (playerPrefab != null)
                {
                    ValidatePlayerSetup(playerPrefab, false);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Prefab", "Please assign a player prefab first.", "OK");
                }
            }
        }

        static void AutoSetupPlayer(GameObject player)
        {
            if (player == null)
            {
                Debug.LogError("Player prefab is null!");
                return;
            }

            // Start undo group
            Undo.SetCurrentGroupName("Auto-Setup Player Prefab");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("=== PLAYER AUTO-SETUP STARTED ===");

            // Step 1: Add PlayerComponents if missing
            PlayerComponents pc = player.GetComponent<PlayerComponents>();
            if (pc == null)
            {
                pc = Undo.AddComponent<PlayerComponents>(player);
                Debug.Log("✅ Added PlayerComponents script");
            }
            else
            {
                Debug.Log("ℹ️ PlayerComponents already exists");
            }

            // Step 2: Create attach points
            CreateAttachPoints(player);

            // Step 3: Assign attach points to PlayerComponents
            AssignAttachPoints(player, pc);

            // Step 4: Validate required components
            ValidatePlayerSetup(player, true);

            // Step 5: Remove unused components
            RemoveUnusedComponents(player);

            // Collapse undo group
            Undo.CollapseUndoOperations(undoGroup);

            // Mark dirty
            EditorUtility.SetDirty(player);

            Debug.Log("=== PLAYER AUTO-SETUP COMPLETE ===");
            EditorUtility.DisplayDialog("Setup Complete!",
                "Player prefab setup complete!\n\n✅ PlayerComponents added\n✅ Attach points created\n✅ Setup validated\n\nCheck Console for details.",
                "OK");
        }

        static void CreateAttachPoints(GameObject player)
        {
            Debug.Log("--- Creating Attach Points ---");

            // Find or create WeaponAttachPoint
            Transform weaponAttach = player.transform.Find("WeaponAttachPoint");
            if (weaponAttach == null)
            {
                // Try to find existing WeaponHolder
                weaponAttach = player.transform.Find("WeaponHolder");
                if (weaponAttach == null)
                {
                    GameObject weaponObj = new GameObject("WeaponAttachPoint");
                    Undo.RegisterCreatedObjectUndo(weaponObj, "Create WeaponAttachPoint");
                    weaponAttach = weaponObj.transform;
                    weaponAttach.SetParent(player.transform);
                    weaponAttach.localPosition = new Vector3(0.3f, 1.4f, 0.3f); // Right hand position
                    weaponAttach.localRotation = Quaternion.identity;
                    Debug.Log("✅ Created WeaponAttachPoint");
                }
                else
                {
                    Debug.Log("ℹ️ Using existing WeaponHolder");
                }
            }
            else
            {
                Debug.Log("ℹ️ WeaponAttachPoint already exists");
            }

            // Find or create HeadAttachPoint
            Transform headAttach = player.transform.Find("HeadAttachPoint");
            if (headAttach == null)
            {
                GameObject headObj = new GameObject("HeadAttachPoint");
                Undo.RegisterCreatedObjectUndo(headObj, "Create HeadAttachPoint");
                headAttach = headObj.transform;
                headAttach.SetParent(player.transform);
                headAttach.localPosition = new Vector3(0f, 1.7f, 0f); // Head height from hitbox data
                headAttach.localRotation = Quaternion.identity;
                Debug.Log("✅ Created HeadAttachPoint");
            }
            else
            {
                Debug.Log("ℹ️ HeadAttachPoint already exists");
            }

            // Find or create BackAttachPoint
            Transform backAttach = player.transform.Find("BackAttachPoint");
            if (backAttach == null)
            {
                GameObject backObj = new GameObject("BackAttachPoint");
                Undo.RegisterCreatedObjectUndo(backObj, "Create BackAttachPoint");
                backAttach = backObj.transform;
                backAttach.SetParent(player.transform);
                backAttach.localPosition = new Vector3(0f, 1.2f, -0.2f); // Chest height, behind player
                backAttach.localRotation = Quaternion.identity;
                Debug.Log("✅ Created BackAttachPoint");
            }
            else
            {
                Debug.Log("ℹ️ BackAttachPoint already exists");
            }
        }

        static void AssignAttachPoints(GameObject player, PlayerComponents pc)
        {
            if (pc == null) return;

            Debug.Log("--- Assigning Attach Points ---");

            SerializedObject so = new SerializedObject(pc);

            // Weapon attach point
            if (pc.weaponAttachPoint == null)
            {
                Transform weaponAttach = player.transform.Find("WeaponAttachPoint");
                if (weaponAttach == null) weaponAttach = player.transform.Find("WeaponHolder");

                if (weaponAttach != null)
                {
                    SerializedProperty weaponProp = so.FindProperty("weaponAttachPoint");
                    weaponProp.objectReferenceValue = weaponAttach;
                    Debug.Log("✅ Assigned weaponAttachPoint");
                }
            }

            // Head attach point
            if (pc.headAttachPoint == null)
            {
                Transform headAttach = player.transform.Find("HeadAttachPoint");
                if (headAttach != null)
                {
                    SerializedProperty headProp = so.FindProperty("headAttachPoint");
                    headProp.objectReferenceValue = headAttach;
                    Debug.Log("✅ Assigned headAttachPoint");
                }
            }

            // Back attach point
            if (pc.backAttachPoint == null)
            {
                Transform backAttach = player.transform.Find("BackAttachPoint");
                if (backAttach != null)
                {
                    SerializedProperty backProp = so.FindProperty("backAttachPoint");
                    backProp.objectReferenceValue = backAttach;
                    Debug.Log("✅ Assigned backAttachPoint");
                }
            }

            so.ApplyModifiedProperties();
        }

        static void ValidatePlayerSetup(GameObject player, bool autoFix)
        {
            Debug.Log("--- Validating Player Setup ---");

            bool allGood = true;

            // Required components
            string[] requiredComponents = new string[]
            {
                "PlayerController",
                "FPSController",
                "InputManager",
                "PlayerComponents",
                "Health",
                "WeaponSystem",
                "AbilityController"
            };

            foreach (string componentName in requiredComponents)
            {
                Component comp = player.GetComponent(componentName);
                if (comp == null)
                {
                    Debug.LogWarning($"⚠️ Missing component: {componentName}");
                    allGood = false;
                }
                else
                {
                    Debug.Log($"✅ {componentName} found");
                }
            }

            // Check attach points
            PlayerComponents pc = player.GetComponent<PlayerComponents>();
            if (pc != null)
            {
                if (pc.weaponAttachPoint == null)
                {
                    Debug.LogWarning("⚠️ weaponAttachPoint not assigned");
                    allGood = false;
                }
                else
                {
                    Debug.Log("✅ weaponAttachPoint assigned");
                }

                if (pc.headAttachPoint == null)
                {
                    Debug.LogWarning("⚠️ headAttachPoint not assigned");
                    allGood = false;
                }
                else
                {
                    Debug.Log("✅ headAttachPoint assigned");
                }

                if (pc.backAttachPoint == null)
                {
                    Debug.LogWarning("⚠️ backAttachPoint not assigned");
                    allGood = false;
                }
                else
                {
                    Debug.Log("✅ backAttachPoint assigned");
                }
            }

            if (allGood)
            {
                Debug.Log("✅✅✅ Player setup is PERFECT! ✅✅✅");
            }
            else
            {
                Debug.LogWarning("⚠️ Player setup has issues. Run Auto-Setup to fix.");
            }
        }

        static void RemoveUnusedComponents(GameObject player)
        {
            Debug.Log("--- Checking for Unused Components ---");

            // List of known unused/dead scripts (from previous cleanup)
            string[] deadScripts = new string[]
            {
                "RigidbodyPlayerMovement",
                "RigidbodyPlayerCamera",
                "RigidbodyPlayerInputHandler"
            };

            int removed = 0;
            Component[] allComponents = player.GetComponents<Component>();

            foreach (Component comp in allComponents)
            {
                if (comp == null)
                {
                    // Missing script reference
                    Debug.LogWarning("⚠️ Found missing script reference (already deleted)");
                    continue;
                }

                string typeName = comp.GetType().Name;

                // Check if it's a dead script
                foreach (string deadScript in deadScripts)
                {
                    if (typeName.Contains(deadScript))
                    {
                        Debug.Log($"🗑️ Removing unused component: {typeName}");
                        Undo.DestroyObjectImmediate(comp);
                        removed++;
                        break;
                    }
                }
            }

            if (removed > 0)
            {
                Debug.Log($"✅ Removed {removed} unused component(s)");
            }
            else
            {
                Debug.Log("✅ No unused components found");
            }
        }

        // Quick validation from Project window
        [MenuItem("Assets/Validate Player Prefab Setup", true)]
        static bool ValidatePlayerPrefabMenu_Validate()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Assets/Validate Player Prefab Setup")]
        static void ValidatePlayerPrefabMenu()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                ValidatePlayerSetup(selected, false);
            }
        }
    }
}
