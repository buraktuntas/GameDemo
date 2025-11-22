using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using TacticalCombat.Player;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ SETUP: Input System Actions ve Animation dosyalarını Player'a otomatik atar
    /// </summary>
    public class InputSystemAndAnimationSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Setup Input System & Animation")]
        public static void ShowWindow()
        {
            GetWindow<InputSystemAndAnimationSetup>("Input System & Animation Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Input System & Animation Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool şunları yapar:", EditorStyles.wordWrappedLabel);
            GUILayout.Label("1. InputSystem_Actions.inputactions dosyasını Player'a atar", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Animator Controller'ı Player'a atar (varsa)", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Animation dosyalarını kontrol eder", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            if (GUILayout.Button("Auto Setup Player Prefab", GUILayout.Height(30)))
            {
                SetupPlayerPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Check Current Setup", GUILayout.Height(30)))
            {
                CheckCurrentSetup();
            }
        }

        private static void SetupPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            // 1. Load InputSystem_Actions
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions == null)
            {
                Debug.LogWarning("⚠️ InputSystem_Actions.inputactions not found at Assets/InputSystem_Actions.inputactions");
            }
            else
            {
                Debug.Log("✅ Found InputSystem_Actions.inputactions");
            }

            // 2. Assign to FPSController
            FPSController fpsController = playerInstance.GetComponent<FPSController>();
            if (fpsController != null && inputActions != null)
            {
                SerializedObject fpsSo = new SerializedObject(fpsController);
                var actionsAssetProp = fpsSo.FindProperty("actionsAsset");
                if (actionsAssetProp != null)
                {
                    if (actionsAssetProp.objectReferenceValue != inputActions)
                    {
                        actionsAssetProp.objectReferenceValue = inputActions;
                        fpsSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(fpsController);
                        modified = true;
                        Debug.Log("✅ InputSystem_Actions assigned to FPSController");
                    }
                    else
                    {
                        Debug.Log("ℹ️ FPSController already has InputSystem_Actions assigned");
                    }
                }
            }

            // 3. Assign to WeaponSystem
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null && inputActions != null)
            {
                SerializedObject weaponSo = new SerializedObject(weaponSystem);
                var actionsAssetProp = weaponSo.FindProperty("actionsAsset");
                if (actionsAssetProp != null)
                {
                    if (actionsAssetProp.objectReferenceValue != inputActions)
                    {
                        actionsAssetProp.objectReferenceValue = inputActions;
                        weaponSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(weaponSystem);
                        modified = true;
                        Debug.Log("✅ InputSystem_Actions assigned to WeaponSystem");
                    }
                    else
                    {
                        Debug.Log("ℹ️ WeaponSystem already has InputSystem_Actions assigned");
                    }
                }
            }

            // 4. Find and assign Animator Controller
            RuntimeAnimatorController animatorController = null;
            
            // Try to find Animator Controller in RPG Tiny Hero Duo folder
            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/RPG Tiny Hero Duo" });
            if (controllerGuids.Length > 0)
            {
                string controllerPath = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
                animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                Debug.Log($"✅ Found Animator Controller: {controllerPath}");
            }

            // If not found, try to find any Animator Controller
            if (animatorController == null)
            {
                string[] allControllerGuids = AssetDatabase.FindAssets("t:AnimatorController");
                foreach (string guid in allControllerGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    // Prefer controllers with "Player" or "Character" in name
                    if (path.Contains("Player") || path.Contains("Character") || path.Contains("RootMotion"))
                    {
                        animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                        Debug.Log($"✅ Found Animator Controller: {path}");
                        break;
                    }
                }
            }

            // 5. Assign Animator Controller to character model
            if (animatorController != null)
            {
                // Find PlayerVisual or CharacterModel
                Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    // Look for character model with Animator
                    Animator animator = playerVisual.GetComponentInChildren<Animator>();
                    if (animator == null)
                    {
                        // Try to find any GameObject that might need an Animator
                        for (int i = 0; i < playerVisual.childCount; i++)
                        {
                            Transform child = playerVisual.GetChild(i);
                            if (child.name.Contains("Character") || child.name.Contains("Model") || 
                                child.GetComponent<SkinnedMeshRenderer>() != null)
                            {
                                animator = child.GetComponent<Animator>();
                                if (animator == null)
                                {
                                    animator = child.gameObject.AddComponent<Animator>();
                                    Debug.Log($"✅ Added Animator component to {child.name}");
                                    modified = true;
                                }
                                break;
                            }
                        }
                    }

                    if (animator != null)
                    {
                        if (animator.runtimeAnimatorController != animatorController)
                        {
                            animator.runtimeAnimatorController = animatorController;
                            EditorUtility.SetDirty(animator);
                            modified = true;
                            Debug.Log($"✅ Animator Controller assigned: {animatorController.name}");
                        }
                        else
                        {
                            Debug.Log($"ℹ️ Animator already has controller assigned: {animatorController.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No Animator found in PlayerVisual. Add character model first using 'Change Player Model' tool.");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ PlayerVisual not found. Character model might not be set up yet.");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No Animator Controller found. Make sure you have an Animator Controller in your project.");
            }

            // 6. Check for Animation files
            string[] animationGuids = AssetDatabase.FindAssets("t:Animation", new[] { "Assets" });
            Debug.Log($"ℹ️ Found {animationGuids.Length} Animation file(s) in project");
            foreach (string guid in animationGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"   - {path}");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Player prefab updated with Input System and Animation!");
                EditorUtility.DisplayDialog("Success", "Input System Actions and Animation setup completed!", "OK");
            }
            else
            {
                Debug.Log("ℹ️ No changes needed - everything is already set up correctly");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static void CheckCurrentSetup()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔍 CHECKING CURRENT SETUP");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Check InputSystem_Actions
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions == null)
            {
                Debug.LogError("❌ InputSystem_Actions.inputactions NOT FOUND");
            }
            else
            {
                Debug.Log("✅ InputSystem_Actions.inputactions found");

                // Check FPSController
                FPSController fpsController = playerInstance.GetComponent<FPSController>();
                if (fpsController != null)
                {
                    SerializedObject fpsSo = new SerializedObject(fpsController);
                    var actionsAssetProp = fpsSo.FindProperty("actionsAsset");
                    if (actionsAssetProp != null)
                    {
                        if (actionsAssetProp.objectReferenceValue == inputActions)
                        {
                            Debug.Log("✅ FPSController has InputSystem_Actions assigned");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ FPSController does NOT have InputSystem_Actions assigned");
                        }
                    }
                }

                // Check WeaponSystem
                WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
                if (weaponSystem != null)
                {
                    SerializedObject weaponSo = new SerializedObject(weaponSystem);
                    var actionsAssetProp = weaponSo.FindProperty("actionsAsset");
                    if (actionsAssetProp != null)
                    {
                        if (actionsAssetProp.objectReferenceValue == inputActions)
                        {
                            Debug.Log("✅ WeaponSystem has InputSystem_Actions assigned");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ WeaponSystem does NOT have InputSystem_Actions assigned");
                        }
                    }
                }
            }

            // Check Animator Controller
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual != null)
            {
                Animator animator = playerVisual.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (animator.runtimeAnimatorController != null)
                    {
                        Debug.Log($"✅ Animator found with Controller: {animator.runtimeAnimatorController.name}");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Animator found but NO Controller assigned");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ No Animator found in PlayerVisual");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerVisual not found");
            }

            Debug.Log("\n═══════════════════════════════════════════════════════\n");

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

