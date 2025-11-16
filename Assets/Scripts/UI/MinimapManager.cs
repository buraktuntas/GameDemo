using UnityEngine;
using System.Collections.Generic;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Manages minimap reveals - shows enemy players and structures when revealed
    /// </summary>
    public class MinimapManager : MonoBehaviour
    {
        public static MinimapManager Instance { get; private set; }

        [Header("Minimap Settings")]
        [SerializeField] private float minimapUpdateInterval = 0.1f; // 10 Hz update rate

        // Track revealed players and structures
        private HashSet<ulong> revealedPlayers = new HashSet<ulong>();
        private HashSet<uint> revealedStructures = new HashSet<uint>();
        private Dictionary<ulong, float> playerRevealExpiry = new Dictionary<ulong, float>();
        private Dictionary<uint, float> structureRevealExpiry = new Dictionary<uint, float>();
        
        private float lastUpdateTime = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // ✅ FIX: Use minimapUpdateInterval for throttling updates
            if (Time.time - lastUpdateTime < minimapUpdateInterval)
                return;
            
            lastUpdateTime = Time.time;
            
            // Clean up expired reveals
            CleanupExpiredReveals();
        }

        /// <summary>
        /// Reveal a player on minimap for specified duration
        /// </summary>
        public void RevealPlayer(ulong playerId, float duration)
        {
            if (playerId == 0) return;

            revealedPlayers.Add(playerId);
            
            if (duration > 0)
            {
                playerRevealExpiry[playerId] = Time.time + duration;
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MinimapManager] Player {playerId} revealed for {duration}s");
            #endif

            // Notify UI to update minimap
            OnPlayerRevealed?.Invoke(playerId, duration);
        }

        /// <summary>
        /// Reveal a structure on minimap for specified duration
        /// </summary>
        public void RevealStructure(uint structureId, float duration)
        {
            if (structureId == 0) return;

            revealedStructures.Add(structureId);
            
            if (duration > 0)
            {
                structureRevealExpiry[structureId] = Time.time + duration;
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MinimapManager] Structure {structureId} revealed for {duration}s");
            #endif

            // Notify UI to update minimap
            OnStructureRevealed?.Invoke(structureId, duration);
        }

        /// <summary>
        /// Hide a player from minimap
        /// </summary>
        public void HidePlayer(ulong playerId)
        {
            revealedPlayers.Remove(playerId);
            playerRevealExpiry.Remove(playerId);
            OnPlayerHidden?.Invoke(playerId);
        }

        /// <summary>
        /// Hide a structure from minimap
        /// </summary>
        public void HideStructure(uint structureId)
        {
            revealedStructures.Remove(structureId);
            structureRevealExpiry.Remove(structureId);
            OnStructureHidden?.Invoke(structureId);
        }

        /// <summary>
        /// Check if a player is currently revealed
        /// </summary>
        public bool IsPlayerRevealed(ulong playerId)
        {
            return revealedPlayers.Contains(playerId);
        }

        /// <summary>
        /// Check if a structure is currently revealed
        /// </summary>
        public bool IsStructureRevealed(uint structureId)
        {
            return revealedStructures.Contains(structureId);
        }

        private void CleanupExpiredReveals()
        {
            float currentTime = Time.time;

            // Clean up expired player reveals
            var expiredPlayers = new List<ulong>();
            foreach (var kvp in playerRevealExpiry)
            {
                if (currentTime >= kvp.Value)
                {
                    expiredPlayers.Add(kvp.Key);
                }
            }
            foreach (var playerId in expiredPlayers)
            {
                HidePlayer(playerId);
            }

            // Clean up expired structure reveals
            var expiredStructures = new List<uint>();
            foreach (var kvp in structureRevealExpiry)
            {
                if (currentTime >= kvp.Value)
                {
                    expiredStructures.Add(kvp.Key);
                }
            }
            foreach (var structureId in expiredStructures)
            {
                HideStructure(structureId);
            }
        }

        /// <summary>
        /// Clear all reveals (called when match ends)
        /// </summary>
        public void ClearAllReveals()
        {
            revealedPlayers.Clear();
            revealedStructures.Clear();
            playerRevealExpiry.Clear();
            structureRevealExpiry.Clear();
        }

        // Events for UI updates
        public System.Action<ulong, float> OnPlayerRevealed;
        public System.Action<uint, float> OnStructureRevealed;
        public System.Action<ulong> OnPlayerHidden;
        public System.Action<uint> OnStructureHidden;
    }
}

