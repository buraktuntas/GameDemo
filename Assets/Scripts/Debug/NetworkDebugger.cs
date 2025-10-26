using UnityEngine;
using Mirror;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// Network durumunu ekranda gösterir
    /// </summary>
    public class NetworkDebugger : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>NETWORK DEBUG</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(10);

            // Network status
            GUILayout.Label($"NetworkServer.active: {NetworkServer.active}");
            GUILayout.Label($"NetworkClient.active: {NetworkClient.active}");
            GUILayout.Label($"NetworkClient.isConnected: {NetworkClient.isConnected}");

            GUILayout.Space(5);

            // Connection count
            GUILayout.Label($"Connections: {NetworkServer.connections.Count}");

            GUILayout.Space(5);

            // Players
            var players = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
            int playerCount = 0;
            int serverCount = 0;
            int clientCount = 0;

            foreach (var player in players)
            {
                if (player.gameObject.name.Contains("Player"))
                {
                    playerCount++;
                    if (player.isServer) serverCount++;
                    if (player.isClient) clientCount++;
                }
            }

            GUILayout.Label($"Player Objects: {playerCount}");
            GUILayout.Label($"  - isServer: {serverCount}");
            GUILayout.Label($"  - isClient: {clientCount}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
