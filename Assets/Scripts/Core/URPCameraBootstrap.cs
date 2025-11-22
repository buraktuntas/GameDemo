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

            // ✅ CRITICAL FIX: Don't add AudioListener to bootstrap camera
            // Player camera will have its own AudioListener
            // This prevents "2 audio listeners" warning
            
            DontDestroyOnLoad(go);

            EnsureUrpAdditionalCameraData(bootstrapCamera);

            Debug.Log("🎥 [URPCameraBootstrap] Bootstrap camera created (will be destroyed when player camera is ready)");
        }

        // ✅ CRITICAL PERFORMANCE FIX: Cache FPSController reference instead of FindObjectsByType every frame
        private TacticalCombat.Player.FPSController cachedLocalFPS;
        private float nextCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.5f; // Check every 0.5s instead of every frame
        
        private void Update()
        {
            // ✅ PERFORMANCE: Time-based check instead of frame-based (more consistent)
            if (Time.unscaledTime < nextCheckTime) return;
            nextCheckTime = Time.unscaledTime + CHECK_INTERVAL;

            // ✅ PERFORMANCE: Use cached reference if available
            if (cachedLocalFPS == null)
            {
                // Only search once, then cache
                var allFPS = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
                foreach (var fps in allFPS)
                {
                    if (fps != null && fps.isLocalPlayer)
                    {
                        cachedLocalFPS = fps;
                        break; // Found it, stop searching
                    }
                }
            }
            
            // Check if cached FPS controller has camera ready
            if (cachedLocalFPS != null && cachedLocalFPS.playerCamera != null)
            {
                // ✅ CRITICAL FIX: Check if player camera is actually enabled and active
                if (cachedLocalFPS.playerCamera.enabled && cachedLocalFPS.playerCamera.gameObject.activeInHierarchy)
                {
                    EnsureUrpAdditionalCameraData(cachedLocalFPS.playerCamera);

                    // ✅ FIX: Immediately disable bootstrap camera before destroying
                    if (bootstrapCamera != null)
                    {
                        bootstrapCamera.enabled = false;
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
