using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Runtime'da URP kamera component'ini otomatik ekler
    /// Main Camera'ya ekle, her zaman URP uyumlu olur
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways] // Edit Mode ve Play Mode'da çalışır
    public class URPCameraAutoFixer : MonoBehaviour
    {
        private void OnEnable()
        {
            CheckAndAddURPComponent();
        }

        private void Awake()
        {
            CheckAndAddURPComponent();
        }

        private void CheckAndAddURPComponent()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) return;

            // URP Additional Camera Data kontrolü
            var cameraData = GetComponent<UniversalAdditionalCameraData>();
            
            if (cameraData == null)
            {
#if UNITY_EDITOR
                Debug.Log($"[URPCameraAutoFixer] Adding UniversalAdditionalCameraData to {gameObject.name}");
#endif
                gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Component eklendiğinde otomatik çalış
            CheckAndAddURPComponent();
        }
#endif
    }
}



