using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON MUZZLE POSITION FINDER: Silahın ucundan ateş çıkması için doğru pozisyonu bulur
    /// </summary>
    public class WeaponMuzzlePositionFinder : EditorWindow
    {
        private GameObject weaponPrefab;
        private Transform weaponHolder;
        private Vector3 foundMuzzlePosition = Vector3.zero;

        [MenuItem("Tools/Tactical Combat/Find Weapon Muzzle Position")]
        public static void ShowWindow()
        {
            GetWindow<WeaponMuzzlePositionFinder>("Weapon Muzzle Position Finder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Muzzle Position Finder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu tool silahın ucundan ateş çıkması için doğru pozisyonu bulur:", EditorStyles.wordWrappedLabel);
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

            // Weapon Holder
            GUILayout.Label("Weapon Holder (Optional):", EditorStyles.boldLabel);
            weaponHolder = (Transform)EditorGUILayout.ObjectField(
                weaponHolder,
                typeof(Transform),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(weaponPrefab == null);
            if (GUILayout.Button("Find Muzzle Position", GUILayout.Height(30)))
            {
                FindMuzzlePosition();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("Auto Fix Player Prefab", GUILayout.Height(30)))
            {
                AutoFixPlayerPrefab();
            }

            GUILayout.Space(10);

            if (foundMuzzlePosition != Vector3.zero)
            {
                GUILayout.Label("Found Muzzle Position:", EditorStyles.boldLabel);
                EditorGUILayout.Vector3Field("Local Position", foundMuzzlePosition);
            }
        }

        private void FindMuzzlePosition()
        {
            if (weaponPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Weapon Prefab!", "OK");
                return;
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 FINDING MUZZLE POSITION: {weaponPrefab.name}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Try multiple methods to find muzzle position
            Vector3 muzzlePos = Vector3.zero;
            bool found = false;

            // Method 1: Look for "Muzzle" or "FirePoint" child
            Transform muzzleTransform = FindChildByName(weaponPrefab.transform, new[] { "Muzzle", "MuzzleFlash", "FirePoint", "Barrel", "GunTip", "WeaponTip" });
            if (muzzleTransform != null)
            {
                muzzlePos = muzzleTransform.localPosition;
                found = true;
                Debug.Log($"✅ Found muzzle by name: {muzzleTransform.name} at {muzzlePos}");
            }

            // Method 2: Look for the forward-most point (highest Z in local space)
            if (!found)
            {
                Renderer[] renderers = weaponPrefab.GetComponentsInChildren<Renderer>();
                float maxZ = float.MinValue;
                Vector3 forwardPoint = Vector3.zero;

                foreach (Renderer renderer in renderers)
                {
                    Bounds bounds = renderer.bounds;
                    Vector3 localCenter = weaponPrefab.transform.InverseTransformPoint(bounds.center);
                    Vector3 localMax = localCenter + new Vector3(0, 0, bounds.extents.z);

                    if (localMax.z > maxZ)
                    {
                        maxZ = localMax.z;
                        forwardPoint = localMax;
                    }
                }

                if (maxZ > float.MinValue)
                {
                    muzzlePos = forwardPoint;
                    found = true;
                    Debug.Log($"✅ Found muzzle by forward-most point: {muzzlePos}");
                }
            }

            // Method 3: Use weapon's forward direction (default offset)
            if (!found)
            {
                // Estimate: weapon tip is usually at the end of the weapon model
                // For most weapons, this is around (0, 0, 0.5-1.0) in local space
                muzzlePos = new Vector3(0f, 0f, 0.8f);
                Debug.LogWarning($"⚠️ Could not find muzzle position, using estimated: {muzzlePos}");
            }

            foundMuzzlePosition = muzzlePos;

            Debug.Log($"\n✅ Muzzle Position: {muzzlePos}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            EditorUtility.DisplayDialog("Success", 
                $"Muzzle position found!\n\n" +
                $"Local Position: {muzzlePos}\n\n" +
                $"Use 'Auto Fix Player Prefab' to apply this to your weapon system.",
                "OK");
        }

        private Transform FindChildByName(Transform parent, string[] names)
        {
            foreach (Transform child in parent)
            {
                foreach (string name in names)
                {
                    if (child.name.Contains(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }

                // Recursive search
                Transform found = FindChildByName(child, names);
                if (found != null) return found;
            }
            return null;
        }

        private void AutoFixPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found at: " + prefabPath, "OK");
                return;
            }

            if (foundMuzzlePosition == Vector3.zero)
            {
                EditorUtility.DisplayDialog("Error", "Please find muzzle position first!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔧 AUTO FIXING PLAYER PREFAB");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // Find WeaponSystem
            WeaponSystem weaponSystem = playerInstance.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                SerializedObject weaponSo = new SerializedObject(weaponSystem);
                
                // Find WeaponHolder
                Transform weaponHolderTransform = null;
                var weaponHolderProp = weaponSo.FindProperty("weaponHolder");
                if (weaponHolderProp != null)
                {
                    weaponHolderTransform = weaponHolderProp.objectReferenceValue as Transform;
                }

                if (weaponHolderTransform == null)
                {
                    weaponHolderTransform = playerInstance.transform.Find("WeaponHolder");
                    if (weaponHolderTransform == null)
                    {
                        var playerVisual = playerInstance.transform.Find("PlayerVisual");
                        if (playerVisual != null)
                        {
                            weaponHolderTransform = playerVisual.Find("WeaponHolder");
                        }
                    }
                }

                // Find or create MuzzlePoint child
                if (weaponHolderTransform != null)
                {
                    Transform muzzlePoint = weaponHolderTransform.Find("MuzzlePoint");
                    if (muzzlePoint == null)
                    {
                        GameObject muzzleGO = new GameObject("MuzzlePoint");
                        muzzleGO.transform.SetParent(weaponHolderTransform);
                        muzzlePoint = muzzleGO.transform;
                        Debug.Log("✅ Created MuzzlePoint GameObject");
                        modified = true;
                    }

                    // Set muzzle position
                    muzzlePoint.localPosition = foundMuzzlePosition;
                    muzzlePoint.localRotation = Quaternion.identity;
                    EditorUtility.SetDirty(muzzlePoint);
                    Debug.Log($"✅ MuzzlePoint position set to: {foundMuzzlePosition}");
                    modified = true;

                    // Update WeaponVFXController to use MuzzlePoint
                    WeaponVFXController vfxController = playerInstance.GetComponent<WeaponVFXController>();
                    if (vfxController != null)
                    {
                        // Note: WeaponVFXController uses weaponHolder.position, but we can add a muzzlePoint reference
                        Debug.Log("✅ WeaponVFXController found - it will use WeaponHolder position");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ WeaponHolder not found!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ WeaponSystem not found on Player prefab!");
            }

            // Also update WeaponSystem to use correct fire position
            // The WeaponSystem already uses weaponHolder.position, so we just need to ensure
            // the WeaponHolder has the correct child (MuzzlePoint) positioned correctly

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(playerInstance, prefabPath);
                AssetDatabase.Refresh();
                Debug.Log($"\n✅ Player prefab updated with muzzle position!");
                Debug.Log($"═══════════════════════════════════════════════════════\n");
                EditorUtility.DisplayDialog("Success", "Player prefab updated with muzzle position!", "OK");
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }
    }
}

