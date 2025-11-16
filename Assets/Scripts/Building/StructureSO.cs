using UnityEngine;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    /// <summary>
    /// ✅ NEW: ScriptableObject for structure configuration (per Level Design Spec)
    /// Centralizes structure data: prefab, cost, health, placement rules
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructure", menuName = "Tactical Combat/Structure Data")]
    public class StructureSO : ScriptableObject
    {
        [Header("Structure Identity")]
        public string id; // Unique identifier (e.g., "WoodWall", "MetalWall")
        public StructureType structureType;
        public StructureCategory category;
        
        [Header("Prefabs")]
        public GameObject prefab; // Structure prefab
        public GameObject ghostPrefab; // Build ghost preview prefab
        
        [Header("Cost & Health")]
        public int cost = 1;
        public int maxHealth = 100;
        
        [Header("Placement Rules")]
        public float placeRadius = 0.5f; // Minimum distance from other structures
        public LayerMask allowedSurface; // Surfaces this structure can be placed on
        public string[] tags; // Additional tags for filtering
        
        [Header("Visual")]
        public Material teamAMaterial;
        public Material teamBMaterial;
        
        [Header("Destruction Effects")]
        public GameObject destructionEffect;
        public AudioClip destructionSound;
        
        /// <summary>
        /// Validate structure data
        /// </summary>
        private void OnValidate()
        {
            // Auto-generate ID from name if not set
            if (string.IsNullOrEmpty(id))
            {
                id = name;
            }
            
            // Ensure cost is positive
            if (cost < 0)
            {
                cost = 1;
            }
            
            // Ensure health is positive
            if (maxHealth < 0)
            {
                maxHealth = 100;
            }
        }
        
        /// <summary>
        /// Get structure cost (fallback to static method if not set)
        /// </summary>
        public int GetCost()
        {
            if (cost > 0)
                return cost;
            
            // Fallback to static method
            return Structure.GetStructureCost(structureType);
        }
        
        /// <summary>
        /// Get structure health (fallback to static method if not set)
        /// </summary>
        public int GetMaxHealth()
        {
            if (maxHealth > 0)
                return maxHealth;
            
            // Fallback to static method (via GameConstants)
            return Structure.GetStructureHealth(structureType);
        }
        
        /// <summary>
        /// Check if structure can be placed on given surface
        /// </summary>
        public bool CanPlaceOnSurface(LayerMask surfaceLayer)
        {
            return (allowedSurface & surfaceLayer) != 0;
        }
    }
}

