using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Player
{
    public class AbilityController : NetworkBehaviour
    {
        [Header("Role Configuration")]
        [SerializeField] private RoleDefinition roleDefinition;

        [Header("Ability State")]
        [SyncVar]
        private float cooldownRemaining = 0f;
        
        [SyncVar]
        private bool isAbilityActive = false;
        
        [SyncVar]
        private float abilityTimeRemaining = 0f;

        private PlayerController playerController;
        private InputAction abilityAction;

        // ✅ PERFORMANCE FIX: Cache frequently accessed components
        private Combat.Health cachedHealth;
        private Combat.WeaponController cachedWeaponController;

        public System.Action<float> OnCooldownChanged;
        public System.Action<bool> OnAbilityActiveChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            
            // ✅ PERFORMANCE FIX: Cache components in Awake (called once)
            cachedHealth = GetComponent<Combat.Health>();
            cachedWeaponController = GetComponent<Combat.WeaponController>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    abilityAction = playerMap.FindAction("UseAbility");
                    if (abilityAction != null)
                    {
                        abilityAction.performed += OnAbilityInput;
                    }
                    else
                    {
                        Debug.LogWarning("[AbilityController] UseAbility action not found in Player action map");
                    }
                }
                else
                {
                    Debug.LogWarning("[AbilityController] Player action map not found");
                }
            }

            // Load role definition based on player's role
            if (playerController != null)
            {
                LoadRoleDefinition(playerController.role);
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerController not found! Using default role.");
                LoadRoleDefinition(RoleId.Builder); // Default role
            }
        }

        private void LoadRoleDefinition(RoleId role)
        {
            // In production, load from Resources or AssetDatabase
            // For now, we'll handle it dynamically
            Debug.Log($"Loading role definition for {role}");
        }

        private void Update()
        {
            if (!isServer) return;

            // Update cooldown
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= Time.deltaTime;
                cooldownRemaining = Mathf.Max(0, cooldownRemaining);
            }

            // Update ability duration
            if (isAbilityActive && abilityTimeRemaining > 0)
            {
                abilityTimeRemaining -= Time.deltaTime;
                if (abilityTimeRemaining <= 0)
                {
                    DeactivateAbility();
                }
            }
        }

        private void OnAbilityInput(InputAction.CallbackContext context)
        {
            if (cooldownRemaining > 0 || isAbilityActive)
            {
                Debug.Log($"Ability on cooldown: {cooldownRemaining}s remaining");
                return;
            }

            CmdActivateAbility();
        }

        [Command]
        private void CmdActivateAbility()
        {
            // Validate
            if (cooldownRemaining > 0 || isAbilityActive)
                return;

            // Phase check
            Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
            if (currentPhase != Phase.Combat && currentPhase != Phase.Build)
                return;

            // ✅ PERFORMANCE FIX: Use cached health component
            // Check if player is alive
            if (cachedHealth == null)
            {
                cachedHealth = GetComponent<Combat.Health>();
            }
            if (cachedHealth != null && cachedHealth.IsDead())
                return;

            // Activate ability based on role
            ActivateAbility();
        }

        [Server]
        private void ActivateAbility()
        {
            RoleId role = playerController.role;
            
            Debug.Log($"Activating ability for {role}");

            switch (role)
            {
                case RoleId.Builder:
                    ActivateBuilderAbility();
                    break;
                case RoleId.Guardian:
                    ActivateGuardianAbility();
                    break;
                case RoleId.Ranger:
                    ActivateRangerAbility();
                    break;
                case RoleId.Saboteur:
                    ActivateSaboteurAbility();
                    break;
            }

            isAbilityActive = true;
            cooldownRemaining = GetAbilityCooldown(role);
            abilityTimeRemaining = GetAbilityDuration(role);

            RpcOnAbilityActivated(role);
        }

        [Server]
        private void DeactivateAbility()
        {
            isAbilityActive = false;
            abilityTimeRemaining = 0f;
            RpcOnAbilityDeactivated();
        }

        // Role-specific ability implementations
        [Server]
        private void ActivateBuilderAbility()
        {
            // Rapid Deploy: Increased build speed
            // This would be checked in BuildPlacementController
            Debug.Log("Builder: Rapid Deploy activated");
        }

        [Server]
        private void ActivateGuardianAbility()
        {
            // Bulwark: Projectile shield
            // This would create a shield component
            Debug.Log("Guardian: Bulwark activated");
            
            var shield = gameObject.AddComponent<GuardianShield>();
            shield.Initialize(GameConstants.GUARDIAN_BULWARK_DURATION);
        }

        [Server]
        private void ActivateRangerAbility()
        {
            // Scout Arrow: Next arrow reveals
            // Set a flag that the next arrow will reveal
            Debug.Log("Ranger: Scout Arrow ready");
            
            // ✅ PERFORMANCE FIX: Use cached weapon controller
            if (cachedWeaponController == null)
            {
                cachedWeaponController = GetComponent<Combat.WeaponController>();
            }
            if (cachedWeaponController != null)
            {
                // Mark next shot as scout arrow
                cachedWeaponController.EnableScoutArrow();
            }
        }

        [Server]
        private void ActivateSaboteurAbility()
        {
            // Shadow Step: Stealth movement
            Debug.Log("Saboteur: Shadow Step activated");
            
            var stealth = gameObject.AddComponent<SaboteurStealth>();
            stealth.Initialize(GameConstants.SABOTEUR_SHADOW_STEP_DURATION);
        }

        [ClientRpc]
        private void RpcOnAbilityActivated(RoleId role)
        {
            OnAbilityActiveChanged?.Invoke(true);
            Debug.Log($"Ability activated: {role}");
        }

        [ClientRpc]
        private void RpcOnAbilityDeactivated()
        {
            OnAbilityActiveChanged?.Invoke(false);
        }

        private float GetAbilityCooldown(RoleId role)
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

        private float GetAbilityDuration(RoleId role)
        {
            return role switch
            {
                RoleId.Builder => GameConstants.BUILDER_RAPID_DEPLOY_DURATION,
                RoleId.Guardian => GameConstants.GUARDIAN_BULWARK_DURATION,
                RoleId.Ranger => GameConstants.RANGER_SCOUT_ARROW_REVEAL_DURATION,
                RoleId.Saboteur => GameConstants.SABOTEUR_SHADOW_STEP_DURATION,
                _ => 5f
            };
        }

        public float GetCooldownRemaining() => cooldownRemaining;
        public bool IsAbilityActive() => isAbilityActive;
        public float GetAbilityTimeRemaining() => abilityTimeRemaining;

        private void OnDisable()
        {
            if (abilityAction != null)
            {
                abilityAction.performed -= OnAbilityInput;
            }
        }
    }

    // Helper components for abilities
    public class GuardianShield : MonoBehaviour
    {
        public void Initialize(float dur)
        {
            Destroy(this, dur);
        }

        public bool BlocksProjectiles() => true;
    }

    public class SaboteurStealth : MonoBehaviour
    {
        private Renderer[] cachedRenderers;
        private Material[] originalMaterials;
        private Material[] stealthMaterials;

        public void Initialize(float dur)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[cachedRenderers.Length];
            stealthMaterials = new Material[cachedRenderers.Length];

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer rend = cachedRenderers[i];
                originalMaterials[i] = rend.sharedMaterial;
                stealthMaterials[i] = new Material(originalMaterials[i]);
                Color c = stealthMaterials[i].color;
                c.a = 0.3f;
                stealthMaterials[i].color = c;
                rend.material = stealthMaterials[i];
            }

            Invoke(nameof(RestoreVisibility), dur);
        }

        private void RestoreVisibility()
        {
            if (cachedRenderers == null) return;

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].sharedMaterial = originalMaterials[i];
                }

                if (stealthMaterials[i] != null)
                {
                    Destroy(stealthMaterials[i]);
                }
            }

            Destroy(this);
        }

        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Cancel Invoke on destroy to prevent memory leaks
            CancelInvoke(nameof(RestoreVisibility)); // Cancel RestoreVisibility Invoke if object destroyed early
            
            if (stealthMaterials != null)
            {
                foreach (var mat in stealthMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
        }

        public bool IsSilent() => true;
    }
}
