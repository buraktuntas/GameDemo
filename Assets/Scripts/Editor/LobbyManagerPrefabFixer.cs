using UnityEngine;
using UnityEditor;
using Mirror;
using System.Security.Cryptography;
using System.Text;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: LobbyManager Prefab Fixer Bot - AAA Quality
    /// LobbyManager prefab'ını düzeltir ve NetworkManager'a ekler
    /// </summary>
    public class LobbyManagerPrefabFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🔧 Fix LobbyManager Prefab")]
        public static void ShowWindow()
        {
            GetWindow<LobbyManagerPrefabFixer>("LobbyManager Prefab Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("🔧 LOBBYMANAGER PREFAB FIXER", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu bot şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("✅ LobbyManager prefab'ını bulur");
            GUILayout.Label("✅ NetworkIdentity assetId'sini düzeltir");
            GUILayout.Label("✅ NetworkManager'a ekler");
            GUILayout.Label("✅ Spawnable Prefabs listesine ekler");
            GUILayout.Space(10);

            if (GUILayout.Button("🔧 FIX LOBBYMANAGER PREFAB", GUILayout.Height(50)))
            {
                FixLobbyManagerPrefab();
            }

            GUILayout.Space(10);
            GUILayout.Label("⚠️ Prefab'ı düzelttikten sonra Unity'yi yeniden başlatman gerekebilir", EditorStyles.helpBox);
        }

        private void FixLobbyManagerPrefab()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔧 LOBBYMANAGER PREFAB FIX BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            // 1. Find LobbyManager prefab
            string[] guids = AssetDatabase.FindAssets("LobbyManager t:Prefab");
            GameObject lobbyPrefab = null;

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                lobbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"✅ LobbyManager prefab found: {path}");
            }
            else
            {
                Debug.LogWarning("⚠️ LobbyManager prefab not found! Creating new one...");
                lobbyPrefab = CreateLobbyManagerPrefab();
            }

            if (lobbyPrefab == null)
            {
                Debug.LogError("❌ Failed to find or create LobbyManager prefab!");
                return;
            }

            // 2. Check and fix NetworkIdentity
            bool needsSave = false;
            NetworkIdentity netIdentity = lobbyPrefab.GetComponent<NetworkIdentity>();
            
            if (netIdentity == null)
            {
                Debug.LogWarning("⚠️ LobbyManager prefab has no NetworkIdentity! Adding...");
                netIdentity = lobbyPrefab.AddComponent<NetworkIdentity>();
                needsSave = true;
            }

            // 3. Check and fix assetId
            if (netIdentity != null)
            {
                SerializedObject serializedIdentity = new SerializedObject(netIdentity);
                SerializedProperty assetIdProperty = serializedIdentity.FindProperty("m_AssetId");
                
                if (assetIdProperty != null)
                {
                    ulong currentAssetId = assetIdProperty.ulongValue;
                    Debug.Log($"📋 Current assetId: {currentAssetId}");

                    if (currentAssetId == 0)
                    {
                        Debug.LogWarning("⚠️ AssetId is 0! This will cause spawn failures.");
                        Debug.Log("   Attempting to regenerate assetId...");
                        
                        // ✅ CRITICAL: Unity generates assetId based on prefab GUID
                        // We need to force Unity to regenerate it by marking the prefab as dirty
                        // and using Unity's internal system
                        EditorUtility.SetDirty(lobbyPrefab);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(lobbyPrefab));
                        
                        // Refresh and check again
                        serializedIdentity.Update();
                        assetIdProperty = serializedIdentity.FindProperty("m_AssetId");
                        ulong newAssetId = assetIdProperty.ulongValue;
                        
                        if (newAssetId == 0)
                        {
                            // If still 0, try manual generation using prefab GUID
                            string prefabPath = AssetDatabase.GetAssetPath(lobbyPrefab);
                            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                            
                            if (!string.IsNullOrEmpty(guid))
                            {
                                // Use GUID hash to generate assetId
                                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(guid));
                                ulong generatedId = System.BitConverter.ToUInt64(hash, 0);
                                
                                // Ensure it's not 0
                                if (generatedId == 0) generatedId = 1;
                                
                                assetIdProperty.ulongValue = generatedId;
                                serializedIdentity.ApplyModifiedProperties();
                                
                                Debug.Log($"✅ Generated assetId from GUID: {generatedId}");
                                needsSave = true;
                            }
                            else
                            {
                                Debug.LogError("❌ Could not get prefab GUID! AssetId cannot be generated.");
                            }
                        }
                        else
                        {
                            Debug.Log($"✅ AssetId regenerated: {newAssetId}");
                            needsSave = true;
                        }
                    }
                    else
                    {
                        Debug.Log($"✅ AssetId is valid: {currentAssetId}");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Could not find 'm_AssetId' property in NetworkIdentity");
                }
            }

            // 4. Ensure prefab is saved
            if (needsSave)
            {
                EditorUtility.SetDirty(lobbyPrefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("✅ Prefab saved");
            }

            // 5. Find NetworkManager and add prefab
            NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogWarning("⚠️ NetworkManager not found in scene! Prefab is fixed but not added to NetworkManager.");
                Debug.LogWarning("   Please add LobbyManager prefab to NetworkManager manually in Inspector.");
                return;
            }

            // 6. Add to NetworkManager's lobbyManagerPrefab field
            SerializedObject serializedManager = new SerializedObject(networkManager);
            SerializedProperty lobbyPrefabProperty = serializedManager.FindProperty("lobbyManagerPrefab");
            
            if (lobbyPrefabProperty != null)
            {
                if (lobbyPrefabProperty.objectReferenceValue != lobbyPrefab)
                {
                    lobbyPrefabProperty.objectReferenceValue = lobbyPrefab;
                    serializedManager.ApplyModifiedProperties();
                    Debug.Log("✅ LobbyManager prefab assigned to NetworkManager");
                }
                else
                {
                    Debug.Log("✅ LobbyManager prefab already assigned to NetworkManager");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find 'lobbyManagerPrefab' property in NetworkManager");
            }

            // 7. Add to spawnPrefabs list
            SerializedProperty spawnPrefabsProperty = serializedManager.FindProperty("spawnPrefabs");
            if (spawnPrefabsProperty != null)
            {
                bool alreadyInList = false;
                for (int i = 0; i < spawnPrefabsProperty.arraySize; i++)
                {
                    SerializedProperty element = spawnPrefabsProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == lobbyPrefab)
                    {
                        alreadyInList = true;
                        break;
                    }
                }

                if (!alreadyInList)
                {
                    spawnPrefabsProperty.arraySize++;
                    SerializedProperty newElement = spawnPrefabsProperty.GetArrayElementAtIndex(spawnPrefabsProperty.arraySize - 1);
                    newElement.objectReferenceValue = lobbyPrefab;
                    serializedManager.ApplyModifiedProperties();
                    Debug.Log("✅ LobbyManager prefab added to spawnPrefabs list");
                }
                else
                {
                    Debug.Log("✅ LobbyManager prefab already in spawnPrefabs list");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find 'spawnPrefabs' property in NetworkManager");
            }

            // 8. Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ LOBBYMANAGER PREFAB FIX TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("2. Test et: Host > LobbyManager spawn edilmeli");
            Debug.Log("3. Client join > LobbyManager bulunmalı");
            Debug.Log("");
        }

        private GameObject CreateLobbyManagerPrefab()
        {
            Debug.Log("📦 Creating new LobbyManager prefab...");

            // Create GameObject
            GameObject lobbyManagerGO = new GameObject("LobbyManager");

            // Add NetworkIdentity
            NetworkIdentity netIdentity = lobbyManagerGO.AddComponent<NetworkIdentity>();
            netIdentity.serverOnly = false; // Client'lar da görebilmeli

            // Add LobbyManager component
            var lobbyManager = lobbyManagerGO.AddComponent<TacticalCombat.Network.LobbyManager>();

            // Create Prefabs folder if not exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Save as prefab
            string prefabPath = "Assets/Prefabs/LobbyManager.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(lobbyManagerGO, prefabPath);
            DestroyImmediate(lobbyManagerGO);

            if (prefab != null)
            {
                Debug.Log($"✅ LobbyManager prefab created: {prefabPath}");
                
                // Force assetId generation
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                
                return prefab;
            }
            else
            {
                Debug.LogError("❌ Failed to create LobbyManager prefab!");
                return null;
            }
        }
    }
}

