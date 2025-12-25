using UnityEngine;
using System.Collections.Generic;

namespace TacticalCombat.Network
{
    /// <summary>
    /// ✅ LAG COMPENSATION: Stores player position history for server-side rewind
    /// Used for fair hit detection in high-ping scenarios (Valorant/CS2 style)
    /// </summary>
    public class PositionHistory
    {
        private class HistoryEntry
        {
            // ✅ DOUBLE PRECISION: Prevents timestamp precision loss over time (MirrorUnityFPS pattern)
            public double timestamp;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public Bounds bounds; // For precise hit detection
        }

        private Queue<HistoryEntry> history = new Queue<HistoryEntry>();

        // ✅ TUNING: 1 second history covers up to 500ms ping (2x round trip)
        private const float MAX_HISTORY_TIME = 1.0f;
        private const int MAX_HISTORY_ENTRIES = 60; // ~60 FPS * 1 sec

        /// <summary>
        /// Add current position to history (call every FixedUpdate on SERVER)
        /// </summary>
        public void RecordPosition(Vector3 pos, Quaternion rot, Vector3 vel, Bounds characterBounds)
        {
            // ✅ DOUBLE PRECISION: Use Mirror.NetworkTime for precise timestamps
            var entry = new HistoryEntry
            {
                timestamp = Mirror.NetworkTime.time,
                position = pos,
                rotation = rot,
                velocity = vel,
                bounds = characterBounds
            };

            history.Enqueue(entry);

            // Remove old entries (older than MAX_HISTORY_TIME)
            double currentTime = Mirror.NetworkTime.time;
            while (history.Count > 0)
            {
                var oldest = history.Peek();
                if (currentTime - oldest.timestamp > MAX_HISTORY_TIME)
                {
                    history.Dequeue();
                }
                else
                {
                    break; // Rest are newer
                }
            }

            // Safety cap (prevent memory leak from lag spikes)
            while (history.Count > MAX_HISTORY_ENTRIES)
            {
                history.Dequeue();
            }
        }

        /// <summary>
        /// Get position at specific timestamp with interpolation (AAA quality)
        /// Returns false if timestamp is outside history range
        /// ✅ DOUBLE PRECISION: Uses double for precise timestamp comparison
        /// </summary>
        public bool GetPositionAtTime(double targetTime, out Vector3 position, out Quaternion rotation, out Bounds bounds)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            bounds = new Bounds();

            if (history.Count == 0)
            {
                return false; // No history at all
            }

            // ✅ FAIRNESS: Handle single entry (newly spawned players)
            if (history.Count == 1)
            {
                var entry = history.ToArray()[0];
                position = entry.position;
                rotation = entry.rotation;
                bounds = entry.bounds;
                return true; // Use the one entry we have
            }

            var entries = history.ToArray();

            // Find entries before and after target time
            HistoryEntry before = null;
            HistoryEntry after = null;

            for (int i = 0; i < entries.Length - 1; i++)
            {
                if (entries[i].timestamp <= targetTime && entries[i + 1].timestamp >= targetTime)
                {
                    before = entries[i];
                    after = entries[i + 1];
                    break;
                }
            }

            if (before == null || after == null)
            {
                // Target time outside history range - use closest entry
                if (targetTime < entries[0].timestamp)
                {
                    // ✅ DEBUG: Log warning for extremely high ping (> 1000ms) - DOUBLE PRECISION
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    double rewindDiff = entries[0].timestamp - targetTime;
                    if (rewindDiff > 0.1) // More than 100ms outside buffer
                    {
                        Debug.LogWarning($"⚠️ [LAG COMP] Rewind time too old by {rewindDiff * 1000.0:F0}ms (ping > 1s?)");
                    }
                    #endif

                    // Too old - use oldest available
                    position = entries[0].position;
                    rotation = entries[0].rotation;
                    bounds = entries[0].bounds;
                }
                else
                {
                    // Too new - use latest available
                    position = entries[entries.Length - 1].position;
                    rotation = entries[entries.Length - 1].rotation;
                    bounds = entries[entries.Length - 1].bounds;
                }
                return true;
            }

            // ✅ AAA QUALITY: Interpolate between before and after for smooth rewind (DOUBLE PRECISION)
            double timeDiff = after.timestamp - before.timestamp;
            if (timeDiff > 0.0001) // Avoid division by zero
            {
                float t = (float)((targetTime - before.timestamp) / timeDiff);
                t = Mathf.Clamp01(t); // Safety clamp

                position = Vector3.Lerp(before.position, after.position, t);
                rotation = Quaternion.Slerp(before.rotation, after.rotation, t);

                // Interpolate bounds center (size stays same)
                bounds.center = Vector3.Lerp(before.bounds.center, after.bounds.center, t);
                bounds.size = before.bounds.size; // Use before's size (shouldn't change)
            }
            else
            {
                // Timestamps too close - use before
                position = before.position;
                rotation = before.rotation;
                bounds = before.bounds;
            }

            return true;
        }

        /// <summary>
        /// Get latest recorded position (for fallback)
        /// </summary>
        public bool GetLatestPosition(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (history.Count == 0) return false;

            var entries = history.ToArray();
            var latest = entries[entries.Length - 1];

            position = latest.position;
            rotation = latest.rotation;
            return true;
        }

        /// <summary>
        /// Get history entry count (for debugging)
        /// </summary>
        public int GetEntryCount()
        {
            return history.Count;
        }

        /// <summary>
        /// Get history time span (for debugging)
        /// </summary>
        public float GetHistoryTimeSpan()
        {
            if (history.Count < 2) return 0f;

            var entries = history.ToArray();
            return (float)(entries[entries.Length - 1].timestamp - entries[0].timestamp);
        }

        /// <summary>
        /// Clear all history (call on player disconnect or scene change)
        /// </summary>
        public void Clear()
        {
            history.Clear();
        }
    }
}
