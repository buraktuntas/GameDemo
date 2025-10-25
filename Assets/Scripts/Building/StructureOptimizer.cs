using UnityEngine;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Unity 6: Yapılar için otomatik GPU Instancing ve optimizasyon
    /// Yüzlerce yapı ile daha iyi performans
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public class StructureOptimizer : MonoBehaviour
    {
        [Header("Unity 6 Optimizations")]
        [SerializeField] private bool enableGPUInstancing = true;
        [SerializeField] private bool optimizeOnSpawn = true;

        private Renderer[] renderers;
        private bool isOptimized = false;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();

            if (optimizeOnSpawn)
            {
                OptimizeStructure();
            }
        }

        public void OptimizeStructure()
        {
            if (isOptimized) return;

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            // Unity 6: GPU Instancing
            if (enableGPUInstancing)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        // Her materyal için GPU instancing aktif et
                        var materials = renderer.sharedMaterials;
                        foreach (var material in materials)
                        {
                            if (material != null)
                            {
                                material.enableInstancing = true;
                            }
                        }

                        // Unity 6: Static shadow caster
                        renderer.staticShadowCaster = false; // Yapılar yıkılabilir, static değil
                    }
                }
            }

            isOptimized = true;
        }

        private void OnEnable()
        {
            if (!isOptimized && optimizeOnSpawn)
            {
                OptimizeStructure();
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Force Optimize")]
        private void ForceOptimize()
        {
            isOptimized = false;
            OptimizeStructure();
            Debug.Log($"[Unity 6] Structure optimized with GPU Instancing");
        }
        #endif
    }
}



