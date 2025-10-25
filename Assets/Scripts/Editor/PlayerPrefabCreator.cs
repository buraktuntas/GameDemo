using UnityEngine;
using UnityEditor;
using Mirror;
using TacticalCombat.Player;
using TacticalCombat.Combat;
using TacticalCombat.Building;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Player prefab'ını otomatik oluşturur
    /// Tools > TacticalCombat > Create Player Prefab
    /// </summary>
    public class PlayerPrefabCreator : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            // Create Prefabs folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Check if Player prefab already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab") != null)
            {
                if (!EditorUtility.DisplayDialog("Player Prefab Exists",
                    "Player prefab zaten var. Üzerine yazmak istediğine emin misin?",
                    "Evet, Yeniden Oluştur", "İptal"))
                {
                    return;
                }
            }

            // Create Player GameObject
            GameObject player = new GameObject("Player");

            // Add visual (Capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.parent = player.transform;
            visual.transform.localPosition = new Vector3(0, 1, 0);
            visual.transform.localScale = Vector3.one;

            // Color the capsule (temporary - team colors will be set at runtime)
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.5f, 0.5f, 1f, 1f); // Light blue
                renderer.material = mat;
            }

            // Remove the capsule collider from visual (we'll use CharacterController)
            var capsuleCollider = visual.GetComponent<Collider>();
            if (capsuleCollider != null)
            {
                Object.DestroyImmediate(capsuleCollider);
            }

            // Add CharacterController
            var charController = player.AddComponent<CharacterController>();
            charController.height = 2f;
            charController.radius = 0.5f;
            charController.center = new Vector3(0, 1, 0);

            // Add Network Components (ORDER MATTERS!)
            var networkIdentity = player.AddComponent<Mirror.NetworkIdentity>();
            
            var networkTransform = player.AddComponent<Mirror.NetworkTransformReliable>();
            
            // Add Game Components
            player.AddComponent<FPSController>();
            player.AddComponent<PlayerController>();
            
            var health = player.AddComponent<Health>();
            
            player.AddComponent<WeaponController>();
            player.AddComponent<AbilityController>();
            player.AddComponent<BuildPlacementController>();
            
            // Add PlayerVisuals for team colors
            player.AddComponent<PlayerVisuals>();

            // Create PlayerCamera as child
            GameObject playerCamera = new GameObject("PlayerCamera");
            playerCamera.transform.parent = player.transform;
            playerCamera.transform.localPosition = new Vector3(0, 1.6f, -3); // Behind and above
            playerCamera.transform.localRotation = Quaternion.Euler(15, 0, 0); // Look down slightly

            var camera = playerCamera.AddComponent<Camera>();
            camera.enabled = false; // Will be enabled for local player only

            // Add URP Camera Data
            playerCamera.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            // FPSController will handle camera automatically
            // Assign camera to FPSController
            var fpsController = player.GetComponent<FPSController>();
            if (fpsController != null)
            {
                fpsController.playerCamera = camera;
            }

            // Add Audio Listener
            playerCamera.AddComponent<AudioListener>();

            // Save as Prefab
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, prefabPath);

            // Clean up scene
            Object.DestroyImmediate(player);

            // Select the prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"✅ Player prefab created at: {prefabPath}");
            
            EditorUtility.DisplayDialog("Player Prefab Created!",
                $"Player prefab başarıyla oluşturuldu!\n\nKonum: {prefabPath}\n\n" +
                "Şimdi yapılacaklar:\n" +
                "1. Hierarchy'de NetworkManager seç\n" +
                "2. NetworkGameManager component'inde:\n" +
                "   - Player Prefab: Player prefab'ını sürükle\n" +
                "   - Team A/B Spawn Points: Spawn point'leri ekle\n" +
                "3. Play'e bas ve test et!",
                "Tamam");
        }

        [MenuItem("Tools/TacticalCombat/Setup NetworkManager")]
        public static void SetupNetworkManager()
        {
            // Find NetworkManager in scene
            var networkManager = Object.FindFirstObjectByType<Network.NetworkGameManager>();
            
            if (networkManager == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Scene'de NetworkManager bulunamadı!\n\nÖnce 'Quick Scene Setup' çalıştır.",
                    "Tamam");
                return;
            }

            // Load Player prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Player prefab bulunamadı!\n\nÖnce 'Create Player Prefab' çalıştır.",
                    "Tamam");
                return;
            }

            // Set player prefab
            var networkManagerBase = networkManager.GetComponent<NetworkManager>();
            networkManagerBase.playerPrefab = playerPrefab;

            // Find spawn points
            Transform[] teamASpawns = FindSpawnPoints("TeamA");
            Transform[] teamBSpawns = FindSpawnPoints("TeamB");

            if (teamASpawns.Length == 0 || teamBSpawns.Length == 0)
            {
                EditorUtility.DisplayDialog("Warning",
                    "Spawn point'ler bulunamadı!\n\nManuel olarak eklemelisin.",
                    "Tamam");
            }

            EditorUtility.SetDirty(networkManager);
            
            Debug.Log("✅ NetworkManager setup complete!");
            Debug.Log($"   Player Prefab: {playerPrefab.name}");
            Debug.Log($"   Team A Spawns: {teamASpawns.Length}");
            Debug.Log($"   Team B Spawns: {teamBSpawns.Length}");

            EditorUtility.DisplayDialog("NetworkManager Setup",
                $"NetworkManager yapılandırıldı!\n\n" +
                $"• Player Prefab: {playerPrefab.name}\n" +
                $"• Team A Spawns: {teamASpawns.Length}\n" +
                $"• Team B Spawns: {teamBSpawns.Length}\n\n" +
                "Spawn point'leri manuel olarak kontrol et:\n" +
                "Hierarchy > NetworkManager > Inspector\n" +
                "NetworkGameManager component'inde Team A/B Spawn Points array'lerini doldur.",
                "Tamam");
        }

        private static Transform[] FindSpawnPoints(string teamName)
        {
            GameObject spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot == null) return new Transform[0];

            Transform teamParent = spawnRoot.transform.Find(teamName);
            if (teamParent == null) return new Transform[0];

            Transform[] spawns = new Transform[teamParent.childCount];
            for (int i = 0; i < teamParent.childCount; i++)
            {
                spawns[i] = teamParent.GetChild(i);
            }

            return spawns;
        }
    }
}

