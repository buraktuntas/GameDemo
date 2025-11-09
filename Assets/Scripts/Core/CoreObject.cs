using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Core Object that can be picked up, carried, and returned
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Collider))]
    public class CoreObject : NetworkBehaviour
    {
        [Header("Core Settings")]
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private GameObject carryIndicatorPrefab;

        [SyncVar]
        private ulong ownerId;

        [SyncVar]
        private ulong carrierId;

        [SyncVar]
        private bool isCarried;

        private ObjectiveManager objectiveManager;
        private GameObject carryIndicator;
        private Collider coreCollider;

        public void Initialize(ulong owner, ObjectiveManager manager)
        {
            ownerId = owner;
            objectiveManager = manager;
            isCarried = false;
            carrierId = 0;
            coreCollider = GetComponent<Collider>();
            coreCollider.isTrigger = true; // Already set as trigger
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdateVisuals();
        }

        // ✅ CRITICAL PERFORMANCE FIX: Remove Update() and CheckForPickup()
        // Use OnTriggerEnter instead of Physics.OverlapSphere every frame
        // This eliminates 60 FPS × cores = massive GC allocations

        /// <summary>
        /// ✅ PERFORMANCE: Trigger-based pickup detection instead of Physics.OverlapSphere every frame
        /// </summary>
        [Server]
        private void OnTriggerEnter(Collider other)
        {
            // Only process if not carried
            if (isCarried) return;

            // Check if it's a player
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ulong playerId = player.netId;

                // Try to pick up
                if (objectiveManager != null)
                {
                    objectiveManager.PickupCore(ownerId, playerId);
                }
            }
        }

        [Server]
        public void OnPickedUp(ulong playerId)
        {
            isCarried = true;
            carrierId = playerId;
            
            // Attach to player
            var player = GetPlayerById(playerId);
            if (player != null)
            {
                transform.SetParent(player.transform);
                transform.localPosition = Vector3.up * 1.5f; // Above player head
                coreCollider.enabled = false;
                
                // Notify PlayerController
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.SetCarryingCore(true, ownerId);
                }
            }

            RpcUpdateCarryState(true, playerId);
        }

        [Server]
        public void OnDropped()
        {
            ulong previousCarrier = carrierId;
            isCarried = false;
            carrierId = 0;
            
            transform.SetParent(null);
            coreCollider.enabled = true;

            // Notify PlayerController
            if (previousCarrier != 0)
            {
                var player = GetPlayerById(previousCarrier);
                if (player != null)
                {
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.SetCarryingCore(false, 0);
                }
                }
            }

            RpcUpdateCarryState(false, 0);
        }

        [Server]
        private PlayerController GetPlayerById(ulong playerId)
        {
            // ✅ PERFORMANCE FIX: Use NetworkIdentity.spawned instead of FindObjectsByType
            // FindObjectsByType scans all objects in scene - O(n) every call
            // NetworkIdentity.spawned is O(1) dictionary lookup
            // ✅ FIX: NetworkServer.spawned uses uint (netId), but we store ulong - cast needed
            uint netId = (uint)playerId;
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                return identity.GetComponent<PlayerController>();
            }
            return null;
        }

        [ClientRpc]
        private void RpcUpdateCarryState(bool carried, ulong carrier)
        {
            isCarried = carried;
            carrierId = carrier;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Show/hide carry indicator
            if (carryIndicatorPrefab != null)
            {
                if (isCarried && carryIndicator == null)
                {
                    carryIndicator = Instantiate(carryIndicatorPrefab, transform);
                }
                else if (!isCarried && carryIndicator != null)
                {
                    Destroy(carryIndicator);
                    carryIndicator = null;
                }
            }
        }

        private void OnDestroy()
        {
            if (carryIndicator != null)
            {
                Destroy(carryIndicator);
            }
        }
    }
}

