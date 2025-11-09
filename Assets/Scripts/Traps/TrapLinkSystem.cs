using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    /// <summary>
    /// Manages trap linking - chains traps together so triggering one triggers others
    /// </summary>
    public class TrapLinkSystem : NetworkBehaviour
    {
        public static TrapLinkSystem Instance { get; private set; }

        // Server-only data
        private Dictionary<uint, TrapLinkData> trapLinks = new Dictionary<uint, TrapLinkData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[TrapLinkSystem] Server started");
        }

        /// <summary>
        /// Link two traps together
        /// </summary>
        [Server]
        public bool LinkTraps(uint trap1Id, uint trap2Id)
        {
            // Validate trap IDs exist
            if (!ValidateTrapExists(trap1Id) || !ValidateTrapExists(trap2Id))
            {
                Debug.LogWarning($"[TrapLinkSystem] Invalid trap IDs: {trap1Id} or {trap2Id}");
                return false;
            }

            // Check chain length limit
            if (GetChainLength(trap1Id) >= GameConstants.MAX_TRAP_CHAIN_LENGTH)
            {
                Debug.LogWarning($"[TrapLinkSystem] Trap {trap1Id} chain too long (max: {GameConstants.MAX_TRAP_CHAIN_LENGTH})");
                return false;
            }

            if (GetChainLength(trap2Id) >= GameConstants.MAX_TRAP_CHAIN_LENGTH)
            {
                Debug.LogWarning($"[TrapLinkSystem] Trap {trap2Id} chain too long (max: {GameConstants.MAX_TRAP_CHAIN_LENGTH})");
                return false;
            }

            // Get or create link data for trap1
            if (!trapLinks.ContainsKey(trap1Id))
            {
                trapLinks[trap1Id] = new TrapLinkData(trap1Id);
            }

            // Get or create link data for trap2
            if (!trapLinks.ContainsKey(trap2Id))
            {
                trapLinks[trap2Id] = new TrapLinkData(trap2Id);
            }

            var link1 = trapLinks[trap1Id];
            var link2 = trapLinks[trap2Id];

            // Link traps (bidirectional)
            if (!link1.linkedTrapIds.Contains(trap2Id))
            {
                link1.linkedTrapIds.Add(trap2Id);
            }

            if (!link2.linkedTrapIds.Contains(trap1Id))
            {
                link2.linkedTrapIds.Add(trap1Id);
            }

            Debug.Log($"[TrapLinkSystem] Linked traps {trap1Id} <-> {trap2Id}");
            return true;
        }

        /// <summary>
        /// Trigger linked traps when one is triggered
        /// </summary>
        [Server]
        public void TriggerLinkedTraps(uint triggeredTrapId)
        {
            if (!trapLinks.ContainsKey(triggeredTrapId))
                return;

            var linkData = trapLinks[triggeredTrapId];
            
            // Trigger all linked traps with delay
            for (int i = 0; i < linkData.linkedTrapIds.Count; i++)
            {
                uint linkedTrapId = linkData.linkedTrapIds[i];
                float delay = i * GameConstants.TRAP_CHAIN_DELAY;
                
                StartCoroutine(TriggerTrapDelayed(linkedTrapId, delay));
            }
        }

        [Server]
        private System.Collections.IEnumerator TriggerTrapDelayed(uint trapId, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Find trap and trigger it
            var trap = GetTrapById(trapId);
            if (trap != null)
            {
                trap.Trigger(null); // Trigger without player (chain trigger)
            }
        }

        [Server]
        private TrapBase GetTrapById(uint trapId)
        {
            // Find trap by network ID
            foreach (var trap in FindObjectsByType<TrapBase>(FindObjectsSortMode.None))
            {
                if (trap.netId == trapId)
                    return trap;
            }
            return null;
        }

        [Server]
        private bool ValidateTrapExists(uint trapId)
        {
            return GetTrapById(trapId) != null;
        }

        [Server]
        private int GetChainLength(uint trapId)
        {
            if (!trapLinks.ContainsKey(trapId))
                return 0;

            // Count total traps in chain (including this one)
            HashSet<uint> visited = new HashSet<uint>();
            return CountChainLength(trapId, visited);
        }

        [Server]
        private int CountChainLength(uint trapId, HashSet<uint> visited)
        {
            if (visited.Contains(trapId))
                return 0; // Cycle detected

            visited.Add(trapId);
            int count = 1;

            if (trapLinks.ContainsKey(trapId))
            {
                foreach (var linkedId in trapLinks[trapId].linkedTrapIds)
                {
                    count += CountChainLength(linkedId, visited);
                }
            }

            return count;
        }

        /// <summary>
        /// Remove trap from links (when destroyed)
        /// </summary>
        [Server]
        public void RemoveTrap(uint trapId)
        {
            if (!trapLinks.ContainsKey(trapId))
                return;

            var linkData = trapLinks[trapId];

            // Remove this trap from all linked traps
            foreach (var linkedId in linkData.linkedTrapIds)
            {
                if (trapLinks.ContainsKey(linkedId))
                {
                    trapLinks[linkedId].linkedTrapIds.Remove(trapId);
                }
            }

            // Remove this trap's link data
            trapLinks.Remove(trapId);
            Debug.Log($"[TrapLinkSystem] Removed trap {trapId} from links");
        }

        /// <summary>
        /// Get linked trap IDs for a trap
        /// </summary>
        [Server]
        public List<uint> GetLinkedTraps(uint trapId)
        {
            if (!trapLinks.ContainsKey(trapId))
                return new List<uint>();

            return new List<uint>(trapLinks[trapId].linkedTrapIds);
        }
    }
}

