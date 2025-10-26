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
        private const float SCAN_INTERVAL = 0.2f;  // Scan every 200ms instead of every frame
        private static readonly Collider[] scanBuffer = new Collider[16];  // NonAlloc buffer

        private void Awake()
        {
            trapType = TrapType.Mechanical;
        }

        protected override void Update()
        {
            base.Update();

            if (!isServer || !isArmed) return;

            // ✅ PERFORMANCE FIX: Throttled scanning instead of every frame
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

            // ✅ PERFORMANCE FIX: Use NonAlloc to avoid GC allocations
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRange,
                scanBuffer,
                LayerMask.GetMask("Player")  // Only check player layer
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
                var health = hit.collider.GetComponent<Combat.Health>();
                if (health != null)
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

