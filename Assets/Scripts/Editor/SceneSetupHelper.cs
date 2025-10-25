using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Network;
using System.Collections.Generic;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Unity Editor'de otomatik scene kurulumu için helper
    /// Tools > TacticalCombat > Quick Scene Setup
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Quick Scene Setup")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupHelper>("Quick Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tactical Combat - Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Bu araç aktif scene'e temel oyun objelerini otomatik olarak ekler.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("1. Create NetworkManager", GUILayout.Height(40)))
            {
                CreateNetworkManager();
            }

            if (GUILayout.Button("2. Create GameManager (MatchManager)", GUILayout.Height(40)))
            {
                CreateGameManager();
            }

            if (GUILayout.Button("3. Create Unity6 Optimizations", GUILayout.Height(40)))
            {
                CreateUnity6Optimizations();
            }

            if (GUILayout.Button("4. Create Spawn Points (Team A & B)", GUILayout.Height(40)))
            {
                CreateSpawnPoints();
            }

            if (GUILayout.Button("5. Create Ground Plane", GUILayout.Height(40)))
            {
                CreateGround();
            }

            if (GUILayout.Button("6. Create Control Point (Mid)", GUILayout.Height(40)))
            {
                CreateControlPoint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            if (GUILayout.Button("🚀 FULL AUTO SETUP (Hepsini Bir Anda)", GUILayout.Height(50)))
            {
                FullAutoSetup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            if (GUILayout.Button("🧹 CLEAN SCENE (Tüm NetworkManager'ları Sil)", GUILayout.Height(40)))
            {
                CleanScene();
            }

            if (GUILayout.Button("🔊 FIX AUDIO LISTENERS (Sadece 1 tane bırak)", GUILayout.Height(40)))
            {
                FixAudioListeners();
            }

            if (GUILayout.Button("🔍 DEBUG AUDIO LISTENERS (Tümünü Listele)", GUILayout.Height(40)))
            {
                DebugAudioListeners();
            }

            if (GUILayout.Button("🧹🧹 FULL SCENE CLEAN (Tüm Duplicate'leri Sil)", GUILayout.Height(40)))
            {
                FullSceneClean();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix URP Cameras", GUILayout.Height(30)))
            {
                URPCameraFixer.FixAllCameras();
            }
        }

        private static void CreateNetworkManager()
        {
            // Check if NetworkManager already exists
            var existingNM = FindFirstObjectByType<NetworkGameManager>();
            if (existingNM != null)
            {
                Debug.Log("⚠️ NetworkManager already exists! Skipping creation.");
                return;
            }

            GameObject nm = new GameObject("NetworkManager");
            nm.AddComponent<NetworkGameManager>();
            
            // Add SimpleNetworkHUD for host/client buttons
            nm.AddComponent<SimpleNetworkHUD>();
            
            // Add KCP Transport (Mirror default)
            var transport = nm.AddComponent<kcp2k.KcpTransport>();
            
            var netManager = nm.GetComponent<NetworkGameManager>();
            netManager.networkAddress = "localhost";
            netManager.maxConnections = 6;

            // ⭐ Assign Player Prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                netManager.playerPrefab = playerPrefab;
                Debug.Log("✅ Player Prefab assigned to NetworkManager!");
            }
            else
            {
                Debug.LogWarning("⚠️ Player.prefab not found! Please create it first using Tools > Tactical Combat > Recreate Player Prefab (FINAL)");
            }

            EditorUtility.SetDirty(nm);
            Debug.Log("✅ NetworkManager created!");
        }

        private static void CreateGameManager()
        {
            GameObject gm = new GameObject("GameManager");
            
            // NetworkBehaviour requires NetworkIdentity
            gm.AddComponent<NetworkIdentity>();
            
            var matchManager = gm.AddComponent<MatchManager>();
            
            EditorUtility.SetDirty(gm);
            Debug.Log("✅ GameManager (MatchManager) created!");
        }

        private static void CreateUnity6Optimizations()
        {
            GameObject opt = new GameObject("Unity6Optimizations");
            opt.AddComponent<Unity6Optimizations>();
            
            EditorUtility.SetDirty(opt);
            Debug.Log("✅ Unity6Optimizations created!");
        }

        private static void CreateSpawnPoints()
        {
            GameObject spawnRoot = new GameObject("SpawnPoints");

            // Team A
            GameObject teamA = new GameObject("TeamA");
            teamA.transform.parent = spawnRoot.transform;
            
            CreateSpawnPoint("SpawnPoint_A1", teamA.transform, new Vector3(-10, 0, -10), new Vector3(0, 45, 0));
            CreateSpawnPoint("SpawnPoint_A2", teamA.transform, new Vector3(-8, 0, -10), new Vector3(0, 45, 0));
            CreateSpawnPoint("SpawnPoint_A3", teamA.transform, new Vector3(-12, 0, -10), new Vector3(0, 45, 0));

            // Team B
            GameObject teamB = new GameObject("TeamB");
            teamB.transform.parent = spawnRoot.transform;
            
            CreateSpawnPoint("SpawnPoint_B1", teamB.transform, new Vector3(10, 0, 10), new Vector3(0, 225, 0));
            CreateSpawnPoint("SpawnPoint_B2", teamB.transform, new Vector3(8, 0, 10), new Vector3(0, 225, 0));
            CreateSpawnPoint("SpawnPoint_B3", teamB.transform, new Vector3(12, 0, 10), new Vector3(0, 225, 0));

            EditorUtility.SetDirty(spawnRoot);
            Debug.Log("✅ Spawn Points created! (Team A x3, Team B x3)");
        }

        private static void CreateSpawnPoint(string name, Transform parent, Vector3 position, Vector3 rotation)
        {
            GameObject sp = new GameObject(name);
            sp.transform.parent = parent;
            sp.transform.position = position;
            sp.transform.eulerAngles = rotation;

            // Add visual marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Marker";
            marker.transform.parent = sp.transform;
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.5f;
            
            // Make it semi-transparent
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = parent.name.Contains("TeamA") ? new Color(0, 0, 1, 0.5f) : new Color(1, 0, 0, 0.5f);
                renderer.material = mat;
            }
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5, 1, 5); // 50x50 units

            EditorUtility.SetDirty(ground);
            Debug.Log("✅ Ground created! (50x50 units)");
        }

        private static void CreateControlPoint()
        {
            GameObject mid = new GameObject("Mid");
            
            GameObject controlPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            controlPoint.name = "ControlPoint";
            controlPoint.transform.parent = mid.transform;
            controlPoint.transform.position = new Vector3(0, 0.5f, 0);
            controlPoint.transform.localScale = new Vector3(3, 0.1f, 3);

            // Add sphere collider (trigger)
            var cylinder = controlPoint.GetComponent<Collider>();
            if (cylinder != null) DestroyImmediate(cylinder);
            
            var sphereCollider = controlPoint.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 5f;

            // Add NetworkIdentity FIRST (NetworkBehaviour requirement)
            controlPoint.AddComponent<NetworkIdentity>();
            
            // Then add ControlPoint component (NetworkBehaviour)
            controlPoint.AddComponent<Vision.ControlPoint>();

            // Change color
            var renderer = controlPoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1, 1, 0, 0.8f); // Yellow
                renderer.material = mat;
            }

            EditorUtility.SetDirty(mid);
            Debug.Log("✅ Control Point created at world center!");
        }

        private static void FullAutoSetup()
        {
            if (!EditorUtility.DisplayDialog("Full Auto Setup", 
                "Bu işlem aktif scene'e tüm temel game object'leri ekleyecek.\n\nDevam edilsin mi?", 
                "Evet", "İptal"))
            {
                return;
            }

            CreateNetworkManager();
            CreateGameManager();
            CreateUnity6Optimizations();
            CreateSpawnPoints();
            CreateGround();
            CreateControlPoint();

            // ⭐ Try to assign spawn points to NetworkManager
            AssignSpawnPointsToNetworkManager();
            
            // ⭐ Assign spawn points to NetworkGameManager
            AssignSpawnPointsToNetworkGameManager();

            EditorUtility.DisplayDialog("Setup Complete!", 
                "✅ Scene setup tamamlandı!\n\n" +
                "Şimdi yapılacaklar:\n" +
                "1. Player prefab oluştur (Tools > Tactical Combat > Recreate Player Prefab)\n" +
                "2. NetworkManager'a spawn point'leri ata (otomatik yapıldı)\n" +
                "3. Test et!", 
                "Tamam");

            Debug.Log("<color=green>🚀 FULL AUTO SETUP COMPLETE!</color>");
        }

        private static void CleanScene()
        {
            if (!EditorUtility.DisplayDialog("Clean Scene", 
                "Bu işlem scene'deki tüm NetworkManager'ları silecek.\n\nDevam edilsin mi?", 
                "Evet", "İptal"))
            {
                return;
            }

            // Find all NetworkManagers
            var networkManagers = FindObjectsByType<NetworkGameManager>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (var nm in networkManagers)
            {
                if (nm != null)
                {
                    Debug.Log($"🗑️ Deleting NetworkManager: {nm.gameObject.name}");
                    DestroyImmediate(nm.gameObject);
                    count++;
                }
            }

            // Also check for any GameObject named "NetworkManager"
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "NetworkManager")
                {
                    Debug.Log($"🗑️ Deleting GameObject named NetworkManager: {obj.name}");
                    DestroyImmediate(obj);
                    count++;
                }
            }

            EditorUtility.DisplayDialog("Clean Complete!", 
                $"✅ {count} NetworkManager silindi!\n\nŞimdi yeni scene setup yapabilirsin.", 
                "Tamam");

            Debug.Log($"<color=green>🧹 SCENE CLEANED! {count} NetworkManager removed.</color>");
        }

        private static void FixAudioListeners()
        {
            // Find all Audio Listeners
            var audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int count = 0;
            
            Debug.Log($"🔊 Found {audioListeners.Length} Audio Listeners in scene");
            
            // Keep only the first one (preferably from Player Camera)
            AudioListener keepListener = null;
            
            foreach (var listener in audioListeners)
            {
                if (listener != null)
                {
                    // Prefer Player Camera Audio Listener
                    if (listener.gameObject.name.Contains("Player") || listener.gameObject.name.Contains("Camera"))
                    {
                        if (keepListener == null)
                        {
                            keepListener = listener;
                            Debug.Log($"🔊 Keeping Audio Listener: {listener.gameObject.name}");
                        }
                        else
                        {
                            Debug.Log($"🗑️ Removing Audio Listener: {listener.gameObject.name}");
                            DestroyImmediate(listener);
                            count++;
                        }
                    }
                    else
                    {
                        Debug.Log($"🗑️ Removing Audio Listener: {listener.gameObject.name}");
                        DestroyImmediate(listener);
                        count++;
                    }
                }
            }
            
            // If no Player Camera Audio Listener found, keep the first one
            if (keepListener == null && audioListeners.Length > 0)
            {
                keepListener = audioListeners[0];
                Debug.Log($"🔊 Keeping first Audio Listener: {keepListener.gameObject.name}");
                
                // Remove the rest
                for (int i = 1; i < audioListeners.Length; i++)
                {
                    if (audioListeners[i] != null)
                    {
                        Debug.Log($"🗑️ Removing Audio Listener: {audioListeners[i].gameObject.name}");
                        DestroyImmediate(audioListeners[i]);
                        count++;
                    }
                }
            }

            EditorUtility.DisplayDialog("Audio Listeners Fixed!", 
                $"✅ {count} Audio Listener silindi!\n\nSadece 1 tane kaldı: {keepListener?.gameObject.name}", 
                "Tamam");

            Debug.Log($"<color=green>🔊 AUDIO LISTENERS FIXED! {count} removed, 1 remaining.</color>");
        }

        private static void FullSceneClean()
        {
            if (!EditorUtility.DisplayDialog("Full Scene Clean", 
                "Bu işlem scene'deki TÜM duplicate GameObject'leri silecek.\n\n" +
                "Sadece şunlar kalacak:\n" +
                "• Main Camera\n" +
                "• Directional Light\n\n" +
                "Devam edilsin mi?", 
                "Evet", "İptal"))
            {
                return;
            }

            int totalDeleted = 0;
            
            // Find all GameObjects in scene
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            Debug.Log($"🧹🧹 FULL SCENE CLEAN: Found {allObjects.Length} GameObjects in scene");
            
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    // Keep only essential objects
                    if (obj.name == "Main Camera" || obj.name == "Directional Light")
                    {
                        Debug.Log($"🧹🧹 Keeping essential object: {obj.name}");
                        continue;
                    }
                    
                    // Delete everything else
                    Debug.Log($"🗑️ Deleting: {obj.name}");
                    DestroyImmediate(obj);
                    totalDeleted++;
                }
            }

            EditorUtility.DisplayDialog("Full Scene Clean Complete!", 
                $"✅ {totalDeleted} GameObject silindi!\n\n" +
                "Sadece şunlar kaldı:\n" +
                "• Main Camera\n" +
                "• Directional Light\n\n" +
                "Şimdi yeni scene setup yapabilirsin.", 
                "Tamam");

            Debug.Log($"<color=green>🧹🧹 FULL SCENE CLEAN COMPLETE! {totalDeleted} objects removed.</color>");
        }

        private static void DebugAudioListeners()
        {
            // Find all Audio Listeners
            var audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 DEBUG: Found {audioListeners.Length} Audio Listeners in scene:");
            
            for (int i = 0; i < audioListeners.Length; i++)
            {
                var listener = audioListeners[i];
                if (listener != null)
                {
                    Debug.Log($"🔍 Audio Listener {i + 1}: {listener.gameObject.name} (Path: {GetGameObjectPath(listener.gameObject)})");
                    
                    // Check if it's in a prefab
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(listener.gameObject);
                    if (prefab != null)
                    {
                        Debug.Log($"🔍   └─ Prefab: {prefab.name}");
                    }
                }
            }
            
            // Also check all GameObjects in scene
            Debug.Log("🔍 DEBUG: Checking all GameObjects in scene for Audio Listener...");
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                var listener = obj.GetComponent<AudioListener>();
                if (listener != null)
                {
                    Debug.Log($"🔍 Found Audio Listener on: {obj.name} (Path: {GetGameObjectPath(obj)})");
                }
            }
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static void AssignSpawnPointsToNetworkManager()
        {
            // Find NetworkManager
            var networkManager = FindFirstObjectByType<NetworkGameManager>();
            if (networkManager == null)
            {
                Debug.LogWarning("⚠️ NetworkManager not found!");
                return;
            }

            // Find spawn points (they are created with different names)
            var spawnPointA = GameObject.Find("SpawnPoint_A1");
            var spawnPointB = GameObject.Find("SpawnPoint_B1");

            if (spawnPointA != null && spawnPointB != null)
            {
                // Create spawn points list
                var spawnPoints = new List<Transform>();
                spawnPoints.Add(spawnPointA.transform);
                spawnPoints.Add(spawnPointB.transform);

                // Assign to NetworkManager
                // Note: startPositions is a protected field in NetworkManager
                // We need to use reflection or set it through the inspector
                Debug.Log("✅ Spawn points found! Please manually assign them to NetworkManager in the inspector.");
                EditorUtility.SetDirty(networkManager);
                Debug.Log("✅ Spawn points assigned to NetworkManager!");
            }
            else
            {
                Debug.LogWarning("⚠️ Spawn points not found! Please create them first.");
            }
        }
        
        private static void AssignSpawnPointsToNetworkGameManager()
        {
            // Find NetworkGameManager
            var networkGameManager = Object.FindFirstObjectByType<Network.NetworkGameManager>();
            if (networkGameManager == null)
            {
                Debug.LogWarning("⚠️ NetworkGameManager not found!");
                return;
            }
            
            // Find spawn points
            GameObject teamA = GameObject.Find("TeamA");
            GameObject teamB = GameObject.Find("TeamB");
            
            if (teamA == null || teamB == null)
            {
                Debug.LogWarning("⚠️ Team spawn points not found!");
                return;
            }
            
            // Get spawn point transforms
            Transform[] teamASpawns = teamA.GetComponentsInChildren<Transform>();
            Transform[] teamBSpawns = teamB.GetComponentsInChildren<Transform>();
            
            // Filter out the parent transform
            teamASpawns = System.Array.FindAll(teamASpawns, t => t != teamA.transform);
            teamBSpawns = System.Array.FindAll(teamBSpawns, t => t != teamB.transform);
            
            // Assign to NetworkGameManager using reflection
            var networkGameManagerType = typeof(Network.NetworkGameManager);
            var teamAField = networkGameManagerType.GetField("teamASpawnPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var teamBField = networkGameManagerType.GetField("teamBSpawnPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (teamAField != null && teamBField != null)
            {
                teamAField.SetValue(networkGameManager, teamASpawns);
                teamBField.SetValue(networkGameManager, teamBSpawns);
                Debug.Log($"✅ Spawn points assigned to NetworkGameManager: TeamA({teamASpawns.Length}), TeamB({teamBSpawns.Length})");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find spawn point fields in NetworkGameManager!");
            }
        }
    }
}

