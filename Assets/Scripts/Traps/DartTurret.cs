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

        private void Awake()
        {
            trapType = TrapType.Mechanical;
        }

        protected override void Update()
        {
            base.Update();

            if (!isServer || !isArmed) return;

            // Scan for enemies
            ScanForTargets();
        }

        [Server]
        private void ScanForTargets()
        {
            if (Time.time < lastFireTime + fireRate) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
            foreach (var hit in hits)
            {
                var player = hit.GetComponent<Player.PlayerController>();
                if (player != null && player.team != ownerTeam)
                {
                    var health = player.GetComponent<Combat.Health>();
                    if (health != null && !health.IsDead())
                    {
                        FireDart(player.gameObject);
                        break;
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

