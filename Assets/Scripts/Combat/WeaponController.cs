using UnityEngine;
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
            EquipWeapon(bow);
        }

        private void Update()
        {
            if (!isLocalPlayer) return;
            
            if (MatchManager.Instance == null || MatchManager.Instance.GetCurrentPhase() != Phase.Combat)
                return;
            
            // LMB = Fire
            if (Input.GetMouseButtonDown(0) && currentWeapon != null && currentWeapon.CanFire())
            {
                currentWeapon.Fire(scoutArrowEnabled);
                scoutArrowEnabled = false;
            }
            
            // Q = Switch weapon
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SwitchWeapon();
            }
        }

        private void SwitchWeapon()
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
                // Initialize ownership for local/client-side systems (server re-validates in Commands)
                var pc = GetComponent<TacticalCombat.Player.PlayerController>();
                if (pc != null)
                {
                    currentWeapon.Initialize(pc.team, pc.playerId);
                }
                currentWeapon.Equip();
            }
        }

        public void EnableScoutArrow()
        {
            scoutArrowEnabled = true;
        }

    }
}



