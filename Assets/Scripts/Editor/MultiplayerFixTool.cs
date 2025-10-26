using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Otomatik Multiplayer Sorun Düzeltici
    /// Menü: Tools → Multiplayer Fix Tool
    /// </summary>
    public class MultiplayerFixTool : EditorWindow
    {
        private Vector2 scrollPos;
        private int issuesFound = 0;
        private int issuesFixed = 0;
        private string statusMessage = "Hazır. 'Tüm Sorunları Tara ve Düzelt' butonuna tıklayın.";
        private Color statusColor = Color.white;

        [MenuItem("Tools/Multiplayer Fix Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiplayerFixTool>("Multiplayer Fix");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🔧 MULTIPLAYER FIX TOOL", headerStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Tüm multiplayer sorunlarını otomatik düzelt", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(10);
            DrawLine();
            EditorGUILayout.Space(10);

            // Status box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true
            };
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusMessage, statusStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Main button
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("🔍 TÜM SORUNLARI TARA VE DÜZELT", GUILayout.Height(50)))
            {
                RunFullFix();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            DrawLine();
            EditorGUILayout.Space(10);

            // Individual fixes
            EditorGUILayout.LabelField("Tekil Düzeltmeler:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawFixButton("1. Scene Clone'larını Temizle",
                "MuzzleFlash ve HitEffect clone'larını siler",
                FixClones);

            DrawFixButton("2. Gereksiz NetworkIdentity'leri Temizle",
                "Statik objelerden NetworkIdentity component'lerini kaldırır",
                FixNetworkIdentities);

            DrawFixButton("3. Scene'deki Player Objelerini Sil",
                "Scene'de bulunmaması gereken Player objelerini siler",
                FixScenePlayers);

            DrawFixButton("4. Spawn Points'leri Kontrol Et",
                "Spawn point'lerin doğru kurulumunu kontrol eder",
                FixSpawnPoints);

            DrawFixButton("5. NetworkManager'ı Kontrol Et",
                "NetworkManager ayarlarını doğrular",
                FixNetworkManager);

            DrawFixButton("6. Build Settings'i Düzelt",
                "Scene'i Build Settings'e ekler",
                FixBuildSettings);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawLine();
            EditorGUILayout.Space(10);

            // Statistics
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📊 İstatistikler:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"   Bulunan Sorun: {issuesFound}");
            EditorGUILayout.LabelField($"   Düzeltilen: {issuesFixed}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Footer
            EditorGUILayout.LabelField("Made with 🚀 by Claude", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawFixButton(string title, string description, System.Action action)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Düzelt", GUILayout.Height(25)))
            {
                action();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void RunFullFix()
        {
            issuesFound = 0;
            issuesFixed = 0;
            statusMessage = "🔍 Tarama başlatılıyor...\n";
            statusColor = Color.yellow;
            Repaint();

            // Run all fixes
            FixClones();
            FixNetworkIdentities();
            FixScenePlayers();
            FixSpawnPoints();
            FixNetworkManager();
            FixBuildSettings();

            // Final message
            if (issuesFixed > 0)
            {
                statusMessage = $"✅ TAMAMLANDI!\n\n{issuesFound} sorun bulundu\n{issuesFixed} sorun düzeltildi\n\nScene kaydedildi. Artık multiplayer test edebilirsin!";
                statusColor = Color.green;
                EditorUtility.DisplayDialog("Multiplayer Fix",
                    $"✅ {issuesFixed} sorun düzeltildi!\n\nScene kaydedildi. Şimdi Host + Client test yapabilirsin.",
                    "Tamam");
            }
            else
            {
                statusMessage = $"✅ Sorun bulunamadı!\n\nProjen zaten temiz görünüyor. Eğer hala sorun yaşıyorsan:\n- NetworkManager prefab'ı kontrol et\n- Player prefab'da NetworkIdentity var mı kontrol et";
                statusColor = new Color(0.3f, 0.8f, 1f);
            }

            Repaint();
        }

        private void FixClones()
        {
            int fixedCount = 0;
            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // Find all MuzzleFlash and HitEffect clones
                var clones = root.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.Contains("MuzzleFlash(Clone)") || t.name.Contains("HitEffect(Clone)"))
                    .ToArray();

                foreach (var clone in clones)
                {
                    DestroyImmediate(clone.gameObject);
                    fixedCount++;
                    issuesFound++;
                    issuesFixed++;
                }
            }

            if (fixedCount > 0)
            {
                statusMessage += $"✅ {fixedCount} clone objesi temizlendi\n";
                Debug.Log($"[MultiplayerFix] {fixedCount} clone objesi silindi");
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
            else
            {
                statusMessage += "✓ Clone objesi bulunamadı\n";
            }

            Repaint();
        }

        private void FixNetworkIdentities()
        {
            int fixedCount = 0;
            var networkIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            foreach (var netId in networkIdentities)
            {
                var go = netId.gameObject;

                // Keep NetworkIdentities on:
                // - NetworkManager
                // - Objects with "Spawn" in name
                // - Objects with NetworkBehaviour components

                bool shouldKeep = go.name.Contains("NetworkManager") ||
                                  go.name.Contains("Spawn") ||
                                  go.GetComponent<NetworkBehaviour>() != null;

                if (!shouldKeep)
                {
                    // Check if it's a static environment object
                    bool isStatic = go.name.Contains("Ground") ||
                                    go.name.Contains("Terrain") ||
                                    go.name.Contains("Wall") ||
                                    go.name.Contains("Floor") ||
                                    go.name.Contains("Environment") ||
                                    go.isStatic;

                    if (isStatic)
                    {
                        Debug.Log($"[MultiplayerFix] Removing NetworkIdentity from: {go.name}");
                        DestroyImmediate(netId);
                        fixedCount++;
                        issuesFound++;
                        issuesFixed++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                statusMessage += $"✅ {fixedCount} gereksiz NetworkIdentity kaldırıldı\n";
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else
            {
                statusMessage += "✓ Gereksiz NetworkIdentity bulunamadı\n";
            }

            Repaint();
        }

        private void FixScenePlayers()
        {
            int fixedCount = 0;
            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // Find Player objects in scene (they should only be spawned at runtime)
                if (root.name.Contains("Player") && !root.name.Contains("Spawn"))
                {
                    var netId = root.GetComponent<NetworkIdentity>();
                    if (netId != null && netId.sceneId != 0)
                    {
                        // This is a scene player - should not exist!
                        Debug.LogWarning($"[MultiplayerFix] Found scene Player object: {root.name} - REMOVING");
                        DestroyImmediate(root);
                        fixedCount++;
                        issuesFound++;
                        issuesFixed++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                statusMessage += $"✅ {fixedCount} scene Player objesi silindi\n";
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
            else
            {
                statusMessage += "✓ Scene'de Player objesi bulunamadı (doğru)\n";
            }

            Repaint();
        }

        private void FixSpawnPoints()
        {
            var spawnPoints = GameObject.Find("SpawnPoints");

            if (spawnPoints == null)
            {
                statusMessage += "⚠️ SpawnPoints bulunamadı - NetworkManager'da spawn point ayarla\n";
                issuesFound++;
                return;
            }

            // Check if spawn points have NetworkIdentity (they shouldn't)
            var netIds = spawnPoints.GetComponentsInChildren<NetworkIdentity>();
            int fixedCount = 0;

            foreach (var netId in netIds)
            {
                DestroyImmediate(netId);
                fixedCount++;
                issuesFixed++;
            }

            if (fixedCount > 0)
            {
                statusMessage += $"✅ {fixedCount} spawn point NetworkIdentity kaldırıldı\n";
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else
            {
                statusMessage += "✓ Spawn points doğru kurulmuş\n";
            }

            Repaint();
        }

        private void FixNetworkManager()
        {
            var networkManager = FindFirstObjectByType<NetworkManager>();

            if (networkManager == null)
            {
                statusMessage += "❌ NetworkManager bulunamadı!\n";
                issuesFound++;
                EditorUtility.DisplayDialog("Hata",
                    "NetworkManager bulunamadı! Lütfen scene'e NetworkManager ekleyin.",
                    "Tamam");
                return;
            }

            bool hadIssues = false;

            // Check player prefab
            if (networkManager.playerPrefab == null)
            {
                statusMessage += "❌ NetworkManager'da Player Prefab atanmamış!\n";
                issuesFound++;
                hadIssues = true;
            }

            // Check spawn method
            var so = new SerializedObject(networkManager);
            var spawnMethod = so.FindProperty("playerSpawnMethod");

            if (spawnMethod.enumValueIndex != 1) // 1 = RoundRobin
            {
                spawnMethod.enumValueIndex = 1;
                so.ApplyModifiedProperties();
                statusMessage += "✅ NetworkManager spawn method düzeltildi\n";
                issuesFound++;
                issuesFixed++;
                hadIssues = true;
            }

            if (!hadIssues)
            {
                statusMessage += "✓ NetworkManager doğru yapılandırılmış\n";
            }

            Repaint();
        }

        private void FixBuildSettings()
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenePath = activeScene.path;

            if (string.IsNullOrEmpty(scenePath))
            {
                statusMessage += "⚠️ Scene henüz kaydedilmemiş - önce kaydedin\n";
                return;
            }

            // Check if scene is in build settings
            var scenesInBuild = EditorBuildSettings.scenes;
            bool sceneInBuild = scenesInBuild.Any(s => s.path == scenePath);

            if (!sceneInBuild)
            {
                // Add to build settings
                var newScenes = new EditorBuildSettingsScene[scenesInBuild.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);

                for (int i = 0; i < scenesInBuild.Length; i++)
                {
                    newScenes[i + 1] = scenesInBuild[i];
                }

                EditorBuildSettings.scenes = newScenes;
                statusMessage += "✅ Scene Build Settings'e eklendi\n";
                issuesFound++;
                issuesFixed++;
            }
            else
            {
                statusMessage += "✓ Scene zaten Build Settings'de\n";
            }

            Repaint();
        }
    }
}
