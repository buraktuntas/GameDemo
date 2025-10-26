using UnityEngine;
using Mirror;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// NetworkManager ayarlarını validate eder
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkManagerValidator : MonoBehaviour
    {
        private void Start()
        {
            var nm = GetComponent<NetworkManager>();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 NETWORK MANAGER VALIDATION");
            Debug.Log("═══════════════════════════════════════");

            // Player Prefab
            if (nm.playerPrefab == null)
            {
                Debug.LogError("❌ CRITICAL: Player Prefab is NULL!");
                Debug.LogError("   Solution: Assign Player prefab in NetworkManager Inspector");
            }
            else
            {
                Debug.Log($"✅ Player Prefab: {nm.playerPrefab.name}");

                // Check if player prefab has NetworkIdentity
                var netId = nm.playerPrefab.GetComponent<NetworkIdentity>();
                if (netId == null)
                {
                    Debug.LogError("❌ CRITICAL: Player Prefab has NO NetworkIdentity!");
                    Debug.LogError("   Solution: Add NetworkIdentity component to Player prefab");
                }
                else
                {
                    Debug.Log($"✅ Player has NetworkIdentity (AssetID: {netId.assetId})");
                }

                // Check for NetworkBehaviour components
                var behaviours = nm.playerPrefab.GetComponents<NetworkBehaviour>();
                Debug.Log($"✅ Player has {behaviours.Length} NetworkBehaviour components:");
                foreach (var behaviour in behaviours)
                {
                    Debug.Log($"   - {behaviour.GetType().Name}");
                }
            }

            Debug.Log("═══════════════════════════════════════");
        }

        [ContextMenu("Force Validate")]
        public void ForceValidate()
        {
            Start();
        }
    }
}
