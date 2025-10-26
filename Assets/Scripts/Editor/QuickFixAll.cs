using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Tüm yaygın sorunları TEK TIKLA düzelt
    /// </summary>
    public class QuickFixAll : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/⚡ TÜM SORUNLARI DÜZELT")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuickFixAll>("Hızlı Düzeltme");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("⚡ TÜM SORUNLARI TEK TIKLA DÜZELT", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Bu tool şu sorunları OTOMATIK düzeltir:\n\n" +
                "✅ Spawnable Prefabs (sync message hatası)\n" +
                "✅ AudioListener duplicates (2 listener uyarısı)\n" +
                "✅ NetworkManager ayarları\n" +
                "✅ Build Settings (scene ekleme)",
                MessageType.Info
            );

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("⚡⚡⚡ HEPSİNİ DÜZELT ⚡⚡⚡", GUILayout.Height(60)))
            {
                FixAllIssues();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            GUILayout.Label("Veya tek tek düzelt:", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Spawnable Prefabs Düzelt", GUILayout.Height(35)))
            {
                FixSpawnablePrefabs();
            }

            if (GUILayout.Button("2. AudioListener Duplicates Düzelt", GUILayout.Height(35)))
            {
                FixAudioListeners();
            }

            if (GUILayout.Button("3. NetworkManager Ayarları Düzelt", GUILayout.Height(35)))
            {
                FixNetworkManager();
            }

            if (GUILayout.Button("4. Build Settings Düzelt", GUILayout.Height(35)))
            {
                FixBuildSettings();
            }
        }

        private void FixAllIssues()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("⚡⚡⚡ TÜM SORUNLAR DÜZELTİLİYOR ⚡⚡⚡");
            Debug.Log("═══════════════════════════════════════\n");

            int totalFixes = 0;

            totalFixes += FixSpawnablePrefabs();
            totalFixes += FixAudioListeners();
            totalFixes += FixNetworkManager();
            totalFixes += FixBuildSettings();

            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log($"✅ TAMAMLANDI - {totalFixes} düzeltme yapıldı!");
            Debug.Log("═══════════════════════════════════════");

            EditorUtility.DisplayDialog(
                "Tüm Sorunlar Düzeltildi!",
                $"✅ {totalFixes} düzeltme yapıldı!\n\n" +
                "ŞİMDİ:\n" +
                "1. Scene'i kaydet (Ctrl+S)\n" +
                "2. Build and Run yap\n" +
                "3. Build'de: H tuşuna bas (Host)\n" +
                "4. Editor'de: C tuşuna bas (Client)\n\n" +
                "Artık çalışmalı! 🚀",
                "Tamam"
            );

            // Scene'i kaydet
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        private int FixSpawnablePrefabs()
        {
            Debug.Log("\n🔧 1. Spawnable Prefabs düzeltiliyor...");

            var netMgr = FindFirstObjectByType<NetworkManager>();
            if (netMgr == null) return 0;

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null) return 0;

            List<GameObject> spawnablePrefabs = new List<GameObject>(netMgr.spawnPrefabs);
            int count = 0;

            if (!spawnablePrefabs.Contains(playerPrefab))
            {
                spawnablePrefabs.Add(playerPrefab);
                Debug.Log("   ✅ Player prefab eklendi");
                count++;
            }

            // Tüm network prefab'ları bul ve ekle
            string[] allPrefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (string guid in allPrefabPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<NetworkIdentity>() != null)
                {
                    if (!spawnablePrefabs.Contains(prefab))
                    {
                        spawnablePrefabs.Add(prefab);
                        Debug.Log($"   ✅ {prefab.name} eklendi");
                        count++;
                    }
                }
            }

            netMgr.spawnPrefabs = spawnablePrefabs;
            EditorUtility.SetDirty(netMgr);

            Debug.Log($"   📊 Toplam spawnable prefabs: {spawnablePrefabs.Count}");
            return count;
        }

        private int FixAudioListeners()
        {
            Debug.Log("\n🔧 2. AudioListener duplicates düzeltiliyor...");

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length <= 1)
            {
                Debug.Log("   ✓ AudioListener sayısı doğru");
                return 0;
            }

            Debug.Log($"   ⚠️ {listeners.Length} AudioListener bulundu! (1 olmalı)");

            // MainCamera'nın listener'ını tut
            AudioListener keep = null;
            foreach (var listener in listeners)
            {
                if (listener.CompareTag("MainCamera"))
                {
                    keep = listener;
                    break;
                }
            }

            // Hiçbiri MainCamera değilse, ilkini tut
            if (keep == null) keep = listeners[0];

            // Diğerlerini sil
            int removed = 0;
            foreach (var listener in listeners)
            {
                if (listener != keep && listener != null)
                {
                    DestroyImmediate(listener);
                    removed++;
                }
            }

            Debug.Log($"   ✅ {removed} duplicate AudioListener silindi");
            return removed;
        }

        private int FixNetworkManager()
        {
            Debug.Log("\n🔧 3. NetworkManager ayarları düzeltiliyor...");

            var netMgr = FindFirstObjectByType<NetworkManager>();
            if (netMgr == null) return 0;

            int count = 0;

            // Player Prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null && netMgr.playerPrefab != playerPrefab)
            {
                netMgr.playerPrefab = playerPrefab;
                Debug.Log("   ✅ Player prefab atandı");
                count++;
            }

            // Auto Create Player
            if (!netMgr.autoCreatePlayer)
            {
                netMgr.autoCreatePlayer = true;
                Debug.Log("   ✅ Auto Create Player açıldı");
                count++;
            }

            // Transport
            if (netMgr.transport == null)
            {
                var transport = netMgr.GetComponent<Mirror.Transport>();
                if (transport == null)
                {
                    transport = netMgr.gameObject.AddComponent<kcp2k.KcpTransport>();
                }
                netMgr.transport = transport;
                Debug.Log("   ✅ Transport atandı");
                count++;
            }

            EditorUtility.SetDirty(netMgr);
            return count;
        }

        private int FixBuildSettings()
        {
            Debug.Log("\n🔧 4. Build Settings düzeltiliyor...");

            string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning("   ⚠️ Scene kayıtlı değil!");
                return 0;
            }

            var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
            bool sceneInBuild = scenes.Exists(s => s.path == scenePath && s.enabled);

            if (!sceneInBuild)
            {
                scenes.RemoveAll(s => s.path == scenePath);
                scenes.Insert(0, new UnityEditor.EditorBuildSettingsScene(scenePath, true));
                UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("   ✅ Scene Build Settings'e eklendi");
                return 1;
            }

            Debug.Log("   ✓ Scene zaten Build Settings'te");
            return 0;
        }
    }
}
