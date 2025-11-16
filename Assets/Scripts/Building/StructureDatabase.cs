using UnityEngine;
using System.Collections.Generic;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    /// <summary>
    /// ✅ NEW: Database for StructureSO ScriptableObjects (per Level Design Spec)
    /// Provides centralized access to structure configurations
    /// </summary>
    [CreateAssetMenu(fileName = "StructureDatabase", menuName = "Tactical Combat/Structure Database")]
    public class StructureDatabase : ScriptableObject
    {
        [Header("Structure Configurations")]
        [SerializeField] private List<StructureSO> structures = new List<StructureSO>();
        
        private Dictionary<string, StructureSO> structureDict;
        private Dictionary<StructureType, StructureSO> structureTypeDict;
        
        /// <summary>
        /// Initialize dictionary for fast lookup
        /// </summary>
        private void OnEnable()
        {
            BuildDictionary();
        }
        
        private void BuildDictionary()
        {
            structureDict = new Dictionary<string, StructureSO>();
            structureTypeDict = new Dictionary<StructureType, StructureSO>();
            
            foreach (var structure in structures)
            {
                if (structure == null) continue;
                
                // Index by ID
                if (!string.IsNullOrEmpty(structure.id))
                {
                    structureDict[structure.id] = structure;
                }
                
                // Index by StructureType
                structureTypeDict[structure.structureType] = structure;
            }
        }
        
        /// <summary>
        /// Get structure by ID (per spec: StructureDatabase.Get(structId))
        /// </summary>
        public static StructureSO Get(string structId)
        {
            // Find database instance
            var database = Resources.Load<StructureDatabase>("StructureDatabase");
            if (database == null)
            {
                Debug.LogWarning($"[StructureDatabase] Database not found at Resources/StructureDatabase.asset");
                return null;
            }
            
            if (database.structureDict == null || database.structureDict.Count == 0)
            {
                database.BuildDictionary();
            }
            
            if (database.structureDict.TryGetValue(structId, out StructureSO structure))
            {
                return structure;
            }
            
            Debug.LogWarning($"[StructureDatabase] Structure '{structId}' not found");
            return null;
        }
        
        /// <summary>
        /// Get structure by StructureType
        /// </summary>
        public static StructureSO Get(StructureType type)
        {
            var database = Resources.Load<StructureDatabase>("StructureDatabase");
            if (database == null)
            {
                Debug.LogWarning($"[StructureDatabase] Database not found at Resources/StructureDatabase.asset");
                return null;
            }
            
            if (database.structureTypeDict == null || database.structureTypeDict.Count == 0)
            {
                database.BuildDictionary();
            }
            
            if (database.structureTypeDict.TryGetValue(type, out StructureSO structure))
            {
                return structure;
            }
            
            Debug.LogWarning($"[StructureDatabase] Structure type '{type}' not found");
            return null;
        }
        
        /// <summary>
        /// Get all structures
        /// </summary>
        public static List<StructureSO> GetAll()
        {
            var database = Resources.Load<StructureDatabase>("StructureDatabase");
            if (database == null)
            {
                return new List<StructureSO>();
            }
            
            return new List<StructureSO>(database.structures);
        }
        
        /// <summary>
        /// Get structures by category
        /// </summary>
        public static List<StructureSO> GetByCategory(StructureCategory category)
        {
            var all = GetAll();
            var filtered = new List<StructureSO>();
            
            foreach (var structure in all)
            {
                if (structure != null && structure.category == category)
                {
                    filtered.Add(structure);
                }
            }
            
            return filtered;
        }
    }
}

