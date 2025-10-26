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
        [SerializeField] private LayerMask hitMask = ~0; // used by hitscan fallback
        [SerializeField] [Range(15f, 179f)] private float maxClientAimAngle = 75f; // anti-spoof guard
        private Camera cachedCamera;

        // server-side fire rate gate (authoritative)
        private float serverLastFireTime = -999f;

        private void Awake()
        {
            damage = GameConstants.BOW_DAMAGE;
            cooldown = GameConstants.BOW_COOLDOWN;
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

        public override void Fire(bool isScoutArrow = false)
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;

            Vector3 origin = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
            // Unity 6: Cache camera reference for better performance
            Camera cam = cachedCamera != null ? cachedCamera : Camera.main;
            Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

            // send only intent; server derives secure origin
            CmdFire(direction, isScoutArrow);
        }

        [Command]
        private void CmdFire(Vector3 clientDirection, bool isScoutArrow)
        {
            // authoritative cooldown check on server
            if (Time.time < serverLastFireTime + cooldown)
                return;
            serverLastFireTime = Time.time;

            // resolve secure origin and shooter identity on server
            Vector3 origin = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
            Vector3 direction = clientDirection.sqrMagnitude > 0.0001f ? clientDirection.normalized : transform.forward;

            // clamp extreme aim spoofing to a sane cone relative to server forward
            float angle = Vector3.Angle(transform.forward, direction);
            if (angle > maxClientAimAngle && angle > 0.001f)
            {
                float t = maxClientAimAngle / angle;
                direction = Vector3.Slerp(transform.forward, direction, Mathf.Clamp01(t)).normalized;
            }

            // nudge origin forward slightly to avoid self-hit
            origin += direction * 0.1f;

            // get owner from connection to avoid trusting client state
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

            // Server spawns projectile
            if (arrowPrefab != null)
            {
                GameObject arrow = TacticalCombat.Core.NetworkObjectPool.Instance != null
                    ? TacticalCombat.Core.NetworkObjectPool.Instance.Get(arrowPrefab, origin, Quaternion.LookRotation(direction))
                    : Instantiate(arrowPrefab, origin, Quaternion.LookRotation(direction));
                var projectile = arrow.GetComponent<Projectile>();
                if (projectile != null)
                {
                    var shooterRoot = (connectionToClient != null && connectionToClient.identity != null)
                        ? connectionToClient.identity.transform
                        : transform;
                    projectile.Initialize(direction * projectileSpeed, damage, shooterTeam, shooterId, isScoutArrow, shooterRoot);
                }
                if (TacticalCombat.Core.NetworkObjectPool.Instance == null)
                {
                    NetworkServer.Spawn(arrow);
                }
            }
            else
            {
                // Hitscan fallback
                if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f, hitMask, QueryTriggerInteraction.Ignore))
                {
                    // Prefer player first
                    var targetPlayer = hit.collider.GetComponent<Player.PlayerController>();
                    if (targetPlayer != null && targetPlayer.team != shooterTeam)
                    {
                        var health = hit.collider.GetComponent<Health>();
                        if (health != null && !health.IsDead())
                            health.TakeDamage(damage, shooterId);
                    }
                    else
                    {
                        // Structures / damageables
                        var structure = hit.collider.GetComponent<Building.Structure>();
                        if (structure != null && structure.team != shooterTeam)
                        {
                            var health = structure.GetComponent<Health>();
                            if (health != null)
                                health.TakeDamage(Mathf.Max(1, damage / 2), shooterId);
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
