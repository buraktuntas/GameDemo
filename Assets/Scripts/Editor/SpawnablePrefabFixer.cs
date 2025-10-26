using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// NetworkManager Spawnable Prefabs düzeltici
    /// SORUN: "Did not find target for sync message" hatası
    /// ÇÖZÜM: Player prefab'ı spawnable listesine ekle
    /// </summary>
    public class SpawnablePrefabFixer : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/FIX: Spawnable Prefabs ⚡")]
        public static void ShowWindow()
        {
            GetWindow<SpawnablePrefabFixer>("Spawnable Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Spawnable Prefabs Düzeltici", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "❌ HATA: 'Did not find target for sync message'\n\n" +
                "SEBEP: Player prefab NetworkManager'ın\n" +
                "       'Spawnable Prefabs' listesinde YOK!\n\n" +
                "Mirror'da tüm network prefab'lar spawnable\n" +
                "listede olmalı ki client onları tanısın.",
                MessageType.Error
            );

            GUILayout.Space(10);

            if (GUILayout.Button("⚡ SPAWNABLE PREFABS'I DÜZELT", GUILayout.Height(50)))
            {
                FixSpawnablePrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Spawnable Listesini Göster", GUILayout.Height(35)))
            {
                ShowSpawnableList();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool:\n" +
                "1. Player prefab'ı spawnable listesine ekler\n" +
                "2. Tüm NetworkIdentity olan prefab'ları bulur\n" +
                "3. Otomatik olarak listeye ekler",
                MessageType.Info
            );
        }

        private void FixSpawnablePrefabs()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("⚡ SPAWNABLE PREFABS FIX BAŞLADI");
            Debug.Log("═══════════════════════════════════════\n");

            // NetworkManager bul
            var netMgr = FindFirstObjectByType<NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ Scene'de NetworkManager yok!");
                EditorUtility.DisplayDialog("Hata", "Scene'de NetworkManager bulunamadı!", "OK");
                return;
            }

            // Player prefab bul
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player.prefab bulunamadı!");
                EditorUtility.DisplayDialog("Hata", "Player.prefab bulunamadı!\n\nPath: Assets/Prefabs/Player.prefab", "OK");
                return;
            }

            Debug.Log($"📦 Player prefab bulundu: {playerPrefab.name}");

            // Mevcut spawnable listesini al
            List<GameObject> spawnablePrefabs = new List<GameObject>(netMgr.spawnPrefabs);

            int addedCount = 0;

            // Player prefab'ı ekle (eğer yoksa)
            if (!spawnablePrefabs.Contains(playerPrefab))
            {
                spawnablePrefabs.Add(playerPrefab);
                Debug.Log("✅ Player prefab spawnable listesine EKLENDİ!");
                addedCount++;
            }
            else
            {
                Debug.Log("✓ Player prefab zaten spawnable listesinde");
            }

            // Prefabs klasöründeki tüm NetworkIdentity prefab'ları bul
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
                        Debug.Log($"✅ Spawnable listesine eklendi: {prefab.name}");
                        addedCount++;
                    }
                }
            }

            // Listeyi geri ata
            netMgr.spawnPrefabs = spawnablePrefabs;
            EditorUtility.SetDirty(netMgr);

            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log($"✅ İŞLEM TAMAMLANDI - {addedCount} prefab eklendi");
            Debug.Log($"📊 Toplam spawnable prefabs: {spawnablePrefabs.Count}");
            Debug.Log("═══════════════════════════════════════");

            // Sonuç göster
            string message = $"✅ {addedCount} prefab spawnable listesine eklendi!\n\n";
            message += $"Toplam spawnable prefabs: {spawnablePrefabs.Count}\n\n";

            if (addedCount > 0)
            {
                message += "ŞİMDİ:\n";
                message += "1. Scene'i kaydet (Ctrl+S)\n";
                message += "2. Build and Run yap\n";
                message += "3. Tekrar test et\n\n";
                message += "Artık 'sync message' hatası gelmeyecek! ✅";
            }
            else
            {
                message += "Tüm prefab'lar zaten listede.\n\n";
                message += "Eğer hata devam ediyorsa:\n";
                message += "Scene'i kaydetmeyi dene.";
            }

            EditorUtility.DisplayDialog("Spawnable Prefabs Düzeltildi", message, "Tamam");

            // Scene'i dirty yap
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }

        private void ShowSpawnableList()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📋 SPAWNABLE PREFABS LİSTESİ");
            Debug.Log("═══════════════════════════════════════\n");

            var netMgr = FindFirstObjectByType<NetworkManager>();

            if (netMgr == null)
            {
                Debug.LogError("❌ NetworkManager bulunamadı!");
                return;
            }

            if (netMgr.spawnPrefabs == null || netMgr.spawnPrefabs.Count == 0)
            {
                Debug.LogWarning("⚠️ Spawnable prefabs listesi BOŞ!");
                Debug.LogWarning("   Bu SORUN! Player prefab mutlaka listede olmalı!");
            }
            else
            {
                Debug.Log($"📊 Toplam {netMgr.spawnPrefabs.Count} spawnable prefab:\n");

                for (int i = 0; i < netMgr.spawnPrefabs.Count; i++)
                {
                    var prefab = netMgr.spawnPrefabs[i];
                    if (prefab != null)
                    {
                        var netId = prefab.GetComponent<NetworkIdentity>();
                        Debug.Log($"{i + 1}. {prefab.name} - NetworkIdentity: {(netId != null ? "✅" : "❌")}");
                    }
                    else
                    {
                        Debug.LogWarning($"{i + 1}. NULL prefab (kaldırılmalı!)");
                    }
                }
            }

            Debug.Log("\n═══════════════════════════════════════");

            // Player prefab kontrolü
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                bool playerInList = netMgr.spawnPrefabs.Contains(playerPrefab);
                if (playerInList)
                {
                    Debug.Log("✅ Player prefab spawnable listesinde VAR");
                }
                else
                {
                    Debug.LogError("❌ Player prefab spawnable listesinde YOK!");
                    Debug.LogError("   Bu yüzden 'sync message' hatası alıyorsun!");
                    Debug.LogError("   '⚡ SPAWNABLE PREFABS'I DÜZELT' butonuna tıkla!");
                }
            }
        }
    }
}
