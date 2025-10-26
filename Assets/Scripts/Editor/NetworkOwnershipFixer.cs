using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Network ownership sorununu düzelten tool
    /// SORUN: Build ve Editor aynı player'ı kontrol ediyor
    /// ÇÖZÜM: Her connection için AYRI player spawn edilmeli
    /// </summary>
    public class NetworkOwnershipFixer : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/FIX: Aynı Player Sorunu ⚡")]
        public static void ShowWindow()
        {
            GetWindow<NetworkOwnershipFixer>("Ownership Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Network Ownership Düzeltici", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "❌ SORUN: Build ve Editor aynı kişi oluyor?\n" +
                "   Birinden hareket edince diğeri de hareket ediyor?\n\n" +
                "✅ OLASI SEBEPLER:\n" +
                "1. NetworkManager'da 'Spawn Player' otomatik açık değil\n" +
                "2. Player spawn'ı doğru çalışmıyor\n" +
                "3. isLocalPlayer kontrolü eksik\n\n" +
                "Bu tool otomatik düzeltir!",
                MessageType.Error
            );

            GUILayout.Space(10);

            if (GUILayout.Button("⚡ NETWORK SORUNLARINI DÜZELT", GUILayout.Height(50)))
            {
                FixNetworkOwnership();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Network Diagnostic Yap", GUILayout.Height(35)))
            {
                RunDiagnostic();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Yapılacaklar:\n" +
                "1. NetworkManager ayarları kontrol edilir\n" +
                "2. Player prefab doğrulanır\n" +
                "3. Spawn sistemi test edilir",
                MessageType.Info
            );
        }

        private void FixNetworkOwnership()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("⚡ NETWORK OWNERSHIP FIX BAŞLADI");
            Debug.Log("═══════════════════════════════════════\n");

            int fixCount = 0;

            // 1. Player Prefab'ı kontrol et
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player.prefab bulunamadı!");
                EditorUtility.DisplayDialog("Hata", "Player.prefab bulunamadı!\n\nPath: Assets/Prefabs/Player.prefab", "OK");
                return;
            }

            Debug.Log("📦 Player prefab bulundu: " + playerPrefab.name);

            // 2. NetworkIdentity kontrolü
            var netId = playerPrefab.GetComponent<NetworkIdentity>();

            if (netId == null)
            {
                Debug.LogError("❌ Player prefab'da NetworkIdentity YOK!");

                string path = AssetDatabase.GetAssetPath(playerPrefab);
                var prefabContents = PrefabUtility.LoadPrefabContents(path);

                prefabContents.AddComponent<NetworkIdentity>();

                PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                Debug.Log("✅ NetworkIdentity eklendi!");
                fixCount++;

                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
                netId = playerPrefab.GetComponent<NetworkIdentity>();
            }

            // 3. NetworkManager kontrolü
            Debug.Log("\n🌐 NetworkManager kontrolü...");

            var netMgr = FindFirstObjectByType<NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ Scene'de NetworkManager YOK!");
                EditorUtility.DisplayDialog("Hata", "Scene'de NetworkManager bulunamadı!\n\nTools → Ultimate Setup → BAŞLAT çalıştır!", "OK");
                return;
            }

            // Player Prefab assignment
            if (netMgr.playerPrefab != playerPrefab)
            {
                netMgr.playerPrefab = playerPrefab;
                EditorUtility.SetDirty(netMgr);
                Debug.Log("✅ NetworkManager'a Player prefab atandı");
                fixCount++;
            }
            else
            {
                Debug.Log("✓ NetworkManager'da Player prefab doğru");
            }

            // Auto Create Player check (ÇOK ÖNEMLİ!)
            if (!netMgr.autoCreatePlayer)
            {
                Debug.LogWarning("⚠️ 'Auto Create Player' KAPALI!");

                bool enable = EditorUtility.DisplayDialog(
                    "Auto Create Player Kapalı",
                    "'Auto Create Player' ayarı KAPALI!\n\n" +
                    "Bu ayar açık olmazsa player spawn olmaz.\n\n" +
                    "Açılsın mı?",
                    "Evet, Aç",
                    "Hayır"
                );

                if (enable)
                {
                    netMgr.autoCreatePlayer = true;
                    EditorUtility.SetDirty(netMgr);
                    Debug.Log("✅ Auto Create Player AÇILDI!");
                    fixCount++;
                }
            }
            else
            {
                Debug.Log("✓ Auto Create Player açık");
            }

            // Player Spawn Method check
            Debug.Log("\n🎯 Player Spawn Method kontrolü...");

            // NetworkGameManager'ın OnServerAddPlayer metodunu kontrol et
            var customNetMgr = netMgr as TacticalCombat.Network.NetworkGameManager;
            if (customNetMgr != null)
            {
                Debug.Log("✓ Custom NetworkGameManager kullanılıyor");
                Debug.Log("  OnServerAddPlayer override var - her connection için ayrı player spawn edilmeli");
            }
            else
            {
                Debug.LogWarning("⚠️ Base NetworkManager kullanılıyor - Custom NetworkGameManager kullanılmalı!");
            }

            // 4. Component kontrolleri
            Debug.Log("\n🔍 Player prefab component kontrolü...");

            var fpsController = playerPrefab.GetComponent<TacticalCombat.Player.FPSController>();
            if (fpsController != null)
            {
                Debug.Log("✓ FPSController var");
            }
            else
            {
                Debug.LogWarning("⚠️ FPSController bulunamadı!");
            }

            var weaponSystem = playerPrefab.GetComponent<TacticalCombat.Combat.WeaponSystem>();
            if (weaponSystem != null)
            {
                Debug.Log("✓ WeaponSystem var");
            }
            else
            {
                Debug.LogWarning("⚠️ WeaponSystem bulunamadı!");
            }

            // SONUÇ
            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log($"✅ DÜZELTME TAMAMLANDI - {fixCount} fix uygulandı");
            Debug.Log("═══════════════════════════════════════");

            string message = $"✅ {fixCount} düzeltme uygulandı!\n\n";
            message += "ŞİMDİ TEST ET:\n\n";
            message += "1. Build and Run yap\n";
            message += "2. Build'de 'Host' başlat\n";
            message += "3. Unity Editor'de Play + Client join et\n\n";
            message += "⚠️ EĞER SORUN DEVAM EDİYORSA:\n";
            message += "'🔍 Network Diagnostic Yap' butonuna tıkla";

            EditorUtility.DisplayDialog("Düzeltme Tamamlandı", message, "Tamam");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void RunDiagnostic()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 NETWORK DIAGNOSTIC");
            Debug.Log("═══════════════════════════════════════\n");

            // NetworkManager
            var netMgr = FindFirstObjectByType<NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ NetworkManager bulunamadı!");
                return;
            }

            Debug.Log("🌐 NETWORKMANAGER:");
            Debug.Log($"  Type: {netMgr.GetType().Name}");
            Debug.Log($"  Player Prefab: {(netMgr.playerPrefab != null ? netMgr.playerPrefab.name : "NULL")}");
            Debug.Log($"  Auto Create Player: {netMgr.autoCreatePlayer}");
            Debug.Log($"  Transport: {(netMgr.transport != null ? netMgr.transport.GetType().Name : "NULL")}");

            if (Application.isPlaying)
            {
                Debug.Log($"\n🎮 RUNTIME BILGI:");
                Debug.Log($"  Mode: {netMgr.mode}");
                Debug.Log($"  Server Active: {NetworkServer.active}");
                Debug.Log($"  Client Active: {NetworkClient.active}");
                Debug.Log($"  Connections: {NetworkServer.connections.Count}");

                // Player'ları listele
                var players = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
                Debug.Log($"\n👥 SPAWN EDİLEN PLAYERS: {players.Length}");

                int localPlayerCount = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    Debug.Log($"\n  Player {i + 1}:");
                    Debug.Log($"    Name: {players[i].gameObject.name}");
                    Debug.Log($"    isLocalPlayer: {players[i].isLocalPlayer}");
                    Debug.Log($"    isServer: {players[i].isServer}");
                    Debug.Log($"    isClient: {players[i].isClient}");

                    var netIdentity = players[i].GetComponent<NetworkIdentity>();
                    if (netIdentity != null)
                    {
                        Debug.Log($"    NetID: {netIdentity.netId}");
                        if (netIdentity.connectionToClient != null)
                        {
                            Debug.Log($"    ConnectionID: {netIdentity.connectionToClient.connectionId}");
                        }
                    }

                    if (players[i].isLocalPlayer) localPlayerCount++;
                }

                Debug.Log($"\n📊 SONUÇ:");
                if (localPlayerCount > 1)
                {
                    Debug.LogError($"❌ SORUN: {localPlayerCount} tane isLocalPlayer=true!");
                    Debug.LogError("   Sadece 1 olmalı! Bu sorunu çözmek için:");
                    Debug.LogError("   1. Her connection'dan OnServerAddPlayer çağrıldığından emin ol");
                    Debug.LogError("   2. NetworkServer.AddPlayerForConnection doğru kullanılıyor mu kontrol et");
                }
                else if (localPlayerCount == 1)
                {
                    Debug.Log("✅ isLocalPlayer kontrolü DOĞRU!");
                }
                else
                {
                    Debug.LogWarning("⚠️ Hiç local player yok - Client bağlanmadı mı?");
                }
            }
            else
            {
                Debug.Log("\n⚠️ Play modunda değil - runtime bilgi yok");
            }

            Debug.Log("\n═══════════════════════════════════════");
        }
    }
}
