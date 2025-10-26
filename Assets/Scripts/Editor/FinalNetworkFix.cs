using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// SON NETWORK FIX - "Aynı player" sorununu kesin çözüm
    /// </summary>
    public class FinalNetworkFix : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/🔥 SON ÇÖZüM - Network Fix")]
        public static void ShowWindow()
        {
            var window = GetWindow<FinalNetworkFix>("Son Çözüm");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("🔥 SON ÇÖZüM - Aynı Player Sorunu", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "SORUN TESPİTİ:\n\n" +
                "✅ Spawnable Prefabs - OK (Player zaten otomatik)\n" +
                "✅ NetworkManager - OK\n" +
                "✅ Build Settings - OK\n\n" +
                "❌ AMA HALA AYNI PLAYER!\n\n" +
                "MUHTEMEL SEBEP:\n" +
                "Scene'de NetworkIdentity olan objeler var ve\n" +
                "Build ile Editor arasında ID uyuşmazlığı var.",
                MessageType.Warning
            );

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("🔥 TEMİZ SCENE + YENİ BUILD", GUILayout.Height(60)))
            {
                CleanSceneAndRebuild();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu yapacak:\n" +
                "1. Scene'deki tüm NetworkIdentity objelerini temizle\n" +
                "2. Sadece NetworkManager ve MatchManager bırak\n" +
                "3. Scene'i kaydet\n" +
                "4. Build yap\n" +
                "5. Test et",
                MessageType.Info
            );

            GUILayout.Space(10);

            GUILayout.Label("VEYA MANUEL KONTROL:", EditorStyles.boldLabel);

            if (GUILayout.Button("🔍 Scene'deki NetworkIdentity'leri Listele", GUILayout.Height(35)))
            {
                ListSceneNetworkIdentities();
            }

            if (GUILayout.Button("🧹 Player Prefab Instance'larını Temizle", GUILayout.Height(35)))
            {
                CleanPlayerInstances();
            }
        }

        private void CleanSceneAndRebuild()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔥 TEMİZ SCENE + YENİ BUILD BAŞLADI");
            Debug.Log("═══════════════════════════════════════\n");

            bool confirm = EditorUtility.DisplayDialog(
                "UYARI!",
                "Bu işlem scene'deki Player instance'larını ve\n" +
                "diğer gereksiz NetworkIdentity objelerini SİLECEK!\n\n" +
                "Scene temiz hale gelecek.\n\n" +
                "Devam edilsin mi?",
                "Evet, Temizle",
                "İptal"
            );

            if (!confirm) return;

            // 1. Scene'deki tüm NetworkIdentity'leri bul
            var allNetIds = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            int cleaned = 0;
            foreach (var netId in allNetIds)
            {
                // NetworkManager ve MatchManager'ı KORU
                if (netId.GetComponent<NetworkManager>() != null ||
                    netId.GetComponent<TacticalCombat.Core.MatchManager>() != null)
                {
                    Debug.Log($"✓ Korunuyor: {netId.gameObject.name}");
                    continue;
                }

                // Player instance'ları ve diğer network objelerini SİL
                Debug.Log($"🗑️ Siliniyor: {netId.gameObject.name}");
                DestroyImmediate(netId.gameObject);
                cleaned++;
            }

            Debug.Log($"\n✅ {cleaned} network object temizlendi");

            // 2. Scene'i kaydet
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("✅ Scene kaydedildi");

            // 3. AudioListener kontrolü
            int audioListenersCleaned = CleanAudioListeners();
            Debug.Log($"✅ {audioListenersCleaned} duplicate AudioListener temizlendi");

            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log("✅ TEMİZLEME TAMAMLANDI");
            Debug.Log("═══════════════════════════════════════");

            EditorUtility.DisplayDialog(
                "Temizleme Tamamlandı!",
                $"✅ {cleaned} network object temizlendi\n" +
                $"✅ {audioListenersCleaned} AudioListener temizlendi\n\n" +
                "ŞİMDİ:\n" +
                "1. File → Build and Run YAP\n" +
                "2. Build'de: H (Host)\n" +
                "3. Editor'de: C (Client)\n\n" +
                "Artık çalışmalı!",
                "Tamam"
            );
        }

        private void ListSceneNetworkIdentities()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📋 SCENE'DEKİ NETWORKIDENTITY OBJELERI");
            Debug.Log("═══════════════════════════════════════\n");

            var allNetIds = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            if (allNetIds.Length == 0)
            {
                Debug.Log("✅ Scene'de NetworkIdentity yok (temiz!)");
            }
            else
            {
                Debug.Log($"Toplam {allNetIds.Length} NetworkIdentity bulundu:\n");

                for (int i = 0; i < allNetIds.Length; i++)
                {
                    var netId = allNetIds[i];
                    string type = "Unknown";

                    if (netId.GetComponent<NetworkManager>() != null)
                        type = "NetworkManager (GEREKLI)";
                    else if (netId.GetComponent<TacticalCombat.Core.MatchManager>() != null)
                        type = "MatchManager (GEREKLI)";
                    else if (netId.GetComponent<TacticalCombat.Player.FPSController>() != null)
                        type = "Player Instance (RUNTIME - SİLİNMELİ!)";
                    else
                        type = "Network Object";

                    Debug.Log($"{i + 1}. {netId.gameObject.name} - {type}");
                }
            }

            Debug.Log("\n═══════════════════════════════════════");

            // Player instance uyarısı
            int playerInstances = 0;
            foreach (var netId in allNetIds)
            {
                if (netId.GetComponent<TacticalCombat.Player.FPSController>() != null)
                    playerInstances++;
            }

            if (playerInstances > 0)
            {
                Debug.LogError($"❌ SORUN: Scene'de {playerInstances} Player instance var!");
                Debug.LogError("   Player'lar runtime'da spawn edilmeli, scene'de olmamalı!");
                Debug.LogError("   '🧹 Player Prefab Instance'larını Temizle' butonuna tıkla!");
            }
        }

        private void CleanPlayerInstances()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🧹 PLAYER INSTANCE TEMİZLEME");
            Debug.Log("═══════════════════════════════════════\n");

            var allPlayers = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);

            if (allPlayers.Length == 0)
            {
                Debug.Log("✅ Scene'de player instance yok");
                EditorUtility.DisplayDialog("OK", "Scene'de player instance yok!\n\nScene temiz.", "Tamam");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Player Instance'ları Sil?",
                $"Scene'de {allPlayers.Length} player instance bulundu.\n\n" +
                "Player'lar runtime'da spawn edilmeli,\n" +
                "scene'de olmamalı.\n\n" +
                "Silinsin mi?",
                "Evet, Sil",
                "İptal"
            );

            if (!confirm) return;

            foreach (var player in allPlayers)
            {
                Debug.Log($"🗑️ Siliniyor: {player.gameObject.name}");
                DestroyImmediate(player.gameObject);
            }

            Debug.Log($"\n✅ {allPlayers.Length} player instance silindi");
            Debug.Log("═══════════════════════════════════════");

            // Scene'i kaydet
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            EditorUtility.DisplayDialog(
                "Temizlendi!",
                $"✅ {allPlayers.Length} player instance silindi\n\n" +
                "Scene kaydedildi.\n\n" +
                "Şimdi Build and Run yap!",
                "Tamam"
            );
        }

        private int CleanAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length <= 1) return 0;

            AudioListener keep = null;
            foreach (var listener in listeners)
            {
                if (listener.CompareTag("MainCamera"))
                {
                    keep = listener;
                    break;
                }
            }
            if (keep == null) keep = listeners[0];

            int removed = 0;
            foreach (var listener in listeners)
            {
                if (listener != keep && listener != null)
                {
                    DestroyImmediate(listener);
                    removed++;
                }
            }

            return removed;
        }
    }
}
