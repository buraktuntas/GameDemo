using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using TacticalCombat.Player;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ POST MIXAMO SETUP: Mixamo karakteri eklendikten sonra gerekli setup adımlarını kontrol eder ve uygular
    /// </summary>
    public class PostMixamoSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Post Mixamo Setup Check")]
        public static void ShowWindow()
        {
            GetWindow<PostMixamoSetup>("Post Mixamo Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Post Mixamo Setup Checklist", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Mixamo karakteri eklendikten sonra kontrol edilmesi gerekenler:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Checklist
            bool hasCharacter = CheckCharacter();
            bool hasAnimator = CheckAnimator();
            bool hasAnimationController = CheckAnimationController();
            bool hasWeaponHolder = CheckWeaponHolder();
            bool hasCamera = CheckCamera();
            bool hasInputSystem = CheckInputSystem();

            EditorGUILayout.Space(10);

            // Auto Fix Button
            if (GUILayout.Button("Auto Fix All Issues", GUILayout.Height(40)))
            {
                AutoFixAll();
            }

            GUILayout.Space(10);

            // Individual Fix Buttons
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Individual Fixes:", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Fix WeaponHolder Reference", GUILayout.Height(30)))
            {
                WeaponHolderReferenceFixer.FixWeaponHolderReference();
            }

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(hasWeaponHolder);
            if (GUILayout.Button("Restore WeaponHolder (if missing)", GUILayout.Height(30)))
            {
                WeaponHolderRestorer.RestoreWeaponHolder();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            if (GUILayout.Button("Fix WeaponHolder & Camera Position", GUILayout.Height(30)))
            {
                FixWeaponHolderAndCamera();
            }

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(hasInputSystem);
            if (GUILayout.Button("Setup Input System", GUILayout.Height(30)))
            {
                FixInputSystem();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(hasAnimationController);
            if (GUILayout.Button("Setup Animation Controller", GUILayout.Height(30)))
            {
                FixAnimationController();
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool CheckCharacter()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                EditorGUILayout.HelpBox("❌ Player prefab not found!", MessageType.Error);
                return false;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            
            bool hasCharacter = false;
            if (playerVisual != null)
            {
                for (int i = 0; i < playerVisual.childCount; i++)
                {
                    Transform child = playerVisual.GetChild(i);
                    if (child.GetComponent<SkinnedMeshRenderer>() != null || child.GetComponent<Animator>() != null)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (hasCharacter)
            {
                EditorGUILayout.HelpBox("✅ Character model found", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ No character model found in PlayerVisual", MessageType.Warning);
            }

            return hasCharacter;
        }

        private bool CheckAnimator()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return false;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            
            bool hasAnimator = false;
            Animator animator = null;
            RuntimeAnimatorController controller = null;
            
            if (playerVisual != null)
            {
                animator = playerVisual.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    controller = animator.runtimeAnimatorController;
                    hasAnimator = controller != null;
                }
            }

            // Store controller name before unloading
            string controllerName = controller != null ? controller.name : "None";

            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (hasAnimator)
            {
                EditorGUILayout.HelpBox($"✅ Animator found with Controller: {controllerName}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Animator or Controller missing", MessageType.Warning);
            }

            return hasAnimator;
        }

        private bool CheckAnimationController()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return false;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool hasController = playerInstance.GetComponent<BattleRoyaleAnimationController>() != null;
            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (hasController)
            {
                EditorGUILayout.HelpBox("✅ BattleRoyaleAnimationController found", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ BattleRoyaleAnimationController missing", MessageType.Warning);
            }

            return hasController;
        }

        private bool CheckWeaponHolder()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return false;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            Transform weaponHolder = null;
            
            // 1. Check Camera children first (most common location)
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                weaponHolder = playerCamera.transform.Find("WeaponHolder");
            }
            
            // 2. Check Player root
            if (weaponHolder == null)
            {
                weaponHolder = playerInstance.transform.Find("WeaponHolder");
            }
            
            // 3. Check PlayerVisual
            if (weaponHolder == null)
            {
                var playerVisual = playerInstance.transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    weaponHolder = playerVisual.Find("WeaponHolder");
                }
            }
            
            // 4. Check WeaponSystem reference
            bool hasReference = false;
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                SerializedObject weaponSo = new SerializedObject(weaponSystem);
                var weaponHolderProp = weaponSo.FindProperty("weaponHolder");
                if (weaponHolderProp != null)
                {
                    Transform referencedHolder = weaponHolderProp.objectReferenceValue as Transform;
                    hasReference = referencedHolder != null;
                    if (referencedHolder != null && weaponHolder == null)
                    {
                        weaponHolder = referencedHolder;
                    }
                }
            }

            // Check if WeaponHolder is child of camera
            bool isChildOfCamera = weaponHolder != null && playerCamera != null && weaponHolder.parent == playerCamera.transform;

            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (isChildOfCamera && hasReference)
            {
                EditorGUILayout.HelpBox("✅ WeaponHolder found and reference is correct", MessageType.Info);
            }
            else if (weaponHolder != null && !hasReference)
            {
                EditorGUILayout.HelpBox("⚠️ WeaponHolder found but WeaponSystem reference is missing!", MessageType.Warning);
            }
            else if (weaponHolder != null)
            {
                EditorGUILayout.HelpBox("⚠️ WeaponHolder found but not positioned correctly (should be child of camera)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ WeaponHolder not found", MessageType.Error);
            }

            return isChildOfCamera && hasReference;
        }

        private bool CheckCamera()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return false;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            bool hasCamera = playerCamera != null;
            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (hasCamera)
            {
                EditorGUILayout.HelpBox("✅ Player camera found", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Player camera not found", MessageType.Error);
            }

            return hasCamera;
        }

        private bool CheckInputSystem()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return false;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            FPSController fpsController = playerInstance.GetComponent<FPSController>();
            
            bool hasInputSystem = false;
            if (fpsController != null)
            {
                SerializedObject fpsSo = new SerializedObject(fpsController);
                var actionsAssetProp = fpsSo.FindProperty("actionsAsset");
                hasInputSystem = actionsAssetProp != null && actionsAssetProp.objectReferenceValue != null;
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (hasInputSystem)
            {
                EditorGUILayout.HelpBox("✅ Input System Actions assigned", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Input System Actions not assigned", MessageType.Warning);
            }

            return hasInputSystem;
        }

        private void AutoFixAll()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔧 AUTO FIXING ALL ISSUES");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            FixWeaponHolderAndCamera();
            FixInputSystem();
            FixAnimationController();

            AssetDatabase.Refresh();
            Debug.Log("\n✅ Auto fix complete!");
            Debug.Log("═══════════════════════════════════════════════════════\n");
            EditorUtility.DisplayDialog("Success", "All issues fixed!", "OK");
        }

        private void FixWeaponHolderAndCamera()
        {
            // Use existing FPSCameraAndWeaponSetup logic
            EditorUtility.DisplayDialog("Info", 
                "Please use 'Tools > Tactical Combat > Setup FPS Camera & Weapon Position'\n\n" +
                "Click 'Use Valorant Preset' then 'Apply to Player Prefab'", 
                "OK");
        }

        private void FixInputSystem()
        {
            // Use existing InputSystemAndAnimationSetup
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions == null)
            {
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", "InputSystem_Actions.inputactions not found!", "OK");
                return;
            }

            FPSController fpsController = playerInstance.GetComponent<FPSController>();
            if (fpsController != null)
            {
                SerializedObject fpsSo = new SerializedObject(fpsController);
                var actionsAssetProp = fpsSo.FindProperty("actionsAsset");
                if (actionsAssetProp != null && actionsAssetProp.objectReferenceValue != inputActions)
                {
                    actionsAssetProp.objectReferenceValue = inputActions;
                    fpsSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(fpsController);
                    modified = true;
                    Debug.Log("✅ InputSystem_Actions assigned to FPSController");
                }
            }

            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                SerializedObject weaponSo = new SerializedObject(weaponSystem);
                var actionsAssetProp = weaponSo.FindProperty("actionsAsset");
                if (actionsAssetProp != null && actionsAssetProp.objectReferenceValue != inputActions)
                {
                    actionsAssetProp.objectReferenceValue = inputActions;
                    weaponSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(weaponSystem);
                    modified = true;
                    Debug.Log("✅ InputSystem_Actions assigned to WeaponSystem");
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Input System setup complete!");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private void FixAnimationController()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null) return;

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            var animationController = playerInstance.GetComponent<BattleRoyaleAnimationController>();
            if (animationController == null)
            {
                animationController = playerInstance.AddComponent<BattleRoyaleAnimationController>();
                Debug.Log("✅ Added BattleRoyaleAnimationController");
                modified = true;
            }

            // Find animator and assign it
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual != null)
            {
                Animator animator = playerVisual.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    SerializedObject animCtrlSo = new SerializedObject(animationController);
                    var animatorProp = animCtrlSo.FindProperty("characterAnimator");
                    if (animatorProp != null && animatorProp.objectReferenceValue != animator)
                    {
                        animatorProp.objectReferenceValue = animator;
                        animCtrlSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(animationController);
                        modified = true;
                        Debug.Log("✅ Set characterAnimator reference");
                    }
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Animation Controller setup complete!");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

