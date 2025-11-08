using UnityEngine;
using Mirror;

namespace TacticalCombat.Network
{
    /// <summary>
    /// Helper script to setup network objects in the scene
    /// </summary>
    public class NetworkSetup : MonoBehaviour
    {
        [Header("Network Configuration")]
        [Tooltip("LEAVE EMPTY for Host/Server (listens on all interfaces). Set to server IP for Client (e.g. '192.168.1.110')")]
        [SerializeField] private string networkAddress = ""; // ✅ FIX: Empty = Host listens on all interfaces
        [SerializeField] private bool autoStartHost = false;

        private NetworkManager networkManager;

        private void Start()
        {
            // ✅ FIX: Cache NetworkManager reference (called once in Start, but good practice)
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
            }

            if (networkManager == null)
            {
                Debug.LogError("❌ [NetworkSetup] No NetworkManager found in scene!");
                return;
            }

            Debug.Log("✅ [NetworkSetup] NetworkManager found and cached");

            if (autoStartHost)
            {
                StartHost();
            }
        }

        public void StartHost()
        {
            if (networkManager != null && !NetworkServer.active && !NetworkClient.isConnected)
            {
                networkManager.StartHost();
                Debug.Log("Started as Host");
            }
        }

        public void StartServer()
        {
            if (networkManager != null && !NetworkServer.active)
            {
                // ✅ FIX: Server should NEVER use networkAddress - always bind to all interfaces
                networkManager.networkAddress = ""; // Empty = 0.0.0.0 (all interfaces)
                networkManager.StartServer();
                Debug.Log("✅ [NetworkSetup] Started as Server (listening on all interfaces)");
            }
        }

        public void StartClient()
        {
            if (networkManager != null && !NetworkClient.isConnected)
            {
                // ✅ FIX: Validate client address
                if (string.IsNullOrEmpty(networkAddress))
                {
                    Debug.LogError("❌ [NetworkSetup] Client mode requires a server IP address! Please set networkAddress in Inspector (e.g. '192.168.1.110')");
                    return;
                }

                networkManager.networkAddress = networkAddress;
                networkManager.StartClient();
                Debug.Log($"✅ [NetworkSetup] Started as Client, connecting to {networkAddress}");
            }
        }

        public void StopNetwork()
        {
            if (networkManager != null)
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    networkManager.StopHost();
                }
                else if (NetworkClient.isConnected)
                {
                    networkManager.StopClient();
                }
                else if (NetworkServer.active)
                {
                    networkManager.StopServer();
                }
                
                Debug.Log("Network stopped");
            }
        }

        public void SetNetworkAddress(string address)
        {
            networkAddress = address;
        }
    }
}

