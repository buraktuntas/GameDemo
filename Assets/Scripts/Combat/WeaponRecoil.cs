using UnityEngine;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// AAA Weapon Recoil System
    /// Handles procedural recoil for both the weapon model and the camera
    /// </summary>
    public class WeaponRecoil : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform weaponHolder;
        private Camera playerCamera;

        [Header("State")]
        private Vector3 originalWeaponPos;
        private Quaternion originalWeaponRot;
        private float recoilAmount;

        public void Initialize(Transform holder, Camera camera)
        {
            weaponHolder = holder;
            playerCamera = camera;
            
            if (weaponHolder != null)
            {
                originalWeaponPos = weaponHolder.localPosition;
                originalWeaponRot = weaponHolder.localRotation;
            }
        }

        public void ApplyRecoil(float amount)
        {
            if (weaponHolder == null) return;
            
            recoilAmount += amount;
            
            // Apply to weapon position (procedural kickback)
            Vector3 kickback = originalWeaponPos + new Vector3(
                Random.Range(-0.02f, 0.02f),
                Random.Range(-0.02f, 0.02f),
                -0.05f * (amount * 2f) // Scale kickback with amount
            );
            
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, kickback, Time.deltaTime * 20f);
            
            // Apply to camera rotation
            if (playerCamera != null)
            {
                float recoilX = Random.Range(-amount, amount);
                float recoilY = amount;
                playerCamera.transform.localRotation *= Quaternion.Euler(-recoilY, recoilX, 0);
            }
        }

        public void UpdateRecoil(float returnSpeed, float snapSpeed)
        {
            if (weaponHolder == null) return;

            // Smooth recovery of recoil amount
            recoilAmount = Mathf.Lerp(recoilAmount, 0, Time.deltaTime * returnSpeed);
            
            // Smooth return to original position
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, originalWeaponPos, Time.deltaTime * snapSpeed);
            
            // Smooth return to original rotation
            if (weaponHolder.localRotation != originalWeaponRot)
            {
                weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, originalWeaponRot, Time.deltaTime * snapSpeed);
            }
        }

        public void ResetState()
        {
            recoilAmount = 0;
            if (weaponHolder != null)
            {
                weaponHolder.localPosition = originalWeaponPos;
                weaponHolder.localRotation = originalWeaponRot;
            }
        }
    }
}
