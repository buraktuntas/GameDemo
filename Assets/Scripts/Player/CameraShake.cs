using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Camera shake effect for hit feedback
    /// </summary>
    public class CameraShake : NetworkBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeIntensity = 0.5f;
        [SerializeField] private float decreaseFactor = 1.5f;

        private Camera playerCamera;
        private Vector3 originalPosition;
        private float shakeTimer = 0f;
        private float currentIntensity = 0f;

        private void Awake()
        {
            // Will be set from FPSController
        }

        public void SetCamera(Camera cam)
        {
            playerCamera = cam;
            if (playerCamera != null)
            {
                originalPosition = playerCamera.transform.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer || playerCamera == null) return;

            if (shakeTimer > 0)
            {
                // Apply shake
                playerCamera.transform.localPosition = originalPosition + Random.insideUnitSphere * currentIntensity;

                // Decrease shake over time
                shakeTimer -= Time.deltaTime * decreaseFactor;
                currentIntensity = shakeIntensity * (shakeTimer / shakeDuration);
            }
            else
            {
                // Reset to original position
                shakeTimer = 0f;
                if (playerCamera.transform.localPosition != originalPosition)
                {
                    playerCamera.transform.localPosition = originalPosition;
                }
            }
        }

        /// <summary>
        /// Trigger camera shake (call from Health component)
        /// </summary>
        public void Shake(float intensity = 1f)
        {
            if (!isLocalPlayer) return;

            shakeTimer = shakeDuration;
            currentIntensity = shakeIntensity * intensity;

            if (playerCamera != null)
            {
                originalPosition = playerCamera.transform.localPosition;
            }
        }

        /// <summary>
        /// Quick shake for hit feedback
        /// </summary>
        public void ShakeOnHit(int damage, int maxHealth)
        {
            // Scale shake based on damage percentage
            float damagePercent = (float)damage / maxHealth;
            float intensity = Mathf.Clamp(damagePercent * 2f, 0.3f, 1.5f);
            Shake(intensity);
        }
    }
}
