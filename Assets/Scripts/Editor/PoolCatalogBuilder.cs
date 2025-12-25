using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;
using TacticalCombat.Core;
using System.IO;

namespace TacticalCombat.Editor
{
    public class PoolCatalogBuilder : EditorWindow
    {
        [MenuItem("Tactical Combat/Rebuild Pool Catalog")]
        [InitializeOnLoadMethod] // ✅ Run on compile/load
        public static void RebuildCatalog()
        {
            string folderPath = "Assets/Resources";
            string assetPath = "Assets/Resources/PoolCatalog.asset";

            // Ensure Resources folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Always load or create to ensure we have the latest references
            PoolCatalog catalog = Resources.Load<PoolCatalog>("PoolCatalog");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PoolCatalog>();
                AssetDatabase.CreateAsset(catalog, assetPath);
            }

            // Find all prefabs with NetworkIdentity
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            List<PoolCatalog.Entry> entries = new List<PoolCatalog.Entry>();
            int addedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // ✅ FIX: Exclude Mirror examples and packages to prevent crashes
                if (path.StartsWith("Assets/Mirror") || path.Contains("/Examples/") || path.Contains("/Samples/"))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;
                if (prefab.GetComponent<NetworkIdentity>() == null) continue;

                // 🛑 SIMPLIFICATION: Only pool high-frequency objects to avoid conflicts with Mirror's default spawning
                // and to keep the logic simple as requested.
                // We ONLY pool: Projectiles, Bullets, VFX, Effects.
                // We DO NOT pool: Walls, Floors, Managers, Players, Doors, etc.
                
                string pName = prefab.name;
                bool shouldPool = false;
                int serverCount = 5;
                int clientCount = 5;

                if (pName.Contains("Projectile") || pName.Contains("Bullet"))
                {
                    shouldPool = true;
                    serverCount = 50;
                    clientCount = 50;
                }
                else if (pName.Contains("Effect") || pName.Contains("VFX") || pName.Contains("Muzzle") || pName.Contains("Flash"))
                {
                    shouldPool = true;
                    serverCount = 20;
                    clientCount = 20;
                }
                
                // If it's not a high-frequency object, SKIP IT.
                // This resolves "Adding prefab... when spawnHandlers already exists" for things like Walls/LobbyManager.
                if (!shouldPool)
                {
                    continue;
                }

                entries.Add(new PoolCatalog.Entry
                {
                    prefab = prefab,
                    serverPrewarmCount = serverCount,
                    clientPrewarmCount = clientCount
                });
                addedCount++;
            }

            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ [PoolCatalogBuilder] Rebuilt catalog with {addedCount} network prefabs.");
        }
    }
}
