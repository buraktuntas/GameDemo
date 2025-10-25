using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Network;

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

            if (GUILayout.Button("Fix URP Cameras", GUILayout.Height(30)))
            {
                URPCameraFixer.FixAllCameras();
            }
        }

        private static void CreateNetworkManager()
        {
            GameObject nm = new GameObject("NetworkManager");
            nm.AddComponent<NetworkGameManager>();
            
            // Add KCP Transport (Mirror default)
            var transport = nm.AddComponent<kcp2k.KcpTransport>();
            
            var netManager = nm.GetComponent<NetworkGameManager>();
            netManager.networkAddress = "localhost";
            netManager.maxConnections = 6;

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

            EditorUtility.DisplayDialog("Setup Complete!", 
                "✅ Scene setup tamamlandı!\n\n" +
                "Şimdi yapılacaklar:\n" +
                "1. Player prefab oluştur\n" +
                "2. NetworkManager'a player prefab ve spawn point'leri ata\n" +
                "3. Test et!", 
                "Tamam");

            Debug.Log("<color=green>🚀 FULL AUTO SETUP COMPLETE!</color>");
        }
    }
}

