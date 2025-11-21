using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

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
                
                // ✅ CRITICAL FIX: DualMode'u aktif et (IPv4 ve IPv6 desteği)
                if (!transport.DualMode)
                {
                    transport.DualMode = true;
                    Debug.Log($"✅ [SimpleNetworkHUD] DualMode enabled for IPv4/IPv6 support");
                }
            }
            
            // ✅ CRITICAL FIX: Host için networkAddress'i boş bırak (tüm interface'lerde dinler)
            // Client için kullanıcı IP girecek, bu yüzden burada ayarlamıyoruz
            // Eğer localhost ayarlıysa, sadece uyarı ver ama değiştirme (client için gerekebilir)
            if (!string.IsNullOrWhiteSpace(networkManager.networkAddress))
            {
                if (networkManager.networkAddress == "localhost" || networkManager.networkAddress == "127.0.0.1")
                {
                    Debug.LogWarning($"⚠️ [SimpleNetworkHUD] networkAddress is '{networkManager.networkAddress}'");
                    Debug.LogWarning("   For LAN hosting, consider leaving networkAddress empty in NetworkManager.");
                    Debug.LogWarning("   Client will set the correct IP when connecting.");
                }
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
                if (Keyboard.current == null) return;

                // H tuşu = Host
                if (Keyboard.current.hKey.wasPressedThisFrame)
                {
                    // ✅ CRITICAL FIX: Host başlatılırken networkAddress'i temizle
                    if (networkManager.networkAddress == "localhost" || networkManager.networkAddress == "127.0.0.1")
                    {
                        networkManager.networkAddress = "";
                        Debug.Log("✅ [H] Cleared networkAddress for host (will listen on all interfaces)");
                    }
                    networkManager.StartHost();
                    Debug.Log("🚀 [H] Starting HOST...");
                }

                // C tuşu = Client
                if (Keyboard.current.cKey.wasPressedThisFrame)
                {
                    // Client için networkAddress ayarlanmalı (kullanıcıdan alınmalı)
                    // Bu basit HUD'da default olarak localhost kullanıyoruz
                    if (string.IsNullOrWhiteSpace(networkManager.networkAddress))
                    {
                        networkManager.networkAddress = "127.0.0.1";
                        Debug.LogWarning("⚠️ [C] networkAddress was empty, set to 127.0.0.1");
                        Debug.LogWarning("   For LAN connection, set networkAddress to server IP first!");
                    }
                    networkManager.StartClient();
                    Debug.Log($"🚀 [C] Starting CLIENT to {networkManager.networkAddress}...");
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
                    // ✅ CRITICAL FIX: Host başlatılırken networkAddress'i temizle
                    if (networkManager.networkAddress == "localhost" || networkManager.networkAddress == "127.0.0.1")
                    {
                        networkManager.networkAddress = "";
                        Debug.Log("✅ Cleared networkAddress for host (will listen on all interfaces)");
                    }
                    networkManager.StartHost();
                    Debug.Log("🚀 Starting HOST...");
                }
                GUI.backgroundColor = Color.white;
                y += buttonHeight + spacing;

                // Client button (Mavi)
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                if (GUI.Button(new Rect(x, y, buttonWidth * 1.8f, buttonHeight), "LAN CLIENT (C)", buttonStyle))
                {
                    // Client için networkAddress ayarlanmalı
                    if (string.IsNullOrWhiteSpace(networkManager.networkAddress))
                    {
                        networkManager.networkAddress = "127.0.0.1";
                        Debug.LogWarning("⚠️ networkAddress was empty, set to 127.0.0.1");
                        Debug.LogWarning("   For LAN connection, set networkAddress to server IP first!");
                    }
                    networkManager.StartClient();
                    Debug.Log($"🚀 Starting CLIENT to {networkManager.networkAddress}...");
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

            // ✅ REMOVED: Controls help UI (user requested removal - health already shows in bottom left)
        }
    }
}







