using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Player visual feedback system
    /// Handles team colors, visual effects, and UI feedback
    /// </summary>
    public class PlayerVisuals : NetworkBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Material teamAMaterial;
        [SerializeField] private Material teamBMaterial;
        [SerializeField] private Material neutralMaterial;
        
        [Header("Team Colors")]
        [SerializeField] private Color teamAColor = Color.blue;
        [SerializeField] private Color teamBColor = Color.red;
        [SerializeField] private Color neutralColor = Color.white;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject spawnEffect;
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private GameObject hitEffect;
        
        [Header("UI References")]
        [SerializeField] private Canvas playerUI;
        [SerializeField] private UnityEngine.UI.Text playerNameText;
        [SerializeField] private UnityEngine.UI.Slider healthBar;
        
        // State
        private Team currentTeam = Team.None;
        private bool isInitialized = false;
        
        private void Awake()
        {
            // Find visual renderer if not assigned
            if (visualRenderer == null)
            {
                visualRenderer = GetComponent<Renderer>();
                if (visualRenderer == null)
                {
                    // Try to find in children
                    visualRenderer = GetComponentInChildren<Renderer>();
                }
            }
            
            // Create default materials if not assigned
            CreateDefaultMaterials();
            
            // Initialize with neutral color
            UpdateTeamColor(Team.None);
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Enable UI for local player
            if (playerUI != null)
            {
                playerUI.gameObject.SetActive(true);
            }
            
            // Update player name
            UpdatePlayerName();
            
            isInitialized = true;
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Update team color for all clients
            if (isInitialized)
            {
                UpdateTeamColor(currentTeam);
            }
        }
        
        /// <summary>
        /// Update team color based on team assignment
        /// </summary>
        public void UpdateTeamColor(Team team)
        {
            if (visualRenderer == null)
            {
                Debug.LogWarning("PlayerVisuals: visualRenderer is null!");
                return;
            }
            
            currentTeam = team;
            
            Material targetMaterial = neutralMaterial;
            
            switch (team)
            {
                case Team.TeamA:
                    targetMaterial = teamAMaterial;
                    break;
                case Team.TeamB:
                    targetMaterial = teamBMaterial;
                    break;
                case Team.None:
                    targetMaterial = neutralMaterial;
                    break;
            }
            
            if (targetMaterial != null)
            {
                visualRenderer.material = targetMaterial;
            }
            else
            {
                // Fallback: change color directly
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
                
                visualRenderer.material.color = targetColor;
            }
            
            Debug.Log($"🎨 Player team color updated: {team}");
        }
        
        /// <summary>
        /// Update player name display
        /// </summary>
        public void UpdatePlayerName()
        {
            if (playerNameText != null)
            {
                string playerName = $"Player {netId}";
                playerNameText.text = playerName;
            }
        }
        
        /// <summary>
        /// Update health bar display
        /// </summary>
        public void UpdateHealthBar(float healthPercentage)
        {
            if (healthBar != null)
            {
                healthBar.value = healthPercentage;
            }
        }
        
        /// <summary>
        /// Play spawn effect
        /// </summary>
        public void PlaySpawnEffect()
        {
            if (spawnEffect != null)
            {
                GameObject effect = Instantiate(spawnEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        
        /// <summary>
        /// Play death effect
        /// </summary>
        public void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
        }
        
        /// <summary>
        /// Play hit effect
        /// </summary>
        public void PlayHitEffect(Vector3 hitPoint)
        {
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, hitPoint, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }
        
        /// <summary>
        /// Create default materials if not assigned
        /// </summary>
        private void CreateDefaultMaterials()
        {
            if (teamAMaterial == null)
            {
                teamAMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                teamAMaterial.color = teamAColor;
            }
            
            if (teamBMaterial == null)
            {
                teamBMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                teamBMaterial.color = teamBColor;
            }
            
            if (neutralMaterial == null)
            {
                neutralMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                neutralMaterial.color = neutralColor;
            }
        }
        
        /// <summary>
        /// Set visual renderer reference
        /// </summary>
        public void SetVisualRenderer(Renderer renderer)
        {
            visualRenderer = renderer;
            if (isInitialized)
            {
                UpdateTeamColor(currentTeam);
            }
        }
        
        /// <summary>
        /// Get current team
        /// </summary>
        public Team GetCurrentTeam()
        {
            return currentTeam;
        }
        
        /// <summary>
        /// Check if visuals are initialized
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}
