using UnityEngine;

namespace TacticalCombat.Core
{
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private GameObject originalPrefab;

        public void SetPrefab(GameObject prefab) => originalPrefab = prefab;
        public GameObject GetPrefab() => originalPrefab;
    }
}

