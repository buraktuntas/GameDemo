using UnityEngine;

namespace TacticalCombat.Core
{
    [CreateAssetMenu(fileName = "PoolCatalog", menuName = "Tactical Combat/Pool Catalog")]
    public class PoolCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public GameObject prefab;
            public int serverPrewarmCount;
            public int clientPrewarmCount;
        }

        public Entry[] entries;
    }
}
