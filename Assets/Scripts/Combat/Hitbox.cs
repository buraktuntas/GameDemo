using UnityEngine;
using TacticalCombat.Combat;

namespace TacticalCombat.Combat
{
    public enum HitZone
    {
        Head,      // 2.5x damage
        Chest,     // 1.0x damage (default)
        Stomach,   // 0.9x damage
        Limbs      // 0.75x damage
    }
    
    /// <summary>
    /// Hitbox component - damage multiplier based on hit location
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("Hitbox Settings")]
        public HitZone zone = HitZone.Chest;
        
        [Header("Damage Multipliers")]
        [Tooltip("Damage multiplier for this hitbox")]
        public float damageMultiplier = 1f;
        
        [Header("Visual Feedback")]
        public Color gizmoColor = Color.yellow;
        
        // Cache
        private Health parentHealth;
        private Collider hitCollider;
        
        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            if (hitCollider == null)
            {
                Debug.LogError($"❌ Hitbox '{name}' has no collider!");
            }
            
            // Set damage multiplier based on zone
            SetMultiplierFromZone();
        }
        
        private void SetMultiplierFromZone()
        {
            damageMultiplier = zone switch
            {
                HitZone.Head => 2.5f,
                HitZone.Chest => 1.0f,
                HitZone.Stomach => 0.9f,
                HitZone.Limbs => 0.75f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Get the Health component from parent
        /// </summary>
        public Health GetParentHealth()
        {
            if (parentHealth == null)
            {
                parentHealth = GetComponentInParent<Health>();
            }
            return parentHealth;
        }
        
        /// <summary>
        /// Calculate final damage with multiplier
        /// </summary>
        public int CalculateDamage(int baseDamage)
        {
            return Mathf.RoundToInt(baseDamage * damageMultiplier);
        }
        
        /// <summary>
        /// Check if this is a critical hit
        /// </summary>
        public bool IsCritical()
        {
            return zone == HitZone.Head;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            
            var col = GetComponent<Collider>();
            if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius * transform.lossyScale.x);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Draw capsule approximation
                Gizmos.DrawWireSphere(transform.position + Vector3.up * capsule.height/2, capsule.radius);
                Gizmos.DrawWireSphere(transform.position + Vector3.down * capsule.height/2, capsule.radius);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw zone label
            UnityEditor.Handles.Label(transform.position, $"{zone}\n{damageMultiplier}x");
        }
        #endif
    }
}
