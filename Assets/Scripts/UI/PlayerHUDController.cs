using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;
using TacticalCombat.Combat;
using TacticalCombat.Sabotage;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Connects player components to the HUD
    /// </summary>
    public class PlayerHUDController : NetworkBehaviour
    {
        private GameHUD hud;
        private PlayerController player;
        private Health health;
        private AbilityController abilityController;
        private SabotageController sabotageController;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // ✅ PERFORMANCE FIX: Use singleton instead of FindFirstObjectByType
            hud = GameHUD.Instance;
            if (hud == null)
            {
                Debug.LogWarning("❌ [PlayerHUDController] GameHUD.Instance is null! Make sure GameHUD exists in scene.");
                return;
            }

            // Get components
            player = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            abilityController = GetComponent<AbilityController>();
            sabotageController = GetComponent<SabotageController>();

            // Subscribe to events
            if (health != null)
            {
                health.OnHealthChangedEvent += OnHealthChanged;
            }

            if (abilityController != null)
            {
                abilityController.OnCooldownChanged += OnAbilityCooldownChanged;
            }

            if (sabotageController != null)
            {
                sabotageController.OnSabotageProgress += OnSabotageProgress;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || hud == null) return;

            // Update resources if in build phase
            if (MatchManager.Instance != null && 
                MatchManager.Instance.GetCurrentPhase() == Phase.Build &&
                player != null)
            {
                var playerState = MatchManager.Instance.GetPlayerState(player.playerId);
                if (playerState != null)
                {
                    hud.UpdateResources(playerState.budget);
                }
            }

            // Update ability cooldown
            if (abilityController != null)
            {
                float cooldown = abilityController.GetCooldownRemaining();
                float maxCooldown = GetMaxCooldown(player.role);
                hud.UpdateAbilityCooldown(cooldown, maxCooldown);
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            if (hud != null)
            {
                hud.UpdateHealth(current, max);
            }
        }

        private void OnAbilityCooldownChanged(float remaining)
        {
            // Handled in Update for smoother visualization
        }

        private void OnSabotageProgress(float progress)
        {
            if (hud != null)
            {
                hud.UpdateSabotageProgress(progress);
            }
        }

        private float GetMaxCooldown(RoleId role)
        {
            return role switch
            {
                RoleId.Builder => GameConstants.BUILDER_RAPID_DEPLOY_COOLDOWN,
                RoleId.Guardian => GameConstants.GUARDIAN_BULWARK_COOLDOWN,
                RoleId.Ranger => GameConstants.RANGER_SCOUT_ARROW_COOLDOWN,
                RoleId.Saboteur => GameConstants.SABOTEUR_SHADOW_STEP_COOLDOWN,
                _ => 30f
            };
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChangedEvent -= OnHealthChanged;
            }

            if (abilityController != null)
            {
                abilityController.OnCooldownChanged -= OnAbilityCooldownChanged;
            }

            if (sabotageController != null)
            {
                sabotageController.OnSabotageProgress -= OnSabotageProgress;
            }
        }
    }
}

