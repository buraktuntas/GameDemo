using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON HAND ATTACHER: T-pose karaktere silahı eline (RightHand bone) bağlar
    /// </summary>
    public class WeaponHandAttacher : EditorWindow
    {
        private GameObject weaponPrefab;
        private string handBoneName = "RightHand";
        private Vector3 weaponPosition = new Vector3(0.05f, -0.02f, 0.1f);
        private Vector3 weaponRotation = new Vector3(0f, 0f, 0f);
        private Vector3 weaponScale = Vector3.one;
        private bool createWeaponHolder = true;
        private bool adjustForFPS = true;

        [MenuItem("Tools/Tactical Combat/Attach Weapon to Hand (T-Pose)")]
        public static void ShowWindow()
        {
            GetWindow<WeaponHandAttacher>("Weapon Hand Attacher");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Hand Attacher", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool T-pose karaktere silahı eline (RightHand bone) bağlar:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            // Weapon Prefab
            GUILayout.Label("Weapon Prefab:", EditorStyles.boldLabel);
            weaponPrefab = (GameObject)EditorGUILayout.ObjectField(
                weaponPrefab,
                typeof(GameObject),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(5);

            // Hand Bone Name
            GUILayout.Label("Hand Bone Name:", EditorStyles.boldLabel);
            handBoneName = EditorGUILayout.TextField(handBoneName);
            GUILayout.Space(5);

            // Weapon Position (relative to hand)
            GUILayout.Label("Weapon Position (relative to hand):", EditorStyles.boldLabel);
            weaponPosition = EditorGUILayout.Vector3Field("", weaponPosition);
            GUILayout.Space(5);

            // Weapon Rotation
            GUILayout.Label("Weapon Rotation:", EditorStyles.boldLabel);
            weaponRotation = EditorGUILayout.Vector3Field("", weaponRotation);
            GUILayout.Space(5);

            // Weapon Scale
            GUILayout.Label("Weapon Scale:", EditorStyles.boldLabel);
            weaponScale = EditorGUILayout.Vector3Field("", weaponScale);
            GUILayout.Space(10);

            // Options
            createWeaponHolder = EditorGUILayout.Toggle("Create WeaponHolder", createWeaponHolder);
            adjustForFPS = EditorGUILayout.Toggle("Adjust for FPS View", adjustForFPS);
            
            if (adjustForFPS)
            {
                EditorGUILayout.HelpBox("FPS View: WeaponHolder will be attached to Camera instead of hand for first-person view.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Third-Person View: WeaponHolder will be attached to hand bone for visible weapon in hand.", MessageType.Info);
            }

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(weaponPrefab == null);
            if (GUILayout.Button("Attach Weapon to Hand", GUILayout.Height(40)))
            {
                AttachWeaponToHand();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Find Hand Bone", GUILayout.Height(30)))
            {
                FindHandBone();
            }

            if (GUILayout.Button("Preview Weapon Position", GUILayout.Height(30)))
            {
                PreviewWeaponPosition();
            }
        }

        private void AttachWeaponToHand()
        {
            if (weaponPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Weapon Prefab!", "OK");
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
            GameObject weaponInstance = null; // Track weapon instance for cleanup

            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("🔧 ATTACHING WEAPON TO HAND");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Find character model (should have Animator)
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                Debug.LogError("❌ No Animator found on Player! Character model not found.");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", 
                    "No Animator found on Player!\n\n" +
                    "Please ensure the character model has an Animator component.",
                    "OK");
                return;
            }

            Debug.Log($"✅ Found Animator: {characterAnimator.name}");

            // Find hand bone
            Transform handBone = FindBoneInHierarchy(characterAnimator.transform, handBoneName);
            if (handBone == null)
            {
                Debug.LogError($"❌ Hand bone '{handBoneName}' not found!");
                Debug.Log("\n📋 Available bones (first 20):");
                ListBones(characterAnimator.transform, 0, 20);
                PrefabUtility.UnloadPrefabContents(playerInstance);
                EditorUtility.DisplayDialog("Error", 
                    $"Hand bone '{handBoneName}' not found!\n\n" +
                    "Check Console for available bone names.",
                    "OK");
                return;
            }

            Debug.Log($"✅ Found hand bone: {GetPath(handBone)}");

            if (adjustForFPS)
            {
                // FPS View: Attach WeaponHolder to Camera
                Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    Debug.LogError("❌ Player Camera not found!");
                    PrefabUtility.UnloadPrefabContents(playerInstance);
                    EditorUtility.DisplayDialog("Error", 
                        "Player Camera not found!\n\n" +
                        "For FPS view, Camera is required.",
                        "OK");
                    return;
                }

                Debug.Log($"✅ Found Camera: {playerCamera.name}");

                // Find or create WeaponHolder under Camera
                Transform weaponHolder = playerCamera.transform.Find("WeaponHolder");
                if (weaponHolder == null && createWeaponHolder)
                {
                    GameObject weaponHolderGO = new GameObject("WeaponHolder");
                    weaponHolder = weaponHolderGO.transform;
                    weaponHolder.SetParent(playerCamera.transform);
                    weaponHolder.localPosition = new Vector3(0.3f, -0.2f, 0.5f); // FPS position
                    weaponHolder.localRotation = Quaternion.identity;
                    weaponHolder.localScale = Vector3.one;
                    Debug.Log("✅ Created WeaponHolder under Camera");
                    modified = true;
                }
                else if (weaponHolder == null)
                {
                    Debug.LogError("❌ WeaponHolder not found and createWeaponHolder is false!");
                    PrefabUtility.UnloadPrefabContents(playerInstance);
                    return;
                }

                // Remove existing weapon
                Transform existingWeapon = weaponHolder.Find("CurrentWeapon");
                if (existingWeapon != null)
                {
                    string existingName = existingWeapon.name;
                    Object.DestroyImmediate(existingWeapon.gameObject);
                    Debug.Log($"✅ Removed existing weapon: {existingName}");
                    modified = true;
                }

                // Instantiate weapon
                weaponInstance = InstantiateWeapon(weaponPrefab);
                if (weaponInstance != null)
                {
                    weaponInstance.name = "CurrentWeapon";
                    weaponInstance.transform.SetParent(weaponHolder);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;
                    weaponInstance.transform.localScale = weaponScale;
                    
                    Debug.Log($"✅ Attached weapon to WeaponHolder (FPS view)");
                    modified = true;
                }

                // Update WeaponSystem reference
                UpdateWeaponSystemReference(playerInstance, weaponHolder);
            }
            else
            {
                // Third-Person View: Attach WeaponHolder to Hand Bone
                Transform weaponHolder = handBone.Find("WeaponHolder");
                if (weaponHolder == null && createWeaponHolder)
                {
                    GameObject weaponHolderGO = new GameObject("WeaponHolder");
                    weaponHolder = weaponHolderGO.transform;
                    weaponHolder.SetParent(handBone);
                    weaponHolder.localPosition = weaponPosition;
                    weaponHolder.localRotation = Quaternion.Euler(weaponRotation);
                    weaponHolder.localScale = Vector3.one;
                    Debug.Log($"✅ Created WeaponHolder under {handBoneName}");
                    modified = true;
                }
                else if (weaponHolder == null)
                {
                    Debug.LogError("❌ WeaponHolder not found and createWeaponHolder is false!");
                    PrefabUtility.UnloadPrefabContents(playerInstance);
                    return;
                }

                // Remove existing weapon
                Transform existingWeapon = weaponHolder.Find("CurrentWeapon");
                if (existingWeapon != null)
                {
                    string existingName = existingWeapon.name;
                    Object.DestroyImmediate(existingWeapon.gameObject);
                    Debug.Log($"✅ Removed existing weapon: {existingName}");
                    modified = true;
                }

                // Instantiate weapon
                weaponInstance = InstantiateWeapon(weaponPrefab);
                if (weaponInstance != null)
                {
                    weaponInstance.name = "CurrentWeapon";
                    weaponInstance.transform.SetParent(weaponHolder);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;
                    weaponInstance.transform.localScale = weaponScale;
                    
                    Debug.Log($"✅ Attached weapon to {handBoneName}");
                    modified = true;
                }

                // Also create WeaponHolder under Camera for FPS view (if Camera exists)
                Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    Transform fpsWeaponHolder = playerCamera.transform.Find("WeaponHolder");
                    if (fpsWeaponHolder == null)
                    {
                        GameObject fpsWeaponHolderGO = new GameObject("WeaponHolder");
                        fpsWeaponHolder = fpsWeaponHolderGO.transform;
                        fpsWeaponHolder.SetParent(playerCamera.transform);
                        fpsWeaponHolder.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                        fpsWeaponHolder.localRotation = Quaternion.identity;
                        fpsWeaponHolder.localScale = Vector3.one;
                        Debug.Log("✅ Created WeaponHolder under Camera (for FPS view)");
                        modified = true;
                    }

                    // Update WeaponSystem to use Camera's WeaponHolder for FPS
                    UpdateWeaponSystemReference(playerInstance, fpsWeaponHolder);
                }
            }

            // Clean up weapon instances (remove NetworkIdentity, etc.)
            if (weaponInstance != null)
            {
                CleanupWeapon(weaponInstance);
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("\n✅ Weapon attached to hand!");
                Debug.Log("═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", 
                    $"Weapon attached successfully!\n\n" +
                    $"Weapon: {weaponPrefab.name}\n" +
                    $"Hand Bone: {handBoneName}\n" +
                    $"FPS View: {(adjustForFPS ? "Camera/WeaponHolder" : "Hand + Camera")}",
                    "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private GameObject InstantiateWeapon(GameObject weaponPrefab)
        {
            string weaponPath = AssetDatabase.GetAssetPath(weaponPrefab);
            bool isPrefab = weaponPath.EndsWith(".prefab");

            if (isPrefab)
            {
                return PrefabUtility.InstantiatePrefab(weaponPrefab) as GameObject;
            }
            else
            {
                return Instantiate(weaponPrefab);
            }
        }

        private void CleanupWeapon(GameObject weaponInstance)
        {
            if (weaponInstance == null) return;

            // Remove NetworkIdentity
            var networkIdentity = weaponInstance.GetComponent<Mirror.NetworkIdentity>();
            if (networkIdentity != null)
            {
                Object.DestroyImmediate(networkIdentity);
            }

            // Remove NetworkTransform
            var networkTransform = weaponInstance.GetComponent("NetworkTransformReliable") ?? 
                                  weaponInstance.GetComponent("NetworkTransform") ??
                                  weaponInstance.GetComponent("NetworkTransformUnreliable");
            if (networkTransform != null)
            {
                Object.DestroyImmediate(networkTransform as Component);
            }
        }

        private void UpdateWeaponSystemReference(GameObject playerInstance, Transform weaponHolder)
        {
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null && weaponHolder != null)
            {
                SerializedObject so = new SerializedObject(weaponSystem);
                var weaponHolderProp = so.FindProperty("weaponHolder");
                if (weaponHolderProp != null)
                {
                    weaponHolderProp.objectReferenceValue = weaponHolder;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(weaponSystem);
                    Debug.Log("✅ Updated WeaponSystem.weaponHolder reference");
                }
            }
        }

        private Transform FindBoneInHierarchy(Transform root, string boneName)
        {
            // First try exact match
            Transform found = root.Find(boneName);
            if (found != null) return found;

            // Then search recursively
            foreach (Transform child in root)
            {
                if (child.name.Contains(boneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform result = FindBoneInHierarchy(child, boneName);
                if (result != null) return result;
            }

            return null;
        }

        private void ListBones(Transform root, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}- {root.name}");

            foreach (Transform child in root)
            {
                ListBones(child, depth + 1, maxDepth);
            }
        }

        private void FindHandBone()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                EditorUtility.DisplayDialog("Error", "No Animator found on Player!", "OK");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }

            Transform handBone = FindBoneInHierarchy(characterAnimator.transform, handBoneName);
            PrefabUtility.UnloadPrefabContents(playerInstance);

            if (handBone != null)
            {
                EditorUtility.DisplayDialog("Hand Bone Found", 
                    $"Hand bone found:\n{GetPath(handBone)}\n\n" +
                    $"Position: {handBone.localPosition}\n" +
                    $"Rotation: {handBone.localRotation.eulerAngles}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Hand Bone Not Found", 
                    $"Hand bone '{handBoneName}' not found!\n\n" +
                    "Common names:\n" +
                    "- RightHand\n" +
                    "- Hand_R\n" +
                    "- R_Hand\n" +
                    "- RightHandIndex1\n\n" +
                    "Check Console for available bones.",
                    "OK");
                
                // List bones
                Debug.Log("\n📋 Available bones in character:");
                ListBones(characterAnimator.transform, 0, 50);
            }
        }

        private void PreviewWeaponPosition()
        {
            if (weaponPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Weapon Prefab first!", "OK");
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
            
            Animator characterAnimator = playerInstance.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                EditorUtility.DisplayDialog("Error", "No Animator found!", "OK");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }

            Transform handBone = FindBoneInHierarchy(characterAnimator.transform, handBoneName);
            if (handBone == null)
            {
                EditorUtility.DisplayDialog("Error", $"Hand bone '{handBoneName}' not found!", "OK");
                PrefabUtility.UnloadPrefabContents(playerInstance);
                return;
            }

            // Create preview weapon
            GameObject previewWeapon = InstantiateWeapon(weaponPrefab);
            if (previewWeapon != null)
            {
                previewWeapon.name = "PreviewWeapon";
                previewWeapon.transform.SetParent(handBone);
                previewWeapon.transform.localPosition = weaponPosition;
                previewWeapon.transform.localRotation = Quaternion.Euler(weaponRotation);
                previewWeapon.transform.localScale = weaponScale;

                Debug.Log($"✅ Preview weapon created at {handBoneName}");
                Debug.Log($"   Position: {weaponPosition}");
                Debug.Log($"   Rotation: {weaponRotation}");
                Debug.Log($"   Scale: {weaponScale}");
                
                EditorUtility.DisplayDialog("Preview Created", 
                    $"Preview weapon created!\n\n" +
                    $"Check the Player prefab in the scene.\n" +
                    $"Adjust position/rotation in this tool,\n" +
                    $"then click 'Attach Weapon to Hand'.",
                    "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}

