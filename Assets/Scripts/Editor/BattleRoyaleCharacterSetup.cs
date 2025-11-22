using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ BATTLE ROYALE CHARACTER SETUP: Battle Royale Duo Polyart PBR paketini Player'a otomatik uygular
    /// </summary>
    public class BattleRoyaleCharacterSetup : EditorWindow
    {
        private enum CharacterType
        {
            MalePBR,
            FemalePBR,
            MalePolyart,
            FemalePolyart
        }

        private enum WeaponType
        {
            AssaultRifle,
            Pistol
        }

        private CharacterType selectedCharacter = CharacterType.MalePBR;
        private WeaponType selectedWeapon = WeaponType.AssaultRifle;

        [MenuItem("Tools/Tactical Combat/Setup Battle Royale Character")]
        public static void ShowWindow()
        {
            GetWindow<BattleRoyaleCharacterSetup>("Battle Royale Character Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Battle Royale Character Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool Battle Royale Duo Polyart PBR paketini Player prefab'ına uygular:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Character Selection
            GUILayout.Label("Character Type:", EditorStyles.boldLabel);
            selectedCharacter = (CharacterType)EditorGUILayout.EnumPopup(selectedCharacter);
            GUILayout.Space(5);

            // Weapon Selection
            GUILayout.Label("Weapon Animator:", EditorStyles.boldLabel);
            selectedWeapon = (WeaponType)EditorGUILayout.EnumPopup(selectedWeapon);
            GUILayout.Space(20);

            if (GUILayout.Button("Auto Setup Player Prefab", GUILayout.Height(30)))
            {
                ApplyBattleRoyaleCharacter();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Check Available Assets", GUILayout.Height(30)))
            {
                CheckAvailableAssets();
            }
        }

        private static void ApplyBattleRoyaleCharacter()
        {
            var window = GetWindow<BattleRoyaleCharacterSetup>();

            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            // Find character prefab
            string characterPrefabPath = GetCharacterPrefabPath(window.selectedCharacter);
            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterPrefabPath);

            if (characterPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", $"Character prefab not found at: {characterPrefabPath}", "OK");
                Debug.LogError($"❌ Character prefab not found: {characterPrefabPath}");
                return;
            }

            // Find animator controller
            string animatorControllerPath = GetAnimatorControllerPath(window.selectedWeapon);
            RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);

            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Warning", $"Animator Controller not found at: {animatorControllerPath}\nCharacter will be applied without animator.", "Continue");
                Debug.LogWarning($"⚠️ Animator Controller not found: {animatorControllerPath}");
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🎮 BATTLE ROYALE CHARACTER SETUP");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. Find or create PlayerVisual GameObject
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual == null)
            {
                GameObject visualGO = new GameObject("PlayerVisual");
                visualGO.transform.SetParent(playerInstance.transform);
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;
                visualGO.transform.localScale = Vector3.one;
                playerVisual = visualGO.transform;
                modified = true;
                Debug.Log("✅ Created PlayerVisual GameObject");
            }

            // 2. Remove old character models and capsule
            RemoveOldCharacterModel(playerVisual);
            modified = true;

            // 3. Instantiate new Battle Royale character model
            GameObject characterInstance = PrefabUtility.InstantiatePrefab(characterPrefab) as GameObject;
            if (characterInstance != null)
            {
                characterInstance.name = "BattleRoyaleCharacter";
                characterInstance.transform.SetParent(playerVisual);
                characterInstance.transform.localPosition = Vector3.zero;
                characterInstance.transform.localRotation = Quaternion.identity;
                characterInstance.transform.localScale = Vector3.one;

                Debug.Log($"✅ Added Battle Royale character: {characterPrefab.name}");
                modified = true;

                // 4. Setup Animator
                Animator animator = characterInstance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = characterInstance.GetComponentInChildren<Animator>();
                }

                if (animator == null)
                {
                    // Find root bone or create Animator on character root
                    Transform rootBone = FindRootBone(characterInstance.transform);
                    if (rootBone != null)
                    {
                        animator = rootBone.gameObject.AddComponent<Animator>();
                        Debug.Log("✅ Added Animator component to root bone");
                    }
                    else
                    {
                        animator = characterInstance.AddComponent<Animator>();
                        Debug.Log("✅ Added Animator component to character root");
                    }
                    modified = true;
                }

                // 5. Assign Animator Controller
                if (animatorController != null && animator != null)
                {
                    animator.runtimeAnimatorController = animatorController;
                    EditorUtility.SetDirty(animator);
                    Debug.Log($"✅ Assigned Animator Controller: {animatorController.name}");
                    modified = true;
                }
                else if (animator != null)
                {
                    Debug.LogWarning("⚠️ No Animator Controller assigned - character will not animate");
                }

                // 6. Configure Animator Avatar
                if (animator != null && animator.avatar == null)
                {
                    // Try to find Avatar in character prefab
                    Avatar avatar = FindAvatarInPrefab(characterPrefab);
                    if (avatar != null)
                    {
                        animator.avatar = avatar;
                        EditorUtility.SetDirty(animator);
                        Debug.Log("✅ Assigned Avatar to Animator");
                        modified = true;
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No Avatar found - animations may not work correctly");
                    }
                }

                // 7. Update PlayerVisuals component
                PlayerVisuals playerVisuals = playerInstance.GetComponent<PlayerVisuals>();
                if (playerVisuals == null)
                {
                    playerVisuals = playerInstance.AddComponent<PlayerVisuals>();
                    Debug.Log("✅ Added PlayerVisuals component");
                    modified = true;
                }

                // Find renderer in character model
                Renderer newRenderer = characterInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (newRenderer == null)
                {
                    newRenderer = characterInstance.GetComponentInChildren<MeshRenderer>();
                }

                if (newRenderer != null)
                {
                    SerializedObject visualsSo = new SerializedObject(playerVisuals);
                    var rendererProp = visualsSo.FindProperty("visualRenderer");
                    if (rendererProp != null)
                    {
                        rendererProp.objectReferenceValue = newRenderer;
                        visualsSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(playerVisuals);
                        Debug.Log($"✅ Updated PlayerVisuals to use renderer: {newRenderer.name}");
                        modified = true;
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ No Renderer found in character model!");
                }

                // 8. Add BattleRoyaleAnimationController to Player (not character model)
                var animationController = playerInstance.GetComponent<BattleRoyaleAnimationController>();
                if (animationController == null)
                {
                    animationController = playerInstance.AddComponent<BattleRoyaleAnimationController>();
                    Debug.Log("✅ Added BattleRoyaleAnimationController to Player");
                    modified = true;
                }

                // Configure animation controller references
                if (animationController != null)
                {
                    SerializedObject animCtrlSo = new SerializedObject(animationController);
                    
                    // Set character animator reference
                    var animatorProp = animCtrlSo.FindProperty("characterAnimator");
                    if (animatorProp != null && animator != null)
                    {
                        animatorProp.objectReferenceValue = animator;
                        Debug.Log("✅ Set characterAnimator reference in BattleRoyaleAnimationController");
                        modified = true;
                    }

                    animCtrlSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(animationController);
                }
            }
            else
            {
                Debug.LogError("❌ Failed to instantiate character prefab!");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ Player prefab updated with Battle Royale character!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    $"Battle Royale character applied successfully!\n\n" +
                    $"Character: {window.selectedCharacter}\n" +
                    $"Animator: {(animatorController != null ? animatorController.name : "None")}", 
                    "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static string GetCharacterPrefabPath(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.MalePBR:
                    return "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultMalePBR.prefab";
                case CharacterType.FemalePBR:
                    return "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultFemalePBR.prefab";
                case CharacterType.MalePolyart:
                    return "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultMalePolyart.prefab";
                case CharacterType.FemalePolyart:
                    return "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultFemalePolyart.prefab";
                default:
                    return "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultMalePBR.prefab";
            }
        }

        private static string GetAnimatorControllerPath(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.AssaultRifle:
                    return "Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller";
                case WeaponType.Pistol:
                    return "Assets/BattleRoyaleDuoPAPBR/Animator/PistolAnimator.controller";
                default:
                    return "Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller";
            }
        }

        private static void RemoveOldCharacterModel(Transform playerVisual)
        {
            // Remove old capsule mesh
            MeshRenderer oldRenderer = playerVisual.GetComponent<MeshRenderer>();
            MeshFilter oldFilter = playerVisual.GetComponent<MeshFilter>();
            if (oldRenderer != null && oldFilter != null)
            {
                Object.DestroyImmediate(oldRenderer);
                Object.DestroyImmediate(oldFilter);
                Debug.Log("✅ Removed old capsule mesh");
            }

            // Remove existing character model children
            for (int i = playerVisual.childCount - 1; i >= 0; i--)
            {
                Transform child = playerVisual.GetChild(i);
                if (child == null) continue; // Safety check
                
                // ✅ FIX: Store name before destroying
                string childName = child.name;
                bool shouldRemove = childName.Contains("Character") || childName.Contains("Model") ||
                    childName.Contains("BattleRoyale") ||
                    child.GetComponent<SkinnedMeshRenderer>() != null ||
                    child.GetComponent<Animator>() != null;
                
                if (shouldRemove)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"✅ Removed old character model: {childName}");
                }
            }
        }

        private static Transform FindRootBone(Transform root)
        {
            // Battle Royale characters usually have a root bone named "Root" or "Hips" or "Pelvis"
            Transform[] allChildren = root.GetComponentsInChildren<Transform>();
            foreach (Transform t in allChildren)
            {
                if (t.name.ToLower().Contains("root") || 
                    t.name.ToLower().Contains("hips") || 
                    t.name.ToLower().Contains("pelvis") ||
                    t.name.ToLower().Contains("mixamorig:hips"))
                {
                    return t;
                }
            }
            return root; // Fallback to root
        }

        private static Avatar FindAvatarInPrefab(GameObject prefab)
        {
            // Check if prefab has an Animator with Avatar
            Animator animator = prefab.GetComponent<Animator>();
            if (animator != null && animator.avatar != null)
            {
                return animator.avatar;
            }

            // Check children
            animator = prefab.GetComponentInChildren<Animator>();
            if (animator != null && animator.avatar != null)
            {
                return animator.avatar;
            }

            return null;
        }

        private static void CheckAvailableAssets()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔍 CHECKING BATTLE ROYALE ASSETS");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Check character prefabs
            string[] characterPrefabs = new string[]
            {
                "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultMalePBR.prefab",
                "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultFemalePBR.prefab",
                "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultMalePolyart.prefab",
                "Assets/BattleRoyaleDuoPAPBR/Prefab/Character/DefaultFemalePolyart.prefab"
            };

            Debug.Log("Character Prefabs:");
            foreach (string path in characterPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Debug.Log($"  ✅ {path}");
                }
                else
                {
                    Debug.LogWarning($"  ❌ {path} - NOT FOUND");
                }
            }

            // Check animator controllers
            string[] animatorControllers = new string[]
            {
                "Assets/BattleRoyaleDuoPAPBR/Animator/AssaultRifleAnimator.controller",
                "Assets/BattleRoyaleDuoPAPBR/Animator/PistolAnimator.controller"
            };

            Debug.Log("\nAnimator Controllers:");
            foreach (string path in animatorControllers)
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if (controller != null)
                {
                    Debug.Log($"  ✅ {path}");
                }
                else
                {
                    Debug.LogWarning($"  ❌ {path} - NOT FOUND");
                }
            }

            Debug.Log("\n═══════════════════════════════════════════════════════\n");
        }
    }
}

