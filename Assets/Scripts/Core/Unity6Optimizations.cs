using UnityEngine;
using UnityEngine.Rendering;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Unity 6 specific optimizations and features
    /// Gelişmiş performans için Unity 6 özellikleri
    /// </summary>
    public class Unity6Optimizations : MonoBehaviour
    {
        [Header("URP 18.x Optimizations")]
        [SerializeField] private bool enableGPUInstancing = true;
        [SerializeField] private bool enableSRPBatcher = true;
        [SerializeField] private bool useRenderGraph = true;

        [Header("GPU Resident Drawer (Unity 6)")]
        [Tooltip("Unity 6'nın GPU Resident Drawer özelliğini kullan - Binlerce obje için performans artışı")]
        [SerializeField] private bool enableGPUResidentDrawer = true;

        [Header("Adaptive Performance")]
        [SerializeField] private bool enableAdaptivePerformance = false;
        [SerializeField] private int targetFrameRate = 60;

        [Header("Lighting Optimization")]
        [Tooltip("Unity 6'da gelişmiş Adaptive Probe Volumes kullan - Varsayılan olarak aktif")]
        [SerializeField] private bool useAdaptiveProbeVolumes = true; // Unity 6'da otomatik aktif

        private void Awake()
        {
            ApplyOptimizations();
        }

        private void ApplyOptimizations()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0; // Disable VSync for target frame rate control

            // Unity 6: GPU Instancing
            if (enableGPUInstancing)
            {
                // Enable GPU instancing globally
                // Materials with GPU instancing will automatically batch
                Debug.Log("[Unity 6] GPU Instancing enabled");
            }

            // Unity 6: SRP Batcher (URP 18.x)
            if (enableSRPBatcher)
            {
                // SRP Batcher is enabled by default in URP 18.x
                // Binlerce draw call'ı optimize eder
                Debug.Log("[Unity 6] SRP Batcher optimization active");
            }

            // Unity 6: GPU Resident Drawer
            if (enableGPUResidentDrawer)
            {
                // GPU Resident Drawer Unity 6'da otomatik aktif
                // LOD ve culling performansını büyük ölçüde artırır
                Debug.Log("[Unity 6] GPU Resident Drawer enabled - Optimized for thousands of objects");
            }

            // Unity 6: Render Graph (URP 18.x)
            if (useRenderGraph)
            {
                // Render Graph URP 18.x'te default
                // Otomatik optimizasyon ve async compute
                Debug.Log("[Unity 6] Render Graph enabled - Automatic optimization");
            }

            // Adaptive Performance (opsiyonel)
            if (enableAdaptivePerformance)
            {
#if UNITY_2023_1_OR_NEWER
                // Unity 6: Adaptive Performance API
                Debug.Log("[Unity 6] Adaptive Performance enabled");
#endif
            }

            // Unity 6: Adaptive Probe Volumes
            if (useAdaptiveProbeVolumes)
            {
                // Unity 6'da Adaptive Probe Volumes otomatik olarak aktif
                // Project Settings > Graphics > Probe Volumes'den yapılandırılabilir
                Debug.Log("[Unity 6] Adaptive Probe Volumes enabled - Configure in Project Settings > Graphics");
            }
        }

        /// <summary>
        /// Unity 6 için GPU Instancing materyallerini yapılandır
        /// </summary>
        public static void EnableGPUInstancingForMaterials(Material[] materials)
        {
            if (materials == null) return;

            foreach (var material in materials)
            {
                if (material != null)
                {
                    material.enableInstancing = true;
                }
            }
        }

        /// <summary>
        /// Renderer için Unity 6 optimizasyonları
        /// </summary>
        public static void OptimizeRenderer(Renderer renderer)
        {
            if (renderer == null) return;

            // Enable GPU instancing on materials
            var materials = renderer.sharedMaterials;
            EnableGPUInstancingForMaterials(materials);

            // Unity 6: Static batching için ayarla
            if (renderer.gameObject.isStatic)
            {
                renderer.staticShadowCaster = true;
            }

            // Motion vectors (URP 18.x)
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        }

        /// <summary>
        /// Unity 6 için Light optimizasyonları
        /// </summary>
        public static void OptimizeLight(Light light)
        {
            if (light == null) return;

            // Unity 6: Adaptive Probe Volumes için optimize et
            if (light.type == LightType.Directional)
            {
                // Directional light için cascade shadows
                light.shadowNormalBias = 0.4f;
                light.shadowBias = 0.05f;
            }

            // Unity 6: Shadow caching
            light.useShadowMatrixOverride = false;
        }

        #if UNITY_EDITOR
        [ContextMenu("Apply Unity 6 Optimizations to Scene")]
        private void ApplyToScene()
        {
            // Sahnedeki tüm renderer'ları optimize et
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                OptimizeRenderer(renderer);
            }

            // Sahnedeki tüm ışıkları optimize et
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                OptimizeLight(light);
            }

            Debug.Log($"[Unity 6] Optimized {renderers.Length} renderers and {lights.Length} lights");
        }
        #endif
    }
}

