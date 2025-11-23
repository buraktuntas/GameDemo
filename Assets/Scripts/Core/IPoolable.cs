using UnityEngine;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Interface for objects that need to be reset when retrieved from or returned to the pool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from the pool (Get) or returned to the pool (Release).
        /// Use this to reset state (velocity, health, visuals, etc.).
        /// </summary>
        void OnReset();
    }
}
