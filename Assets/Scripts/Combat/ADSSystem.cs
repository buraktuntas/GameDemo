using UnityEngine;
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
        [SerializeField] private KeyCode adsKey = KeyCode.Mouse1; // Right click
        
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
            
            if (playerCamera == null)
                playerCamera = Camera.main;
            
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
            // Toggle ADS
            if (Input.GetKeyDown(adsKey))
            {
                StartADS();
            }
            else if (Input.GetKeyUp(adsKey))
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
        
        // Debug
        private void OnGUI()
        {
            if (!showDebug) return;
            
            GUILayout.BeginArea(new Rect(10, 250, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("<b>ADS System</b>");
            GUILayout.Label($"Aiming: {(isAiming ? "<color=green>YES</color>" : "<color=red>NO</color>")}");
            GUILayout.Label($"FOV: {currentFOV:F1}° (Target: {(isAiming ? adsFOV : normalFOV):F1}°)");
            GUILayout.Label($"Spread: {GetCurrentSpread():F3}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
