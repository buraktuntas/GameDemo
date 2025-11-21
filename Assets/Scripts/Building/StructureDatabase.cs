using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalCombat.Core; // ✅ FIX: Required for StructureType and StructureCategory enums

namespace TacticalCombat.Building
{
    /// <summary>
    /// ✅ NEW: Central database for all structure data.
    /// Replaces hardcoded switch statements in Structure.cs.
    /// </summary>
    [CreateAssetMenu(fileName = "StructureDatabase", menuName = "Tactical Combat/Structure Database")]
    public class StructureDatabase : ScriptableObject
    {
        private static StructureDatabase _instance;
        public static StructureDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<StructureDatabase>("StructureDatabase");
                }
                return _instance;
            }
        }

        [Header("Registered Structures")]
        public List<StructureSO> structures = new List<StructureSO>();

        private Dictionary<StructureType, StructureSO> _lookup;

        private void OnEnable()
        {
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            _lookup = new Dictionary<StructureType, StructureSO>();
            foreach (var s in structures)
            {
                if (s != null && !_lookup.ContainsKey(s.structureType))
                {
                    _lookup.Add(s.structureType, s);
                }
            }
        }

        public StructureSO GetStructureData(StructureType type)
        {
            if (_lookup == null || _lookup.Count == 0)
            {
                InitializeLookup();
            }

            if (_lookup.TryGetValue(type, out var data))
            {
                return data;
            }

            Debug.LogWarning($"[StructureDatabase] No data found for structure type: {type}");
            return null;
        }

        public int GetCost(StructureType type)
        {
            var data = GetStructureData(type);
            return data != null ? data.GetCost() : 1; // Default cost 1
        }

        public int GetHealth(StructureType type)
        {
            var data = GetStructureData(type);
            return data != null ? data.GetMaxHealth() : 100; // Default health 100
        }

        /// <summary>
        /// ✅ NEW: Get structure category (replaces Structure.GetStructureCategory)
        /// </summary>
        public StructureCategory GetCategory(StructureType type)
        {
            var data = GetStructureData(type);
            return data != null ? data.category : StructureCategory.Wall; // Default to Wall
        }
    }
}
