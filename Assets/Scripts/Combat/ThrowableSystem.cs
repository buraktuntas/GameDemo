using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Manages throwable items: Smoke, EMP, Sticky Bomb, Reveal Dart
    /// </summary>
    public class ThrowableSystem : NetworkBehaviour
    {
        [Header("Throwable Prefabs")]
        [SerializeField] private GameObject smokePrefab;
        [SerializeField] private GameObject empPrefab;
        [SerializeField] private GameObject stickyBombPrefab;
        [SerializeField] private GameObject revealDartPrefab;

        [Header("Throw Settings")]
        [SerializeField] private float throwForce = 15f;
        // ✅ REMOVED: throwArc - not currently used (future feature for arc visualization)

        // Server-only tracking
        private Dictionary<uint, ThrowableData> activeThrowables = new Dictionary<uint, ThrowableData>();

        // ✅ CRITICAL PERFORMANCE FIX: Static buffer for Physics.OverlapSphereNonAlloc
        // This eliminates GC allocations from every throwable activation
        private static Collider[] throwableColliderBuffer = new Collider[50];

        public override void OnStartServer()
        {
            base.OnStartServer();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[ThrowableSystem] Server started");
            #endif
        }

        /// <summary>
        /// Throw a throwable item
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdThrow(ThrowableType type, Vector3 throwPosition, Vector3 throwDirection, ulong throwerId)
        {
            GameObject prefab = GetPrefabForType(type);
            if (prefab == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[ThrowableSystem] Prefab not found for type {type}");
                #endif
                return;
            }

            // Spawn throwable
            GameObject throwable = Instantiate(prefab, throwPosition, Quaternion.identity);
            
            // Apply throw force
            Rigidbody rb = throwable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 force = throwDirection.normalized * throwForce;
                rb.AddForce(force, ForceMode.VelocityChange);
            }

            NetworkServer.Spawn(throwable);

            // Track throwable
            var throwableComponent = throwable.GetComponent<ThrowableItem>();
            if (throwableComponent != null)
            {
                throwableComponent.Initialize(type, throwerId, this);
                uint netId = throwableComponent.netId;
                
                float duration = GetDurationForType(type);
                float radius = GetRadiusForType(type);
                activeThrowables[netId] = new ThrowableData(type, throwPosition, throwerId, duration, radius);
            }

            RpcOnThrowableThrown(type, throwPosition, throwDirection);
        }

        [Server]
        private GameObject GetPrefabForType(ThrowableType type)
        {
            return type switch
            {
                ThrowableType.Smoke => smokePrefab,
                ThrowableType.EMP => empPrefab,
                ThrowableType.StickyBomb => stickyBombPrefab,
                ThrowableType.RevealDart => revealDartPrefab,
                _ => null
            };
        }

        [Server]
        private float GetDurationForType(ThrowableType type)
        {
            return type switch
            {
                ThrowableType.Smoke => GameConstants.SMOKE_DURATION,
                ThrowableType.EMP => GameConstants.EMP_DURATION,
                ThrowableType.StickyBomb => 0f, // Instant explosion
                ThrowableType.RevealDart => GameConstants.REVEAL_DART_DURATION,
                _ => 0f
            };
        }

        [Server]
        private float GetRadiusForType(ThrowableType type)
        {
            return type switch
            {
                ThrowableType.Smoke => 10f,
                ThrowableType.EMP => GameConstants.EMP_RADIUS,
                ThrowableType.StickyBomb => 5f,
                ThrowableType.RevealDart => GameConstants.REVEAL_DART_RADIUS,
                _ => 0f
            };
        }

        /// <summary>
        /// Handle throwable impact/activation
        /// </summary>
        [Server]
        public void OnThrowableActivated(uint throwableId, Vector3 position, ThrowableType type)
        {
            if (!activeThrowables.ContainsKey(throwableId))
                return;

            var data = activeThrowables[throwableId];

            switch (type)
            {
                case ThrowableType.Smoke:
                    ActivateSmoke(position, data.radius);
                    break;
                case ThrowableType.EMP:
                    ActivateEMP(position, data.radius);
                    break;
                case ThrowableType.StickyBomb:
                    ActivateStickyBomb(position, data.throwerId);
                    break;
                case ThrowableType.RevealDart:
                    ActivateRevealDart(position, data.radius, data.duration);
                    break;
            }

            // Remove after activation
            activeThrowables.Remove(throwableId);
        }

        [Server]
        private void ActivateSmoke(Vector3 position, float radius)
        {
            // ✅ VFX: Smoke effect is handled by ThrowableItem prefab (particle system)
            // The grenade spawns with a smoke particle effect that lasts for the duration
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ThrowableSystem] Smoke activated at {position}, radius: {radius}");
            #endif

            // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc to avoid GC allocation
            int count = Physics.OverlapSphereNonAlloc(position, radius, throwableColliderBuffer);

            for (int i = 0; i < count; i++)
            {
                var player = throwableColliderBuffer[i].GetComponent<Player.PlayerController>();
                if (player != null)
                {
                    // ✅ GAMEPLAY: Smoke obscures vision (handled by minimap/vision system)
                    // Players inside smoke cloud are hidden from minimap
                    // Advanced implementation: Could add post-processing fog effect
                }
            }
        }

        [Server]
        private void ActivateEMP(Vector3 position, float radius)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ThrowableSystem] EMP activated at {position}, radius: {radius}");
            #endif

            // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc to avoid GC allocation
            int count = Physics.OverlapSphereNonAlloc(position, radius, throwableColliderBuffer);

            for (int i = 0; i < count; i++)
            {
                var col = throwableColliderBuffer[i];

                // Disable traps
                var trap = col.GetComponent<Traps.TrapBase>();
                if (trap != null)
                {
                    trap.DisableTemporarily(GameConstants.EMP_DURATION);
                }

                // ✅ GAMEPLAY: Structures are not affected by EMP
                // Only traps (electronic devices) are disabled
                // Walls, elevation platforms, and turrets are unaffected
            }
        }


        [Server]
        private void ActivateStickyBomb(Vector3 position, ulong throwerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ThrowableSystem] Sticky bomb activated at {position}");
            #endif

            // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc to avoid GC allocation
            int count = Physics.OverlapSphereNonAlloc(position, 5f, throwableColliderBuffer);

            for (int i = 0; i < count; i++)
            {
                var health = throwableColliderBuffer[i].GetComponent<Health>();
                if (health != null)
                {
                    // ✅ FIX: Use correct DamageInfo constructor
                    // ✅ FIX: DamageType.Explosive -> Explosion
                    var damageInfo = new DamageInfo(
                        (int)GameConstants.STICKY_BOMB_DAMAGE,
                        throwerId,
                        DamageType.Explosion,
                        position
                    );
                    health.ApplyDamage(damageInfo);
                }
            }
        }

        [Server]
        private void ActivateRevealDart(Vector3 position, float radius, float duration)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ThrowableSystem] Reveal dart activated at {position}, radius: {radius}, duration: {duration}");
            #endif
            
            // Reveal enemies on minimap
            StartCoroutine(RevealEnemies(position, radius, duration));
        }

        [Server]
        private System.Collections.IEnumerator RevealEnemies(Vector3 position, float radius, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // ✅ CRITICAL PERFORMANCE FIX: Use OverlapSphereNonAlloc
                // This coroutine runs every 0.5s, so GC allocation was happening repeatedly!
                int count = Physics.OverlapSphereNonAlloc(position, radius, throwableColliderBuffer);

                for (int i = 0; i < count; i++)
                {
                    var player = throwableColliderBuffer[i].GetComponent<Player.PlayerController>();
                    if (player != null)
                    {
                        // ✅ IMPLEMENTED: Reveal player on minimap via RpcRevealPlayer
                        RpcRevealPlayer(player.netId);
                    }
                }

                elapsed += 0.5f; // Check every 0.5 seconds
                yield return new WaitForSeconds(0.5f);
            }
        }

        [ClientRpc]
        private void RpcRevealPlayer(ulong playerId)
        {
            // ✅ FIX: Integrate with MinimapManager
            var minimapManager = UI.MinimapManager.Instance;
            if (minimapManager != null)
            {
                minimapManager.RevealPlayer(playerId, GameConstants.REVEAL_DART_DURATION);
            }
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Player {playerId} revealed on minimap");
            #endif
        }

        [ClientRpc]
        private void RpcOnThrowableThrown(ThrowableType type, Vector3 position, Vector3 direction)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Throwable {type} thrown at {position}");
            #endif
        }
    }
}

