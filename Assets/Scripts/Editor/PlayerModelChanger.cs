using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ PLAYER MODEL CHANGER: Player görselini animasyonlu karakter modeli ile değiştirir
    /// </summary>
    public class PlayerModelChanger : EditorWindow
    {
        private GameObject characterModelPrefab;
        private RuntimeAnimatorController animatorController;
        
        [MenuItem("Tools/Tactical Combat/Change Player Model")]
        public static void ShowWindow()
        {
            GetWindow<PlayerModelChanger>("Player Model Changer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Player Model Changer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool ile Player prefab'ına animasyonlu karakter modeli ekleyebilirsiniz:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Character Model Prefab
            GUILayout.Label("Character Model Prefab:", EditorStyles.boldLabel);
            characterModelPrefab = (GameObject)EditorGUILayout.ObjectField(
                characterModelPrefab, 
                typeof(GameObject), 
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Animator Controller
            GUILayout.Label("Animator Controller (Optional):", EditorStyles.boldLabel);
            animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                animatorController, 
                typeof(RuntimeAnimatorController), 
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(characterModelPrefab == null);
            if (GUILayout.Button("Apply to Player Prefab", GUILayout.Height(30)))
            {
                ApplyCharacterModel();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Remove Current Model (Reset to Capsule)", GUILayout.Height(30)))
            {
                RemoveCharacterModel();
            }
        }

        private static void ApplyCharacterModel()
        {
            var window = GetWindow<PlayerModelChanger>();
            if (window.characterModelPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a Character Model Prefab!", "OK");
                return;
            }

            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

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

            // 2. Remove old capsule mesh if exists
            MeshRenderer oldRenderer = playerVisual.GetComponent<MeshRenderer>();
            MeshFilter oldFilter = playerVisual.GetComponent<MeshFilter>();
            if (oldRenderer != null && oldFilter != null)
            {
                // Check if it's a primitive capsule
                if (oldFilter.sharedMesh != null && oldFilter.sharedMesh.name.Contains("Capsule"))
                {
                    Object.DestroyImmediate(oldRenderer);
                    Object.DestroyImmediate(oldFilter);
                    Debug.Log("✅ Removed old capsule mesh");
                    modified = true;
                }
            }

            // 3. Remove any existing character model children
            for (int i = playerVisual.childCount - 1; i >= 0; i--)
            {
                Transform child = playerVisual.GetChild(i);
                if (child.name.Contains("Character") || child.name.Contains("Model") || 
                    child.GetComponent<SkinnedMeshRenderer>() != null || 
                    child.GetComponent<Animator>() != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"✅ Removed old character model: {child.name}");
                    modified = true;
                }
            }

            // 4. Instantiate new character model
            GameObject characterInstance = PrefabUtility.InstantiatePrefab(window.characterModelPrefab) as GameObject;
            if (characterInstance != null)
            {
                characterInstance.name = "CharacterModel";
                characterInstance.transform.SetParent(playerVisual);
                characterInstance.transform.localPosition = Vector3.zero;
                characterInstance.transform.localRotation = Quaternion.identity;
                characterInstance.transform.localScale = Vector3.one;
                
                Debug.Log($"✅ Added character model: {window.characterModelPrefab.name}");
                modified = true;

                // 5. Setup Animator
                Animator animator = characterInstance.GetComponent<Animator>();
                if (animator == null)
                {
                    // Try to find in children
                    animator = characterInstance.GetComponentInChildren<Animator>();
                }

                if (animator == null)
                {
                    // Add Animator if not found
                    animator = characterInstance.AddComponent<Animator>();
                    Debug.Log("✅ Added Animator component");
                    modified = true;
                }

                // 6. Assign Animator Controller if provided
                if (window.animatorController != null && animator != null)
                {
                    animator.runtimeAnimatorController = window.animatorController;
                    Debug.Log($"✅ Assigned Animator Controller: {window.animatorController.name}");
                    modified = true;
                }

                // 7. Update PlayerVisuals component to use new renderer
                PlayerVisuals playerVisuals = playerInstance.GetComponent<PlayerVisuals>();
                if (playerVisuals != null)
                {
                    // Find SkinnedMeshRenderer or MeshRenderer in character model
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
                            Debug.Log($"✅ Updated PlayerVisuals to use new renderer: {newRenderer.name}");
                            modified = true;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Failed to instantiate character model prefab!");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Player prefab updated with character model!");
                EditorUtility.DisplayDialog("Success", "Character model applied successfully!", "OK");
            }
            else
            {
                Debug.Log("ℹ️ No changes made");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static void RemoveCharacterModel()
        {
            if (!EditorUtility.DisplayDialog("Confirm", "Remove current character model and reset to capsule?", "Yes", "No"))
            {
                return;
            }

            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            // Find PlayerVisual
            Transform playerVisual = playerInstance.transform.Find("PlayerVisual");
            if (playerVisual != null)
            {
                // Remove all children (character models)
                for (int i = playerVisual.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(playerVisual.GetChild(i).gameObject);
                    modified = true;
                }

                // Remove existing renderers
                MeshRenderer oldRenderer = playerVisual.GetComponent<MeshRenderer>();
                MeshFilter oldFilter = playerVisual.GetComponent<MeshFilter>();
                if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
                if (oldFilter != null) Object.DestroyImmediate(oldFilter);

                // Create capsule mesh
                GameObject tempCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                MeshFilter meshFilter = playerVisual.gameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = playerVisual.gameObject.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = tempCapsule.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tempCapsule);

                // Update PlayerVisuals
                PlayerVisuals playerVisuals = playerInstance.GetComponent<PlayerVisuals>();
                if (playerVisuals != null)
                {
                    SerializedObject visualsSo = new SerializedObject(playerVisuals);
                    var rendererProp = visualsSo.FindProperty("visualRenderer");
                    if (rendererProp != null)
                    {
                        rendererProp.objectReferenceValue = meshRenderer;
                        visualsSo.ApplyModifiedProperties();
                    }
                }

                modified = true;
                Debug.Log("✅ Reset to capsule mesh");
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("✅ Player prefab reset to capsule!");
                EditorUtility.DisplayDialog("Success", "Player model reset to capsule!", "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

