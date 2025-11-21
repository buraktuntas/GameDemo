using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// ADS (Aim Down Sights) System
    /// Modern FPS oyunlarında standart mekanik
    /// - Smooth FOV transition
    /// - Weapon position animation
    /// - Accuracy improvement
    /// - Movement speed reduction
    /// </summary>
    public class ADSSystem : MonoBehaviour
    {
        [Header("🎯 ADS Settings")]
        [SerializeField] private bool adsEnabled = true;
        // [SerializeField] private KeyCode adsKey = KeyCode.Mouse1; // Replaced by Input System (Right Click)
        
        [Header("📷 Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float adsFOV = 40f;
        [SerializeField] private float fovTransitionSpeed = 8f;
        
        [Header("🔫 Weapon Position")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Vector3 normalPosition = new Vector3(0.5f, -0.3f, 0.5f);
        [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.15f, 0.3f);
        [SerializeField] private float positionTransitionSpeed = 10f;
        
        [Header("🎯 Accuracy")]
        [SerializeField] private float normalSpread = 0.05f;
        [SerializeField] private float adsSpread = 0.01f;
        
        [Header("🏃 Movement")]
        [SerializeField] private float normalMovementSpeed = 7f;
        [SerializeField] private float adsMovementSpeed = 4f;
        
        [Header("📊 Stats")]
        [SerializeField] private bool showDebug = false;
        
        // State
        private bool isAiming = false;
        private float currentFOV;
        private Vector3 currentWeaponPosition;
        
        // References
        private WeaponSystem weaponSystem;
        private TacticalCombat.Player.FPSController fpsController;
        
        private void Awake()
        {
            weaponSystem = GetComponent<WeaponSystem>();
            fpsController = GetComponent<TacticalCombat.Player.FPSController>();

            // ✅ PERFORMANCE FIX: Get camera from FPSController instead of Camera.main
            if (playerCamera == null)
            {
                if (fpsController != null)
                {
                    playerCamera = fpsController.GetCamera();
                }

                if (playerCamera == null)
                {
                    playerCamera = GetComponentInChildren<Camera>();
                }

                if (playerCamera == null)
                {
                    Debug.LogError("❌ [ADSSystem] No camera found! Camera.main usage is BANNED for performance.");
                }
            }

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;

            currentFOV = normalFOV;
            currentWeaponPosition = normalPosition;
        }
        
        private void Update()
        {
            if (!adsEnabled) return;
            
            HandleInput();
            UpdateADS();
        }
        
        private void HandleInput()
        {
            // Toggle ADS (Hold Right Mouse Button)
            bool isAimingInput = false;
            
            if (Mouse.current != null)
            {
                isAimingInput = Mouse.current.rightButton.isPressed;
            }
            
            // Optional: Keyboard fallback (e.g. Z key)
            if (!isAimingInput && Keyboard.current != null)
            {
                // isAimingInput = Keyboard.current.zKey.isPressed;
            }

            if (isAimingInput && !isAiming)
            {
                StartADS();
            }
            else if (!isAimingInput && isAiming)
            {
                StopADS();
            }
        }
        
        private void StartADS()
        {
            if (isAiming) return;
            
            isAiming = true;
            
            if (showDebug)
                Debug.Log("🎯 [ADS] Started aiming");
            
            // Notify weapon system
            if (weaponSystem != null)
            {
                // weaponSystem.SetSpread(adsSpread);
            }
            
            // Reduce movement speed
            if (fpsController != null)
            {
                fpsController.walkSpeed = adsMovementSpeed;
            }
        }
        
        private void StopADS()
        {
            if (!isAiming) return;
            
            isAiming = false;
            
            if (showDebug)
                Debug.Log("🎯 [ADS] Stopped aiming");
            
            // Notify weapon system
            if (weaponSystem != null)
            {
                // weaponSystem.SetSpread(normalSpread);
            }
            
            // Restore movement speed
            if (fpsController != null)
            {
                fpsController.walkSpeed = normalMovementSpeed;
            }
        }
        
        private void UpdateADS()
        {
            // Target values
            float targetFOV = isAiming ? adsFOV : normalFOV;
            Vector3 targetPosition = isAiming ? adsPosition : normalPosition;
            
            // Smooth transitions
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
            currentWeaponPosition = Vector3.Lerp(currentWeaponPosition, targetPosition, Time.deltaTime * positionTransitionSpeed);
            
            // Apply to camera
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentFOV;
            }
            
            // Apply to weapon
            if (weaponHolder != null)
            {
                weaponHolder.localPosition = currentWeaponPosition;
            }
        }
        
        // Public API
        public bool IsAiming() => isAiming;
        
        public float GetCurrentSpread()
        {
            return isAiming ? adsSpread : normalSpread;
        }
        
        public void SetADSEnabled(bool enabled)
        {
            adsEnabled = enabled;
            
            if (!enabled && isAiming)
            {
                StopADS();
            }
        }
        
        // Configuration methods
        public void SetADSFOV(float fov)
        {
            adsFOV = fov;
        }
        
        public void SetADSPosition(Vector3 position)
        {
            adsPosition = position;
        }
        
        // ✅ PERFORMANCE FIX: OnGUI removed (runs every frame - very slow!)
        // Use Unity's new UI system or TextMeshPro for runtime debug info

        #if UNITY_EDITOR
        // ✅ EDITOR ONLY: Debug visualization with Gizmos (zero runtime cost)
        private void OnDrawGizmosSelected()
        {
            if (!showDebug || playerCamera == null) return;

            // Draw ADS FOV cone in scene view
            UnityEditor.Handles.color = isAiming ? Color.green : Color.white;
            UnityEditor.Handles.Label(
                playerCamera.transform.position + playerCamera.transform.forward * 2f,
                $"ADS: {(isAiming ? "ON" : "OFF")}\nFOV: {currentFOV:F1}°\nSpread: {GetCurrentSpread():F3}"
            );
        }
        #endif
    }
}
