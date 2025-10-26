using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Multiplayer test yardımcısı - Doğru şekilde test etmek için
    /// </summary>
    public class MultiplayerTestHelper : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Multiplayer Test Helper 🎮")]
        public static void ShowWindow()
        {
            GetWindow<MultiplayerTestHelper>("Multiplayer Test");
        }

        private void OnGUI()
        {
            GUILayout.Label("Multiplayer Test Yardımcısı", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "🎮 DOĞRU TEST YÖNTEMİ:\n\n" +
                "1. BUILD: Host olarak başlat\n" +
                "2. EDITOR: Client olarak join et\n\n" +
                "⚠️ SORUN: Her iki taraf da aynı player'ı kontrol ediyor?\n" +
                "SEBEP: NetworkManager ayarları veya spawn sistemi yanlış.\n\n" +
                "ÇÖZÜMLERİ:",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Test senaryosu
            GUILayout.Label("═══ Test Senaryosu ═══", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "DOĞRU TEST ADIMLARI:\n\n" +
                "1. Build and Run yap (Build #1 açılır)\n" +
                "2. Build'de 'Host' butonuna tıkla\n" +
                "3. Unity Editor'de Play'e bas\n" +
                "4. Editor'de 'Client' butonuna tıkla\n" +
                "5. LAN'da IP: localhost veya 127.0.0.1\n\n" +
                "Her client için AYRI player spawn edilmeli!",
                MessageType.Warning
            );

            GUILayout.Space(10);

            // NetworkManager kontrolü
            GUILayout.Label("═══ NetworkManager Kontrolü ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("🔧 NetworkManager'ı Kontrol Et", GUILayout.Height(35)))
            {
                CheckNetworkManager();
            }

            GUILayout.Space(10);

            // Diagnostic
            GUILayout.Label("═══ Diagnostic ═══", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                var netMgr = FindFirstObjectByType<NetworkManager>();
                if (netMgr != null)
                {
                    GUILayout.Label($"Network Mode: {netMgr.mode}");
                    GUILayout.Label($"Is Server: {NetworkServer.active}");
                    GUILayout.Label($"Is Client: {NetworkClient.active}");
                    GUILayout.Label($"Connections: {NetworkServer.connections.Count}");

                    GUILayout.Space(5);

                    if (GUILayout.Button("📊 Tüm Players'ı Listele"))
                    {
                        ListAllPlayers();
                    }
                }
                else
                {
                    GUILayout.Label("NetworkManager bulunamadı!");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Play modunda değil. Diagnostic için Play'e basın.", MessageType.Info);
            }
        }

        private void CheckNetworkManager()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔧 NETWORKMANAGER KONTROLÜ");
            Debug.Log("═══════════════════════════════════════\n");

            var netMgr = FindFirstObjectByType<NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ NetworkManager bulunamadı!");
                return;
            }

            // 1. Player Prefab
            Debug.Log($"Player Prefab: {(netMgr.playerPrefab != null ? "✅ " + netMgr.playerPrefab.name : "❌ NULL")}");

            if (netMgr.playerPrefab != null)
            {
                var netId = netMgr.playerPrefab.GetComponent<NetworkIdentity>();
                Debug.Log($"  NetworkIdentity: {(netId != null ? "✅ VAR" : "❌ YOK")}");

                var fpsController = netMgr.playerPrefab.GetComponent<TacticalCombat.Player.FPSController>();
                Debug.Log($"  FPSController: {(fpsController != null ? "✅ VAR" : "❌ YOK")}");
            }

            // 2. Transport
            Debug.Log($"\nTransport: {(netMgr.transport != null ? "✅ " + netMgr.transport.GetType().Name : "❌ NULL")}");

            // 3. Auto Create Player
            Debug.Log($"Auto Create Player: {(netMgr.autoCreatePlayer ? "✅ AÇIK" : "❌ KAPALI (SORUN!)")}");

            // 4. Network Mode (Play modunda)
            if (Application.isPlaying)
            {
                Debug.Log($"\n🎮 RUNTIME INFO:");
                Debug.Log($"  Mode: {netMgr.mode}");
                Debug.Log($"  Server Active: {NetworkServer.active}");
                Debug.Log($"  Client Active: {NetworkClient.active}");
                Debug.Log($"  Connection Count: {NetworkServer.connections.Count}");
            }

            Debug.Log("\n═══════════════════════════════════════");

            // Uyarılar
            if (!netMgr.autoCreatePlayer)
            {
                Debug.LogWarning("⚠️ UYARI: 'Auto Create Player' KAPALI!");
                Debug.LogWarning("   Bu ayar kapalıysa player otomatik spawn olmaz.");
                Debug.LogWarning("   NetworkManager'da bu ayarı AÇ!");

                bool fix = EditorUtility.DisplayDialog(
                    "Auto Create Player Kapalı!",
                    "'Auto Create Player' ayarı kapalı.\n\n" +
                    "Bu ayar açık olmalı ki her client bağlandığında otomatik player spawn olsun.\n\n" +
                    "Otomatik düzeltilsin mi?",
                    "Evet, Düzelt",
                    "Hayır"
                );

                if (fix)
                {
                    netMgr.autoCreatePlayer = true;
                    EditorUtility.SetDirty(netMgr);
                    Debug.Log("✅ Auto Create Player AÇILDI!");
                }
            }
        }

        private void ListAllPlayers()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📊 TÜM PLAYERS");
            Debug.Log("═══════════════════════════════════════\n");

            var players = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);

            if (players.Length == 0)
            {
                Debug.Log("❌ Hiç player bulunamadı!");
            }
            else
            {
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    var netId = player.GetComponent<NetworkIdentity>();

                    Debug.Log($"\n🎮 Player {i + 1}:");
                    Debug.Log($"  GameObject: {player.gameObject.name}");
                    Debug.Log($"  NetID: {(netId != null ? netId.netId.ToString() : "YOK")}");
                    Debug.Log($"  isLocalPlayer: {player.isLocalPlayer}");
                    Debug.Log($"  isServer: {player.isServer}");
                    Debug.Log($"  isClient: {player.isClient}");

                    if (netId != null && netId.connectionToClient != null)
                    {
                        Debug.Log($"  ConnectionID: {netId.connectionToClient.connectionId}");
                    }
                }
            }

            Debug.Log("\n═══════════════════════════════════════");

            // Sorun tespit
            int localPlayerCount = 0;
            foreach (var player in players)
            {
                if (player.isLocalPlayer) localPlayerCount++;
            }

            if (localPlayerCount > 1)
            {
                Debug.LogError($"❌ SORUN TESPİT EDİLDİ: {localPlayerCount} tane isLocalPlayer=true!");
                Debug.LogError("   Sadece 1 tane olmalı! Her client kendi player'ını kontrol etmeli.");
                Debug.LogError("\n   OLASI SEBEPLER:");
                Debug.LogError("   1. Aynı cihazda hem Host hem Client çalışıyor olabilir");
                Debug.LogError("   2. NetworkServer.AddPlayerForConnection her connection için ayrı çağrılmıyor");
                Debug.LogError("   3. Player prefab'da NetworkIdentity yok veya yanlış ayarlanmış");
            }
            else if (localPlayerCount == 1)
            {
                Debug.Log("✅ isLocalPlayer kontrolü doğru (1 tane)");
            }
        }
    }
}
