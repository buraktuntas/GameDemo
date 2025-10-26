using UnityEngine;
using UnityEngine.Rendering.Universal;
using Mirror;
using UnityEngine.Scripting;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Ensures there is always a URP-ready camera before any rendering occurs.
    /// Creates a lightweight bootstrap camera for menus/loading screens and
    /// removes it once the local player camera is available.
    /// </summary>
    [Preserve]
    public class URPCameraBootstrap : MonoBehaviour
    {
        private Camera bootstrapCamera;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrapper()
        {
            var go = new GameObject("URPCameraBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<URPCameraBootstrap>();
        }

        private void Awake()
        {
            // ✅ FIX: Never use Camera.main - always create bootstrap camera
            // This ensures URP doesn't complain and we have full control
            var go = new GameObject("BootstrapCamera");
            bootstrapCamera = go.AddComponent<Camera>();
            bootstrapCamera.clearFlags = CameraClearFlags.SolidColor;
            bootstrapCamera.backgroundColor = Color.black;
            bootstrapCamera.cullingMask = 0; // Render nothing to save performance
            bootstrapCamera.depth = -100; // Lowest priority

            var listener = go.AddComponent<AudioListener>();
            listener.enabled = true;

            DontDestroyOnLoad(go);

            EnsureUrpAdditionalCameraData(bootstrapCamera);

            Debug.Log("🎥 [URPCameraBootstrap] Bootstrap camera created (will be destroyed when player camera is ready)");
        }

        private void Update()
        {
            // ✅ FIX: Check more frequently during early frames (player spawn)
            int checkInterval = Time.frameCount < 600 ? 10 : 30; // First 10s: check every 10 frames, then every 30
            if (Time.frameCount % checkInterval != 0) return;

            // If a local player's FPS camera exists, ensure it's URP-ready and remove bootstrap
            var allFPS = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
            foreach (var fps in allFPS)
            {
                if (fps != null && fps.isLocalPlayer && fps.playerCamera != null)
                {
                    EnsureUrpAdditionalCameraData(fps.playerCamera);

                    // ✅ FIX: Immediately disable bootstrap camera before destroying
                    if (bootstrapCamera != null)
                    {
                        bootstrapCamera.enabled = false;

                        // Disable AudioListener to prevent conflicts
                        var listener = bootstrapCamera.GetComponent<AudioListener>();
                        if (listener != null)
                        {
                            listener.enabled = false;
                        }

                        Destroy(bootstrapCamera.gameObject);
                        bootstrapCamera = null;

                        Debug.Log("🎥 [URPCameraBootstrap] Bootstrap camera destroyed - player camera is ready");
                    }

                    Destroy(gameObject); // ✅ FIX: Destroy bootstrap completely
                    return;
                }
            }
        }

        private static void EnsureUrpAdditionalCameraData(Camera cam)
        {
            if (cam == null) return;
            if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
        }
    }
}
