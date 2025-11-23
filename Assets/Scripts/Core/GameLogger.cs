using UnityEngine;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Production-ready logging system
    /// Automatically filters logs based on build configuration
    /// </summary>
    public static class GameLogger
    {
        /// <summary>
        /// Log info message (only in Editor/Development builds)
        /// </summary>
        public static void LogInfo(string message)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message);
            #endif
        }

        /// <summary>
        /// Log warning (always shown, but less verbose in production)
        /// </summary>
        public static void LogWarning(string message)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(message);
            #else
            // In production, only log critical warnings
            if (message.Contains("❌") || message.Contains("CRITICAL"))
            {
                Debug.LogWarning(message);
            }
            #endif
        }

        /// <summary>
        /// Log error (always shown)
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        /// <summary>
        /// Log network event (only in Editor/Development builds)
        /// </summary>
        public static void LogNetwork(string message)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NETWORK] {message}");
            #endif
        }

        /// <summary>
        /// Log UI event (only in Editor/Development builds)
        /// </summary>
        public static void LogUI(string message)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[UI] {message}");
            #endif
        }
    }
}







