using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Automatically fixes "No cameras rendering" issue in scenes
    /// Adds proper Main Camera or validates existing camera setup
    /// </summary>
    public class SceneCameraFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Fix Scene Camera")]
        public static void ShowWindow()
        {
            GetWindow<SceneCameraFixer>("Camera Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎥 Scene Camera Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool \"Display 1 No cameras rendering\" hatasını düzeltir.\n\n" +
                "Seçenekler:\n" +
                "• Scene'e Main Camera ekle\n" +
                "• Player prefab kamerasını kontrol et\n" +
                "• Mevcut kameraları validate et",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Check current scene status
            Camera mainCamera = Camera.main;
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            GUILayout.Label($"📊 Current Scene Status:", EditorStyles.boldLabel);
            GUILayout.Label($"   Main Camera: {(mainCamera != null ? "✅ Found" : "❌ Missing")}");
            GUILayout.Label($"   Total Cameras: {allCameras.Length}");
            GUILayout.Space(10);

            if (allCameras.Length > 0)
            {
                GUILayout.Label("Existing Cameras:", EditorStyles.boldLabel);
                foreach (var cam in allCameras)
                {
                    string status = cam.enabled ? "🟢" : "🔴";
                    string tag = cam.CompareTag("MainCamera") ? " [MainCamera]" : "";
                    GUILayout.Label($"   {status} {cam.name}{tag}");
                }
                GUILayout.Space(10);
            }

            // Action buttons
            if (mainCamera == null)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🎥 Add Main Camera to Scene", GUILayout.Height(40)))
                {
                    AddMainCamera();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("🔧 Validate Main Camera Setup", GUILayout.Height(30)))
                {
                    ValidateMainCamera(mainCamera);
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(5);

            if (GUILayout.Button("🎮 Check Player Prefab Camera", GUILayout.Height(30)))
            {
                CheckPlayerPrefabCamera();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("🔍 Find All Cameras in Scene", GUILayout.Height(30)))
            {
                FindAndSelectAllCameras();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📘 Open Documentation", GUILayout.Height(25)))
            {
                System.Diagnostics.Process.Start("CRITICAL_ISSUES_SUMMARY.md");
            }
        }

        private void AddMainCamera()
        {
            // Create camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            AudioListener audioListener = cameraObj.AddComponent<AudioListener>();

            // Configure camera
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;

            // Position for overview (temporary - will be replaced by player camera)
            cameraObj.transform.position = new Vector3(0f, 10f, -10f);
            cameraObj.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // Select camera
            Selection.activeGameObject = cameraObj;

            EditorUtility.DisplayDialog("Camera Added",
                "Main Camera başarıyla eklendi!\n\n" +
                "Position: (0, 10, -10)\n" +
                "Rotation: (45, 0, 0)\n\n" +
                "Bu geçici bir kamera. Multiplayer'da player prefab'ın " +
                "kendi kamerası kullanılacak.\n\n" +
                "Test için Play'e basabilirsin!",
                "OK");

            Debug.Log("✅ [SceneCameraFixer] Main Camera added to scene");
        }

        private void ValidateMainCamera(Camera mainCamera)
        {
            bool issues = false;
            string report = "Main Camera Validation:\n\n";

            // Check if enabled
            if (!mainCamera.enabled)
            {
                report += "❌ Camera is DISABLED\n";
                issues = true;
            }
            else
            {
                report += "✅ Camera is enabled\n";
            }

            // Check tag
            if (!mainCamera.CompareTag("MainCamera"))
            {
                report += $"⚠️ Tag is '{mainCamera.tag}', should be 'MainCamera'\n";
                issues = true;
            }
            else
            {
                report += "✅ Tag is correct (MainCamera)\n";
            }

            // Check AudioListener
            AudioListener listener = mainCamera.GetComponent<AudioListener>();
            if (listener == null)
            {
                report += "❌ Missing AudioListener component\n";
                issues = true;
            }
            else if (!listener.enabled)
            {
                report += "⚠️ AudioListener is disabled\n";
                issues = true;
            }
            else
            {
                report += "✅ AudioListener present and enabled\n";
            }

            // Check multiple AudioListeners
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (allListeners.Length > 1)
            {
                report += $"⚠️ WARNING: {allListeners.Length} AudioListeners found (should be 1)\n";
                issues = true;
            }

            // Check clear flags
            if (mainCamera.clearFlags == CameraClearFlags.Nothing)
            {
                report += "⚠️ Clear Flags is 'Nothing' (might show black screen)\n";
                issues = true;
            }

            // Auto-fix option
            if (issues)
            {
                bool fix = EditorUtility.DisplayDialog("Camera Issues Detected",
                    report + "\nOtomatik düzeltmek ister misin?",
                    "Evet, düzelt",
                    "Hayır");

                if (fix)
                {
                    AutoFixCamera(mainCamera);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Camera Validation",
                    report + "\n✅ Tüm kontroller geçti!",
                    "OK");
            }

            Debug.Log(report);
        }

        private void AutoFixCamera(Camera camera)
        {
            // Enable camera
            camera.enabled = true;

            // Set tag
            if (!camera.CompareTag("MainCamera"))
            {
                camera.tag = "MainCamera";
            }

            // Add AudioListener if missing
            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = camera.gameObject.AddComponent<AudioListener>();
            }
            listener.enabled = true;

            // Fix clear flags
            if (camera.clearFlags == CameraClearFlags.Nothing)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
            }

            // Disable extra AudioListeners
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (allListeners.Length > 1)
            {
                foreach (var al in allListeners)
                {
                    if (al != listener)
                    {
                        al.enabled = false;
                        Debug.Log($"⚠️ Disabled extra AudioListener on {al.gameObject.name}");
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Camera Fixed",
                "Kamera sorunları düzeltildi!\n\n" +
                "Değişiklikler:\n" +
                "✅ Camera enabled\n" +
                "✅ Tag set to MainCamera\n" +
                "✅ AudioListener added/enabled\n" +
                "✅ Clear flags set to Skybox\n" +
                "✅ Extra AudioListeners disabled",
                "OK");

            Debug.Log("✅ [SceneCameraFixer] Camera auto-fixed successfully");
        }

        private void CheckPlayerPrefabCamera()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Player Prefab Not Found",
                    $"Player prefab bulunamadı:\n{prefabPath}\n\n" +
                    "Prefab yolunu kontrol edin.",
                    "OK");
                return;
            }

            // Check for FPSController
            var fpsController = playerPrefab.GetComponent<TacticalCombat.Player.FPSController>();
            if (fpsController == null)
            {
                EditorUtility.DisplayDialog("FPSController Not Found",
                    "Player prefab'da FPSController component'i yok.\n\n" +
                    "Hareket için hangi controller kullanıyorsun?",
                    "OK");
                return;
            }

            // Check for camera
            Camera[] cameras = playerPrefab.GetComponentsInChildren<Camera>();
            string report = $"Player Prefab Analysis:\n\n";
            report += $"Prefab: {playerPrefab.name}\n";
            report += $"FPSController: ✅ Found\n";
            report += $"Cameras: {cameras.Length}\n\n";

            if (cameras.Length == 0)
            {
                report += "❌ Player prefab'da kamera yok!\n\n";
                report += "Çözüm:\n";
                report += "1. Player prefab'ı aç\n";
                report += "2. Player objesine child olarak Camera ekle\n";
                report += "3. Camera position: (0, 1.6, 0)\n";
                report += "4. FPSController.playerCamera'ya assign et\n";
            }
            else
            {
                report += "Kameralar:\n";
                foreach (var cam in cameras)
                {
                    report += $"   • {cam.name} {(cam.enabled ? "🟢" : "🔴")}\n";
                }
            }

            EditorUtility.DisplayDialog("Player Prefab Camera Check", report, "OK");
            Debug.Log(report);

            // Select prefab
            Selection.activeObject = playerPrefab;
        }

        private void FindAndSelectAllCameras()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            if (cameras.Length == 0)
            {
                EditorUtility.DisplayDialog("No Cameras Found",
                    "Scene'de hiç kamera bulunamadı!\n\n" +
                    "'Add Main Camera' butonunu kullan.",
                    "OK");
                return;
            }

            // Select all cameras
            Selection.objects = System.Array.ConvertAll(cameras, cam => cam.gameObject);

            string report = $"Found {cameras.Length} camera(s):\n\n";
            foreach (var cam in cameras)
            {
                string status = cam.enabled ? "🟢" : "🔴";
                string tag = cam.CompareTag("MainCamera") ? " [MAIN]" : "";
                string audioListener = cam.GetComponent<AudioListener>() != null ? " [Audio]" : "";
                report += $"{status} {cam.name}{tag}{audioListener}\n";
                report += $"   Position: {cam.transform.position}\n";
                report += $"   Enabled: {cam.enabled}\n\n";
            }

            EditorUtility.DisplayDialog("Camera Search Results", report, "OK");
            Debug.Log(report);
        }
    }
}
