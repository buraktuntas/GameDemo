using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    public class NetworkDebugger : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Network Debugger")]
        public static void ShowWindow()
        {
            GetWindow<NetworkDebugger>("Network Debugger");
        }

        private void OnGUI()
        {
            GUILayout.Label("Network Debugger", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Check NetworkManager"))
            {
                CheckNetworkManager();
            }

            if (GUILayout.Button("🔍 Check Player Prefab"))
            {
                CheckPlayerPrefab();
            }

            if (GUILayout.Button("🔍 Check Spawn Points"))
            {
                CheckSpawnPoints();
            }

            if (GUILayout.Button("🔍 Check Audio Listeners"))
            {
                CheckAudioListeners();
            }

            if (GUILayout.Button("🔧 Fix Audio Listeners"))
            {
                FixAudioListeners();
            }

            if (GUILayout.Button("🔧 Fix NetworkManager"))
            {
                FixNetworkManager();
            }
        }

        private void CheckNetworkManager()
        {
            NetworkManager[] networkManagers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 NetworkManager Sayısı: {networkManagers.Length}");
            
            foreach (var nm in networkManagers)
            {
                Debug.Log($"📋 NetworkManager: {nm.name}");
                Debug.Log($"   Player Prefab: {(nm.playerPrefab != null ? nm.playerPrefab.name : "NULL")}");
                Debug.Log($"   Spawn Points: {NetworkManager.startPositions.Count}");
                Debug.Log($"   Transport: {(nm.transport != null ? nm.transport.GetType().Name : "NULL")}");
                
                if (nm.playerPrefab == null)
                {
                    Debug.LogError("❌ Player Prefab NULL!");
                }
            }
        }

        private void CheckPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                Debug.LogError($"❌ Player prefab bulunamadı: {prefabPath}");
                return;
            }

            Debug.Log($"🔍 Player Prefab Kontrolü:");
            Debug.Log($"   Path: {prefabPath}");
            Debug.Log($"   Name: {playerPrefab.name}");

            // Component kontrolü
            var components = playerPrefab.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    Debug.Log($"   ✅ {comp.GetType().Name}");
                }
                else
                {
                    Debug.LogError($"   ❌ Missing Script: {comp}");
                }
            }

            // NetworkIdentity kontrolü
            NetworkIdentity netId = playerPrefab.GetComponent<NetworkIdentity>();
            if (netId == null)
            {
                Debug.LogError("❌ NetworkIdentity eksik!");
            }
            else
            {
                Debug.Log($"   ✅ NetworkIdentity: {netId.netId}");
            }

            // Camera kontrolü
            Camera camera = playerPrefab.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                Debug.LogError("❌ Camera eksik!");
            }
            else
            {
                Debug.Log($"   ✅ Camera: {camera.name}");
                
                // AudioListener kontrolü
                AudioListener audioListener = camera.GetComponent<AudioListener>();
                if (audioListener == null)
                {
                    Debug.LogError("❌ AudioListener eksik!");
                }
                else
                {
                    Debug.Log($"   ✅ AudioListener: {audioListener.enabled}");
                }
            }
        }

        private void CheckSpawnPoints()
        {
            NetworkStartPosition[] spawnPoints = FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 Spawn Points Sayısı: {spawnPoints.Length}");
            
            foreach (var spawn in spawnPoints)
            {
                Debug.Log($"📍 Spawn Point: {spawn.name} at {spawn.transform.position}");
            }
        }

        private void CheckAudioListeners()
        {
            AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            Debug.Log($"🔍 Audio Listeners Sayısı: {audioListeners.Length}");
            
            foreach (var listener in audioListeners)
            {
                Debug.Log($"🔊 AudioListener: {listener.name} - Enabled: {listener.enabled}");
            }
        }

        private void FixAudioListeners()
        {
            AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            Debug.Log($"🔧 Audio Listeners düzeltiliyor...");
            
            // Tüm AudioListener'ları kapat
            foreach (var listener in audioListeners)
            {
                listener.enabled = false;
                Debug.Log($"🔇 AudioListener kapatıldı: {listener.name}");
            }
            
            Debug.Log("✅ Tüm AudioListener'lar kapatıldı. Oyun başladığında sadece local player'da aktif olacak.");
        }

        private void FixNetworkManager()
        {
            NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
            
            if (networkManager == null)
            {
                Debug.LogError("❌ NetworkManager bulunamadı!");
                return;
            }

            // Player prefab'ı ata
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                Debug.LogError($"❌ Player prefab bulunamadı: {prefabPath}");
                return;
            }

            networkManager.playerPrefab = playerPrefab;
            EditorUtility.SetDirty(networkManager);
            
            Debug.Log($"✅ NetworkManager'a Player Prefab atandı: {playerPrefab.name}");
        }
    }
}
