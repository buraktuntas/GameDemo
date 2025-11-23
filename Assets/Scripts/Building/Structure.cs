using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Combat;

namespace TacticalCombat.Building
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(StructuralIntegrity))]
    public class Structure : NetworkBehaviour
    {
        [Header("Structure Info")]
        [SyncVar(hook = nameof(OnTeamChanged))]
        public Team team;

        [SyncVar]
        public StructureType structureType;

        [SyncVar]
        public StructureCategory category;

        [SyncVar]
        public ulong ownerId;

        [Header("Visual")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material teamAMaterial;
        [SerializeField] private Material teamBMaterial;

        [Header("Destruction Effects")]
        [SerializeField] private GameObject destructionEffect;
        [SerializeField] private AudioClip destructionSound;

        private Health health;
        private bool isCore = false;

        private void Awake()
        {
            health = GetComponent<Health>();
            isCore = (structureType == StructureType.CoreStructure);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // ✅ REFACTOR: Get health from StructureDatabase
            int hp = 100; // Default fallback
            if (StructureDatabase.Instance != null)
            {
                hp = StructureDatabase.Instance.GetHealth(structureType);
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"⚠️ [Structure] StructureDatabase.Instance is NULL! Using default health {hp} for {structureType}");
                #endif
            }
            health.SetMaxHealth(hp);

            // Subscribe to death
            health.OnDeathEvent += OnStructureDestroyed;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdateVisuals();
        }

        public void Initialize(Team ownerTeam, StructureType type, StructureCategory cat, ulong owner)
        {
            team = ownerTeam;
            structureType = type;
            category = cat;
            ownerId = owner;
            UpdateVisuals();
        }

        /// <summary>
        /// ✅ HIGH PRIORITY: SyncVar hook for team changes (update visuals when team changes)
        /// </summary>
        private void OnTeamChanged(Team oldTeam, Team newTeam)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (renderers == null || renderers.Length == 0) return;

            Material mat = team == Team.TeamA ? teamAMaterial : teamBMaterial;
            if (mat != null)
            {
                // ✅ CRITICAL FIX: Use sharedMaterial to prevent memory leak
                // rend.material creates new instance every time!
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.sharedMaterial = mat;  // No instance created - prevents leak
                    }
                }
            }
        }

        [Server]
        private void OnStructureDestroyed()
        {
            if (isCore)
            {
                Team winner = team == Team.TeamA ? Team.TeamB : Team.TeamA;
                MatchManager.Instance?.OnCoreDestroyed(winner);
            }

            // Play destruction effects
            RpcPlayDestructionEffects();

            // ✅ MEMORY LEAK FIX: Unsubscribe from event before destruction
            if (health != null)
            {
                health.OnDeathEvent -= OnStructureDestroyed;
            }

            // Delayed destruction to allow effects to play
            Invoke(nameof(DestroyStructure), 0.5f);
        }

        [Server]
        private void DestroyStructure()
        {
            // Return to pool if available, otherwise destroy
            if (NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.Release(gameObject);
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        /// <summary>
        /// ✅ HIGH PRIORITY: Public method to trigger destruction effects (called from StructuralIntegrity)
        /// </summary>
        [Server]
        public void TriggerDestructionEffects()
        {
            RpcPlayDestructionEffects();
        }

        [ClientRpc]
        private void RpcPlayDestructionEffects()
        {
            // Spawn destruction particle effect
            if (destructionEffect != null)
            {
                // ✅ PERFORMANCE: Use object pooling if available
                GameObject effect;
                if (NetworkObjectPool.Instance != null)
                {
                    effect = NetworkObjectPool.Instance.Get(destructionEffect, transform.position, Quaternion.identity);
                    // Auto-return to pool after 3 seconds
                    StartCoroutine(ReturnEffectToPool(effect, 3f));
                }
                else
                {
                    effect = Instantiate(destructionEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 3f);
                }
            }

            // Play destruction sound
            if (destructionSound != null)
            {
                AudioSource.PlayClipAtPoint(destructionSound, transform.position);
            }

            // Hide renderers
            if (renderers != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.enabled = false;
                    }
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"💥 Structure destroyed: {structureType} (Team: {team})");
            #endif
        }

        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Cancel Invoke on destroy to prevent memory leaks
            CancelInvoke(nameof(DestroyStructure)); // Cancel delayed destruction if object destroyed early
            
            // ✅ MEMORY LEAK FIX: Cleanup event subscription on destroy
            if (health != null)
            {
                health.OnDeathEvent -= OnStructureDestroyed;
            }
        }

        /// <summary>
        /// Get structure type (for BlueprintSystem)
        /// </summary>
        public StructureType GetStructureType()
        {
            return structureType;
        }

        /// <summary>
        /// Get owner ID (for BlueprintSystem)
        /// </summary>
        public ulong GetOwnerId()
        {
            return ownerId;
        }

        /// <summary>
        /// ✅ PERFORMANCE: Helper coroutine to return effect to pool after delay
        /// </summary>
        private System.Collections.IEnumerator ReturnEffectToPool(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null && NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.Release(effect);
            }
        }
    }
}


