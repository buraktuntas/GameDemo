using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Professional Lobby UI Setup Bot - AAA Quality
    /// Sıfırdan temiz, modern lobby UI oluşturur
    /// Her çalıştırmada önceki yapıyı temizler
    /// </summary>
    public class LobbyUISetupBot : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🎮 Setup Lobby UI (Professional Bot)")]
        public static void ShowWindow()
        {
            GetWindow<LobbyUISetupBot>("Lobby UI Setup Bot");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 PROFESSIONAL LOBBY UI SETUP BOT", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Bu bot şunları yapar:", EditorStyles.helpBox);
            GUILayout.Label("✅ Önceki LobbyUI yapılarını temizler");
            GUILayout.Label("✅ GameModeSelectionPanel altındaki eski PlayerListContainer'ı siler");
            GUILayout.Label("✅ Sıfırdan temiz LobbyUIController oluşturur");
            GUILayout.Label("✅ Modern, güzel UI tasarımı");
            GUILayout.Label("✅ Hem Host hem Client için çalışır");
            GUILayout.Label("✅ AAA kalitesinde kod");
            GUILayout.Space(10);

            if (GUILayout.Button("🧹 CLEAN & SETUP LOBBY UI", GUILayout.Height(50)))
            {
                CleanAndSetupLobbyUI();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🗑️ SADECE ESKİ YAPILARI TEMİZLE", GUILayout.Height(40)))
            {
                CleanOldStructuresOnly();
            }

            GUILayout.Space(10);
            GUILayout.Label("✅ Mevcut scene'deki Canvas kullanılacak veya yeni oluşturulacak", EditorStyles.helpBox);
            GUILayout.Label("⚠️ Scene'i kaydetmeyi unutma: File > Save Scene (Ctrl+S)", EditorStyles.helpBox);
        }

        private void CleanAndSetupLobbyUI()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🧹 LOBBY UI TEMİZLİK BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            int deletedCount = 0;

            // 1. Delete old LobbyUIController instances
            var oldControllers = FindObjectsByType<TacticalCombat.UI.LobbyUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var controller in oldControllers)
            {
                if (controller != null && controller.gameObject != null)
                {
                    Debug.Log($"🗑️ Deleting old LobbyUIController: {controller.gameObject.name}");
                    DestroyImmediate(controller.gameObject);
                    deletedCount++;
                }
            }

            // 2. Delete old LobbyUI instances (legacy)
            var oldLobbyUIs = FindObjectsByType<TacticalCombat.UI.LobbyUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lobbyUI in oldLobbyUIs)
            {
                if (lobbyUI != null && lobbyUI.gameObject != null)
                {
                    Debug.Log($"🗑️ Deleting old LobbyUI (legacy): {lobbyUI.gameObject.name}");
                    DestroyImmediate(lobbyUI.gameObject);
                    deletedCount++;
                }
            }

            // 3. Delete old LobbyCanvas (if exists and empty)
            GameObject lobbyCanvas = GameObject.Find("LobbyCanvas");
            if (lobbyCanvas != null)
            {
                // Check if canvas has only LobbyUIController or is empty
                bool canDelete = true;
                var children = lobbyCanvas.GetComponentsInChildren<Transform>();
                if (children.Length > 1) // More than just the canvas itself
                {
                    // Check if it only has runtime-created UI (LobbyPanel, etc.)
                    bool hasOnlyRuntimeUI = true;
                    foreach (var child in children)
                    {
                        if (child == lobbyCanvas.transform) continue;
                        if (child.name != "LobbyPanel" && 
                            child.name != "LobbyUIController" &&
                            !child.name.Contains("Lobby"))
                        {
                            hasOnlyRuntimeUI = false;
                            break;
                        }
                    }
                    canDelete = hasOnlyRuntimeUI;
                }

                if (canDelete)
                {
                    Debug.Log($"🗑️ Deleting old LobbyCanvas: {lobbyCanvas.name}");
                    DestroyImmediate(lobbyCanvas);
                    deletedCount++;
                }
                else
                {
                    Debug.Log($"⚠️ LobbyCanvas has other UI elements, keeping it");
                }
            }

            // 4. Delete old LobbyPanel GameObjects (orphaned)
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                
                // Delete orphaned LobbyPanel (not under a Canvas or LobbyUIController)
                if (obj.name == "LobbyPanel" || obj.name.Contains("LobbyPanel"))
                {
                    Transform parent = obj.transform.parent;
                    bool isOrphaned = true;
                    
                    if (parent != null)
                    {
                        // Check if parent is Canvas or LobbyUIController
                        if (parent.GetComponent<Canvas>() != null || 
                            parent.GetComponent<TacticalCombat.UI.LobbyUIController>() != null)
                        {
                            isOrphaned = false;
                        }
                    }
                    
                    if (isOrphaned)
                    {
                        Debug.Log($"🗑️ Deleting orphaned LobbyPanel: {obj.name}");
                        DestroyImmediate(obj);
                        deletedCount++;
                    }
                }
            }

            // 5. ✅ NEW: Delete old PlayerListContainer under GameModeSelectionPanel
            deletedCount += CleanOldGameModeSelectionLobbyStructures();

            Debug.Log($"✅ Temizlik tamamlandı: {deletedCount} obje silindi");
            Debug.Log("");

            // 6. Create new LobbyUIController
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🎮 YENİ LOBBY UI OLUŞTURULUYOR...");
            Debug.Log("═══════════════════════════════════════");

            GameObject controllerObj = new GameObject("LobbyUIController");
            var newController = controllerObj.AddComponent<TacticalCombat.UI.LobbyUIController>();

            Debug.Log("✅ LobbyUIController created");

            // 7. Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ PROFESSIONAL LOBBY UI SETUP TAMAMLANDI!");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("2. Test et: Host > LobbyUI otomatik oluşturulacak");
            Debug.Log("3. Client join > LobbyUI otomatik oluşturulacak");
            Debug.Log("");
            Debug.Log("💡 NOT: LobbyUIController runtime'da otomatik olarak");
            Debug.Log("   tüm UI elementlerini oluşturur. Scene'de hazır");
            Debug.Log("   UI elementlerine gerek yok!");
            Debug.Log("");
        }

        /// <summary>
        /// ✅ NEW: Clean only old structures without creating new ones
        /// </summary>
        private void CleanOldStructuresOnly()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🗑️ SADECE ESKİ YAPILARI TEMİZLEME BAŞLIYOR...");
            Debug.Log("═══════════════════════════════════════");

            int deletedCount = 0;

            // Clean old LobbyUIController instances
            var oldControllers = FindObjectsByType<TacticalCombat.UI.LobbyUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var controller in oldControllers)
            {
                if (controller != null && controller.gameObject != null)
                {
                    Debug.Log($"🗑️ Deleting old LobbyUIController: {controller.gameObject.name}");
                    DestroyImmediate(controller.gameObject);
                    deletedCount++;
                }
            }

            // Clean old LobbyUI instances
            var oldLobbyUIs = FindObjectsByType<TacticalCombat.UI.LobbyUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lobbyUI in oldLobbyUIs)
            {
                if (lobbyUI != null && lobbyUI.gameObject != null)
                {
                    Debug.Log($"🗑️ Deleting old LobbyUI (legacy): {lobbyUI.gameObject.name}");
                    DestroyImmediate(lobbyUI.gameObject);
                    deletedCount++;
                }
            }

            // Clean GameModeSelectionPanel lobby structures
            deletedCount += CleanOldGameModeSelectionLobbyStructures();

            // Clean orphaned LobbyPanel
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                
                if (obj.name == "LobbyPanel" || obj.name.Contains("LobbyPanel"))
                {
                    Transform parent = obj.transform.parent;
                    bool isOrphaned = true;
                    
                    if (parent != null)
                    {
                        if (parent.GetComponent<Canvas>() != null || 
                            parent.GetComponent<TacticalCombat.UI.LobbyUIController>() != null)
                        {
                            isOrphaned = false;
                        }
                    }
                    
                    if (isOrphaned)
                    {
                        Debug.Log($"🗑️ Deleting orphaned LobbyPanel: {obj.name}");
                        DestroyImmediate(obj);
                        deletedCount++;
                    }
                }
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"✅ TEMİZLİK TAMAMLANDI: {deletedCount} obje silindi");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("");
            Debug.Log("📋 SONRAKI ADIMLAR:");
            Debug.Log("1. Scene'i kaydet: File > Save Scene (Ctrl+S)");
            Debug.Log("2. Oyunu çalıştır - LobbyUIController runtime'da otomatik oluşturulacak");
            Debug.Log("");
        }

        /// <summary>
        /// ✅ NEW: Clean old GameModeSelectionPanel lobby structures
        /// </summary>
        private int CleanOldGameModeSelectionLobbyStructures()
        {
            int deletedCount = 0;

            // Find GameModeSelectionPanel
            var gameModePanels = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var panel in gameModePanels)
            {
                if (panel == null) continue;
                if (panel.name != "GameModeSelectionPanel") continue;

                Debug.Log($"🔍 Found GameModeSelectionPanel: {panel.name}");

                // Find LobbySection
                Transform lobbySection = panel.transform.Find("LobbySection");
                if (lobbySection != null)
                {
                    Debug.Log($"🔍 Found LobbySection under GameModeSelectionPanel");

                    // Delete PlayerListContainer
                    Transform playerListContainer = lobbySection.Find("PlayerListContainer");
                    if (playerListContainer != null)
                    {
                        Debug.Log($"🗑️ Deleting old PlayerListContainer under GameModeSelectionPanel");
                        DestroyImmediate(playerListContainer.gameObject);
                        deletedCount++;
                    }

                    // Delete HostControlsPanel
                    Transform hostControls = lobbySection.Find("HostControlsPanel");
                    if (hostControls != null)
                    {
                        Debug.Log($"🗑️ Deleting old HostControlsPanel under GameModeSelectionPanel");
                        DestroyImmediate(hostControls.gameObject);
                        deletedCount++;
                    }

                    // Delete PlayerControlsPanel
                    Transform playerControls = lobbySection.Find("PlayerControlsPanel");
                    if (playerControls != null)
                    {
                        Debug.Log($"🗑️ Deleting old PlayerControlsPanel under GameModeSelectionPanel");
                        DestroyImmediate(playerControls.gameObject);
                        deletedCount++;
                    }

                    // Delete PlayerCountText
                    Transform playerCountText = lobbySection.Find("PlayerCountText");
                    if (playerCountText != null)
                    {
                        Debug.Log($"🗑️ Deleting old PlayerCountText under GameModeSelectionPanel");
                        DestroyImmediate(playerCountText.gameObject);
                        deletedCount++;
                    }

                    // Delete GameModeText
                    Transform gameModeText = lobbySection.Find("GameModeText");
                    if (gameModeText != null)
                    {
                        Debug.Log($"🗑️ Deleting old GameModeText under GameModeSelectionPanel");
                        DestroyImmediate(gameModeText.gameObject);
                        deletedCount++;
                    }

                    // Check if LobbySection is now empty (only has these runtime-created elements)
                    // If empty or only has these, we could delete it too, but let's keep it for now
                    // as it might be used by GameModeSelectionUI
                }
            }

            // Also search for orphaned PlayerListContainer anywhere in scene
            foreach (var obj in gameModePanels)
            {
                if (obj == null) continue;
                if (obj.name == "PlayerListContainer")
                {
                    // Check if it's under GameModeSelectionPanel or orphaned
                    Transform parent = obj.transform.parent;
                    bool isOrphaned = true;
                    
                    if (parent != null)
                    {
                        // Check if parent is LobbySection (old structure) or not under LobbyPanel
                        if (parent.name == "LobbySection" || 
                            (parent.parent != null && parent.parent.name == "GameModeSelectionPanel"))
                        {
                            isOrphaned = true; // This is old structure, delete it
                        }
                        else if (parent.name == "LobbyPanel" || 
                                 (parent.parent != null && parent.parent.name == "LobbyPanel"))
                        {
                            isOrphaned = false; // This is new structure, keep it
                        }
                    }
                    
                    if (isOrphaned)
                    {
                        Debug.Log($"🗑️ Deleting orphaned PlayerListContainer: {obj.name}");
                        DestroyImmediate(obj);
                        deletedCount++;
                    }
                }
            }

            return deletedCount;
        }
    }
}

