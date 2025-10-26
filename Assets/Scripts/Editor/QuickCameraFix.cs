using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Kamera sorunlarını hızlıca tespit ve düzelt
    /// </summary>
    public class QuickCameraFix : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Quick Camera Fix")]
        public static void ShowWindow()
        {
            GetWindow<QuickCameraFix>("Camera Fix");
        }

        private void OnGUI()
        {
            GUILayout.Label("Camera Fix", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Display 1 No cameras rendering hatası için:\n\n" +
                "Bu tool kamera sorunlarını tespit ve düzeltir.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Diagnose Camera Issues", GUILayout.Height(40)))
            {
                DiagnoseCameras();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Fallback Camera", GUILayout.Height(30)))
            {
                CreateFallbackCamera();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Check Player Prefab Camera", GUILayout.Height(30)))
            {
                CheckPlayerPrefabCamera();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Fix Duplicate AudioListeners", GUILayout.Height(30)))
            {
                FixDuplicateAudioListeners();
            }
        }

        private void FixDuplicateAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            Debug.Log($"=== AUDIOLISTENER FIX ===");
            Debug.Log($"Toplam AudioListener: {listeners.Length}");

            if (listeners.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "AudioListener Yok",
                    "Scene'de hiç AudioListener yok!\n\n" +
                    "En az 1 tane olmalı (ses için).",
                    "Tamam"
                );
                return;
            }

            if (listeners.Length == 1)
            {
                Debug.Log("✅ AudioListener sayısı doğru (1 tane)");
                EditorUtility.DisplayDialog(
                    "AudioListener OK",
                    "AudioListener sayısı doğru (1 tane).\n\n" +
                    "Sorun yok!",
                    "Tamam"
                );
                return;
            }

            // Birden fazla var - temizle
            Debug.LogWarning($"⚠️ {listeners.Length} AudioListener bulundu! (Olması gereken: 1)");

            foreach (var listener in listeners)
            {
                Debug.Log($"  - {listener.name} (Parent: {(listener.transform.parent != null ? listener.transform.parent.name : "None")})");
            }

            // İlk listener'ı tut
            AudioListener keepListener = listeners[0];
            Debug.Log($"Tutulan: {keepListener.name}");

            // Geri kalanları sil
            int removed = 0;
            for (int i = 1; i < listeners.Length; i++)
            {
                Debug.Log($"Siliniyor: {listeners[i].name}");
                DestroyImmediate(listeners[i]);
                removed++;
            }

            Debug.Log($"✅ {removed} duplicate AudioListener silindi");
            Debug.Log("======================");

            EditorUtility.DisplayDialog(
                "AudioListener Fixed",
                $"{removed} duplicate AudioListener silindi.\n\n" +
                "Artık sadece 1 tane var.",
                "Tamam"
            );
        }

        private void DiagnoseCameras()
        {
            Debug.Log("=== CAMERA DIAGNOSTIC ===");

            // Scene'deki tüm kameraları bul
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Debug.Log($"Total cameras in scene: {cameras.Length}");

            if (cameras.Length == 0)
            {
                Debug.LogError("❌ NO CAMERAS IN SCENE!");
                Debug.Log("Çözüm: 'Create Fallback Camera' butonuna tıkla");
                return;
            }

            foreach (var cam in cameras)
            {
                Debug.Log($"Camera: {cam.name}");
                Debug.Log($"  - Enabled: {cam.enabled}");
                Debug.Log($"  - GameObject Active: {cam.gameObject.activeInHierarchy}");
                Debug.Log($"  - Target Display: {cam.targetDisplay}");
                Debug.Log($"  - Depth: {cam.depth}");
                Debug.Log($"  - Tag: {cam.tag}");
                Debug.Log($"  - Position: {cam.transform.position}");
                Debug.Log($"  - Parent: {(cam.transform.parent != null ? cam.transform.parent.name : "None")}");
            }

            // AudioListener kontrolü
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            Debug.Log($"\nAudioListeners: {listeners.Length}");

            if (listeners.Length == 0)
            {
                Debug.LogWarning("⚠️ NO AUDIOLISTENER! Ses çıkmayabilir.");
            }

            // NetworkManager kontrolü
            var netMgr = FindFirstObjectByType<Mirror.NetworkManager>();
            if (netMgr != null)
            {
                Debug.Log($"\nNetworkManager: {netMgr.name}");
                Debug.Log($"  - Player Prefab: {(netMgr.playerPrefab != null ? netMgr.playerPrefab.name : "NULL")}");
                Debug.Log($"  - Network Active: {Mirror.NetworkServer.active || Mirror.NetworkClient.active}");
                Debug.Log($"  - Is Server: {Mirror.NetworkServer.active}");
                Debug.Log($"  - Is Client: {Mirror.NetworkClient.active}");
            }
            else
            {
                Debug.LogError("❌ NetworkManager bulunamadı!");
            }

            Debug.Log("======================");

            EditorUtility.DisplayDialog(
                "Camera Diagnostic",
                $"Toplam Kamera: {cameras.Length}\n" +
                $"AudioListener: {listeners.Length}\n" +
                $"NetworkManager: {(netMgr != null ? "✅" : "❌")}\n\n" +
                "Console'da detaylı rapor var.",
                "Tamam"
            );
        }

        private void CreateFallbackCamera()
        {
            // Scene'de kamera var mı kontrol et
            Camera existingCam = FindFirstObjectByType<Camera>();
            if (existingCam != null)
            {
                bool create = EditorUtility.DisplayDialog(
                    "Kamera Zaten Var",
                    $"Scene'de zaten kamera var: {existingCam.name}\n\n" +
                    "Yine de fallback kamera oluşturulsun mu?",
                    "Evet",
                    "Hayır"
                );

                if (!create) return;
            }

            // Fallback kamera oluştur
            GameObject camGO = new GameObject("FallbackCamera");
            Camera cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();

            cam.transform.position = new Vector3(0, 1.6f, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.depth = -1; // En düşük depth (player kamerası gelince override olur)

            camGO.tag = "MainCamera";

            Debug.Log("✅ Fallback Camera oluşturuldu!");
            Debug.Log("⚠️ Bu geçici bir çözüm - Player spawn olunca kendi kamerasını kullanmalı");

            Selection.activeGameObject = camGO;

            EditorUtility.DisplayDialog(
                "Fallback Camera Oluşturuldu",
                "Geçici kamera oluşturuldu.\n\n" +
                "Player spawn olunca kendi kamerasını kullanacak.\n\n" +
                "Şimdi Play tuşuna basabilirsin.",
                "Tamam"
            );
        }

        private void CheckPlayerPrefabCamera()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Yok",
                    "Assets/Prefabs/Player.prefab bulunamadı!",
                    "Tamam"
                );
                return;
            }

            Debug.Log("=== PLAYER PREFAB CAMERA CHECK ===");

            // Prefab'taki kamerayı kontrol et
            Camera playerCam = playerPrefab.GetComponentInChildren<Camera>();

            if (playerCam == null)
            {
                Debug.LogError("❌ Player prefab'ta kamera YOK!");
                EditorUtility.DisplayDialog(
                    "Kamera Yok",
                    "Player prefab'ta kamera bulunamadı!\n\n" +
                    "Ultimate Setup'ı çalıştır veya manuel olarak kamera ekle.",
                    "Tamam"
                );
                return;
            }

            Debug.Log($"✅ Player Camera: {playerCam.name}");
            Debug.Log($"  - Enabled: {playerCam.enabled}");
            Debug.Log($"  - Target Display: {playerCam.targetDisplay}");
            Debug.Log($"  - Tag: {playerCam.tag}");
            Debug.Log($"  - Depth: {playerCam.depth}");
            Debug.Log($"  - Local Position: {playerCam.transform.localPosition}");

            // FPSController'da referans var mı?
            var fps = playerPrefab.GetComponent<Player.FPSController>();
            if (fps != null)
            {
                SerializedObject so = new SerializedObject(fps);
                var camProp = so.FindProperty("playerCamera");
                Debug.Log($"FPSController.playerCamera: {(camProp.objectReferenceValue != null ? "✅ Atanmış" : "❌ NULL")}");
            }

            // WeaponSystem'de referans var mı?
            var ws = playerPrefab.GetComponent<Combat.WeaponSystem>();
            if (ws != null)
            {
                SerializedObject so = new SerializedObject(ws);
                var camProp = so.FindProperty("playerCamera");
                Debug.Log($"WeaponSystem.playerCamera: {(camProp.objectReferenceValue != null ? "✅ Atanmış" : "❌ NULL")}");
            }

            Debug.Log("================================");

            EditorUtility.DisplayDialog(
                "Player Prefab Check",
                $"Kamera: {(playerCam != null ? "✅ VAR" : "❌ YOK")}\n" +
                $"FPSController ref: {(fps != null ? "✅" : "❌")}\n" +
                $"WeaponSystem ref: {(ws != null ? "✅" : "❌")}\n\n" +
                "Console'da detaylı bilgi var.",
                "Tamam"
            );
        }
    }
}
