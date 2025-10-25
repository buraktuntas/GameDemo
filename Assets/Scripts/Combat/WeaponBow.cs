using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class WeaponBow : WeaponBase
    {
        [Header("Bow Settings")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform arrowSpawnPoint;
        [SerializeField] private float projectileSpeed = GameConstants.BOW_PROJECTILE_SPEED;

        private void Awake()
        {
            damage = GameConstants.BOW_DAMAGE;
            cooldown = GameConstants.BOW_COOLDOWN;
        }

        public override void Fire(bool isScoutArrow = false)
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;

            Vector3 origin = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
            // Unity 6: Cache camera reference for better performance
            Camera cam = Camera.main;
            Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

            CmdFire(origin, direction, isScoutArrow);
        }

        [Command]
        private void CmdFire(Vector3 origin, Vector3 direction, bool isScoutArrow)
        {
            // Server spawns projectile
            if (arrowPrefab != null)
            {
                GameObject arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(direction));
                var projectile = arrow.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(direction * projectileSpeed, damage, ownerTeam, ownerId, isScoutArrow);
                }
                NetworkServer.Spawn(arrow);
            }
            else
            {
                // Hitscan fallback
                if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
                {
                    var health = hit.collider.GetComponent<Health>();
                    if (health != null)
                    {
                        var targetPlayer = hit.collider.GetComponent<Player.PlayerController>();
                        if (targetPlayer != null && targetPlayer.team != ownerTeam)
                        {
                            health.TakeDamage(damage, ownerId);
                        }
                    }

                    RpcShowHitEffect(hit.point, hit.normal);
                }
            }
        }

        [ClientRpc]
        private void RpcShowHitEffect(Vector3 position, Vector3 normal)
        {
            // Spawn hit VFX
            Debug.Log($"Arrow hit at {position}");
        }
    }
}

