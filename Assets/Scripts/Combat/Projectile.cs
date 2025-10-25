using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public class Projectile : NetworkBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private float lifetime = 5f;
        
        private Vector3 velocity;
        private int damage;
        private Team ownerTeam;
        private ulong shooterId;
        private bool isScoutArrow;
        private float spawnTime;

        public void Initialize(Vector3 vel, int dmg, Team team, ulong shooter, bool scout = false)
        {
            velocity = vel;
            damage = dmg;
            ownerTeam = team;
            shooterId = shooter;
            isScoutArrow = scout;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (!isServer) return;

            // Move projectile
            transform.position += velocity * Time.deltaTime;

            // Apply gravity
            velocity += Physics.gravity * Time.deltaTime;

            // Lifetime check
            if (Time.time - spawnTime > lifetime)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;

            // Check if hit a player
            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && player.team != ownerTeam)
            {
                var health = other.GetComponent<Health>();
                if (health != null && !health.IsDead())
                {
                    health.TakeDamage(damage, shooterId);
                }

                if (isScoutArrow)
                {
                    // Reveal player position
                    RpcRevealPosition(other.transform.position);
                }

                NetworkServer.Destroy(gameObject);
                return;
            }

            // Check if hit a structure
            var structure = other.GetComponent<Building.Structure>();
            if (structure != null && structure.team != ownerTeam)
            {
                var health = structure.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage / 2, shooterId);
                }

                if (isScoutArrow)
                {
                    RpcRevealPosition(structure.transform.position);
                }

                NetworkServer.Destroy(gameObject);
                return;
            }

            // Hit terrain/static object
            if (other.gameObject.isStatic)
            {
                if (isScoutArrow)
                {
                    RpcRevealPosition(transform.position);
                }
                NetworkServer.Destroy(gameObject);
            }
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



