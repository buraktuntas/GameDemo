using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Ensures there is always an enabled camera rendering Display 1.
    /// Creates a lightweight fallback camera on client if none exists yet
    /// (e.g., before the local player spawns), and removes it once a real
    /// player camera becomes available. Prevents "Display 1 No cameras rendering".
    /// </summary>
    public class CameraBootstrap : MonoBehaviour
    {
        private Camera fallbackCam;
        private float nextCheckTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            // ✅ FIX: ALWAYS skip if URP bootstrap exists (URP handles everything better)
            if (FindFirstObjectByType<URPCameraBootstrap>() != null)
            {
                Debug.Log("⏭️ [CameraBootstrap] URPCameraBootstrap found - skipping non-URP bootstrap");
                return;
            }

            var go = new GameObject("[CameraBootstrap]");
            DontDestroyOnLoad(go);
            go.AddComponent<CameraBootstrap>();
            Debug.Log("✅ [CameraBootstrap] Created fallback camera bootstrap");
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            // Force re-check on scene change
            nextCheckTime = 0f;
        }

        private void Update()
        {
            // ✅ FIX: If URP bootstrap exists, destroy this and let URP handle it
            if (FindFirstObjectByType<URPCameraBootstrap>() != null)
            {
                if (fallbackCam != null)
                {
                    Destroy(fallbackCam.gameObject);
                }
                Destroy(gameObject);
                return;
            }

            if (Time.unscaledTime < nextCheckTime) return;
            nextCheckTime = Time.unscaledTime + 0.5f; // check twice per second

            bool hasActiveCam = HasActiveCamera(out Camera activeCam);

            if (!hasActiveCam)
            {
                EnsureFallback();
            }
            else
            {
                // If a real camera is active, remove fallback
                if (fallbackCam != null)
                {
                    Destroy(fallbackCam.gameObject);
                    fallbackCam = null;
                }
            }
        }

        private bool HasActiveCamera(out Camera activeCam)
        {
            activeCam = null;
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                {
                    activeCam = c;
                    return true;
                }
            }
            return false;
        }

        private void EnsureFallback()
        {
            if (fallbackCam == null)
            {
                var go = new GameObject("[FallbackCamera]");
                DontDestroyOnLoad(go);
                fallbackCam = go.AddComponent<Camera>();
                fallbackCam.clearFlags = CameraClearFlags.Skybox;
                fallbackCam.tag = "MainCamera";
                go.transform.position = new Vector3(0f, 1.6f, -5f);
                go.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

                // Do NOT add AudioListener here to avoid multiple listeners warning in build.
                // Local player camera will own the only active listener.
            }

            if (!fallbackCam.enabled)
                fallbackCam.enabled = true;
            // No listener management here
        }
    }
}
