using UnityEngine;

namespace TacticalCombat.UI
{
    public class CrosshairController : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        public float expansionSpeed = 5f;
        public float contractionSpeed = 3f;
        public float maxScale = 1.2f;
        public float minScale = 0.8f;

        private RectTransform crosshairRect;
        private Vector3 originalScale;
        private bool isExpanding = false;

        private void Awake()
        {
            crosshairRect = GetComponent<RectTransform>();
            originalScale = crosshairRect.localScale;
        }

        private void Update()
        {
            // Crosshair animasyonu
            if (isExpanding)
            {
                crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, 
                    originalScale * maxScale, expansionSpeed * Time.deltaTime);
            }
            else
            {
                crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, 
                    originalScale * minScale, contractionSpeed * Time.deltaTime);
            }
        }

        public void ExpandCrosshair()
        {
            isExpanding = true;
        }

        public void ContractCrosshair()
        {
            isExpanding = false;
        }
    }
}