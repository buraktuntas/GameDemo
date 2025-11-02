using UnityEngine;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Damage types for different sources
    /// </summary>
    public enum DamageType
    {
        Bullet,
        Melee,
        Fall,
        Explosion,
        Fire,
        Poison
    }
    
    /// <summary>
    /// Damage information structure
    /// </summary>
    public struct DamageInfo
    {
        public int Amount;
        public ulong AttackerID;
        public DamageType Type;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float Force; // For knockback effects
        public bool IsHeadshot; // For headshot indicator

        public DamageInfo(int amount, ulong attackerId, DamageType type, Vector3 hitPoint, Vector3 hitNormal = default, float force = 0f, bool isHeadshot = false)
        {
            Amount = amount;
            AttackerID = attackerId;
            Type = type;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Force = force;
            IsHeadshot = isHeadshot;
        }
    }
    
    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object
        /// </summary>
        /// <param name="damageInfo">Damage information</param>
        void ApplyDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// Check if this object is currently alive
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Get current health percentage (0-1)
        /// </summary>
        float HealthPercentage { get; }
    }
}
