using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class Projectile : NetworkBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float collisionRadius = 0.05f;
        [SerializeField] private LayerMask hitMask = ~0;
        
        // Runtime state
        private Vector3 velocity;
        private int damage;
        private Team ownerTeam;
        private ulong shooterId;
        private bool isScoutArrow;
        private float spawnTime;
        private Vector3 prevPosition;
        private Transform shooterTransform;

        // NonAlloc buffer for sphere casts to avoid GC spikes
        private static readonly RaycastHit[] sphereCastHits = new RaycastHit[8];

        private void Awake()
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
        }

        // Initial sync for client-side visual simulation without per-frame net sync
        [SyncVar] private Vector3 initVelocity;
        [SyncVar] private int syncDamage;
        [SyncVar] private Team syncOwnerTeam;
        [SyncVar] private ulong syncShooterId;
        [SyncVar] private bool syncScout;
        [SyncVar] private float syncSpawnTime;

        [Server]
        public void Initialize(Vector3 vel, int dmg, Team team, ulong shooter, bool scout = false, Transform shooterRoot = null)
        {
            velocity = vel;
            damage = dmg;
            ownerTeam = team;
            shooterId = shooter;
            isScoutArrow = scout;
            spawnTime = Time.time;
            shooterTransform = shooterRoot;

            // set sync snapshot included in spawn payload
            initVelocity = vel;
            syncDamage = dmg;
            syncOwnerTeam = team;
            syncShooterId = shooter;
            syncScout = scout;
            syncSpawnTime = spawnTime;
        }

        private void Update()
        {
            // Move projectile (clients simulate visually too; only server processes damage+life)
            Vector3 start = transform.position;
            Vector3 end = start + velocity * Time.deltaTime;

            if (isServer)
            {
                // Continuous collision detection to prevent tunneling
                Vector3 dir = (end - start);
                float dist = dir.magnitude;
                if (dist > 0.0001f)
                {
                    dir /= dist;
                    int hitCount = Physics.SphereCastNonAlloc(start, collisionRadius, dir, sphereCastHits, dist, hitMask, QueryTriggerInteraction.Ignore);
                    if (hitCount > 0)
                    {
                        // sort by distance (simple insertion sort due to tiny arrays)
                        for (int i = 1; i < hitCount; i++)
                        {
                            var key = sphereCastHits[i];
                            int j = i - 1;
                            while (j >= 0 && sphereCastHits[j].distance > key.distance)
                            {
                                sphereCastHits[j + 1] = sphereCastHits[j];
                                j--;
                            }
                            sphereCastHits[j + 1] = key;
                        }

                        for (int i = 0; i < hitCount; i++)
                        {
                            var h = sphereCastHits[i];
                            if (IsShooterCollider(h.collider))
                                continue; // skip own colliders
                            HandleServerHit(h.collider, h.point);
                            return; // destroyed inside handler
                        }
                    }
                }
            }

            transform.position = end;
            velocity += Physics.gravity * Time.deltaTime;

            if (isServer)
            {
                // Lifetime check (authoritative)
                if (Time.time - spawnTime > lifetime)
                {
                    var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                    if (pool != null)
                        pool.Release(gameObject);
                    else
                        NetworkServer.Destroy(gameObject);
                }
            }
        }

        [Server]
        private bool IsShooterCollider(Collider col)
        {
            if (shooterTransform == null || col == null) return false;
            return col.transform == shooterTransform || col.transform.IsChildOf(shooterTransform);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // reconstruct initial state for client visual flight
            if (!isServer)
            {
                velocity = initVelocity;
                damage = syncDamage;
                ownerTeam = syncOwnerTeam;
                shooterId = syncShooterId;
                isScoutArrow = syncScout;
                spawnTime = syncSpawnTime;
            }
        }

        [Server]
        private void HandleServerHit(Collider other, Vector3 hitPoint)
        {
            // Player hit
            var player = other.GetComponent<TacticalCombat.Player.PlayerController>();
            if (player != null && player.team != ownerTeam)
            {
                var health = other.GetComponent<Health>();
                if (health != null && !health.IsDead())
                {
                    health.TakeDamage(damage, shooterId);
                }
                if (isScoutArrow)
                {
                    RpcRevealPosition(other.transform.position);
                }
                var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                if (pool != null)
                    pool.Release(gameObject);
                else
                    NetworkServer.Destroy(gameObject);
                return;
            }

            // Structure hit
            var structure = other.GetComponent<Building.Structure>();
            if (structure != null && structure.team != ownerTeam)
            {
                var health = structure.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(Mathf.Max(1, damage / 2), shooterId);
                }
                if (isScoutArrow)
                {
                    RpcRevealPosition(structure.transform.position);
                }
                var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                if (pool != null)
                    pool.Release(gameObject);
                else
                    NetworkServer.Destroy(gameObject);
                return;
            }

            // Static world hit
            if (other.gameObject.isStatic)
            {
                if (isScoutArrow)
                {
                    RpcRevealPosition(hitPoint);
                }
                var pool = TacticalCombat.Core.NetworkObjectPool.Instance;
                if (pool != null)
                    pool.Release(gameObject);
                else
                    NetworkServer.Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;
            // Fallback trigger path: reuse server hit handling and pooled release
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            HandleServerHit(other, hitPoint);
        }

        [ClientRpc]
        private void RpcRevealPosition(Vector3 position)
        {
            // Show reveal VFX at position
            Debug.Log($"Scout arrow revealed position: {position}");
            // This would create a temporary marker or ping on the UI
        }
    }
}



