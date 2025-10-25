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

        private bool isValid = false;

        public void SetValid(bool valid)
        {
            isValid = valid;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (renderers == null || renderers.Length == 0) return;

            Material mat = isValid ? validMaterial : invalidMaterial;
            if (mat != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.material = mat;
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



