using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    /// <summary>
    /// ✅ AAA QUALITY: Perlin Noise Camera Shake
    /// Provides smooth, heavy, and realistic shake effects.
    /// </summary>
    public class CameraShake : NetworkBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float traumaDecreaseSpeed = 2.0f;
        [SerializeField] private float maxAngle = 5.0f;
        [SerializeField] private float maxOffset = 0.2f;
        [SerializeField] private float noiseSpeed = 15.0f;

        private Camera playerCamera;
        private float trauma = 0f; // 0 to 1
        private float seed;
        private Vector3 originalLocalPos;
        private Quaternion originalLocalRot;
        
        // Cache for performance
        private float timeCounter = 0f;

        private void Awake()
        {
            seed = Random.value * 100f;
        }

        public void SetCamera(Camera cam)
        {
            playerCamera = cam;
            if (playerCamera != null)
            {
                originalLocalPos = playerCamera.transform.localPosition;
                originalLocalRot = playerCamera.transform.localRotation;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || playerCamera == null) return;

            if (trauma > 0)
            {
                // Decrease trauma over time
                trauma = Mathf.Clamp01(trauma - Time.deltaTime * traumaDecreaseSpeed);

                // Shake = Trauma^2 (Non-linear falloff feels better)
                float shake = trauma * trauma;

                // Calculate noise based on time and seed
                timeCounter += Time.deltaTime * noiseSpeed;
                
                // Perlin Noise for smooth random movement
                float yaw = (Mathf.PerlinNoise(seed, timeCounter) - 0.5f) * 2f * maxAngle * shake;
                float pitch = (Mathf.PerlinNoise(seed + 1f, timeCounter) - 0.5f) * 2f * maxAngle * shake;
                float roll = (Mathf.PerlinNoise(seed + 2f, timeCounter) - 0.5f) * 2f * maxAngle * shake;

                float offsetX = (Mathf.PerlinNoise(seed + 3f, timeCounter) - 0.5f) * 2f * maxOffset * shake;
                float offsetY = (Mathf.PerlinNoise(seed + 4f, timeCounter) - 0.5f) * 2f * maxOffset * shake;

                // Apply to camera
                playerCamera.transform.localRotation = originalLocalRot * Quaternion.Euler(pitch, yaw, roll);
                playerCamera.transform.localPosition = originalLocalPos + new Vector3(offsetX, offsetY, 0);
            }
            else
            {
                // Reset smoothly if needed, or just snap if close enough
                if (playerCamera.transform.localPosition != originalLocalPos)
                {
                    playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalLocalPos, Time.deltaTime * 5f);
                    playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, originalLocalRot, Time.deltaTime * 5f);
                }
            }
        }

        /// <summary>
        /// Add trauma to the camera (0 to 1)
        /// </summary>
        public void AddTrauma(float amount)
        {
            if (!isLocalPlayer) return;
            
            // Ensure camera is set
            if (playerCamera == null)
            {
                var fps = GetComponent<FPSController>();
                if (fps != null) SetCamera(fps.playerCamera);
            }

            trauma = Mathf.Clamp01(trauma + amount);
        }

        /// <summary>
        /// Legacy support for existing calls
        /// </summary>
        public void Shake(float intensity = 0.5f)
        {
            AddTrauma(intensity);
        }

        /// <summary>
        /// Quick shake for hit feedback
        /// </summary>
        public void ShakeOnHit(int damage, int maxHealth)
        {
            float damagePercent = (float)damage / maxHealth;
            // Map damage to trauma (e.g., 100% damage = 1.0 trauma, 10% damage = 0.3 trauma)
            float traumaAmount = Mathf.Clamp(damagePercent * 1.5f, 0.3f, 1.0f);
            AddTrauma(traumaAmount);
        }
    }
}
