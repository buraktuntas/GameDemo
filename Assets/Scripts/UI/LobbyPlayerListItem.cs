using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TacticalCombat.Network;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Individual player list item component
    /// Optional - for additional functionality per item
    /// </summary>
    public class LobbyPlayerListItem : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI readyText;
        public TextMeshProUGUI teamText;
        public GameObject hostIcon;
        public Image background;

        private LobbyPlayerData playerData;

        public void Setup(LobbyPlayerData data)
        {
            playerData = data;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (nameText != null)
            {
                nameText.text = playerData.playerName;
                if (playerData.isHost)
                    nameText.text += " [HOST]";
            }

            if (readyText != null)
            {
                readyText.text = playerData.isReady ? "[OK] READY" : "[X] NOT READY";
                readyText.color = playerData.isReady ? Color.green : Color.red;
            }

            if (teamText != null)
            {
                switch (playerData.teamId)
                {
                    case 0:
                        teamText.text = "TEAM A";
                        teamText.color = new Color(0.2f, 0.6f, 1f);
                        break;
                    case 1:
                        teamText.text = "TEAM B";
                        teamText.color = new Color(1f, 0.4f, 0.2f);
                        break;
                    default:
                        teamText.text = "NO TEAM";
                        teamText.color = Color.gray;
                        break;
                }
            }

            if (hostIcon != null)
                hostIcon.SetActive(playerData.isHost);
        }
    }
}

