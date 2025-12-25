using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace TacticalCombat.Core
{
    public class NetworkObjectPool : MonoBehaviour
    {
        public static NetworkObjectPool Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private PoolCatalog poolCatalog;

        // Runtime state
        private readonly Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<uint, GameObject> assetIdToPrefab = new Dictionary<uint, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePool();
        }

        private void InitializePool()
        {
            // ✅ FIX: Fallback to Resources if reference is missing
            if (poolCatalog == null)
            {
                poolCatalog = Resources.Load<PoolCatalog>("PoolCatalog");
                if (poolCatalog != null)
                {
                    Debug.Log("[NetworkObjectPool] Loaded PoolCatalog from Resources.");
                }
            }

            if (poolCatalog == null)
            {
                Debug.LogError("[NetworkObjectPool] PoolCatalog is missing! use 'Tactical Combat > Rebuild Pool Catalog' menu.");
                return;
            }

            foreach (var entry in poolCatalog.entries)
            {
                if (entry.prefab == null) continue;

                // Register AssetId mapping
                if (entry.prefab.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    assetIdToPrefab[identity.assetId] = entry.prefab;
                    
                    // ✅ CRITICAL FIX: Register Mirror Spawn Handlers
                    // This tells Mirror: "When you want to spawn this AssetId, ask ME for the object"
                    // This allows us to give Mirror a pooled object instead of it instantiating a new one
                    NetworkClient.RegisterSpawnHandler(identity.assetId, OnSpawnByMessage, UnSpawnHandler);
                }
                else
                {
                    Debug.LogWarning($"[NetworkObjectPool] Prefab {entry.prefab.name} missing NetworkIdentity!");
                }

                // Prewarm (Server & Client logic handled internally by Prewarm method if needed, 
                // but usually we just initialize the queue structure here)
                if (!pool.ContainsKey(entry.prefab))
                {
                    pool[entry.prefab] = new Queue<GameObject>();
                }
            }
            
            Debug.Log($"[NetworkObjectPool] Initialized with {poolCatalog.entries.Length} entries. Handlers registered.");
        }

        // Mirror Spawn Handler (Client Side)
        private GameObject OnSpawnByMessage(SpawnMessage msg)
        {
            return SpawnHandlerCommon(msg.assetId, msg.position, msg.rotation);
        }

        // Overload for older Mirror versions or manual calls
        private GameObject OnSpawnByPosition(Vector3 position, uint assetId)
        {
             return SpawnHandlerCommon(assetId, position, Quaternion.identity);
        }

        private GameObject SpawnHandlerCommon(uint assetId, Vector3 position, Quaternion rotation)
        {
            if (!assetIdToPrefab.TryGetValue(assetId, out var prefab))
            {
                Debug.LogError($"[NetworkObjectPool] SpawnHandler: Unknown AssetId {assetId}");
                return null;
            }

            // Get from local pool (Client side pooling!)
            GameObject obj = GetFromPool(prefab, position, rotation);
            return obj;
        }

        // Mirror UnSpawn Handler (Client Side)
        private void UnSpawnHandler(GameObject spawned)
        {
            ReleaseToPool(spawned);
        }

        // Internal method to get from queue or instantiate
        private GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

            Queue<GameObject> q = pool[prefab];
            GameObject obj = null;

            // Try to find valid object in queue
            while (q.Count > 0)
            {
                obj = q.Dequeue();
                if (obj != null) break; 
            }

            // Create new if needed
            if (obj == null)
            {
                obj = Instantiate(prefab);
                // Ensure PooledObject component exists
                var po = obj.GetComponent<PooledObject>();
                if (po == null) po = obj.AddComponent<PooledObject>();
                po.SetPrefab(prefab);
            }

            // Move to position
            obj.transform.SetPositionAndRotation(position, rotation);
            
            // ✅ CRITICAL FIX: Reset state (IPoolable)
            var poolable = obj.GetComponent<IPoolable>();
            if (poolable != null) poolable.OnReset();

            obj.SetActive(true);
            return obj;
        }

        private void ReleaseToPool(GameObject instance)
        {
            if (instance == null) return;
            
            var po = instance.GetComponent<PooledObject>();
            var prefab = po != null ? po.GetPrefab() : null;

            if (prefab == null)
            {
                // Not a pooled object, just destroy
                Destroy(instance);
                return;
            }

            // ✅ CRITICAL FIX: Reset state before disabling
            var poolable = instance.GetComponent<IPoolable>();
            if (poolable != null) poolable.OnReset();

            instance.SetActive(false);
            
            // Add back to pool
            if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();
            pool[prefab].Enqueue(instance);
        }

        /// <summary>
        /// Server-side method to spawn a valid networked object from pool
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[NetworkObjectPool] Get called on Client! Use local instantiation or Command.");
                return null;
            }

            // Get the object locally on server
            GameObject obj = GetFromPool(prefab, position, rotation);

            // Network spawn it (Mirror will send Spawn message to clients)
            // Clients will receive AssetId, and their SpawnHandler will be called
            // Their SpawnHandler will call GetFromPool locally
            NetworkServer.Spawn(obj);

            return obj;
        }

        public void Release(GameObject instance)
        {
            // Server-side release
            if (NetworkServer.active)
            {
                // UnSpawn tells clients to destroy/unspawn the object
                // Clients will receive UnSpawn message, calling UnSpawnHandler
                // UnSpawnHandler will call ReleaseToPool locally
                NetworkServer.UnSpawn(instance);
                
                // Finally release locally on server
                ReleaseToPool(instance);
            }
            else
            {
                // Client-side force release (shouldn't happen often for networked objects)
                ReleaseToPool(instance);
            }
        }
        
        // Manual prewarm if needed
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                try 
                {
                    var obj = Instantiate(prefab);
                    var po = obj.GetComponent<PooledObject>();
                    if (po == null) po = obj.AddComponent<PooledObject>();
                    po.SetPrefab(prefab);
                    obj.SetActive(false);
                    pool[prefab].Enqueue(obj);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[NetworkObjectPool] Failed to prewarm '{prefab.name}': {e.Message}");
                    // Stop trying to prewarm this broken prefab to avoid spamming logs
                    break;
                }
            }
        }
    }
}

