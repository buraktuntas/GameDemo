using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace TacticalCombat.Core
{
    public class NetworkObjectPool : MonoBehaviour
    {
        public static NetworkObjectPool Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(prefab);
                var po = obj.GetComponent<PooledObject>();
                if (po == null) po = obj.AddComponent<PooledObject>();
                po.SetPrefab(prefab);
                obj.SetActive(false);
                pool[prefab].Enqueue(obj);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (!pool.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pool[prefab] = q;
            }

            GameObject obj = q.Count > 0 ? q.Dequeue() : Instantiate(prefab);

            var po = obj.GetComponent<PooledObject>();
            if (po == null)
            {
                po = obj.AddComponent<PooledObject>();
                po.SetPrefab(prefab);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            // Server must (re)spawn to notify clients
            if (NetworkServer.active)
            {
                NetworkServer.Spawn(obj);
            }

            return obj;
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;
            var po = instance.GetComponent<PooledObject>();
            var prefab = po != null ? po.GetPrefab() : null;
            if (prefab == null)
            {
                // no prefab info; fallback to destroy networked object
                if (NetworkServer.active && instance.TryGetComponent<NetworkIdentity>(out _))
                {
                    NetworkServer.Destroy(instance);
                }
                else
                {
                    Destroy(instance);
                }
                return;
            }

            if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

            // Unspawn so clients clean up while server keeps instance pooled
            if (NetworkServer.active && instance.TryGetComponent<NetworkIdentity>(out _))
            {
                NetworkServer.UnSpawn(instance);
            }

            instance.SetActive(false);
            pool[prefab].Enqueue(instance);
        }
    }
}

