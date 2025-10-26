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
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] [Range(15f, 179f)] private float maxClientAimAngle = 85f;
        private Camera cachedCamera;

        private float serverLastFireTime = -999f;

        private void Awake()
        {
            damage = GameConstants.SPEAR_DAMAGE;
            cooldown = GameConstants.SPEAR_COOLDOWN;
        }

        private void Start()
        {
            if (hitMask == 0)
            {
                var cfg = TacticalCombat.Core.LayerConfigProvider.Instance;
                if (cfg != null && cfg.projectileHitMask != 0)
                {
                    hitMask = cfg.projectileHitMask;
                }
                else
                {
                    hitMask = LayerMask.GetMask("Default", "Player", "Structure", "Trap");
                }
            }

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }
        }

        public override void Fire(bool special = false)
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;

            Vector3 origin = stabPoint != null ? stabPoint.position : transform.position;
            // Unity 6: Cache camera reference for better performance
            Camera cam = cachedCamera != null ? cachedCamera : Camera.main;
            Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

            CmdStab(direction);
        }

        [Command]
        private void CmdStab(Vector3 clientDirection)
        {
            if (Time.time < serverLastFireTime + cooldown)
                return;
            serverLastFireTime = Time.time;

            Vector3 origin = stabPoint != null ? stabPoint.position : transform.position;
            Vector3 direction = clientDirection.sqrMagnitude > 0.0001f ? clientDirection.normalized : transform.forward;

            float angle = Vector3.Angle(transform.forward, direction);
            if (angle > maxClientAimAngle && angle > 0.001f)
            {
                float t = maxClientAimAngle / angle;
                direction = Vector3.Slerp(transform.forward, direction, Mathf.Clamp01(t)).normalized;
            }

            origin += direction * 0.05f; // tiny nudge to avoid self-hit

            Team shooterTeam = ownerTeam;
            ulong shooterId = ownerId;
            if (connectionToClient != null && connectionToClient.identity != null)
            {
                var pc = connectionToClient.identity.GetComponent<Player.PlayerController>();
                if (pc != null)
                {
                    shooterTeam = pc.team;
                    shooterId = pc.playerId;
                }
            }

            // Server-side melee detection
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                var health = hit.collider.GetComponent<Health>();
                if (health != null)
                {
                    var targetPlayer = hit.collider.GetComponent<Player.PlayerController>();
                    if (targetPlayer != null && targetPlayer.team != shooterTeam)
                    {
                        health.TakeDamage(damage, shooterId);
                        RpcPlayStabAnimation();
                    }
                }
                else
                {
                    // Check if it's a structure
                    var structure = hit.collider.GetComponent<Building.Structure>();
                    if (structure != null && structure.team != shooterTeam)
                    {
                        health = structure.GetComponent<Health>();
                        if (health != null)
                        {
                            health.TakeDamage(Mathf.Max(1, damage / 2), shooterId); // Reduced damage to structures
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

