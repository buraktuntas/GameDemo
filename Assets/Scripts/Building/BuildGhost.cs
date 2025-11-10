using UnityEngine;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    public class BuildGhost : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private float ghostAlpha = 0.15f; // ✅ Less intrusive - very transparent (was 0.3f, now 0.15f)

        private bool isValid = false;
        private bool lastValidState = false; // Track state to avoid unnecessary updates

        public void SetValid(bool valid)
        {
            isValid = valid;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (renderers == null || renderers.Length == 0) return;

            // ✅ CRITICAL FIX: Only update if state changed (prevent unnecessary material assignments)
            if (lastValidState == isValid) return;
            lastValidState = isValid;

            Material mat = isValid ? validMaterial : invalidMaterial;
            if (mat != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        // ✅ CRITICAL FIX: Use sharedMaterial to prevent memory leak
                        // rend.material creates new instance every time!
                        rend.sharedMaterial = mat;
                        
                        // ✅ Make ghost more transparent (less intrusive)
                        if (rend.material.HasProperty("_Color"))
                        {
                            Color color = rend.material.color;
                            color.a = ghostAlpha;
                            rend.material.color = color;
                        }
                        else if (rend.material.HasProperty("_BaseColor")) // URP
                        {
                            Color color = rend.material.GetColor("_BaseColor");
                            color.a = ghostAlpha;
                            rend.material.SetColor("_BaseColor", color);
                        }
                    }
                }
            }
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
    }
}



