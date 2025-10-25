using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapons")]
        [SerializeField] private WeaponBow bow;
        [SerializeField] private WeaponSpear spear;

        [Header("Settings")]
        [SerializeField] private Transform weaponMount;

        private WeaponBase currentWeapon;
        private InputAction fireAction;
        private InputAction switchWeaponAction;
        private bool scoutArrowEnabled = false;

        private void Awake()
        {
            if (bow == null)
            {
                bow = GetComponentInChildren<WeaponBow>();
            }
            if (spear == null)
            {
                spear = GetComponentInChildren<WeaponSpear>();
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                fireAction = playerMap.FindAction("Fire");
                switchWeaponAction = playerMap.FindAction("SwitchWeapon");

                fireAction.performed += OnFire;
                switchWeaponAction.performed += OnSwitchWeapon;
            }

            // Default to bow
            EquipWeapon(bow);
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            if (MatchManager.Instance.GetCurrentPhase() != Phase.Combat)
                return;

            if (currentWeapon != null && currentWeapon.CanFire())
            {
                currentWeapon.Fire(scoutArrowEnabled);
                scoutArrowEnabled = false; // Reset after use
            }
        }

        private void OnSwitchWeapon(InputAction.CallbackContext context)
        {
            if (currentWeapon == bow)
            {
                EquipWeapon(spear);
            }
            else
            {
                EquipWeapon(bow);
            }
        }

        private void EquipWeapon(WeaponBase weapon)
        {
            if (currentWeapon != null)
            {
                currentWeapon.Unequip();
            }

            currentWeapon = weapon;
            if (currentWeapon != null)
            {
                currentWeapon.Equip();
            }
        }

        public void EnableScoutArrow()
        {
            scoutArrowEnabled = true;
        }

        private void OnDisable()
        {
            if (fireAction != null)
            {
                fireAction.performed -= OnFire;
            }
            if (switchWeaponAction != null)
            {
                switchWeaponAction.performed -= OnSwitchWeapon;
            }
        }
    }
}



