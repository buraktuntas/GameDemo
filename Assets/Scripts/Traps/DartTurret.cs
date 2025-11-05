using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    public class DartTurret : TrapBase
    {
        [Header("Dart Turret")]
        [SerializeField] private int dartDamage = GameConstants.DART_TRAP_DAMAGE;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float fireRate = 1.5f;
        [SerializeField] private GameObject dartPrefab;
        [SerializeField] private Transform firePoint;

        private float lastFireTime;
        private float lastScanTime;
        private const float SCAN_INTERVAL = 0.2f;
        private static readonly Collider[] scanBuffer = new Collider[16];
        private static int playerLayerMask = -1;  // Cache layer mask

        private void Awake()
        {
            trapType = TrapType.Mechanical;

            // Cache layer mask once
            if (playerLayerMask == -1)
            {
                playerLayerMask = LayerMask.GetMask("Player");
            }
        }

        private void Update()
        {
            if (!isServer || !isArmed) return;

            // Throttled scanning every 200ms
            if (Time.time - lastScanTime >= SCAN_INTERVAL)
            {
                lastScanTime = Time.time;
                ScanForTargets();
            }
        }

        [Server]
        private void ScanForTargets()
        {
            if (Time.time < lastFireTime + fireRate) return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRange,
                scanBuffer,
                playerLayerMask
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = scanBuffer[i];

                // ✅ PERFORMANCE FIX: Use TryGetComponent (faster than GetComponent)
                if (hit.TryGetComponent<Player.PlayerController>(out var player))
                {
                    if (player.team != ownerTeam)
                    {
                        if (hit.TryGetComponent<Combat.Health>(out var health))
                        {
                            if (!health.IsDead())
                            {
                                FireDart(player.gameObject);
                                break;
                            }
                        }
                    }
                }
            }
        }

        [Server]
        private void FireDart(GameObject target)
        {
            lastFireTime = Time.time;

            Vector3 direction = (target.transform.position - transform.position).normalized;
            
            // Simple hitscan
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, detectionRange))
            {
                // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent (faster, no GC)
                if (hit.collider.TryGetComponent<Combat.Health>(out var health))
                {
                    health.TakeDamage(dartDamage);
                }
            }

            RpcPlayFireEffect(direction);
        }

        [ClientRpc]
        private void RpcPlayFireEffect(Vector3 direction)
        {
            Debug.Log($"Dart turret fired in direction {direction}");
        }

        [Server]
        protected override void Trigger(GameObject target)
        {
            // Dart turret doesn't use trigger - it scans automatically
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

