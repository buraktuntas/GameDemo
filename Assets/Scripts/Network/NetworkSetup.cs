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
        [SerializeField] private string networkAddress = "localhost";
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
                networkManager.StartServer();
                Debug.Log("Started as Server");
            }
        }

        public void StartClient()
        {
            if (networkManager != null && !NetworkClient.isConnected)
            {
                networkManager.networkAddress = networkAddress;
                networkManager.StartClient();
                Debug.Log($"Started as Client, connecting to {networkAddress}");
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

