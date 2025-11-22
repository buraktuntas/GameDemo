using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ DIAGNOSTIC: WeaponSystem ve CurrentWeapon görünürlüğünü kontrol eder
    /// </summary>
    public class WeaponVisibilityChecker : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Check Weapon Visibility")]
        public static void ShowWindow()
        {
            GetWindow<WeaponVisibilityChecker>("Weapon Visibility Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Visibility Checker", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Check Player Prefab", GUILayout.Height(30)))
            {
                CheckPlayerPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Check Scene Players", GUILayout.Height(30)))
            {
                CheckScenePlayers();
            }
        }

        private static void CheckPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found!");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            CheckWeaponSetup(playerInstance, "Player Prefab");
            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private static void CheckScenePlayers()
        {
            var players = FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            Debug.Log($"🔍 Found {players.Length} player(s) in scene");

            foreach (var playerController in players)
            {
                CheckWeaponSetup(playerController.gameObject, $"Scene Player: {playerController.gameObject.name}");
            }
        }

        private static void CheckWeaponSetup(GameObject player, string context)
        {
            Debug.Log($"\n═══════════════════════════════════════════════════════");
            Debug.Log($"🔍 Checking: {context}");
            Debug.Log($"═══════════════════════════════════════════════════════\n");

            // 1. Check WeaponSystem on Player
            var weaponSystem = player.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogError($"❌ [{context}] WeaponSystem NOT FOUND on Player!");
            }
            else
            {
                Debug.Log($"✅ [{context}] WeaponSystem found on Player");
                
                // Check WeaponVFXController
                var vfxController = weaponSystem.GetComponent<WeaponVFXController>();
                if (vfxController == null)
                {
                    Debug.LogError($"❌ [{context}] WeaponVFXController NOT FOUND on Player!");
                }
                else
                {
                    Debug.Log($"✅ [{context}] WeaponVFXController found on Player");
                    
                    // Check prefab references
                    SerializedObject vfxSo = new SerializedObject(vfxController);
                    var muzzleFlashProp = vfxSo.FindProperty("muzzleFlashPrefab");
                    if (muzzleFlashProp != null && muzzleFlashProp.objectReferenceValue == null)
                    {
                        Debug.LogWarning($"⚠️ [{context}] WeaponVFXController.muzzleFlashPrefab is NULL!");
                    }
                    else
                    {
                        Debug.Log($"✅ [{context}] WeaponVFXController.muzzleFlashPrefab assigned");
                    }
                }
                
                // Check WeaponAudioController
                var audioController = weaponSystem.GetComponent<WeaponAudioController>();
                if (audioController == null)
                {
                    Debug.LogError($"❌ [{context}] WeaponAudioController NOT FOUND on Player!");
                }
                else
                {
                    Debug.Log($"✅ [{context}] WeaponAudioController found on Player");
                }
            }

            // 2. Find WeaponHolder
            Transform weaponHolder = player.transform.Find("WeaponHolder");
            if (weaponHolder == null)
            {
                var playerVisual = player.transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    weaponHolder = playerVisual.Find("WeaponHolder");
                }
            }

            if (weaponHolder == null)
            {
                Debug.LogError($"❌ [{context}] WeaponHolder NOT FOUND!");
                return;
            }

            Debug.Log($"✅ [{context}] WeaponHolder found: {weaponHolder.name}");
            Debug.Log($"   Active: {weaponHolder.gameObject.activeSelf}");
            Debug.Log($"   Position: {weaponHolder.localPosition}");

            // 3. Find CurrentWeapon
            Transform currentWeapon = weaponHolder.Find("CurrentWeapon");
            if (currentWeapon == null)
            {
                Debug.LogError($"❌ [{context}] CurrentWeapon NOT FOUND in WeaponHolder!");
                return;
            }

            Debug.Log($"✅ [{context}] CurrentWeapon found: {currentWeapon.name}");
            Debug.Log($"   Active: {currentWeapon.gameObject.activeSelf}");
            Debug.Log($"   Position: {currentWeapon.localPosition}");

            // 4. Check CurrentWeapon components
            var components = currentWeapon.GetComponents<Component>();
            Debug.Log($"   Components ({components.Length}):");
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    Debug.Log($"     - {comp.GetType().Name} (enabled: {(comp is Behaviour ? ((Behaviour)comp).enabled : "N/A")})");
                }
            }

            // 5. Check CurrentWeapon renderers
            var renderers = currentWeapon.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"   Renderers ({renderers.Length}):");
            if (renderers.Length == 0)
            {
                Debug.LogError($"❌ [{context}] CurrentWeapon has NO RENDERERS! This is why it's not visible!");
            }
            else
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Debug.Log($"     - {renderer.GetType().Name} on {renderer.gameObject.name}");
                        Debug.Log($"       Enabled: {renderer.enabled}");
                        Debug.Log($"       Active: {renderer.gameObject.activeSelf}");
                        
                        if (renderer is MeshRenderer)
                        {
                            var meshRenderer = renderer as MeshRenderer;
                            var meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                Debug.Log($"       Mesh: {meshFilter.sharedMesh.name}");
                            }
                            else
                            {
                                Debug.LogWarning($"       ⚠️ MeshFilter has no mesh!");
                            }
                        }
                    }
                }
            }

            // 6. Check if CurrentWeapon has WeaponSystem (should NOT have it)
            var weaponSystemOnWeapon = currentWeapon.GetComponent<WeaponSystem>();
            if (weaponSystemOnWeapon != null)
            {
                Debug.LogError($"❌ [{context}] CurrentWeapon has WeaponSystem component! It should be on Player!");
            }

            // 7. Check if CurrentWeapon has WeaponVFXController (should NOT have it)
            var vfxOnWeapon = currentWeapon.GetComponent<WeaponVFXController>();
            if (vfxOnWeapon != null)
            {
                Debug.LogError($"❌ [{context}] CurrentWeapon has WeaponVFXController component! It should be on Player!");
            }

            Debug.Log($"\n═══════════════════════════════════════════════════════\n");
        }
    }
}

