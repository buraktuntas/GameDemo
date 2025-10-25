using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class WeaponSpear : WeaponBase
    {
        [Header("Spear Settings")]
        [SerializeField] private float range = GameConstants.SPEAR_RANGE;
        [SerializeField] private Transform stabPoint;

        private void Awake()
        {
            damage = GameConstants.SPEAR_DAMAGE;
            cooldown = GameConstants.SPEAR_COOLDOWN;
        }

        public override void Fire(bool special = false)
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;

            Vector3 origin = stabPoint != null ? stabPoint.position : transform.position;
            // Unity 6: Cache camera reference for better performance
            Camera cam = Camera.main;
            Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

            CmdStab(origin, direction);
        }

        [Command]
        private void CmdStab(Vector3 origin, Vector3 direction)
        {
            // Server-side melee detection
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
            {
                var health = hit.collider.GetComponent<Health>();
                if (health != null)
                {
                    var targetPlayer = hit.collider.GetComponent<Player.PlayerController>();
                    if (targetPlayer != null && targetPlayer.team != ownerTeam)
                    {
                        health.TakeDamage(damage, ownerId);
                        RpcPlayStabAnimation();
                    }
                }
                else
                {
                    // Check if it's a structure
                    var structure = hit.collider.GetComponent<Building.Structure>();
                    if (structure != null && structure.team != ownerTeam)
                    {
                        health = structure.GetComponent<Health>();
                        if (health != null)
                        {
                            health.TakeDamage(damage / 2, ownerId); // Reduced damage to structures
                        }
                    }
                }
            }

            RpcPlayStabAnimation();
        }

        [ClientRpc]
        private void RpcPlayStabAnimation()
        {
            // Play stab animation/VFX
            Debug.Log("Spear stab!");
        }
    }
}

