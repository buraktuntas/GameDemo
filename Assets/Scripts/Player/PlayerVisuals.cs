using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Player görsellerini yönetir - Team renkleri vb.
    /// </summary>
    public class PlayerVisuals : NetworkBehaviour
    {
        [Header("Team Colors")]
        [SerializeField] private Color teamAColor = new Color(0.2f, 0.5f, 1f); // Mavi
        [SerializeField] private Color teamBColor = new Color(1f, 0.3f, 0.3f); // Kırmızı
        [SerializeField] private Color neutralColor = new Color(0.8f, 0.8f, 0.8f); // Gri

        [Header("Components")]
        private Renderer visualRenderer;
        private PlayerController playerController;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            // Find the Visual child's renderer
            Transform visual = transform.Find("Visual");
            if (visual != null)
            {
                visualRenderer = visual.GetComponent<Renderer>();
            }

            playerController = GetComponent<PlayerController>();
            propertyBlock = new MaterialPropertyBlock();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Apply team color when spawned (works for all clients)
            if (playerController != null)
            {
                UpdateTeamColor(playerController.team);
            }
        }

        public void UpdateTeamColor(Team team)
        {
            if (visualRenderer == null)
            {
                Debug.LogWarning("PlayerVisuals: visualRenderer is null!");
                return;
            }

            Color targetColor = neutralColor;

            switch (team)
            {
                case Team.TeamA:
                    targetColor = teamAColor;
                    break;
                case Team.TeamB:
                    targetColor = teamBColor;
                    break;
            }

            // Direct material modification (creates instance automatically)
            if (visualRenderer.material != null)
            {
                visualRenderer.material.color = targetColor;
                
                // Also try specific shader properties
                if (visualRenderer.material.HasProperty("_BaseColor"))
                    visualRenderer.material.SetColor("_BaseColor", targetColor);
                
                if (visualRenderer.material.HasProperty("_Color"))
                    visualRenderer.material.SetColor("_Color", targetColor);
            }

            Debug.Log($"✅ Player visual color updated to {team}: {targetColor}");
        }

        // Called when team changes (if needed)
        public void OnTeamChanged(Team newTeam)
        {
            UpdateTeamColor(newTeam);
        }
    }
}

