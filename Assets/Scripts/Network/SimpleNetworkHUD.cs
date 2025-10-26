using UnityEngine;
using Mirror;

namespace TacticalCombat.Network
{
    /// <summary>
    /// Basit network test HUD - Host/Client butonları
    /// NetworkManager'a ekle
    /// </summary>
    public class SimpleNetworkHUD : MonoBehaviour
    {
        private NetworkManager networkManager;
        
        [Header("Port Settings")]
        [SerializeField] private ushort port = 7777; // Her ikisi için aynı port

        private void Start()
        {
            networkManager = GetComponent<NetworkManager>();
            
            // Her ikisi için aynı port kullan
            var transport = networkManager.transport as kcp2k.KcpTransport;
            if (transport != null)
            {
                transport.port = port;
                Debug.Log($"🎮 [SimpleNetworkHUD] Port: {port}");
            }
            
            // ✅ FIX: Spawnable prefabs kontrolü
            EnsurePlayerPrefabInSpawnableList();
        }
        
        /// <summary>
        /// Player prefab'ın spawnable listesinde olduğundan emin ol
        /// </summary>
        private void EnsurePlayerPrefabInSpawnableList()
        {
            if (networkManager.playerPrefab != null)
            {
                // ✅ FIX: AssetId kontrolü
                var netIdentity = networkManager.playerPrefab.GetComponent<NetworkIdentity>();
                if (netIdentity != null && netIdentity.assetId == 0)
                {
                    Debug.LogError("❌ [SimpleNetworkHUD] Player prefab assetId is 0! This will cause network issues.");
                    Debug.LogError("   Solution: Run 'Tools > Tactical Combat > Recreate Player Prefab (FINAL)'");
                    return;
                }
                
                if (!networkManager.spawnPrefabs.Contains(networkManager.playerPrefab))
                {
                    networkManager.spawnPrefabs.Add(networkManager.playerPrefab);
                    Debug.Log($"✅ [SimpleNetworkHUD] Player prefab spawnable listesine eklendi: {networkManager.playerPrefab.name}");
                }
                else
                {
                    Debug.Log($"✓ [SimpleNetworkHUD] Player prefab zaten spawnable listesinde: {networkManager.playerPrefab.name}");
                }
            }
            else
            {
                Debug.LogError("❌ [SimpleNetworkHUD] Player prefab NULL! NetworkManager'a Player Prefab atayın.");
            }
        }

        private void Update()
        {
            // Klavye kısayolları
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                // H tuşu = Host
                if (Input.GetKeyDown(KeyCode.H))
                {
                    networkManager.StartHost();
                    Debug.Log("🚀 [H] Starting HOST...");
                }

                // C tuşu = Client
                if (Input.GetKeyDown(KeyCode.C))
                {
                    networkManager.StartClient();
                    Debug.Log("🚀 [C] Starting CLIENT...");
                }
            }
        }

        private void OnGUI()
        {
            if (networkManager == null) return;

            // GUI Style
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.padding = new RectOffset(20, 20, 10, 10);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 18;
            labelStyle.normal.textColor = Color.white;

            // Position (top-left corner)
            float x = 10;
            float y = 10;
            float buttonWidth = 150;
            float buttonHeight = 40;
            float spacing = 10;

            // Title
            GUI.Label(new Rect(x, y, 300, 30), "TACTICAL COMBAT - MVP TEST", labelStyle);
            y += 40;

            // Network status
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                // Büyük uyarı kutusu
                GUI.color = new Color(1f, 0.9f, 0.5f);
                GUI.Box(new Rect(x - 5, y - 5, 450, 220), "");
                GUI.color = Color.white;

                GUI.Label(new Rect(x, y, 400, 25), "⚠️ BAĞLANTI YOK", labelStyle);
                y += 35;

                // Talimatlar
                GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
                instructionStyle.fontSize = 14;
                instructionStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);

                GUI.Label(new Rect(x, y, 400, 20), "BUILD'DE:", instructionStyle);
                y += 20;
                GUI.Label(new Rect(x, y, 400, 20), "  → 'LAN HOST (H)' butonuna tıkla", instructionStyle);
                y += 25;

                GUI.Label(new Rect(x, y, 400, 20), "EDITOR'DE:", instructionStyle);
                y += 20;
                GUI.Label(new Rect(x, y, 400, 20), "  → 'LAN CLIENT (C)' butonuna tıkla", instructionStyle);
                y += 30;

                // Host button (Yeşil)
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUI.Button(new Rect(x, y, buttonWidth * 1.8f, buttonHeight), "LAN HOST (H)", buttonStyle))
                {
                    networkManager.StartHost();
                    Debug.Log("🚀 Starting HOST...");
                }
                GUI.backgroundColor = Color.white;
                y += buttonHeight + spacing;

                // Client button (Mavi)
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                if (GUI.Button(new Rect(x, y, buttonWidth * 1.8f, buttonHeight), "LAN CLIENT (C)", buttonStyle))
                {
                    networkManager.StartClient();
                    Debug.Log("🚀 Starting CLIENT...");
                }
                GUI.backgroundColor = Color.white;
                y += buttonHeight + spacing;
            }
            else
            {
                // Show status
                string status = "";
                if (NetworkServer.active && NetworkClient.isConnected)
                    status = "HOST (Server + Client)";
                else if (NetworkServer.active)
                    status = "SERVER";
                else if (NetworkClient.isConnected)
                    status = "CLIENT";

                GUI.Label(new Rect(x, y, 300, 25), $"Status: {status}", labelStyle);
                y += 35;

                // Connection info
                if (NetworkClient.isConnected)
                {
                    GUI.Label(new Rect(x, y, 300, 25), 
                        $"Connected to: {networkManager.networkAddress}", labelStyle);
                    y += 30;
                }

                // Player count (if server)
                if (NetworkServer.active)
                {
                    GUI.Label(new Rect(x, y, 300, 25), 
                        $"Players: {NetworkServer.connections.Count}", labelStyle);
                    y += 30;
                }

                y += 10;

                // Stop button
                if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "STOP", buttonStyle))
                {
                    if (NetworkServer.active && NetworkClient.isConnected)
                        networkManager.StopHost();
                    else if (NetworkClient.isConnected)
                        networkManager.StopClient();
                    else if (NetworkServer.active)
                        networkManager.StopServer();

                    Debug.Log("⏹️ Stopping network...");
                }
            }

            // Instructions (bottom-left)
            y = Screen.height - 120;
            GUI.Label(new Rect(x, y, 400, 25), "CONTROLS:", labelStyle);
            y += 30;
            GUI.Label(new Rect(x, y, 400, 25), "• WASD - Hareket", labelStyle);
            y += 25;
            GUI.Label(new Rect(x, y, 400, 25), "• Mouse - Kamera", labelStyle);
            y += 25;
            GUI.Label(new Rect(x, y, 400, 25), "• Space - Zıpla", labelStyle);
        }
    }
}



