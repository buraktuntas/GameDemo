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

        private void Start()
        {
            networkManager = GetComponent<NetworkManager>();
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
                GUI.Label(new Rect(x, y, 300, 25), "Status: Disconnected", labelStyle);
                y += 35;

                // Host button
                if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "HOST", buttonStyle))
                {
                    networkManager.StartHost();
                    Debug.Log("🚀 Starting HOST...");
                }
                y += buttonHeight + spacing;

                // Client button
                if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "CLIENT", buttonStyle))
                {
                    networkManager.StartClient();
                    Debug.Log("🚀 Starting CLIENT...");
                }
                y += buttonHeight + spacing;

                // Server button (headless)
                if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "SERVER ONLY", buttonStyle))
                {
                    networkManager.StartServer();
                    Debug.Log("🚀 Starting SERVER...");
                }
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



